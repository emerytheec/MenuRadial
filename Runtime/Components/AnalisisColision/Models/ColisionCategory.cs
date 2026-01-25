namespace Bender_Dios.MenuRadial.Components.AnalisisColision.Models
{
    /// <summary>
    /// Categorias de componentes de Modular Avatar segun su impacto en Menu Radial.
    /// </summary>
    public enum ColisionCategory
    {
        /// <summary>
        /// Componentes que causan conflictos directos con MR y deben desactivarse automaticamente
        /// si estan en la raiz de una prenda de ropa.
        /// Incluye: MA Vertex Filter, MA Mesh Cutter, MA Shape Changer, MA Mesh Settings
        /// </summary>
        Problematic = 0,

        /// <summary>
        /// Componentes que pueden causar conflictos pero el usuario debe decidir que hacer.
        /// Incluye: Animator, MA Merge Animator, MA Parameters, MA Menu Installer, MA Menu Group, MA Menu Item, MA Bone Proxy
        /// </summary>
        UserDecision = 1,

        /// <summary>
        /// Componentes compatibles con MR, solo informativos.
        /// Incluye: MA Merge Armature (MR ya lo respeta)
        /// </summary>
        Compatible = 2
    }
}
