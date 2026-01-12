namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Modo de cosido de ropa
    /// </summary>
    public enum StitchingMode
    {
        /// <summary>
        /// Coser: Reparenta huesos de ropa bajo huesos del avatar.
        /// Los huesos de la ropa se mantienen como hijos (duplicados).
        /// Resultado: Hips -> Hips (Ropa)
        /// </summary>
        Stitch,

        /// <summary>
        /// Fusionar: Actualiza el SkinnedMeshRenderer para usar huesos del avatar.
        /// Los huesos de la ropa se eliminan, el mesh usa directamente los del avatar.
        /// Resultado: Solo Hips (del avatar), mesh de ropa lo usa directamente.
        /// Similar a Modular Avatar Merge Armature.
        /// </summary>
        Merge
    }
}
