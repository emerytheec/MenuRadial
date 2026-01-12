using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para el sistema de preview de frames
    /// REFACTORIZADO: Extraído de MRAgruparObjetos.cs para responsabilidad única
    /// Versión: 0.035 - Controlador de preview independiente
    /// </summary>
    public class FramePreviewController
    {
        private readonly FrameObjectController _objectController;
        private readonly FrameMaterialController _materialController;
        private readonly FrameBlendshapeController _blendshapeController;
        
        // Estados de preview
        private bool _isPreviewActive = false;
        private List<ObjectReference> _originalObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="objectController">Controlador de objetos</param>
        /// <param name="materialController">Controlador de materiales</param>
        /// <param name="blendshapeController">Controlador de blendshapes</param>
        public FramePreviewController(
            FrameObjectController objectController,
            FrameMaterialController materialController,
            FrameBlendshapeController blendshapeController)
        {
            _objectController = objectController ?? throw new System.ArgumentNullException(nameof(objectController));
            _materialController = materialController ?? throw new System.ArgumentNullException(nameof(materialController));
            _blendshapeController = blendshapeController ?? throw new System.ArgumentNullException(nameof(blendshapeController));
        }
        
        /// <summary>
        /// Indica si la previsualización está activa
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        /// <summary>
        /// Indica si hay estados guardados para restaurar
        /// </summary>
        public bool HasSavedStates => 
            _originalObjectStates.Count > 0 || 
            _originalMaterialStates.Count > 0 || 
            _originalBlendshapeStates.Count > 0;
        
        
        /// <summary>
        /// Previsualiza el frame aplicando su estado (toggle)
        /// EXTRAÍDO: De MRAgruparObjetos.PreviewFrame()
        /// </summary>
        public void PreviewFrame()
        {
            // Validación defensiva antes de aplicar
            if (_objectController == null || _materialController == null || _blendshapeController == null)
                return;
                
            if (_isPreviewActive)
            {
                // Si está en preview, restaurar estado original
                RestoreOriginalStates();
            }
            else
            {
                // Si no está en preview, guardar estado actual y aplicar frame
                SaveOriginalStates();
                ApplyFrameStates();
                _isPreviewActive = true;
            }
        }
        
        /// <summary>
        /// Refresca la previsualización aplicando los estados actuales del frame
        /// sin modificar los estados originales guardados.
        /// Útil para actualizar la escena cuando el usuario modifica valores durante el preview.
        /// </summary>
        public void RefreshPreview()
        {
            if (!_isPreviewActive)
                return;

            // Validación defensiva
            if (_objectController == null || _materialController == null || _blendshapeController == null)
                return;

            // Reaplicar estados del frame sin guardar/restaurar
            ApplyFrameStates();
        }

        /// <summary>
        /// Cancela la previsualización sin aplicar cambios
        /// EXTRAÍDO: De MRAgruparObjetos.CancelPreview()
        /// </summary>
        public void CancelPreview()
        {
            // Validación defensiva antes de cancelar
            if (_objectController == null || _materialController == null || _blendshapeController == null)
                return;
                
            if (_isPreviewActive)
            {
                RestoreOriginalStates();
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Sincroniza con estados serializados externos
        /// EXTRAÍDO: Lógica de sincronización de MRAgruparObjetos
        /// </summary>
        /// <param name="isActive">Estado de preview</param>
        /// <param name="objectStates">Estados de objetos guardados</param>
        /// <param name="materialStates">Estados de materiales guardados</param>
        /// <param name="blendshapeStates">Estados de blendshapes guardados</param>
        public void SyncWithSerializedStates(
            bool isActive,
            List<ObjectReference> objectStates,
            List<MaterialReference> materialStates,
            List<BlendshapeReference> blendshapeStates)
        {
            _isPreviewActive = isActive;
            
            // Sincronizar estados guardados
            _originalObjectStates = objectStates ?? new List<ObjectReference>();
            _originalMaterialStates = materialStates ?? new List<MaterialReference>();
            _originalBlendshapeStates = blendshapeStates ?? new List<BlendshapeReference>();
            
            // Si no hay estados guardados pero está marcado como activo, resetear
            if (_isPreviewActive && !HasSavedStates)
            {
                _isPreviewActive = false;
            }
        }
        
        
        
        /// <summary>
        /// Guarda los estados originales de todos los elementos
        /// EXTRAÍDO: De MRAgruparObjetos.SaveOriginalStates()
        /// </summary>
        private void SaveOriginalStates()
        {
            // Limpiar estados previos
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
            
            // Capturar estados actuales usando controladores especializados
            _originalObjectStates = _objectController.CaptureCurrentStates();
            _originalMaterialStates = _materialController.CaptureCurrentStates();
            _originalBlendshapeStates = _blendshapeController.CaptureCurrentStates();
        }
        
        /// <summary>
        /// Restaura los estados originales guardados
        /// EXTRAÍDO: De MRAgruparObjetos.RestoreOriginalStates()
        /// </summary>
        private void RestoreOriginalStates()
        {
            // Restaurar usando controladores especializados
            if (_originalObjectStates.Count > 0)
            {
                _objectController.RestoreStates(_originalObjectStates);
            }
            
            if (_originalMaterialStates.Count > 0)
            {
                _materialController.RestoreStates(_originalMaterialStates);
            }
            
            if (_originalBlendshapeStates.Count > 0)
            {
                _blendshapeController.RestoreStates(_originalBlendshapeStates);
            }
            
            // Limpiar estados guardados
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
            _isPreviewActive = false;
            
        }
        
        /// <summary>
        /// Aplica los estados del frame usando controladores especializados
        /// EXTRAÍDO: Lógica de aplicación de MRAgruparObjetos.ApplyCurrentFrame()
        /// </summary>
        private void ApplyFrameStates()
        {
            
            // Aplicar estados usando controladores especializados
            _objectController.ApplyObjectStates();
            _materialController.ApplyMaterialStates();
            _blendshapeController.ApplyBlendshapeStates();
        }
        
        
        
        
        /// <summary>
        /// Fuerza la limpieza de todos los estados guardados
        /// NUEVO: Método para limpieza manual
        /// </summary>
        public void ForceCleanup()
        {
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
            _isPreviewActive = false;
            
        }
        
        /// <summary>
        /// Verifica si los estados guardados siguen siendo válidos
        /// NUEVO: Método para validación de estados
        /// </summary>
        /// <returns>True si todos los estados son válidos</returns>
        public bool AreStatesValid()
        {
            if (!HasSavedStates) return true;
            
            // Verificar objetos
            foreach (var objState in _originalObjectStates)
            {
                if (objState?.GameObject == null)
                {
                    return false;
                }
            }
            
            // Verificar materiales
            foreach (var matState in _originalMaterialStates)
            {
                if (matState?.TargetRenderer == null)
                {
                    return false;
                }
            }
            
            // Verificar blendshapes
            foreach (var blendState in _originalBlendshapeStates)
            {
                if (blendState?.TargetRenderer == null || string.IsNullOrEmpty(blendState.BlendshapeName))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        
    }
}
