using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Clase agregadora que centraliza el estado y dependencias del sistema RadialMenu.
    /// </summary>
    public class RadialMenuState
    {
        private readonly string _componentName;

        private RadialFrameManager _frameManager;
        private RadialPropertyManager _propertyManager;
        private RadialPreviewManager _previewManager;

        private bool _isInitialized = false;

        public RadialMenuState(MonoBehaviour ownerComponent,
                              List<MRAgruparObjetos> initialFrames,
                              int initialActiveFrameIndex,
                              string animationName,
                              string animationPath,
                              bool autoUpdatePaths)
        {
            if (ownerComponent == null) throw new ArgumentNullException(nameof(ownerComponent));
            _componentName = ownerComponent.name;

            if (initialFrames == null) throw new ArgumentNullException(nameof(initialFrames));

            _frameManager = new RadialFrameManager(initialFrames, initialActiveFrameIndex);
            _propertyManager = new RadialPropertyManager(_componentName, animationName, animationPath, autoUpdatePaths);
            _previewManager = new RadialPreviewManager(_frameManager, _componentName);

            _isInitialized = true;
        }

        public bool IsInitialized => _isInitialized;

        public void UpdateFrames(List<MRAgruparObjetos> frames)
        {
            // RadialFrameManager trabaja por referencia, no necesita actualización explícita
        }

        public void UpdateProperties(string animationName = null, string animationPath = null, bool? autoUpdatePaths = null)
        {
            if (!_isInitialized) return;

            if (animationName != null) _propertyManager.AnimationName = animationName;
            if (animationPath != null) _propertyManager.AnimationPath = animationPath;
            if (autoUpdatePaths.HasValue) _propertyManager.AutoUpdatePaths = autoUpdatePaths.Value;
        }

        public ValidationResult ValidateComplete()
        {
            if (!_isInitialized)
                return new ValidationResult("Sistema no inicializado", false, ValidationSeverity.Error);

            if (!_frameManager.HasValidFrames())
                return new ValidationResult("No hay frames válidos", false, ValidationSeverity.Error);

            if (!_propertyManager.ValidateAllProperties())
                return new ValidationResult("Propiedades inválidas", false, ValidationSeverity.Error);

            return new ValidationResult("Estado válido", true, ValidationSeverity.Info);
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;

            _previewManager?.Cleanup();
            _propertyManager?.Cleanup();
            _frameManager = null;
            _propertyManager = null;
            _previewManager = null;

            _isInitialized = false;
        }
    }
}
