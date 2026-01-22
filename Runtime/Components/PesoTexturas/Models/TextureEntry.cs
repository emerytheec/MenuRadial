using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas.Models
{
    /// <summary>
    /// Tipo de textura para calculo de compresion VRAM.
    /// </summary>
    public enum TextureCompressionType
    {
        /// <summary>
        /// Textura estandar (diffuse, etc.) - BC1 sin alpha, BC3/BC7 con alpha
        /// </summary>
        Default,

        /// <summary>
        /// Normal map - siempre usa BC5 (1.0 byte/pixel)
        /// </summary>
        NormalMap,

        /// <summary>
        /// Textura de un solo canal (mask, height) - BC4 (0.5 byte/pixel)
        /// </summary>
        SingleChannel
    }

    /// <summary>
    /// Almacena informacion de una textura individual para el calculo de peso VRAM.
    /// </summary>
    [Serializable]
    public class TextureEntry
    {
        #region Serialized Fields

        [SerializeField]
        private Texture2D _texture;

        [SerializeField]
        private string _assetPath;

        [SerializeField]
        private int _originalWidth;

        [SerializeField]
        private int _originalHeight;

        [SerializeField]
        private int _currentMaxSize;

        [SerializeField]
        private bool _hasAlpha;

        [SerializeField]
        private bool _hasMipmaps;

        [SerializeField]
        private bool _hasMipStreaming;

        [SerializeField]
        private long _estimatedVRAMBytes;

        [SerializeField]
        private string _textureName;

        [SerializeField]
        private bool _isFromAlternativeMaterial;

        [SerializeField]
        private TextureCompressionType _compressionType;

        #endregion

        #region Properties

        /// <summary>
        /// Referencia a la textura
        /// </summary>
        public Texture2D Texture => _texture;

        /// <summary>
        /// Ruta del asset en el proyecto
        /// </summary>
        public string AssetPath => _assetPath;

        /// <summary>
        /// Ancho original de la textura
        /// </summary>
        public int OriginalWidth => _originalWidth;

        /// <summary>
        /// Alto original de la textura
        /// </summary>
        public int OriginalHeight => _originalHeight;

        /// <summary>
        /// Tamanio maximo actual configurado en el TextureImporter
        /// </summary>
        public int CurrentMaxSize
        {
            get => _currentMaxSize;
            set => _currentMaxSize = value;
        }

        /// <summary>
        /// Indica si la textura tiene canal alpha
        /// </summary>
        public bool HasAlpha => _hasAlpha;

        /// <summary>
        /// Indica si la textura tiene mipmaps habilitados
        /// </summary>
        public bool HasMipmaps => _hasMipmaps;

        /// <summary>
        /// Indica si la textura tiene Mip Streaming habilitado (requerido por VRChat)
        /// </summary>
        public bool HasMipStreaming
        {
            get => _hasMipStreaming;
            set => _hasMipStreaming = value;
        }

        /// <summary>
        /// Peso estimado en VRAM (bytes)
        /// </summary>
        public long EstimatedVRAMBytes
        {
            get => _estimatedVRAMBytes;
            set => _estimatedVRAMBytes = value;
        }

        /// <summary>
        /// Nombre de la textura para mostrar
        /// </summary>
        public string TextureName => _textureName;

        /// <summary>
        /// Indica si esta textura proviene de un material alternativo.
        /// Las texturas de materiales alternativos no se incluyen en el bundle de VRChat
        /// a menos que esten asignadas a un renderer.
        /// </summary>
        public bool IsFromAlternativeMaterial
        {
            get => _isFromAlternativeMaterial;
            set => _isFromAlternativeMaterial = value;
        }

        /// <summary>
        /// Tipo de compresion de la textura (afecta el calculo de VRAM).
        /// Normal maps usan BC5 (1.0 byte/pixel), single channel usa BC4 (0.5 byte/pixel).
        /// </summary>
        public TextureCompressionType CompressionType
        {
            get => _compressionType;
            set => _compressionType = value;
        }

        /// <summary>
        /// Indica si es un normal map
        /// </summary>
        public bool IsNormalMap => _compressionType == TextureCompressionType.NormalMap;

        /// <summary>
        /// Resolucion efectiva (limitada por CurrentMaxSize)
        /// </summary>
        public int EffectiveWidth => Mathf.Min(_originalWidth, _currentMaxSize);

        /// <summary>
        /// Resolucion efectiva (limitada por CurrentMaxSize)
        /// </summary>
        public int EffectiveHeight => Mathf.Min(_originalHeight, _currentMaxSize);

        /// <summary>
        /// Indica si la textura es valida
        /// </summary>
        public bool IsValid => _texture != null && !string.IsNullOrEmpty(_assetPath);

        /// <summary>
        /// Etiqueta de resolucion para mostrar (ej: "2048x2048")
        /// </summary>
        public string ResolutionLabel => $"{EffectiveWidth}x{EffectiveHeight}";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor por defecto para serializacion
        /// </summary>
        public TextureEntry()
        {
        }

        /// <summary>
        /// Constructor con todos los parametros
        /// </summary>
        public TextureEntry(
            Texture2D texture,
            string assetPath,
            int originalWidth,
            int originalHeight,
            int currentMaxSize,
            bool hasAlpha,
            bool hasMipmaps,
            bool hasMipStreaming = true,
            bool isFromAlternativeMaterial = false,
            TextureCompressionType compressionType = TextureCompressionType.Default)
        {
            _texture = texture;
            _assetPath = assetPath;
            _originalWidth = originalWidth;
            _originalHeight = originalHeight;
            _currentMaxSize = currentMaxSize;
            _hasAlpha = hasAlpha;
            _hasMipmaps = hasMipmaps;
            _hasMipStreaming = hasMipStreaming;
            _isFromAlternativeMaterial = isFromAlternativeMaterial;
            _compressionType = compressionType;
            _textureName = texture != null ? texture.name : System.IO.Path.GetFileNameWithoutExtension(assetPath);

            RecalculateEstimate();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Recalcula el peso estimado basado en la configuracion actual
        /// </summary>
        public void RecalculateEstimate()
        {
            _estimatedVRAMBytes = VRChatTextureWeightCalculator.CalculateVRAMBytes(
                EffectiveWidth,
                EffectiveHeight,
                _hasAlpha,
                _hasMipmaps,
                _compressionType);
        }

        /// <summary>
        /// Obtiene el siguiente tamanio de step-down (reduccion de resolucion)
        /// </summary>
        /// <returns>El siguiente tamanio menor, o el actual si ya es el minimo</returns>
        public int GetNextStepDownSize()
        {
            return VRChatTextureWeightCalculator.GetNextStepDown(_currentMaxSize);
        }

        /// <summary>
        /// Calcula el peso estimado para un tamanio especifico
        /// </summary>
        /// <param name="maxSize">Tamanio maximo a simular</param>
        /// <returns>Peso estimado en bytes</returns>
        public long GetEstimateAtSize(int maxSize)
        {
            int effectiveW = Mathf.Min(_originalWidth, maxSize);
            int effectiveH = Mathf.Min(_originalHeight, maxSize);

            return VRChatTextureWeightCalculator.CalculateVRAMBytes(
                effectiveW,
                effectiveH,
                _hasAlpha,
                _hasMipmaps,
                _compressionType);
        }

        /// <summary>
        /// Calcula el ahorro en bytes si se reduce la resolucion un paso
        /// </summary>
        /// <returns>Bytes que se ahorrarian</returns>
        public long GetSavingsOnStepDown()
        {
            int nextSize = GetNextStepDownSize();
            if (nextSize >= _currentMaxSize)
                return 0;

            long currentEstimate = _estimatedVRAMBytes;
            long newEstimate = GetEstimateAtSize(nextSize);

            return currentEstimate - newEstimate;
        }

        /// <summary>
        /// Actualiza el tamanio maximo despues de un step-down
        /// </summary>
        /// <param name="newMaxSize">Nuevo tamanio maximo</param>
        public void UpdateMaxSize(int newMaxSize)
        {
            _currentMaxSize = newMaxSize;
            RecalculateEstimate();
        }

        #endregion
    }
}
