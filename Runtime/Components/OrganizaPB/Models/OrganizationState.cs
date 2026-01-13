namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Estado de la organización de PhysBones.
    /// </summary>
    public enum OrganizationState
    {
        /// <summary>
        /// No se ha escaneado el avatar.
        /// </summary>
        NotScanned,

        /// <summary>
        /// Escaneado pero no organizado. Los PhysBones están en su ubicación original.
        /// </summary>
        Scanned,

        /// <summary>
        /// Los PhysBones han sido organizados en contenedores.
        /// </summary>
        Organized
    }
}
