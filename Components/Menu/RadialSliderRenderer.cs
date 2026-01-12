using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.Radial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
#if UNITY_EDITOR
    /// <summary>
    /// Renderizador de deslizador radial integrado para slots lineales en el menú circular
    /// Basado en sliderradial.cs pero adaptado para funcionar dentro del espacio de un slot
    /// VERSIÓN 0.047: Integración directa en SimpleRadialMenuDrawer
    /// </summary>
    public class RadialSliderRenderer
    {
        
        private MRUnificarObjetos _targetRadialMenu;
        private float _currentValue = 0f; // Valor 0-1 que representa el progreso
        private float _currentAngle = 0f; // Ángulo actual del cursor (0-360)
        
        // Configuración visual
        private readonly Color _backgroundColor = new Color(0.15f, 0.25f, 0.25f, 0.8f);
        private readonly Color _activeColor = new Color(0f, 0.8f, 0.8f, 0.9f);
        private readonly Color _innerCircleColor = new Color(0.25f, 0.4f, 0.45f, 1f);
        private readonly Color _borderColor = new Color(0f, 0.6f, 0.6f, 0.6f);
        private readonly Color _cursorColor = new Color(0f, 1f, 1f, 1f);
        
        // Configuración de tamaño (adaptada para slot del menú - OPTIMIZADA para menú 300x300)
        private const float OUTER_RADIUS_RATIO = 0.85f; // Reducido de 0.9f a 0.85f
        private const float INNER_RADIUS_RATIO = 0.4f; // Ajustado de 0.35f a 0.4f para mejor proporción
        private const float CURSOR_SIZE = 5f; // Reducido de 6f a 5f
        
        
        
        public RadialSliderRenderer(MRUnificarObjetos targetRadialMenu)
        {
            _targetRadialMenu = targetRadialMenu;
            
            // Inicializar valor basado en el frame activo actual
            UpdateValueFromRadialMenu();
        }
        
        
        
        /// <summary>
        /// Renderiza el deslizador radial en el área especificada
        /// </summary>
        /// <param name="center">Centro del deslizador</param>
        /// <param name="availableRadius">Radio disponible para el deslizador</param>
        /// <returns>True si hubo cambios que requieren actualización</returns>
        public bool RenderSlider(Vector2 center, float availableRadius)
        {
            if (_targetRadialMenu == null)
                return false;
                
            bool hasChanges = false;
            
            // Calcular radios basados en el espacio disponible
            float outerRadius = availableRadius * OUTER_RADIUS_RATIO;
            float innerRadius = outerRadius * INNER_RADIUS_RATIO;
            
            // Manejar interacción del mouse primero
            if (HandleMouseInteraction(center, outerRadius))
            {
                hasChanges = true;
                UpdateRadialMenuFromValue();
            }
            
            // Renderizar componentes visuales EN EL ORDEN CORRECTO
            RenderBackground(center, outerRadius, innerRadius);
            RenderProgressArc(center, outerRadius, innerRadius);
            // MOVIDO: El círculo central debe ir DESPUÉS del progreso para estar encima
            RenderCenterCircle(center, innerRadius); // Nuevo método separado
            RenderCursor(center, outerRadius);
            RenderCenterText(center, innerRadius);
            
            return hasChanges;
        }
        
        /// <summary>
        /// Actualiza el valor interno basado en el estado actual del MRUnificarObjetos
        /// </summary>
        public void UpdateValueFromRadialMenu()
        {
            if (_targetRadialMenu == null || _targetRadialMenu.FrameCount <= 1)
                return;
                
            // Convertir frame index a valor 0-1
            _currentValue = (float)_targetRadialMenu.ActiveFrameIndex / (_targetRadialMenu.FrameCount - 1);
            
            // Convertir valor a ángulo (0 = arriba, crece en sentido horario)
            _currentAngle = _currentValue * 360f;
        }
        
        /// <summary>
        /// Cancela todos los previews activos en el MRUnificarObjetos
        /// SIMPLIFICADO: Solo para limpieza al cambiar componentes
        /// </summary>
        public void CancelAllPreviews()
        {
            if (_targetRadialMenu == null)
                return;
                
            // Solo cancelar previews sin restauración manual
            foreach (var frame in _targetRadialMenu.FrameObjects)
            {
                if (frame != null && frame.IsPreviewActive)
                {
                    frame.CancelPreview();
                }
            }
        }
        
        
        
        private void RenderBackground(Vector2 center, float outerRadius, float innerRadius)
        {
            // Solo círculo exterior (fondo del slider)
            Handles.color = _backgroundColor;
            Handles.DrawSolidDisc(center, Vector3.forward, outerRadius);
        }
        
        /// <summary>
        /// Renderiza el círculo central que debe ir ENCIMA del progreso
        /// </summary>
        /// <param name="center">Centro del deslizador</param>
        /// <param name="innerRadius">Radio del círculo interior</param>
        private void RenderCenterCircle(Vector2 center, float innerRadius)
        {
            // Círculo interior (debe ir encima del progreso)
            Handles.color = _innerCircleColor;
            Handles.DrawSolidDisc(center, Vector3.forward, innerRadius);
            
            // Solo borde del círculo interior
            Handles.color = _borderColor;
            Handles.DrawWireDisc(center, Vector3.forward, innerRadius);
        }
        
        private void RenderProgressArc(Vector2 center, float outerRadius, float innerRadius)
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
            
            // Dibujar sector de progreso
            Handles.color = _activeColor;
            Handles.DrawAAConvexPolygon(sectorPoints);
        }
        
        private void RenderCursor(Vector2 center, float outerRadius)
        {
            // Calcular posición del cursor
            float cursorRadians = (_currentAngle - 90f) * Mathf.Deg2Rad;
            Vector2 cursorPos = center + new Vector2(Mathf.Cos(cursorRadians), Mathf.Sin(cursorRadians)) * outerRadius;
            
            // Dibujar cursor
            Handles.color = _cursorColor;
            Handles.DrawSolidDisc(cursorPos, Vector3.forward, CURSOR_SIZE);
            
            // Borde del cursor
            Handles.color = Color.white;
            Handles.DrawWireDisc(cursorPos, Vector3.forward, CURSOR_SIZE);
        }
        
        private void RenderCenterText(Vector2 center, float innerRadius)
        {
            // Texto de porcentaje
            string percentageText = (_currentValue * 100f).ToString("F0") + "%";
            
            GUIStyle percentageStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12, // Reducido de 14 a 12 para menú más pequeño
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            
            Vector2 textSize = percentageStyle.CalcSize(new GUIContent(percentageText));
            Rect textRect = new Rect(
                center.x - textSize.x / 2,
                center.y - textSize.y / 2 - 3f, // Ligeramente hacia arriba
                textSize.x,
                textSize.y
            );
            
            GUI.Label(textRect, percentageText, percentageStyle);
            
            // El usuario no necesita ver "F2" - solo el porcentaje es suficiente
        }
        
        
        
        /// <summary>
        /// Maneja la interacción del mouse con el deslizador radial
        /// </summary>
        /// <param name="center">Centro del deslizador</param>
        /// <param name="outerRadius">Radio exterior para detección de área</param>
        /// <returns>True si hubo cambios</returns>
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
        /// Actualiza el MRUnificarObjetos basado en el valor actual del deslizador
        /// SIMPLIFICADO: Usa exactamente la misma lógica que el MRUnificarObjetos original
        /// </summary>
        private void UpdateRadialMenuFromValue()
        {
            if (_targetRadialMenu == null || _targetRadialMenu.FrameCount <= 1)
                return;
                
            // Convertir valor 0-1 a frame index
            int newFrameIndex = Mathf.RoundToInt(_currentValue * (_targetRadialMenu.FrameCount - 1));
            newFrameIndex = Mathf.Clamp(newFrameIndex, 0, _targetRadialMenu.FrameCount - 1);
            
            // Solo actualizar si el frame cambió
            if (newFrameIndex != _targetRadialMenu.ActiveFrameIndex)
            {
                // SIMPLIFICADO: Usar exactamente la misma lógica que MRUnificarObjetos
                _targetRadialMenu.ActiveFrameIndex = newFrameIndex;
                
                // Llamar al mismo método que usa el componente original
                _targetRadialMenu.ApplyCurrentFrame();
                
            }
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
        /// MRUnificarObjetos asociado
        /// </summary>
        public MRUnificarObjetos TargetRadialMenu => _targetRadialMenu;
        
    }
#endif
}
