using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.Interfaces;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.BoneNames;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Controllers
{
    /// <summary>
    /// Utilidad para mapeo de huesos humanoid entre avatar y ropa.
    /// Usa detección en 3 niveles:
    /// 1. Humanoid API (Animator.GetBoneTransform) - más confiable
    /// 2. Nombre exacto (case-insensitive)
    /// 3. Heurística con 230+ patrones de nombres (BoneNameDatabase)
    /// </summary>
    public class HumanoidBoneMapper : IBoneMapper
    {
        #region Constants

        /// <summary>
        /// Huesos que se ignoran por defecto porque pueden dañar expresiones faciales
        /// </summary>
        private static readonly HashSet<HumanBodyBones> IgnoredBones = new HashSet<HumanBodyBones>
        {
            HumanBodyBones.LeftEye,
            HumanBodyBones.RightEye,
            HumanBodyBones.Jaw
        };

        /// <summary>
        /// Umbral de similitud para matching heurístico (0-1)
        /// </summary>
        private const float SIMILARITY_THRESHOLD = 0.7f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Detecta mapeos de huesos entre avatar y ropa.
        /// Usa detección multinivel: Humanoid API → Nombre exacto → Heurística
        /// </summary>
        public List<BoneMapping> DetectBoneMappings(ArmatureReference avatar, ArmatureReference clothing)
        {
            return DetectBoneMappings(avatar, clothing, null, null);
        }

        /// <summary>
        /// Detecta mapeos de huesos entre avatar y ropa con soporte para prefijo/sufijo.
        /// Usa detección multinivel: Humanoid API → Nombre exacto → Heurística
        /// </summary>
        /// <param name="avatar">Armature del avatar</param>
        /// <param name="clothing">Armature de la ropa</param>
        /// <param name="bonePrefix">Prefijo a eliminar de los nombres de huesos de la ropa</param>
        /// <param name="boneSuffix">Sufijo a eliminar de los nombres de huesos de la ropa</param>
        public List<BoneMapping> DetectBoneMappings(ArmatureReference avatar, ArmatureReference clothing, string bonePrefix, string boneSuffix)
        {
            var mappings = new List<BoneMapping>();

            if (avatar == null || clothing == null)
            {
                Debug.LogWarning("[HumanoidBoneMapper] Avatar o Clothing es null");
                return mappings;
            }

            if (avatar.RootObject == null || clothing.RootObject == null)
            {
                Debug.LogWarning("[HumanoidBoneMapper] RootObject de avatar o clothing es null");
                return mappings;
            }

            bool hasCustomNaming = !string.IsNullOrEmpty(bonePrefix) || !string.IsNullOrEmpty(boneSuffix);

            Debug.Log($"[HumanoidBoneMapper] Detectando huesos con base de datos mejorada...");
            Debug.Log($"  Avatar: {avatar.RootObject.name}, Humanoid: {avatar.IsHumanoid}");
            Debug.Log($"  Ropa: {clothing.RootObject.name}, Humanoid: {clothing.IsHumanoid}");
            if (hasCustomNaming)
            {
                Debug.Log($"  Prefijo: \"{bonePrefix ?? ""}\", Sufijo: \"{boneSuffix ?? ""}\"");
            }

            // Determinar donde buscar huesos en la ropa
            Transform clothingSearchRoot = GetClothingSearchRoot(clothing);

            // Construir cache de todos los huesos en la ropa (con nombres procesados si hay prefijo/sufijo)
            var clothingBoneCache = BuildBoneCache(clothingSearchRoot, bonePrefix, boneSuffix);
            Debug.Log($"  Huesos en ropa: {clothingBoneCache.Count}");

            // Estadísticas de métodos usados
            int humanoidMatches = 0;
            int exactNameMatches = 0;
            int heuristicMatches = 0;

            // Iterar todos los HumanBodyBones
            foreach (HumanBodyBones boneType in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (boneType == HumanBodyBones.LastBone)
                    continue;

                if (IgnoredBones.Contains(boneType))
                    continue;

                // Buscar hueso en avatar
                Transform avatarBone = GetBoneFromAvatar(avatar, boneType, out BoneMappingMethod avatarMethod);

                if (avatarBone == null)
                    continue;

                // Buscar hueso correspondiente en ropa (3 niveles)
                Transform clothingBone = null;
                BoneMappingMethod clothingMethod = BoneMappingMethod.None;

                // Nivel 1: Humanoid API (si la ropa es humanoid)
                if (clothing.IsHumanoid && clothing.Animator != null)
                {
                    try
                    {
                        clothingBone = clothing.Animator.GetBoneTransform(boneType);
                        if (clothingBone != null)
                        {
                            clothingMethod = BoneMappingMethod.HumanoidMapping;
                            humanoidMatches++;
                        }
                    }
                    catch { }
                }

                // Nivel 2: Nombre exacto del hueso del avatar
                if (clothingBone == null)
                {
                    clothingBone = FindBoneByExactName(clothingBoneCache, avatarBone.name);
                    if (clothingBone != null)
                    {
                        clothingMethod = BoneMappingMethod.NameMatching;
                        exactNameMatches++;
                    }
                }

                // Nivel 3: Heurística con BoneNameDatabase
                if (clothingBone == null)
                {
                    clothingBone = FindBoneByHeuristic(clothingBoneCache, boneType, avatarBone.name);
                    if (clothingBone != null)
                    {
                        clothingMethod = BoneMappingMethod.NameMatching;
                        heuristicMatches++;
                    }
                }

                var mapping = new BoneMapping(boneType, avatarBone, clothingBone,
                    clothingBone != null ? clothingMethod : BoneMappingMethod.None);
                mappings.Add(mapping);
            }

            // Estadísticas
            int validCount = mappings.Count(m => m.IsValid);
            Debug.Log($"[HumanoidBoneMapper] Resultado: {validCount}/{mappings.Count} mapeos válidos");
            Debug.Log($"  Métodos: Humanoid={humanoidMatches}, NombreExacto={exactNameMatches}, Heurística={heuristicMatches}");

            return mappings;
        }

        /// <summary>
        /// Obtiene un hueso específico usando Humanoid API o búsqueda por nombre.
        /// </summary>
        public Transform GetBone(Animator animator, HumanBodyBones boneType)
        {
            if (animator == null)
                return null;

            // Primario: Humanoid mapping
            if (animator.isHuman)
            {
                try
                {
                    var bone = animator.GetBoneTransform(boneType);
                    if (bone != null)
                        return bone;
                }
                catch { }
            }

            // Fallback: Búsqueda heurística
            return BoneNameDatabase.FindMatchingBone(
                boneType.ToString(),
                animator,
                animator.transform);
        }

        /// <summary>
        /// Obtiene todos los huesos humanoid de un animator.
        /// </summary>
        public Dictionary<HumanBodyBones, Transform> GetAllHumanoidBones(Animator animator)
        {
            var bones = new Dictionary<HumanBodyBones, Transform>();

            if (animator == null || !animator.isHuman)
                return bones;

            foreach (HumanBodyBones boneType in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (boneType == HumanBodyBones.LastBone)
                    continue;

                try
                {
                    var bone = animator.GetBoneTransform(boneType);
                    if (bone != null)
                    {
                        bones[boneType] = bone;
                    }
                }
                catch { }
            }

            return bones;
        }

        /// <summary>
        /// Verifica si un nombre de hueso corresponde a un hueso humanoid conocido.
        /// </summary>
        public bool IsHumanoidBone(string boneName)
        {
            return BoneNameDatabase.IsKnownHumanoidBone(boneName);
        }

        /// <summary>
        /// Intenta identificar qué HumanBodyBones corresponde a un nombre dado.
        /// </summary>
        public bool TryIdentifyBone(string boneName, out HumanBodyBones result)
        {
            return BoneNameDatabase.TryGetBoneFromName(boneName, out result);
        }

        /// <summary>
        /// Busca un hueso por nombre usando la base de datos de patrones.
        /// Implementa IBoneMapper.FindBoneByName.
        /// </summary>
        /// <param name="root">Transform raíz donde buscar</param>
        /// <param name="boneType">Tipo de hueso a buscar</param>
        /// <returns>Transform encontrado o null</returns>
        public Transform FindBoneByName(Transform root, HumanBodyBones boneType)
        {
            if (root == null)
                return null;

            // Obtener variantes de nombre para este tipo de hueso
            var variants = BoneNameDatabase.GetBoneNameVariants(boneType);

            // Buscar en la jerarquía
            var allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (var variant in variants)
            {
                foreach (var t in allTransforms)
                {
                    if (string.Equals(BoneNameDatabase.NormalizeName(t.name), variant, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Obtiene el transform raíz desde donde buscar huesos en la ropa.
        /// </summary>
        private Transform GetClothingSearchRoot(ArmatureReference clothing)
        {
            if (clothing.ArmatureRoot != null)
                return clothing.ArmatureRoot;

            if (clothing.Animator != null)
                return clothing.Animator.transform;

            return clothing.RootObject?.transform;
        }

        /// <summary>
        /// Obtiene un hueso del avatar usando el método apropiado.
        /// </summary>
        private Transform GetBoneFromAvatar(ArmatureReference avatar, HumanBodyBones boneType, out BoneMappingMethod method)
        {
            method = BoneMappingMethod.None;

            // Nivel 1: Humanoid API
            if (avatar.IsHumanoid && avatar.Animator != null)
            {
                try
                {
                    var bone = avatar.Animator.GetBoneTransform(boneType);
                    if (bone != null)
                    {
                        method = BoneMappingMethod.HumanoidMapping;
                        return bone;
                    }
                }
                catch { }
            }

            // Nivel 2: Búsqueda heurística en avatar
            Transform searchRoot = avatar.ArmatureRoot ?? avatar.Animator?.transform ?? avatar.RootObject.transform;
            var foundBone = BoneNameDatabase.FindMatchingBone(
                boneType.ToString(),
                avatar.Animator,
                searchRoot);

            if (foundBone != null)
            {
                method = BoneMappingMethod.NameMatching;
                return foundBone;
            }

            return null;
        }

        /// <summary>
        /// Construye un cache de todos los transforms indexados por nombre.
        /// </summary>
        private Dictionary<string, Transform> BuildBoneCache(Transform root)
        {
            return BuildBoneCache(root, null, null);
        }

        /// <summary>
        /// Construye un cache de todos los transforms indexados por nombre,
        /// eliminando prefijo/sufijo si se especifican.
        /// </summary>
        private Dictionary<string, Transform> BuildBoneCache(Transform root, string prefix, string suffix)
        {
            var cache = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            if (root != null)
            {
                BuildBoneCacheRecursive(root, cache, prefix, suffix);
            }
            return cache;
        }

        private void BuildBoneCacheRecursive(Transform current, Dictionary<string, Transform> cache)
        {
            BuildBoneCacheRecursive(current, cache, null, null);
        }

        private void BuildBoneCacheRecursive(Transform current, Dictionary<string, Transform> cache, string prefix, string suffix)
        {
            string boneName = current.name;

            // Indexar por nombre original
            if (!cache.ContainsKey(boneName))
            {
                cache[boneName] = current;
            }

            // Eliminar prefijo/sufijo si se especificaron
            string strippedName = StripPrefixSuffix(boneName, prefix, suffix);
            if (strippedName != boneName && !string.IsNullOrEmpty(strippedName) && !cache.ContainsKey(strippedName))
            {
                cache[strippedName] = current;
            }

            // También indexar por nombre normalizado
            string normalized = BoneNameDatabase.NormalizeName(boneName);
            if (!string.IsNullOrEmpty(normalized) && !cache.ContainsKey(normalized))
            {
                cache[normalized] = current;
            }

            // Normalizar también el nombre sin prefijo/sufijo
            if (strippedName != boneName)
            {
                string strippedNormalized = BoneNameDatabase.NormalizeName(strippedName);
                if (!string.IsNullOrEmpty(strippedNormalized) && !cache.ContainsKey(strippedNormalized))
                {
                    cache[strippedNormalized] = current;
                }
            }

            foreach (Transform child in current)
            {
                BuildBoneCacheRecursive(child, cache, prefix, suffix);
            }
        }

        /// <summary>
        /// Elimina prefijo y/o sufijo de un nombre de hueso.
        /// </summary>
        private string StripPrefixSuffix(string name, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            string result = name;

            // Eliminar prefijo
            if (!string.IsNullOrEmpty(prefix) && result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length);
            }

            // Eliminar sufijo
            if (!string.IsNullOrEmpty(suffix) && result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - suffix.Length);
            }

            return result;
        }

        /// <summary>
        /// Busca un hueso por nombre exacto (case-insensitive).
        /// </summary>
        private Transform FindBoneByExactName(Dictionary<string, Transform> cache, string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return null;

            cache.TryGetValue(boneName, out Transform result);
            return result;
        }

        /// <summary>
        /// Busca un hueso usando heurística con BoneNameDatabase.
        /// </summary>
        private Transform FindBoneByHeuristic(Dictionary<string, Transform> cache, HumanBodyBones boneType, string avatarBoneName)
        {
            // Obtener todas las variantes de nombre para este tipo de hueso
            var variants = BoneNameDatabase.GetBoneNameVariants(boneType);

            // Buscar cada variante en el cache
            foreach (var variant in variants)
            {
                if (cache.TryGetValue(variant, out Transform bone))
                {
                    return bone;
                }
            }

            // Búsqueda por similitud si no hay match exacto
            float bestSimilarity = 0f;
            Transform bestMatch = null;

            foreach (var kvp in cache)
            {
                float similarity = BoneNameDatabase.CalculateSimilarity(kvp.Key, avatarBoneName);

                if (similarity > bestSimilarity && similarity >= SIMILARITY_THRESHOLD)
                {
                    bestSimilarity = similarity;
                    bestMatch = kvp.Value;
                }

                // También comparar contra variantes del hueso buscado
                foreach (var variant in variants)
                {
                    similarity = BoneNameDatabase.CalculateSimilarity(kvp.Key, variant);
                    if (similarity > bestSimilarity && similarity >= SIMILARITY_THRESHOLD)
                    {
                        bestSimilarity = similarity;
                        bestMatch = kvp.Value;
                    }
                }
            }

            return bestMatch;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Debug: Lista todos los huesos encontrados en un transform.
        /// </summary>
        public void DebugListAllBones(Transform root, string prefix = "")
        {
            if (root == null) return;

            string boneInfo = BoneNameDatabase.IsKnownHumanoidBone(root.name) ? " [HUMANOID]" : "";
            Debug.Log($"{prefix}- {root.name}{boneInfo}");

            foreach (Transform child in root)
            {
                DebugListAllBones(child, prefix + "  ");
            }
        }

        /// <summary>
        /// Debug: Analiza y reporta la estructura de huesos de un armature.
        /// </summary>
        public void AnalyzeArmature(Transform root)
        {
            if (root == null)
            {
                Debug.Log("[HumanoidBoneMapper] Root es null");
                return;
            }

            int totalBones = 0;
            int humanoidBones = 0;
            var identifiedBones = new Dictionary<HumanBodyBones, string>();

            AnalyzeRecursive(root, ref totalBones, ref humanoidBones, identifiedBones);

            Debug.Log($"[HumanoidBoneMapper] Análisis de '{root.name}':");
            Debug.Log($"  Total huesos: {totalBones}");
            Debug.Log($"  Huesos humanoid identificados: {humanoidBones}");

            foreach (var kvp in identifiedBones.OrderBy(x => (int)x.Key))
            {
                Debug.Log($"    {kvp.Key}: {kvp.Value}");
            }
        }

        private void AnalyzeRecursive(Transform current, ref int total, ref int humanoid, Dictionary<HumanBodyBones, string> identified)
        {
            total++;

            if (BoneNameDatabase.TryGetBoneFromName(current.name, out var boneType))
            {
                humanoid++;
                if (!identified.ContainsKey(boneType))
                {
                    identified[boneType] = current.name;
                }
            }

            foreach (Transform child in current)
            {
                AnalyzeRecursive(child, ref total, ref humanoid, identified);
            }
        }

        #endregion
    }
}
