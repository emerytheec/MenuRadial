using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Gestor de iconos para el menú radial
    /// Responsabilidad única: Gestión y renderizado de iconos (fondo, primer plano, assets)
    /// </summary>
    public static class RadialIconManager
    {
        /// <summary>
        /// Configuración para el renderizado de iconos multicapa
        /// </summary>
        public struct IconRenderConfig
        {
            /// <summary>
            /// Tamaño del icono de fondo (imagen logo)
            /// </summary>
            public float BackgroundIconSize;
            
            /// <summary>
            /// Tamaño del icono de primer plano (menú)
            /// </summary>
            public float ForegroundIconSize;
            
            /// <summary>
            /// Transparencia del icono de fondo
            /// </summary>
            public float BackgroundAlpha;
            
            /// <summary>
            /// Offset del icono de primer plano
            /// </summary>
            public Vector2 ForegroundOffset;
            
            /// <summary>
            /// Transparencia del icono de primer plano cuando hay icono de fondo
            /// </summary>
            public float ForegroundAlphaWithBackground;

            /// <summary>
            /// Configuración por defecto
            /// </summary>
            public static IconRenderConfig Default => new IconRenderConfig
            {
                BackgroundIconSize = 63f, // Icono del usuario (+50% más grande: 42 * 1.5 = 63)
                ForegroundIconSize = 20f, // Icono automático más pequeño
                BackgroundAlpha = 1.0f,   // Icono del usuario opaco
                ForegroundAlphaWithBackground = 0.7f, // Icono automático semi-transparente cuando hay usuario
                ForegroundOffset = new Vector2(0f, -8f) // Offset para separar
            };
        }
        
        /// <summary>
        /// Dibuja iconos multicapa en una posición específica
        /// Primero el icono de fondo (logo) semi-transparente, luego el de primer plano (menú) opaco
        /// </summary>
        /// <param name="center">Centro donde dibujar los iconos</param>
        /// <param name="backgroundIcon">Icono de fondo (imagen logo)</param>
        /// <param name="foregroundIcon">Icono de primer plano (menú)</param>
        /// <param name="config">Configuración de renderizado</param>
        public static void DrawLayeredIcons(Vector2 center, Texture2D backgroundIcon, Texture2D foregroundIcon, IconRenderConfig? config = null)
        {
            IconRenderConfig renderConfig = config ?? IconRenderConfig.Default;

            bool hasBackgroundIcon = backgroundIcon != null;

            if (hasBackgroundIcon)
            {
                // Si el usuario tiene icono personalizado, solo mostrar ese (sin icono automático encima)
                DrawIcon(center, backgroundIcon, renderConfig.BackgroundIconSize, renderConfig.BackgroundAlpha);
            }
            else if (foregroundIcon != null)
            {
                // Sin icono de usuario: mostrar icono automático
                DrawIcon(center, foregroundIcon, renderConfig.BackgroundIconSize, 1.0f);
            }
        }
        
        /// <summary>
        /// Dibuja un icono individual en una posición específica
        /// </summary>
        /// <param name="center">Centro donde dibujar el icono</param>
        /// <param name="icon">Textura del icono</param>
        /// <param name="size">Tamaño del icono</param>
        /// <param name="alpha">Transparencia (0.0 a 1.0)</param>
        public static void DrawIcon(Vector2 center, Texture2D icon, float size, float alpha = 1.0f)
        {
            if (icon == null)
                return;
                
            Rect iconRect = RadialGeometryCalculator.CalculateCenteredRect(center, size);
            
            // Aplicar transparencia si es necesario
            if (alpha < 1.0f)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(iconRect, icon);
                GUI.color = oldColor;
            }
            else
            {
                GUI.DrawTexture(iconRect, icon);
            }
        }
        
        /// <summary>
        /// Obtiene los iconos apropiados para un slot basado en su tipo
        /// </summary>
        /// <param name="slot">Slot de animación</param>
        /// <returns>Tupla con icono del menú y imagen logo</returns>
        public static (Texture2D menuIcon, Texture2D logoImage) GetIconsForSlot(MRAnimationSlot slot)
        {
            if (slot == null)
                return (null, null);
                
            // Usar MRIconLoader para obtener los iconos
            return MRIconLoader.GetIconsForSlot(slot);
        }
        
        /// <summary>
        /// Obtiene el icono del botón Back
        /// </summary>
        /// <returns>Icono del botón Back</returns>
        public static Texture2D GetBackIcon()
        {
            return MRIconLoader.GetBackIcon();
        }
        
        /// <summary>
        /// Valida si una textura es válida para ser usada como icono
        /// </summary>
        /// <param name="texture">Textura a validar</param>
        /// <returns>True si la textura es válida</returns>
        public static bool IsValidIcon(Texture2D texture)
        {
            return texture != null && texture.width > 0 && texture.height > 0;
        }
        
        /// <summary>
        /// Calcula el área ocupada por los iconos multicapa
        /// </summary>
        /// <param name="center">Centro de los iconos</param>
        /// <param name="config">Configuración de renderizado</param>
        /// <returns>Rect que contiene ambos iconos</returns>
        public static Rect CalculateIconsBounds(Vector2 center, IconRenderConfig? config = null)
        {
            IconRenderConfig renderConfig = config ?? IconRenderConfig.Default;
            
            // Calcular el área que ocupan ambos iconos
            float maxSize = Mathf.Max(renderConfig.BackgroundIconSize, renderConfig.ForegroundIconSize);
            Vector2 foregroundCenter = center + renderConfig.ForegroundOffset;
            
            // Expandir el área para incluir ambos iconos
            float minX = Mathf.Min(center.x - renderConfig.BackgroundIconSize/2, foregroundCenter.x - renderConfig.ForegroundIconSize/2);
            float maxX = Mathf.Max(center.x + renderConfig.BackgroundIconSize/2, foregroundCenter.x + renderConfig.ForegroundIconSize/2);
            float minY = Mathf.Min(center.y - renderConfig.BackgroundIconSize/2, foregroundCenter.y - renderConfig.ForegroundIconSize/2);
            float maxY = Mathf.Max(center.y + renderConfig.BackgroundIconSize/2, foregroundCenter.y + renderConfig.ForegroundIconSize/2);
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        
        /// <summary>
        /// Dibuja texto del slot debajo de los iconos
        /// </summary>
        /// <param name="center">Centro de los iconos</param>
        /// <param name="text">Texto a mostrar</param>
        /// <param name="config">Configuración de renderizado</param>
        public static void DrawSlotText(Vector2 center, string text, IconRenderConfig? config = null)
        {
            if (string.IsNullOrEmpty(text))
                return;
                
            IconRenderConfig renderConfig = config ?? IconRenderConfig.Default;
            
            // Calcular posición del texto debajo de los iconos
            float textOffset = Mathf.Max(renderConfig.BackgroundIconSize, renderConfig.ForegroundIconSize) / 2f + 8f;
            Vector2 textPosition = new Vector2(center.x, center.y + textOffset);
            
            RadialMenuRenderer.DrawText(textPosition, text, 10, Color.white);
        }
        
        /// <summary>
        /// Renderiza un botón completo con iconos y texto
        /// </summary>
        /// <param name="center">Centro del botón</param>
        /// <param name="backgroundIcon">Icono de fondo</param>
        /// <param name="foregroundIcon">Icono de primer plano</param>
        /// <param name="text">Texto del botón</param>
        /// <param name="config">Configuración de renderizado</param>
        public static void RenderCompleteButton(Vector2 center, Texture2D backgroundIcon, Texture2D foregroundIcon, string text, IconRenderConfig? config = null)
        {
            // Dibujar iconos multicapa
            DrawLayeredIcons(center, backgroundIcon, foregroundIcon, config);
            
            // Dibujar texto del slot
            DrawSlotText(center, text, config);
        }
        
        
        /// <summary>
        /// Limpia el cache de iconos (delegado a MRIconLoader)
        /// </summary>
        public static void ClearIconCache()
        {
            MRIconLoader.ClearCache();
        }
        
        /// <summary>
        /// Crea una configuración personalizada para iconos
        /// </summary>
        /// <param name="backgroundSize">Tamaño del icono de fondo</param>
        /// <param name="foregroundSize">Tamaño del icono de primer plano</param>
        /// <param name="backgroundAlpha">Transparencia del fondo</param>
        /// <param name="foregroundOffsetY">Offset Y del primer plano</param>
        /// <returns>Configuración personalizada</returns>
        public static IconRenderConfig CreateCustomConfig(float backgroundSize, float foregroundSize, float backgroundAlpha = 1.0f, float foregroundOffsetY = -8f, float foregroundAlphaWithBg = 0.7f)
        {
            return new IconRenderConfig
            {
                BackgroundIconSize = backgroundSize,
                ForegroundIconSize = foregroundSize,
                BackgroundAlpha = backgroundAlpha,
                ForegroundAlphaWithBackground = foregroundAlphaWithBg,
                ForegroundOffset = new Vector2(0f, foregroundOffsetY)
            };
        }
        
        /// <summary>
        /// Escala una configuración de iconos por un factor
        /// </summary>
        /// <param name="baseConfig">Configuración base</param>
        /// <param name="scaleFactor">Factor de escala</param>
        /// <returns>Configuración escalada</returns>
        public static IconRenderConfig ScaleConfig(IconRenderConfig baseConfig, float scaleFactor)
        {
            return new IconRenderConfig
            {
                BackgroundIconSize = baseConfig.BackgroundIconSize * scaleFactor,
                ForegroundIconSize = baseConfig.ForegroundIconSize * scaleFactor,
                BackgroundAlpha = baseConfig.BackgroundAlpha, // Alpha no se escala
                ForegroundAlphaWithBackground = baseConfig.ForegroundAlphaWithBackground, // Alpha no se escala
                ForegroundOffset = baseConfig.ForegroundOffset * scaleFactor
            };
        }

        /// <summary>
        /// Calcula el tamaño adaptativo de iconos basado en la geometría del sector
        /// </summary>
        /// <param name="slotCount">Número de slots (sin contar Back)</param>
        /// <param name="outerRadius">Radio exterior del menú</param>
        /// <param name="innerRadius">Radio interior del menú</param>
        /// <returns>Tamaño óptimo del icono</returns>
        public static float CalculateAdaptiveIconSize(int slotCount, float outerRadius, float innerRadius)
        {
            // Mínimo 1 slot para evitar división por cero
            int totalSectors = Mathf.Max(slotCount + 1, 2); // +1 para Back

            // Ángulo disponible por sector
            float anglePerSlot = 360f / totalSectors;

            // Radio promedio donde se posicionan los iconos
            float avgRadius = (outerRadius + innerRadius) / 2f;

            // Longitud del arco en el radio promedio
            float arcLength = 2f * Mathf.PI * avgRadius * (anglePerSlot / 360f);

            // Ancho radial disponible (70% del espacio entre radios)
            float radialWidth = (outerRadius - innerRadius) * 0.7f;

            // El icono no debe exceder el menor de los dos, con margen de seguridad del 75%
            float maxSize = Mathf.Min(arcLength, radialWidth) * 0.75f;

            // Límites mínimo y máximo absolutos
            const float MIN_ICON_SIZE = 25f;
            const float MAX_ICON_SIZE = 70f;

            return Mathf.Clamp(maxSize, MIN_ICON_SIZE, MAX_ICON_SIZE);
        }

        /// <summary>
        /// Crea una configuración de iconos adaptativa basada en el número de slots
        /// </summary>
        /// <param name="slotCount">Número de slots</param>
        /// <param name="outerRadius">Radio exterior</param>
        /// <param name="innerRadius">Radio interior</param>
        /// <returns>Configuración adaptada</returns>
        public static IconRenderConfig CreateAdaptiveConfig(int slotCount, float outerRadius, float innerRadius)
        {
            float iconSize = CalculateAdaptiveIconSize(slotCount, outerRadius, innerRadius);

            return new IconRenderConfig
            {
                BackgroundIconSize = iconSize,
                ForegroundIconSize = iconSize * 0.5f, // Icono automático es 50% del tamaño
                BackgroundAlpha = 1.0f,
                ForegroundAlphaWithBackground = 0.7f,
                ForegroundOffset = new Vector2(0f, -iconSize * 0.15f)
            };
        }
    }
}
