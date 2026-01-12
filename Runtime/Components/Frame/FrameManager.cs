using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Gestor centralizado para operaciones de Frame
    /// FASE 3: Extrae lógica compleja de MRAgruparObjetos para simplificarlo
    /// Coordina FrameData optimizado con strategies especializadas
    /// </summary>
    public class FrameManager
    {
        private readonly FrameData _frameData;
        private readonly IFramePreviewStrategy _previewStrategy;
        
        /// <summary>
        /// Datos del frame gestionado
        /// </summary>
        public FrameData FrameData => _frameData;
        
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        /// <param name="previewStrategy">Estrategia de previsualización</param>
        public FrameManager(FrameData frameData, IFramePreviewStrategy previewStrategy = null)
        {
            _frameData = frameData ?? new FrameData("Managed Frame");
            _previewStrategy = previewStrategy ?? new DefaultFramePreviewStrategy();
        }
        
        
        /// <summary>
        /// Añade un GameObject al frame
        /// SIMPLIFICADO: Delegación directa a FrameData optimizado
        /// </summary>
        public void AddObject(GameObject gameObject, bool isActive = true)
        {
            _frameData.AddObjectReference(gameObject, isActive);
        }
        
        /// <summary>
        /// Elimina un GameObject del frame
        /// </summary>
        public void RemoveObject(GameObject gameObject)
        {
            _frameData.RemoveObjectReference(gameObject);
        }
        
        /// <summary>
        /// Limpia todos los objetos del frame
        /// </summary>
        public void ClearAllObjects()
        {
            var count = _frameData.ObjectReferences.Count;
            _frameData.ClearObjectReferences();
        }
        
        /// <summary>
        /// Selecciona/deselecciona todos los objetos
        /// </summary>
        public void SetAllObjectsActive(bool isActive)
        {
            _frameData.SetAllReferencesActive(isActive);
        }
        
        
        
        /// <summary>
        /// Añade una referencia de material al frame
        /// </summary>
        public void AddMaterial(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            _frameData.AddMaterialReference(renderer, materialIndex, alternativeMaterial);
        }
        
        /// <summary>
        /// Elimina una referencia de material del frame
        /// </summary>
        public void RemoveMaterial(Renderer renderer, int materialIndex = 0)
        {
            _frameData.RemoveMaterialReference(renderer, materialIndex);
        }
        
        /// <summary>
        /// Limpia todas las referencias de materiales
        /// </summary>
        public void ClearAllMaterials()
        {
            var count = _frameData.MaterialReferencesData.Count;
            _frameData.ClearMaterialReferences();
        }
        
        /// <summary>
        /// Actualiza las referencias originales de todos los materiales
        /// </summary>
        public void UpdateAllOriginalMaterials()
        {
            _frameData.UpdateAllOriginalMaterials();
        }
        
        
        
        /// <summary>
        /// Añade una referencia de blendshape al frame
        /// </summary>
        public void AddBlendshape(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f)
        {
            _frameData.AddBlendshapeReference(renderer, blendshapeName, value);
        }
        
        /// <summary>
        /// Elimina una referencia de blendshape del frame
        /// </summary>
        public void RemoveBlendshape(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            _frameData.RemoveBlendshapeReference(renderer, blendshapeName);
        }
        
        /// <summary>
        /// Limpia todas las referencias de blendshapes
        /// </summary>
        public void ClearAllBlendshapes()
        {
            var count = _frameData.BlendshapeReferences.Count;
            _frameData.ClearBlendshapeReferences();
        }
        
        /// <summary>
        /// Captura los valores actuales de todos los blendshapes
        /// </summary>
        public void CaptureAllBlendshapeValues()
        {
            _frameData.CaptureAllBlendshapeValues();
        }
        
        
        
        /// <summary>
        /// Aplica el estado completo del frame
        /// SIMPLIFICADO: Una línea reemplaza bucles complejos
        /// </summary>
        public void ApplyFrame()
        {
            _frameData.ApplyState();
        }
        
        /// <summary>
        /// Previsualiza el frame usando la estrategia configurada
        /// NUEVO: Strategy Pattern para diferentes tipos de preview
        /// </summary>
        public void PreviewFrame()
        {
            _previewStrategy.PreviewFrame(_frameData);
        }
        
        /// <summary>
        /// Cancela la previsualización del frame
        /// </summary>
        public void CancelPreview()
        {
            _previewStrategy.CancelPreview();
        }
        
        /// <summary>
        /// Indica si hay una previsualización activa
        /// </summary>
        public bool IsPreviewActive => _previewStrategy.IsPreviewActive;
        
        
        
        /// <summary>
        /// Recalcula todas las rutas jerárquicas
        /// SIMPLIFICADO: Una línea maneja todos los tipos
        /// </summary>
        public void RecalculateAllPaths()
        {
            _frameData.UpdateAllHierarchyPaths();
        }
        
        /// <summary>
        /// Elimina todas las referencias inválidas
        /// NUEVO: Operación unificada para todos los tipos
        /// </summary>
        public int RemoveAllInvalidReferences()
        {
            var removed = _frameData.RemoveAllInvalidReferences();
            return removed;
        }
        
        /// <summary>
        /// Limpia todas las referencias de todos los tipos
        /// NUEVO: Operación masiva
        /// </summary>
        public void ClearAllReferences()
        {
            var totalBefore = _frameData.GetTotalReferenceCount();
            _frameData.ClearAllReferences();
        }
        
        
        
        
        
        
        
        /// <summary>
        /// Valida el estado completo del frame
        /// NUEVO: Validación unificada y detallada
        /// </summary>
        public ValidationResult ValidateFrame()
        {
            var result = new ValidationResult();
            
            // Validar frame data
            if (_frameData == null)
            {
                result.AddChild(ValidationResult.Error("FrameData no puede ser null"));
                return result;
            }
            
            // Usar validación unificada del FrameData optimizado
            var dataValidation = _frameData.ValidateAllReferences();
            result.AddChild(dataValidation);
            
            // Validar preview strategy
            if (_previewStrategy == null)
            {
                result.AddChild(ValidationResult.Warning("No hay estrategia de previsualización configurada"));
            }
            
            // Validación de contenido
            if (_frameData.ObjectReferences.Count == 0 && 
                _frameData.MaterialReferencesData.Count == 0 && 
                _frameData.BlendshapeReferences.Count == 0)
            {
                result.AddChild(ValidationResult.Info("El frame está vacío (sin referencias)"));
            }
            
            return result;
        }
        
    }
    
}
