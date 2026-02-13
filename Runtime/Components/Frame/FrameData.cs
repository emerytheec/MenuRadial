using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    [Serializable]
    public class FrameData
    {
        [SerializeField] private string _name = "Frame";
        
        // NUEVO: Gestores genéricos reemplazan lógica duplicada
        [SerializeField] private ReferenceListManager<ObjectReference, GameObject> _objectManager = new ReferenceListManager<ObjectReference, GameObject>();
        [SerializeField] private ReferenceListManager<MaterialReference, Renderer> _materialManager = new ReferenceListManager<MaterialReference, Renderer>();
        [SerializeField] private ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer> _blendshapeManager = new ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer>();

        // Cache para evitar alocación en cada acceso a MaterialReferences (interfaz IMaterialReference)
        [NonSerialized] private List<IMaterialReference> _materialReferencesCache;
        [NonSerialized] private int _materialReferencesCacheCount = -1;
        
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
        public List<IMaterialReference> MaterialReferences
        {
            get
            {
                var refs = _materialManager.References;
                int currentCount = refs.Count;
                if (_materialReferencesCache == null || _materialReferencesCacheCount != currentCount)
                {
                    _materialReferencesCache = refs.Cast<IMaterialReference>().ToList();
                    _materialReferencesCacheCount = currentCount;
                }
                return _materialReferencesCache;
            }
        }
        
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
        /// Limpia las referencias inválidas
        /// </summary>
        public void RemoveInvalidReferences()
        {
            _objectManager.RemoveInvalid();
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
            return _objectManager.References.All(r => r.IsValid)
                && _materialManager.References.All(r => r.IsValid)
                && _blendshapeManager.References.All(r => r.IsValid);
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
        /// Elimina todas las referencias de blendshapes de un renderer específico
        /// </summary>
        public void RemoveAllBlendshapeReferences(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;
            _blendshapeManager.RemoveByTarget(renderer);
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
        
        
        
    }
}
