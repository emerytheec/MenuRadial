using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Editor.Components.Radial
{
    /// <summary>
    /// Gestor especializado para la previsualización de frames en el editor
    /// Responsabilidad única: Manejo de estados de preview, restauración y aplicación de frames
    /// VERSIÓN 0.002: Soporte especial para animaciones On/Off con un solo frame
    /// </summary>
    public class MRUnificarObjetosPreviewManager
    {
        
        private readonly MRUnificarObjetos _target;
        
        
        
        public MRUnificarObjetosPreviewManager(MRUnificarObjetos target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }
        
        
        
        /// <summary>
        /// Aplica la previsualización del frame activo en la escena
        /// NUEVO: Detecta automáticamente animaciones On/Off y maneja el estado Off
        /// </summary>
        public void ApplyFramePreview()
        {
            if (_target.FrameCount == 0)
                return;
            
            // NUEVO: Lógica especial para animaciones On/Off (1 frame)
            if (_target.FrameCount == 1)
            {
                ApplyOnOffFramePreview();
                return;
            }
            
            // Lógica original para múltiples frames
            if (_target.ActiveFrame == null)
                return;
                
            // SOLUCIÓN DIRECTA: Restaurar todos los objetos a su estado original
            // y luego aplicar solo el frame activo
            RestoreAllObjectsToOriginalState();
            
            // Aplicar directamente el frame activo
            _target.ActiveFrame.ApplyCurrentFrame();
            
            // Marcar la escena como modificada para que Unity actualice la vista
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                
            // Forzar repaint de la Scene View
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// NUEVO: Aplica previsualización especial para animaciones On/Off (1 frame)
        /// ActiveFrameIndex 0 = Estado OFF (todo apagado)
        /// ActiveFrameIndex 1+ = Estado ON (frame activo)
        /// </summary>
        private void ApplyOnOffFramePreview()
        {
            if (_target.ActiveFrameIndex == 0)
            {
                // Estado OFF: Restaurar todo a estado apagado/neutral
                RestoreAllObjectsToOriginalState();
            }
            else
            {
                // Estado ON: Aplicar el único frame disponible
                RestoreAllObjectsToOriginalState();
                
                // Verificar que el frame existe antes de aplicarlo
                if (_target.FrameObjects.Count > 0 && _target.FrameObjects[0] != null)
                {
                    _target.FrameObjects[0].ApplyCurrentFrame();
                }
                else
                {
                }
            }
            
            // Marcar la escena como modificada
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                
            // Forzar repaint de la Scene View
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// Restaura todos los objetos, materiales y blendshapes de todos los frames a su estado original
        /// </summary>
        public void RestoreAllObjectsToOriginalState()
        {
            if (_target.FrameObjects == null) return;
            
            // Restaurar objetos (GameObjects activos/inactivos)
            RestoreAllGameObjectStates();
            
            // Restaurar materiales a sus estados originales
            RestoreAllMaterialStates();
            
            // Restaurar blendshapes a sus valores originales
            RestoreAllBlendshapeStates();
        }
        
        /// <summary>
        /// Cancela las previsualizations activas en todos los frames
        /// </summary>
        public void CancelAllFramePreviews()
        {
            if (_target.FrameObjects == null) return;
            
            int canceledCount = 0;
            for (int i = 0; i < _target.FrameObjects.Count; i++)
            {
                var frame = _target.FrameObjects[i];
                if (frame != null && frame.IsPreviewActive)
                {
                    frame.CancelPreview();
                    canceledCount++;
                }
            }
            
            if (canceledCount > 0)
            {
            }
        }
        
        
        
        /// <summary>
        /// Restaura todos los GameObjects a estado desactivado
        /// </summary>
        private void RestoreAllGameObjectStates()
        {
            var allGameObjects = new HashSet<GameObject>();
            
            foreach (var frame in _target.FrameObjects)
            {
                if (frame?.ObjectReferences == null) continue;
                
                foreach (var objRef in frame.ObjectReferences)
                {
                    if (objRef?.GameObject != null)
                    {
                        allGameObjects.Add(objRef.GameObject);
                    }
                }
            }
            
            // Desactivar todos los objetos (estado neutro)
            foreach (var go in allGameObjects)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }
        }
        
        
        
        /// <summary>
        /// Restaura todos los materiales a sus estados originales
        /// </summary>
        private void RestoreAllMaterialStates()
        {
            var allRenderers = new Dictionary<Renderer, Dictionary<int, Material>>();
            
            // Recopilar todos los renderers y sus materiales originales
            foreach (var frame in _target.FrameObjects)
            {
                if (frame?.MaterialReferences == null) continue;
                
                foreach (var matRef in frame.MaterialReferences)
                {
                    if (matRef?.TargetRenderer == null) continue;
                    
                    var renderer = matRef.TargetRenderer;
                    var materialIndex = matRef.MaterialIndex;
                    var originalMaterial = matRef.OriginalMaterial;
                    
                    if (!allRenderers.ContainsKey(renderer))
                    {
                        allRenderers[renderer] = new Dictionary<int, Material>();
                    }
                    
                    // Solo guardar el original si no lo tenemos ya
                    if (!allRenderers[renderer].ContainsKey(materialIndex) && originalMaterial != null)
                    {
                        allRenderers[renderer][materialIndex] = originalMaterial;
                    }
                }
            }
            
            // Restaurar materiales originales usando sharedMaterials para evitar leaks
            int restoredMaterials = 0;
            foreach (var rendererPair in allRenderers)
            {
                var renderer = rendererPair.Key;
                var materials = rendererPair.Value;
                
                if (renderer == null) continue;
                
                foreach (var materialPair in materials)
                {
                    var index = materialPair.Key;
                    var originalMaterial = materialPair.Value;
                    
                    if (originalMaterial != null && index < renderer.sharedMaterials.Length)
                    {
                        var currentMaterials = renderer.sharedMaterials;
                        currentMaterials[index] = originalMaterial;
                        renderer.sharedMaterials = currentMaterials;
                        restoredMaterials++;
                    }
                }
            }
            
            if (restoredMaterials > 0)
            {
            }
        }
        
        
        
        /// <summary>
        /// Restaura todos los blendshapes a valor 0 (estado neutro)
        /// </summary>
        private void RestoreAllBlendshapeStates()
        {
            var allBlendshapes = new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
            
            // Recopilar todos los blendshapes de todos los frames
            foreach (var frame in _target.FrameObjects)
            {
                if (frame?.BlendshapeReferences == null) continue;
                
                foreach (var blendRef in frame.BlendshapeReferences)
                {
                    if (blendRef?.TargetRenderer == null || string.IsNullOrEmpty(blendRef.BlendshapeName)) continue;
                    
                    var renderer = blendRef.TargetRenderer;
                    var blendshapeName = blendRef.BlendshapeName;
                    
                    if (!allBlendshapes.ContainsKey(renderer))
                    {
                        allBlendshapes[renderer] = new HashSet<string>();
                    }
                    
                    allBlendshapes[renderer].Add(blendshapeName);
                }
            }
            
            // Restaurar todos los blendshapes a valor 0 (estado neutro)
            int restoredBlendshapes = 0;
            foreach (var rendererPair in allBlendshapes)
            {
                var renderer = rendererPair.Key;
                var blendshapeNames = rendererPair.Value;
                
                if (renderer?.sharedMesh == null) continue;
                
                foreach (var blendshapeName in blendshapeNames)
                {
                    var blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
                    if (blendshapeIndex >= 0)
                    {
                        renderer.SetBlendShapeWeight(blendshapeIndex, 0f); // Estado neutro = 0
                        restoredBlendshapes++;
                    }
                }
            }
            
            if (restoredBlendshapes > 0)
            {
            }
        }
        
    }
}
