using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.BoneNames;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Utilidad centralizada para encontrar armatures de forma robusta.
    /// Implementa estrategia combinada (Opcion D):
    /// 1. Busqueda por nombre con patrones flexibles (Contains)
    /// 2. Busqueda por huesos de SkinnedMeshRenderer
    /// 3. Validacion con BoneNameDatabase para confirmar huesos humanoid
    /// </summary>
    public static class ArmatureFinder
    {
        #region Patrones de Nombre

        /// <summary>
        /// Patrones comunes para nombres de armature.
        /// Se buscan con Contains, no con Equals.
        /// </summary>
        private static readonly string[] ArmaturePatterns = new[]
        {
            "armature",
            "skeleton",
            "rig",
            "bones",
            "root",
            "bip01",
            "bip001"
        };

        /// <summary>
        /// Nombres exactos que tienen prioridad (para compatibilidad).
        /// Se buscan SOLO en hijos directos, no recursivamente.
        /// NOTA: No incluir "Hips" porque es un hueso, no un armature.
        /// </summary>
        private static readonly string[] ExactArmatureNames = new[]
        {
            "Armature",
            "armature",
            "Skeleton",
            "skeleton",
            "Root",
            "root",
            "Rig",
            "rig",
            "Bones",
            "bones",
            "Bip01",
            "bip01"
        };

        /// <summary>
        /// Patrones que indican que NO es un armature (es un mesh).
        /// NOTA: No incluir "cloth" ni "outfit" porque son patrones de ropa legitimos.
        /// </summary>
        private static readonly string[] MeshPatterns = new[]
        {
            "mesh",
            "body",
            "skin",
            "face"
        };

        /// <summary>
        /// Huesos humanoid principales para validacion rapida.
        /// Si encontramos varios de estos, es un armature valido.
        /// </summary>
        private static readonly string[] CoreHumanoidBones = new[]
        {
            "hips", "hip", "pelvis",
            "spine",
            "chest",
            "neck",
            "head",
            "shoulder",
            "arm", "upperarm", "lowerarm",
            "hand", "wrist",
            "leg", "upperleg", "lowerleg", "thigh",
            "foot", "ankle"
        };

        /// <summary>
        /// Minimo de huesos humanoid para considerar valido un armature.
        /// </summary>
        private const int MIN_HUMANOID_BONES = 3;

        #endregion

        #region API Publica

        /// <summary>
        /// Resultado de la busqueda de armature.
        /// </summary>
        public class FindResult
        {
            public Transform Armature;
            public FindMethod Method;
            public int HumanoidBoneCount;
            public string Details;

            public bool Success => Armature != null;
        }

        /// <summary>
        /// Metodo usado para encontrar el armature.
        /// </summary>
        public enum FindMethod
        {
            None,
            HumanoidAPI,
            ExactName,
            PatternName,
            BoneAnalysis,
            Fallback
        }

        /// <summary>
        /// Encuentra el armature dentro de un contenedor usando estrategia combinada.
        /// </summary>
        /// <param name="container">Transform contenedor donde buscar (ej: raiz de ropa)</param>
        /// <param name="animator">Animator opcional para usar Humanoid API</param>
        /// <returns>Resultado con el armature encontrado y metodo usado</returns>
        public static FindResult FindArmature(Transform container, Animator animator = null)
        {
            var result = new FindResult { Method = FindMethod.None };

            if (container == null)
            {
                result.Details = "Container es null";
                return result;
            }

            // Log de inicio para debugging
            Debug.Log($"[ArmatureFinder] Buscando armature en '{container.name}' (hijos: {container.childCount})");

            // Listar hijos para debugging
            foreach (Transform child in container)
            {
                Debug.Log($"[ArmatureFinder]   Hijo: '{child.name}' (childCount: {child.childCount})");
            }

            // Nivel 1: Humanoid API (si hay animator humanoid)
            if (animator != null && animator.isHuman)
            {
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null)
                {
                    result.Armature = hips.parent;
                    result.Method = FindMethod.HumanoidAPI;
                    result.HumanoidBoneCount = CountHumanoidBones(result.Armature);
                    result.Details = "Encontrado via Humanoid API (Hips.parent)";
                    Debug.Log($"[ArmatureFinder] Nivel 1 (Humanoid API): Encontrado '{result.Armature.name}'");
                    return result;
                }
            }

            // Nivel 2: Nombre exacto (compatibilidad)
            result = FindByExactName(container);
            if (result.Success)
            {
                Debug.Log($"[ArmatureFinder] Nivel 2 (Nombre exacto): Encontrado '{result.Armature.name}'");
                return result;
            }

            // Nivel 3: Patron de nombre (Contains)
            result = FindByNamePattern(container);
            if (result.Success)
            {
                Debug.Log($"[ArmatureFinder] Nivel 3 (Patron): Encontrado '{result.Armature.name}'");
                return result;
            }

            // Nivel 4: Analisis de huesos de SkinnedMeshRenderer
            result = FindByBoneAnalysis(container);
            if (result.Success)
            {
                Debug.Log($"[ArmatureFinder] Nivel 4 (Huesos SMR): Encontrado '{result.Armature.name}'");
                return result;
            }

            // Nivel 5: Fallback - primer hijo con hijos que no sea mesh
            result = FindByFallback(container);
            if (result.Success)
            {
                Debug.Log($"[ArmatureFinder] Nivel 5 (Fallback): Encontrado '{result.Armature.name}'");
            }
            else
            {
                Debug.LogWarning($"[ArmatureFinder] No se encontro armature en '{container.name}'");
            }
            return result;
        }

        /// <summary>
        /// Version simplificada que solo retorna el Transform.
        /// </summary>
        public static Transform Find(Transform container, Animator animator = null)
        {
            return FindArmature(container, animator).Armature;
        }

        /// <summary>
        /// Valida si un Transform es un armature valido (tiene huesos humanoid).
        /// </summary>
        public static bool IsValidArmature(Transform candidate)
        {
            if (candidate == null)
                return false;

            int humanoidCount = CountHumanoidBones(candidate);
            return humanoidCount >= MIN_HUMANOID_BONES;
        }

        /// <summary>
        /// Cuenta cuantos huesos humanoid tiene un armature.
        /// </summary>
        public static int CountHumanoidBones(Transform armature, bool logDetails = false)
        {
            if (armature == null)
                return 0;

            int count = 0;
            var allChildren = armature.GetComponentsInChildren<Transform>(true);
            var foundBones = new List<string>();

            foreach (var child in allChildren)
            {
                if (IsHumanoidBoneName(child.name))
                {
                    count++;
                    if (logDetails)
                        foundBones.Add(child.name);
                }
            }

            if (logDetails && foundBones.Count > 0)
            {
                Debug.Log($"[ArmatureFinder] Huesos humanoid en '{armature.name}': {string.Join(", ", foundBones)}");
            }

            return count;
        }

        #endregion

        #region Metodos de Busqueda

        /// <summary>
        /// Nivel 2: Busqueda por nombre exacto SOLO en hijos directos.
        /// No busca recursivamente para evitar encontrar huesos en lugar de armatures.
        /// </summary>
        private static FindResult FindByExactName(Transform container)
        {
            var result = new FindResult { Method = FindMethod.ExactName };

            // Buscar SOLO en hijos directos
            foreach (Transform child in container)
            {
                foreach (var name in ExactArmatureNames)
                {
                    if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[ArmatureFinder]   ExactName - Encontrado '{child.name}' como hijo directo");
                        if (ValidateCandidate(child, result))
                        {
                            result.Details = $"Encontrado por nombre exacto: '{name}'";
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Nivel 3: Busqueda por patron de nombre (Contains).
        /// </summary>
        private static FindResult FindByNamePattern(Transform container)
        {
            var result = new FindResult { Method = FindMethod.PatternName };
            Transform bestCandidate = null;
            string bestPattern = null;

            // Buscar en hijos directos primero
            foreach (Transform child in container)
            {
                string nameLower = child.name.ToLowerInvariant();

                // Excluir si parece un mesh
                if (IsMeshName(nameLower))
                {
                    Debug.Log($"[ArmatureFinder]   Patron - Excluido '{child.name}' (parece mesh)");
                    continue;
                }

                // Verificar si contiene patron de armature
                foreach (var pattern in ArmaturePatterns)
                {
                    if (nameLower.Contains(pattern))
                    {
                        Debug.Log($"[ArmatureFinder]   Patron - '{child.name}' coincide con '{pattern}', validando...");

                        // Primero intentar validacion estricta
                        if (ValidateCandidate(child, result))
                        {
                            result.Details = $"Encontrado por patron '{pattern}' en nombre '{child.name}'";
                            return result;
                        }

                        // Si falla validacion estricta, guardar como candidato
                        // (lo usaremos si no hay mejor opcion)
                        if (bestCandidate == null && child.childCount > 0)
                        {
                            bestCandidate = child;
                            bestPattern = pattern;
                            Debug.Log($"[ArmatureFinder]   Patron - '{child.name}' guardado como candidato (validacion estricta fallo)");
                        }
                        break; // Ya coincidio con un patron, no seguir buscando patrones
                    }
                }
            }

            // Si encontramos un candidato por patron pero no paso validacion estricta,
            // usarlo de todas formas (es mejor que nada)
            if (bestCandidate != null)
            {
                result.Armature = bestCandidate;
                result.HumanoidBoneCount = CountHumanoidBones(bestCandidate);
                result.Details = $"Encontrado por patron '{bestPattern}' en '{bestCandidate.name}' (validacion relajada)";
                Debug.Log($"[ArmatureFinder]   Patron - Usando candidato '{bestCandidate.name}' con validacion relajada");
                return result;
            }

            // Buscar recursivamente si no encontramos en hijos directos
            var allChildren = container.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child == container)
                    continue;

                string nameLower = child.name.ToLowerInvariant();

                if (IsMeshName(nameLower))
                    continue;

                foreach (var pattern in ArmaturePatterns)
                {
                    if (nameLower.Contains(pattern))
                    {
                        if (ValidateCandidate(child, result))
                        {
                            result.Details = $"Encontrado recursivamente por patron '{pattern}' en '{child.name}'";
                            return result;
                        }

                        // Guardar como candidato si tiene hijos
                        if (bestCandidate == null && child.childCount > 0)
                        {
                            bestCandidate = child;
                            bestPattern = pattern;
                        }
                        break;
                    }
                }
            }

            // Usar candidato recursivo si existe
            if (bestCandidate != null)
            {
                result.Armature = bestCandidate;
                result.HumanoidBoneCount = CountHumanoidBones(bestCandidate);
                result.Details = $"Encontrado recursivamente por patron '{bestPattern}' en '{bestCandidate.name}' (validacion relajada)";
                return result;
            }

            return result;
        }

        /// <summary>
        /// Nivel 4: Busqueda por analisis de huesos de SkinnedMeshRenderer.
        /// Encuentra los SMRs, obtiene sus huesos, y sube por la jerarquia.
        /// </summary>
        private static FindResult FindByBoneAnalysis(Transform container)
        {
            var result = new FindResult { Method = FindMethod.BoneAnalysis };

            // Obtener todos los SkinnedMeshRenderers
            var smrs = container.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var smr in smrs)
            {
                if (smr.bones == null || smr.bones.Length == 0)
                    continue;

                // Obtener el primer hueso valido
                Transform firstBone = smr.bones.FirstOrDefault(b => b != null);
                if (firstBone == null)
                    continue;

                // Buscar hacia arriba hasta encontrar un ancestro que sea armature
                Transform candidate = FindArmatureAncestor(firstBone, container);
                if (candidate != null && ValidateCandidate(candidate, result))
                {
                    result.Details = $"Encontrado via huesos de '{smr.name}' -> '{candidate.name}'";
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Nivel 5: Fallback - primer hijo con hijos que no sea mesh.
        /// </summary>
        private static FindResult FindByFallback(Transform container)
        {
            var result = new FindResult { Method = FindMethod.Fallback };

            foreach (Transform child in container)
            {
                // Debe tener hijos
                if (child.childCount == 0)
                    continue;

                // No debe ser un mesh
                if (child.GetComponent<Renderer>() != null)
                    continue;

                string nameLower = child.name.ToLowerInvariant();
                if (IsMeshName(nameLower))
                    continue;

                // Validar que tenga huesos humanoid
                if (ValidateCandidate(child, result))
                {
                    result.Details = $"Encontrado por fallback: '{child.name}' (primer hijo con hijos)";
                    return result;
                }
            }

            // Ultimo recurso: cualquier hijo con hijos
            foreach (Transform child in container)
            {
                if (child.childCount > 0 && child.GetComponent<Renderer>() == null)
                {
                    result.Armature = child;
                    result.HumanoidBoneCount = CountHumanoidBones(child);
                    result.Details = $"Fallback sin validacion: '{child.name}'";
                    return result;
                }
            }

            result.Details = "No se encontro armature";
            return result;
        }

        #endregion

        #region Utilidades

        /// <summary>
        /// Busca un hijo por nombre exacto (case-insensitive), recursivamente.
        /// </summary>
        private static Transform FindChildByExactName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            // Busqueda recursiva
            foreach (Transform child in parent)
            {
                var found = FindChildByExactName(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Sube por la jerarquia desde un hueso hasta encontrar el armature.
        /// </summary>
        private static Transform FindArmatureAncestor(Transform bone, Transform container)
        {
            Transform current = bone.parent;
            Transform lastValid = null;

            while (current != null && current != container)
            {
                // Si este nivel tiene multiples hijos con nombres de huesos,
                // probablemente sea el armature o un nivel superior
                int humanoidChildren = 0;
                foreach (Transform child in current)
                {
                    if (IsHumanoidBoneName(child.name))
                        humanoidChildren++;
                }

                if (humanoidChildren >= 2)
                {
                    lastValid = current;
                }

                // Si llegamos al padre directo del container, retornar el ultimo valido
                if (current.parent == container)
                {
                    return lastValid ?? current;
                }

                current = current.parent;
            }

            return lastValid;
        }

        /// <summary>
        /// Valida si un candidato es un armature valido y actualiza el resultado.
        /// </summary>
        private static bool ValidateCandidate(Transform candidate, FindResult result)
        {
            if (candidate == null)
                return false;

            int humanoidCount = CountHumanoidBones(candidate, logDetails: true);

            Debug.Log($"[ArmatureFinder]     ValidateCandidate '{candidate.name}': " +
                     $"humanoidCount={humanoidCount}, childCount={candidate.childCount}, minRequired={MIN_HUMANOID_BONES}");

            if (humanoidCount >= MIN_HUMANOID_BONES)
            {
                result.Armature = candidate;
                result.HumanoidBoneCount = humanoidCount;
                Debug.Log($"[ArmatureFinder]     -> VALIDO (cumple minimo de huesos)");
                return true;
            }

            // Si no cumple el minimo pero tiene al menos un hueso, podria ser valido
            // en casos de ropa parcial (solo falda, solo guantes, etc.)
            if (humanoidCount > 0 && candidate.childCount > 0)
            {
                result.Armature = candidate;
                result.HumanoidBoneCount = humanoidCount;
                Debug.Log($"[ArmatureFinder]     -> VALIDO (tiene huesos y hijos)");
                return true;
            }

            Debug.Log($"[ArmatureFinder]     -> INVALIDO (humanoidCount={humanoidCount})");
            return false;
        }

        /// <summary>
        /// Verifica si un nombre parece ser de mesh (no armature).
        /// </summary>
        private static bool IsMeshName(string nameLower)
        {
            foreach (var pattern in MeshPatterns)
            {
                if (nameLower.Contains(pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si un nombre de hueso es reconocido como humanoid.
        /// Usa tanto patrones simples como BoneNameDatabase.
        /// </summary>
        private static bool IsHumanoidBoneName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return false;

            // Normalizar nombre: quitar numeros, puntos, guiones, espacios
            string normalized = boneName.ToLowerInvariant();

            // Quitar sufijos numericos comunes como ".1", ".001", "_1", etc.
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[\._\-]\d+$", "");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\d+$", "");

            // Quitar caracteres especiales
            normalized = normalized
                .Replace("_", "")
                .Replace(".", "")
                .Replace("-", "")
                .Replace(" ", "");

            // Verificar patrones simples primero (mas rapido)
            foreach (var pattern in CoreHumanoidBones)
            {
                if (normalized.Contains(pattern))
                    return true;
            }

            // Usar BoneNameDatabase para verificacion mas precisa
            // Probar tanto con nombre original como normalizado
            if (BoneNameDatabase.IsKnownHumanoidBone(boneName))
                return true;

            if (BoneNameDatabase.IsKnownHumanoidBone(normalized))
                return true;

            return false;
        }

        #endregion
    }
}
