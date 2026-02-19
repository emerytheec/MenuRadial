using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// Detector de pelucas basado en scoring multi-señal.
    /// Identifica pelucas entre los hijos del avatar usando heurísticas combinadas.
    /// Threshold de 7 puntos para considerar algo como peluca.
    /// </summary>
    public static class WigDetector
    {
        #region Configuración

        private const int WIG_SCORE_THRESHOLD = 7;

        /// <summary>
        /// Patrones de nombre que indican pelo/peluca (lowercase).
        /// </summary>
        private static readonly string[] HairNamePatterns = new[]
        {
            "hair", "wig", "pelo", "cabello", "peluca",
            "bangs", "fringe", "ponytail", "braid", "strand",
            "flequillo", "coleta", "trenza"
        };

        /// <summary>
        /// Huesos de extremidades (brazos y piernas). Su ausencia indica peluca.
        /// </summary>
        private static readonly HumanBodyBones[] LimbBones = new[]
        {
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
        };

        /// <summary>
        /// Nombres normalizados de huesos de extremidades para matching sin Animator.
        /// </summary>
        private static readonly string[] LimbBoneNames = new[]
        {
            "upperarm", "lowerarm", "hand", "wrist",
            "upperleg", "lowerleg", "thigh", "foot", "ankle",
            "shoulder", "elbow", "knee"
        };

        #endregion

        #region API Pública

        /// <summary>
        /// Candidato a peluca detectado.
        /// </summary>
        public struct WigCandidate
        {
            /// <summary>GameObject raíz del contenedor de la peluca.</summary>
            public GameObject Root;

            /// <summary>Transform raíz del armature de la peluca (puede ser null si no tiene).</summary>
            public Transform ArmatureRoot;

            /// <summary>Meshes asociados a la peluca.</summary>
            public List<SkinnedMeshRenderer> Meshes;

            /// <summary>Nombre para mostrar en UI.</summary>
            public string Name;

            /// <summary>Puntuación total del scoring.</summary>
            public int Score;

            /// <summary>Índice en DetectedClothings si fue reclasificado desde ropa, -1 si no.</summary>
            public int ClothingEntryIndex;
        }

        /// <summary>
        /// Detecta pelucas entre los hijos del avatar.
        /// Fuente 1: Reclasifica ClothingEntries que parecen pelucas.
        /// Fuente 2: Escanea hijos directos no detectados como ropa.
        /// </summary>
        /// <param name="avatarRoot">GameObject raíz del avatar.</param>
        /// <param name="detectedClothings">Lista de ropas detectadas por MRCoserRopa (puede ser null).</param>
        /// <returns>Lista de candidatos a peluca que superan el threshold.</returns>
        public static List<WigCandidate> DetectWigs(GameObject avatarRoot, List<ClothingEntry> detectedClothings)
        {
            var wigs = new List<WigCandidate>();

            if (avatarRoot == null)
                return wigs;

            var avatarAnimator = avatarRoot.GetComponent<Animator>();

            // Obtener armature del avatar para saber qué hijos son ropa ya detectada
            Transform avatarArmature = null;
            if (avatarAnimator != null && avatarAnimator.isHuman)
            {
                var hips = avatarAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null)
                    avatarArmature = hips.parent;
            }
            if (avatarArmature == null)
                avatarArmature = BodyMeshDetector.FindArmature(avatarRoot.transform);

            // Set de GameObjects ya cubiertos por detectedClothings
            var clothingRoots = new HashSet<GameObject>();
            if (detectedClothings != null)
            {
                foreach (var c in detectedClothings)
                {
                    if (c?.GameObject != null)
                        clothingRoots.Add(c.GameObject);
                }
            }

            // Fuente 1: Reclasificar ClothingEntries
            if (detectedClothings != null)
            {
                for (int i = 0; i < detectedClothings.Count; i++)
                {
                    var clothing = detectedClothings[i];
                    if (!clothing.IsValid)
                        continue;

                    int score = ScoreClothingEntry(clothing, avatarAnimator);
                    if (score >= WIG_SCORE_THRESHOLD)
                    {
                        Transform armature = clothing.ArmatureReference?.ArmatureRoot;
                        var meshes = armature != null
                            ? BodyMeshDetector.GetAllSiblingMeshes(armature)
                            : new List<SkinnedMeshRenderer>();

                        wigs.Add(new WigCandidate
                        {
                            Root = clothing.GameObject,
                            ArmatureRoot = armature,
                            Meshes = meshes,
                            Name = clothing.Name,
                            Score = score,
                            ClothingEntryIndex = i
                        });

                        Debug.Log($"[WigDetector] Reclasificado '{clothing.Name}' como peluca (score={score})");
                    }
                }
            }

            // Fuente 2: Hijos directos del avatar no detectados como ropa
            foreach (Transform child in avatarRoot.transform)
            {
                // Saltar avatar armature y meshes del avatar
                if (avatarArmature != null && child == avatarArmature)
                    continue;

                // Saltar si ya fue detectado como ropa
                if (clothingRoots.Contains(child.gameObject))
                    continue;

                // Saltar si ya fue detectado como peluca en Fuente 1
                if (wigs.Any(w => w.Root == child.gameObject))
                    continue;

                // Necesita tener al menos un SkinnedMeshRenderer
                var meshes = child.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (meshes.Length == 0)
                    continue;

                int score = ScoreUndetectedChild(child, avatarAnimator);
                if (score >= WIG_SCORE_THRESHOLD)
                {
                    // Buscar armature si existe
                    Transform armRoot = ArmatureFinder.Find(child);
                    var siblingMeshes = armRoot != null
                        ? BodyMeshDetector.GetAllSiblingMeshes(armRoot)
                        : new List<SkinnedMeshRenderer>(meshes);

                    wigs.Add(new WigCandidate
                    {
                        Root = child.gameObject,
                        ArmatureRoot = armRoot,
                        Meshes = siblingMeshes,
                        Name = child.name,
                        Score = score,
                        ClothingEntryIndex = -1
                    });

                    Debug.Log($"[WigDetector] Detectado '{child.name}' como peluca no-ropa (score={score})");
                }
            }

            return wigs;
        }

        #endregion

        #region Scoring

        /// <summary>
        /// Calcula score para un ClothingEntry (ya tiene armature detectado).
        /// </summary>
        private static int ScoreClothingEntry(ClothingEntry clothing, Animator avatarAnimator)
        {
            int score = 0;
            Transform armature = clothing.ArmatureReference?.ArmatureRoot;

            if (armature == null)
                return 0;

            // Señal 1: Sin huesos de extremidades (+3)
            if (!HasLimbBones(armature))
                score += 3;

            // Señal 2: Tiene hueso Head (+2)
            if (HasHeadBone(armature))
                score += 2;

            // Señal 3: Mesh hermano con nombre de pelo (+3)
            if (HasHairNamedMesh(armature))
                score += 3;

            // Señal 4: Nombre del contenedor con patrón de pelo (+2)
            if (MatchesHairPattern(clothing.Name))
                score += 2;

            // Señal 5: MA BoneProxy apuntando a Head (+2)
            if (HasMABoneProxyToHead(clothing.GameObject, avatarAnimator))
                score += 2;

            // Señal 6: Head tiene ≥3 hijos (cadenas de pelo) (+2)
            if (HeadHasMultipleChildren(armature))
                score += 2;

            // Señal 7: ≤5 huesos humanoides (+1)
            int humanoidCount = ArmatureFinder.CountHumanoidBones(armature);
            if (humanoidCount <= 5)
                score += 1;

            return score;
        }

        /// <summary>
        /// Calcula score para un hijo del avatar no detectado como ropa.
        /// </summary>
        private static int ScoreUndetectedChild(Transform child, Animator avatarAnimator)
        {
            int score = 0;

            // Señal 3: Nombre con patrón de pelo (+3) — evaluamos nombre del mesh también
            var meshes = child.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool hasHairMesh = false;
            foreach (var mesh in meshes)
            {
                if (MatchesHairPattern(mesh.gameObject.name))
                {
                    hasHairMesh = true;
                    break;
                }
            }
            if (hasHairMesh)
                score += 3;

            // Señal 4: Nombre del contenedor con patrón de pelo (+2)
            if (MatchesHairPattern(child.name))
                score += 2;

            // Señal 5: MA BoneProxy apuntando a Head (+2)
            if (HasMABoneProxyToHead(child.gameObject, avatarAnimator))
                score += 2;

            // Intentar encontrar armature para señales basadas en huesos
            Transform armature = ArmatureFinder.Find(child);
            if (armature != null)
            {
                // Señal 1: Sin huesos de extremidades (+3)
                if (!HasLimbBones(armature))
                    score += 3;

                // Señal 2: Tiene hueso Head (+2)
                if (HasHeadBone(armature))
                    score += 2;

                // Señal 6: Head tiene ≥3 hijos (+2)
                if (HeadHasMultipleChildren(armature))
                    score += 2;

                // Señal 7: ≤5 huesos humanoides (+1)
                int humanoidCount = ArmatureFinder.CountHumanoidBones(armature);
                if (humanoidCount <= 5)
                    score += 1;
            }
            else
            {
                // Sin armature: probablemente accesorio simple.
                // Solo las señales de nombre aplican, que ya fueron evaluadas arriba.
            }

            return score;
        }

        #endregion

        #region Señales individuales

        /// <summary>
        /// Verifica si el armature tiene huesos de extremidades (brazos/piernas).
        /// </summary>
        private static bool HasLimbBones(Transform armature)
        {
            var allChildren = armature.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                string normalized = child.name.ToLowerInvariant()
                    .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                foreach (var limbName in LimbBoneNames)
                {
                    if (normalized.Contains(limbName))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Verifica si el armature tiene un hueso Head.
        /// </summary>
        private static bool HasHeadBone(Transform armature)
        {
            var allChildren = armature.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                string normalized = child.name.ToLowerInvariant()
                    .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                if (normalized == "head" || normalized.EndsWith("head"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si hay un mesh hermano del armature con nombre de pelo.
        /// </summary>
        private static bool HasHairNamedMesh(Transform armature)
        {
            if (armature.parent == null)
                return false;

            foreach (Transform sibling in armature.parent)
            {
                if (sibling == armature)
                    continue;

                var smr = sibling.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && MatchesHairPattern(sibling.name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si un nombre coincide con patrones de pelo/peluca.
        /// </summary>
        private static bool MatchesHairPattern(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string lower = name.ToLowerInvariant();
            foreach (var pattern in HairNamePatterns)
            {
                if (lower.Contains(pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si el objeto tiene un MA BoneProxy apuntando a Head (via reflexión).
        /// </summary>
        private static bool HasMABoneProxyToHead(GameObject obj, Animator avatarAnimator)
        {
            if (obj == null)
                return false;

            var components = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in components)
            {
                if (comp == null)
                    continue;

                string typeName = comp.GetType().Name;
                if (typeName != "ModularAvatarBoneProxy")
                    continue;

                // Intentar leer el campo target via reflexión
                var targetField = comp.GetType().GetField("target",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    var target = targetField.GetValue(comp) as Transform;
                    if (target != null)
                    {
                        string targetName = target.name.ToLowerInvariant()
                            .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                        if (targetName == "head" || targetName.EndsWith("head"))
                            return true;
                    }
                }

                // Intentar leer el campo boneReference (HumanBodyBones enum)
                var boneRefField = comp.GetType().GetField("boneReference",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (boneRefField != null)
                {
                    var value = boneRefField.GetValue(comp);
                    if (value is HumanBodyBones bone && bone == HumanBodyBones.Head)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Verifica si el hueso Head dentro del armature tiene ≥3 hijos (cadenas de pelo para PhysBones).
        /// </summary>
        private static bool HeadHasMultipleChildren(Transform armature)
        {
            var allChildren = armature.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                string normalized = child.name.ToLowerInvariant()
                    .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                if (normalized == "head" || normalized.EndsWith("head"))
                {
                    if (child.childCount >= 3)
                        return true;
                }
            }
            return false;
        }

        #endregion
    }
}
