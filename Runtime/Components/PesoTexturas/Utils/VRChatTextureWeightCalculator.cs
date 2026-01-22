using UnityEngine;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas
{
    /// <summary>
    /// Utilidad para calcular el peso estimado de texturas en VRAM para VRChat.
    /// Los calculos usan compresion BC (Block Compression) que es el formato
    /// utilizado por VRChat/Unity en PC.
    /// </summary>
    public static class VRChatTextureWeightCalculator
    {
        #region Constants

        /// <summary>
        /// Bytes por pixel para texturas BC1 (sin alpha) = 0.5 bytes/pixel
        /// </summary>
        public const double BYTES_PER_PIXEL_BC1 = 0.5;

        /// <summary>
        /// Bytes por pixel para texturas BC3/BC7 (con alpha) = 1.0 byte/pixel
        /// </summary>
        public const double BYTES_PER_PIXEL_BC3_BC7 = 1.0;

        /// <summary>
        /// Bytes por pixel para texturas BC5 (normal maps, 2 canales) = 1.0 byte/pixel
        /// </summary>
        public const double BYTES_PER_PIXEL_BC5 = 1.0;

        /// <summary>
        /// Bytes por pixel para texturas BC4 (1 canal, masks) = 0.5 bytes/pixel
        /// </summary>
        public const double BYTES_PER_PIXEL_BC4 = 0.5;

        // Aliases para compatibilidad
        public const double BYTES_PER_PIXEL_NO_ALPHA = BYTES_PER_PIXEL_BC1;
        public const double BYTES_PER_PIXEL_WITH_ALPHA = BYTES_PER_PIXEL_BC3_BC7;

        /// <summary>
        /// Factor multiplicador para mipmaps.
        /// Valor 1.0 = sin considerar mipmaps adicionales (coincide mejor con VRChat que usa Mip Streaming).
        /// Valor teorico completo seria 1.33 (suma de la serie geometrica 1 + 1/4 + 1/16 + ...).
        /// </summary>
        public const double MIPMAP_FACTOR = 1.0;

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
        /// Calcula el peso estimado en VRAM para una textura con las caracteristicas dadas.
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
            // Calcular pixeles totales
            long pixels = (long)width * height;

            // Seleccionar bytes por pixel segun tipo de compresion
            double bytesPerPixel = GetBytesPerPixel(hasAlpha, compressionType);

            // Calcular tamanio base
            double baseSize = pixels * bytesPerPixel;

            // Aplicar factor de mipmaps si aplica
            if (hasMipmaps)
            {
                baseSize *= MIPMAP_FACTOR;
            }

            return (long)baseSize;
        }

        /// <summary>
        /// Obtiene los bytes por pixel segun el tipo de compresion.
        /// </summary>
        /// <param name="hasAlpha">Si la textura tiene canal alpha</param>
        /// <param name="compressionType">Tipo de compresion</param>
        /// <returns>Bytes por pixel</returns>
        public static double GetBytesPerPixel(bool hasAlpha, TextureCompressionType compressionType)
        {
            switch (compressionType)
            {
                case TextureCompressionType.NormalMap:
                    // Normal maps siempre usan BC5 (1.0 byte/pixel)
                    return BYTES_PER_PIXEL_BC5;

                case TextureCompressionType.SingleChannel:
                    // Single channel usa BC4 (0.5 byte/pixel)
                    return BYTES_PER_PIXEL_BC4;

                case TextureCompressionType.Default:
                default:
                    // Texturas estandar: BC1 sin alpha, BC3/BC7 con alpha
                    return hasAlpha ? BYTES_PER_PIXEL_BC3_BC7 : BYTES_PER_PIXEL_BC1;
            }
        }

        /// <summary>
        /// Calcula el peso estimado usando un tamanio maximo que limita la resolucion.
        /// </summary>
        /// <param name="originalWidth">Ancho original de la textura</param>
        /// <param name="originalHeight">Alto original de la textura</param>
        /// <param name="maxSize">Tamanio maximo configurado en el importer</param>
        /// <param name="hasAlpha">Si tiene canal alpha</param>
        /// <param name="hasMipmaps">Si tiene mipmaps</param>
        /// <returns>Peso estimado en bytes</returns>
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
