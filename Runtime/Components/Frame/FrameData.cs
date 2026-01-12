using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Implementación simplificada de IFrameData usando ReferenceListManager
    /// </summary>
    [Serializable]
    public class FrameData : IFrameData
    {
        [SerializeField] private string _name = "Frame";
        
        // NUEVO: Gestores genéricos reemplazan lógica duplicada
        [SerializeField] private ReferenceListManager<ObjectReference, GameObject> _objectManager = new ReferenceListManager<ObjectReference, GameObject>();
        [SerializeField] private ReferenceListManager<MaterialReference, Renderer> _materialManager = new ReferenceListManager<MaterialReference, Renderer>();
        [SerializeField] private ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer> _blendshapeManager = new ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer>();
        
        /// <summary>
        /// Nombre identificativo del frame
        /// </summary>
        public string Name 
        { 
            get => _name; 
            set => _name = value; 
        }
        
        /// <summary>
        /// Lista de referencias de objetos en este frame (delegada al manager)
        /// </summary>
        public List<ObjectReference> ObjectReferences => _objectManager.References;
        
        /// <summary>
        /// Lista de referencias de materiales en este frame (conversión para compatibilidad)
        /// </summary>
        public List<IMaterialReference> MaterialReferences => _materialManager.References.Cast<IMaterialReference>().ToList();
        
        /// <summary>
        /// Lista directa de referencias de materiales (para serialización)
        /// </summary>
        public List<MaterialReference> MaterialReferencesData => _materialManager.References;
        
        /// <summary>
        /// Lista de referencias de blendshapes en este frame (delegada al manager)
        /// </summary>
        public List<BlendshapeReference> BlendshapeReferences => _blendshapeManager.References;
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public FrameData()
        {
        }
        
        /// <summary>
        /// Constructor con nombre
        /// </summary>
        /// <param name="name">Nombre del frame</param>
        public FrameData(string name)
        {
            _name = name;
        }
        
        /// <summary>
        /// Aplica el estado de todos los objetos, materiales y blendshapes definidos en este frame
        /// </summary>
        public void ApplyState()
        {
            
            // Aplicar todos los tipos usando los managers
            _objectManager.ApplyAll();
            _materialManager.ApplyAll();
            _blendshapeManager.ApplyAll();
            
        }
        
        
        /// <summary>
        /// Añade una referencia de objeto al frame
        /// </summary>
        public void AddObjectReference(GameObject gameObject, bool isActive = true)
        {
            if (gameObject == null) return;
            _objectManager.Add(new ObjectReference(gameObject, isActive));
        }
        
        /// <summary>
        /// Elimina una referencia de objeto del frame
        /// </summary>
        public void RemoveObjectReference(GameObject gameObject)
        {
            if (gameObject == null) return;
            _objectManager.RemoveByTarget(gameObject);
        }
        
        /// <summary>
        /// Limpia todas las referencias de objetos
        /// </summary>
        public void ClearObjectReferences()
        {
            _objectManager.Clear();
        }
        
        /// <summary>
        /// Obtiene el número de referencias válidas
        /// </summary>
        public int GetValidReferenceCount()
        {
            return _objectManager.ValidCount;
        }
        
        /// <summary>
        /// Obtiene el número de referencias inválidas
        /// </summary>
        public int GetInvalidReferenceCount()
        {
            return _objectManager.InvalidCount;
        }
        
        /// <summary>
        /// Limpia las referencias inválidas
        /// </summary>
        public void RemoveInvalidReferences()
        {
            _objectManager.RemoveInvalid();
        }
        
        /// <summary>
        /// Establece el estado activo de todas las referencias
        /// </summary>
        public void SetAllReferencesActive(bool isActive)
        {
            foreach (var objRef in _objectManager.References)
            {
                objRef.IsActive = isActive;
            }
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todas las referencias
        /// </summary>
        public void UpdateAllHierarchyPaths()
        {
            _objectManager.UpdateAllHierarchyPaths();
            _materialManager.UpdateAllHierarchyPaths();
            _blendshapeManager.UpdateAllHierarchyPaths();
        }
        
        /// <summary>
        /// Valida que todas las referencias del frame sean válidas
        /// </summary>
        public bool ValidateReferences()
        {
            return _objectManager.References.All(r => r.IsValid);
        }
        
        
        
        /// <summary>
        /// Añade una referencia de material al frame
        /// </summary>
        public void AddMaterialReference(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            if (renderer == null) return;
            _materialManager.Add(new MaterialReference(renderer, materialIndex, alternativeMaterial));
        }
        
        /// <summary>
        /// Elimina una referencia de material del frame
        /// </summary>
        public void RemoveMaterialReference(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null) return;
            
            // Buscar y eliminar por renderer y índice específicos
            var toRemove = _materialManager.References
                .Where(r => r.TargetRenderer == renderer && r.MaterialIndex == materialIndex)
                .ToList();
                
            foreach (var matRef in toRemove)
            {
                _materialManager.Remove(matRef);
            }
        }
        
        /// <summary>
        /// Limpia todas las referencias de materiales
        /// </summary>
        public void ClearMaterialReferences()
        {
            _materialManager.Clear();
        }
        
        /// <summary>
        /// Obtiene el número de referencias de materiales válidas
        /// </summary>
        public int GetValidMaterialReferenceCount()
        {
            return _materialManager.ValidCount;
        }
        
        /// <summary>
        /// Obtiene el número de referencias de materiales inválidas
        /// </summary>
        public int GetInvalidMaterialReferenceCount()
        {
            return _materialManager.InvalidCount;
        }
        
        /// <summary>
        /// Limpia las referencias de materiales inválidas
        /// </summary>
        public void RemoveInvalidMaterialReferences()
        {
            _materialManager.RemoveInvalid();
        }
        
        /// <summary>
        /// Actualiza las referencias originales de todos los materiales
        /// </summary>
        public void UpdateAllOriginalMaterials()
        {
            foreach (var materialRef in _materialManager.References)
            {
                materialRef.UpdateOriginalMaterial();
            }
        }
        
        /// <summary>
        /// Restaura todos los materiales originales
        /// </summary>
        public void RestoreAllOriginalMaterials()
        {
            foreach (var materialRef in _materialManager.GetValidReferences())
            {
                materialRef.RestoreOriginalMaterial();
            }
        }
        
        
        
        /// <summary>
        /// Añade una referencia de blendshape al frame
        /// </summary>
        public void AddBlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return;
            _blendshapeManager.Add(new BlendshapeReference(renderer, blendshapeName, value));
        }
        
        /// <summary>
        /// Elimina una referencia de blendshape del frame
        /// </summary>
        public void RemoveBlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return;
            
            // Buscar y eliminar por renderer y nombre específicos
            var toRemove = _blendshapeManager.References
                .Where(r => r.TargetRenderer == renderer && r.BlendshapeName == blendshapeName)
                .ToList();
                
            foreach (var blendRef in toRemove)
            {
                _blendshapeManager.Remove(blendRef);
            }
        }
        
        /// <summary>
        /// Elimina todas las referencias de blendshapes de un renderer específico
        /// </summary>
        public void RemoveAllBlendshapeReferences(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;
            _blendshapeManager.RemoveByTarget(renderer);
        }
        
        /// <summary>
        /// Limpia todas las referencias de blendshapes
        /// </summary>
        public void ClearBlendshapeReferences()
        {
            _blendshapeManager.Clear();
        }
        
        /// <summary>
        /// Obtiene el número de referencias de blendshapes válidas
        /// </summary>
        public int GetValidBlendshapeReferenceCount()
        {
            return _blendshapeManager.ValidCount;
        }
        
        /// <summary>
        /// Obtiene el número de referencias de blendshapes inválidas
        /// </summary>
        public int GetInvalidBlendshapeReferenceCount()
        {
            return _blendshapeManager.InvalidCount;
        }
        
        /// <summary>
        /// Limpia las referencias de blendshapes inválidas
        /// </summary>
        public void RemoveInvalidBlendshapeReferences()
        {
            _blendshapeManager.RemoveInvalid();
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todas las referencias de blendshapes
        /// </summary>
        public void UpdateAllBlendshapeRendererPaths()
        {
            _blendshapeManager.UpdateAllHierarchyPaths();
        }
        
        /// <summary>
        /// Captura los valores actuales de todos los blendshapes desde los renderers
        /// </summary>
        public void CaptureAllBlendshapeValues()
        {
            _blendshapeManager.CaptureAllCurrentStates();
        }
        
        /// <summary>
        /// Obtiene todos los blendshapes disponibles de un SkinnedMeshRenderer
        /// </summary>
        public static List<string> GetAvailableBlendshapes(SkinnedMeshRenderer renderer)
        {
            var blendshapes = new List<string>();
            
            if (renderer == null || renderer.sharedMesh == null)
                return blendshapes;
            
            var mesh = renderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                blendshapes.Add(mesh.GetBlendShapeName(i));
            }
            
            return blendshapes;
        }
        
        
        
        /// <summary>
        /// Obtiene el número total de referencias en el frame
        /// </summary>
        public int GetTotalReferenceCount()
        {
            return _objectManager.Count + _materialManager.Count + _blendshapeManager.Count;
        }
        
        /// <summary>
        /// Obtiene el número total de referencias válidas
        /// </summary>
        public int GetTotalValidReferenceCount()
        {
            return _objectManager.ValidCount + _materialManager.ValidCount + _blendshapeManager.ValidCount;
        }
        
        /// <summary>
        /// Obtiene el número total de referencias inválidas
        /// </summary>
        public int GetTotalInvalidReferenceCount()
        {
            return _objectManager.InvalidCount + _materialManager.InvalidCount + _blendshapeManager.InvalidCount;
        }
        
        /// <summary>
        /// Limpia todas las referencias inválidas de todos los tipos
        /// </summary>
        public int RemoveAllInvalidReferences()
        {
            int removed = _objectManager.RemoveInvalid();
            removed += _materialManager.RemoveInvalid();
            removed += _blendshapeManager.RemoveInvalid();
            return removed;
        }
        
        /// <summary>
        /// Limpia todas las referencias de todos los tipos
        /// </summary>
        public void ClearAllReferences()
        {
            _objectManager.Clear();
            _materialManager.Clear();
            _blendshapeManager.Clear();
        }
        
        /// <summary>
        /// Valida todas las referencias usando los managers
        /// </summary>
        public ValidationResult ValidateAllReferences()
        {
            var result = new ValidationResult();
            
            // Validar cada tipo de referencia
            result.AddChild(_objectManager.Validate("Objetos"));
            result.AddChild(_materialManager.Validate("Materiales"));
            result.AddChild(_blendshapeManager.Validate("Blendshapes"));
            
            // Estadísticas generales
            var totalValid = GetTotalValidReferenceCount();
            var totalInvalid = GetTotalInvalidReferenceCount();
            
            if (totalValid > 0)
            {
                result.AddChild(ValidationResult.Success("Total: " + totalValid + " referencias válidas"));
            }
            
            if (totalInvalid > 0)
            {
                result.AddChild(ValidationResult.Warning("Total: " + totalInvalid + " referencias inválidas"));
            }
            
            return result;
        }
        
        
        
        
        /// <summary>
        /// Obtiene el manager de lista de objetos para BaseReferenceManager
        /// </summary>
        /// <returns>ReferenceListManager de objetos</returns>
        public ReferenceListManager<ObjectReference, GameObject> GetObjectListManager()
        {
            return _objectManager;
        }
        
        /// <summary>
        /// Obtiene el manager de lista de materiales para BaseReferenceManager
        /// </summary>
        /// <returns>ReferenceListManager de materiales</returns>
        public ReferenceListManager<MaterialReference, Renderer> GetMaterialListManager()
        {
            return _materialManager;
        }
        
        /// <summary>
        /// Obtiene el manager de lista de blendshapes para BaseReferenceManager
        /// </summary>
        /// <returns>ReferenceListManager de blendshapes</returns>
        public ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer> GetBlendshapeListManager()
        {
            return _blendshapeManager;
        }
        
        /// <summary>
        /// Acceso directo al manager de objetos para compatibilidad con FrameObjectManager
        /// </summary>
        public ReferenceListManager<ObjectReference, GameObject> ObjectReferenceListManager => _objectManager;
        
        /// <summary>
        /// Acceso directo al manager de materiales para compatibilidad con FrameMaterialManager
        /// </summary>
        public ReferenceListManager<MaterialReference, Renderer> MaterialReferenceListManager => _materialManager;
        
        /// <summary>
        /// Acceso directo al manager de blendshapes para compatibilidad con FrameBlendshapeManager
        /// </summary>
        public ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer> BlendshapeReferenceListManager => _blendshapeManager;
        
    }
}
