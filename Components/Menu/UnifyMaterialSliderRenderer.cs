using UnityEngine;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Components.Menu
{
#if UNITY_EDITOR
    using UnityEditor;

    /// <summary>
    /// Renderizador de deslizador radial para MRUnificarMateriales.
    /// Permite controlar los materiales con un slider circular.
    /// </summary>
    public class UnifyMaterialSliderRenderer
    {
        private MRUnificarMateriales _targetUnifyMaterial;
        private float _currentValue = 0f; // Valor 0-1 que representa el progreso (default 0%)
        private float _currentAngle = 0f; // Angulo actual del cursor (0-360)

        // Cache de materiales originales para restauracion
        private Dictionary<MRMaterialSlot, Material> _originalMaterials = new Dictionary<MRMaterialSlot, Material>();
        private List<UnifySlotAnimationData> _animationData;
        private bool _hasStoredOriginals = false;

        // Configuracion visual (MISMO color que otros sliders para consistencia)
        private readonly Color _backgroundColor = new Color(0.15f, 0.25f, 0.25f, 0.8f);
        private readonly Color _activeColor = new Color(0f, 0.8f, 0.8f, 0.9f);
        private readonly Color _innerCircleColor = new Color(0.25f, 0.4f, 0.45f, 1f);
        private readonly Color _borderColor = new Color(0f, 0.6f, 0.6f, 0.6f);
        private readonly Color _cursorColor = new Color(0f, 1f, 1f, 1f);

        // Configuracion de tamano
        private const float OUTER_RADIUS_RATIO = 0.85f;
        private const float INNER_RADIUS_RATIO = 0.4f;
        private const float CURSOR_SIZE = 5f;

        public UnifyMaterialSliderRenderer(MRUnificarMateriales targetUnifyMaterial)
        {
            _targetUnifyMaterial = targetUnifyMaterial;

            // Inicializar valor al 0% por defecto (frame 0)
            _currentValue = 0f;
            _currentAngle = 0f;

            // Cachear materiales y guardar originales
            if (_targetUnifyMaterial != null)
            {
                CacheAnimationDataAndStoreOriginals();
            }
        }

        /// <summary>
        /// Cachea los datos de animacion y guarda los materiales originales
        /// </summary>
        private void CacheAnimationDataAndStoreOriginals()
        {
            if (_targetUnifyMaterial == null) return;

            _animationData = _targetUnifyMaterial.CollectAnimationData();
            _originalMaterials.Clear();

            foreach (var data in _animationData)
            {
                if (data.Slot == null || !data.Slot.IsValid) continue;

                // Guardar material original
                if (!_hasStoredOriginals)
                {
                    var renderer = data.Slot.TargetRenderer;
                    if (renderer != null && data.Slot.MaterialIndex < renderer.sharedMaterials.Length)
                    {
                        _originalMaterials[data.Slot] = renderer.sharedMaterials[data.Slot.MaterialIndex];
                    }
                }
            }

            _hasStoredOriginals = _originalMaterials.Count > 0;
        }

        /// <summary>
        /// Renderiza el deslizador radial en el area especificada
        /// </summary>
        /// <param name="center">Centro del deslizador</param>
        /// <param name="availableRadius">Radio disponible para el deslizador</param>
        /// <returns>True si hubo cambios que requieren actualizacion</returns>
        public bool RenderSlider(Vector2 center, float availableRadius)
        {
            if (_targetUnifyMaterial == null)
                return false;

            bool hasChanges = false;

            // Calcular radios basados en el espacio disponible
            float outerRadius = availableRadius * OUTER_RADIUS_RATIO;
            float innerRadius = outerRadius * INNER_RADIUS_RATIO;

            // Manejar interaccion del mouse primero
            if (HandleMouseInteraction(center, outerRadius))
            {
                hasChanges = true;
                ApplyValueToMaterials();
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
        /// Aplica el valor actual a los materiales de los slots
        /// Frame 0-255 distribuido segun la configuracion de cada grupo
        /// </summary>
        private void ApplyValueToMaterials()
        {
            if (_targetUnifyMaterial == null)
                return;

            // Asegurar que tenemos datos cacheados
            if (_animationData == null || _animationData.Count == 0)
            {
                CacheAnimationDataAndStoreOriginals();
            }

            if (_animationData == null || _animationData.Count == 0)
                return;

            // Convertir valor 0-1 a frame 0-255
            int frame = Mathf.RoundToInt(_currentValue * 255f);

            // Aplicar material correspondiente a cada slot
            foreach (var data in _animationData)
            {
                if (data.Slot == null || !data.Slot.IsValid) continue;
                if (data.Materials == null || data.Materials.Count == 0) continue;
                if (data.FrameDistribution == null || data.FrameDistribution.Count == 0) continue;

                // Encontrar que material corresponde a este frame
                Material targetMaterial = GetMaterialForFrame(data, frame);
                if (targetMaterial == null) continue;

                // Aplicar material al renderer
                var renderer = data.Slot.TargetRenderer;
                if (renderer != null && data.Slot.MaterialIndex < renderer.sharedMaterials.Length)
                {
                    var materials = renderer.sharedMaterials;
                    materials[data.Slot.MaterialIndex] = targetMaterial;
                    renderer.sharedMaterials = materials;
                }
            }
        }

        /// <summary>
        /// Obtiene el material que corresponde a un frame especifico
        /// </summary>
        private Material GetMaterialForFrame(UnifySlotAnimationData data, int frame)
        {
            foreach (var range in data.FrameDistribution)
            {
                if (frame >= range.StartFrame && frame <= range.EndFrame)
                {
                    if (range.MaterialIndex >= 0 && range.MaterialIndex < data.Materials.Count)
                    {
                        return data.Materials[range.MaterialIndex];
                    }
                }
            }

            // Si no se encuentra, retornar el ultimo material
            if (data.Materials.Count > 0)
            {
                return data.Materials[data.Materials.Count - 1];
            }

            return null;
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

            // Crear sector de progreso como poligono suave
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
            // Mostrar porcentaje
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

            if (currentEvent.button != 0) // Solo boton izquierdo
                return false;

            // Verificar si el mouse esta dentro del area del deslizador
            float distanceFromCenter = Vector2.Distance(mousePosition, center);
            if (distanceFromCenter > outerRadius)
                return false;

            // Calcular angulo del mouse
            Vector2 direction = mousePosition - center;
            float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Ajustar para que 0% este arriba (12 en punto) y crezca en sentido horario
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
        /// Actualiza el valor del slider basado en el estado actual del componente
        /// </summary>
        public void UpdateValueFromUnifyMaterial()
        {
            // Solo asegurar que los datos esten cacheados
            if (_targetUnifyMaterial != null && (_animationData == null || _animationData.Count == 0))
            {
                CacheAnimationDataAndStoreOriginals();
            }
        }

        /// <summary>
        /// Restaura los materiales originales
        /// </summary>
        public void RestoreOriginalMaterials()
        {
            foreach (var kvp in _originalMaterials)
            {
                var slot = kvp.Key;
                var originalMaterial = kvp.Value;

                if (slot == null || !slot.IsValid || originalMaterial == null) continue;

                var renderer = slot.TargetRenderer;
                if (renderer != null && slot.MaterialIndex < renderer.sharedMaterials.Length)
                {
                    var materials = renderer.sharedMaterials;
                    materials[slot.MaterialIndex] = originalMaterial;
                    renderer.sharedMaterials = materials;
                }
            }

            // Resetear slider al 0%
            _currentValue = 0f;
            _currentAngle = 0f;
        }

        /// <summary>
        /// Establece el valor del slider directamente (0-1)
        /// </summary>
        public void SetValue(float normalizedValue)
        {
            _currentValue = Mathf.Clamp01(normalizedValue);
            _currentAngle = _currentValue * 360f;
            ApplyValueToMaterials();
        }

        /// <summary>
        /// Valor actual del deslizador (0-1)
        /// </summary>
        public float CurrentValue => _currentValue;

        /// <summary>
        /// Angulo actual del cursor (0-360)
        /// </summary>
        public float CurrentAngle => _currentAngle;

        /// <summary>
        /// MRUnificarMateriales asociado
        /// </summary>
        public MRUnificarMateriales TargetUnifyMaterial => _targetUnifyMaterial;
    }
#endif
}
