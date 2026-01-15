using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.AnimationSystem.Interfaces;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Services;
using Bender_Dios.MenuRadial.Shaders;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.AnimationSystem.Services
{
    /// <summary>
    /// Generador de animaciones de iluminación para materiales compatibles
    /// </summary>
    [MRService(typeof(IIlluminationAnimationGenerator))]
    public partial class IlluminationAnimationGenerator : IIlluminationAnimationGenerator
    {
        /// <summary>
        /// Configuración de animación - usa constantes centralizadas
        /// </summary>
        private static class AnimationConfig
        {
            public const float TotalDuration = MRAnimationConstants.TOTAL_DURATION;
            public const float FrameRate = MRAnimationConstants.FRAME_RATE;
            public const int TotalFrames = MRAnimationConstants.TOTAL_FRAMES;
        }
        
        /// <summary>
        /// Genera una animación de iluminación con keyframes predefinidos
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="keyframes">Keyframes de iluminación</param>
        /// <param name="savePath">Ruta donde guardar (opcional)</param>
        /// <param name="rootObject">Objeto raíz para limitar la búsqueda (opcional)</param>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateIlluminationAnimation(
            string animationName,
            List<Material> materials,
            IlluminationKeyframe[] keyframes,
            string savePath = null,
            GameObject rootObject = null)
        {
            if (string.IsNullOrEmpty(animationName))
                animationName = "RadialIllumination";
                
            if (materials == null || materials.Count == 0)
            {
                return null;
            }
            
            if (keyframes == null || keyframes.Length == 0)
            {
                return null;
            }
            
            // Validar materiales
            if (!ValidateMaterials(materials))
            {
                return null;
            }
            
            var clip = CreateAnimationClip(animationName);
            
            // Establecer objeto raíz para búsqueda
            _searchRootObject = rootObject;
            
            // Limpiar cache de rutas si cambió el objeto raíz
            _rendererPathCache.Clear();
            
            // Generar curvas para cada material
            foreach (var material in materials.Where(m => m != null))
            {
                AddMaterialCurves(clip, material, keyframes);
            }
            
            
            // Guardar el archivo si se especifica una ruta
            if (!string.IsNullOrEmpty(savePath))
            {
                SaveAnimationClip(clip, savePath, animationName);
            }
            
            return clip;
        }
        
        // Campo para almacenar el objeto raíz de búsqueda
        private GameObject _searchRootObject;
        
        // Cache para rutas de renderers para mejorar performance
        private readonly Dictionary<Renderer, string> _rendererPathCache = new Dictionary<Renderer, string>();
        
        /// <summary>
        /// Genera una animación de iluminación con keyframes por defecto
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="savePath">Ruta donde guardar (opcional)</param>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateDefaultIlluminationAnimation(
            string animationName,
            List<Material> materials,
            string savePath = null,
            GameObject rootObject = null)
        {
            var defaultKeyframes = IlluminationKeyframe.CreateDefaultKeyframes();
            return GenerateIlluminationAnimation(animationName, materials, defaultKeyframes, savePath, rootObject);
        }
        
        /// <summary>
        /// Valida que los materiales sean compatibles para animación
        /// </summary>
        /// <param name="materials">Materiales a validar</param>
        /// <returns>True si todos son compatibles</returns>
        public bool ValidateMaterials(List<Material> materials)
        {
            if (materials == null || materials.Count == 0) return false;
            
            var factory = ShaderStrategyFactory.Instance;
            
            foreach (var material in materials)
            {
                if (material == null) continue;
                
                if (!factory.IsCompatible(material))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Crea un AnimationClip configurado correctamente
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <returns>AnimationClip configurado</returns>
        private AnimationClip CreateAnimationClip(string animationName)
        {
            var clip = new AnimationClip
            {
                name = animationName,
                frameRate = AnimationConfig.FrameRate,
                wrapMode = WrapMode.Clamp
            };
            
            // Configurar duración fija (delegado a implementación Editor)
            ConfigureClipSettings(clip);
            
            return clip;
        }
        
        /// <summary>
        /// Añade curvas de animación para un material específico
        /// </summary>
        /// <param name="clip">AnimationClip al cual añadir curvas</param>
        /// <param name="material">Material a animar</param>
        /// <param name="keyframes">Keyframes de iluminación</param>
        private void AddMaterialCurves(AnimationClip clip, Material material, IlluminationKeyframe[] keyframes)
        {
            var strategy = ShaderStrategyFactory.Instance.GetStrategyForMaterial(material);
            if (strategy == null) 
            {
                return;
            }
            
            var propertyNames = strategy.GetPropertyNames();
            
            // Encontrar todos los renderers que usan este material
            var renderersWithMaterial = FindRenderersUsingMaterial(material);
            
            if (renderersWithMaterial.Count == 0)
            {
                return;
            }
            
            foreach (var rendererInfo in renderersWithMaterial)
            {
                foreach (var propertyName in propertyNames)
                {
                    var animationCurve = CreatePropertyCurve(propertyName, keyframes);
                    if (animationCurve != null && animationCurve.keys.Length > 0)
                    {
                        // Crear binding correcto para Renderer.material
                        var binding = CreateMaterialBinding(rendererInfo.renderer, rendererInfo.materialIndex, propertyName);
                        
                        // Aplicar curva al clip (delegado a implementación Editor)
                        SetEditorCurve(clip, binding, animationCurve);
                        
                    }
                }
            }
        }
        
        /// <summary>
        /// Renderer y índice de material
        /// </summary>
        private struct RendererMaterialInfo
        {
            public Renderer renderer;
            public int materialIndex;
            public string path;
        }
        
        /// <summary>
        /// Encuentra todos los renderers que usan un material específico
        /// </summary>
        /// <param name="targetMaterial">Material a buscar</param>
        /// <returns>Lista de renderers con información de índice</returns>
        private List<RendererMaterialInfo> FindRenderersUsingMaterial(Material targetMaterial)
        {
            var result = new List<RendererMaterialInfo>();
            
            // Buscar en el objeto raíz si está definido, sino en toda la escena
            Renderer[] allRenderers;
            
            if (_searchRootObject != null)
            {
                allRenderers = _searchRootObject.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                allRenderers = Object.FindObjectsOfType<Renderer>();
            }
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;
                
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == targetMaterial)
                    {
                        var rendererPath = GetRendererPath(renderer);
                        result.Add(new RendererMaterialInfo
                        {
                            renderer = renderer,
                            materialIndex = i,
                            path = rendererPath
                        });
                        
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Obtiene la ruta jerárquica de un renderer
        /// </summary>
        /// <param name="renderer">Renderer del cual obtener la ruta</param>
        /// <returns>Ruta jerárquica del renderer</returns>
        private string GetRendererPath(Renderer renderer)
        {
            if (renderer == null) return "";
            
            // Verificar cache primero
            if (_rendererPathCache.TryGetValue(renderer, out var cachedPath))
            {
                return cachedPath;
            }
            
            string path;
            
            // Si hay un objeto raíz definido, generar ruta relativa desde ese objeto
            if (_searchRootObject != null)
            {
                path = GetRelativePathFromRoot(renderer.transform, _searchRootObject.transform);
            }
            else
            {
                // Si no hay objeto raíz, generar ruta completa
                path = renderer.name;
                var parent = renderer.transform.parent;
                
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }
            
            // Guardar en cache
            _rendererPathCache[renderer] = path;
            
            return path;
        }
        
        /// <summary>
        /// Obtiene la ruta relativa desde un objeto raíz
        /// </summary>
        /// <param name="target">Transform objetivo</param>
        /// <param name="root">Transform raíz</param>
        /// <returns>Ruta relativa desde el objeto raíz</returns>
        private string GetRelativePathFromRoot(Transform target, Transform root)
        {
            if (target == null || root == null) return "";
            
            // Si el target es el mismo que el root, no hay ruta
            if (target == root) return "";
            
            var pathParts = new List<string>();
            var current = target;
            
            // Construir la ruta desde el target hasta el root
            while (current != null && current != root)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }
            
            // Si no llegamos al root, el target no es hijo del root
            if (current != root)
            {
                // Retornar ruta completa como fallback
                return target.name;
            }
            
            // Invertir la lista para tener la ruta desde root hacia target
            pathParts.Reverse();
            
            return string.Join("/", pathParts);
        }
        
        /// <summary>
        /// Crea una curva de animación para una propiedad específica
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <param name="keyframes">Keyframes de iluminación</param>
        /// <returns>AnimationCurve generada</returns>
        private AnimationCurve CreatePropertyCurve(string propertyName, IlluminationKeyframe[] keyframes)
        {
            var curve = new AnimationCurve();
            
            foreach (var keyframe in keyframes)
            {
                if (keyframe?.Properties == null) continue;
                
                float value = GetPropertyValue(keyframe.Properties, propertyName);
                var animKeyframe = new Keyframe(keyframe.Time, value)
                {
                    inTangent = 0f,
                    outTangent = 0f,
                    tangentMode = 0
                };
                
                curve.AddKey(animKeyframe);
            }
            
            return curve;
        }
        
        /// <summary>
        /// Obtiene el valor de una propiedad específica
        /// </summary>
        /// <param name="properties">Propiedades de iluminación</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <returns>Valor de la propiedad</returns>
        private float GetPropertyValue(IlluminationProperties properties, string propertyName)
        {
            return propertyName switch
            {
                MRShaderProperties.AS_UNLIT => properties.AsUnlit,
                MRShaderProperties.LIGHT_MAX_LIMIT => properties.LightMaxLimit,
                MRShaderProperties.SHADOW_BORDER => properties.ShadowBorder,
                MRShaderProperties.SHADOW_STRENGTH => properties.ShadowStrength,
                _ => 0f
            };
        }
        
        /// <summary>
        /// Crea un binding para propiedades de material en un renderer
        /// </summary>
        /// <param name="renderer">Renderer que contiene el material</param>
        /// <param name="materialIndex">Índice del material en el renderer</param>
        /// <param name="propertyName">Nombre de la propiedad del shader</param>
        /// <returns>Binding configurado</returns>
        private object CreateMaterialBinding(Renderer renderer, int materialIndex, string propertyName)
        {
            // Intentar usar factory Editor si está disponible
            if (_editorBindingFactory != null)
            {
                return _editorBindingFactory(renderer, materialIndex, propertyName);
            }
            
            // Fallback para Runtime
            return CreateFallbackBinding(renderer, propertyName);
        }
        
        /// <summary>
        /// Crea binding de fallback para Runtime
        /// </summary>
        private (string path, System.Type type, string propertyName) CreateFallbackBinding(Renderer renderer, string propertyName)
        {
            var path = GetRendererPath(renderer);
            return (path, typeof(Renderer), $"material.{propertyName}");
        }
        
        /// <summary>
        /// Guarda el AnimationClip en disco
        /// </summary>
        /// <param name="clip">Clip a guardar</param>
        /// <param name="savePath">Ruta base donde guardar</param>
        /// <param name="animationName">Nombre de la animación</param>
        private void SaveAnimationClip(AnimationClip clip, string savePath, string animationName)
        {
            if (_editorClipSaver != null)
            {
                _editorClipSaver(clip, savePath, animationName);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Configura settings del clip (delegado a implementación Editor)
        /// </summary>
        private void ConfigureClipSettings(AnimationClip clip)
        {
            if (_editorClipConfigurator != null)
            {
                _editorClipConfigurator(clip);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Aplica curva al clip (delegado a implementación Editor)
        /// </summary>
        private void SetEditorCurve(AnimationClip clip, object binding, AnimationCurve curve)
        {
            if (_editorCurveApplicator != null)
            {
                _editorCurveApplicator(clip, binding, curve);
            }
            else
            {
            }
        }
        
        
        /// <summary>
        /// Delegate para crear binding de material Editor-specific
        /// </summary>
        private System.Func<Renderer, int, string, object> _editorBindingFactory;
        
        /// <summary>
        /// Delegate para configurar clip settings Editor-specific
        /// </summary>
        private System.Action<AnimationClip> _editorClipConfigurator;
        
        /// <summary>
        /// Delegate para aplicar curvas Editor-specific
        /// </summary>
        private System.Action<AnimationClip, object, AnimationCurve> _editorCurveApplicator;
        
        /// <summary>
        /// Delegate para guardar clips Editor-specific
        /// </summary>
        private System.Action<AnimationClip, string, string> _editorClipSaver;
        
        /// <summary>
        /// Configura el factory de binding para Editor
        /// </summary>
        public void SetEditorBindingFactory(System.Func<Renderer, int, string, object> factory)
        {
            _editorBindingFactory = factory;
        }
        
        /// <summary>
        /// Configura el configurador de clip para Editor
        /// </summary>
        public void SetEditorClipConfigurator(System.Action<AnimationClip> configurator)
        {
            _editorClipConfigurator = configurator;
        }
        
        /// <summary>
        /// Configura el aplicador de curvas para Editor
        /// </summary>
        public void SetEditorCurveApplicator(System.Action<AnimationClip, object, AnimationCurve> applicator)
        {
            _editorCurveApplicator = applicator;
        }
        
        /// <summary>
        /// Configura el guardador de clips para Editor
        /// </summary>
        public void SetEditorClipSaver(System.Action<AnimationClip, string, string> saver)
        {
            _editorClipSaver = saver;
        }
        
        // Métodos delegate reemplazan partial methods para evitar problemas de compilación
        
    }
}
