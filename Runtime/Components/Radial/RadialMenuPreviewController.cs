using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Controlador especializado para la lógica de preview del menú radial
    /// Extraído de MRUnificarObjetos para cumplir con SRP
    /// </summary>
    public class RadialMenuPreviewController
    {
        // Campos privados
        
        private readonly List<MRAgruparObjetos> _frames;
        private readonly Func<int> _getActiveFrameIndex;
        private readonly Func<MRAgruparObjetos> _getActiveFrame;
        
        // Estados de preview para restauración
        private Dictionary<GameObject, bool> _originalObjectStates = new Dictionary<GameObject, bool>();
        private Dictionary<SkinnedMeshRenderer, Dictionary<string, float>> _originalBlendshapeValues = new Dictionary<SkinnedMeshRenderer, Dictionary<string, float>>();
        private Dictionary<Renderer, Dictionary<int, Material>> _originalMaterialStates = new Dictionary<Renderer, Dictionary<int, Material>>();
        private bool _isPreviewActive = false;
        
        // Constructor
        
        /// <summary>
        /// Inicializa el controlador de preview
        /// </summary>
        /// <param name="frames">Lista de frames del menú</param>
        /// <param name="getActiveFrameIndex">Función para obtener índice activo</param>
        /// <param name="getActiveFrame">Función para obtener frame activo</param>
        public RadialMenuPreviewController(List<MRAgruparObjetos> frames, 
                                         Func<int> getActiveFrameIndex, 
                                         Func<MRAgruparObjetos> getActiveFrame)
        {
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            _getActiveFrameIndex = getActiveFrameIndex ?? throw new ArgumentNullException(nameof(getActiveFrameIndex));
            _getActiveFrame = getActiveFrame ?? throw new ArgumentNullException(nameof(getActiveFrame));
        }
        
        // Propiedades públicas
        
        /// <summary>
        /// Indica si el sistema de previsualización está activo
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        /// <summary>
        /// Obtiene el tipo de previsualización para este componente
        /// </summary>
        /// <returns>Tipo de preview basado en el número de frames</returns>
        public string GetPreviewType()
        {
            int frameCount = _frames?.Count(f => f != null) ?? 0;
            return frameCount switch
            {
                0 => "None",
                1 => "Toggle",
                2 => "Toggle", 
                _ => "Slider"
            };
        }
        
        // Métodos públicos
        
        /// <summary>
        /// Activa el sistema de previsualización
        /// </summary>
        public void ActivatePreview()
        {
            if (!_isPreviewActive)
            {
                StoreOriginalStates();
                _isPreviewActive = true;
            }
            ApplyCurrentFrame();
        }
        
        /// <summary>
        /// Desactiva el sistema de previsualización y restaura el estado original
        /// </summary>
        public void DeactivatePreview()
        {
            if (_isPreviewActive)
            {
                RestoreOriginalStates();
                _isPreviewActive = false;
            }
        }
        
        /// <summary>
        /// Alterna el estado de preview (activar/desactivar)
        /// </summary>
        public void TogglePreview()
        {
            if (_isPreviewActive)
                DeactivatePreview();
            else
                ActivatePreview();
        }
        
        /// <summary>
        /// Establece un valor específico para previews lineales
        /// </summary>
        /// <param name="normalizedValue">Valor entre 0 y 1</param>
        /// <param name="setFrameIndex">Acción para establecer el índice del frame</param>
        public void SetPreviewValue(float normalizedValue, System.Action<int> setFrameIndex)
        {
            int frameCount = _frames?.Count(f => f != null) ?? 0;
            if (frameCount <= 1) return;
            
            int targetIndex = Mathf.RoundToInt(normalizedValue * (frameCount - 1));
            setFrameIndex?.Invoke(targetIndex);
            
            if (_isPreviewActive)
                ApplyCurrentFrame();
        }
        
        
        /// <summary>
        /// Aplica inmediatamente el frame activo seleccionado
        /// CORREGIDO v2: Desactiva todos los objetos antes de aplicar el nuevo frame
        /// </summary>
        public void ApplyCurrentFrame()
        {
            // Asegurar que los estados estén almacenados antes de modificar
            if (_originalObjectStates.Count == 0)
            {
                StoreOriginalStates();
            }

            // Contar frames válidos para detectar OnOff
            int validFrameCount = _frames?.Count(f => f != null) ?? 0;

            // Caso especial: OnOff (1 frame)
            if (validFrameCount == 1)
            {
                ApplyOnOffFrame();
                return;
            }

            // Caso normal: múltiples frames
            var activeFrame = _getActiveFrame?.Invoke();
            if (activeFrame != null)
            {
                // Desactivar todos los objetos referenciados primero (evita acumulación)
                DeactivateAllReferencedObjects();
                // Luego aplicar el frame activo
                activeFrame.ApplyCurrentFrame();
            }

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
#endif
        }

        /// <summary>
        /// Aplica lógica especial para animaciones OnOff (1 frame)
        /// ActiveFrameIndex 0 = Estado OFF (restaurar originales)
        /// ActiveFrameIndex 1+ = Estado ON (aplicar frame)
        /// CORREGIDO v2: OFF restaura originales, ON desactiva todo y aplica frame
        /// </summary>
        private void ApplyOnOffFrame()
        {
            int activeIndex = _getActiveFrameIndex?.Invoke() ?? 0;

            if (activeIndex == 0)
            {
                // Estado OFF: Restaurar a estados originales
                RestoreOriginalStates();
            }
            else
            {
                // Estado ON: Desactivar todo y aplicar el frame
                DeactivateAllReferencedObjects();

                var firstFrame = _frames?.FirstOrDefault(f => f != null);
                if (firstFrame != null)
                {
                    firstFrame.ApplyCurrentFrame();
                }
            }

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
#endif
        }
        
        /// <summary>
        /// Limpia recursos del controlador
        /// </summary>
        public void Cleanup()
        {
            if (_isPreviewActive)
            {
                DeactivatePreview();
            }

            _originalObjectStates?.Clear();
            _originalBlendshapeValues?.Clear();
            _originalMaterialStates?.Clear();
        }
        
        // Métodos privados
        
        /// <summary>
        /// Almacena los estados originales para restauración
        /// CORREGIDO: Ahora guarda ObjectReferences, MaterialReferences y BlendshapeReferences de cada frame
        /// </summary>
        private void StoreOriginalStates()
        {
            _originalObjectStates.Clear();
            _originalBlendshapeValues.Clear();
            _originalMaterialStates.Clear();

            var validFrames = _frames?.Where(f => f != null) ?? Enumerable.Empty<MRAgruparObjetos>();
            foreach (var frame in validFrames)
            {
                // 1. Almacenar estados de ObjectReferences (GameObjects referenciados)
                if (frame.ObjectReferences != null)
                {
                    foreach (var objRef in frame.ObjectReferences)
                    {
                        if (objRef?.GameObject != null && !_originalObjectStates.ContainsKey(objRef.GameObject))
                        {
                            _originalObjectStates[objRef.GameObject] = objRef.GameObject.activeSelf;
                        }
                    }
                }

                // 2. Almacenar estados de MaterialReferences
                if (frame.MaterialReferences != null)
                {
                    foreach (var matRef in frame.MaterialReferences)
                    {
                        if (matRef?.TargetRenderer == null) continue;

                        var renderer = matRef.TargetRenderer;
                        var materialIndex = matRef.MaterialIndex;

                        if (!_originalMaterialStates.ContainsKey(renderer))
                        {
                            _originalMaterialStates[renderer] = new Dictionary<int, Material>();
                        }

                        // Guardar el material original si no lo tenemos ya
                        if (!_originalMaterialStates[renderer].ContainsKey(materialIndex))
                        {
                            if (materialIndex < renderer.sharedMaterials.Length)
                            {
                                _originalMaterialStates[renderer][materialIndex] = renderer.sharedMaterials[materialIndex];
                            }
                        }
                    }
                }

                // 3. Almacenar estados de BlendshapeReferences
                if (frame.BlendshapeReferences != null)
                {
                    foreach (var blendRef in frame.BlendshapeReferences)
                    {
                        if (blendRef?.TargetRenderer == null || string.IsNullOrEmpty(blendRef.BlendshapeName)) continue;

                        var renderer = blendRef.TargetRenderer;
                        var blendshapeName = blendRef.BlendshapeName;

                        if (!_originalBlendshapeValues.ContainsKey(renderer))
                        {
                            _originalBlendshapeValues[renderer] = new Dictionary<string, float>();
                        }

                        // Guardar el valor original si no lo tenemos ya
                        if (!_originalBlendshapeValues[renderer].ContainsKey(blendshapeName))
                        {
                            int blendIndex = renderer.sharedMesh?.GetBlendShapeIndex(blendshapeName) ?? -1;
                            if (blendIndex >= 0)
                            {
                                _originalBlendshapeValues[renderer][blendshapeName] = renderer.GetBlendShapeWeight(blendIndex);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Restaura los estados originales de todos los objetos
        /// CORREGIDO v2: Ahora restaura a los estados ORIGINALES, no solo desactiva
        /// </summary>
        private void RestoreOriginalStates()
        {
            // 1. Restaurar GameObjects a su estado ORIGINAL (no solo desactivar)
            foreach (var kvp in _originalObjectStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.SetActive(kvp.Value); // Restaurar al estado original guardado
                }
            }

            // 2. Restaurar materiales originales
            foreach (var rendererPair in _originalMaterialStates)
            {
                var renderer = rendererPair.Key;
                if (renderer == null) continue;

                var materials = rendererPair.Value;
                foreach (var materialPair in materials)
                {
                    var index = materialPair.Key;
                    var originalMaterial = materialPair.Value;

                    if (originalMaterial != null && index < renderer.sharedMaterials.Length)
                    {
                        var currentMaterials = renderer.sharedMaterials;
                        currentMaterials[index] = originalMaterial;
                        renderer.sharedMaterials = currentMaterials;
                    }
                }
            }

            // 3. Restaurar blendshapes a sus valores ORIGINALES (no a 0)
            foreach (var rendererPair in _originalBlendshapeValues)
            {
                var renderer = rendererPair.Key;
                if (renderer == null || renderer.sharedMesh == null) continue;

                var blendshapes = rendererPair.Value;
                foreach (var blendPair in blendshapes)
                {
                    var blendshapeName = blendPair.Key;
                    var originalValue = blendPair.Value;
                    int blendIndex = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
                    if (blendIndex >= 0)
                    {
                        renderer.SetBlendShapeWeight(blendIndex, originalValue); // Valor ORIGINAL
                    }
                }
            }

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
#endif
        }

        /// <summary>
        /// Resetea todos los estados referenciados (GameObjects, materiales, blendshapes)
        /// para preparar un frame limpio antes de aplicar un frame específico.
        /// CORREGIDO v3: Ahora también restaura materiales y blendshapes a sus estados originales
        /// </summary>
        private void DeactivateAllReferencedObjects()
        {
            // 1. Desactivar todos los GameObjects
            foreach (var kvp in _originalObjectStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.SetActive(false);
                }
            }

            // 2. Restaurar todos los materiales a sus estados originales
            foreach (var rendererPair in _originalMaterialStates)
            {
                var renderer = rendererPair.Key;
                if (renderer == null) continue;

                var materials = rendererPair.Value;
                foreach (var materialPair in materials)
                {
                    var index = materialPair.Key;
                    var originalMaterial = materialPair.Value;

                    if (originalMaterial != null && index < renderer.sharedMaterials.Length)
                    {
                        var currentMaterials = renderer.sharedMaterials;
                        currentMaterials[index] = originalMaterial;
                        renderer.sharedMaterials = currentMaterials;
                    }
                }
            }

            // 3. Restaurar todos los blendshapes a sus valores originales
            foreach (var rendererPair in _originalBlendshapeValues)
            {
                var renderer = rendererPair.Key;
                if (renderer == null || renderer.sharedMesh == null) continue;

                var blendshapes = rendererPair.Value;
                foreach (var blendPair in blendshapes)
                {
                    var blendshapeName = blendPair.Key;
                    var originalValue = blendPair.Value;
                    int blendIndex = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
                    if (blendIndex >= 0)
                    {
                        renderer.SetBlendShapeWeight(blendIndex, originalValue);
                    }
                }
            }
        }
    }
}
