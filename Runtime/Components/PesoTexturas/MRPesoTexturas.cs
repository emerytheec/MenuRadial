using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas
{
    /// <summary>
    /// MR Peso Texturas - Componente para analizar y optimizar el peso de texturas del avatar.
    ///
    /// Funcionalidad:
    /// 1. Escanea todas las texturas del avatar y ropas detectadas
    /// 2. Calcula el peso estimado en VRAM usando compresion BC
    /// 3. Agrupa texturas por fuente (avatar base, ropas)
    /// 4. Permite reducir resoluciones por grupo o globalmente
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Peso Texturas")]
    public class MRPesoTexturas : MRComponentBase
    {
        #region Serialized Fields

        [Header("Avatar")]
        [SerializeField]
        [Tooltip("GameObject raiz del avatar")]
        private GameObject _avatarRoot;

        [Header("Opciones de Escaneo")]
        [SerializeField]
        [Tooltip("Incluir texturas del avatar base (body, head, etc.)")]
        private bool _includeAvatarBase = true;

        [SerializeField]
        [Tooltip("Incluir texturas de ropas detectadas por MRCoserRopa")]
        private bool _includeClothing = true;

        [SerializeField]
        [Tooltip("Incluir texturas de materiales alternativos (MRUnificarMateriales, MRAgruparMateriales)")]
        private bool _includeAlternativeMaterials = true;

        [Header("Grupos de Texturas")]
        [SerializeField]
        private List<TextureGroupEntry> _textureGroups = new List<TextureGroupEntry>();

        [Header("Estado")]
        [SerializeField]
        private bool _isScanned;

        [SerializeField]
        private long _totalEstimatedVRAM;

        [SerializeField]
        private int _totalTextureCount;

        #endregion

        #region Properties

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
                    ClearScanResults();
                }
            }
        }

        /// <summary>
        /// Incluir texturas del avatar base
        /// </summary>
        public bool IncludeAvatarBase
        {
            get => _includeAvatarBase;
            set => _includeAvatarBase = value;
        }

        /// <summary>
        /// Incluir texturas de ropas
        /// </summary>
        public bool IncludeClothing
        {
            get => _includeClothing;
            set => _includeClothing = value;
        }

        /// <summary>
        /// Incluir texturas de materiales alternativos
        /// </summary>
        public bool IncludeAlternativeMaterials
        {
            get => _includeAlternativeMaterials;
            set => _includeAlternativeMaterials = value;
        }

        /// <summary>
        /// Lista de grupos de texturas escaneados
        /// </summary>
        public List<TextureGroupEntry> TextureGroups => _textureGroups;

        /// <summary>
        /// Indica si ya se ha realizado un escaneo
        /// </summary>
        public bool IsScanned => _isScanned;

        /// <summary>
        /// Peso total estimado en VRAM (bytes) - incluye materiales alternativos
        /// </summary>
        public long TotalEstimatedVRAM => _totalEstimatedVRAM;

        /// <summary>
        /// Peso estimado solo de texturas actuales (sin materiales alternativos).
        /// Este valor deberia coincidir con lo que VRChat reporta.
        /// </summary>
        public long CurrentEstimatedVRAM
        {
            get
            {
                if (_textureGroups == null || _textureGroups.Count == 0)
                    return 0;
                return _textureGroups.Sum(g => g.CurrentEstimatedVRAM);
            }
        }

        /// <summary>
        /// Peso estimado solo de texturas de materiales alternativos
        /// </summary>
        public long AlternativeEstimatedVRAM
        {
            get
            {
                if (_textureGroups == null || _textureGroups.Count == 0)
                    return 0;
                return _textureGroups.Sum(g => g.AlternativeEstimatedVRAM);
            }
        }

        /// <summary>
        /// Etiqueta formateada del peso actual (sin alternativos)
        /// </summary>
        public string CurrentSizeLabel => VRChatTextureWeightCalculator.FormatBytes(CurrentEstimatedVRAM);

        /// <summary>
        /// Cantidad total de texturas escaneadas
        /// </summary>
        public int TotalTextureCount => _totalTextureCount;

        /// <summary>
        /// Cantidad de texturas actuales (sin materiales alternativos)
        /// </summary>
        public int CurrentTextureCount
        {
            get
            {
                if (_textureGroups == null || _textureGroups.Count == 0)
                    return 0;
                return _textureGroups.Sum(g => g.CurrentTextureCount);
            }
        }

        /// <summary>
        /// Cantidad de texturas de materiales alternativos
        /// </summary>
        public int AlternativeTextureCount
        {
            get
            {
                if (_textureGroups == null || _textureGroups.Count == 0)
                    return 0;
                return _textureGroups.Sum(g => g.AlternativeTextureCount);
            }
        }

        /// <summary>
        /// Etiqueta formateada del peso total
        /// </summary>
        public string TotalSizeLabel => VRChatTextureWeightCalculator.FormatBytes(_totalEstimatedVRAM);

        /// <summary>
        /// Resolucion maxima encontrada entre todas las texturas
        /// </summary>
        public int MaxResolutionFound
        {
            get
            {
                if (_textureGroups == null || _textureGroups.Count == 0)
                    return 0;

                int max = 0;
                foreach (var group in _textureGroups)
                {
                    if (group.MaxResolution > max)
                        max = group.MaxResolution;
                }
                return max;
            }
        }

        /// <summary>
        /// Cantidad de grupos escaneados
        /// </summary>
        public int GroupCount => _textureGroups?.Count ?? 0;

        /// <summary>
        /// Cantidad de grupos habilitados
        /// </summary>
        public int EnabledGroupCount => _textureGroups?.Count(g => g.IsEnabled) ?? 0;

        /// <summary>
        /// Indica si el peso total es alto
        /// </summary>
        public bool IsHighTotalWeight => VRChatTextureWeightCalculator.IsHighTotalWeight(_totalEstimatedVRAM);

        /// <summary>
        /// Indica si hay texturas para procesar
        /// </summary>
        public bool HasTextures => _totalTextureCount > 0;

        #endregion

        #region Lifecycle

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            UpdateVersion("0.001");
            _textureGroups ??= new List<TextureGroupEntry>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Limpia todos los resultados del escaneo
        /// </summary>
        public void ClearScanResults()
        {
            _textureGroups.Clear();
            _isScanned = false;
            _totalEstimatedVRAM = 0;
            _totalTextureCount = 0;
        }

        /// <summary>
        /// Agrega un grupo de texturas
        /// </summary>
        public void AddTextureGroup(TextureGroupEntry group)
        {
            if (group != null && !_textureGroups.Contains(group))
            {
                _textureGroups.Add(group);
                RecalculateTotal();
            }
        }

        /// <summary>
        /// Remueve un grupo de texturas
        /// </summary>
        public bool RemoveTextureGroup(TextureGroupEntry group)
        {
            bool removed = _textureGroups.Remove(group);
            if (removed)
            {
                RecalculateTotal();
            }
            return removed;
        }

        /// <summary>
        /// Recalcula los totales a partir de los grupos
        /// </summary>
        public void RecalculateTotal()
        {
            _totalEstimatedVRAM = 0;
            _totalTextureCount = 0;

            foreach (var group in _textureGroups)
            {
                _totalEstimatedVRAM += group.TotalEstimatedVRAM;
                _totalTextureCount += group.TextureCount;
            }
        }

        /// <summary>
        /// Marca el componente como escaneado
        /// </summary>
        public void MarkAsScanned()
        {
            _isScanned = true;
            RecalculateTotal();
        }

        /// <summary>
        /// Calcula el peso total despues de un step-down global
        /// </summary>
        public long GetTotalAfterStepDown()
        {
            long total = 0;
            foreach (var group in _textureGroups)
            {
                if (group.IsEnabled)
                    total += group.GetEstimateAfterStepDown();
                else
                    total += group.TotalEstimatedVRAM;
            }
            return total;
        }

        /// <summary>
        /// Calcula los bytes de ahorro potencial con un step-down global
        /// </summary>
        public long GetPotentialSavings()
        {
            return _totalEstimatedVRAM - GetTotalAfterStepDown();
        }

        /// <summary>
        /// Calcula el porcentaje de ahorro con un step-down global
        /// </summary>
        public float GetOverallSavingsPercentage()
        {
            return VRChatTextureWeightCalculator.CalculateSavingsPercentage(
                _totalEstimatedVRAM,
                GetTotalAfterStepDown());
        }

        /// <summary>
        /// Verifica si se puede realizar un step-down
        /// </summary>
        public bool CanStepDown()
        {
            return _textureGroups.Any(g => g.IsEnabled && g.CanStepDown());
        }

        /// <summary>
        /// Obtiene todos los grupos habilitados
        /// </summary>
        public IEnumerable<TextureGroupEntry> GetEnabledGroups()
        {
            return _textureGroups.Where(g => g.IsEnabled);
        }

        /// <summary>
        /// Obtiene todas las texturas de todos los grupos
        /// </summary>
        public IEnumerable<TextureEntry> GetAllTextures()
        {
            return _textureGroups.SelectMany(g => g.Textures);
        }

        /// <summary>
        /// Obtiene todas las texturas habilitadas (de grupos habilitados)
        /// </summary>
        public IEnumerable<TextureEntry> GetEnabledTextures()
        {
            return _textureGroups.Where(g => g.IsEnabled).SelectMany(g => g.Textures);
        }

        /// <summary>
        /// Obtiene las texturas pesadas de todos los grupos
        /// </summary>
        public IEnumerable<TextureEntry> GetHighWeightTextures()
        {
            return _textureGroups.SelectMany(g => g.GetHighWeightTextures());
        }

        /// <summary>
        /// Obtiene un resumen del estado actual
        /// </summary>
        public string GetSummary()
        {
            if (!_isScanned)
                return "Sin escanear";

            if (_totalTextureCount == 0)
                return "No se encontraron texturas";

            return $"{_totalTextureCount} texturas en {GroupCount} grupos, {TotalSizeLabel}";
        }

        /// <summary>
        /// Habilita todos los grupos
        /// </summary>
        public void EnableAllGroups()
        {
            foreach (var group in _textureGroups)
            {
                group.IsEnabled = true;
            }
        }

        /// <summary>
        /// Deshabilita todos los grupos
        /// </summary>
        public void DisableAllGroups()
        {
            foreach (var group in _textureGroups)
            {
                group.IsEnabled = false;
            }
        }

        /// <summary>
        /// Encuentra un grupo por nombre
        /// </summary>
        public TextureGroupEntry FindGroupByName(string name)
        {
            return _textureGroups.FirstOrDefault(g => g.SourceName == name);
        }

        /// <summary>
        /// Encuentra un grupo por GameObject
        /// </summary>
        public TextureGroupEntry FindGroupByObject(GameObject obj)
        {
            return _textureGroups.FirstOrDefault(g => g.SourceObject == obj);
        }

        /// <summary>
        /// Obtiene todas las texturas que no tienen Mip Streaming habilitado.
        /// VRChat requiere Mip Streaming para texturas con mipmaps.
        /// </summary>
        public IEnumerable<TextureEntry> GetTexturesWithoutMipStreaming()
        {
            return _textureGroups.SelectMany(g => g.Textures)
                .Where(t => t.HasMipmaps && !t.HasMipStreaming);
        }

        /// <summary>
        /// Cantidad de texturas sin Mip Streaming
        /// </summary>
        public int TexturesWithoutMipStreamingCount
        {
            get
            {
                int count = 0;
                foreach (var group in _textureGroups)
                {
                    foreach (var texture in group.Textures)
                    {
                        if (texture.HasMipmaps && !texture.HasMipStreaming)
                            count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Indica si hay texturas que requieren Mip Streaming
        /// </summary>
        public bool HasTexturesRequiringMipStreaming => TexturesWithoutMipStreamingCount > 0;

        #endregion

        #region Validation

        public override ValidationResult Validate()
        {
            var result = new ValidationResult("MRPesoTexturas Validation");

            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Warning("No se ha asignado un avatar."));
                return result;
            }

            if (!_isScanned)
            {
                result.AddChild(ValidationResult.Info("Escanea las texturas para ver el peso estimado."));
                return result;
            }

            if (_totalTextureCount == 0)
            {
                result.AddChild(ValidationResult.Info("No se encontraron texturas para analizar."));
                return result;
            }

            // Verificar peso total
            if (VRChatTextureWeightCalculator.IsHighTotalWeight(_totalEstimatedVRAM))
            {
                result.AddChild(ValidationResult.Warning(
                    $"Peso total de texturas alto: {TotalSizeLabel}. " +
                    "Considera reducir resoluciones para mejorar rendimiento."));
            }
            else
            {
                result.AddChild(ValidationResult.Success(
                    $"Peso de texturas: {TotalSizeLabel} ({_totalTextureCount} texturas)"));
            }

            // Verificar texturas pesadas individuales
            var heavyTextures = GetHighWeightTextures().ToList();
            if (heavyTextures.Count > 0)
            {
                result.AddChild(ValidationResult.Info(
                    $"{heavyTextures.Count} textura(s) superan los 10 MB individuales."));
            }

            // Verificar Mip Streaming (requerido por VRChat)
            int missingMipStreaming = TexturesWithoutMipStreamingCount;
            if (missingMipStreaming > 0)
            {
                result.AddChild(ValidationResult.Error(
                    $"{missingMipStreaming} textura(s) no tienen Mip Streaming habilitado. " +
                    "VRChat requiere Mip Streaming para subir el avatar."));
            }

            return result;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();
        }
#endif

        #endregion
    }
}
