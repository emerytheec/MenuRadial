using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Implementación de IMaterialReference usando ReferenceBase
    /// Refactorizada para eliminar código duplicado de rutas jerárquicas
    /// </summary>
    [Serializable]
    public class MaterialReference : ReferenceBase<Renderer>, IMaterialReference
    {
        [SerializeField] private int _materialIndex = 0;
        [SerializeField] private Material _alternativeMaterial;
        [SerializeField] private Material _originalMaterial;
        
        /// <summary>
        /// Renderer objetivo donde se aplicará el material
        /// </summary>
        public Renderer TargetRenderer 
        { 
            get => Target; 
            set 
            { 
                Target = value;
                UpdateOriginalMaterial();
            } 
        }
        
        /// <summary>
        /// Índice del material dentro del array de materiales del renderer
        /// </summary>
        public int MaterialIndex 
        { 
            get => _materialIndex; 
            set 
            { 
                _materialIndex = Mathf.Max(0, value);
                UpdateOriginalMaterial();
            } 
        }
        
        /// <summary>
        /// Material alternativo a aplicar. Si es null, usa el material original
        /// </summary>
        public Material AlternativeMaterial 
        { 
            get => _alternativeMaterial; 
            set => _alternativeMaterial = value; 
        }
        
        /// <summary>
        /// Material original del renderer (para restauración)
        /// </summary>
        public Material OriginalMaterial => _originalMaterial;
        
        /// <summary>
        /// Ruta jerárquica del renderer  (usa base class)
        /// </summary>
        public string RendererPath => HierarchyPath;
        
        /// <summary>
        /// Indica si la referencia es válida (override con validación específica)
        /// </summary>
        public override bool IsValid => Target != null && 
                                       _materialIndex >= 0 && 
                                       _materialIndex < Target.sharedMaterials.Length;
        
        /// <summary>
        /// Indica si tiene un material alternativo asignado
        /// </summary>
        public bool HasAlternativeMaterial => _alternativeMaterial != null;
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MaterialReference() : base()
        {
        }
        
        /// <summary>
        /// Constructor con renderer y índice
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        public MaterialReference(Renderer renderer, int materialIndex = 0) : base(renderer)
        {
            _materialIndex = Mathf.Max(0, materialIndex);
            UpdateOriginalMaterial();
        }
        
        /// <summary>
        /// Constructor completo
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <param name="alternativeMaterial">Material alternativo</param>
        public MaterialReference(Renderer renderer, int materialIndex, Material alternativeMaterial) : base(renderer)
        {
            _materialIndex = Mathf.Max(0, materialIndex);
            _alternativeMaterial = alternativeMaterial;
            UpdateOriginalMaterial();
        }
        
        /// <summary>
        /// Aplica el material alternativo al renderer
        /// Implementación del método abstracto Apply()
        /// </summary>
        public override void Apply()
        {
            ApplyMaterial();
        }
        
        /// <summary>
        /// Aplica el material alternativo al renderer
        /// </summary>
        public void ApplyMaterial()
        {
            if (!IsValid)
            {
                return;
            }
            
            // Si no hay material alternativo, no hacer nada (mantener original)
            if (!HasAlternativeMaterial)
            {
                return;
            }
            
            // Aplicar material alternativo usando sharedMaterials para evitar leaks en Edit Mode
            var materials = Target.sharedMaterials;
            materials[_materialIndex] = _alternativeMaterial;
            Target.sharedMaterials = materials;
            
        }
        
        /// <summary>
        /// Restaura el material original al renderer
        /// </summary>
        public void RestoreOriginalMaterial()
        {
            if (!IsValid || _originalMaterial == null)
            {
                return;
            }
            
            var materials = Target.sharedMaterials;
            materials[_materialIndex] = _originalMaterial;
            Target.sharedMaterials = materials;
            
        }
        
        /// <summary>
        /// Actualiza la referencia al material original
        /// </summary>
        public void UpdateOriginalMaterial()
        {
            if (IsValid)
            {
                _originalMaterial = Target.sharedMaterials[_materialIndex];
            }
        }
        
        /// <summary>
        /// Obtiene el material actual aplicado en el renderer
        /// </summary>
        /// <returns>Material actualmente aplicado</returns>
        public Material GetCurrentMaterial()
        {
            if (IsValid)
            {
                return Target.sharedMaterials[_materialIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Captura el estado actual del material desde el renderer
        /// Implementación del método abstracto CaptureCurrentState()
        /// </summary>
        public override void CaptureCurrentState()
        {
            if (IsValid)
            {
                var currentMaterial = Target.sharedMaterials[_materialIndex];
                
                // Si el material actual es diferente al original y al alternativo,
                // podríamos capturarlo como nuevo alternativo
                if (currentMaterial != _originalMaterial && currentMaterial != _alternativeMaterial)
                {
                    _alternativeMaterial = currentMaterial;
                }
                
                // Actualizar referencia original si es necesario
                if (_originalMaterial == null)
                {
                    UpdateOriginalMaterial();
                }
            }
        }
        
        /// <summary>
        /// Representación como string 
        /// </summary>
        /// <returns>String descriptivo del material reference</returns>
        public override string ToString()
        {
            var rendererName = Target != null ? Target.name : "[Missing]";
            var altMaterialName = _alternativeMaterial != null ? _alternativeMaterial.name : "[None]";
            var origMaterialName = _originalMaterial != null ? _originalMaterial.name : "[None]";
            
            return "MaterialRef: " + rendererName + "[" + _materialIndex + "] - " + origMaterialName + " → " + altMaterialName + " (" + (IsValid ? "Valid" : "Invalid") + ")";
        }
    }
}
