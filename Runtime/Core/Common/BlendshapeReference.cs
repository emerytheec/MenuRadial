using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Referencia a un blendshape específico de un SkinnedMeshRenderer
    /// Refactorizada para usar ReferenceBase - elimina código duplicado de rutas jerárquicas
    /// </summary>
    [Serializable]
    public class BlendshapeReference : ReferenceBase<SkinnedMeshRenderer>
    {
        [SerializeField] private string _blendshapeName;
        [SerializeField] private float _value;
        [SerializeField] private float _actualValue;
        [SerializeField] private bool _hasBaseValue;
        
        /// <summary>
        /// SkinnedMeshRenderer objetivo que contiene el blendshape
        /// </summary>
        public SkinnedMeshRenderer TargetRenderer 
        { 
            get => Target; 
            set => Target = value; 
        }
        
        /// <summary>
        /// Nombre del blendshape (se guarda por nombre para mayor estabilidad)
        /// </summary>
        public string BlendshapeName 
        { 
            get => _blendshapeName; 
            set => _blendshapeName = value; 
        }
        
        /// <summary>
        /// Valor que debe tener el blendshape cuando este frame está activo (0-100)
        /// </summary>
        public float Value 
        { 
            get => _value; 
            set => _value = Mathf.Clamp(value, 0f, 100f); 
        }
        
        /// <summary>
        /// Valor base original del blendshape (0-100)
        /// Capturado al momento de añadir la referencia al frame
        /// </summary>
        public float ActualValue
        {
            get => _actualValue;
            set => _actualValue = Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>
        /// Indica si el valor base fue capturado explícitamente.
        /// false para datos serializados antiguos (pre-fix), true para nuevos.
        /// </summary>
        public bool HasBaseValue => _hasBaseValue;
        
        /// <summary>
        /// Ruta jerárquica del renderer (usa base class)
        /// </summary>
        public string RendererPath => HierarchyPath;
        
        /// <summary>
        /// Indica si la referencia es válida (override con validación específica)
        /// </summary>
        public override bool IsValid
        {
            get
            {
                if (Target == null || string.IsNullOrEmpty(_blendshapeName))
                    return false;
                
                // Verificar que el mesh tiene el blendshape especificado
                var mesh = Target.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0)
                    return false;
                
                return GetBlendshapeIndex() >= 0;
            }
        }
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BlendshapeReference() : base()
        {
        }
        
        /// <summary>
        /// Constructor con parámetros
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        /// <param name="value">Valor inicial del blendshape</param>
        /// <param name="actualValue">Valor actual/por defecto del blendshape</param>
        public BlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f, float actualValue = 0f) : base(renderer)
        {
            _blendshapeName = blendshapeName;
            _value = Mathf.Clamp(value, 0f, 100f);
            _actualValue = Mathf.Clamp(actualValue, 0f, 100f);
            _hasBaseValue = true;
        }
        
        /// <summary>
        /// Obtiene el índice del blendshape en el mesh
        /// </summary>
        /// <returns>Índice del blendshape o -1 si no se encuentra</returns>
        public int GetBlendshapeIndex()
        {
            if (!IsRendererValid())
                return -1;
            
            var mesh = Target.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i) == _blendshapeName)
                    return i;
            }
            
            return -1;
        }
        
        /// <summary>
        /// Aplica el valor del blendshape al renderer
        /// Implementación del método abstracto Apply()
        /// </summary>
        public override void Apply()
        {
            ApplyBlendshape();
        }
        
        /// <summary>
        /// Aplica el valor del blendshape al renderer
        /// </summary>
        public void ApplyBlendshape()
        {
            if (!IsValid)
            {
                return;
            }
            
            int blendshapeIndex = GetBlendshapeIndex();
            if (blendshapeIndex >= 0)
            {
                Target.SetBlendShapeWeight(blendshapeIndex, _value);
            }
        }
        
        /// <summary>
        /// Obtiene el valor actual del blendshape desde el renderer
        /// </summary>
        /// <returns>Valor actual del blendshape o 0 si no es válido</returns>
        public float GetCurrentValue()
        {
            if (!IsValid)
                return 0f;
            
            int blendshapeIndex = GetBlendshapeIndex();
            if (blendshapeIndex >= 0)
            {
                return Target.GetBlendShapeWeight(blendshapeIndex);
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Captura el valor actual del blendshape desde el renderer
        /// Implementación del método abstracto CaptureCurrentState()
        /// </summary>
        public override void CaptureCurrentState()
        {
            if (IsValid)
            {
                _value = GetCurrentValue();
            }
        }
        
        /// <summary>
        /// Verifica si el renderer es válido (existe y es SkinnedMeshRenderer)
        /// </summary>
        /// <returns>True si el renderer es válido</returns>
        private bool IsRendererValid()
        {
            return Target != null && Target.sharedMesh != null;
        }
        
        /// <summary>
        /// Representación en string
        /// </summary>
        /// <returns>Representación textual</returns>
        public override string ToString()
        {
            string rendererName = Target != null ? Target.name : "(Perdido)";
            return "BlendshapeRef: " + rendererName + "." + _blendshapeName + " = " + _value + " (" + (IsValid ? "Valid" : "Invalid") + ")"; 
        }
    }
}
