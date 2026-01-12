using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.BoneNames
{
    /// <summary>
    /// Base de datos de patrones de nombres de huesos para detección heurística.
    /// Basado en patrones de la comunidad VRChat (HhotateA, Azukimochi, bd_).
    /// Soporta: Blender, MMD, VRM, Unity Humanoid, Character Creator, y más.
    /// </summary>
    public static class BoneNameDatabase
    {
        #region Regex Patterns

        private static readonly Regex PatEndNumber = new Regex(@"[_\.][0-9]+$");
        private static readonly Regex PatEndSide = new Regex(@"[_\.]([LR])$");
        private static readonly Regex PatVrmBone = new Regex(@"^([LRC])_(.*)$");
        private static readonly Regex PatNormalize = new Regex(@"^bone_|[0-9 ._\-]");

        #endregion

        #region Bone Name Patterns

        /// <summary>
        /// Patrones de nombres de huesos organizados por HumanBodyBones.
        /// El índice corresponde al valor del enum HumanBodyBones.
        /// Cada array contiene variantes de nombres para ese hueso.
        /// </summary>
        private static readonly string[][] BoneNamePatterns = new[]
        {
            // 0: Hips
            new[] { "Hips", "Hip", "pelvis", "Pelvis", "hip", "hips" },

            // 1: LeftUpperLeg
            new[] { "LeftUpperLeg", "UpperLeg_Left", "UpperLeg_L", "Leg_Left", "Leg_L", "ULeg_L",
                    "Left leg", "LeftUpLeg", "UpLeg.L", "Thigh_L", "LeftThigh", "thigh_L", "Upper_Leg_L" },

            // 2: RightUpperLeg
            new[] { "RightUpperLeg", "UpperLeg_Right", "UpperLeg_R", "Leg_Right", "Leg_R", "ULeg_R",
                    "Right leg", "RightUpLeg", "UpLeg.R", "Thigh_R", "RightThigh", "thigh_R", "Upper_Leg_R" },

            // 3: LeftLowerLeg
            new[] { "LeftLowerLeg", "LowerLeg_Left", "LowerLeg_L", "Knee_Left", "Knee_L", "LLeg_L",
                    "Left knee", "LeftLeg", "leg_L", "shin.L", "Shin_L", "Calf_L", "Lower_Leg_L" },

            // 4: RightLowerLeg
            new[] { "RightLowerLeg", "LowerLeg_Right", "LowerLeg_R", "Knee_Right", "Knee_R", "LLeg_R",
                    "Right knee", "RightLeg", "leg_R", "shin.R", "Shin_R", "Calf_R", "Lower_Leg_R" },

            // 5: LeftFoot
            new[] { "LeftFoot", "Foot_Left", "Foot_L", "Ankle_L", "Foot.L.001", "Left ankle",
                    "heel.L", "heel", "LeftAnkle", "foot_L" },

            // 6: RightFoot
            new[] { "RightFoot", "Foot_Right", "Foot_R", "Ankle_R", "Foot.R.001", "Right ankle",
                    "heel.R", "RightAnkle", "foot_R" },

            // 7: Spine
            new[] { "Spine", "spine01", "Spine1", "spine_01", "spine.001", "Spine_01" },

            // 8: Chest
            new[] { "Chest", "Bust", "spine02", "upper_chest", "Spine2", "spine_02", "chest", "Ribcage" },

            // 9: Neck
            new[] { "Neck", "neck", "Neck1" },

            // 10: Head
            new[] { "Head", "head" },

            // 11: LeftShoulder
            new[] { "LeftShoulder", "Shoulder_Left", "Shoulder_L", "shoulder_L", "L_Shoulder", "Clavicle_L" },

            // 12: RightShoulder
            new[] { "RightShoulder", "Shoulder_Right", "Shoulder_R", "shoulder_R", "R_Shoulder", "Clavicle_R" },

            // 13: LeftUpperArm
            new[] { "LeftUpperArm", "UpperArm_Left", "UpperArm_L", "Arm_Left", "Arm_L", "UArm_L",
                    "Left arm", "UpperLeftArm", "arm_L", "Upper_Arm_L" },

            // 14: RightUpperArm
            new[] { "RightUpperArm", "UpperArm_Right", "UpperArm_R", "Arm_Right", "Arm_R", "UArm_R",
                    "Right arm", "UpperRightArm", "arm_R", "Upper_Arm_R" },

            // 15: LeftLowerArm
            new[] { "LeftLowerArm", "LowerArm_Left", "LowerArm_L", "LArm_L", "Left elbow",
                    "LeftForeArm", "Elbow_L", "forearm_L", "ForArm_L", "Lower_Arm_L" },

            // 16: RightLowerArm
            new[] { "RightLowerArm", "LowerArm_Right", "LowerArm_R", "LArm_R", "Right elbow",
                    "RightForeArm", "Elbow_R", "forearm_R", "ForArm_R", "Lower_Arm_R" },

            // 17: LeftHand
            new[] { "LeftHand", "Hand_Left", "Hand_L", "Left wrist", "Wrist_L", "hand_L" },

            // 18: RightHand
            new[] { "RightHand", "Hand_Right", "Hand_R", "Right wrist", "Wrist_R", "hand_R" },

            // 19: LeftToes
            new[] { "LeftToes", "Toes_Left", "Toe_Left", "ToeIK_L", "Toes_L", "Toe_L",
                    "Foot.L.002", "Left Toe", "LeftToeBase", "toe_L" },

            // 20: RightToes
            new[] { "RightToes", "Toes_Right", "Toe_Right", "ToeIK_R", "Toes_R", "Toe_R",
                    "Foot.R.002", "Right Toe", "RightToeBase", "toe_R" },

            // 21: LeftEye
            new[] { "LeftEye", "Eye_Left", "Eye_L", "eye_L" },

            // 22: RightEye
            new[] { "RightEye", "Eye_Right", "Eye_R", "eye_R" },

            // 23: Jaw
            new[] { "Jaw", "jaw" },

            // 24-38: Left Hand Fingers
            new[] { "LeftThumbProximal", "ProximalThumb_Left", "ProximalThumb_L", "Thumb1_L",
                    "ThumbFinger1_L", "LeftHandThumb1", "Thumb Proximal.L", "Thunb1_L", "finger01_01_L" },
            new[] { "LeftThumbIntermediate", "IntermediateThumb_Left", "IntermediateThumb_L", "Thumb2_L",
                    "ThumbFinger2_L", "LeftHandThumb2", "Thumb Intermediate.L", "Thunb2_L", "finger01_02_L" },
            new[] { "LeftThumbDistal", "DistalThumb_Left", "DistalThumb_L", "Thumb3_L", "ThumbFinger3_L",
                    "LeftHandThumb3", "Thumb Distal.L", "Thunb3_L", "finger01_03_L" },

            new[] { "LeftIndexProximal", "ProximalIndex_Left", "ProximalIndex_L", "Index1_L",
                    "IndexFinger1_L", "LeftHandIndex1", "Index Proximal.L", "finger02_01_L", "f_index.01.L" },
            new[] { "LeftIndexIntermediate", "IntermediateIndex_Left", "IntermediateIndex_L", "Index2_L",
                    "IndexFinger2_L", "LeftHandIndex2", "Index Intermediate.L", "finger02_02_L", "f_index.02.L" },
            new[] { "LeftIndexDistal", "DistalIndex_Left", "DistalIndex_L", "Index3_L", "IndexFinger3_L",
                    "LeftHandIndex3", "Index Distal.L", "finger02_03_L", "f_index.03.L" },

            new[] { "LeftMiddleProximal", "ProximalMiddle_Left", "ProximalMiddle_L", "Middle1_L",
                    "MiddleFinger1_L", "LeftHandMiddle1", "Middle Proximal.L", "finger03_01_L", "f_middle.01.L" },
            new[] { "LeftMiddleIntermediate", "IntermediateMiddle_Left", "IntermediateMiddle_L", "Middle2_L",
                    "MiddleFinger2_L", "LeftHandMiddle2", "Middle Intermediate.L", "finger03_02_L", "f_middle.02.L" },
            new[] { "LeftMiddleDistal", "DistalMiddle_Left", "DistalMiddle_L", "Middle3_L", "MiddleFinger3_L",
                    "LeftHandMiddle3", "Middle Distal.L", "finger03_03_L", "f_middle.03.L" },

            new[] { "LeftRingProximal", "ProximalRing_Left", "ProximalRing_L", "Ring1_L", "RingFinger1_L",
                    "LeftHandRing1", "Ring Proximal.L", "finger04_01_L", "f_ring.01.L" },
            new[] { "LeftRingIntermediate", "IntermediateRing_Left", "IntermediateRing_L", "Ring2_L",
                    "RingFinger2_L", "LeftHandRing2", "Ring Intermediate.L", "finger04_02_L", "f_ring.02.L" },
            new[] { "LeftRingDistal", "DistalRing_Left", "DistalRing_L", "Ring3_L", "RingFinger3_L",
                    "LeftHandRing3", "Ring Distal.L", "finger04_03_L", "f_ring.03.L" },

            new[] { "LeftLittleProximal", "ProximalLittle_Left", "ProximalLittle_L", "Little1_L",
                    "LittleFinger1_L", "LeftHandPinky1", "Little Proximal.L", "finger05_01_L", "f_pinky.01.L" },
            new[] { "LeftLittleIntermediate", "IntermediateLittle_Left", "IntermediateLittle_L", "Little2_L",
                    "LittleFinger2_L", "LeftHandPinky2", "Little Intermediate.L", "finger05_02_L", "f_pinky.02.L" },
            new[] { "LeftLittleDistal", "DistalLittle_Left", "DistalLittle_L", "Little3_L", "LittleFinger3_L",
                    "LeftHandPinky3", "Little Distal.L", "finger05_03_L", "f_pinky.03.L" },

            // 39-53: Right Hand Fingers
            new[] { "RightThumbProximal", "ProximalThumb_Right", "ProximalThumb_R", "Thumb1_R",
                    "ThumbFinger1_R", "RightHandThumb1", "Thumb Proximal.R", "Thunb1_R", "finger01_01_R" },
            new[] { "RightThumbIntermediate", "IntermediateThumb_Right", "IntermediateThumb_R", "Thumb2_R",
                    "ThumbFinger2_R", "RightHandThumb2", "Thumb Intermediate.R", "Thunb2_R", "finger01_02_R" },
            new[] { "RightThumbDistal", "DistalThumb_Right", "DistalThumb_R", "Thumb3_R", "ThumbFinger3_R",
                    "RightHandThumb3", "Thumb Distal.R", "Thunb3_R", "finger01_03_R" },

            new[] { "RightIndexProximal", "ProximalIndex_Right", "ProximalIndex_R", "Index1_R",
                    "IndexFinger1_R", "RightHandIndex1", "Index Proximal.R", "finger02_01_R", "f_index.01.R" },
            new[] { "RightIndexIntermediate", "IntermediateIndex_Right", "IntermediateIndex_R", "Index2_R",
                    "IndexFinger2_R", "RightHandIndex2", "Index Intermediate.R", "finger02_02_R", "f_index.02.R" },
            new[] { "RightIndexDistal", "DistalIndex_Right", "DistalIndex_R", "Index3_R", "IndexFinger3_R",
                    "RightHandIndex3", "Index Distal.R", "finger02_03_R", "f_index.03.R" },

            new[] { "RightMiddleProximal", "ProximalMiddle_Right", "ProximalMiddle_R", "Middle1_R",
                    "MiddleFinger1_R", "RightHandMiddle1", "Middle Proximal.R", "finger03_01_R", "f_middle.01.R" },
            new[] { "RightMiddleIntermediate", "IntermediateMiddle_Right", "IntermediateMiddle_R", "Middle2_R",
                    "MiddleFinger2_R", "RightHandMiddle2", "Middle Intermediate.R", "finger03_02_R", "f_middle.02.R" },
            new[] { "RightMiddleDistal", "DistalMiddle_Right", "DistalMiddle_R", "Middle3_R", "MiddleFinger3_R",
                    "RightHandMiddle3", "Middle Distal.R", "finger03_03_R", "f_middle.03.R" },

            new[] { "RightRingProximal", "ProximalRing_Right", "ProximalRing_R", "Ring1_R", "RingFinger1_R",
                    "RightHandRing1", "Ring Proximal.R", "finger04_01_R", "f_ring.01.R" },
            new[] { "RightRingIntermediate", "IntermediateRing_Right", "IntermediateRing_R", "Ring2_R",
                    "RingFinger2_R", "RightHandRing2", "Ring Intermediate.R", "finger04_02_R", "f_ring.02.R" },
            new[] { "RightRingDistal", "DistalRing_Right", "DistalRing_R", "Ring3_R", "RingFinger3_R",
                    "RightHandRing3", "Ring Distal.R", "finger04_03_R", "f_ring.03.R" },

            new[] { "RightLittleProximal", "ProximalLittle_Right", "ProximalLittle_R", "Little1_R",
                    "LittleFinger1_R", "RightHandPinky1", "Little Proximal.R", "finger05_01_R", "f_pinky.01.R" },
            new[] { "RightLittleIntermediate", "IntermediateLittle_Right", "IntermediateLittle_R", "Little2_R",
                    "LittleFinger2_R", "RightHandPinky2", "Little Intermediate.R", "finger05_02_R", "f_pinky.02.R" },
            new[] { "RightLittleDistal", "DistalLittle_Right", "DistalLittle_R", "Little3_R", "LittleFinger3_R",
                    "RightHandPinky3", "Little Distal.R", "finger05_03_R", "f_pinky.03.R" },

            // 54: UpperChest
            new[] { "UpperChest", "UChest", "upper_chest", "Spine3", "spine03", "UpperChest1" },
        };

        #endregion

        #region Lookup Dictionaries

        private static Dictionary<string, List<HumanBodyBones>> _nameToBoneMap;
        private static Dictionary<HumanBodyBones, List<string>> _boneToNameMap;
        private static HashSet<string> _allNormalizedNames;
        private static bool _isInitialized = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa los diccionarios de búsqueda. Se llama automáticamente en el primer uso.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            _nameToBoneMap = new Dictionary<string, List<HumanBodyBones>>(StringComparer.OrdinalIgnoreCase);
            _boneToNameMap = new Dictionary<HumanBodyBones, List<string>>();
            _allNormalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < BoneNamePatterns.Length; i++)
            {
                var bone = (HumanBodyBones)i;
                var patterns = BoneNamePatterns[i];

                if (!_boneToNameMap.ContainsKey(bone))
                {
                    _boneToNameMap[bone] = new List<string>();
                }

                foreach (var name in patterns)
                {
                    var normalizedName = NormalizeName(name);

                    // Registrar nombre -> hueso
                    if (!_nameToBoneMap.TryGetValue(normalizedName, out var boneList))
                    {
                        boneList = new List<HumanBodyBones>();
                        _nameToBoneMap[normalizedName] = boneList;
                    }
                    if (!boneList.Contains(bone))
                    {
                        boneList.Add(bone);
                    }

                    // Registrar hueso -> nombre
                    if (!_boneToNameMap[bone].Contains(normalizedName))
                    {
                        _boneToNameMap[bone].Add(normalizedName);
                    }

                    _allNormalizedNames.Add(normalizedName);

                    // Generar variantes adicionales
                    GenerateVariants(name, bone);
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Genera variantes adicionales de nombres (VRM, side swaps, etc.)
        /// </summary>
        private static void GenerateVariants(string name, HumanBodyBones bone)
        {
            // Variante con lado al principio (L.Arm -> Arm_L)
            var matchSide = PatEndSide.Match(name);
            if (matchSide.Success)
            {
                var altName = name.Substring(0, name.Length - 2);
                altName = matchSide.Groups[1].Value + "." + altName;
                RegisterVariant(NormalizeName(altName), bone);
            }
            else
            {
                // VRM pattern: C.[bone] para huesos centrales
                var altName = "C." + name;
                RegisterVariant(NormalizeName(altName), bone);
            }
        }

        private static void RegisterVariant(string normalizedName, HumanBodyBones bone)
        {
            if (!_nameToBoneMap.TryGetValue(normalizedName, out var boneList))
            {
                boneList = new List<HumanBodyBones>();
                _nameToBoneMap[normalizedName] = boneList;
            }
            if (!boneList.Contains(bone))
            {
                boneList.Add(bone);
            }
            _allNormalizedNames.Add(normalizedName);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Normaliza un nombre de hueso para comparación.
        /// Convierte a minúsculas y elimina números, espacios, guiones bajos, puntos.
        /// </summary>
        public static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;

            name = name.ToLowerInvariant();
            name = PatNormalize.Replace(name, "");
            return name;
        }

        /// <summary>
        /// Intenta encontrar el HumanBodyBones correspondiente a un nombre de hueso.
        /// </summary>
        /// <param name="boneName">Nombre del hueso</param>
        /// <param name="result">HumanBodyBones encontrado</param>
        /// <returns>true si se encontró coincidencia</returns>
        public static bool TryGetBoneFromName(string boneName, out HumanBodyBones result)
        {
            EnsureInitialized();
            result = HumanBodyBones.LastBone;

            if (string.IsNullOrEmpty(boneName)) return false;

            var normalized = NormalizeName(boneName);

            if (_nameToBoneMap.TryGetValue(normalized, out var bones) && bones.Count > 0)
            {
                result = bones[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Obtiene todos los HumanBodyBones posibles para un nombre de hueso.
        /// </summary>
        public static List<HumanBodyBones> GetPossibleBones(string boneName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boneName))
                return new List<HumanBodyBones>();

            var normalized = NormalizeName(boneName);

            if (_nameToBoneMap.TryGetValue(normalized, out var bones))
            {
                return new List<HumanBodyBones>(bones);
            }

            return new List<HumanBodyBones>();
        }

        /// <summary>
        /// Obtiene todas las variantes de nombre para un HumanBodyBones.
        /// </summary>
        public static List<string> GetBoneNameVariants(HumanBodyBones bone)
        {
            EnsureInitialized();

            if (_boneToNameMap.TryGetValue(bone, out var names))
            {
                return new List<string>(names);
            }

            return new List<string>();
        }

        /// <summary>
        /// Verifica si un nombre de hueso es reconocido como humanoid.
        /// </summary>
        public static bool IsKnownHumanoidBone(string boneName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boneName)) return false;

            var normalized = NormalizeName(boneName);
            return _allNormalizedNames.Contains(normalized);
        }

        /// <summary>
        /// Intenta encontrar un hueso del avatar que coincida con un nombre dado.
        /// Usa Humanoid API primero, luego fallback a patrones de nombre.
        /// </summary>
        /// <param name="boneName">Nombre del hueso a buscar</param>
        /// <param name="avatarAnimator">Animator del avatar</param>
        /// <param name="avatarRoot">Transform raíz del avatar para búsqueda por nombre</param>
        /// <returns>Transform del hueso encontrado, o null</returns>
        public static Transform FindMatchingBone(string boneName, Animator avatarAnimator, Transform avatarRoot)
        {
            if (string.IsNullOrEmpty(boneName)) return null;

            EnsureInitialized();

            // 1. Primero intentar Humanoid API
            if (avatarAnimator != null && avatarAnimator.isHuman)
            {
                if (TryGetBoneFromName(boneName, out var humanBone))
                {
                    var avatarBone = avatarAnimator.GetBoneTransform(humanBone);
                    if (avatarBone != null) return avatarBone;
                }
            }

            // 2. Búsqueda por nombre exacto (case-insensitive)
            if (avatarRoot != null)
            {
                var exactMatch = FindBoneByName(avatarRoot, boneName);
                if (exactMatch != null) return exactMatch;
            }

            // 3. Búsqueda por variantes de nombre
            var possibleBones = GetPossibleBones(boneName);
            foreach (var humanBone in possibleBones)
            {
                // Intentar Humanoid API con cada posible match
                if (avatarAnimator != null && avatarAnimator.isHuman)
                {
                    var avatarBone = avatarAnimator.GetBoneTransform(humanBone);
                    if (avatarBone != null) return avatarBone;
                }

                // Intentar búsqueda por variantes de nombre
                var variants = GetBoneNameVariants(humanBone);
                foreach (var variant in variants)
                {
                    var match = FindBoneByNormalizedName(avatarRoot, variant);
                    if (match != null) return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Busca un hueso por nombre exacto (case-insensitive) en la jerarquía.
        /// </summary>
        private static Transform FindBoneByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;

            // Búsqueda BFS para mejor rendimiento
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (string.Equals(current.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Busca un hueso por nombre normalizado en la jerarquía.
        /// </summary>
        private static Transform FindBoneByNormalizedName(Transform root, string normalizedName)
        {
            if (root == null || string.IsNullOrEmpty(normalizedName)) return null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (string.Equals(NormalizeName(current.name), normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Calcula la similitud entre dos nombres de huesos (0-1).
        /// </summary>
        public static float CalculateSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2)) return 0f;

            var norm1 = NormalizeName(name1);
            var norm2 = NormalizeName(name2);

            if (norm1 == norm2) return 1f;

            // Levenshtein distance normalizado
            int distance = LevenshteinDistance(norm1, norm2);
            int maxLen = Math.Max(norm1.Length, norm2.Length);

            if (maxLen == 0) return 1f;

            return 1f - (float)distance / maxLen;
        }

        private static int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        #endregion
    }
}
