using UnityEngine;
using Bender_Dios.MenuRadial.Components.Illumination;

namespace Bender_Dios.MenuRadial.Components.Menu
{
#if UNITY_EDITOR
    using UnityEditor;

    /// <summary>
    /// Renderizador de deslizador radial para MRIluminacionRadial
    /// Permite controlar la iluminación con un slider circular
    /// VERSIÓN 0.002: Corregido - Usa material.SetFloat() directo como el original
    /// </summary>
    public class IlluminationSliderRenderer
    {
        private MRIluminacionRadial _targetIllumination;
        private float _currentValue = 0.5f; // Valor 0-1 que representa el progreso (default 50%)
        private float _currentAngle = 180f; // Ángulo actual del cursor (0-360)

        // Cache de materiales y propiedades originales para restauración
        private System.Collections.Generic.List<Material> _cachedMaterials = new System.Collections.Generic.List<Material>();
        private System.Collections.Generic.Dictionary<Material, float[]> _originalMaterialProperties = new System.Collections.Generic.Dictionary<Material, float[]>();
        private bool _hasStoredOriginals = false;

        // Configuración visual (MISMO color que RadialSliderRenderer para consistencia)
        private readonly Color _backgroundColor = new Color(0.15f, 0.25f, 0.25f, 0.8f);
        private readonly Color _activeColor = new Color(0f, 0.8f, 0.8f, 0.9f);
        private readonly Color _innerCircleColor = new Color(0.25f, 0.4f, 0.45f, 1f);
        private readonly Color _borderColor = new Color(0f, 0.6f, 0.6f, 0.6f);
        private readonly Color _cursorColor = new Color(0f, 1f, 1f, 1f);

        // Configuración de tamaño
        private const float OUTER_RADIUS_RATIO = 0.85f;
        private const float INNER_RADIUS_RATIO = 0.4f;
        private const float CURSOR_SIZE = 5f;

        public IlluminationSliderRenderer(MRIluminacionRadial targetIllumination)
        {
            _targetIllumination = targetIllumination;

            // Inicializar valor al 50% por defecto (frame 127)
            _currentValue = 0.5f;
            _currentAngle = 180f;

            // Cachear materiales y guardar propiedades originales
            if (_targetIllumination != null)
            {
                CacheMaterialsAndStoreOriginals();
            }
        }

        /// <summary>
        /// Cachea los materiales del componente y guarda sus propiedades originales
        /// </summary>
        private void CacheMaterialsAndStoreOriginals()
        {
            if (_targetIllumination == null) return;

            // Asegurar que los materiales estén escaneados
            if (_targetIllumination.DetectedMaterials.Count == 0)
            {
                _targetIllumination.ScanMaterials();
            }

            _cachedMaterials.Clear();
            _originalMaterialProperties.Clear();

            foreach (var material in _targetIllumination.DetectedMaterials)
            {
                if (material == null) continue;

                _cachedMaterials.Add(material);

                // Guardar propiedades originales [AsUnlit, LightMaxLimit, ShadowBorder, ShadowStrength]
                if (!_hasStoredOriginals)
                {
                    float[] originals = new float[4];
                    originals[0] = material.HasProperty("_AsUnlit") ? material.GetFloat("_AsUnlit") : 0f;
                    originals[1] = material.HasProperty("_LightMaxLimit") ? material.GetFloat("_LightMaxLimit") : 1f;
                    originals[2] = material.HasProperty("_ShadowBorder") ? material.GetFloat("_ShadowBorder") : 0.5f;
                    originals[3] = material.HasProperty("_ShadowStrength") ? material.GetFloat("_ShadowStrength") : 0.5f;
                    _originalMaterialProperties[material] = originals;
                }
            }

            _hasStoredOriginals = _originalMaterialProperties.Count > 0;
        }

        /// <summary>
        /// Renderiza el deslizador radial en el área especificada
        /// </summary>
        /// <param name="center">Centro del deslizador</param>
        /// <param name="availableRadius">Radio disponible para el deslizador</param>
        /// <returns>True si hubo cambios que requieren actualización</returns>
        public bool RenderSlider(Vector2 center, float availableRadius)
        {
            if (_targetIllumination == null)
                return false;

            bool hasChanges = false;

            // Calcular radios basados en el espacio disponible
            float outerRadius = availableRadius * OUTER_RADIUS_RATIO;
            float innerRadius = outerRadius * INNER_RADIUS_RATIO;

            // Manejar interacción del mouse primero
            if (HandleMouseInteraction(center, outerRadius))
            {
                hasChanges = true;
                ApplyValueToIllumination();
            }

            // Renderizar componentes visuales
            RenderBackground(center, outerRadius);
            RenderProgressArc(center, outerRadius);
            RenderCenterCircle(center, innerRadius);
            RenderCursor(center, outerRadius);
            RenderCenterText(center);

            return hasChanges;
        }

        /// <summary>
        /// Aplica el valor actual directamente a los materiales
        /// CORREGIDO v0.002: Usa material.SetFloat() directo como IlluminationPreviewManager
        /// Frame 0 (0%):   AsUnlit=0, LightMaxLimit=0.15, ShadowBorder=1, ShadowStrength=1
        /// Frame 127 (50%): AsUnlit=0, LightMaxLimit=1, ShadowBorder=0.05, ShadowStrength=0.5
        /// Frame 255 (100%): AsUnlit=1, LightMaxLimit=1, ShadowBorder=0.05, ShadowStrength=0
        /// </summary>
        private void ApplyValueToIllumination()
        {
            if (_targetIllumination == null)
                return;

            // Asegurar que tenemos materiales cacheados
            if (_cachedMaterials.Count == 0)
            {
                CacheMaterialsAndStoreOriginals();
            }

            // Convertir valor 0-1 a frame 0-255
            int frame = Mathf.RoundToInt(_currentValue * 255f);

            // Interpolar entre los 3 puntos predefinidos (misma lógica que IlluminationPreviewManager)
            float asUnlit, lightMaxLimit, shadowBorder, shadowStrength;

            if (frame <= 127)
            {
                // Interpolación entre frame 0 y frame 127
                float t = frame / 127f;
                asUnlit = Mathf.Lerp(0f, 0f, t);           // 0 → 0
                lightMaxLimit = Mathf.Lerp(0.15f, 1f, t);  // 0.15 → 1
                shadowBorder = Mathf.Lerp(1f, 0.05f, t);   // 1 → 0.05
                shadowStrength = Mathf.Lerp(1f, 0.5f, t);  // 1 → 0.5
            }
            else
            {
                // Interpolación entre frame 127 y frame 255
                float t = (frame - 127) / 128f;
                asUnlit = Mathf.Lerp(0f, 1f, t);           // 0 → 1
                lightMaxLimit = Mathf.Lerp(1f, 1f, t);     // 1 → 1
                shadowBorder = Mathf.Lerp(0.05f, 0.05f, t); // 0.05 → 0.05
                shadowStrength = Mathf.Lerp(0.5f, 0f, t);  // 0.5 → 0
            }

            // CORREGIDO: Aplicar directamente a los materiales (igual que IlluminationPreviewManager)
            foreach (var material in _cachedMaterials)
            {
                if (material == null) continue;

                material.SetFloat("_AsUnlit", asUnlit);
                material.SetFloat("_LightMaxLimit", lightMaxLimit);
                material.SetFloat("_ShadowBorder", shadowBorder);
                material.SetFloat("_ShadowStrength", shadowStrength);
            }
        }

        private void RenderBackground(Vector2 center, float outerRadius)
        {
            Handles.color = _backgroundColor;
            Handles.DrawSolidDisc(center, Vector3.forward, outerRadius);
        }

        private void RenderCenterCircle(Vector2 center, float innerRadius)
        {
            Handles.color = _innerCircleColor;
            Handles.DrawSolidDisc(center, Vector3.forward, innerRadius);

            Handles.color = _borderColor;
            Handles.DrawWireDisc(center, Vector3.forward, innerRadius);
        }

        private void RenderProgressArc(Vector2 center, float outerRadius)
        {
            if (_currentAngle <= 0f)
                return;

            // Crear sector de progreso como polígono suave
            int segments = Mathf.Max(8, Mathf.RoundToInt(_currentAngle / 3f));
            Vector3[] sectorPoints = new Vector3[segments + 2];

            // Primer punto: centro
            sectorPoints[0] = center;

            // Segundo punto: inicio del arco (arriba)
            float startRadians = -90f * Mathf.Deg2Rad;
            sectorPoints[1] = center + new Vector2(Mathf.Cos(startRadians), Mathf.Sin(startRadians)) * outerRadius;

            // Puntos del arco desde arriba hasta el cursor
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                float currentAngle = t * _currentAngle;
                float currentRadians = (currentAngle - 90f) * Mathf.Deg2Rad;
                sectorPoints[i + 2] = center + new Vector2(Mathf.Cos(currentRadians), Mathf.Sin(currentRadians)) * outerRadius;
            }

            Handles.color = _activeColor;
            Handles.DrawAAConvexPolygon(sectorPoints);
        }

        private void RenderCursor(Vector2 center, float outerRadius)
        {
            float cursorRadians = (_currentAngle - 90f) * Mathf.Deg2Rad;
            Vector2 cursorPos = center + new Vector2(Mathf.Cos(cursorRadians), Mathf.Sin(cursorRadians)) * outerRadius;

            Handles.color = _cursorColor;
            Handles.DrawSolidDisc(cursorPos, Vector3.forward, CURSOR_SIZE);

            Handles.color = Color.white;
            Handles.DrawWireDisc(cursorPos, Vector3.forward, CURSOR_SIZE);
        }

        private void RenderCenterText(Vector2 center)
        {
            string percentageText = (_currentValue * 100f).ToString("F0") + "%";

            GUIStyle percentageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            Vector2 textSize = percentageStyle.CalcSize(new GUIContent(percentageText));
            Rect textRect = new Rect(
                center.x - textSize.x / 2,
                center.y - textSize.y / 2 - 3f,
                textSize.x,
                textSize.y
            );

            GUI.Label(textRect, percentageText, percentageStyle);
        }

        private bool HandleMouseInteraction(Vector2 center, float outerRadius)
        {
            Event currentEvent = Event.current;
            Vector2 mousePosition = currentEvent.mousePosition;

            // Solo manejar mouse down y drag
            if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
                return false;

            if (currentEvent.button != 0) // Solo botón izquierdo
                return false;

            // Verificar si el mouse está dentro del área del deslizador
            float distanceFromCenter = Vector2.Distance(mousePosition, center);
            if (distanceFromCenter > outerRadius)
                return false;

            // Calcular ángulo del mouse
            Vector2 direction = mousePosition - center;
            float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Ajustar para que 0% esté arriba (12 en punto) y crezca en sentido horario
            mouseAngle += 90f;
            if (mouseAngle < 0) mouseAngle += 360f;
            if (mouseAngle >= 360f) mouseAngle -= 360f;

            // Actualizar valores
            float newAngle = mouseAngle;
            float newValue = newAngle / 360f;

            // Verificar si hay cambios significativos
            if (Mathf.Abs(newValue - _currentValue) > 0.001f)
            {
                _currentAngle = newAngle;
                _currentValue = newValue;

                currentEvent.Use(); // Consumir el evento
                return true;
            }

            return false;
        }

        /// <summary>
        /// Actualiza el valor del slider basado en el estado actual del MRIluminacionRadial
        /// CORREGIDO v0.002: No derivar de AsUnlit porque no es lineal
        /// El valor del slider es la fuente de verdad, no el componente
        /// </summary>
        public void UpdateValueFromIllumination()
        {
            // No hacer nada - el valor interno del slider es la fuente de verdad
            // AsUnlit no es lineal (es 0 para frames 0-127, luego sube a 1)
            // por lo que no se puede usar para determinar la posición del slider

            // Solo asegurar que los materiales estén cacheados
            if (_targetIllumination != null && _cachedMaterials.Count == 0)
            {
                CacheMaterialsAndStoreOriginals();
            }
        }

        /// <summary>
        /// Restaura las propiedades originales de los materiales
        /// Útil para el botón de Reset
        /// </summary>
        public void RestoreOriginalMaterialProperties()
        {
            foreach (var kvp in _originalMaterialProperties)
            {
                var material = kvp.Key;
                var originals = kvp.Value;

                if (material == null || originals == null || originals.Length < 4) continue;

                material.SetFloat("_AsUnlit", originals[0]);
                material.SetFloat("_LightMaxLimit", originals[1]);
                material.SetFloat("_ShadowBorder", originals[2]);
                material.SetFloat("_ShadowStrength", originals[3]);
            }

            // Resetear slider al 50%
            _currentValue = 0.5f;
            _currentAngle = 180f;
        }

        /// <summary>
        /// Establece el valor del slider directamente (0-1)
        /// </summary>
        public void SetValue(float normalizedValue)
        {
            _currentValue = Mathf.Clamp01(normalizedValue);
            _currentAngle = _currentValue * 360f;
            ApplyValueToIllumination();
        }

        /// <summary>
        /// Valor actual del deslizador (0-1)
        /// </summary>
        public float CurrentValue => _currentValue;

        /// <summary>
        /// Ángulo actual del cursor (0-360)
        /// </summary>
        public float CurrentAngle => _currentAngle;

        /// <summary>
        /// MRIluminacionRadial asociado
        /// </summary>
        public MRIluminacionRadial TargetIllumination => _targetIllumination;
    }
#endif
}
