namespace Bender_Dios.MenuRadial.Components.MenuRadial.Models
{
    /// <summary>
    /// Modos de integración del menú MR en el menú de expresiones del avatar.
    /// </summary>
    public enum MenuIntegrationMode
    {
        /// <summary>
        /// Añade el menú directamente al menú raíz del avatar.
        /// </summary>
        RootMenu = 0,

        /// <summary>
        /// Añade el menú dentro de un submenú existente del avatar.
        /// </summary>
        ExistingSubMenu = 1,

        /// <summary>
        /// Crea una ruta de menús personalizada (ej: "Outfits/Casual").
        /// Si los menús intermedios no existen, se crean automáticamente.
        /// </summary>
        CustomPath = 2
    }
}
