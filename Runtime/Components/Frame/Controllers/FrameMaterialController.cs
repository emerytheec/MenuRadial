using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de materiales en frames.
    /// Implementa IMaterialReferenceController para abstracción.
    /// REFACTORIZADO: Extraído de MRAgruparObjetos.cs para responsabilidad única.
    /// </summary>
    public class FrameMaterialController : IMaterialReferenceController
    {
        private readonly FrameData _frameData;

        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameData">Datos del frame para gestionar</param>
        public FrameMaterialController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region IReferenceController Implementation

        public int Count => _frameData.MaterialReferencesData?.Count ?? 0;
        public int ValidCount => _frameData.MaterialReferencesData?.Count(m => m != null && m.IsValid) ?? 0;
        public int InvalidCount => Count - ValidCount;
        public List<MaterialReference> References => _frameData.MaterialReferencesData;

        // Alias para compatibilidad
        public int MaterialCount => Count;
        public int ValidMaterialCount => ValidCount;
        public int InvalidMaterialCount => InvalidCount;
        public List<MaterialReference> MaterialReferences => References;

        public void ClearAll() => ClearAllMaterials();
        public void ApplyStates() => ApplyMaterialStates();
        public void RemoveInvalidReferences() => RemoveInvalidMaterialReferences();

        #endregion
        
        
        /// <summary>
        /// Añade una referencia de material al frame
        /// EXTRAÍDO: De MRAgruparObjetos.AddMaterialReference()
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <param name="alternativeMaterial">Material alternativo</param>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        public bool AddMaterial(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            if (renderer == null)
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir material: Renderer es null");
                return false;
            }

            // Validar que el índice esté en rango
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir material: el renderer '{renderer.name}' no tiene materiales");
                return false;
            }

            if (materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir material: índice {materialIndex} fuera de rango (0-{renderer.sharedMaterials.Length - 1}) en '{renderer.name}'");
                return false;
            }

            // Verificar si ya existe
            var existing = _frameData.MaterialReferencesData.FirstOrDefault(m =>
                m?.TargetRenderer == renderer && m.MaterialIndex == materialIndex);

            if (existing != null)
            {
                if (alternativeMaterial != null)
                {
                    existing.AlternativeMaterial = alternativeMaterial;
                }
                return true; // Se considera éxito porque se actualizó
            }

            // Crear nueva referencia
            var newReference = new MaterialReference(renderer, materialIndex, alternativeMaterial);
            _frameData.MaterialReferencesData.Add(newReference);

            return true;
        }
        
        /// <summary>
        /// Elimina una referencia de material del frame
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveMaterialReference()
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        public void RemoveMaterial(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null) return;
            
            var toRemove = _frameData.MaterialReferencesData.Where(m => 
                m?.TargetRenderer == renderer && m.MaterialIndex == materialIndex).ToList();
            
            foreach (var reference in toRemove)
            {
                _frameData.MaterialReferencesData.Remove(reference);
            }
        }
        
        /// <summary>
        /// Limpia todas las referencias de materiales
        /// EXTRAÍDO: De MRAgruparObjetos.ClearAllMaterials()
        /// </summary>
        public void ClearAllMaterials()
        {
            int count = MaterialCount;
            _frameData.MaterialReferencesData.Clear();
        }
        
        /// <summary>
        /// Elimina las referencias de materiales inválidas
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveInvalidMaterialReferences()
        /// </summary>
        public void RemoveInvalidMaterialReferences()
        {
            var invalidReferences = _frameData.MaterialReferencesData.Where(m => m == null || !m.IsValid).ToList();
            
            foreach (var invalidRef in invalidReferences)
            {
                _frameData.MaterialReferencesData.Remove(invalidRef);
            }
            
            if (invalidReferences.Count > 0)
            {
            }
        }
        
        /// <summary>
        /// Actualiza las referencias originales de todos los materiales
        /// EXTRAÍDO: De MRAgruparObjetos.UpdateAllOriginalMaterials()
        /// </summary>
        public void UpdateAllOriginalMaterials()
        {
            int updatedCount = 0;
            
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.IsValid))
            {
                matRef.UpdateOriginalMaterial();
                updatedCount++;
            }
            
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todos los renderers de materiales
        /// EXTRAÍDO: De MRAgruparObjetos.UpdateAllMaterialRendererPaths()
        /// </summary>
        public void UpdateAllMaterialRendererPaths()
        {
            int updatedCount = 0;
            
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.TargetRenderer != null))
            {
                matRef.UpdateHierarchyPath();
                updatedCount++;
            }
            
        }
        
        
        
        /// <summary>
        /// Aplica los estados de los materiales en la escena
        /// EXTRAÍDO: Parte de MRAgruparObjetos.ApplyCurrentFrame()
        /// </summary>
        public void ApplyMaterialStates()
        {
            int appliedCount = 0;
            
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.IsValid))
            {
                Material materialToApply = matRef.HasAlternativeMaterial ? 
                    matRef.AlternativeMaterial : matRef.OriginalMaterial;
                
                if (materialToApply != null && matRef.TargetRenderer != null)
                {
                    var materials = matRef.TargetRenderer.sharedMaterials;
                    if (matRef.MaterialIndex < materials.Length)
                    {
                        materials[matRef.MaterialIndex] = materialToApply;
                        matRef.TargetRenderer.sharedMaterials = materials;
                        appliedCount++;
                    }
                }
            }
            
        }
        
        /// <summary>
        /// Captura los estados actuales de los materiales en la escena
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <returns>Lista de estados actuales</returns>
        public List<MaterialReference> CaptureCurrentStates()
        {
            var currentStates = new List<MaterialReference>();
            
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.IsValid))
            {
                var renderer = matRef.TargetRenderer;
                var materialIndex = matRef.MaterialIndex;
                
                if (renderer != null && materialIndex < renderer.sharedMaterials.Length)
                {
                    // Capturar el material actual
                    var currentMaterial = renderer.sharedMaterials[materialIndex];
                    var stateCapture = new MaterialReference(renderer, materialIndex, currentMaterial);
                    currentStates.Add(stateCapture);
                }
            }
            
            return currentStates;
        }
        
        /// <summary>
        /// Restaura estados previamente capturados
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <param name="savedStates">Estados a restaurar</param>
        public void RestoreStates(List<MaterialReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
            {
                return;
            }
            
            int restoredCount = 0;
            
            foreach (var savedState in savedStates.Where(s => s != null && s.IsValid))
            {
                var renderer = savedState.TargetRenderer;
                var materialIndex = savedState.MaterialIndex;
                var materialToRestore = savedState.HasAlternativeMaterial ? 
                    savedState.AlternativeMaterial : savedState.OriginalMaterial;
                
                if (renderer != null && materialToRestore != null && materialIndex < renderer.sharedMaterials.Length)
                {
                    var materials = renderer.sharedMaterials;
                    materials[materialIndex] = materialToRestore;
                    renderer.sharedMaterials = materials;
                    restoredCount++;
                }
            }
            
        }
        
        
        
        /// <summary>
        /// Busca una referencia de material específica
        /// NUEVO: Método utilitario para búsquedas
        /// </summary>
        /// <param name="renderer">Renderer a buscar</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <returns>Referencia encontrada o null</returns>
        public MaterialReference FindMaterialReference(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null) return null;
            
            return _frameData.MaterialReferencesData.FirstOrDefault(m => 
                m?.TargetRenderer == renderer && m.MaterialIndex == materialIndex);
        }
        
        /// <summary>
        /// Verifica si un material está en el frame
        /// NUEVO: Método utilitario para verificaciones
        /// </summary>
        /// <param name="renderer">Renderer a verificar</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <returns>True si está en el frame</returns>
        public bool ContainsMaterial(Renderer renderer, int materialIndex = 0)
        {
            return FindMaterialReference(renderer, materialIndex) != null;
        }
        
        /// <summary>
        /// Obtiene todos los materiales con alternativas
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de materiales con alternativas</returns>
        public List<MaterialReference> GetMaterialsWithAlternatives()
        {
            return _frameData.MaterialReferencesData
                .Where(m => m != null && m.IsValid && m.HasAlternativeMaterial)
                .ToList();
        }
        
        /// <summary>
        /// Obtiene todos los materiales sin alternativas
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de materiales sin alternativas</returns>
        public List<MaterialReference> GetMaterialsWithoutAlternatives()
        {
            return _frameData.MaterialReferencesData
                .Where(m => m != null && m.IsValid && !m.HasAlternativeMaterial)
                .ToList();
        }
        
        
    }
}
