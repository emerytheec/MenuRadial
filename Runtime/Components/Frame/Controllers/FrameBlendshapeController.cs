using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de blendshapes en frames.
    /// </summary>
    public class FrameBlendshapeController
    {
        private readonly FrameData _frameData;

        public FrameBlendshapeController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region Reference Operations

        public int Count => _frameData.BlendshapeReferences?.Count ?? 0;
        public int ValidCount => _frameData.BlendshapeReferences?.Count(b => b != null && b.IsValid) ?? 0;
        public int InvalidCount => Count - ValidCount;
        public List<BlendshapeReference> References => _frameData.BlendshapeReferences;

        // Alias para compatibilidad
        public int BlendshapeCount => Count;
        public int ValidBlendshapeCount => ValidCount;
        public int InvalidBlendshapeCount => InvalidCount;
        public List<BlendshapeReference> BlendshapeReferences => References;

        public void ClearAll() => ClearAllBlendshapes();
        public void ApplyStates() => ApplyBlendshapeStates();
        public void RemoveInvalidReferences() => RemoveInvalidBlendshapeReferences();

        #endregion

        public bool AddBlendshape(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f, float actualValue = -1f)
        {
            if (renderer == null)
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir blendshape: renderer es null");
                return false;
            }

            if (string.IsNullOrEmpty(blendshapeName))
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir blendshape: nombre vacío");
                return false;
            }

            if (renderer.sharedMesh == null)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir blendshape '{blendshapeName}': el renderer '{renderer.name}' no tiene mesh asignado");
                return false;
            }

            int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            if (blendshapeIndex < 0)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir blendshape '{blendshapeName}': no existe en el mesh de '{renderer.name}'");
                return false;
            }

            var existing = _frameData.BlendshapeReferences.FirstOrDefault(b =>
                b?.TargetRenderer == renderer && b.BlendshapeName == blendshapeName);

            if (existing != null)
            {
                existing.Value = value;
                return true;
            }

            // Si no se proporcionó valor base, capturar del mesh actual
            float baseValue = actualValue >= 0f ? actualValue : renderer.GetBlendShapeWeight(blendshapeIndex);
            var newReference = new BlendshapeReference(renderer, blendshapeName, value, baseValue);
            _frameData.BlendshapeReferences.Add(newReference);

            return true;
        }

        public void RemoveBlendshape(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return;

            var toRemove = _frameData.BlendshapeReferences.Where(b =>
                b?.TargetRenderer == renderer && b.BlendshapeName == blendshapeName).ToList();

            foreach (var reference in toRemove)
            {
                _frameData.BlendshapeReferences.Remove(reference);
            }
        }

        public void RemoveAllBlendshapesFromRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;

            var toRemove = _frameData.BlendshapeReferences.Where(b => b?.TargetRenderer == renderer).ToList();

            foreach (var reference in toRemove)
            {
                _frameData.BlendshapeReferences.Remove(reference);
            }
        }

        public void ClearAllBlendshapes()
        {
            _frameData.BlendshapeReferences.Clear();
        }

        public void RemoveInvalidBlendshapeReferences()
        {
            _frameData.BlendshapeReferences.RemoveAll(b => b == null || !b.IsValid);
        }

        public void UpdateAllBlendshapeRendererPaths()
        {
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.TargetRenderer != null))
            {
                blendRef.UpdateHierarchyPath();
            }
        }

        public void CaptureAllBlendshapeValues()
        {
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            blendRef.Value = renderer.GetBlendShapeWeight(blendshapeIndex);
                        }
                    }
                }
            }
        }

        public void ApplyBlendshapeStates()
        {
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            renderer.SetBlendShapeWeight(blendshapeIndex, blendRef.Value);
                        }
                    }
                }
            }
        }

        public List<BlendshapeReference> CaptureCurrentStates()
        {
            var currentStates = new List<BlendshapeReference>();

            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            float currentValue = renderer.GetBlendShapeWeight(blendshapeIndex);
                            currentStates.Add(new BlendshapeReference(renderer, blendRef.BlendshapeName, currentValue));
                        }
                    }
                }
            }

            return currentStates;
        }

        public void RestoreStates(List<BlendshapeReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
                return;

            foreach (var savedState in savedStates.Where(s => s != null && s.IsValid))
            {
                if (savedState != null && !string.IsNullOrEmpty(savedState.BlendshapeName))
                {
                    var renderer = savedState.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(savedState.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            renderer.SetBlendShapeWeight(blendshapeIndex, savedState.Value);
                        }
                    }
                }
            }
        }
    }
}
