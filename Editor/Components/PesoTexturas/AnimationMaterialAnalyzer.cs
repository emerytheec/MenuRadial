using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Analiza archivos de animacion para extraer los GUIDs de materiales referenciados.
    /// Esto permite calcular con precision que texturas de materiales alternativos
    /// seran incluidas en el build de VRChat.
    /// </summary>
    public static class AnimationMaterialAnalyzer
    {
        // Regex para extraer GUIDs de materiales en archivos .anim
        // Formato: guid: 0045f4b1cea31184fa65505a249cab79
        private static readonly Regex GuidRegex = new Regex(
            @"guid:\s*([a-fA-F0-9]{32})",
            RegexOptions.Compiled);

        /// <summary>
        /// Obtiene todos los GUIDs de materiales referenciados en las animaciones generadas.
        /// </summary>
        /// <param name="avatarName">Nombre del avatar para buscar en la carpeta Generated</param>
        /// <returns>HashSet con los GUIDs de materiales encontrados</returns>
        public static HashSet<string> GetReferencedMaterialGuids(string avatarName)
        {
            var guids = new HashSet<string>();

            if (string.IsNullOrEmpty(avatarName))
                return guids;

            // Buscar en la carpeta Generated del avatar
            string generatedPath = $"Assets/Bender_Dios/Generated/{avatarName}";

            if (!AssetDatabase.IsValidFolder(generatedPath))
            {
                // Intentar buscar con variaciones del nombre
                generatedPath = FindGeneratedFolder(avatarName);
                if (string.IsNullOrEmpty(generatedPath))
                    return guids;
            }

            // Buscar todos los archivos .anim en la carpeta
            string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { generatedPath });

            foreach (string animGuid in animGuids)
            {
                string animPath = AssetDatabase.GUIDToAssetPath(animGuid);
                ExtractMaterialGuidsFromAnimation(animPath, guids);
            }

            return guids;
        }

        /// <summary>
        /// Obtiene todos los GUIDs de materiales referenciados en animaciones
        /// buscando en multiples ubicaciones posibles.
        /// </summary>
        /// <param name="avatarRoot">GameObject raiz del avatar</param>
        /// <returns>HashSet con los GUIDs de materiales encontrados</returns>
        public static HashSet<string> GetReferencedMaterialGuids(GameObject avatarRoot)
        {
            var guids = new HashSet<string>();

            if (avatarRoot == null)
                return guids;

            // 1. Buscar por nombre del avatar en Generated
            var guidsByName = GetReferencedMaterialGuids(avatarRoot.name);
            guids.UnionWith(guidsByName);

            // 2. Buscar en toda la carpeta Generated (por si el nombre no coincide exactamente)
            string[] allAnimGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Bender_Dios/Generated" });

            foreach (string animGuid in allAnimGuids)
            {
                string animPath = AssetDatabase.GUIDToAssetPath(animGuid);
                ExtractMaterialGuidsFromAnimation(animPath, guids);
            }

            return guids;
        }

        /// <summary>
        /// Extrae los GUIDs de materiales de un archivo de animacion.
        /// </summary>
        /// <param name="animationPath">Ruta del archivo .anim</param>
        /// <param name="guids">HashSet donde agregar los GUIDs encontrados</param>
        private static void ExtractMaterialGuidsFromAnimation(string animationPath, HashSet<string> guids)
        {
            if (string.IsNullOrEmpty(animationPath) || !File.Exists(animationPath))
                return;

            try
            {
                // Leer el archivo de animacion como texto
                string content = File.ReadAllText(animationPath);

                // Buscar referencias a materiales (PPtrCurve con type: 21 para Material)
                // Los archivos .anim son YAML y contienen bloques como:
                // - curve:
                //     serializedVersion: 2
                //     m_Curve:
                //     - time: 0
                //       value:
                //         m_FileID: ...
                //         m_PathID: ...
                //       ...
                //   attribute: m_Materials.Array.data[0]
                //   path: ...
                //   classID: 21
                //   script: {fileID: 0}
                //   flags: 0

                // Buscar todos los GUIDs en el archivo
                var matches = GuidRegex.Matches(content);

                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        string guid = match.Groups[1].Value.ToLowerInvariant();

                        // Verificar que sea un material (no otra cosa como texture, script, etc.)
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".mat"))
                        {
                            guids.Add(guid);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AnimationMaterialAnalyzer] Error leyendo {animationPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca la carpeta Generated para un avatar con nombre similar.
        /// </summary>
        private static string FindGeneratedFolder(string avatarName)
        {
            string basePath = "Assets/Bender_Dios/Generated";

            if (!AssetDatabase.IsValidFolder(basePath))
                return null;

            // Listar subcarpetas
            string[] subfolders = AssetDatabase.GetSubFolders(basePath);

            foreach (string folder in subfolders)
            {
                string folderName = Path.GetFileName(folder);

                // Comparar ignorando mayusculas/minusculas
                if (folderName.Equals(avatarName, System.StringComparison.OrdinalIgnoreCase))
                    return folder;

                // Verificar si el nombre del avatar contiene el nombre de la carpeta o viceversa
                if (folderName.Contains(avatarName) || avatarName.Contains(folderName))
                    return folder;
            }

            return null;
        }

        /// <summary>
        /// Verifica si un material esta referenciado en las animaciones.
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <param name="referencedGuids">HashSet de GUIDs referenciados</param>
        /// <returns>True si el material esta referenciado</returns>
        public static bool IsMaterialReferenced(Material material, HashSet<string> referencedGuids)
        {
            if (material == null || referencedGuids == null || referencedGuids.Count == 0)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(assetPath))
                return false;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return referencedGuids.Contains(guid.ToLowerInvariant());
        }

        /// <summary>
        /// Filtra una lista de materiales para incluir solo los referenciados en animaciones.
        /// </summary>
        /// <param name="materials">Lista de materiales a filtrar</param>
        /// <param name="referencedGuids">HashSet de GUIDs referenciados</param>
        /// <returns>Lista filtrada de materiales</returns>
        public static List<Material> FilterReferencedMaterials(
            IEnumerable<Material> materials,
            HashSet<string> referencedGuids)
        {
            var result = new List<Material>();

            if (materials == null)
                return result;

            foreach (var material in materials)
            {
                if (material != null && IsMaterialReferenced(material, referencedGuids))
                {
                    result.Add(material);
                }
            }

            return result;
        }
    }
}
