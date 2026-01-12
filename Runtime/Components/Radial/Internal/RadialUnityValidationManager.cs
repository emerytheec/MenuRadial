using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial.Internal
{
    /// <summary>
    /// Gestor especializado para validación de Unity en menús radiales
    /// Extraído de RadialUnityIntegration para cumplir con principio de responsabilidad única
    /// </summary>
    public class RadialUnityValidationManager
    {
        private readonly RadialFrameManager _frameManager;
        private readonly RadialPropertyManager _propertyManager;
        private readonly MonoBehaviour _ownerComponent;
        
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        public RadialUnityValidationManager(RadialFrameManager frameManager,
                                          RadialPropertyManager propertyManager,
                                          MonoBehaviour ownerComponent)
        {
            _frameManager = frameManager ?? throw new ArgumentNullException(nameof(frameManager));
            _propertyManager = propertyManager ?? throw new ArgumentNullException(nameof(propertyManager));
            _ownerComponent = ownerComponent ?? throw new ArgumentNullException(nameof(ownerComponent));
        }
        
        /// <summary>
        /// Maneja el evento OnValidate() del componente
        /// </summary>
        public void HandleOnValidate()
        {
            if (_frameManager == null) throw new InvalidOperationException("FrameManager no ha sido inicializado");
            if (_propertyManager == null) throw new InvalidOperationException("PropertyManager no ha sido inicializado");
            if (_ownerComponent == null) throw new InvalidOperationException("OwnerComponent no ha sido configurado");
            
            ValidateFrames();
            ValidateProperties();
            AutoUpdateIfEnabled();
            CleanupInvalidReferences();
        }
        
        /// <summary>
        /// Valida la configuración completa del componente
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public ValidationResult ValidateComplete()
        {
            var result = new ValidationResult 
            { 
                IsValid = true, 
                Message = "Validación completa de Unity Integration" 
            };
            
            // Validar frames
            var frameValidation = ValidateFramesInternal();
            result.AddChild(frameValidation);
            
            // Validar propiedades
            var propertyValidation = ValidatePropertiesInternal();
            result.AddChild(propertyValidation);
            
            // Validar configuración del componente
            var componentValidation = ValidateComponentConfiguration();
            result.AddChild(componentValidation);
            
            return result;
        }
        
        /// <summary>
        /// Valida que el componente tenga la configuración mínima requerida
        /// </summary>
        /// <returns>True si la configuración es válida</returns>
        public bool HasValidMinimalConfiguration()
        {
            return _ownerComponent != null &&
                   _frameManager != null &&
                   _propertyManager != null &&
                   !string.IsNullOrEmpty(_propertyManager?.AnimationName);
        }
        
        
        private void ValidateFrames()
        {
            if (_frameManager?.FrameCount == 0)
            {
                // Advertir sobre falta de frames
            }
        }
        
        private void ValidateProperties()
        {
            if (!_propertyManager?.ValidateAllProperties() == true)
            {
                // Advertir sobre propiedades inválidas
            }
        }
        
        private void AutoUpdateIfEnabled()
        {
            if (_propertyManager?.AutoUpdatePaths == true)
            {
                _frameManager?.CleanupInvalidFrames();
            }
        }
        
        private void CleanupInvalidReferences()
        {
            _frameManager?.CleanupInvalidFrames();
        }
        
        private ValidationResult ValidateFramesInternal()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación de frames" };
            
            if (_frameManager?.FrameCount == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay frames configurados"));
            }
            else
            {
                result.AddChild(ValidationResult.Success($"Frames válidos: {_frameManager.FrameCount}"));
            }
            
            return result;
        }
        
        private ValidationResult ValidatePropertiesInternal()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación de propiedades" };
            
            if (_propertyManager == null)
            {
                result.AddChild(ValidationResult.Error("PropertyManager no inicializado"));
                return result;
            }
            
            if (string.IsNullOrEmpty(_propertyManager.AnimationName))
            {
                result.AddChild(ValidationResult.Error("Nombre de animación requerido"));
            }
            
            if (string.IsNullOrEmpty(_propertyManager.AnimationPath))
            {
                result.AddChild(ValidationResult.Warning("Ruta de animación vacía"));
            }
            
            return result;
        }
        
        private ValidationResult ValidateComponentConfiguration()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación de configuración" };
            
            if (_ownerComponent == null)
            {
                result.AddChild(ValidationResult.Error("Componente propietario no configurado"));
            }
            else
            {
                result.AddChild(ValidationResult.Success("Configuración básica válida"));
            }
            
            return result;
        }
        
    }
}