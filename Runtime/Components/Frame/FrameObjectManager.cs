using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Managers;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Manager especializado para la gestión de objetos en frames
    /// VERSIÓN 0.036: REFACTORIZADO - Hereda de BaseReferenceManager<T>
    /// REDUCIDO de 245 líneas a 45 líneas (-82%)
    /// Responsabilidad única: Solo especialización específica de ObjectReference
    /// </summary>
    public class FrameObjectManager : BaseReferenceManager<ObjectReference, GameObject>
    {
        /// <summary>
        /// Constructor con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        public FrameObjectManager(FrameData frameData) : base(frameData)
        {
        }
        
        
        /// <summary>
        /// Obtiene el list manager específico para ObjectReference
        /// </summary>
        protected override ReferenceListManager<ObjectReference, GameObject> GetListManager()
        {
            return _frameData.ObjectReferenceListManager;
        }
        
        /// <summary>
        /// Crea una copia del estado actual de una referencia de objeto
        /// </summary>
        protected override ObjectReference CreateStateCopy(ObjectReference reference)
        {
            if (reference?.GameObject == null) return null;
            
            // Capturar el estado actual del GameObject en la escena
            bool currentState = reference.GameObject.activeSelf;
            return new ObjectReference(reference.GameObject, currentState);
        }
        
        
        
        /// <summary>
        /// Añade un GameObject al frame con estado específico
        /// </summary>
        public void AddObject(GameObject gameObject, bool isActive = true)
        {
            if (gameObject == null)
            {
                return;
            }
            
            var reference = new ObjectReference(gameObject, isActive);
            Add(reference);
        }
        
        /// <summary>
        /// Elimina un GameObject específico del frame
        /// </summary>
        public void RemoveObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }
            
            RemoveByTarget(gameObject);
        }
        
        /// <summary>
        /// Marca todos los objetos como activos
        /// </summary>
        public void SelectAllObjects()
        {
            foreach (var objRef in GetValidReferences())
            {
                objRef.IsActive = true;
            }
        }
        
        /// <summary>
        /// Marca todos los objetos como inactivos
        /// </summary>
        public void DeselectAllObjects()
        {
            foreach (var objRef in GetValidReferences())
            {
                objRef.IsActive = false;
            }
        }
        
        
        
        /// <summary>
        /// Alias para compatibilidad: ClearAllObjects -> ClearAll
        /// </summary>
        public void ClearAllObjects() => ClearAll();
        
        /// <summary>
        /// Alias para compatibilidad: RecalculateAllPaths -> UpdateAllHierarchyPaths
        /// </summary>
        public void RecalculateAllPaths() => UpdateAllHierarchyPaths();
        
        /// <summary>
        /// Alias para compatibilidad: RemoveInvalidReferences -> RemoveInvalid
        /// </summary>
        public void RemoveInvalidReferences() => RemoveInvalid();
        
        /// <summary>
        /// Alias para compatibilidad: GetObjectCount -> Count
        /// </summary>
        public int GetObjectCount() => Count;
        
        /// <summary>
        /// Alias para compatibilidad: GetValidObjectCount -> ValidCount
        /// </summary>
        public int GetValidObjectCount() => ValidCount;
        
        /// <summary>
        /// Alias para compatibilidad: GetInvalidObjectCount -> InvalidCount
        /// </summary>
        public int GetInvalidObjectCount() => InvalidCount;
        
        /// <summary>
        /// Alias para compatibilidad: ApplyObjectStates -> ApplyAllStates
        /// </summary>
        public void ApplyObjectStates() => ApplyAllStates();
        
        /// <summary>
        /// Alias para compatibilidad: CaptureCurrentStates -> CaptureCurrentStates (ya existe en base)
        /// </summary>
        public List<ObjectReference> CaptureCurrentStates() => CaptureCurrentStates();
        
        /// <summary>
        /// Alias para compatibilidad: RestoreStates -> RestoreStates (ya existe en base)
        /// </summary>
        public void RestoreStates(List<ObjectReference> capturedStates) => RestoreStates(capturedStates);
        
        /// <summary>
        /// Alias para compatibilidad: ValidateObjects -> ValidateAll con filtro
        /// </summary>
        public ValidationResult ValidateObjects() => ValidateAll();
        
        
        
        
        /// <summary>
        /// Acceso directo a ObjectReferences para compatibilidad
        /// </summary>
        public List<ObjectReference> ObjectReferences => References;
        
        /// <summary>
        /// Número total de objetos (alias para Count)
        /// </summary>
        public int ObjectCount => Count;
        
        /// <summary>
        /// Número de objetos válidos (alias para ValidCount)
        /// </summary>
        public int ValidObjectCount => ValidCount;
        
        /// <summary>
        /// Número de objetos inválidos (alias para InvalidCount)
        /// </summary>
        public int InvalidObjectCount => InvalidCount;
        
    }
}
