namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Tipos de animación soportados por el sistema Menu Radial
    /// </summary>
    public enum AnimationType
    {
        /// <summary>
        /// Sin tipo de animación definido o componente que no genera animaciones
        /// </summary>
        None,
        
        /// <summary>
        /// Animación ON/OFF - Se genera cuando hay 1 frame
        /// Utiliza parámetro Bool en VRChat
        /// </summary>
        OnOff,
        
        /// <summary>
        /// Animación A/B - Se genera cuando hay 2 frames  
        /// Utiliza parámetro Bool en VRChat
        /// </summary>
        AB,
        
        /// <summary>
        /// Animación lineal - Se genera cuando hay 3+ frames o es MRIluminacionRadial
        /// Utiliza parámetro Float en VRChat
        /// </summary>
        Linear,
        
        /// <summary>
        /// SubMenú - Otro MRMenuControl usado como navegación
        /// No genera animaciones, solo navegación
        /// </summary>
        SubMenu
    }
}
