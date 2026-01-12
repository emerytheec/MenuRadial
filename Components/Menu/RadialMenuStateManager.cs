using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Gestor de estados del menú radial
    /// Responsabilidad única: Gestión de estados del menú (inicial vs con contenido)
    /// </summary>
    public static class RadialMenuStateManager
    {
        /// <summary>
        /// Estados posibles del menú radial
        /// </summary>
        public enum MenuState
        {
            /// <summary>
            /// Estado inicial: sin slots configurados, muestra Back + región vacía
            /// </summary>
            Initial,
            
            /// <summary>
            /// Estado normal: con slots configurados y contenido
            /// </summary>
            WithContent
        }
        
        /// <summary>
        /// Determina el estado actual del menú basado en los botones disponibles
        /// </summary>
        /// <param name="buttonNames">Array de nombres de botones</param>
        /// <returns>Estado del menú determinado</returns>
        public static MenuState DetermineMenuState(string[] buttonNames)
        {
            return (buttonNames == null || buttonNames.Length == 0) ? MenuState.Initial : MenuState.WithContent;
        }
        
        /// <summary>
        /// Obtiene el número total de sectores basado en el estado del menú
        /// </summary>
        /// <param name="state">Estado del menú</param>
        /// <param name="buttonCount">Número de botones del usuario</param>
        /// <returns>Número total de sectores</returns>
        public static int GetTotalSectors(MenuState state, int buttonCount)
        {
            return state switch
            {
                MenuState.Initial => 2, // Back + región vacía
                MenuState.WithContent => buttonCount + 1, // Back + botones del usuario
                _ => 2
            };
        }
        
        /// <summary>
        /// Obtiene la configuración de ángulos para el estado del menú
        /// </summary>
        /// <param name="state">Estado del menú</param>
        /// <param name="buttonCount">Número de botones del usuario</param>
        /// <returns>Configuración de ángulos</returns>
        public static AngleConfiguration GetAngleConfiguration(MenuState state, int buttonCount)
        {
            int totalSectors = GetTotalSectors(state, buttonCount);
            float anglePerButton = totalSectors > 0 ? 360f / totalSectors : 0f;
            float initialAngle = RadialGeometryCalculator.GetInitialAngle(); // -90f (12 en punto)
            
            return new AngleConfiguration
            {
                TotalSectors = totalSectors,
                AnglePerButton = anglePerButton,
                InitialAngle = initialAngle,
                BackButtonAngle = initialAngle, // Back siempre en posición 0 (arriba)
                ContentStartAngle = initialAngle + anglePerButton // Contenido empieza después del Back
            };
        }
        
        /// <summary>
        /// Verifica si el menú debe mostrar el estado inicial
        /// </summary>
        /// <param name="buttonNames">Array de nombres de botones</param>
        /// <returns>True si debe mostrar estado inicial</returns>
        public static bool ShouldShowInitialState(string[] buttonNames)
        {
            return DetermineMenuState(buttonNames) == MenuState.Initial;
        }
        
        
        /// <summary>
        /// Obtiene el texto central apropiado para el estado del menú
        /// </summary>
        /// <param name="state">Estado del menú</param>
        /// <returns>Texto central a mostrar</returns>
        public static string GetCentralText(MenuState state)
        {
            return "Menú Control"; // Mismo texto para todos los estados por ahora
        }
        
        /// <summary>
        /// Valida si la transición entre estados es válida
        /// </summary>
        /// <param name="fromState">Estado actual</param>
        /// <param name="toState">Estado destino</param>
        /// <returns>True si la transición es válida</returns>
        public static bool IsValidTransition(MenuState fromState, MenuState toState)
        {
            // Todas las transiciones son válidas en este sistema simple
            return true;
        }
        
        /// <summary>
        /// Obtiene información de configuración para renderizado específica del estado
        /// </summary>
        /// <param name="state">Estado del menú</param>
        /// <returns>Configuración de renderizado</returns>
        public static RenderConfiguration GetRenderConfiguration(MenuState state)
        {
            return state switch
            {
                MenuState.Initial => new RenderConfiguration
                {
                    ShowEmptyRegion = true,
                    EmptyRegionAngle = 90f, // 6 en punto (abajo)
                    EmptyRegionColor = new Color(0.3f, 0.3f, 0.3f, 0.3f),
                    ShowDividerLines = true,
                    CentralTextSize = 14f
                },
                MenuState.WithContent => new RenderConfiguration
                {
                    ShowEmptyRegion = false,
                    EmptyRegionAngle = 0f,
                    EmptyRegionColor = Color.clear,
                    ShowDividerLines = true,
                    CentralTextSize = 14f
                },
                _ => new RenderConfiguration()
            };
        }
    }
    
    /// <summary>
    /// Configuración de ángulos para el menú radial
    /// </summary>
    public struct AngleConfiguration
    {
        /// <summary>
        /// Número total de sectores en el menú
        /// </summary>
        public int TotalSectors;
        
        /// <summary>
        /// Ángulo en grados por cada botón/sector
        /// </summary>
        public float AnglePerButton;
        
        /// <summary>
        /// Ángulo inicial de referencia
        /// </summary>
        public float InitialAngle;
        
        /// <summary>
        /// Ángulo específico del botón Back
        /// </summary>
        public float BackButtonAngle;
        
        /// <summary>
        /// Ángulo donde empieza el contenido del usuario
        /// </summary>
        public float ContentStartAngle;
    }
    
    /// <summary>
    /// Configuración de renderizado específica por estado
    /// </summary>
    public struct RenderConfiguration
    {
        /// <summary>
        /// Si debe mostrar región vacía (estado inicial)
        /// </summary>
        public bool ShowEmptyRegion;
        
        /// <summary>
        /// Ángulo de la región vacía
        /// </summary>
        public float EmptyRegionAngle;
        
        /// <summary>
        /// Color de la región vacía
        /// </summary>
        public Color EmptyRegionColor;
        
        /// <summary>
        /// Si debe mostrar líneas divisorias
        /// </summary>
        public bool ShowDividerLines;
        
        /// <summary>
        /// Tamaño del texto central
        /// </summary>
        public float CentralTextSize;
    }
}
