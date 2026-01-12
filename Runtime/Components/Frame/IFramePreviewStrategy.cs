using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Interfaz para estrategias de previsualización de frames
    /// FASE 3: Strategy Pattern para diferentes tipos de preview
    /// </summary>
    public interface IFramePreviewStrategy
    {
        /// <summary>
        /// Indica si hay una previsualización activa
        /// </summary>
        bool IsPreviewActive { get; }
        
        /// <summary>
        /// Previsualiza un frame aplicando su estado
        /// </summary>
        /// <param name="frameData">Datos del frame a previsualizar</param>
        void PreviewFrame(FrameData frameData);
        
        /// <summary>
        /// Cancela la previsualización restaurando el estado original
        /// </summary>
        void CancelPreview();
    }
    
    /// <summary>
    /// Estrategia por defecto para previsualización de frames
    /// NUEVO: Extrae lógica compleja del MRAgruparObjetos original
    /// </summary>
    public class DefaultFramePreviewStrategy : IFramePreviewStrategy
    {
        private bool _isPreviewActive = false;
        private List<ObjectReference> _originalObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        /// <summary>
        /// Indica si hay una previsualización activa
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive && HasSavedStates();
        
        /// <summary>
        /// Previsualiza un frame (toggle: activa/cancela)
        /// SIMPLIFICADO: Extrae lógica compleja del MRAgruparObjetos
        /// </summary>
        public void PreviewFrame(FrameData frameData)
        {
            if (frameData == null)
            {
                return;
            }
            
            if (IsPreviewActive)
            {
                // Cancelar previsualización actual
                CancelPreview();
            }
            else
            {
                // Activar previsualización
                StartPreview(frameData);
            }
        }
        
        /// <summary>
        /// Cancela la previsualización restaurando estados originales
        /// </summary>
        public void CancelPreview()
        {
            if (!_isPreviewActive) return;
            
            RestoreOriginalStates();
            ClearSavedStates();
            _isPreviewActive = false;
            
        }
        
        /// <summary>
        /// Inicia la previsualización guardando estados y aplicando frame
        /// </summary>
        private void StartPreview(FrameData frameData)
        {
            // Limpiar estados anteriores
            ClearSavedStates();
            
            // Guardar estados actuales
            SaveOriginalStates(frameData);
            
            // Aplicar frame
            frameData.ApplyState();
            
            _isPreviewActive = true;
            
            var stats = GetPreviewStats();
        }
        
        /// <summary>
        /// Guarda los estados originales de todos los elementos del frame
        /// OPTIMIZADO: Usa las nuevas APIs unificadas
        /// </summary>
        private void SaveOriginalStates(FrameData frameData)
        {
            // Guardar estados de objetos
            foreach (var objRef in frameData.ObjectReferences)
            {
                if (objRef.IsValid)
                {
                    bool currentState = objRef.GameObject.activeSelf;
                    var originalState = new ObjectReference(objRef.GameObject, currentState);
                    _originalObjectStates.Add(originalState);
                }
            }
            
            // Guardar estados de materiales
            foreach (var matRef in frameData.MaterialReferencesData)
            {
                if (matRef.IsValid)
                {
                    var originalMatRef = new MaterialReference(matRef.TargetRenderer, matRef.MaterialIndex);
                    _originalMaterialStates.Add(originalMatRef);
                }
            }
            
            // Guardar estados de blendshapes
            foreach (var blendRef in frameData.BlendshapeReferences)
            {
                if (blendRef.IsValid)
                {
                    float currentValue = blendRef.GetCurrentValue();
                    var originalBlendRef = new BlendshapeReference(blendRef.TargetRenderer, blendRef.BlendshapeName, currentValue);
                    _originalBlendshapeStates.Add(originalBlendRef);
                }
            }
            
        }
        
        /// <summary>
        /// Restaura los estados originales guardados
        /// OPTIMIZADO: Usa las nuevas APIs unificadas
        /// </summary>
        private void RestoreOriginalStates()
        {
            // Restaurar objetos
            foreach (var originalState in _originalObjectStates)
            {
                if (originalState.IsValid)
                {
                    originalState.Apply();
                }
            }
            
            // Restaurar materiales
            foreach (var originalMatState in _originalMaterialStates)
            {
                if (originalMatState.IsValid)
                {
                    originalMatState.RestoreOriginalMaterial();
                }
            }
            
            // Restaurar blendshapes
            foreach (var originalBlendState in _originalBlendshapeStates)
            {
                if (originalBlendState.IsValid)
                {
                    originalBlendState.Apply();
                }
            }
            
        }
        
        /// <summary>
        /// Limpia todos los estados guardados
        /// </summary>
        private void ClearSavedStates()
        {
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
        }
        
        /// <summary>
        /// Verifica si hay estados guardados
        /// </summary>
        private bool HasSavedStates()
        {
            return _originalObjectStates.Count > 0 || 
                   _originalMaterialStates.Count > 0 || 
                   _originalBlendshapeStates.Count > 0;
        }
        
        /// <summary>
        /// Obtiene el número total de estados guardados
        /// </summary>
        private int GetSavedStatesCount()
        {
            return _originalObjectStates.Count + _originalMaterialStates.Count + _originalBlendshapeStates.Count;
        }
        
        /// <summary>
        /// Obtiene estadísticas de previsualización 
        /// </summary>
        private string GetPreviewStats()
        {
            return "Objetos=" + _originalObjectStates.Count + 
                   ", Materiales=" + _originalMaterialStates.Count + 
                   ", Blendshapes=" + _originalBlendshapeStates.Count;
        }
    }
}
