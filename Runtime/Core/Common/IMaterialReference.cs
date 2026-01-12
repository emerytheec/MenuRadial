using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz para referencias de materiales en frames
    /// Define el comportamiento base para asignar materiales específicos a meshes
    /// </summary>
    public interface IMaterialReference
    {
        /// <summary>
        /// Renderer objetivo donde se aplicará el material
        /// </summary>
        Renderer TargetRenderer { get; set; }
        
        /// <summary>
        /// Índice del material dentro del array de materiales del renderer
        /// </summary>
        int MaterialIndex { get; set; }
        
        /// <summary>
        /// Material alternativo a aplicar. Si es null, usa el material original
        /// </summary>
        Material AlternativeMaterial { get; set; }
        
        /// <summary>
        /// Material original del renderer (para restauración)
        /// </summary>
        Material OriginalMaterial { get; }
        
        /// <summary>
        /// Indica si la referencia es válida
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        /// Indica si tiene un material alternativo asignado
        /// </summary>
        bool HasAlternativeMaterial { get; }
        
        /// <summary>
        /// Aplica el material alternativo al renderer
        /// </summary>
        void ApplyMaterial();
        
        /// <summary>
        /// Restaura el material original al renderer
        /// </summary>
        void RestoreOriginalMaterial();
        
        /// <summary>
        /// Actualiza la referencia al material original
        /// </summary>
        void UpdateOriginalMaterial();
    }
}