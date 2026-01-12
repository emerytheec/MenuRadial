namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz para componentes que pueden generar animaciones
    /// Permite a MRMenuControl identificar el tipo de animación sin necesidad de cálculos
    /// </summary>
    public interface IAnimationProvider
    {
        /// <summary>
        /// Tipo de animación que genera este componente
        /// Se calcula automáticamente basado en la configuración del componente
        /// </summary>
        AnimationType AnimationType { get; }
        
        /// <summary>
        /// Nombre de la animación que se generará
        /// </summary>
        string AnimationName { get; }
        
        /// <summary>
        /// Indica si el componente está listo para generar animaciones
        /// </summary>
        bool CanGenerateAnimation { get; }
        
        /// <summary>
        /// Descripción del tipo de animación 
        /// </summary>
        string GetAnimationTypeDescription();
    }
}
