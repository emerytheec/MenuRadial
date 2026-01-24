using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas
{
    /// <summary>
    /// Utilidad para calcular el peso estimado de texturas en VRAM para VRChat.
    /// Implementa calculo preciso de mipmaps y soporte para multiples formatos de textura.
    /// Basado en el algoritmo de whiteflare Avatar Texture Tool.
    /// </summary>
    public static class VRChatTextureWeightCalculator
    {
        #region Constants

        /// <summary>
        /// Bits por pixel para texturas DXT1/BC1 (sin alpha)
        /// </summary>
        public const double BITS_PER_PIXEL_DXT1 = 4;

        /// <summary>
        /// Bits por pixel para texturas DXT5/BC3 (con alpha)
        /// </summary>
        public const double BITS_PER_PIXEL_DXT5 = 8;

        /// <summary>
        /// Bits por pixel para texturas BC4 (1 canal)
        /// </summary>
        public const double BITS_PER_PIXEL_BC4 = 4;

        /// <summary>
        /// Bits por pixel para texturas BC5/BC6H/BC7
        /// </summary>
        public const double BITS_PER_PIXEL_BC5_BC6H_BC7 = 8;

        /// <summary>
        /// Bits por pixel para ASTC 4x4
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_4x4 = 8;

        /// <summary>
        /// Bits por pixel para ASTC 5x5
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_5x5 = 5.12;

        /// <summary>
        /// Bits por pixel para ASTC 6x6
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_6x6 = 3.56;

        /// <summary>
        /// Bits por pixel para ASTC 8x8
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_8x8 = 2;

        /// <summary>
        /// Bits por pixel para ASTC 10x10
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_10x10 = 1.28;

        /// <summary>
        /// Bits por pixel para ASTC 12x12
        /// </summary>
        public const double BITS_PER_PIXEL_ASTC_12x12 = 0.89;

        /// <summary>
        /// Bits por pixel para ETC_RGB4
        /// </summary>
        public const double BITS_PER_PIXEL_ETC_RGB4 = 4;

        /// <summary>
        /// Bits por pixel para ETC2_RGBA8
        /// </summary>
        public const double BITS_PER_PIXEL_ETC2_RGBA8 = 8;

        /// <summary>
        /// Bits por pixel para RGB24 (Unity usa 32 bits internamente)
        /// </summary>
        public const double BITS_PER_PIXEL_RGB24 = 32;

        /// <summary>
        /// Bits por pixel para RGBA32
        /// </summary>
        public const double BITS_PER_PIXEL_RGBA32 = 32;

        /// <summary>
        /// Bits por pixel para RGBAHalf (16 bits por canal)
        /// </summary>
        public const double BITS_PER_PIXEL_RGBA_HALF = 64;

        /// <summary>
        /// Bits por pixel para Alpha8
        /// </summary>
        public const double BITS_PER_PIXEL_ALPHA8 = 8;

        // Constantes de compatibilidad (en bytes, para codigo legacy)
        public const double BYTES_PER_PIXEL_BC1 = 0.5;
        public const double BYTES_PER_PIXEL_BC3_BC7 = 1.0;
        public const double BYTES_PER_PIXEL_BC5 = 1.0;
        public const double BYTES_PER_PIXEL_BC4 = 0.5;
        public const double BYTES_PER_PIXEL_NO_ALPHA = BYTES_PER_PIXEL_BC1;
        public const double BYTES_PER_PIXEL_WITH_ALPHA = BYTES_PER_PIXEL_BC3_BC7;

        /// <summary>
        /// Umbral para considerar una textura como "pesada" (10 MB)
        /// </summary>
        public const long HIGH_WEIGHT_THRESHOLD = 10 * 1024 * 1024;

        /// <summary>
        /// Limite de VRChat para Download Size (archivo comprimido): 200 MB
        /// Si el avatar supera este limite, no se puede subir.
        /// </summary>
        public const long VRCHAT_DOWNLOAD_SIZE_LIMIT = 200 * 1024 * 1024;

        /// <summary>
        /// Limite de VRChat para Uncompressed Size (bundle descomprimido): 500 MB
        /// Si el avatar supera este limite, no se puede subir.
        /// </summary>
        public const long VRCHAT_UNCOMPRESSED_SIZE_LIMIT = 500 * 1024 * 1024;

        /// <summary>
        /// Umbral para considerar el peso de texturas como "alto".
        /// Basado en el limite de Uncompressed Size de VRChat (500 MB),
        /// ya que las texturas suelen ser la mayor parte del bundle.
        /// </summary>
        public const long HIGH_TOTAL_THRESHOLD = 500 * 1024 * 1024;

        /// <summary>
        /// Pasos de resolucion disponibles para step-down
        /// </summary>
        public static readonly int[] RESOLUTION_STEPS = { 8192, 4096, 2048, 1024, 512, 256, 128 };

        /// <summary>
        /// Resolucion minima permitida
        /// </summary>
        public const int MIN_RESOLUTION = 128;

        /// <summary>
        /// Resolucion maxima soportada
        /// </summary>
        public const int MAX_RESOLUTION = 8192;

        #endregion

        #region VRAM Calculation

        /// <summary>
        /// Calcula el peso estimado en VRAM usando el formato real de la textura.
        /// Este es el metodo principal y mas preciso.
        /// </summary>
        /// <param name="width">Ancho de la textura en pixeles</param>
        /// <param name="height">Alto de la textura en pixeles</param>
        /// <param name="textureFormat">Formato real de la textura</param>
        /// <param name="mipmapCount">Cantidad de niveles de mipmap (1 = sin mipmaps)</param>
        /// <returns>Peso estimado en bytes</returns>
        public static long CalculateVRAMBytesFromFormat(
            int width,
            int height,
            TextureFormat textureFormat,
            int mipmapCount)
        {
            double? bitsPerPixel = GetBitsPerPixel(textureFormat);

            // Fallback si no conocemos el formato
            if (bitsPerPixel == null)
            {
                // Asumir formato comprimido con alpha (8 bits/pixel)
                bitsPerPixel = BITS_PER_PIXEL_DXT5;
            }

            return CalculateVRAMBytesWithMipmaps(width, height, bitsPerPixel.Value, mipmapCount);
        }

        /// <summary>
        /// Calcula VRAM iterando por cada nivel de mipmap (algoritmo correcto).
        /// </summary>
        /// <param name="width">Ancho base</param>
        /// <param name="height">Alto base</param>
        /// <param name="bitsPerPixel">Bits por pixel del formato</param>
        /// <param name="mipmapCount">Cantidad de niveles de mipmap</param>
        /// <returns>Peso total en bytes</returns>
        public static long CalculateVRAMBytesWithMipmaps(
            int width,
            int height,
            double bitsPerPixel,
            int mipmapCount)
        {
            long totalBits = 0;
            int w = width;
            int h = height;

            // Minimo 1 nivel (la textura base)
            int levels = Math.Max(1, mipmapCount);

            for (int i = 0; i < levels; i++)
            {
                // Calcular bits para este nivel
                totalBits += (long)Math.Ceiling(bitsPerPixel * w * h);

                // Reducir dimensiones para siguiente nivel
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);

                // Si llegamos a 1x1, terminar
                if (w == 1 && h == 1 && i < levels - 1)
                {
                    // Agregar niveles restantes como 1x1
                    int remaining = levels - i - 1;
                    totalBits += (long)Math.Ceiling(bitsPerPixel * remaining);
                    break;
                }
            }

            // Convertir bits a bytes
            return totalBits / 8;
        }

        /// <summary>
        /// Obtiene los bits por pixel para un formato de textura especifico.
        /// Tabla completa basada en la documentacion de Unity.
        /// </summary>
        /// <param name="format">Formato de textura</param>
        /// <returns>Bits por pixel, o null si el formato es desconocido</returns>
        public static double? GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                // DXT/BC Compression (PC)
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                    return BITS_PER_PIXEL_DXT1;  // 4

                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                    return BITS_PER_PIXEL_DXT5;  // 8

                case TextureFormat.BC4:
                    return BITS_PER_PIXEL_BC4;   // 4

                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                    return BITS_PER_PIXEL_BC5_BC6H_BC7;  // 8

                // ETC Compression (Mobile)
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4Crunched:
                case TextureFormat.ETC2_RGB:
                    return BITS_PER_PIXEL_ETC_RGB4;  // 4

                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ETC2_RGBA8Crunched:
                case TextureFormat.ETC2_RGBA1:
                    return BITS_PER_PIXEL_ETC2_RGBA8;  // 8

                // ASTC Compression (Mobile/Quest) - valores fraccionarios
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_HDR_4x4:
                    return BITS_PER_PIXEL_ASTC_4x4;  // 8

                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_HDR_5x5:
                    return BITS_PER_PIXEL_ASTC_5x5;  // 5.12

                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_HDR_6x6:
                    return BITS_PER_PIXEL_ASTC_6x6;  // 3.56

                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_HDR_8x8:
                    return BITS_PER_PIXEL_ASTC_8x8;  // 2

                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_HDR_10x10:
                    return BITS_PER_PIXEL_ASTC_10x10;  // 1.28

                case TextureFormat.ASTC_12x12:
                case TextureFormat.ASTC_HDR_12x12:
                    return BITS_PER_PIXEL_ASTC_12x12;  // 0.89

                // Uncompressed formats
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    return BITS_PER_PIXEL_ALPHA8;  // 8

                case TextureFormat.R16:
                case TextureFormat.RG16:
                case TextureFormat.RGB565:
                case TextureFormat.ARGB4444:
                case TextureFormat.RGBA4444:
                    return 16;

                case TextureFormat.RGB24:
                    return BITS_PER_PIXEL_RGB24;  // 32 (Unity pads to 32)

                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                case TextureFormat.RG32:
                case TextureFormat.RFloat:
                    return BITS_PER_PIXEL_RGBA32;  // 32

                case TextureFormat.RGB48:
                case TextureFormat.RGBA64:
                case TextureFormat.RGBAHalf:
                case TextureFormat.RGHalf:
                    return BITS_PER_PIXEL_RGBA_HALF;  // 64

                case TextureFormat.RGBAFloat:
                    return 128;

                // PVRTC (iOS)
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                    return 2;

                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                    return 4;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Metodo legacy: Calcula el peso estimado en VRAM para una textura.
        /// Mantiene compatibilidad con codigo existente.
        /// </summary>
        /// <param name="width">Ancho de la textura en pixeles</param>
        /// <param name="height">Alto de la textura en pixeles</param>
        /// <param name="hasAlpha">Si la textura tiene canal alpha</param>
        /// <param name="hasMipmaps">Si la textura tiene mipmaps generados</param>
        /// <param name="compressionType">Tipo de compresion de la textura</param>
        /// <returns>Peso estimado en bytes</returns>
        public static long CalculateVRAMBytes(
            int width,
            int height,
            bool hasAlpha,
            bool hasMipmaps,
            TextureCompressionType compressionType = TextureCompressionType.Default)
        {
            // Obtener bits por pixel segun tipo de compresion
            double bitsPerPixel = GetBitsPerPixelFromCompressionType(hasAlpha, compressionType);

            // Calcular mipmaps correctamente
            int mipmapCount = hasMipmaps ? CalculateMipmapCount(width, height) : 1;

            return CalculateVRAMBytesWithMipmaps(width, height, bitsPerPixel, mipmapCount);
        }

        /// <summary>
        /// Calcula la cantidad de niveles de mipmap para una textura.
        /// </summary>
        public static int CalculateMipmapCount(int width, int height)
        {
            int maxDimension = Math.Max(width, height);
            return (int)Math.Floor(Math.Log(maxDimension, 2)) + 1;
        }

        /// <summary>
        /// Obtiene los bits por pixel segun el tipo de compresion (legacy).
        /// </summary>
        public static double GetBitsPerPixelFromCompressionType(bool hasAlpha, TextureCompressionType compressionType)
        {
            switch (compressionType)
            {
                case TextureCompressionType.NormalMap:
                    return BITS_PER_PIXEL_BC5_BC6H_BC7;  // 8 bits

                case TextureCompressionType.SingleChannel:
                    return BITS_PER_PIXEL_BC4;  // 4 bits

                case TextureCompressionType.Default:
                default:
                    return hasAlpha ? BITS_PER_PIXEL_DXT5 : BITS_PER_PIXEL_DXT1;  // 8 o 4 bits
            }
        }

        /// <summary>
        /// Obtiene los bytes por pixel segun el tipo de compresion (legacy, para compatibilidad).
        /// </summary>
        public static double GetBytesPerPixel(bool hasAlpha, TextureCompressionType compressionType)
        {
            return GetBitsPerPixelFromCompressionType(hasAlpha, compressionType) / 8.0;
        }

        /// <summary>
        /// Calcula el peso estimado usando un tamanio maximo que limita la resolucion.
        /// </summary>
        public static long CalculateVRAMBytesWithMaxSize(
            int originalWidth,
            int originalHeight,
            int maxSize,
            bool hasAlpha,
            bool hasMipmaps)
        {
            int effectiveWidth = Mathf.Min(originalWidth, maxSize);
            int effectiveHeight = Mathf.Min(originalHeight, maxSize);

            return CalculateVRAMBytes(effectiveWidth, effectiveHeight, hasAlpha, hasMipmaps);
        }

        /// <summary>
        /// Calcula el peso estimado usando el formato real y maxSize.
        /// </summary>
        public static long CalculateVRAMBytesWithMaxSizeAndFormat(
            int originalWidth,
            int originalHeight,
            int maxSize,
            TextureFormat format,
            int mipmapCount)
        {
            int effectiveWidth = Mathf.Min(originalWidth, maxSize);
            int effectiveHeight = Mathf.Min(originalHeight, maxSize);

            // Recalcular mipmaps para el tamanio efectivo
            int effectiveMipmaps = mipmapCount > 1
                ? CalculateMipmapCount(effectiveWidth, effectiveHeight)
                : 1;

            return CalculateVRAMBytesFromFormat(effectiveWidth, effectiveHeight, format, effectiveMipmaps);
        }

        /// <summary>
        /// Calcula el peso de una textura generica, soportando Texture2D, Cubemap, Texture2DArray, etc.
        /// Usa el Profiler API como fallback si no puede determinar el formato.
        /// </summary>
        /// <param name="texture">Textura a calcular</param>
        /// <returns>Peso estimado en bytes</returns>
        public static long CalculateTextureVRAM(Texture texture)
        {
            if (texture == null)
                return 0;

            // Obtener dimensiones y profundidad segun tipo de textura
            GetTextureDimensions(texture, out int width, out int height, out int depth);

            // Obtener formato y mipmaps
            TextureFormat? format = GetTextureFormat(texture);
            int mipmapCount = texture.mipmapCount;

            if (format.HasValue)
            {
                double? bitsPerPixel = GetBitsPerPixel(format.Value);
                if (bitsPerPixel.HasValue)
                {
                    return CalculateVRAMBytesWithMipmapsAndDepth(
                        width, height, depth, bitsPerPixel.Value, mipmapCount);
                }
            }

            // Fallback: usar Unity Profiler API
            return GetRuntimeMemorySize(texture);
        }

        /// <summary>
        /// Obtiene las dimensiones de una textura, incluyendo profundidad para arrays y cubemaps.
        /// </summary>
        public static void GetTextureDimensions(Texture texture, out int width, out int height, out int depth)
        {
            width = texture.width;
            height = texture.height;
            depth = 1;

            if (texture is Texture2DArray t2dArray)
            {
                depth = t2dArray.depth;
            }
            else if (texture is Cubemap)
            {
                depth = 6; // 6 caras del cubemap
            }
            else if (texture is CubemapArray cubemapArray)
            {
                depth = 6 * cubemapArray.cubemapCount;
            }
            else if (texture is Texture3D t3d)
            {
                depth = t3d.depth;
            }
        }

        /// <summary>
        /// Obtiene el formato de una textura si es posible.
        /// </summary>
        public static TextureFormat? GetTextureFormat(Texture texture)
        {
            if (texture is Texture2D t2d)
                return t2d.format;
            if (texture is Texture2DArray t2dArray)
                return t2dArray.format;
            if (texture is Texture3D t3d)
                return t3d.format;
            if (texture is Cubemap cubemap)
                return cubemap.format;

            return null;
        }

        /// <summary>
        /// Calcula VRAM considerando profundidad (para arrays, cubemaps, etc.)
        /// </summary>
        public static long CalculateVRAMBytesWithMipmapsAndDepth(
            int width,
            int height,
            int depth,
            double bitsPerPixel,
            int mipmapCount)
        {
            long totalBits = 0;
            int w = width;
            int h = height;

            int levels = Math.Max(1, mipmapCount);

            for (int i = 0; i < levels; i++)
            {
                // Multiplicar por depth para cada nivel
                totalBits += (long)Math.Ceiling(bitsPerPixel * w * h * depth);

                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);

                if (w == 1 && h == 1 && i < levels - 1)
                {
                    int remaining = levels - i - 1;
                    totalBits += (long)Math.Ceiling(bitsPerPixel * depth * remaining);
                    break;
                }
            }

            return totalBits / 8;
        }

        /// <summary>
        /// Fallback: obtiene el tamanio de memoria usando Unity Profiler API.
        /// </summary>
        public static long GetRuntimeMemorySize(Texture texture)
        {
            if (texture == null)
                return 0;

            return UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
        }

        #endregion

        #region Resolution Steps

        /// <summary>
        /// Obtiene el siguiente paso de resolucion menor.
        /// </summary>
        /// <param name="currentMaxSize">Tamanio maximo actual</param>
        /// <returns>El siguiente tamanio menor, o el actual si ya es el minimo</returns>
        public static int GetNextStepDown(int currentMaxSize)
        {
            for (int i = 0; i < RESOLUTION_STEPS.Length; i++)
            {
                if (RESOLUTION_STEPS[i] < currentMaxSize)
                {
                    return RESOLUTION_STEPS[i];
                }
            }

            // Ya esta en el minimo
            return currentMaxSize;
        }

        /// <summary>
        /// Obtiene el siguiente paso de resolucion mayor.
        /// </summary>
        /// <param name="currentMaxSize">Tamanio maximo actual</param>
        /// <returns>El siguiente tamanio mayor, o el actual si ya es el maximo</returns>
        public static int GetNextStepUp(int currentMaxSize)
        {
            for (int i = RESOLUTION_STEPS.Length - 1; i >= 0; i--)
            {
                if (RESOLUTION_STEPS[i] > currentMaxSize)
                {
                    return RESOLUTION_STEPS[i];
                }
            }

            // Ya esta en el maximo
            return currentMaxSize;
        }

        /// <summary>
        /// Obtiene el indice del paso de resolucion para un tamanio dado.
        /// </summary>
        /// <param name="maxSize">Tamanio a buscar</param>
        /// <returns>Indice en RESOLUTION_STEPS o -1 si no se encuentra exacto</returns>
        public static int GetResolutionStepIndex(int maxSize)
        {
            for (int i = 0; i < RESOLUTION_STEPS.Length; i++)
            {
                if (RESOLUTION_STEPS[i] == maxSize)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Normaliza un tamanio al paso de resolucion mas cercano (hacia abajo).
        /// </summary>
        /// <param name="size">Tamanio a normalizar</param>
        /// <returns>Paso de resolucion mas cercano</returns>
        public static int NormalizeToStep(int size)
        {
            foreach (int step in RESOLUTION_STEPS)
            {
                if (step <= size)
                    return step;
            }
            return MIN_RESOLUTION;
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Formatea un valor en bytes a una cadena legible.
        /// </summary>
        /// <param name="bytes">Cantidad de bytes</param>
        /// <returns>Cadena formateada (ej: "12.5 MB")</returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0)
                return "0 B";

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";

            return $"{bytes} B";
        }

        /// <summary>
        /// Formatea un valor en bytes de forma compacta.
        /// </summary>
        /// <param name="bytes">Cantidad de bytes</param>
        /// <returns>Cadena formateada compacta (ej: "12.5MB")</returns>
        public static string FormatBytesCompact(long bytes)
        {
            if (bytes < 0)
                return "0B";

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F1}GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F1}MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F0}KB";

            return $"{bytes}B";
        }

        /// <summary>
        /// Calcula el porcentaje de ahorro entre dos valores.
        /// </summary>
        /// <param name="original">Valor original</param>
        /// <param name="reduced">Valor reducido</param>
        /// <returns>Porcentaje de ahorro (0-100)</returns>
        public static float CalculateSavingsPercentage(long original, long reduced)
        {
            if (original <= 0)
                return 0f;

            if (reduced >= original)
                return 0f;

            return ((original - reduced) / (float)original) * 100f;
        }

        #endregion

        #region Weight Categories

        /// <summary>
        /// Determina si una textura individual se considera pesada.
        /// </summary>
        /// <param name="bytes">Peso en bytes</param>
        /// <returns>True si supera el umbral de textura pesada</returns>
        public static bool IsHighWeight(long bytes)
        {
            return bytes >= HIGH_WEIGHT_THRESHOLD;
        }

        /// <summary>
        /// Determina si el peso total se considera alto.
        /// </summary>
        /// <param name="totalBytes">Peso total en bytes</param>
        /// <returns>True si supera el umbral total alto</returns>
        public static bool IsHighTotalWeight(long totalBytes)
        {
            return totalBytes >= HIGH_TOTAL_THRESHOLD;
        }

        /// <summary>
        /// Obtiene una etiqueta de categoria para un peso dado.
        /// </summary>
        /// <param name="bytes">Peso en bytes</param>
        /// <returns>Etiqueta descriptiva</returns>
        public static string GetWeightCategory(long bytes)
        {
            const long MB = 1024 * 1024;

            if (bytes >= 50 * MB)
                return "Muy Alto";
            if (bytes >= 20 * MB)
                return "Alto";
            if (bytes >= 10 * MB)
                return "Moderado";
            if (bytes >= 5 * MB)
                return "Normal";

            return "Bajo";
        }

        #endregion
    }
}
