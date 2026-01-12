using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame.Preview
{
    /// <summary>
    /// Operaciones especializadas de previsualización de frames
    /// FASE 2: Extraído de FramePreviewService (180 líneas)
    /// Responsabilidad única: Solo operaciones de aplicación y restauración
    /// </summary>
    public class PreviewOperations
    {
        private readonly FrameObjectManager _objectManager;
        private readonly FrameMaterialManager _materialManager;
        private readonly FrameBlendshapeManager _blendshapeManager;
        private readonly PreviewStateManager _stateManager;
        
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="objectManager">Manager de objetos</param>
        /// <param name="materialManager">Manager de materiales</param>
        /// <param name="blendshapeManager">Manager de blendshapes</param>
        /// <param name="stateManager">Gestor de estado</param>
        public PreviewOperations(FrameObjectManager objectManager, 
                                 FrameMaterialManager materialManager, 
                                 FrameBlendshapeManager blendshapeManager,
                                 PreviewStateManager stateManager)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _materialManager = materialManager ?? throw new ArgumentNullException(nameof(materialManager));
            _blendshapeManager = blendshapeManager ?? throw new ArgumentNullException(nameof(blendshapeManager));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }
        
        
        /// <summary>
        /// Inicia la previsualización guardando estados actuales y aplicando frame
        /// </summary>
        public void StartPreview()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            
            // Limpiar cualquier estado anterior
            _stateManager.Reset();
            
            // Capturar estados actuales usando los managers
            CaptureCurrentStates();
            
            // Aplicar estados del frame
            ApplyFrameStates();
            
            // Activar el estado de preview
            _stateManager.ActivatePreview();
        }
        
        /// <summary>
        /// Termina la previsualización restaurando estados originales
        /// </summary>
        public void EndPreview()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            
            if (!_stateManager.IsPreviewActive)
            {
                return;
            }
            
            // Restaurar estados originales
            RestoreOriginalStates();
            
            // Desactivar preview y limpiar estados
            _stateManager.DeactivatePreview();
            _stateManager.ClearAllStates();
        }
        
        /// <summary>
        /// Alterna el estado de previsualización (toggle)
        /// </summary>
        public void TogglePreview()
        {
            if (_stateManager.IsPreviewActive)
            {
                EndPreview();
            }
            else
            {
                StartPreview();
            }
        }
        
        
        
        /// <summary>
        /// Captura los estados actuales de todos los tipos usando los managers
        /// </summary>
        private void CaptureCurrentStates()
        {
            var objectStates = _objectManager.CaptureCurrentStates();
            var materialStates = _materialManager.CaptureCurrentStates();
            var blendshapeStates = _blendshapeManager.CaptureCurrentStates();
            
            _stateManager.SaveAllStates(objectStates, materialStates, blendshapeStates);
            
        }
        
        /// <summary>
        /// Captura solo los estados de objetos
        /// </summary>
        public void CaptureObjectStatesOnly()
        {
            var objectStates = _objectManager.CaptureCurrentStates();
            _stateManager.SaveObjectStates(objectStates);
        }
        
        /// <summary>
        /// Captura solo los estados de materiales
        /// </summary>
        public void CaptureMaterialStatesOnly()
        {
            var materialStates = _materialManager.CaptureCurrentStates();
            _stateManager.SaveMaterialStates(materialStates);
        }
        
        /// <summary>
        /// Captura solo los estados de blendshapes
        /// </summary>
        public void CaptureBlendshapeStatesOnly()
        {
            var blendshapeStates = _blendshapeManager.CaptureCurrentStates();
            _stateManager.SaveBlendshapeStates(blendshapeStates);
        }
        
        
        
        /// <summary>
        /// Aplica los estados del frame usando todos los managers
        /// </summary>
        private void ApplyFrameStates()
        {
            if (_objectManager == null) throw new InvalidOperationException("ObjectManager no ha sido inicializado");
            if (_materialManager == null) throw new InvalidOperationException("MaterialManager no ha sido inicializado");
            if (_blendshapeManager == null) throw new InvalidOperationException("BlendshapeManager no ha sido inicializado");
            
            _objectManager.ApplyObjectStates();
            _materialManager.ApplyMaterialStates();
            _blendshapeManager.ApplyBlendshapeStates();
        }
        
        /// <summary>
        /// Aplica solo los estados de objetos del frame
        /// </summary>
        public void ApplyObjectStatesOnly()
        {
            if (_objectManager == null) throw new InvalidOperationException("ObjectManager no ha sido inicializado");
            
            _objectManager.ApplyObjectStates();
        }
        
        /// <summary>
        /// Aplica solo los estados de materiales del frame
        /// </summary>
        public void ApplyMaterialStatesOnly()
        {
            if (_materialManager == null) throw new InvalidOperationException("MaterialManager no ha sido inicializado");
            
            _materialManager.ApplyMaterialStates();
        }
        
        /// <summary>
        /// Aplica solo los estados de blendshapes del frame
        /// </summary>
        public void ApplyBlendshapeStatesOnly()
        {
            if (_blendshapeManager == null) throw new InvalidOperationException("BlendshapeManager no ha sido inicializado");
            
            _blendshapeManager.ApplyBlendshapeStates();
        }
        
        /// <summary>
        /// Fuerza la aplicación del frame sin guardar estados (sin preview)
        /// </summary>
        public void ForceApplyFrame()
        {
            ApplyFrameStates();
        }
        
        
        
        /// <summary>
        /// Restaura los estados originales de todos los tipos
        /// </summary>
        private void RestoreOriginalStates()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            if (_objectManager == null) throw new InvalidOperationException("ObjectManager no ha sido inicializado");
            if (_materialManager == null) throw new InvalidOperationException("MaterialManager no ha sido inicializado");
            if (_blendshapeManager == null) throw new InvalidOperationException("BlendshapeManager no ha sido inicializado");
            
            // Restaurar objetos
            var objectStates = _stateManager.GetObjectStates();
            if (objectStates != null && objectStates.Count > 0)
            {
                _objectManager.RestoreStates(objectStates);
            }
            
            // Restaurar materiales
            var materialStates = _stateManager.GetMaterialStates();
            if (materialStates != null && materialStates.Count > 0)
            {
                _materialManager.RestoreStates(materialStates);
            }
            
            // Restaurar blendshapes
            var blendshapeStates = _stateManager.GetBlendshapeStates();
            if (blendshapeStates != null && blendshapeStates.Count > 0)
            {
                _blendshapeManager.RestoreStates(blendshapeStates);
            }
        }
        
        /// <summary>
        /// Restaura solo los objetos a su estado original
        /// </summary>
        public void RestoreObjectsOnly()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            if (_objectManager == null) throw new InvalidOperationException("ObjectManager no ha sido inicializado");
            
            var objectStates = _stateManager.GetObjectStates();
            if (objectStates != null && objectStates.Count > 0)
            {
                _objectManager.RestoreStates(objectStates);
                _stateManager.ClearObjectStates();
            }
        }
        
        /// <summary>
        /// Restaura solo los materiales a su estado original
        /// </summary>
        public void RestoreMaterialsOnly()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            if (_materialManager == null) throw new InvalidOperationException("MaterialManager no ha sido inicializado");
            
            var materialStates = _stateManager.GetMaterialStates();
            if (materialStates != null && materialStates.Count > 0)
            {
                _materialManager.RestoreStates(materialStates);
                _stateManager.ClearMaterialStates();
            }
        }
        
        /// <summary>
        /// Restaura solo los blendshapes a su estado original
        /// </summary>
        public void RestoreBlendshapesOnly()
        {
            if (_stateManager == null) throw new InvalidOperationException("StateManager no ha sido inicializado");
            if (_blendshapeManager == null) throw new InvalidOperationException("BlendshapeManager no ha sido inicializado");
            
            var blendshapeStates = _stateManager.GetBlendshapeStates();
            if (blendshapeStates != null && blendshapeStates.Count > 0)
            {
                _blendshapeManager.RestoreStates(blendshapeStates);
                _stateManager.ClearBlendshapeStates();
            }
        }
        
        
        
        /// <summary>
        /// Actualiza los estados guardados con los valores actuales (útil durante edición)
        /// </summary>
        public void UpdateSavedStates()
        {
            if (_stateManager.IsPreviewActive)
            {
                CaptureCurrentStates();
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Aplica el frame manteniendo la previsualización activa (actualiza preview)
        /// </summary>
        public void RefreshPreview()
        {
            if (_stateManager != null && _stateManager.IsPreviewActive)
            {
                ApplyFrameStates();
            }
        }
        
        
        
        
        
        
    }
    
}
