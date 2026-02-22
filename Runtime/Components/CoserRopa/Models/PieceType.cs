namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Tipo de pieza detectada dentro del avatar.
    /// Clasificada automaticamente por zona de cosido y senales de deteccion,
    /// pero el usuario puede cambiarla manualmente.
    /// </summary>
    public enum PieceType
    {
        /// <summary>
        /// Ropa tradicional: vestidos, camisas, pantalones, trajes completos
        /// </summary>
        Ropa,

        /// <summary>
        /// Pelucas y accesorios de cabeza con peso en huesos de cabeza
        /// </summary>
        Pelo,

        /// <summary>
        /// Piezas genericas: accesorios, props, piezas sin huesos humanoides
        /// </summary>
        Pieza
    }
}
