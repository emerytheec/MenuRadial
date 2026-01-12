using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Clase agregadora que centraliza el estado y dependencias del sistema RadialMenu
    /// Implementa el patrón Aggregate para reducir acoplamiento de MRUnificarObjetos
    /// </summary>
    public class RadialMenuState
    {
        private readonly string _componentName;
        private readonly MonoBehaviour _ownerComponent;
        
        // Gestores especializados
        private RadialFrameManager _frameManager;
        private RadialPropertyManager _propertyManager;
        private RadialPropertyNotifier _propertyNotifier;
        private RadialPreviewManager _previewManager;
        private RadialUnityIntegration _unityIntegration;
        
        // Estado interno
        private bool _isInitialized = false;
        
        /// <summary>
        /// Inicializa el estado del menú radial con dependencias controladas
        /// </summary>
        /// <param name="ownerComponent">Componente propietario</param>
        /// <param name="initialFrames">Lista inicial de frames</param>
        /// <param name="initialActiveFrameIndex">Índice inicial activo</param>
        /// <param name="animationName">Nombre inicial de animación</param>
        /// <param name="animationPath">Ruta inicial de animación</param>
        /// <param name="autoUpdatePaths">Estado inicial de auto-actualización</param>
        public RadialMenuState(MonoBehaviour ownerComponent, 
                              List<MRAgruparObjetos> initialFrames,
                              int initialActiveFrameIndex,
                              string animationName,
                              string animationPath,
                              bool autoUpdatePaths)
        {
            _ownerComponent = ownerComponent ?? throw new ArgumentNullException(nameof(ownerComponent));
            _componentName = _ownerComponent.name;
            
            Initialize(initialFrames, initialActiveFrameIndex, animationName, animationPath, autoUpdatePaths);
        }
        
        
        /// <summary>
        /// Gestor de frames
        /// </summary>
        public RadialFrameManager FrameManager => _frameManager;
        
        /// <summary>
        /// Gestor de propiedades
        /// </summary>
        public RadialPropertyManager PropertyManager => _propertyManager;
        
        /// <summary>
        /// Notificador de propiedades
        /// </summary>
        public RadialPropertyNotifier PropertyNotifier => _propertyNotifier;
        
        /// <summary>
        /// Gestor de preview
        /// </summary>
        public RadialPreviewManager PreviewManager => _previewManager;
        
        /// <summary>
        /// Integración con Unity
        /// </summary>
        public RadialUnityIntegration UnityIntegration => _unityIntegration;
        
        /// <summary>
        /// Indica si el estado está inicializado
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        
        // Métodos públicos
        
        /// <summary>
        /// Actualiza los frames del sistema
        /// </summary>
        /// <param name="frames">Nueva lista de frames</param>
        public void UpdateFrames(List<MRAgruparObjetos> frames)
        {
            if (!_isInitialized) return;
            
            // Nota: RadialFrameManager gestiona internamente la lista de frames
            // No necesita método UpdateFramesList, ya que trabaja por referencia
        }
        
        /// <summary>
        /// Actualiza las propiedades de animación
        /// </summary>
        /// <param name="animationName">Nuevo nombre de animación</param>
        /// <param name="animationPath">Nueva ruta de animación</param>
        /// <param name="autoUpdatePaths">Nuevo estado de auto-actualización</param>
        public void UpdateProperties(string animationName = null, string animationPath = null, bool? autoUpdatePaths = null)
        {
            if (!_isInitialized) return;
            
            if (animationName != null) _propertyManager.AnimationName = animationName;
            if (animationPath != null) _propertyManager.AnimationPath = animationPath;
            if (autoUpdatePaths.HasValue) _propertyManager.AutoUpdatePaths = autoUpdatePaths.Value;
        }
        
        /// <summary>
        /// Valida el estado completo del sistema
        /// </summary>
        /// <returns>Resultado de validación agregada</returns>
        public ValidationResult ValidateComplete()
        {
            if (!_isInitialized)
                return new ValidationResult("Sistema no inicializado", false, ValidationSeverity.Error);
            
            // Validar frames
            if (!_frameManager.HasValidFrames())
                return new ValidationResult("No hay frames válidos", false, ValidationSeverity.Error);
            
            // Validar propiedades
            if (!_propertyManager.ValidateAllProperties())
                return new ValidationResult("Propiedades inválidas", false, ValidationSeverity.Error);
            
            return new ValidationResult("Estado válido", true, ValidationSeverity.Info);
        }
        
        /// <summary>
        /// Limpia todos los recursos y estado
        /// </summary>
        public void Cleanup()
        {
            if (!_isInitialized) return;
            
            _unityIntegration?.HandleOnDestroy();
            _previewManager?.Cleanup();
            _propertyManager?.Cleanup();
            _frameManager = null;
            _propertyManager = null;
            _propertyNotifier = null;
            _previewManager = null;
            _unityIntegration = null;
            
            _isInitialized = false;
        }
        
        
        // Métodos privados
        
        /// <summary>
        /// Inicializa todos los gestores con las dependencias correctas
        /// </summary>
        private void Initialize(List<MRAgruparObjetos> frames, int activeFrameIndex, string animationName, string animationPath, bool autoUpdatePaths)
        {
            // Validaciones iniciales
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));
            
            // FASE 1: Gestores independientes
            _frameManager = new RadialFrameManager(frames, activeFrameIndex);
            _propertyManager = new RadialPropertyManager(_componentName, animationName, animationPath, autoUpdatePaths);
            _propertyNotifier = new RadialPropertyNotifier(_componentName, null);
            
            // FASE 2: Gestores con dependencias
            if (_frameManager != null)
            {
                _previewManager = new RadialPreviewManager(_frameManager, _componentName);
            }
            
            // FASE 3: Integración Unity (depende de todos los anteriores)
            if (_frameManager != null && _propertyManager != null && _previewManager != null)
            {
                _unityIntegration = new RadialUnityIntegration(
                    _frameManager,
                    null, // serviceCoordinator no disponible
                    _propertyManager,
                    _previewManager,
                    _ownerComponent
                );
            }
            
            // FASE 4: Conectar referencias cruzadas
            _propertyManager?.ConnectServiceCoordinator(null);
            
            _isInitialized = true;
        }
        
    }
}
