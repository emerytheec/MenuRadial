using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Controllers;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds
{
    /// <summary>
    /// MR Ajustar Bounds - Componente para unificar los bounds de todos los meshes de un avatar.
    ///
    /// Proceso:
    /// 1. Escanea el avatar buscando todos los SkinnedMeshRenderer
    /// 2. Calcula los bounds individuales de cada mesh
    /// 3. Encuentra los limites maximos globales del avatar
    /// 4. Aplica un bounding box unificado + margen a todos los meshes
    ///
    /// Esto previene que partes del avatar desaparezcan por culling prematuro.
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Ajustar Bounds")]
    public class MRAjustarBounds : MRComponentBase
    {
        #region Serialized Fields

        [Header("Avatar")]
        [SerializeField]
        [Tooltip("GameObject raiz del avatar")]
        private GameObject _avatarRoot;

        [Header("Configuracion")]
        [SerializeField]
        [Tooltip("Porcentaje de margen adicional (0.1 = 10%)")]
        [Range(0f, 0.5f)]
        private float _marginPercentage = 0.10f;

        [SerializeField]
        [Tooltip("Aplicar bounds automaticamente al detectar cambios")]
        private bool _autoApply = false;

        [Header("Particulas")]
        [SerializeField]
        [Tooltip("Incluir sistemas de particulas en el ajuste de bounds")]
        private bool _includeParticles = false;

        [SerializeField]
        [Tooltip("Porcentaje de margen para particulas (0.2 = 20%)")]
        [Range(0f, 1f)]
        private float _particleMarginPercentage = 0.20f;

        [Header("Meshes Detectados")]
        [SerializeField]
        private List<MeshBoundsInfo> _detectedMeshes = new List<MeshBoundsInfo>();

        [Header("Particulas Detectadas")]
        [SerializeField]
        private List<ParticleBoundsInfo> _detectedParticles = new List<ParticleBoundsInfo>();

        [Header("Resultado")]
        [SerializeField]
        private BoundsCalculationResult _lastCalculationResult;

        [SerializeField]
        private bool _boundsApplied = false;

        [SerializeField]
        private bool _particleBoundsApplied = false;

        #endregion

        #region Private Fields

        private BoundsCalculator _calculator;

        #endregion

        #region Properties

        /// <summary>
        /// Calculador de bounds (lazy loading)
        /// </summary>
        private BoundsCalculator Calculator => _calculator ??= new BoundsCalculator();

        /// <summary>
        /// GameObject raiz del avatar
        /// </summary>
        public GameObject AvatarRoot
        {
            get => _avatarRoot;
            set
            {
                if (_avatarRoot != value)
                {
                    _avatarRoot = value;
                    OnAvatarChanged();
                }
            }
        }

        /// <summary>
        /// Porcentaje de margen adicional
        /// </summary>
        public float MarginPercentage
        {
            get => _marginPercentage;
            set => _marginPercentage = Mathf.Clamp(value, 0f, 0.5f);
        }

        /// <summary>
        /// Aplicar automaticamente al detectar cambios
        /// </summary>
        public bool AutoApply
        {
            get => _autoApply;
            set => _autoApply = value;
        }

        /// <summary>
        /// Lista de meshes detectados
        /// </summary>
        public List<MeshBoundsInfo> DetectedMeshes => _detectedMeshes;

        /// <summary>
        /// Numero de meshes detectados
        /// </summary>
        public int DetectedMeshCount => _detectedMeshes?.Count ?? 0;

        /// <summary>
        /// Numero de meshes validos
        /// </summary>
        public int ValidMeshCount => _detectedMeshes?.Count(m => m.IsValid) ?? 0;

        /// <summary>
        /// Resultado del ultimo calculo
        /// </summary>
        public BoundsCalculationResult LastCalculationResult => _lastCalculationResult;

        /// <summary>
        /// Indica si los bounds han sido aplicados
        /// </summary>
        public bool BoundsApplied
        {
            get => _boundsApplied;
            private set => _boundsApplied = value;
        }

        /// <summary>
        /// Indica si hay un calculo exitoso disponible
        /// </summary>
        public bool HasValidCalculation => _lastCalculationResult != null && _lastCalculationResult.Success;

        /// <summary>
        /// Bounds unificados finales (con margen)
        /// </summary>
        public Bounds? UnifiedBounds => HasValidCalculation ? _lastCalculationResult.UnifiedBoundsWithMargin : null;

        #region Particle Properties

        /// <summary>
        /// Incluir particulas en el ajuste
        /// </summary>
        public bool IncludeParticles
        {
            get => _includeParticles;
            set => _includeParticles = value;
        }

        /// <summary>
        /// Porcentaje de margen para particulas
        /// </summary>
        public float ParticleMarginPercentage
        {
            get => _particleMarginPercentage;
            set => _particleMarginPercentage = Mathf.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Lista de particulas detectadas
        /// </summary>
        public List<ParticleBoundsInfo> DetectedParticles => _detectedParticles;

        /// <summary>
        /// Numero de particulas detectadas
        /// </summary>
        public int DetectedParticleCount => _detectedParticles?.Count ?? 0;

        /// <summary>
        /// Numero de particulas validas
        /// </summary>
        public int ValidParticleCount => _detectedParticles?.Count(p => p.IsValid) ?? 0;

        /// <summary>
        /// Indica si los bounds de particulas han sido aplicados
        /// </summary>
        public bool ParticleBoundsApplied
        {
            get => _particleBoundsApplied;
            private set => _particleBoundsApplied = value;
        }

        #endregion

        #endregion

        #region Lifecycle Methods

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            UpdateVersion("0.002");
            _detectedMeshes ??= new List<MeshBoundsInfo>();
            _detectedParticles ??= new List<ParticleBoundsInfo>();
        }

        protected override void CleanupComponent()
        {
            base.CleanupComponent();
            _calculator = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Llamado cuando cambia el avatar asignado
        /// </summary>
        public void OnAvatarChanged()
        {
            _detectedMeshes.Clear();
            _detectedParticles.Clear();
            _lastCalculationResult = null;
            _boundsApplied = false;
            _particleBoundsApplied = false;

            if (_avatarRoot == null)
            {
                return;
            }

            // Escanear avatar (meshes)
            ScanAvatar();

            // Escanear particulas si esta habilitado
            if (_includeParticles)
            {
                ScanParticles();
            }

            // Calcular bounds unificados
            CalculateBounds();

            // Calcular bounds de particulas si esta habilitado
            if (_includeParticles)
            {
                CalculateParticleBounds();
            }

            // Auto-aplicar si esta habilitado
            if (_autoApply && HasValidCalculation)
            {
                ApplyBounds();

                if (_includeParticles)
                {
                    ApplyParticleBounds();
                }
            }
        }

        /// <summary>
        /// Escanea el avatar buscando todos los SkinnedMeshRenderer
        /// </summary>
        public void ScanAvatar()
        {
            _detectedMeshes.Clear();

            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MRAjustarBounds] No hay avatar asignado");
                return;
            }

            _detectedMeshes = Calculator.ScanAvatar(_avatarRoot);
            Debug.Log($"[MRAjustarBounds] Detectados {_detectedMeshes.Count} meshes en '{_avatarRoot.name}'");
        }

        /// <summary>
        /// Calcula los bounds unificados
        /// </summary>
        public void CalculateBounds()
        {
            if (_avatarRoot == null || _detectedMeshes.Count == 0)
            {
                _lastCalculationResult = BoundsCalculationResult.CreateFailure("No hay avatar o meshes para calcular");
                return;
            }

            _lastCalculationResult = Calculator.CalculateUnifiedBounds(
                _detectedMeshes,
                _avatarRoot.transform,
                _marginPercentage
            );

            if (_lastCalculationResult.Success)
            {
                Debug.Log($"[MRAjustarBounds] Calculo exitoso: {_lastCalculationResult.GetSummary()}");
            }
            else
            {
                Debug.LogWarning($"[MRAjustarBounds] Calculo fallido: {_lastCalculationResult.GetSummary()}");
            }
        }

        /// <summary>
        /// Aplica los bounds unificados a todos los meshes
        /// </summary>
        public void ApplyBounds()
        {
            if (!HasValidCalculation)
            {
                Debug.LogWarning("[MRAjustarBounds] No hay calculo valido para aplicar");
                return;
            }

            int applied = Calculator.ApplyUnifiedBounds(_detectedMeshes, _lastCalculationResult.UnifiedBoundsWithMargin);
            _boundsApplied = applied > 0;

            Debug.Log($"[MRAjustarBounds] Bounds aplicados a {applied} meshes");
        }

        /// <summary>
        /// Restaura los bounds originales de todos los meshes
        /// </summary>
        public void RestoreBounds()
        {
            int restored = Calculator.RestoreOriginalBounds(_detectedMeshes);
            _boundsApplied = false;

            Debug.Log($"[MRAjustarBounds] Bounds originales restaurados en {restored} meshes");
        }

        /// <summary>
        /// Refresca la deteccion y calculo
        /// </summary>
        public void Refresh()
        {
            if (_avatarRoot != null)
            {
                bool wasApplied = _boundsApplied;
                bool wasParticlesApplied = _particleBoundsApplied;

                // Restaurar primero si estaban aplicados
                if (wasApplied)
                {
                    RestoreBounds();
                }
                if (wasParticlesApplied)
                {
                    RestoreParticleBounds();
                }

                // Re-escanear y recalcular meshes
                ScanAvatar();
                CalculateBounds();

                // Re-escanear y recalcular particulas si esta habilitado
                if (_includeParticles)
                {
                    ScanParticles();
                    CalculateParticleBounds();
                }

                // Re-aplicar si estaban aplicados
                if (wasApplied && HasValidCalculation)
                {
                    ApplyBounds();
                }
                if (wasParticlesApplied && _includeParticles)
                {
                    ApplyParticleBounds();
                }
            }
        }

        /// <summary>
        /// Valida las referencias de meshes
        /// </summary>
        public int ValidateMeshReferences()
        {
            return Calculator.ValidateMeshInfos(_detectedMeshes);
        }

        /// <summary>
        /// Elimina meshes invalidos de la lista
        /// </summary>
        public void RemoveInvalidMeshes()
        {
            int removed = _detectedMeshes.RemoveAll(m => !m.IsValid);
            if (removed > 0)
            {
                Debug.Log($"[MRAjustarBounds] Eliminados {removed} meshes invalidos");
            }
        }

        #endregion

        #region Particle Methods

        /// <summary>
        /// Escanea el avatar buscando todos los ParticleSystem
        /// </summary>
        public void ScanParticles()
        {
            _detectedParticles.Clear();

            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MRAjustarBounds] No hay avatar asignado");
                return;
            }

            _detectedParticles = Calculator.ScanParticles(_avatarRoot);
            Debug.Log($"[MRAjustarBounds] Detectadas {_detectedParticles.Count} particulas en '{_avatarRoot.name}'");
        }

        /// <summary>
        /// Calcula los bounds individuales para cada sistema de particulas
        /// </summary>
        public void CalculateParticleBounds()
        {
            if (_avatarRoot == null || _detectedParticles.Count == 0)
            {
                Debug.LogWarning("[MRAjustarBounds] No hay avatar o particulas para calcular");
                return;
            }

            int processed = Calculator.CalculateIndividualParticleBounds(
                _detectedParticles,
                _avatarRoot.transform,
                _particleMarginPercentage
            );

            Debug.Log($"[MRAjustarBounds] Bounds calculados para {processed} particulas");
        }

        /// <summary>
        /// Aplica los bounds calculados a todos los sistemas de particulas
        /// </summary>
        public void ApplyParticleBounds()
        {
            if (_detectedParticles.Count == 0)
            {
                Debug.LogWarning("[MRAjustarBounds] No hay particulas para aplicar bounds");
                return;
            }

            int applied = Calculator.ApplyParticleBounds(_detectedParticles);
            _particleBoundsApplied = applied > 0;

            Debug.Log($"[MRAjustarBounds] Bounds aplicados a {applied} particulas");
        }

        /// <summary>
        /// Restaura los bounds originales de todos los sistemas de particulas
        /// </summary>
        public void RestoreParticleBounds()
        {
            int restored = Calculator.RestoreParticleBounds(_detectedParticles);
            _particleBoundsApplied = false;

            Debug.Log($"[MRAjustarBounds] Bounds originales restaurados en {restored} particulas");
        }

        /// <summary>
        /// Valida las referencias de particulas
        /// </summary>
        public int ValidateParticleReferences()
        {
            return Calculator.ValidateParticleInfos(_detectedParticles);
        }

        /// <summary>
        /// Elimina particulas invalidas de la lista
        /// </summary>
        public void RemoveInvalidParticles()
        {
            int removed = _detectedParticles.RemoveAll(p => !p.IsValid);
            if (removed > 0)
            {
                Debug.Log($"[MRAjustarBounds] Eliminadas {removed} particulas invalidas");
            }
        }

        #endregion

        #region IValidatable Implementation

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validacion de avatar
            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Error("Arrastra tu avatar aqui"));
                return result;
            }

            // Validacion de meshes
            if (_detectedMeshes == null || _detectedMeshes.Count == 0)
            {
                result.AddChild(ValidationResult.Warning(
                    "No se detectaron meshes. Haz clic en 'Escanear' para buscar SkinnedMeshRenderer."));
                return result;
            }

            int validCount = ValidMeshCount;
            int totalCount = DetectedMeshCount;

            if (validCount == 0)
            {
                result.AddChild(ValidationResult.Error("No hay meshes validos"));
                return result;
            }

            if (validCount < totalCount)
            {
                result.AddChild(ValidationResult.Warning(
                    $"{totalCount - validCount} mesh(es) tienen referencias invalidas"));
            }

            // Validacion de calculo
            if (!HasValidCalculation)
            {
                result.AddChild(ValidationResult.Warning(
                    "Haz clic en 'Calcular' para obtener los bounds unificados"));
                return result;
            }

            // Estado final de meshes
            string status = _boundsApplied ? "aplicados" : "calculados (sin aplicar)";
            result.AddChild(ValidationResult.Success(
                $"{validCount} meshes, bounds {status}. " +
                $"Tamanio: {_lastCalculationResult.FinalSize.y:F2}m alto"));

            // Validacion de particulas si esta habilitado
            if (_includeParticles)
            {
                int validParticles = ValidParticleCount;
                int totalParticles = DetectedParticleCount;

                if (totalParticles == 0)
                {
                    result.AddChild(ValidationResult.Warning(
                        "No se detectaron particulas. Haz clic en 'Escanear' para buscar ParticleSystem."));
                }
                else if (validParticles == 0)
                {
                    result.AddChild(ValidationResult.Warning("No hay particulas validas"));
                }
                else
                {
                    string particleStatus = _particleBoundsApplied ? "aplicados" : "calculados";
                    result.AddChild(ValidationResult.Success(
                        $"{validParticles} particulas, bounds {particleStatus}"));
                }
            }

            return result;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Auto-refrescar si el avatar cambio
            if (_avatarRoot != null && _detectedMeshes.Count == 0)
            {
                OnAvatarChanged();
            }
        }
#endif

        #endregion
    }
}
