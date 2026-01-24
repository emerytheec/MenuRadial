using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas.Models
{
    /// <summary>
    /// Tipo de fuente de un grupo de texturas
    /// </summary>
    public enum TextureGroupType
    {
        /// <summary>
        /// Texturas del avatar base (body, head, etc.)
        /// </summary>
        AvatarBase,

        /// <summary>
        /// Texturas de una ropa/outfit
        /// </summary>
        Clothing,

        /// <summary>
        /// Texturas de materiales alternativos (MRUnificarMateriales/MRAgruparMateriales)
        /// </summary>
        AlternativeMaterials
    }

    /// <summary>
    /// Agrupa texturas por fuente (avatar base o ropa especifica).
    /// Permite calcular el peso total del grupo y realizar step-down masivo.
    /// </summary>
    [Serializable]
    public class TextureGroupEntry
    {
        #region Serialized Fields

        [SerializeField]
        private string _sourceName;

        [SerializeField]
        private GameObject _sourceObject;

        [SerializeField]
        private TextureGroupType _groupType;

        [SerializeField]
        private List<TextureEntry> _textures = new List<TextureEntry>();

        [SerializeField]
        private bool _isExpanded = false;

        [SerializeField]
        private bool _isEnabled = true;

        #endregion

        #region Properties

        /// <summary>
        /// Nombre de la fuente del grupo (nombre del avatar o ropa)
        /// </summary>
        public string SourceName
        {
            get => _sourceName;
            set => _sourceName = value;
        }

        /// <summary>
        /// GameObject de origen del grupo
        /// </summary>
        public GameObject SourceObject
        {
            get => _sourceObject;
            set => _sourceObject = value;
        }

        /// <summary>
        /// Tipo de grupo (AvatarBase o Clothing)
        /// </summary>
        public TextureGroupType GroupType
        {
            get => _groupType;
            set => _groupType = value;
        }

        /// <summary>
        /// Lista de texturas en este grupo
        /// </summary>
        public List<TextureEntry> Textures => _textures;

        /// <summary>
        /// Si el foldout esta expandido en el inspector
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => _isExpanded = value;
        }

        /// <summary>
        /// Si este grupo esta habilitado para step-down
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Cantidad de texturas en el grupo
        /// </summary>
        public int TextureCount => _textures?.Count ?? 0;

        /// <summary>
        /// Peso total estimado del grupo en VRAM (bytes) - incluye materiales alternativos
        /// </summary>
        public long TotalEstimatedVRAM => _textures?.Sum(t => t.EstimatedVRAMBytes) ?? 0;

        /// <summary>
        /// Peso estimado solo de texturas actuales (sin materiales alternativos).
        /// Este valor deberia coincidir con lo que VRChat reporta.
        /// </summary>
        public long CurrentEstimatedVRAM => _textures?.Where(t => !t.IsFromAlternativeMaterial).Sum(t => t.EstimatedVRAMBytes) ?? 0;

        /// <summary>
        /// Peso estimado solo de texturas de materiales alternativos
        /// </summary>
        public long AlternativeEstimatedVRAM => _textures?.Where(t => t.IsFromAlternativeMaterial).Sum(t => t.EstimatedVRAMBytes) ?? 0;

        /// <summary>
        /// Cantidad de texturas de materiales actuales (no alternativos)
        /// </summary>
        public int CurrentTextureCount => _textures?.Count(t => !t.IsFromAlternativeMaterial) ?? 0;

        /// <summary>
        /// Cantidad de texturas de materiales alternativos
        /// </summary>
        public int AlternativeTextureCount => _textures?.Count(t => t.IsFromAlternativeMaterial) ?? 0;

        /// <summary>
        /// Resolucion maxima encontrada en el grupo
        /// </summary>
        public int MaxResolution => _textures?.Max(t => t.CurrentMaxSize) ?? 0;

        /// <summary>
        /// Resolucion minima encontrada en el grupo
        /// </summary>
        public int MinResolution => _textures?.Count > 0 ? _textures.Min(t => t.CurrentMaxSize) : 0;

        /// <summary>
        /// Indica si el grupo tiene texturas validas
        /// </summary>
        public bool HasValidTextures => _textures?.Any(t => t.IsValid) ?? false;

        /// <summary>
        /// Etiqueta de peso total formateada
        /// </summary>
        public string TotalWeightLabel => VRChatTextureWeightCalculator.FormatBytes(TotalEstimatedVRAM);

        /// <summary>
        /// Indica si el grupo tiene texturas pesadas
        /// </summary>
        public bool HasHighWeightTextures => _textures?.Any(t => VRChatTextureWeightCalculator.IsHighWeight(t.EstimatedVRAMBytes)) ?? false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor por defecto para serializacion
        /// </summary>
        public TextureGroupEntry()
        {
            _textures = new List<TextureEntry>();
        }

        /// <summary>
        /// Constructor con parametros basicos
        /// </summary>
        public TextureGroupEntry(string sourceName, GameObject sourceObject, TextureGroupType groupType)
        {
            _sourceName = sourceName;
            _sourceObject = sourceObject;
            _groupType = groupType;
            _textures = new List<TextureEntry>();
            _isExpanded = false;
            _isEnabled = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Agrega una textura al grupo
        /// </summary>
        public void AddTexture(TextureEntry texture)
        {
            if (texture != null && !_textures.Contains(texture))
            {
                _textures.Add(texture);
            }
        }

        /// <summary>
        /// Remueve una textura del grupo
        /// </summary>
        public bool RemoveTexture(TextureEntry texture)
        {
            return _textures.Remove(texture);
        }

        /// <summary>
        /// Limpia todas las texturas del grupo
        /// </summary>
        public void ClearTextures()
        {
            _textures.Clear();
        }

        /// <summary>
        /// Recalcula las estimaciones de todas las texturas del grupo
        /// </summary>
        public void RecalculateAll()
        {
            foreach (var texture in _textures)
            {
                texture.RecalculateEstimate();
            }
        }

        /// <summary>
        /// Calcula el peso estimado despues de un step-down.
        /// Solo considera las texturas con la resolucion maxima del grupo,
        /// ya que son las unicas que se reduciran.
        /// </summary>
        /// <returns>Peso estimado en bytes despues del step-down</returns>
        public long GetEstimateAfterStepDown()
        {
            if (_textures == null || _textures.Count == 0)
                return 0;

            int maxResolution = MaxResolution;
            long total = 0;

            foreach (var texture in _textures)
            {
                if (texture.CurrentMaxSize == maxResolution)
                {
                    // Solo las texturas con resolucion maxima bajaran
                    int nextSize = texture.GetNextStepDownSize();
                    total += texture.GetEstimateAtSize(nextSize);
                }
                else
                {
                    // Las demas mantienen su peso actual
                    total += texture.EstimatedVRAMBytes;
                }
            }
            return total;
        }

        /// <summary>
        /// Calcula los bytes que se ahorrarian con un step-down global
        /// </summary>
        /// <returns>Bytes de ahorro potencial</returns>
        public long GetPotentialSavings()
        {
            return TotalEstimatedVRAM - GetEstimateAfterStepDown();
        }

        /// <summary>
        /// Calcula el porcentaje de ahorro con un step-down global
        /// </summary>
        /// <returns>Porcentaje de ahorro (0-100)</returns>
        public float GetSavingsPercentage()
        {
            return VRChatTextureWeightCalculator.CalculateSavingsPercentage(
                TotalEstimatedVRAM,
                GetEstimateAfterStepDown());
        }

        /// <summary>
        /// Obtiene las texturas ordenadas por peso (mayor a menor)
        /// </summary>
        public IEnumerable<TextureEntry> GetTexturesByWeight()
        {
            return _textures.OrderByDescending(t => t.EstimatedVRAMBytes);
        }

        /// <summary>
        /// Obtiene las texturas que superan el umbral de peso alto
        /// </summary>
        public IEnumerable<TextureEntry> GetHighWeightTextures()
        {
            return _textures.Where(t => VRChatTextureWeightCalculator.IsHighWeight(t.EstimatedVRAMBytes));
        }

        /// <summary>
        /// Verifica si alguna textura puede reducirse mas
        /// </summary>
        public bool CanStepDown()
        {
            return _textures.Any(t => t.GetNextStepDownSize() < t.CurrentMaxSize);
        }

        /// <summary>
        /// Obtiene el conteo de texturas por resolucion
        /// </summary>
        public Dictionary<int, int> GetResolutionDistribution()
        {
            var distribution = new Dictionary<int, int>();

            foreach (var texture in _textures)
            {
                int size = texture.CurrentMaxSize;
                if (distribution.ContainsKey(size))
                    distribution[size]++;
                else
                    distribution[size] = 1;
            }

            return distribution;
        }

        /// <summary>
        /// Obtiene un resumen del grupo para mostrar
        /// </summary>
        public string GetSummary()
        {
            if (TextureCount == 0)
                return "Sin texturas";

            return $"{TextureCount} texturas, {TotalWeightLabel}, max {MaxResolution}px";
        }

        #endregion
    }
}
