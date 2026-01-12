using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.AnimationSystem.Interfaces;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Services;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Components.Illumination.Controllers
{
    /// <summary>
    /// Controlador para generación de animaciones de iluminación
    /// </summary>
    public class IlluminationAnimationController
    {
        private string _animationName = "RadialIllumination";
        private string _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        private IlluminationKeyframe[] _keyframes;
        private IIlluminationAnimationGenerator _animationGenerator;
        private GameObject _rootObject; // AGREGADO: Para limitar la búsqueda de meshes
        
        /// <summary>
        /// Nombre de la animación a generar
        /// </summary>
        public string AnimationName
        {
            get => _animationName;
            set => _animationName = string.IsNullOrEmpty(value) ? "RadialIllumination" : value;
        }
        
        /// <summary>
        /// Ruta donde guardar la animación
        /// </summary>
        public string AnimationPath
        {
            get => _animationPath;
            set => _animationPath = string.IsNullOrEmpty(value) ? MRConstants.ANIMATION_OUTPUT_PATH : value;
        }
        
        /// <summary>
        /// Objeto raíz para limitar la búsqueda de renderers
        /// </summary>
        public GameObject RootObject
        {
            get => _rootObject;
            set => _rootObject = value;
        }
        
        /// <summary>
        /// Keyframes de iluminación para la animación
        /// </summary>
        public IlluminationKeyframe[] Keyframes
        {
            get => _keyframes ?? (_keyframes = IlluminationKeyframe.CreateDefaultKeyframes());
            set => _keyframes = value ?? IlluminationKeyframe.CreateDefaultKeyframes();
        }
        
        /// <summary>
        /// Generador de animaciones (lazy loading)
        /// </summary>
        private IIlluminationAnimationGenerator AnimationGenerator
        {
            get
            {
                if (_animationGenerator == null)
                    _animationGenerator = MenuRadialServiceBootstrap.GetService<IIlluminationAnimationGenerator>();
                return _animationGenerator;
            }
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public IlluminationAnimationController()
        {
            _animationName = "RadialIllumination";
            _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
            _keyframes = IlluminationKeyframe.CreateDefaultKeyframes();
            _rootObject = null;
        }
        
        /// <summary>
        /// Constructor con parámetros
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="animationPath">Ruta de la animación</param>
        /// <param name="rootObject">Objeto raíz para búsqueda (opcional)</param>
        public IlluminationAnimationController(string animationName, string animationPath, GameObject rootObject = null)
        {
            AnimationName = animationName;
            AnimationPath = animationPath;
            RootObject = rootObject;
            _keyframes = IlluminationKeyframe.CreateDefaultKeyframes();
        }
        
        /// <summary>
        /// Genera la animación de iluminación con los materiales especificados
        /// </summary>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="saveToFile">Si debe guardar el archivo en disco</param>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateAnimation(List<Material> materials, bool saveToFile = true)
        {
            if (materials == null || materials.Count == 0)
            {
                return null;
            }
            
            if (AnimationGenerator == null)
            {
                return null;
            }
            
            string savePath = saveToFile ? AnimationPath : null;
            
            // CORREGIDO: Pasar el rootObject al generador
            var clip = AnimationGenerator.GenerateIlluminationAnimation(
                AnimationName,
                materials,
                Keyframes,
                savePath,
                RootObject  // CRÍTICO: Esto es lo que faltaba
            );
            
            if (clip != null)
            {
            }
            else
            {
            }
            
            return clip;
        }
        
        /// <summary>
        /// Genera la animación con keyframes por defecto
        /// </summary>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="saveToFile">Si debe guardar el archivo en disco</param>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateDefaultAnimation(List<Material> materials, bool saveToFile = true)
        {
            if (AnimationGenerator == null)
            {
                return null;
            }
            
            string savePath = saveToFile ? AnimationPath : null;
            
            // CORREGIDO: Pasar el rootObject al generador
            var clip = AnimationGenerator.GenerateDefaultIlluminationAnimation(
                AnimationName,
                materials,
                savePath,
                RootObject  // CRÍTICO: Esto es lo que faltaba
            );
            
            if (clip != null)
            {
            }
            
            return clip;
        }
        
        /// <summary>
        /// Valida que los materiales sean compatibles para animación
        /// </summary>
        /// <param name="materials">Materiales a validar</param>
        /// <returns>True si todos son compatibles</returns>
        public bool ValidateMaterials(List<Material> materials)
        {
            if (AnimationGenerator == null) return false;
            
            return AnimationGenerator.ValidateMaterials(materials);
        }
        
        /// <summary>
        /// Configura keyframes personalizados
        /// </summary>
        /// <param name="frame0Properties">Propiedades para frame 0</param>
        /// <param name="frame127Properties">Propiedades para frame 127</param>
        /// <param name="frame255Properties">Propiedades para frame 255</param>
        public void SetCustomKeyframes(
            IlluminationProperties frame0Properties,
            IlluminationProperties frame127Properties,
            IlluminationProperties frame255Properties)
        {
            _keyframes = new[]
            {
                new IlluminationKeyframe(0f, frame0Properties ?? IlluminationProperties.CreateFrame0()),
                new IlluminationKeyframe(127f / 60f, frame127Properties ?? IlluminationProperties.CreateFrame127()),
                new IlluminationKeyframe(255f / 60f, frame255Properties ?? IlluminationProperties.CreateFrame255())
            };
            
        }
        
        /// <summary>
        /// Restaura los keyframes por defecto
        /// </summary>
        public void ResetToDefaultKeyframes()
        {
            _keyframes = IlluminationKeyframe.CreateDefaultKeyframes();
        }
        
        
        /// <summary>
        /// Verifica si la configuración es válida
        /// </summary>
        /// <returns>True si la configuración es válida</returns>
        public bool IsConfigurationValid()
        {
            if (string.IsNullOrEmpty(AnimationName))
            {
                return false;
            }
            
            if (string.IsNullOrEmpty(AnimationPath))
            {
                return false;
            }
            
            if (Keyframes == null || Keyframes.Length == 0)
            {
                return false;
            }
            
            if (AnimationGenerator == null)
            {
                return false;
            }
            
            return true;
        }
        
    }
}
