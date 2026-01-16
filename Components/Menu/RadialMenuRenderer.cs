#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Renderizador visual para el menú radial
    /// Responsabilidad única: Renderizado visual puro (círculos, sectores, fondos, bordes)
    /// </summary>
    public static class RadialMenuRenderer
    {
        // Colores del menu radial (copiados de VRC-GM)
        private static readonly Color BackgroundColor = new Color(0.14f, 0.18f, 0.2f, 0.8f);
        private static readonly Color BorderColor = new Color(0.1f, 0.35f, 0.38f, 1f);
        private static readonly Color ButtonColor = new Color(0.07f, 0.55f, 0.58f, 0.6f);
        // CORREGIDO: Usar mismo color que los demás botones para consistencia visual
        private static readonly Color BackButtonColor = new Color(0.14f, 0.18f, 0.2f, 0.8f);
        
        /// <summary>
        /// Dibuja el fondo principal del menú radial (círculo exterior e interior)
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        public static void DrawMenuBackground(Vector2 center, float outerRadius, float innerRadius)
        {
            // Círculo de fondo exterior
            DrawCircle(center.x, center.y, outerRadius, BackgroundColor);
            
            // Círculo interior
            Color innerColor = new Color(0.21f, 0.24f, 0.27f, 1f);
            DrawCircle(center.x, center.y, innerRadius, innerColor);
        }
        
        /// <summary>
        /// Dibuja las líneas divisorias radiales entre sectores del menú
        /// Las líneas van ENTRE los botones, no sobre ellos
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <param name="totalSectors">Número total de sectores</param>
        public static void DrawSectorLines(Vector2 center, float outerRadius, float innerRadius, int totalSectors)
        {
            if (totalSectors <= 1) return; // No hay líneas que dibujar con 1 o menos sectores
            
            Color oldColor = Handles.color;
            Handles.color = BorderColor;
            
            float anglePerSector = 360f / totalSectors;
            
            for (int i = 0; i < totalSectors; i++)
            {
                // Calcular ángulo de la línea divisoria ENTRE sectores
                // Los botones están en: -90°, -90° + anglePerSector, -90° + anglePerSector*2, etc.
                // Las líneas deben estar en el MEDIO entre botones: -90° + anglePerSector/2, etc.
                float angle = ((anglePerSector * i) + (anglePerSector / 2f) - 90f) * Mathf.Deg2Rad;
                
                // Punto interior (en el borde del círculo interno)
                Vector3 innerPoint = new Vector3(
                    center.x + Mathf.Cos(angle) * innerRadius,
                    center.y + Mathf.Sin(angle) * innerRadius,
                    0
                );
                
                // Punto exterior (en el borde del círculo externo)
                Vector3 outerPoint = new Vector3(
                    center.x + Mathf.Cos(angle) * outerRadius,
                    center.y + Mathf.Sin(angle) * outerRadius,
                    0
                );
                
                // Dibujar línea divisoria
                Handles.DrawLine(innerPoint, outerPoint);
            }
            
            Handles.color = oldColor;
        }
        
        /// <summary>
        /// Dibuja los bordes del menú (círculo exterior e interior)
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        public static void DrawMenuBorders(Vector2 center, float outerRadius, float innerRadius)
        {
            DrawCircleBorder(center.x, center.y, outerRadius);
            DrawCircleBorder(center.x, center.y, innerRadius);
        }
        
        /// <summary>
        /// Dibuja el texto central del menú
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="text">Texto a mostrar</param>
        /// <param name="fontSize">Tamaño de fuente</param>
        public static void DrawCentralText(Vector2 center, string text, float fontSize = 14f)
        {
            GUIStyle centralTextStyle = new GUIStyle(GUI.skin.label) 
            { 
                alignment = TextAnchor.MiddleCenter, 
                fontSize = Mathf.RoundToInt(fontSize),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white } 
            };
            
            Vector2 centralTextSize = centralTextStyle.CalcSize(new GUIContent(text));
            Rect textRect = new Rect(
                center.x - centralTextSize.x/2, 
                center.y - centralTextSize.y/2, 
                centralTextSize.x, 
                centralTextSize.y
            );
            
            GUI.Label(textRect, text, centralTextStyle);
        }
        
        /// <summary>
        /// Dibuja el fondo de un botón/sector individual
        /// </summary>
        /// <param name="center">Centro del botón</param>
        /// <param name="size">Tamaño del sector</param>
        /// <param name="color">Color del fondo</param>
        /// <param name="isHovering">Si está en hover (aplica feedback visual)</param>
        public static void DrawButtonBackground(Vector2 center, float size, Color color, bool isHovering = false)
        {
            Rect buttonRect = RadialGeometryCalculator.CalculateCenteredRect(center, size);
            
            // Aplicar feedback visual en hover
            Color buttonColor = isHovering ? new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, color.a) : color;
            
            Color oldColor = GUI.color;
            GUI.color = buttonColor;
            GUI.DrawTexture(buttonRect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }
        
        /// <summary>
        /// Dibuja texto en una posición específica con estilo personalizable
        /// </summary>
        /// <param name="center">Centro del texto</param>
        /// <param name="text">Texto a mostrar</param>
        /// <param name="fontSize">Tamaño de fuente</param>
        /// <param name="textColor">Color del texto</param>
        /// <param name="fontStyle">Estilo de fuente</param>
        /// <param name="offset">Offset adicional de posición</param>
        public static void DrawText(Vector2 center, string text, int fontSize = 10, Color? textColor = null, FontStyle fontStyle = FontStyle.Normal, Vector2 offset = default)
        {
            if (string.IsNullOrEmpty(text))
                return;
                
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = fontStyle,
                normal = { textColor = textColor ?? Color.white }
            };
            
            Vector2 textSize = textStyle.CalcSize(new GUIContent(text));
            Vector2 finalPosition = center + offset;
            
            Rect textRect = new Rect(
                finalPosition.x - textSize.x / 2,
                finalPosition.y - textSize.y / 2,
                textSize.x,
                textSize.y
            );
            
            GUI.Label(textRect, text, textStyle);
        }
        
        /// <summary>
        /// Dibuja una región vacía (para estado inicial del menú)
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <param name="angle">Ángulo de la región vacía</param>
        /// <param name="color">Color de la región vacía</param>
        public static void DrawEmptyRegion(Vector2 center, float outerRadius, float innerRadius, float angle, Color color)
        {
            Vector2 buttonPosition = RadialGeometryCalculator.CalculateButtonPosition(
                center.x, center.y, angle, RadialGeometryCalculator.CalculateAverageRadius(outerRadius, innerRadius)
            );
            
            float sectorSize = RadialGeometryCalculator.CalculateSectorSize(outerRadius, innerRadius);
            DrawButtonBackground(buttonPosition, sectorSize, color, false);
        }
        
        /// <summary>
        /// Crea una textura circular con anti-aliasing
        /// </summary>
        /// <param name="size">Tamaño de la textura (resolución)</param>
        /// <returns>Textura circular creada</returns>
        public static Texture2D CreateCircleTexture(int size = 128)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            float center = size / 2f;
            float radius = center - 1f;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    // Anti-aliasing: suavizar bordes del círculo
                    if (distance <= radius - 1f)
                    {
                        // Completamente dentro del círculo
                        pixels[y * size + x] = Color.white;
                    }
                    else if (distance <= radius)
                    {
                        // Zona de transición (anti-aliasing)
                        float alpha = 1f - (distance - (radius - 1f));
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        // Fuera del círculo
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }
        
        /// <summary>
        /// Obtiene el color de fondo para el botón Back
        /// </summary>
        /// <returns>Color del botón Back</returns>
        public static Color GetBackButtonColor()
        {
            return BackButtonColor;
        }
        
        /// <summary>
        /// Obtiene el color de fondo predeterminado para botones
        /// </summary>
        /// <returns>Color de fondo predeterminado</returns>
        public static Color GetDefaultButtonColor()
        {
            return BackgroundColor;
        }
        
        /// <summary>
        /// Obtiene el color del borde
        /// </summary>
        /// <returns>Color del borde</returns>
        public static Color GetBorderColor()
        {
            return BorderColor;
        }
        
        
        /// <summary>
        /// Dibuja un círculo con color específico
        /// </summary>
        /// <param name="centerX">Centro X</param>
        /// <param name="centerY">Centro Y</param>
        /// <param name="radius">Radio del círculo</param>
        /// <param name="color">Color del círculo</param>
        private static void DrawCircle(float centerX, float centerY, float radius, Color color)
        {
            Rect circleRect = new Rect(
                centerX - radius,
                centerY - radius,
                radius * 2,
                radius * 2
            );
            
            Color oldColor = GUI.color;
            GUI.color = color;
            
            // Crear textura circular con mayor resolución para suavidad
            Texture2D circleTexture = CreateCircleTexture(128); // Aumentado de 64 a 128
            GUI.DrawTexture(circleRect, circleTexture);
            
            GUI.color = oldColor;
            Object.DestroyImmediate(circleTexture);
        }
        
        /// <summary>
        /// Dibuja el borde de un círculo
        /// </summary>
        /// <param name="centerX">Centro X</param>
        /// <param name="centerY">Centro Y</param>
        /// <param name="radius">Radio del círculo</param>
        private static void DrawCircleBorder(float centerX, float centerY, float radius)
        {
            Color oldColor = Handles.color;
            Handles.color = BorderColor;

            Vector3 center = new Vector3(centerX, centerY, 0);
            Handles.DrawWireDisc(center, Vector3.forward, radius);

            Handles.color = oldColor;
        }

    }
}
#endif
