using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.PesoTexturas;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Utilidad de editor para escanear texturas de GameObjects.
    /// Usa AssetDatabase para obtener informacion del TextureImporter.
    /// </summary>
    public static class TextureScanner
    {
        #region Public Methods

        /// <summary>
        /// Escanea todas las texturas de un GameObject y sus hijos.
        /// </summary>
        /// <param name="root">GameObject raiz para escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanTextures(GameObject root, HashSet<string> processedPaths)
        {
            var result = new List<TextureEntry>();

            if (root == null)
                return result;

            processedPaths ??= new HashSet<string>();

            // Obtener todos los Renderers
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                // Obtener materiales compartidos
                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                foreach (var material in materials)
                {
                    if (material == null)
                        continue;

                    // Obtener todas las propiedades de textura del material
                    ScanMaterialTextures(material, processedPaths, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Crea una entrada de textura a partir de una Texture2D.
        /// </summary>
        /// <param name="texture">Textura a procesar</param>
        /// <param name="assetPath">Ruta del asset</param>
        /// <returns>TextureEntry con informacion calculada</returns>
        public static TextureEntry CreateTextureEntry(Texture2D texture, string assetPath)
        {
            if (texture == null || string.IsNullOrEmpty(assetPath))
                return null;

            // Obtener el TextureImporter para esta textura
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            int maxSize = 2048; // Default
            bool hasAlpha = false;
            bool hasMipmaps = true;
            bool hasMipStreaming = true;

            if (importer != null)
            {
                maxSize = importer.maxTextureSize;
                hasMipmaps = importer.mipmapEnabled;
                hasAlpha = DetectAlphaChannel(texture, importer);
                hasMipStreaming = importer.streamingMipmaps;
            }
            else
            {
                // Fallback: detectar alpha del formato de textura
                hasAlpha = HasAlphaFromFormat(texture.format);
            }

            return new TextureEntry(
                texture,
                assetPath,
                texture.width,
                texture.height,
                maxSize,
                hasAlpha,
                hasMipmaps,
                hasMipStreaming);
        }

        /// <summary>
        /// Detecta si una textura tiene canal alpha.
        /// </summary>
        /// <param name="texture">Textura a analizar</param>
        /// <param name="importer">TextureImporter de la textura</param>
        /// <returns>True si tiene canal alpha</returns>
        public static bool DetectAlphaChannel(Texture2D texture, TextureImporter importer)
        {
            if (importer != null)
            {
                // Primero intentar con el metodo del importer
                if (importer.DoesSourceTextureHaveAlpha())
                    return true;

                // Verificar configuracion de alpha
                if (importer.alphaSource != TextureImporterAlphaSource.None)
                    return true;
            }

            // Fallback: verificar formato de textura
            return HasAlphaFromFormat(texture.format);
        }

        /// <summary>
        /// Aplica un step-down (reduccion de resolucion) a una textura.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool ApplyStepDown(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            int nextSize = entry.GetNextStepDownSize();
            if (nextSize >= entry.CurrentMaxSize)
                return false;

            return ApplyMaxSize(entry, nextSize);
        }

        /// <summary>
        /// Aplica un tamanio maximo especifico a una textura.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <param name="newMaxSize">Nuevo tamanio maximo</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool ApplyMaxSize(TextureEntry entry, int newMaxSize)
        {
            if (entry == null || !entry.IsValid)
                return false;

            var importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
            if (importer == null)
                return false;

            // Registrar Undo
            Undo.RecordObject(importer, "Change Texture Max Size");

            importer.maxTextureSize = newMaxSize;

            // Guardar y reimportar
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Actualizar la entrada
            entry.UpdateMaxSize(newMaxSize);

            return true;
        }

        /// <summary>
        /// Aplica step-down a todas las texturas de un grupo.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int ApplyStepDownToGroup(TextureGroupEntry group)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (ApplyStepDown(texture))
                    count++;
            }

            group.RecalculateAll();
            return count;
        }

        /// <summary>
        /// Aplica un tamanio maximo a todas las texturas de un grupo.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <param name="maxSize">Tamanio maximo a aplicar</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int ApplyMaxSizeToGroup(TextureGroupEntry group, int maxSize)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (texture.CurrentMaxSize > maxSize)
                {
                    if (ApplyMaxSize(texture, maxSize))
                        count++;
                }
            }

            group.RecalculateAll();
            return count;
        }

        /// <summary>
        /// Habilita Mip Streaming en una textura.
        /// Requerido por VRChat para texturas con mipmaps.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool EnableMipStreaming(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            // Ya tiene Mip Streaming habilitado
            if (entry.HasMipStreaming)
                return false;

            var importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
            if (importer == null)
                return false;

            // Solo aplica si tiene mipmaps habilitados
            if (!importer.mipmapEnabled)
                return false;

            // Registrar Undo
            Undo.RecordObject(importer, "Enable Mip Streaming");

            importer.streamingMipmaps = true;

            // Guardar y reimportar
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Actualizar la entrada
            entry.HasMipStreaming = true;

            return true;
        }

        /// <summary>
        /// Habilita Mip Streaming en todas las texturas de un grupo.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int EnableMipStreamingInGroup(TextureGroupEntry group)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (EnableMipStreaming(texture))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Habilita Mip Streaming en todas las texturas de una lista.
        /// </summary>
        /// <param name="entries">Lista de entradas de textura</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int EnableMipStreamingInList(IEnumerable<TextureEntry> entries)
        {
            if (entries == null)
                return 0;

            int count = 0;
            foreach (var texture in entries)
            {
                if (EnableMipStreaming(texture))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Escanea texturas de una lista de materiales.
        /// Util para escanear materiales alternativos de MRAgruparMateriales.
        /// </summary>
        /// <param name="materials">Lista de materiales a escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanMaterials(IEnumerable<Material> materials, HashSet<string> processedPaths)
        {
            var result = new List<TextureEntry>();

            if (materials == null)
                return result;

            processedPaths ??= new HashSet<string>();

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                ScanMaterialTextures(material, processedPaths, result);
            }

            return result;
        }

        /// <summary>
        /// Escanea texturas de un solo material.
        /// </summary>
        /// <param name="material">Material a escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanSingleMaterial(Material material, HashSet<string> processedPaths)
        {
            var result = new List<TextureEntry>();

            if (material == null)
                return result;

            processedPaths ??= new HashSet<string>();
            ScanMaterialTextures(material, processedPaths, result);

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Escanea todas las texturas de un material.
        /// </summary>
        private static void ScanMaterialTextures(
            Material material,
            HashSet<string> processedPaths,
            List<TextureEntry> result)
        {
            if (material == null)
                return;

            // Obtener todas las propiedades de textura del shader
            var shader = material.shader;
            if (shader == null)
                return;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;

                if (texture == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // Ignorar texturas embebidas o de paquetes
                if (!assetPath.StartsWith("Assets/"))
                    continue;

                // Evitar duplicados
                if (processedPaths.Contains(assetPath))
                    continue;

                processedPaths.Add(assetPath);

                var entry = CreateTextureEntry(texture, assetPath);
                if (entry != null)
                {
                    result.Add(entry);
                }
            }
        }

        /// <summary>
        /// Determina si un formato de textura tiene canal alpha.
        /// </summary>
        private static bool HasAlphaFromFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32:
                case TextureFormat.RGBA32:
                case TextureFormat.BGRA32:
                case TextureFormat.ARGB4444:
                case TextureFormat.RGBA4444:
                case TextureFormat.Alpha8:
                case TextureFormat.DXT5:
                case TextureFormat.BC7:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                    return true;

                default:
                    return false;
            }
        }

        #endregion
    }
}
