using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Servicio especializado para la gestión de previsualización de frames
    /// FASE 3: Extrae toda la lógica de preview de MRAgruparObjetos para simplificarlo
    /// Responsabilidad única: Solo sistema de previsualización
    /// </summary>
    public class FramePreviewService
    {
        private readonly FrameObjectManager _objectManager;
        private readonly FrameMaterialManager _materialManager;
        private readonly FrameBlendshapeManager _blendshapeManager;
        
        // Estados originales para restauración
        private List<ObjectReference> _originalObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        private bool _isPreviewActive = false;
        
        /// <summary>
        /// Constructor con inyección de dependencias de los managers
        /// </summary>
        /// <param name="objectManager">Manager de objetos</param>
        /// <param name="materialManager">Manager de materiales</param>
        /// <param name="blendshapeManager">Manager de blendshapes</param>
        public FramePreviewService(FrameObjectManager objectManager, FrameMaterialManager materialManager, FrameBlendshapeManager blendshapeManager)
        {
            _objectManager = objectManager ?? throw new System.ArgumentNullException(nameof(objectManager));
            _materialManager = materialManager ?? throw new System.ArgumentNullException(nameof(materialManager));
            _blendshapeManager = blendshapeManager ?? throw new System.ArgumentNullException(nameof(blendshapeManager));
        }
        
        
        /// <summary>
        /// Indica si la previsualización está activa (basado en estados reales)
        /// EXTRAÍDO de MRAgruparObjetos.IsPreviewActive
        /// </summary>
        public bool IsPreviewActive 
        { 
            get 
            {
                // La previsualización está activa si tenemos estados guardados
                bool hasObjectStates = _originalObjectStates != null && _originalObjectStates.Count > 0;
                bool hasMaterialStates = _originalMaterialStates != null && _originalMaterialStates.Count > 0;
                bool hasBlendshapeStates = _originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0;
                
                return _isPreviewActive && (hasObjectStates || hasMaterialStates || hasBlendshapeStates);
            }
        }
        
        /// <summary>
        /// Obtiene el número total de estados guardados
        /// NUEVO: Estadística útil 
        /// </summary>
        public int SavedStatesCount => 
            (_originalObjectStates?.Count ?? 0) + 
            (_originalMaterialStates?.Count ?? 0) + 
            (_originalBlendshapeStates?.Count ?? 0);
        
        
        
        /// <summary>
        /// Previsualiza el frame aplicando su estado (toggle)
        /// EXTRAÍDO de MRAgruparObjetos.PreviewFrame()
        /// </summary>
        public void PreviewFrame()
        {
            // Verificar si realmente tenemos una previsualización activa
            bool hasOriginalStates = (_originalObjectStates != null && _originalObjectStates.Count > 0) ||
                                     (_originalMaterialStates != null && _originalMaterialStates.Count > 0) ||
                                     (_originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0);
            
            if (_isPreviewActive && hasOriginalStates)
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
        /// EXTRAÍDO de MRAgruparObjetos.CancelPreview()
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
        /// NUEVO: Lógica de inicio extraída y simplificada
        /// </summary>
        private void StartPreview()
        {
            // Limpiar cualquier estado anterior inconsistente
            ClearSavedStates();
            
            // Guardar estados actuales usando los managers
            SaveOriginalStates();
            
            // Aplicar estados del frame usando los managers
            ApplyFrameStates();
            
            _isPreviewActive = true;
            
        }
        
        /// <summary>
        /// Aplica los estados del frame usando todos los managers
        /// NUEVO: Aplicación centralizada extraída de ApplyCurrentFrame()
        /// </summary>
        private void ApplyFrameStates()
        {
            // Aplicar estados usando los managers especializados
            _objectManager.ApplyObjectStates();
            _materialManager.ApplyMaterialStates();
            _blendshapeManager.ApplyBlendshapeStates();
            
        }
        
        
        
        /// <summary>
        /// Guarda los estados originales de todos los objetos, materiales y blendshapes
        /// EXTRAÍDO de MRAgruparObjetos.SaveOriginalStates()
        /// </summary>
        private void SaveOriginalStates()
        {
            // Guardar estados usando los managers especializados
            _originalObjectStates = _objectManager.CaptureCurrentStates();
            _originalMaterialStates = _materialManager.CaptureCurrentStates();
            _originalBlendshapeStates = _blendshapeManager.CaptureCurrentStates();
        }
        
        /// <summary>
        /// Restaura los estados originales de todos los objetos, materiales y blendshapes
        /// EXTRAÍDO de MRAgruparObjetos.RestoreOriginalStates()
        /// </summary>
        private void RestoreOriginalStates()
        {
            // Restaurar estados usando los managers especializados
            if (_originalObjectStates != null && _originalObjectStates.Count > 0)
            {
                _objectManager.RestoreStates(_originalObjectStates);
            }
            
            if (_originalMaterialStates != null && _originalMaterialStates.Count > 0)
            {
                _materialManager.RestoreStates(_originalMaterialStates);
            }
            
            if (_originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0)
            {
                _blendshapeManager.RestoreStates(_originalBlendshapeStates);
            }
            
            var restoredCount = (_originalObjectStates?.Count ?? 0) + 
                                (_originalMaterialStates?.Count ?? 0) + 
                                (_originalBlendshapeStates?.Count ?? 0);
            
            
            // Limpiar estados guardados
            ClearSavedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia todos los estados guardados
        /// NUEVO: Operación de limpieza centralizada
        /// </summary>
        private void ClearSavedStates()
        {
            if (_originalObjectStates == null) _originalObjectStates = new List<ObjectReference>();
            if (_originalMaterialStates == null) _originalMaterialStates = new List<MaterialReference>();
            if (_originalBlendshapeStates == null) _originalBlendshapeStates = new List<BlendshapeReference>();
            
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
        }
        
        
        
        /// <summary>
        /// Forza la aplicación del frame sin guardar estados (para uso externo)
        /// NUEVO: Operación de aplicación directa
        /// </summary>
        public void ForceApplyFrame()
        {
            ApplyFrameStates();
        }
        
        /// <summary>
        /// Restaura solo los objetos a su estado original
        /// NUEVO: Restauración selectiva
        /// </summary>
        public void RestoreObjectsOnly()
        {
            if (_originalObjectStates != null && _originalObjectStates.Count > 0)
            {
                _objectManager.RestoreStates(_originalObjectStates);
                _originalObjectStates.Clear();
            }
        }
        
        /// <summary>
        /// Restaura solo los materiales a su estado original
        /// NUEVO: Restauración selectiva
        /// </summary>
        public void RestoreMaterialsOnly()
        {
            if (_originalMaterialStates != null && _originalMaterialStates.Count > 0)
            {
                _materialManager.RestoreStates(_originalMaterialStates);
                _originalMaterialStates.Clear();
            }
        }
        
        /// <summary>
        /// Restaura solo los blendshapes a su estado original
        /// NUEVO: Restauración selectiva
        /// </summary>
        public void RestoreBlendshapesOnly()
        {
            if (_originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0)
            {
                _blendshapeManager.RestoreStates(_originalBlendshapeStates);
                _originalBlendshapeStates.Clear();
            }
        }
        
        /// <summary>
        /// Actualiza los estados guardados con los valores actuales (útil durante edición)
        /// NUEVO: Operación de actualización de estados
        /// </summary>
        public void UpdateSavedStates()
        {
            if (_isPreviewActive)
            {
                // Actualizar estados guardados sin cambiar el estado de preview
                SaveOriginalStates();
            }
        }
        
        /// <summary>
        /// Verifica si hay estados guardados inconsistentes
        /// NUEVO: Validación de integridad
        /// </summary>
        public bool HasInconsistentStates()
        {
            // Verificar si los estados guardados son consistentes con los managers actuales
            if (_isPreviewActive)
            {
                bool hasObjectStates = _originalObjectStates != null && _originalObjectStates.Count > 0;
                bool hasMaterialStates = _originalMaterialStates != null && _originalMaterialStates.Count > 0;
                bool hasBlendshapeStates = _originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0;
                
                bool hasObjectsInFrame = _objectManager.GetObjectCount() > 0;
                bool hasMaterialsInFrame = _materialManager.GetMaterialCount() > 0;
                bool hasBlendshapesInFrame = _blendshapeManager.GetBlendshapeCount() > 0;
                
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
        /// NUEVO: Auto-corrección de estados
        /// </summary>
        public void FixInconsistentStates()
        {
            if (HasInconsistentStates())
            {
                
                if (_isPreviewActive)
                {
                    // Cancelar preview y limpiar estados
                    _isPreviewActive = false;
                    ClearSavedStates();
                }
                
            }
        }
        
        
        
        /// <summary>
        /// Inicializa el servicio con estados limpios
        /// NUEVO: Método de inicialización
        /// </summary>
        public void Initialize()
        {
            ClearSavedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia el servicio y cancela cualquier preview activo
        /// NUEVO: Método de limpieza
        /// </summary>
        public void Cleanup()
        {
            if (_isPreviewActive)
            {
                CancelPreview();
            }
            ClearSavedStates();
        }
        
        /// <summary>
        /// Sincroniza el estado del servicio con estados serializados externos
        /// NUEVO: Sincronización con serialización
        /// </summary>
        /// <param name="isPreviewActive">Estado de preview externo</param>
        /// <param name="originalObjectStates">Estados de objetos externos</param>
        /// <param name="originalMaterialStates">Estados de materiales externos</param>
        /// <param name="originalBlendshapeStates">Estados de blendshapes externos</param>
        public void SyncWithSerializedStates(bool isPreviewActive, 
                                             List<ObjectReference> originalObjectStates,
                                             List<MaterialReference> originalMaterialStates,
                                             List<BlendshapeReference> originalBlendshapeStates)
        {
            _isPreviewActive = isPreviewActive;
            _originalObjectStates = originalObjectStates ?? new List<ObjectReference>();
            _originalMaterialStates = originalMaterialStates ?? new List<MaterialReference>();
            _originalBlendshapeStates = originalBlendshapeStates ?? new List<BlendshapeReference>();
            
            // Verificar consistencia después de sincronizar
            if (HasInconsistentStates())
            {
                FixInconsistentStates();
            }
        }
        
        
        
        
        
        
    }
    
}
