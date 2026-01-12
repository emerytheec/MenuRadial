using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de blendshapes en frames.
    /// Implementa IBlendshapeReferenceController para abstracción.
    /// REFACTORIZADO: Extraído de MRAgruparObjetos.cs para responsabilidad única.
    /// </summary>
    public class FrameBlendshapeController : IBlendshapeReferenceController
    {
        private readonly FrameData _frameData;

        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameData">Datos del frame para gestionar</param>
        public FrameBlendshapeController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region IReferenceController Implementation

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
        
        
        /// <summary>
        /// Añade una referencia de blendshape al frame
        /// EXTRAÍDO: De MRAgruparObjetos.AddBlendshapeReference()
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        /// <param name="value">Valor del blendshape (0-100)</param>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        public bool AddBlendshape(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f)
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

            // Verificar que el mesh existe
            if (renderer.sharedMesh == null)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir blendshape '{blendshapeName}': el renderer '{renderer.name}' no tiene mesh asignado");
                return false;
            }

            // Verificar que el blendshape existe en el mesh
            int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            if (blendshapeIndex < 0)
            {
                Debug.LogWarning($"[MRAgruparObjetos] No se puede añadir blendshape '{blendshapeName}': no existe en el mesh de '{renderer.name}'");
                return false;
            }

            // Verificar si ya existe
            var existing = _frameData.BlendshapeReferences.FirstOrDefault(b =>
                b?.TargetRenderer == renderer && b.BlendshapeName == blendshapeName);

            if (existing != null)
            {
                existing.Value = value; // Actualizar valor existente
                return true; // Se considera éxito porque se actualizó
            }

            // Crear nueva referencia
            var newReference = new BlendshapeReference(renderer, blendshapeName, value);
            _frameData.BlendshapeReferences.Add(newReference);

            return true;
        }
        
        /// <summary>
        /// Elimina una referencia de blendshape del frame
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveBlendshapeReference()
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
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
        
        /// <summary>
        /// Elimina todas las referencias de blendshapes de un renderer específico
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveAllBlendshapeReferences()
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        public void RemoveAllBlendshapesFromRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;
            
            var toRemove = _frameData.BlendshapeReferences.Where(b => b?.TargetRenderer == renderer).ToList();
            
            foreach (var reference in toRemove)
            {
                _frameData.BlendshapeReferences.Remove(reference);
            }
            
            if (toRemove.Count > 0)
            {
            }
        }
        
        /// <summary>
        /// Limpia todas las referencias de blendshapes
        /// EXTRAÍDO: De MRAgruparObjetos.ClearAllBlendshapes()
        /// </summary>
        public void ClearAllBlendshapes()
        {
            int count = BlendshapeCount;
            _frameData.BlendshapeReferences.Clear();
        }
        
        /// <summary>
        /// Elimina las referencias de blendshapes inválidas
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveInvalidBlendshapeReferences()
        /// </summary>
        public void RemoveInvalidBlendshapeReferences()
        {
            var invalidReferences = _frameData.BlendshapeReferences.Where(b => b == null || !b.IsValid).ToList();
            
            foreach (var invalidRef in invalidReferences)
            {
                _frameData.BlendshapeReferences.Remove(invalidRef);
            }
            
            if (invalidReferences.Count > 0)
            {
            }
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todos los blendshapes
        /// EXTRAÍDO: De MRAgruparObjetos.UpdateAllBlendshapeRendererPaths()
        /// </summary>
        public void UpdateAllBlendshapeRendererPaths()
        {
            int updatedCount = 0;
            
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.TargetRenderer != null))
            {
                blendRef.UpdateRendererPath();
                updatedCount++;
            }
            
        }
        
        /// <summary>
        /// Captura los valores actuales de todos los blendshapes desde los renderers
        /// EXTRAÍDO: De MRAgruparObjetos.CaptureAllBlendshapeValues()
        /// </summary>
        public void CaptureAllBlendshapeValues()
        {
            int capturedCount = 0;
            
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                // Validación defensiva sin try-catch silencioso
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            float currentValue = renderer.GetBlendShapeWeight(blendshapeIndex);
                            blendRef.Value = currentValue;
                            capturedCount++;
                            
                        }
                    }
                }
            }
            
        }
        
        
        
        /// <summary>
        /// Aplica los estados de los blendshapes en la escena
        /// EXTRAÍDO: Parte de MRAgruparObjetos.ApplyCurrentFrame()
        /// </summary>
        public void ApplyBlendshapeStates()
        {
            int appliedCount = 0;
            
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                // Validación defensiva sin try-catch silencioso
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            renderer.SetBlendShapeWeight(blendshapeIndex, blendRef.Value);
                            appliedCount++;
                            
                        }
                    }
                }
            }
            
        }
        
        /// <summary>
        /// Captura los estados actuales de los blendshapes en la escena
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <returns>Lista de estados actuales</returns>
        public List<BlendshapeReference> CaptureCurrentStates()
        {
            var currentStates = new List<BlendshapeReference>();
            
            foreach (var blendRef in _frameData.BlendshapeReferences.Where(b => b != null && b.IsValid))
            {
                // Validación defensiva sin try-catch silencioso
                if (blendRef != null && !string.IsNullOrEmpty(blendRef.BlendshapeName))
                {
                    var renderer = blendRef.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            // Capturar el valor actual
                            float currentValue = renderer.GetBlendShapeWeight(blendshapeIndex);
                            var stateCapture = new BlendshapeReference(renderer, blendRef.BlendshapeName, currentValue);
                            currentStates.Add(stateCapture);
                            
                        }
                    }
                }
            }
            
            return currentStates;
        }
        
        /// <summary>
        /// Restaura estados previamente capturados
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <param name="savedStates">Estados a restaurar</param>
        public void RestoreStates(List<BlendshapeReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
            {
                return;
            }
            
            int restoredCount = 0;
            
            foreach (var savedState in savedStates.Where(s => s != null && s.IsValid))
            {
                // Validación defensiva sin try-catch silencioso
                if (savedState != null && !string.IsNullOrEmpty(savedState.BlendshapeName))
                {
                    var renderer = savedState.TargetRenderer;
                    if (renderer?.sharedMesh != null)
                    {
                        int blendshapeIndex = renderer.sharedMesh.GetBlendShapeIndex(savedState.BlendshapeName);
                        if (blendshapeIndex >= 0)
                        {
                            renderer.SetBlendShapeWeight(blendshapeIndex, savedState.Value);
                            restoredCount++;
                            
                        }
                    }
                }
            }
            
        }
        
        
        
        /// <summary>
        /// Busca una referencia de blendshape específica
        /// NUEVO: Método utilitario para búsquedas
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer a buscar</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        /// <returns>Referencia encontrada o null</returns>
        public BlendshapeReference FindBlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return null;
            
            return _frameData.BlendshapeReferences.FirstOrDefault(b => 
                b?.TargetRenderer == renderer && b.BlendshapeName == blendshapeName);
        }
        
        /// <summary>
        /// Verifica si un blendshape está en el frame
        /// NUEVO: Método utilitario para verificaciones
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer a verificar</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        /// <returns>True si está en el frame</returns>
        public bool ContainsBlendshape(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            return FindBlendshapeReference(renderer, blendshapeName) != null;
        }
        
        /// <summary>
        /// Obtiene todos los blendshapes de un renderer específico
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        /// <returns>Lista de blendshapes del renderer</returns>
        public List<BlendshapeReference> GetBlendshapesByRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return new List<BlendshapeReference>();
            
            return _frameData.BlendshapeReferences
                .Where(b => b != null && b.IsValid && b.TargetRenderer == renderer)
                .ToList();
        }
        
        /// <summary>
        /// Obtiene todos los blendshapes con valores mayores a cero
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de blendshapes activos</returns>
        public List<BlendshapeReference> GetActiveBlendshapes()
        {
            return _frameData.BlendshapeReferences
                .Where(b => b != null && b.IsValid && b.Value > 0f)
                .ToList();
        }
        
        /// <summary>
        /// Obtiene todos los blendshapes con valor cero
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de blendshapes inactivos</returns>
        public List<BlendshapeReference> GetInactiveBlendshapes()
        {
            return _frameData.BlendshapeReferences
                .Where(b => b != null && b.IsValid && b.Value == 0f)
                .ToList();
        }
        
        
    }
}
