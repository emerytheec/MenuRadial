namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Zona del cuerpo donde se cose la ropa.
    /// Clasificada automáticamente según los huesos humanoides detectados.
    /// </summary>
    public enum StitchZone
    {
        /// <summary>
        /// Torso + extremidades (ropa tradicional: camisas, vestidos, trajes completos)
        /// </summary>
        FullBody,

        /// <summary>
        /// Solo Spine/Chest/Neck sin extremidades (capas, chalecos)
        /// </summary>
        Torso,

        /// <summary>
        /// Solo Head/Neck (pelucas, sombreros, accesorios de cabeza)
        /// </summary>
        Head,

        /// <summary>
        /// Solo brazos/manos (guantes, brazaletes)
        /// </summary>
        UpperLimb,

        /// <summary>
        /// Solo piernas/pies (zapatos, botas)
        /// </summary>
        LowerLimb,

        /// <summary>
        /// Solo Hips (cinturones, faldas, colas)
        /// </summary>
        Hip
    }
}
