using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Managers;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Manager especializado para la gestión de blendshapes en frames
    /// VERSIÓN 0.036: REFACTORIZADO - Hereda de BaseReferenceManager<T>
    /// REDUCIDO de 345 líneas a 75 líneas (-78%)
    /// Responsabilidad única: Solo especialización específica de BlendshapeReference
    /// </summary>
    public class FrameBlendshapeManager : BaseReferenceManager<BlendshapeReference, SkinnedMeshRenderer>
    {
        /// <summary>
        /// Constructor con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        public FrameBlendshapeManager(FrameData frameData) : base(frameData)
        {
        }
        
        
        /// <summary>
        /// Obtiene el list manager específico para BlendshapeReference
        /// </summary>
        protected override ReferenceListManager<BlendshapeReference, SkinnedMeshRenderer> GetListManager()
        {
            return _frameData.BlendshapeReferenceListManager;
        }
        
        /// <summary>
        /// Crea una copia del estado actual de una referencia de blendshape
        /// </summary>
        protected override BlendshapeReference CreateStateCopy(BlendshapeReference reference)
        {
            if (reference?.TargetRenderer == null) return null;
            
            // Capturar el valor actual del blendshape
            float currentValue = reference.GetCurrentValue();
            return new BlendshapeReference(reference.TargetRenderer, reference.BlendshapeName, currentValue);
        }
        
        
        
        /// <summary>
        /// Añade una referencia de blendshape al frame con validación específica
        /// </summary>
        public void AddBlendshape(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f)
        {
            if (renderer == null)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(blendshapeName))
            {
                return;
            }
            
            // Verificar que el blendshape existe en el mesh
            if (!IsBlendshapeAvailable(renderer, blendshapeName))
            {
                return;
            }
            
            var reference = new BlendshapeReference(renderer, blendshapeName, Mathf.Clamp(value, 0f, 100f));
            Add(reference);
        }
        
        /// <summary>
        /// Elimina una referencia de blendshape específica del frame
        /// </summary>
        public void RemoveBlendshape(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName))
            {
                return;
            }
            
            var blendshapesToRemove = References.Where(b => b.TargetRenderer == renderer && b.BlendshapeName == blendshapeName).ToList();
            foreach (var blendshape in blendshapesToRemove)
            {
                Remove(blendshape);
            }
        }
        
        /// <summary>
        /// Elimina todas las referencias de blendshapes de un renderer específico
        /// </summary>
        public int RemoveAllBlendshapesFromRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
            {
                return 0;
            }
            
            return RemoveByTarget(renderer);
        }
        
        /// <summary>
        /// Captura los valores actuales de todos los blendshapes desde los renderers
        /// </summary>
        public void CaptureAllBlendshapeValues()
        {
            foreach (var blendRef in GetValidReferences())
            {
                blendRef.CaptureCurrentValue();
            }
        }
        
        /// <summary>
        /// Restaura todos los blendshapes a sus valores por defecto (0)
        /// </summary>
        public void ResetAllBlendshapesToZero()
        {
            var reset = 0;
            foreach (var blendRef in GetValidReferences())
            {
                if (SetBlendshapeValue(blendRef.TargetRenderer, blendRef.BlendshapeName, 0f))
                {
                    reset++;
                }
            }
        }
        
        /// <summary>
        /// Obtiene todas las referencias de blendshapes de un renderer específico
        /// </summary>
        public List<BlendshapeReference> GetBlendshapesForRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return new List<BlendshapeReference>();
            return FindAllByTarget(renderer);
        }
        
        /// <summary>
        /// Obtiene lista de renderers únicos que tienen blendshapes en el frame
        /// </summary>
        public List<SkinnedMeshRenderer> GetUniqueRenderers()
        {
            return GetValidReferences()
                .Select(b => b.TargetRenderer)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Obtiene todos los blendshapes disponibles de un SkinnedMeshRenderer
        /// </summary>
        public List<string> GetAvailableBlendshapes(SkinnedMeshRenderer renderer)
        {
            return FrameData.GetAvailableBlendshapes(renderer);
        }
        
        /// <summary>
        /// Verifica si un blendshape específico está disponible en un renderer
        /// </summary>
        public bool IsBlendshapeAvailable(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer?.sharedMesh == null || string.IsNullOrEmpty(blendshapeName)) return false;
            return renderer.sharedMesh.GetBlendShapeIndex(blendshapeName) >= 0;
        }
        
        /// <summary>
        /// Obtiene el valor actual de un blendshape específico
        /// </summary>
        public float GetCurrentBlendshapeValue(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer?.sharedMesh == null || string.IsNullOrEmpty(blendshapeName)) return 0f;
            
            var index = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            return index >= 0 ? renderer.GetBlendShapeWeight(index) : 0f;
        }
        
        /// <summary>
        /// Establece el valor de un blendshape específico
        /// </summary>
        public bool SetBlendshapeValue(SkinnedMeshRenderer renderer, string blendshapeName, float value)
        {
            if (renderer?.sharedMesh == null || string.IsNullOrEmpty(blendshapeName)) return false;
            
            var index = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            if (index >= 0)
            {
                renderer.SetBlendShapeWeight(index, Mathf.Clamp(value, 0f, 100f));
                return true;
            }
            return false;
        }
        
        
        
        /// <summary>
        /// Validaciones específicas de blendshapes
        /// </summary>
        protected override ValidationResult ValidateSpecific()
        {
            var result = new ValidationResult();
            
            // Verificar blendshapes con valores fuera de rango (0-100)
            var outOfRange = GetValidReferences()
                .Where(b => b.Value < 0f || b.Value > 100f)
                .Count();
            
            if (outOfRange > 0)
            {
                result.AddChild(ValidationResult.Warning($"Hay {outOfRange} blendshapes con valores fuera del rango 0-100"));
            }
            
            // Verificar blendshapes duplicados (mismo renderer + mismo nombre)
            var duplicates = GetValidReferences()
                .GroupBy(b => new { b.TargetRenderer, b.BlendshapeName })
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            
            if (duplicates > 0)
            {
                result.AddChild(ValidationResult.Warning($"Hay {duplicates} blendshapes duplicados en el frame"));
            }
            
            // Verificar blendshapes que no existen en el mesh
            var nonExistent = GetValidReferences()
                .Where(b => !IsBlendshapeAvailable(b.TargetRenderer, b.BlendshapeName))
                .Count();
            
            if (nonExistent > 0)
            {
                result.AddChild(ValidationResult.Error($"Hay {nonExistent} blendshapes que no existen en sus meshes"));
            }
            
            return result;
        }
        
        
        
        /// <summary>
        /// Alias para compatibilidad: BlendshapeReferences -> References
        /// </summary>
        public List<BlendshapeReference> BlendshapeReferences => References;
        
        /// <summary>
        /// Alias para compatibilidad: ClearAllBlendshapes -> ClearAll
        /// </summary>
        public void ClearAllBlendshapes() => ClearAll();
        
        /// <summary>
        /// Alias para compatibilidad: RemoveInvalidBlendshapeReferences -> RemoveInvalid
        /// </summary>
        public void RemoveInvalidBlendshapeReferences() => RemoveInvalid();
        
        /// <summary>
        /// Alias para compatibilidad: UpdateAllBlendshapeRendererPaths -> UpdateAllHierarchyPaths
        /// </summary>
        public void UpdateAllBlendshapeRendererPaths() => UpdateAllHierarchyPaths();
        
        /// <summary>
        /// Alias para compatibilidad: GetBlendshapeCount -> Count
        /// </summary>
        public int GetBlendshapeCount() => Count;
        
        /// <summary>
        /// Alias para compatibilidad: GetValidBlendshapeCount -> ValidCount
        /// </summary>
        public int GetValidBlendshapeCount() => ValidCount;
        
        /// <summary>
        /// Alias para compatibilidad: GetInvalidBlendshapeCount -> InvalidCount
        /// </summary>
        public int GetInvalidBlendshapeCount() => InvalidCount;
        
        /// <summary>
        /// Alias para compatibilidad: ApplyBlendshapeStates -> ApplyAllStates
        /// </summary>
        public void ApplyBlendshapeStates() => ApplyAllStates();
        
        /// <summary>
        /// Alias para compatibilidad: CaptureCurrentStates -> CaptureCurrentStates (ya existe en base)
        /// </summary>
        public List<BlendshapeReference> CaptureCurrentStates() => CaptureCurrentStates();
        
        /// <summary>
        /// Alias para compatibilidad: RestoreStates -> RestoreStates (ya existe en base)
        /// </summary>
        public void RestoreStates(List<BlendshapeReference> capturedStates) => RestoreStates(capturedStates);
        
        /// <summary>
        /// Alias para compatibilidad: ValidateBlendshapes -> ValidateAll
        /// </summary>
        public ValidationResult ValidateBlendshapes() => ValidateAll();
        
        
        
        
        /// <summary>
        /// Número total de blendshapes (alias para Count)
        /// </summary>
        public int BlendshapeCount => Count;
        
        /// <summary>
        /// Número de blendshapes válidos (alias para ValidCount)
        /// </summary>
        public int ValidBlendshapeCount => ValidCount;
        
        /// <summary>
        /// Número de blendshapes inválidos (alias para InvalidCount)
        /// </summary>
        public int InvalidBlendshapeCount => InvalidCount;
        
    }
}
