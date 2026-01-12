using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Gestor especializado para el estado y previsualización de frames
    /// FASE 1: Extraído de MRAgruparObjetos.cs (200 líneas) para separar responsabilidades
    /// Responsabilidad única: Solo gestión de estado y previsualización
    /// Usa managers optimizados con BaseReferenceManager
    /// </summary>
    public class MRFrameStateManager
    {
        private readonly FrameData _frameData;
        
        // Referencias a managers principales
        private FrameObjectManager _objectManager;
        private FrameMaterialManager _materialManager;
        private FrameBlendshapeManager _blendshapeManager;
        
        // Sistema de previsualización
        private bool _isPreviewActive = false;
        private List<ObjectReference> _capturedObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _capturedMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _capturedBlendshapeStates = new List<BlendshapeReference>();
        
        /// <summary>
        /// Constructor con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        public MRFrameStateManager(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
            InitializeManagers();
        }
        
        /// <summary>
        /// Inicializa los managers principales
        /// </summary>
        private void InitializeManagers()
        {
            _objectManager = new FrameObjectManager(_frameData);
            _materialManager = new FrameMaterialManager(_frameData);
            _blendshapeManager = new FrameBlendshapeManager(_frameData);
        }
        
        
        /// <summary>
        /// Indica si la previsualización está activa
        /// </summary>
        public bool IsPreviewActive 
        { 
            get 
            {
                // La previsualización está activa si tenemos estados guardados
                bool hasStates = (_capturedObjectStates?.Count ?? 0) > 0 ||
                                (_capturedMaterialStates?.Count ?? 0) > 0 ||
                                (_capturedBlendshapeStates?.Count ?? 0) > 0;
                
                return _isPreviewActive && hasStates;
            }
        }
        
        /// <summary>
        /// Obtiene el número total de estados guardados 
        /// </summary>
        public int SavedStatesCount => 
            (_capturedObjectStates?.Count ?? 0) + 
            (_capturedMaterialStates?.Count ?? 0) + 
            (_capturedBlendshapeStates?.Count ?? 0);
        
        
        
        /// <summary>
        /// Aplica el estado completo del frame
        /// FASE 1: Usa managers optimizados con BaseReferenceManager
        /// </summary>
        public void ApplyFrameState()
        {
            if (_frameData == null)
            {
                return;
            }
            
            
            // Aplicar estados usando managers principales
            _objectManager.ApplyObjectStates();
            _materialManager.ApplyMaterialStates();
            _blendshapeManager.ApplyBlendshapeStates();
            
        }
        
        
        
        /// <summary>
        /// Previsualiza el frame aplicando su estado (toggle)
        /// FASE 1: Sistema optimizado con managers especializados
        /// </summary>
        public void PreviewFrame()
        {
            if (_isPreviewActive && HasCapturedStates())
            {
                // Cancelar previsualización - restaurar estados originales
                RestoreOriginalStates();
            }
            else
            {
                // Activar previsualización - guardar estados y aplicar frame
                StartPreview();
            }
        }
        
        /// <summary>
        /// Cancela la previsualización sin aplicar cambios
        /// </summary>
        public void CancelPreview()
        {
            if (_isPreviewActive)
            {
                RestoreOriginalStates();
            }
        }
        
        /// <summary>
        /// Inicia la previsualización guardando estados y aplicando frame
        /// FASE 1: Optimizado con managers especializados
        /// </summary>
        private void StartPreview()
        {
            // Limpiar cualquier estado anterior inconsistente
            ClearCapturedStates();
            
            // Guardar estados actuales usando managers optimizados
            CaptureCurrentStates();
            
            // Aplicar frame
            ApplyFrameState();
            
            _isPreviewActive = true;
            
        }
        
        
        
        /// <summary>
        /// Captura los estados actuales usando managers optimizados
        /// FASE 1: Usa BaseReferenceManager.CaptureCurrentStates()
        /// </summary>
        private void CaptureCurrentStates()
        {
            ClearCapturedStates();
            
            // FASE 1: Capturar estados usando managers optimizados
            _capturedObjectStates = _objectManager.CaptureCurrentStates();
            _capturedMaterialStates = _materialManager.CaptureCurrentStates();
            _capturedBlendshapeStates = _blendshapeManager.CaptureCurrentStates();
            
        }
        
        /// <summary>
        /// Restaura los estados originales usando managers optimizados
        /// FASE 1: Usa BaseReferenceManager.RestoreStates()
        /// </summary>
        private void RestoreOriginalStates()
        {
            var restored = 0;
            
            // FASE 1: Restaurar usando managers optimizados
            if (_capturedObjectStates?.Count > 0)
            {
                _objectManager.RestoreStates(_capturedObjectStates);
                restored += _capturedObjectStates.Count;
            }
            
            if (_capturedMaterialStates?.Count > 0)
            {
                _materialManager.RestoreStates(_capturedMaterialStates);
                restored += _capturedMaterialStates.Count;
            }
            
            if (_capturedBlendshapeStates?.Count > 0)
            {
                _blendshapeManager.RestoreStates(_capturedBlendshapeStates);
                restored += _capturedBlendshapeStates.Count;
            }
            
            
            // Limpiar estados guardados
            ClearCapturedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia todos los estados capturados
        /// </summary>
        private void ClearCapturedStates()
        {
            if (_capturedObjectStates == null) _capturedObjectStates = new List<ObjectReference>();
            if (_capturedMaterialStates == null) _capturedMaterialStates = new List<MaterialReference>();
            if (_capturedBlendshapeStates == null) _capturedBlendshapeStates = new List<BlendshapeReference>();
            
            _capturedObjectStates.Clear();
            _capturedMaterialStates.Clear();
            _capturedBlendshapeStates.Clear();
        }
        
        /// <summary>
        /// Verifica si hay estados capturados
        /// </summary>
        private bool HasCapturedStates()
        {
            return (_capturedObjectStates?.Count ?? 0) > 0 ||
                   (_capturedMaterialStates?.Count ?? 0) > 0 ||
                   (_capturedBlendshapeStates?.Count ?? 0) > 0;
        }
        
        
        
        /// <summary>
        /// Verifica si hay estados guardados inconsistentes
        /// </summary>
        public bool HasInconsistentStates()
        {
            if (_isPreviewActive)
            {
                bool hasObjectStates = (_capturedObjectStates?.Count ?? 0) > 0;
                bool hasMaterialStates = (_capturedMaterialStates?.Count ?? 0) > 0;
                bool hasBlendshapeStates = (_capturedBlendshapeStates?.Count ?? 0) > 0;
                
                bool hasObjectsInFrame = _frameData.ObjectReferences.Count > 0;
                bool hasMaterialsInFrame = _frameData.MaterialReferencesData.Count > 0;
                bool hasBlendshapesInFrame = _frameData.BlendshapeReferences.Count > 0;
                
                // Inconsistencia: tenemos elementos en el frame pero no estados guardados
                if ((hasObjectsInFrame && !hasObjectStates) ||
                    (hasMaterialsInFrame && !hasMaterialStates) ||
                    (hasBlendshapesInFrame && !hasBlendshapeStates))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Corrige estados inconsistentes
        /// </summary>
        public void FixInconsistentStates()
        {
            if (HasInconsistentStates())
            {
                
                if (_isPreviewActive)
                {
                    // Cancelar preview y limpiar estados
                    _isPreviewActive = false;
                    ClearCapturedStates();
                }
                
            }
        }
        
        /// <summary>
        /// Fuerza la aplicación del frame sin guardar estados (para uso externo)
        /// </summary>
        public void ForceApplyFrame()
        {
            ApplyFrameState();
        }
        
        
        
        /// <summary>
        /// Inicializa el gestor con estados limpios
        /// </summary>
        public void Initialize()
        {
            ClearCapturedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia el gestor y cancela cualquier preview activo
        /// </summary>
        public void Cleanup()
        {
            if (_isPreviewActive)
            {
                CancelPreview();
            }
            ClearCapturedStates();
        }
        
        
        
        /// <summary>
        /// Obtiene información detallada 
        /// </summary>
        
        
    }
}
