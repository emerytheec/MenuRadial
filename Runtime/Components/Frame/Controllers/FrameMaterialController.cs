using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de materiales en frames.
    /// </summary>
    public class FrameMaterialController
    {
        private readonly FrameData _frameData;

        public FrameMaterialController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region Reference Operations

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

        public bool AddMaterial(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            if (renderer == null)
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir material: Renderer es null");
                return false;
            }

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

            var existing = _frameData.MaterialReferencesData.FirstOrDefault(m =>
                m?.TargetRenderer == renderer && m.MaterialIndex == materialIndex);

            if (existing != null)
            {
                if (alternativeMaterial != null)
                {
                    existing.AlternativeMaterial = alternativeMaterial;
                }
                return true;
            }

            var newReference = new MaterialReference(renderer, materialIndex, alternativeMaterial);
            _frameData.MaterialReferencesData.Add(newReference);

            return true;
        }

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

        public void ClearAllMaterials()
        {
            _frameData.MaterialReferencesData.Clear();
        }

        public void RemoveInvalidMaterialReferences()
        {
            _frameData.MaterialReferencesData.RemoveAll(m => m == null || !m.IsValid);
        }

        public void UpdateAllOriginalMaterials()
        {
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.IsValid))
            {
                matRef.UpdateOriginalMaterial();
            }
        }

        public void UpdateAllMaterialRendererPaths()
        {
            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.TargetRenderer != null))
            {
                matRef.UpdateHierarchyPath();
            }
        }

        public void ApplyMaterialStates()
        {
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
                    }
                }
            }
        }

        public List<MaterialReference> CaptureCurrentStates()
        {
            var currentStates = new List<MaterialReference>();

            foreach (var matRef in _frameData.MaterialReferencesData.Where(m => m != null && m.IsValid))
            {
                var renderer = matRef.TargetRenderer;
                var materialIndex = matRef.MaterialIndex;

                if (renderer != null && materialIndex < renderer.sharedMaterials.Length)
                {
                    var currentMaterial = renderer.sharedMaterials[materialIndex];
                    currentStates.Add(new MaterialReference(renderer, materialIndex, currentMaterial));
                }
            }

            return currentStates;
        }

        public void RestoreStates(List<MaterialReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
                return;

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
                }
            }
        }
    }
}
