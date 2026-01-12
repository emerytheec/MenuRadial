using UnityEngine;
using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz que define los datos de un frame individual
    /// </summary>
    public interface IFrameData
    {
        /// <summary>
        /// Lista de referencias de objetos en este frame
        /// </summary>
        List<ObjectReference> ObjectReferences { get; }
        
        /// <summary>
        /// Lista de referencias de materiales en este frame
        /// </summary>
        List<IMaterialReference> MaterialReferences { get; }
        
        /// <summary>
        /// Nombre identificativo del frame
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Aplica el estado de todos los objetos y materiales definidos en este frame
        /// </summary>
        void ApplyState();
        
        /// <summary>
        /// Añade una referencia de objeto al frame
        /// </summary>
        /// <param name="gameObject">GameObject a añadir</param>
        /// <param name="isActive">Estado de activación deseado</param>
        void AddObjectReference(GameObject gameObject, bool isActive = true);
        
        /// <summary>
        /// Elimina una referencia de objeto del frame
        /// </summary>
        /// <param name="gameObject">GameObject a eliminar</param>
        void RemoveObjectReference(GameObject gameObject);
        
        /// <summary>
        /// Limpia todas las referencias de objetos
        /// </summary>
        void ClearObjectReferences();
        
        /// <summary>
        /// Añade una referencia de material al frame
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <param name="alternativeMaterial">Material alternativo</param>
        void AddMaterialReference(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null);
        
        /// <summary>
        /// Elimina una referencia de material del frame
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        void RemoveMaterialReference(Renderer renderer, int materialIndex = 0);
        
        /// <summary>
        /// Limpia todas las referencias de materiales
        /// </summary>
        void ClearMaterialReferences();
        
        /// <summary>
        /// Valida que todas las referencias del frame sean válidas
        /// </summary>
        /// <returns>True si todas las referencias son válidas</returns>
        bool ValidateReferences();
    }
}