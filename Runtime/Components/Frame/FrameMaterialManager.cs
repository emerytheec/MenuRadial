using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Managers;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Manager especializado para la gestión de materiales en frames
    /// VERSIÓN 0.036: REFACTORIZADO - Hereda de BaseReferenceManager<T>
    /// REDUCIDO de 285 líneas a 65 líneas (-77%)
    /// Responsabilidad única: Solo especialización específica de MaterialReference
    /// </summary>
    public class FrameMaterialManager : BaseReferenceManager<MaterialReference, Renderer>
    {
        /// <summary>
        /// Constructor con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        public FrameMaterialManager(FrameData frameData) : base(frameData)
        {
        }
        
        
        /// <summary>
        /// Obtiene el list manager específico para MaterialReference
        /// </summary>
        protected override ReferenceListManager<MaterialReference, Renderer> GetListManager()
        {
            return _frameData.MaterialReferenceListManager;
        }
        
        /// <summary>
        /// Crea una copia del estado actual de una referencia de material
        /// </summary>
        protected override MaterialReference CreateStateCopy(MaterialReference reference)
        {
            if (reference?.TargetRenderer == null) return null;
            
            // Crear una copia del material reference con el material original actual
            return new MaterialReference(reference.TargetRenderer, reference.MaterialIndex);
        }
        
        
        
        /// <summary>
        /// Añade una referencia de material al frame con parámetros específicos
        /// </summary>
        public void AddMaterial(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            if (renderer == null)
            {
                return;
            }
            
            if (materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length)
            {
                return;
            }
            
            var reference = new MaterialReference(renderer, materialIndex, alternativeMaterial);
            Add(reference);
        }
        
        /// <summary>
        /// Elimina una referencia de material específica del frame
        /// </summary>
        public void RemoveMaterial(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null)
            {
                return;
            }
            
            var materialsToRemove = References.Where(m => m.TargetRenderer == renderer && m.MaterialIndex == materialIndex).ToList();
            foreach (var material in materialsToRemove)
            {
                Remove(material);
            }
        }
        
        /// <summary>
        /// Actualiza las referencias originales de todos los materiales
        /// </summary>
        public void UpdateAllOriginalMaterials()
        {
            foreach (var matRef in GetValidReferences())
            {
                matRef.UpdateOriginalMaterial();
            }
        }
        
        /// <summary>
        /// Restaura todos los materiales originales del frame
        /// </summary>
        public void RestoreAllOriginalMaterials()
        {
            var restored = 0;
            foreach (var matRef in GetValidReferences())
            {
                matRef.RestoreOriginalMaterial();
                restored++;
            }
        }
        
        /// <summary>
        /// Obtiene todas las referencias de materiales de un renderer específico
        /// </summary>
        public List<MaterialReference> GetMaterialsForRenderer(Renderer renderer)
        {
            if (renderer == null) return new List<MaterialReference>();
            return FindAllByTarget(renderer);
        }
        
        /// <summary>
        /// Elimina todas las referencias de materiales de un renderer específico
        /// </summary>
        public int RemoveAllMaterialsForRenderer(Renderer renderer)
        {
            if (renderer == null) return 0;
            return RemoveByTarget(renderer);
        }
        
        /// <summary>
        /// Obtiene lista de renderers únicos que tienen materiales en el frame
        /// </summary>
        public List<Renderer> GetUniqueRenderers()
        {
            return GetValidReferences()
                .Select(m => m.TargetRenderer)
                .Distinct()
                .ToList();
        }
        
        
        
        /// <summary>
        /// Validaciones específicas de materiales
        /// </summary>
        protected override ValidationResult ValidateSpecific()
        {
            var result = new ValidationResult();
            
            // Verificar materiales sin material alternativo asignado
            var materialsWithoutAlternative = GetValidReferences()
                .Where(m => !m.HasAlternativeMaterial)
                .Count();
            
            if (materialsWithoutAlternative > 0)
            {
                result.AddChild(ValidationResult.Info($"{materialsWithoutAlternative} materiales usarán el material original"));
            }
            
            // Verificar materiales duplicados (mismo renderer + mismo índice)
            var duplicates = GetValidReferences()
                .GroupBy(m => new { m.TargetRenderer, m.MaterialIndex })
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            
            if (duplicates > 0)
            {
                result.AddChild(ValidationResult.Warning($"Hay {duplicates} materiales duplicados en el frame"));
            }
            
            return result;
        }
        
        
        
        /// <summary>
        /// Alias para compatibilidad: MaterialReferences -> References
        /// </summary>
        public List<MaterialReference> MaterialReferences => References;
        
        /// <summary>
        /// Alias para compatibilidad: ClearAllMaterials -> ClearAll
        /// </summary>
        public void ClearAllMaterials() => ClearAll();
        
        /// <summary>
        /// Alias para compatibilidad: RemoveInvalidMaterialReferences -> RemoveInvalid
        /// </summary>
        public void RemoveInvalidMaterialReferences() => RemoveInvalid();
        
        /// <summary>
        /// Alias para compatibilidad: UpdateAllMaterialRendererPaths -> UpdateAllHierarchyPaths
        /// </summary>
        public void UpdateAllMaterialRendererPaths() => UpdateAllHierarchyPaths();
        
        /// <summary>
        /// Alias para compatibilidad: GetMaterialCount -> Count
        /// </summary>
        public int GetMaterialCount() => Count;
        
        /// <summary>
        /// Alias para compatibilidad: GetValidMaterialCount -> ValidCount
        /// </summary>
        public int GetValidMaterialCount() => ValidCount;
        
        /// <summary>
        /// Alias para compatibilidad: GetInvalidMaterialCount -> InvalidCount
        /// </summary>
        public int GetInvalidMaterialCount() => InvalidCount;
        
        /// <summary>
        /// Alias para compatibilidad: ApplyMaterialStates -> ApplyAllStates
        /// </summary>
        public void ApplyMaterialStates() => ApplyAllStates();
        
        /// <summary>
        /// Alias para compatibilidad: CaptureCurrentStates -> CaptureCurrentStates (ya existe en base)
        /// </summary>
        public List<MaterialReference> CaptureCurrentStates() => CaptureCurrentStates();
        
        /// <summary>
        /// Alias para compatibilidad: RestoreStates -> RestoreStates (ya existe en base)
        /// </summary>
        public void RestoreStates(List<MaterialReference> capturedStates) => RestoreStates(capturedStates);
        
        /// <summary>
        /// Alias para compatibilidad: ValidateMaterials -> ValidateAll
        /// </summary>
        public ValidationResult ValidateMaterials() => ValidateAll();
        
        
        
        
        /// <summary>
        /// Número total de materiales (alias para Count)
        /// </summary>
        public int MaterialCount => Count;
        
        /// <summary>
        /// Número de materiales válidos (alias para ValidCount)
        /// </summary>
        public int ValidMaterialCount => ValidCount;
        
        /// <summary>
        /// Número de materiales inválidos (alias para InvalidCount)
        /// </summary>
        public int InvalidMaterialCount => InvalidCount;
        
    }
}
