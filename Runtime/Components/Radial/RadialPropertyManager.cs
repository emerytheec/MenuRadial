using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Gestor de propiedades de animación para menús radiales.
    /// Maneja nombre, ruta y auto-update con validación y notificaciones.
    /// </summary>
    public class RadialPropertyManager
    {
        private string _animationName;
        private string _animationPath;
        private bool _autoUpdatePaths;

        public event Action<string> OnAnimationNameChanged;
        public event Action<string> OnAnimationPathChanged;

        public RadialPropertyManager(string componentName,
                                   string initialAnimationName = "RadialToggle",
                                   string initialAnimationPath = null,
                                   bool initialAutoUpdatePaths = true)
        {
            _animationName = initialAnimationName ?? "RadialToggle";
            _animationPath = NormalizePath(initialAnimationPath ?? MRConstants.ANIMATION_OUTPUT_PATH);
            _autoUpdatePaths = initialAutoUpdatePaths;
        }

        public string AnimationName
        {
            get => _animationName;
            set
            {
                if (value == _animationName || string.IsNullOrEmpty(value)) return;
                _animationName = value;
                OnAnimationNameChanged?.Invoke(_animationName);
            }
        }

        public string AnimationPath
        {
            get => _animationPath;
            set
            {
                var normalized = NormalizePath(value);
                if (normalized == _animationPath) return;
                if (!normalized.StartsWith("Assets/")) return;
                _animationPath = normalized;
                OnAnimationPathChanged?.Invoke(_animationPath);
            }
        }

        public bool AutoUpdatePaths
        {
            get => _autoUpdatePaths;
            set => _autoUpdatePaths = value;
        }

        public bool ValidateAllProperties()
        {
            return !string.IsNullOrEmpty(_animationName)
                && !string.IsNullOrEmpty(_animationPath)
                && _animationPath.StartsWith("Assets/");
        }

        public void ResetToDefaults()
        {
            _animationName = "RadialToggle";
            _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
            _autoUpdatePaths = true;
            OnAnimationNameChanged?.Invoke(_animationName);
            OnAnimationPathChanged?.Invoke(_animationPath);
        }

        public void AutoUpdatePathFromHierarchy(string gameObjectPath)
        {
            if (!_autoUpdatePaths || string.IsNullOrEmpty(gameObjectPath))
                return;

            var hierarchyPath = NormalizePath(
                $"{MRConstants.ANIMATION_OUTPUT_PATH}{gameObjectPath.Replace('/', '_')}/");

            if (hierarchyPath != _animationPath)
                AnimationPath = hierarchyPath;
        }

        public void Cleanup()
        {
            OnAnimationNameChanged = null;
            OnAnimationPathChanged = null;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return MRConstants.ANIMATION_OUTPUT_PATH;
            path = path.Replace('\\', '/');
            if (!path.EndsWith("/"))
                path += "/";
            return path;
        }
    }
}
