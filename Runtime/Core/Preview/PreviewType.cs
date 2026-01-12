namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Tipos de previsualización disponibles en el sistema MR Control Menu
    /// </summary>
    public enum PreviewType
    {
        /// <summary>
        /// Sin previsualización disponible
        /// </summary>
        None,
        
        /// <summary>
        /// Previsualización lineal con deslizador circular
        /// Usado para animaciones con 3+ frames que requieren interfaz de valor continuo
        /// </summary>
        Linear,
        
        /// <summary>
        /// Previsualización de toggle simple
        /// Usado para animaciones ON/OFF o A/B que alternan entre dos estados
        /// </summary>
        Toggle,
        
        /// <summary>
        /// Previsualización de submenú
        /// Usado para navegación a otros MR Control Menu
        /// </summary>
        SubMenu,
        
        /// <summary>
        /// Previsualización de iluminación
        /// Caso especial para MRIluminacionRadial con acciones automáticas
        /// </summary>
        Illumination
    }
}
