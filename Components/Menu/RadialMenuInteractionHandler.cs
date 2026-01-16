#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Manejador de interacción del usuario para el menú radial
    /// Responsabilidad única: Manejo de eventos de mouse, clics, hover y callbacks
    /// </summary>
    public static class RadialMenuInteractionHandler
    {
        /// <summary>
        /// Información de un botón clickeable
        /// </summary>
        public struct ClickableButton
        {
            /// <summary>
            /// Área clickeable del botón
            /// </summary>
            public Rect ClickArea;
            
            /// <summary>
            /// Índice del botón para callback (-1 para Back, 0+ para slots)
            /// </summary>
            public int ButtonIndex;
            
            /// <summary>
            /// Nombre del botón
            /// </summary>
            public string ButtonName;
            
            /// <summary>
            /// Si el botón debe mostrar cursor de link
            /// </summary>
            public bool ShowLinkCursor;
        }
        
        /// <summary>
        /// Resultado de la detección de hover
        /// </summary>
        public struct HoverResult
        {
            /// <summary>
            /// Si hay hover activo
            /// </summary>
            public bool IsHovering;
            
            /// <summary>
            /// Índice del botón en hover
            /// </summary>
            public int HoveredButtonIndex;
            
            /// <summary>
            /// Área del botón en hover
            /// </summary>
            public Rect HoveredArea;
        }
        
        /// <summary>
        /// Maneja la interacción completa de un botón clickeable con feedback visual
        /// </summary>
        /// <param name="center">Centro del botón</param>
        /// <param name="size">Tamaño del área clickeable</param>
        /// <param name="backgroundColor">Color de fondo del botón</param>
        /// <param name="buttonIndex">Índice del botón para callback</param>
        /// <param name="buttonName">Nombre del botón</param>
        /// <param name="onButtonClick">Callback de clic</param>
        /// <returns>True si está en hover</returns>
        public static bool HandleClickableButton(Vector2 center, float size, Color backgroundColor, 
                                               int buttonIndex, string buttonName, System.Action<int> onButtonClick)
        {
            Rect buttonRect = RadialGeometryCalculator.CalculateCenteredRect(center, size);
            
            // Detectar hover para feedback visual
            bool isHovering = IsMouseOver(buttonRect);
            
            // Dibujar fondo del botón con feedback visual
            RadialMenuRenderer.DrawButtonBackground(center, size, backgroundColor, isHovering);
            
            // Manejar clics
            if (HandleButtonClick(buttonRect, buttonIndex, onButtonClick))
            {
            }
            
            // Cambiar cursor en hover
            if (isHovering && onButtonClick != null)
            {
                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
            }
            
            return isHovering;
        }
        
        /// <summary>
        /// Detecta si el mouse está sobre un área específica
        /// </summary>
        /// <param name="area">Área a verificar</param>
        /// <returns>True si el mouse está sobre el área</returns>
        public static bool IsMouseOver(Rect area)
        {
            return area.Contains(Event.current.mousePosition);
        }
        
        /// <summary>
        /// Maneja el clic en un botón específico
        /// MODIFICADO: Usa MouseUp para mayor confiabilidad en Unity IMGUI
        /// </summary>
        /// <param name="buttonRect">Área del botón</param>
        /// <param name="buttonIndex">Índice del botón</param>
        /// <param name="onButtonClick">Callback de clic</param>
        /// <returns>True si se procesó un clic</returns>
        public static bool HandleButtonClick(Rect buttonRect, int buttonIndex, System.Action<int> onButtonClick)
        {
            Event currentEvent = Event.current;

            // Usar MouseUp para mayor confiabilidad (evita problemas con múltiples pases de IMGUI)
            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 0 &&
                buttonRect.Contains(currentEvent.mousePosition))
            {
                onButtonClick?.Invoke(buttonIndex);
                currentEvent.Use();
                GUI.changed = true;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Detecta hover sobre múltiples botones y devuelve información detallada
        /// </summary>
        /// <param name="buttons">Array de botones clickeables</param>
        /// <returns>Resultado de la detección de hover</returns>
        public static HoverResult DetectHover(ClickableButton[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
                return new HoverResult { IsHovering = false };
                
            Vector2 mousePos = Event.current.mousePosition;
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].ClickArea.Contains(mousePos))
                {
                    return new HoverResult
                    {
                        IsHovering = true,
                        HoveredButtonIndex = buttons[i].ButtonIndex,
                        HoveredArea = buttons[i].ClickArea
                    };
                }
            }
            
            return new HoverResult { IsHovering = false };
        }
        
        /// <summary>
        /// Aplica cursores apropriados a todos los botones clickeables
        /// </summary>
        /// <param name="buttons">Array de botones clickeables</param>
        /// <param name="hasCallback">Si hay callback disponible</param>
        public static void ApplyCursors(ClickableButton[] buttons, bool hasCallback)
        {
            if (!hasCallback || buttons == null)
                return;
                
            foreach (var button in buttons)
            {
                if (button.ShowLinkCursor)
                {
                    EditorGUIUtility.AddCursorRect(button.ClickArea, MouseCursor.Link);
                }
            }
        }
        
        /// <summary>
        /// Procesa clics en múltiples botones
        /// MODIFICADO: Usa MouseUp para mayor confiabilidad
        /// </summary>
        /// <param name="buttons">Array de botones clickeables</param>
        /// <param name="onButtonClick">Callback de clic</param>
        /// <returns>Índice del botón clickeado, o -999 si ninguno</returns>
        public static int ProcessMultipleButtonClicks(ClickableButton[] buttons, System.Action<int> onButtonClick)
        {
            if (buttons == null || buttons.Length == 0 || onButtonClick == null)
                return -999;

            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
                return -999;

            Vector2 mousePos = currentEvent.mousePosition;

            foreach (var button in buttons)
            {
                if (button.ClickArea.Contains(mousePos))
                {
                    onButtonClick.Invoke(button.ButtonIndex);
                    currentEvent.Use();
                    GUI.changed = true;
                    return button.ButtonIndex;
                }
            }

            return -999;
        }
        
        /// <summary>
        /// Maneja clics en el área del slot (fuera del deslizador pero dentro del slot)
        /// Usado específicamente para slots con deslizadores radiales
        /// MODIFICADO: Usa MouseUp para mayor confiabilidad
        /// </summary>
        /// <param name="center">Centro del slot</param>
        /// <param name="outerRadius">Radio exterior para detección</param>
        /// <param name="innerRadius">Radio interior (área del deslizador)</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="onButtonClick">Callback de clic</param>
        /// <returns>True si se procesó un clic</returns>
        public static bool HandleSlotAreaClick(Vector2 center, float outerRadius, float innerRadius,
                                             int slotIndex, System.Action<int> onButtonClick)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0 || onButtonClick == null)
                return false;

            Vector2 mousePos = currentEvent.mousePosition;
            float distance = RadialGeometryCalculator.CalculateDistance(mousePos, center);

            // Solo procesar clics en el área externa (fuera del deslizador pero dentro del slot)
            if (distance > innerRadius && distance <= outerRadius)
            {
                onButtonClick.Invoke(slotIndex);
                currentEvent.Use();
                GUI.changed = true;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Verifica si el evento actual es un clic válido
        /// MODIFICADO: Usa MouseUp para mayor confiabilidad
        /// </summary>
        /// <returns>True si es un clic con botón izquierdo</returns>
        public static bool IsValidClick()
        {
            Event currentEvent = Event.current;
            return currentEvent.type == EventType.MouseUp && currentEvent.button == 0;
        }
        
        /// <summary>
        /// Verifica si el evento actual es un evento de mouse relevante
        /// </summary>
        /// <returns>True si es MouseDown, MouseUp o MouseDrag</returns>
        public static bool IsRelevantMouseEvent()
        {
            Event currentEvent = Event.current;
            return currentEvent.type == EventType.MouseDown || 
                   currentEvent.type == EventType.MouseUp || 
                   currentEvent.type == EventType.MouseDrag;
        }
        
        /// <summary>
        /// Obtiene la posición actual del mouse
        /// </summary>
        /// <returns>Posición del mouse en coordenadas de pantalla</returns>
        public static Vector2 GetMousePosition()
        {
            return Event.current.mousePosition;
        }
        
        /// <summary>
        /// Calcula si un punto está dentro de un círculo
        /// </summary>
        /// <param name="point">Punto a verificar</param>
        /// <param name="center">Centro del círculo</param>
        /// <param name="radius">Radio del círculo</param>
        /// <returns>True si el punto está dentro del círculo</returns>
        public static bool IsPointInCircle(Vector2 point, Vector2 center, float radius)
        {
            return RadialGeometryCalculator.CalculateDistance(point, center) <= radius;
        }
        
        /// <summary>
        /// Calcula si un punto está en un anillo (entre dos radios)
        /// </summary>
        /// <param name="point">Punto a verificar</param>
        /// <param name="center">Centro del anillo</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <returns>True si el punto está en el anillo</returns>
        public static bool IsPointInRing(Vector2 point, Vector2 center, float innerRadius, float outerRadius)
        {
            float distance = RadialGeometryCalculator.CalculateDistance(point, center);
            return distance >= innerRadius && distance <= outerRadius;
        }
        
        /// <summary>
        /// Crea información de botón clickeable
        /// </summary>
        /// <param name="center">Centro del botón</param>
        /// <param name="size">Tamaño del área clickeable</param>
        /// <param name="buttonIndex">Índice del botón</param>
        /// <param name="buttonName">Nombre del botón</param>
        /// <param name="showLinkCursor">Si debe mostrar cursor de link</param>
        /// <returns>Información de botón clickeable</returns>
        public static ClickableButton CreateClickableButton(Vector2 center, float size, int buttonIndex, 
                                                           string buttonName, bool showLinkCursor = true)
        {
            return new ClickableButton
            {
                ClickArea = RadialGeometryCalculator.CalculateCenteredRect(center, size),
                ButtonIndex = buttonIndex,
                ButtonName = buttonName ?? $"Button_{buttonIndex}",
                ShowLinkCursor = showLinkCursor
            };
        }
        
        /// <summary>
        /// Consume el evento actual para evitar procesamiento posterior
        /// </summary>
        public static void ConsumeCurrentEvent()
        {
            Event.current?.Use();
        }

    }
}
#endif
