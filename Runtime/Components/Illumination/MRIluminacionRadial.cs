using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Illumination.Controllers;
using Bender_Dios.MenuRadial.Shaders.Models;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Core.Preview;

namespace Bender_Dios.MenuRadial.Components.Illumination
{
    /// <summary>
    /// Componente MR Iluminación Radial (antes MRRadialIllumination)
    /// Control de iluminación radial en materiales lilToon, genera animaciones Linear
    /// </summary>
    [System.Serializable]
    [AddComponentMenu("MR/MR Iluminación Radial")]
    public class MRIluminacionRadial : MRComponentBase, IIlluminationComponent, IAnimationProvider, IPreviewable
    {
        [FormerlySerializedAs("rootObject")]
        [SerializeField] private GameObject _rootObject;
        [FormerlySerializedAs("autoUpdateRoutes")]
        [SerializeField] private bool _autoUpdatePaths = true;
        
        [FormerlySerializedAs("asUnlit")]
        [SerializeField, Range(0f, 1f)] private float _asUnlit = 0.5f;
        [FormerlySerializedAs("lightMaxLimit")]
        [SerializeField, Range(0f, 1f)] private float _lightMaxLimit = 1f;
        [FormerlySerializedAs("shadowBorder")]
        [SerializeField, Range(0f, 1f)] private float _shadowBorder = 0.05f;
        [FormerlySerializedAs("shadowStrength")]
        [SerializeField, Range(0f, 1f)] private float _shadowStrength = 0f;
        
        [FormerlySerializedAs("animationName")]
        [SerializeField] private string _animationName = "RadialIllumination";

        // Ruta de animación - controlada internamente por MRSlotInfoCollector desde MRMenuRadial.OutputPath
        private string _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        
        // Preview system fields
        [System.NonSerialized] private bool _isPreviewActive = false;
        [System.NonSerialized] private IlluminationProperties _originalProperties;
        [System.NonSerialized] private List<Material> _previewMaterials = new List<Material>();
        
        // Controladores (lazy loading)
        private IlluminationMaterialController _materialController;
        private IlluminationAnimationController _animationController;
        
        // Optimizaciones de cache para evitar operaciones redundantes
        [System.NonSerialized] private string _lastPathsHash = string.Empty;
        [System.NonSerialized] private GameObject _cachedRootObject;
        
        /// <summary>
        /// Objeto raíz desde donde buscar materiales
        /// </summary>
        public GameObject RootObject
        {
            get => _rootObject;
            set 
            {
                _rootObject = value;
                // Actualizar el controlador de animación si ya existe
                if (_animationController != null)
                {
                    _animationController.RootObject = value;
                }
            }
        }
        
        /// <summary>
        /// Lista de materiales detectados y compatibles
        /// </summary>
        public List<Material> DetectedMaterials => MaterialController.DetectedMaterials;
        
        /// <summary>
        /// Nombre de la animación a generar
        /// </summary>
        public string AnimationName
        {
            get => _animationName;
            set
            {
                _animationName = value;
                if (AnimationController != null)
                    AnimationController.AnimationName = value;
            }
        }
        
        /// <summary>
        /// Ruta donde guardar la animación
        /// </summary>
        public string AnimationPath
        {
            get => _animationPath;
            set
            {
                _animationPath = value;
                if (AnimationController != null)
                    AnimationController.AnimationPath = value;
            }
        }
        
        /// <summary>
        /// Propiedades de iluminación actuales
        /// </summary>
        public IlluminationProperties CurrentProperties => new IlluminationProperties(_asUnlit, _lightMaxLimit, _shadowBorder, _shadowStrength);
        
        /// <summary>
        /// Auto-actualizar rutas de materiales (consistencia con otros componentes MR)
        /// </summary>
        public bool AutoUpdatePaths { get => _autoUpdatePaths; set => _autoUpdatePaths = value; }

        /// <summary>
        /// Valor de As Unlit (0 = iluminación normal, 1 = completamente unlit)
        /// </summary>
        public float AsUnlit
        {
            get => _asUnlit;
            set => _asUnlit = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Valor de Light Max Limit
        /// </summary>
        public float LightMaxLimit
        {
            get => _lightMaxLimit;
            set => _lightMaxLimit = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Valor de Shadow Border
        /// </summary>
        public float ShadowBorder
        {
            get => _shadowBorder;
            set => _shadowBorder = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Valor de Shadow Strength
        /// </summary>
        public float ShadowStrength
        {
            get => _shadowStrength;
            set => _shadowStrength = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Controlador de materiales (lazy loading)
        /// </summary>
        private IlluminationMaterialController MaterialController
        {
            get
            {
                if (_materialController == null)
                {
                    _materialController = new IlluminationMaterialController();
                }
                return _materialController;
            }
        }
        
        /// <summary>
        /// Controlador de animaciones (lazy loading optimizado)
        /// </summary>
        private IlluminationAnimationController AnimationController
        {
            get
            {
                if (_animationController == null)
                {
                    _animationController = new IlluminationAnimationController(_animationName, _animationPath, RootObject);
                    _cachedRootObject = RootObject;
                }
                else if (_cachedRootObject != RootObject)
                {
                    // Solo actualizar RootObject si realmente cambió
                    _animationController.RootObject = RootObject;
                    _cachedRootObject = RootObject;
                }
                return _animationController;
            }
        }
        
        /// <summary>
        /// Inicialización del componente
        /// </summary>
        protected override void InitializeComponent()
        {
            UpdateVersion("0.001");
            
            // Configurar valores por defecto si es necesario
            if (string.IsNullOrEmpty(_animationName))
                _animationName = "RadialIllumination";
                
            if (string.IsNullOrEmpty(_animationPath))
                _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        }
        
        /// <summary>
        /// Escanea materiales compatibles desde el objeto raíz
        /// </summary>
        public void ScanMaterials()
        {
            if (RootObject == null)
            {
                return;
            }
            
            int materialsFound = MaterialController.ScanMaterials(RootObject);
        }
        
        /// <summary>
        /// Aplica los valores de iluminación actuales a todos los materiales
        /// </summary>
        public void ApplyValuesToAllMaterials()
        {
            MaterialController.CurrentProperties = CurrentProperties;
            int materialsApplied = MaterialController.ApplyPropertiesToAllMaterials();
            
        }
        
        /// <summary>
        /// Genera la animación de iluminación
        /// Automáticamente escanea materiales si no hay detectados
        /// </summary>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateIlluminationAnimation()
        {
            // Auto-escanear materiales si no hay detectados
            if (DetectedMaterials.Count == 0)
            {
                if (RootObject == null)
                {
                    Debug.LogWarning("[MRIluminacionRadial] No se puede generar: RootObject no está asignado");
                    return null;
                }

                ScanMaterials();

                if (DetectedMaterials.Count == 0)
                {
                    Debug.LogWarning("[MRIluminacionRadial] No se encontraron materiales lilToon compatibles");
                    return null;
                }
            }

            // Actualizar configuración del controlador de animación
            AnimationController.AnimationName = AnimationName;
            AnimationController.AnimationPath = AnimationPath;

            var clip = AnimationController.GenerateDefaultAnimation(DetectedMaterials, true);

            if (clip != null)
            {
                Debug.Log($"[MRIluminacionRadial] Animación generada: {AnimationName} con {DetectedMaterials.Count} materiales");
            }

            return clip;
        }
        
        /// <summary>
        /// Limpia la lista de materiales detectados
        /// </summary>
        public void ClearDetectedMaterials()
        {
            MaterialController.ClearDetectedMaterials();
        }
        
        /// <summary>
        /// Recalcula las rutas de los materiales detectados (optimizado con detección de cambios)
        /// NUEVO: Para funcionalidad Auto-actualizar Rutas
        /// </summary>
        public void RecalculatePaths()
        {
            if (RootObject == null)
            {
                return;
            }
            
            // OPTIMIZACIÓN 1: Detectar cambios antes de reescanear
            string currentPathsHash = GeneratePathsHash();
            if (currentPathsHash == _lastPathsHash && DetectedMaterials.Count > 0)
            {
                return; // Sin cambios detectados, evitar reescaneo
            }
            
            // Validación defensiva: Verificar que el RootObject aún existe
            if (RootObject == null || RootObject.Equals(null))
            {
                return;
            }
            
            // Re-escanear materiales desde el objeto raíz actualizado
            int previousCount = DetectedMaterials.Count;
            
            // Limpiar y volver a escanear
            ClearDetectedMaterials();
            ScanMaterials();
            
            int newCount = DetectedMaterials.Count;
            
            // Actualizar hash de paths después del escaneo exitoso
            _lastPathsHash = currentPathsHash;
            
            // Si hay materiales detectados, actualizar el controlador de animación
            if (newCount > 0 && _animationController != null)
            {
                _animationController.RootObject = RootObject;
                _cachedRootObject = RootObject; // Sincronizar cache
            }
        }
        
        /// <summary>
        /// Validación del componente
        /// </summary>
        /// <returns>Resultado de la validación</returns>
        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validar objeto raíz
            if (RootObject == null)
            {
                result.AddChild(ValidationResult.Error("Se requiere un objeto raíz para buscar materiales"));
            }
            
            // Validar nombre de animación
            if (string.IsNullOrEmpty(AnimationName))
            {
                result.AddChild(ValidationResult.Warning("Nombre de animación vacío, se usará nombre por defecto"));
            }
            
            // Validar ruta de animación
            if (string.IsNullOrEmpty(AnimationPath))
            {
                result.AddChild(ValidationResult.Warning("Ruta de animación vacía, se usará ruta por defecto"));
            }
            
            // Validar materiales si ya se ha hecho escaneo
            if (DetectedMaterials.Count == 0 && RootObject != null)
            {
                result.AddChild(ValidationResult.Info("No se han detectado materiales. Ejecuta 'Buscar Materiales' para escanear"));
            }
            
            // Validar rango de valores
            if (_asUnlit < 0f || _asUnlit > 1f)
            {
                result.AddChild(ValidationResult.Warning("AsUnlit fuera del rango válido (0-1)"));
            }
            
            if (_lightMaxLimit < 0f || _lightMaxLimit > 1f)
            {
                result.AddChild(ValidationResult.Warning("LightMaxLimit fuera del rango válido (0-1)"));
            }
            
            if (_shadowBorder < 0f || _shadowBorder > 1f)
            {
                result.AddChild(ValidationResult.Warning("ShadowBorder fuera del rango válido (0-1)"));
            }
            
            if (_shadowStrength < 0f || _shadowStrength > 1f)
            {
                result.AddChild(ValidationResult.Warning("ShadowStrength fuera del rango válido (0-1)"));
            }
            
            return result;
        }
        
        /// <summary>
        /// Obtiene estadísticas de materiales
        /// </summary>
        /// <returns>Estadísticas de materiales detectados</returns>
        public string GetMaterialStats()
        {
            if (RootObject == null) return "No hay objeto raíz asignado";
            
            var stats = MaterialController.GetMaterialStats(RootObject);
            return stats.ToString();
        }
        
        
        /// <summary>
        /// Aplica propiedades específicas del frame
        /// </summary>
        /// <param name="frameType">Tipo de frame (0, 127, 255)</param>
        public void ApplyFrameProperties(int frameType)
        {
            IlluminationProperties properties = frameType switch
            {
                0 => IlluminationProperties.CreateFrame0(),
                127 => IlluminationProperties.CreateFrame127(),
                255 => IlluminationProperties.CreateFrame255(),
                _ => CurrentProperties
            };
            
            _asUnlit = properties.AsUnlit;
            _lightMaxLimit = properties.LightMaxLimit;
            _shadowBorder = properties.ShadowBorder;
            _shadowStrength = properties.ShadowStrength;
            
        }
        
        
        
        /// <summary>
        /// Tipo de animación que genera este componente
        /// MRIluminacionRadial siempre genera animaciones de tipo Linear
        /// </summary>
        public AnimationType AnimationType => AnimationType.Linear;
        
        /// <summary>
        /// Indica si el componente puede generar animaciones
        /// </summary>
        public bool CanGenerateAnimation => RootObject != null && 
                                          DetectedMaterials.Count > 0 && 
                                          !string.IsNullOrEmpty(AnimationName);
        
        /// <summary>
        /// Descripción del tipo de animación 
        /// </summary>
        /// <returns>Descripción legible del tipo de animación</returns>
        public string GetAnimationTypeDescription()
        {
            return $"Animación de Iluminación Lineal ({DetectedMaterials.Count} materiales) - Parámetro Float";
        }
        
        
        
        /// <summary>
        /// Indica si el sistema de previsualización está activo
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        /// <summary>
        /// Obtiene el tipo de previsualización para este componente
        /// Illumination siempre es tipo Illumination (especial)
        /// </summary>
        /// <returns>PreviewType.Illumination</returns>
        public PreviewType GetPreviewType()
        {
            return PreviewType.Illumination;
        }
        
        /// <summary>
        /// Activa el sistema de previsualización
        /// Ejecuta automáticamente ScanMaterials() y ApplyValuesToAllMaterials()
        /// </summary>
        public void ActivatePreview()
        {
            if (_isPreviewActive)
            {
                return;
            }
            
            // OPTIMIZACIÓN 3: Validación defensiva mejorada
            if (RootObject == null || RootObject.Equals(null))
            {
                return;
            }
            
            // Guardar propiedades originales
            _originalProperties = CurrentProperties;
            
            // Ejecutar automáticamente ScanMaterials (equivalente al botón manual)
            ScanMaterials();
            
            if (DetectedMaterials.Count == 0)
            {
                return;
            }
            
            // OPTIMIZACIÓN 3: Validar que los materiales detectados aún existen
            var validMaterials = new List<Material>();
            foreach (var material in DetectedMaterials)
            {
                if (material != null && !material.Equals(null))
                {
                    validMaterials.Add(material);
                }
            }
            
            if (validMaterials.Count == 0)
            {
                return;
            }
            
            // Guardar lista de materiales válidos para posterior restauración
            _previewMaterials.Clear();
            _previewMaterials.AddRange(validMaterials);
            
            // Ejecutar automáticamente ApplyValuesToAllMaterials (equivalente al botón manual)
            ApplyValuesToAllMaterials();
            
            // Marcar como activo solo si todo fue exitoso
            _isPreviewActive = true;
            
            // Registrar en el PreviewManager
            PreviewManager.RegisterComponent(this);
        }
        
        /// <summary>
        /// Desactiva el sistema de previsualización y restaura propiedades originales
        /// </summary>
        public void DeactivatePreview()
        {
            if (!_isPreviewActive)
            {
                return;
            }
            
            // OPTIMIZACIÓN 2: Validación defensiva sin try-catch silencioso
            // Restaurar propiedades originales en los materiales
            if (_originalProperties != null && _previewMaterials.Count > 0)
            {
                // Validar que los materiales aún existen antes de restaurar
                var validMaterials = new List<Material>();
                foreach (var material in _previewMaterials)
                {
                    if (material != null && !material.Equals(null))
                    {
                        validMaterials.Add(material);
                    }
                }
                
                if (validMaterials.Count > 0)
                {
                    // Aplicar propiedades originales
                    _asUnlit = _originalProperties.AsUnlit;
                    _lightMaxLimit = _originalProperties.LightMaxLimit;
                    _shadowBorder = _originalProperties.ShadowBorder;
                    _shadowStrength = _originalProperties.ShadowStrength;
                    
                    // Aplicar a materiales válidos
                    MaterialController.CurrentProperties = _originalProperties;
                    MaterialController.ApplyPropertiesToAllMaterials();
                }
            }
            
            // Limpiar estado siempre
            _isPreviewActive = false;
            _originalProperties = null;
            _previewMaterials.Clear();
        }
        
        
        /// <summary>
        /// Método público para alternar el preview (útil para testing)
        /// </summary>
        public void TogglePreview()
        {
            if (_isPreviewActive)
                DeactivatePreview();
            else
                ActivatePreview();
        }
        
        /// <summary>
        /// Método para aplicar un frame específico de iluminación durante preview
        /// </summary>
        /// <param name="frameType">Tipo de frame (0, 127, 255)</param>
        public void SetPreviewFrame(int frameType)
        {
            if (!_isPreviewActive)
            {
                return;
            }
            
            // Aplicar propiedades del frame específico
            ApplyFrameProperties(frameType);
            
            // Aplicar a materiales
            ApplyValuesToAllMaterials();
        }
        
        
        
        /// <summary>
        /// Desregistrar del PreviewManager al destruir el componente
        /// </summary>
        private void OnDestroy()
        {
            if (_isPreviewActive)
            {
                PreviewManager.UnregisterComponent(this);
            }
        }
        
        
#if UNITY_EDITOR
        /// <summary>
        /// Validaciones específicas para el editor
        /// </summary>
        protected override void ValidateInEditor()
        {
            // Validar que los valores estén en rango
            _asUnlit = Mathf.Clamp01(_asUnlit);
            _lightMaxLimit = Mathf.Clamp01(_lightMaxLimit);
            _shadowBorder = Mathf.Clamp01(_shadowBorder);
            _shadowStrength = Mathf.Clamp01(_shadowStrength);
            
            // Validar strings
            if (string.IsNullOrEmpty(_animationName))
                _animationName = "RadialIllumination";
                
            if (string.IsNullOrEmpty(_animationPath))
                _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        }
#endif

        /// <summary>
        /// Genera hash de paths para detección de cambios en RecalculatePaths()
        /// </summary>
        private string GeneratePathsHash()
        {
            if (RootObject == null)
                return string.Empty;
                
            // Hash basado en instancia del RootObject y nombre de animación
            var hashComponents = new string[]
            {
                RootObject.GetInstanceID().ToString(),
                RootObject.name,
                _animationName ?? string.Empty,
                _animationPath ?? string.Empty
            };
            
            return string.Join("|", hashComponents);
        }
    }
}
