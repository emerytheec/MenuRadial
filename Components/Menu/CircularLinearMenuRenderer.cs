using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Menu
{
#if UNITY_EDITOR
    /// <summary>
    /// Renderizador de interfaz circular para animaciones lineales
    /// Reutiliza la lógica de MRUnificarObjetos pero con visualización circular
    /// VERSIÓN 0.046: Integración completa con MR Control Menu
    /// </summary>
    public class CircularLinearMenuRenderer
    {
        
        private readonly MRUnificarObjetos _targetRadialMenu;
        private readonly float _centerX;
        private readonly float _centerY;
        private readonly float _radius;
        
        // Constantes de diseño
        private const float DEFAULT_RADIUS = 80f;
        private const float CENTER_CIRCLE_RADIUS = 25f;
        private const float SEGMENT_THICKNESS = 15f;
        
        
        
        public CircularLinearMenuRenderer(MRUnificarObjetos targetRadialMenu, Vector2 center, float radius = DEFAULT_RADIUS)
        {
            _targetRadialMenu = targetRadialMenu;
            _centerX = center.x;
            _centerY = center.y;
            _radius = radius;
        }
        
        
        
        /// <summary>
        /// Renderiza la interfaz circular y maneja la interacción
        /// </summary>
        /// <param name="controlRect">Área de control asignada</param>
        /// <returns>True si hubo cambios que requieren repaint</returns>
        public bool RenderCircularInterface(Rect controlRect)
        {
            if (_targetRadialMenu == null || _targetRadialMenu.FrameCount < 3)
                return false;
                
            bool hasChanges = false;
            
            // Calcular centro basado en el rect
            Vector2 center = new Vector2(controlRect.x + controlRect.width * 0.5f, controlRect.y + controlRect.height * 0.5f);
            
            // Dibujar círculo principal
            DrawMainCircle(center);
            
            // Dibujar segmentos de frames
            DrawFrameSegments(center);
            
            // Dibujar círculo central con progreso
            DrawCenterProgress(center);
            
            // Manejar interacción del mouse
            if (HandleMouseInteraction(center, controlRect))
            {
                hasChanges = true;
            }
            
            // Dibujar deslizador debajo del círculo
            hasChanges |= DrawSliderControl(controlRect);
            
            return hasChanges;
        }
        
        
        
        private void DrawMainCircle(Vector2 center)
        {
            // Círculo exterior
            Handles.color = new Color(0.2f, 0.8f, 0.8f, 0.3f); // Color cian translúcido
            Handles.DrawWireDisc(center, Vector3.forward, _radius);
            
            // Círculo interior
            Handles.color = new Color(0.2f, 0.8f, 0.8f, 0.1f);
            Handles.DrawWireDisc(center, Vector3.forward, _radius - SEGMENT_THICKNESS);
        }
        
        private void DrawFrameSegments(Vector2 center)
        {
            int frameCount = _targetRadialMenu.FrameCount;
            float anglePerSegment = 360f / frameCount;
            float startAngle = -90f; // Comenzar desde arriba (12 en punto)
            
            for (int i = 0; i < frameCount; i++)
            {
                float currentAngle = startAngle + (i * anglePerSegment);
                float nextAngle = startAngle + ((i + 1) * anglePerSegment);
                
                // Determinar si este segmento está activo
                bool isActiveSegment = i == _targetRadialMenu.ActiveFrameIndex;
                
                // Color del segmento
                Color segmentColor = isActiveSegment ? 
                    new Color(0.2f, 1f, 0.8f, 0.8f) : // Activo: cian brillante
                    new Color(0.3f, 0.3f, 0.3f, 0.5f); // Inactivo: gris
                
                // Dibujar arco del segmento
                DrawSegmentArc(center, currentAngle, nextAngle, segmentColor);
                
                // Dibujar líneas divisorias
                if (i < frameCount - 1) // No dibujar línea después del último
                {
                    DrawSegmentDivider(center, nextAngle);
                }
                
                // Dibujar ícono o número del frame si hay espacio
                DrawSegmentLabel(center, currentAngle, anglePerSegment, i);
            }
        }
        
        private void DrawSegmentArc(Vector2 center, float startAngle, float endAngle, Color color)
        {
            Handles.color = color;
            
            // Convertir ángulos a radianes
            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;
            
            // Calcular puntos del arco
            int segments = 10; // Más segmentos = arco más suave
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startRad, endRad, t);
                
                float x = center.x + Mathf.Cos(angle) * (_radius - SEGMENT_THICKNESS * 0.5f);
                float y = center.y + Mathf.Sin(angle) * (_radius - SEGMENT_THICKNESS * 0.5f);
                
                points[i] = new Vector3(x, y, 0);
            }
            
            // Dibujar línea del arco
            Handles.DrawAAPolyLine(3f, points);
        }
        
        private void DrawSegmentDivider(Vector2 center, float angle)
        {
            Handles.color = new Color(0.2f, 0.8f, 0.8f, 0.6f);
            
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 innerPoint = new Vector3(
                center.x + Mathf.Cos(rad) * (_radius - SEGMENT_THICKNESS),
                center.y + Mathf.Sin(rad) * (_radius - SEGMENT_THICKNESS),
                0
            );
            
            Vector3 outerPoint = new Vector3(
                center.x + Mathf.Cos(rad) * _radius,
                center.y + Mathf.Sin(rad) * _radius,
                0
            );
            
            Handles.DrawLine(innerPoint, outerPoint);
        }
        
        private void DrawSegmentLabel(Vector2 center, float startAngle, float segmentAngle, int frameIndex)
        {
            // Solo dibujar etiquetas si hay espacio suficiente
            if (segmentAngle < 30f) return;
            
            float midAngle = startAngle + (segmentAngle * 0.5f);
            float rad = midAngle * Mathf.Deg2Rad;
            
            float labelRadius = _radius - (SEGMENT_THICKNESS * 0.5f);
            Vector2 labelPos = new Vector2(
                center.x + Mathf.Cos(rad) * labelRadius,
                center.y + Mathf.Sin(rad) * labelRadius
            );
            
            // Obtener nombre del frame
            string frameLabel = (frameIndex + 1).ToString();
            if (_targetRadialMenu.FrameObjects.Count > frameIndex && 
                _targetRadialMenu.FrameObjects[frameIndex] != null)
            {
                string frameName = _targetRadialMenu.FrameObjects[frameIndex].FrameName;
                if (!string.IsNullOrEmpty(frameName) && frameName.Length <= 3)
                {
                    frameLabel = frameName;
                }
            }
            
            // Estilo de texto
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            // Dibujar etiqueta centrada
            Vector2 labelSize = labelStyle.CalcSize(new GUIContent(frameLabel));
            Rect labelRect = new Rect(
                labelPos.x - labelSize.x * 0.5f,
                labelPos.y - labelSize.y * 0.5f,
                labelSize.x,
                labelSize.y
            );
            
            GUI.Label(labelRect, frameLabel, labelStyle);
        }
        
        private void DrawCenterProgress(Vector2 center)
        {
            // Círculo central de fondo
            Handles.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            Handles.DrawSolidDisc(center, Vector3.forward, CENTER_CIRCLE_RADIUS);
            
            // Borde del círculo central
            Handles.color = new Color(0.2f, 0.8f, 0.8f, 1f);
            Handles.DrawWireDisc(center, Vector3.forward, CENTER_CIRCLE_RADIUS);
            
            // Calcular porcentaje de progreso
            float progress = 0f;
            if (_targetRadialMenu.FrameCount > 0)
            {
                progress = (float)_targetRadialMenu.ActiveFrameIndex / (_targetRadialMenu.FrameCount - 1);
            }
            
            // Texto de progreso
            string progressText = $"{progress * 100:F0}%";
            
            GUIStyle centerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 14
            };
            
            Vector2 textSize = centerStyle.CalcSize(new GUIContent(progressText));
            Rect textRect = new Rect(
                center.x - textSize.x * 0.5f,
                center.y - textSize.y * 0.5f,
                textSize.x,
                textSize.y
            );
            
            GUI.Label(textRect, progressText, centerStyle);
            
            // Nombre del frame actual (debajo del porcentaje)
            if (_targetRadialMenu.ActiveFrame != null)
            {
                string frameName = _targetRadialMenu.ActiveFrame.FrameName;
                if (!string.IsNullOrEmpty(frameName))
                {
                    GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
                    };
                    
                    Vector2 nameSize = nameStyle.CalcSize(new GUIContent(frameName));
                    Rect nameRect = new Rect(
                        center.x - nameSize.x * 0.5f,
                        center.y + textSize.y * 0.3f,
                        nameSize.x,
                        nameSize.y
                    );
                    
                    GUI.Label(nameRect, frameName, nameStyle);
                }
            }
        }
        
        
        
        private bool HandleMouseInteraction(Vector2 center, Rect controlRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
                return false;
                
            Vector2 mousePos = currentEvent.mousePosition;
            
            // Verificar si el clic está dentro del área del círculo
            float distanceFromCenter = Vector2.Distance(mousePos, center);
            if (distanceFromCenter < (_radius - SEGMENT_THICKNESS) || distanceFromCenter > _radius)
                return false;
                
            // Calcular ángulo del clic
            Vector2 direction = (mousePos - center).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Ajustar ángulo para que 0° esté arriba (12 en punto)
            angle += 90f;
            if (angle < 0) angle += 360f;
            
            // Calcular segmento clickeado
            float segmentAngle = 360f / _targetRadialMenu.FrameCount;
            int clickedSegment = Mathf.FloorToInt(angle / segmentAngle);
            
            // Asegurar que el segmento esté en rango válido
            clickedSegment = Mathf.Clamp(clickedSegment, 0, _targetRadialMenu.FrameCount - 1);
            
            // Cambiar al frame clickeado si es diferente
            if (clickedSegment != _targetRadialMenu.ActiveFrameIndex)
            {
                _targetRadialMenu.ActiveFrameIndex = clickedSegment;
                
                // Aplicar preview del frame usando la lógica existente
                _targetRadialMenu.ApplyCurrentFrame();
                
                currentEvent.Use(); // Consumir el evento
                return true;
            }
            
            return false;
        }
        
        private bool DrawSliderControl(Rect controlRect)
        {
            // Área para el slider debajo del círculo
            Rect sliderRect = new Rect(
                controlRect.x + 10f,
                controlRect.y + controlRect.height - 40f,
                controlRect.width - 20f,
                20f
            );
            
            // Guardar valor actual
            int oldValue = _targetRadialMenu.ActiveFrameIndex;
            
            // Dibujar slider
            int newValue = EditorGUI.IntSlider(sliderRect, "Frame", oldValue, 0, _targetRadialMenu.FrameCount - 1);
            
            // Detectar cambios
            if (newValue != oldValue)
            {
                _targetRadialMenu.ActiveFrameIndex = newValue;
                
                // Aplicar preview del frame usando la lógica existente
                _targetRadialMenu.ApplyCurrentFrame();
                
                return true;
            }
            
            return false;
        }
        
    }
#endif
}
