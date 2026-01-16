#if UNITY_EDITOR
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using static Bender_Dios.MenuRadial.Components.Menu.RadialMenuStateManager;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Sistema de menú radial visual simple basado en VRC-Gesture-Manager
    /// REFACTORIZADO v0.050: Ahora actúa como coordinador de componentes especializados
    /// Responsabilidad única: Coordinación y API pública del sistema de menú radial
    /// </summary>
    public static class SimpleRadialMenuDrawer
    {
        /// <summary>
        /// Dibuja un menú radial simple en el área especificada
        /// Si no hay botones (array vacío), muestra estado inicial con Back + región vacía
        /// </summary>
        public static void DrawRadialMenu(Rect area, string[] buttonNames, Texture2D[] buttonIcons = null)
        {
            DrawRadialMenu(area, buttonNames, buttonIcons, null, null);
        }
        
        /// <summary>
        /// Dibuja un menú radial con soporte para iconos de fondo (logos) e iconos de primer plano (menú)
        /// </summary>
        public static void DrawRadialMenu(Rect area, string[] buttonNames, Texture2D[] foregroundIcons = null, Texture2D[] backgroundIcons = null)
        {
            DrawRadialMenu(area, buttonNames, foregroundIcons, backgroundIcons, null);
        }
        
        /// <summary>
        /// Dibuja un menú radial con soporte para iconos y navegación interactiva
        /// </summary>
        /// <param name="area">Rect donde dibujar el menú</param>
        /// <param name="buttonNames">Nombres de los botones</param>
        /// <param name="foregroundIcons">Iconos de primer plano (menú)</param>
        /// <param name="backgroundIcons">Iconos de fondo (logos)</param>
        /// <param name="onButtonClick">Callback para cuando se hace clic en un botón (-1 = Back, 0+ = slots)</param>
        public static void DrawRadialMenu(Rect area, string[] buttonNames, Texture2D[] foregroundIcons = null, 
                                        Texture2D[] backgroundIcons = null, System.Action<int> onButtonClick = null)
        {
            DrawRadialMenuWithSliders(area, buttonNames, foregroundIcons, backgroundIcons, onButtonClick, null, null);
        }
        
        /// <summary>
        /// Dibuja un menú radial con soporte completo para deslizadores radiales en slots lineales
        /// VERSIÓN 0.050: Completamente refactorizado usando componentes especializados
        /// CORREGIDO: Ahora soporta cualquier IAnimationProvider de tipo Linear
        /// </summary>
        /// <param name="area">Rect donde dibujar el menú</param>
        /// <param name="buttonNames">Nombres de los botones</param>
        /// <param name="foregroundIcons">Iconos de primer plano (menú)</param>
        /// <param name="backgroundIcons">Iconos de fondo (logos)</param>
        /// <param name="onButtonClick">Callback para cuando se hace clic en un botón (-1 = Back, 0+ = slots)</param>
        /// <param name="linearSlots">Array de IAnimationProvider para slots que deben mostrar deslizador radial</param>
        /// <param name="slotKeys">Claves únicas para cada slot (para cache de renderizadores)</param>
        public static void DrawRadialMenuWithSliders(Rect area, string[] buttonNames, Texture2D[] foregroundIcons = null, 
                                        Texture2D[] backgroundIcons = null, System.Action<int> onButtonClick = null,
                                        IAnimationProvider[] linearSlots = null, string[] slotKeys = null)
        {
            // 1. VALIDACIÓN DE PARÁMETROS
            if (buttonNames == null)
                buttonNames = new string[0];
            
            // 2. CÁLCULOS GEOMÉTRICOS INICIALES
            Vector2 center = RadialGeometryCalculator.CalculateMenuCenter(area);
            float outerRadius = RadialGeometryCalculator.CalculateMenuRadius(area);
            float innerRadius = RadialGeometryCalculator.CalculateInnerRadius(outerRadius);
            
            // 3. DETERMINACIÓN DEL ESTADO DEL MENÚ
            var menuState = RadialMenuStateManager.DetermineMenuState(buttonNames);
            var angleConfig = RadialMenuStateManager.GetAngleConfiguration(menuState, buttonNames.Length);
            var renderConfig = RadialMenuStateManager.GetRenderConfiguration(menuState);
            
            // 4. CASO ESPECIAL: Estado inicial
            if (menuState == MenuState.Initial)
            {
                DrawInitialStateRefactored(center, outerRadius, innerRadius, renderConfig, onButtonClick);
                return;
            }
            
            // 5. RENDERIZADO DEL FONDO DEL MENÚ
            RadialMenuRenderer.DrawMenuBackground(center, outerRadius, innerRadius);
            
            // 6. RENDERIZADO DE LÍNEAS DIVISORIAS
            if (renderConfig.ShowDividerLines)
            {
                RadialMenuRenderer.DrawSectorLines(center, outerRadius, innerRadius, angleConfig.TotalSectors);
            }
            
            // 7. CALCULAR CONFIGURACIÓN ADAPTATIVA DE ICONOS
            var adaptiveIconConfig = RadialIconManager.CreateAdaptiveConfig(buttonNames.Length, outerRadius, innerRadius);

            // 8. RENDERIZADO DEL BOTÓN BACK
            DrawBackButtonRefactored(center, outerRadius, innerRadius, angleConfig.BackButtonAngle, onButtonClick, adaptiveIconConfig);

            // 9. RENDERIZADO DE BOTONES DEL USUARIO
            float currentAngle = angleConfig.ContentStartAngle;
            for (int i = 0; i < buttonNames.Length; i++)
            {
                DrawUserButtonRefactored(center, outerRadius, innerRadius, currentAngle, buttonNames[i], i,
                                       foregroundIcons, backgroundIcons, linearSlots, slotKeys, onButtonClick, adaptiveIconConfig);
                currentAngle += angleConfig.AnglePerButton;
            }
            
            // 9. RENDERIZADO DE BORDES
            RadialMenuRenderer.DrawMenuBorders(center, outerRadius, innerRadius);
            
            // 10. TEXTO CENTRAL
            RadialMenuRenderer.DrawCentralText(center, RadialMenuStateManager.GetCentralText(menuState), renderConfig.CentralTextSize);
        }
        
        /// <summary>
        /// Dibuja el estado inicial del menú usando componentes refactorizados
        /// </summary>
        private static void DrawInitialStateRefactored(Vector2 center, float outerRadius, float innerRadius, 
                                                     RenderConfiguration renderConfig, 
                                                     System.Action<int> onButtonClick)
        {
            // Fondo del menú
            RadialMenuRenderer.DrawMenuBackground(center, outerRadius, innerRadius);
            
            // Líneas divisorias (2 sectores)
            if (renderConfig.ShowDividerLines)
            {
                RadialMenuRenderer.DrawSectorLines(center, outerRadius, innerRadius, 2);
            }
            
            // Botón Back en posición superior
            DrawBackButtonRefactored(center, outerRadius, innerRadius, -90f, onButtonClick);
            
            // Región vacía en posición inferior
            if (renderConfig.ShowEmptyRegion)
            {
                RadialMenuRenderer.DrawEmptyRegion(center, outerRadius, innerRadius, 
                                                 renderConfig.EmptyRegionAngle, renderConfig.EmptyRegionColor);
            }
            
            // Bordes
            RadialMenuRenderer.DrawMenuBorders(center, outerRadius, innerRadius);
            
            // Texto central
            RadialMenuRenderer.DrawCentralText(center, "Menú Control", renderConfig.CentralTextSize);
        }
        
        /// <summary>
        /// Dibuja el botón Back usando componentes refactorizados
        /// </summary>
        private static void DrawBackButtonRefactored(Vector2 center, float outerRadius, float innerRadius,
                                                   float angle, System.Action<int> onButtonClick,
                                                   RadialIconManager.IconRenderConfig? iconConfig = null)
        {
            // Calcular posición del botón Back
            Vector2 buttonPosition = RadialGeometryCalculator.CalculateButtonPosition(
                center.x, center.y, angle,
                RadialGeometryCalculator.CalculateAverageRadius(outerRadius, innerRadius)
            );

            // Tamaño del sector
            float sectorSize = RadialGeometryCalculator.CalculateSectorSize(outerRadius, innerRadius);

            // Obtener iconos
            Texture2D backIcon = RadialIconManager.GetBackIcon();

            // Manejar interacción
            bool isHovering = RadialMenuInteractionHandler.HandleClickableButton(
                buttonPosition, sectorSize, RadialMenuRenderer.GetBackButtonColor(),
                -1, "Back", onButtonClick
            );

            // Renderizar iconos y texto con configuración adaptativa
            RadialIconManager.RenderCompleteButton(buttonPosition, null, backIcon, "Back", iconConfig);
        }
        
        /// <summary>
        /// Dibuja un botón de usuario usando componentes refactorizados
        /// ACTUALIZADO: Implementa click-to-show para sliders
        /// </summary>
        private static void DrawUserButtonRefactored(Vector2 center, float outerRadius, float innerRadius,
                                                   float angle, string buttonName, int buttonIndex,
                                                   Texture2D[] foregroundIcons, Texture2D[] backgroundIcons,
                                                   IAnimationProvider[] linearSlots, string[] slotKeys,
                                                   System.Action<int> onButtonClick,
                                                   RadialIconManager.IconRenderConfig? iconConfig = null)
        {
            // Obtener clave del slot
            string slotKey = slotKeys != null && buttonIndex < slotKeys.Length ? slotKeys[buttonIndex] : null;

            // Verificar si es un slot lineal (puede tener slider)
            bool isLinearSlot = RadialSliderIntegration.IsLinearSlot(buttonIndex, linearSlots);

            // Verificar si el slider está expandido
            bool isSliderExpanded = isLinearSlot && RadialSliderIntegration.IsSliderExpanded(slotKey);

            if (isSliderExpanded)
            {
                // Dibujar slot con deslizador radial expandido
                bool hasChanges = RadialSliderIntegration.DrawSlotWithRadialSlider(
                    center, outerRadius, innerRadius, angle, buttonName, buttonIndex,
                    linearSlots[buttonIndex], slotKey, onButtonClick
                );

                // Si hay cambios en el deslizador, forzar repaint
                if (hasChanges && Event.current != null)
                {
                    GUI.changed = true;
                }
            }
            else
            {
                // Crear callback que maneja la interacción
                System.Action<int> wrappedCallback;

                if (isLinearSlot)
                {
                    // Para slots lineales: expandir/colapsar el slider
                    wrappedCallback = (idx) => {
                        RadialSliderIntegration.ToggleSlider(slotKey);
                    };
                }
                else
                {
                    // Para slots no lineales: cerrar slider activo y ejecutar callback original
                    wrappedCallback = (idx) => {
                        // Cerrar cualquier slider activo
                        RadialSliderIntegration.CollapseActiveSlider();

                        // Ejecutar callback original
                        onButtonClick?.Invoke(idx);
                    };
                }

                // Dibujar slot normal con iconos
                DrawNormalSlotRefactored(center, outerRadius, innerRadius, angle, buttonName, buttonIndex,
                                       foregroundIcons, backgroundIcons, wrappedCallback, iconConfig);
            }
        }

        /// <summary>
        /// Dibuja un slot normal (no lineal) usando componentes refactorizados
        /// </summary>
        private static void DrawNormalSlotRefactored(Vector2 center, float outerRadius, float innerRadius,
                                                   float angle, string buttonName, int buttonIndex,
                                                   Texture2D[] foregroundIcons, Texture2D[] backgroundIcons,
                                                   System.Action<int> onButtonClick,
                                                   RadialIconManager.IconRenderConfig? iconConfig = null)
        {
            // Calcular posición del botón
            Vector2 buttonPosition = RadialGeometryCalculator.CalculateButtonPosition(
                center.x, center.y, angle, 
                RadialGeometryCalculator.CalculateAverageRadius(outerRadius, innerRadius)
            );
            
            // Tamaño del sector
            float sectorSize = RadialGeometryCalculator.CalculateSectorSize(outerRadius, innerRadius);
            
            // Obtener iconos
            Texture2D foregroundIcon = (foregroundIcons != null && buttonIndex < foregroundIcons.Length) ? 
                                     foregroundIcons[buttonIndex] : null;
            Texture2D backgroundIcon = (backgroundIcons != null && buttonIndex < backgroundIcons.Length) ? 
                                     backgroundIcons[buttonIndex] : null;
            
            // Manejar interacción
            bool isHovering = RadialMenuInteractionHandler.HandleClickableButton(
                buttonPosition, sectorSize, RadialMenuRenderer.GetDefaultButtonColor(),
                buttonIndex, buttonName, onButtonClick
            );

            // Renderizar iconos y texto con configuración adaptativa
            RadialIconManager.RenderCompleteButton(buttonPosition, backgroundIcon, foregroundIcon, buttonName, iconConfig);
        }

    }
}
#endif
