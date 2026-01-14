using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// Sistema de detección inteligente para identificar meshes del body/head/hair del avatar.
    /// Usa combinación de patrones de nombre + análisis de huesos para mayor precisión.
    /// </summary>
    public static class BodyMeshDetector
    {
        #region Patrones de exclusión

        /// <summary>
        /// Patrones para detectar meshes del cuerpo (body)
        /// </summary>
        private static readonly string[] BodyPatterns = new[]
        {
            "body", "skin", "torso", "nude", "naked", "base", "flesh",
            "cuerpo", "piel", "desnudo"
        };

        /// <summary>
        /// Patrones para detectar meshes de la cabeza
        /// </summary>
        private static readonly string[] HeadPatterns = new[]
        {
            "head", "face", "jaw", "tongue", "teeth", "mouth", "nose", "ear",
            "cabeza", "cara", "lengua", "dientes", "boca", "nariz", "oreja"
        };

        /// <summary>
        /// Patrones para detectar meshes del pelo
        /// </summary>
        private static readonly string[] HairPatterns = new[]
        {
            "hair", "bangs", "fringe", "ponytail", "braid", "strand", "wig",
            "pelo", "cabello", "flequillo", "coleta", "trenza"
        };

        /// <summary>
        /// Patrones para detectar meshes de ojos
        /// </summary>
        private static readonly string[] EyePatterns = new[]
        {
            "eye", "pupil", "iris", "eyelash", "eyelid", "brow", "eyebrow",
            "ojo", "pupila", "pestana", "ceja", "parpado"
        };

        /// <summary>
        /// Patrones para detectar meshes que son claramente ropa/accesorios y NO deben excluirse.
        /// Estos tienen prioridad sobre el análisis de huesos.
        /// </summary>
        private static readonly string[] ClothingPatterns = new[]
        {
            "outfit", "cloth", "dress", "shirt", "pants", "skirt", "shoes", "boots",
            "jacket", "coat", "sweater", "cardigan", "hoodie", "vest", "glove",
            "sock", "stocking", "underwear", "bra", "shorts", "ribbon", "accessory",
            "hat", "cap", "glasses", "bag", "belt", "tie", "scarf", "mask",
            "armor", "armour", "weapon", "sword", "shield",
            "ropa", "vestido", "camisa", "pantalon", "falda", "zapato", "bota",
            "chaqueta", "abrigo", "guante", "calcetin", "accesorio", "sombrero",
            "under_", "item_"
        };

        /// <summary>
        /// Huesos humanoid principales que indica que es un mesh de body
        /// </summary>
        private static readonly HumanBodyBones[] MainBodyBones = new[]
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg
        };

        #endregion

        #region Configuración

        /// <summary>
        /// Umbral de huesos para considerar un mesh como body (70%)
        /// Si un mesh tiene weights en más del 70% de los huesos principales, es body
        /// </summary>
        private const float BONE_WEIGHT_THRESHOLD = 0.7f;

        /// <summary>
        /// Peso mínimo para considerar que un hueso influye en el mesh
        /// </summary>
        private const float MIN_BONE_WEIGHT = 0.01f;

        #endregion

        #region API Pública

        /// <summary>
        /// Resultado de la detección de un mesh
        /// </summary>
        public struct DetectionResult
        {
            public SkinnedMeshRenderer Mesh;
            public bool ShouldExclude;
            public string ExclusionReason;
            public DetectionMethod Method;
        }

        /// <summary>
        /// Método de detección usado
        /// </summary>
        public enum DetectionMethod
        {
            None,
            NamePattern,
            BoneAnalysis,
            Manual
        }

        /// <summary>
        /// Analiza todos los meshes hermanos del armature y determina cuáles excluir.
        /// </summary>
        /// <param name="armatureRoot">Transform raíz del armature</param>
        /// <param name="animator">Animator del avatar (para análisis de huesos)</param>
        /// <param name="manualExclusions">Lista de nombres de mesh a excluir manualmente</param>
        /// <returns>Lista de resultados de detección para cada mesh</returns>
        public static List<DetectionResult> AnalyzeMeshes(
            Transform armatureRoot,
            Animator animator = null,
            HashSet<string> manualExclusions = null)
        {
            var results = new List<DetectionResult>();

            if (armatureRoot == null || armatureRoot.parent == null)
                return results;

            // Obtener el contenedor padre del armature
            Transform container = armatureRoot.parent;

            // Buscar todos los SkinnedMeshRenderer hermanos del armature
            foreach (Transform sibling in container)
            {
                // Saltar el propio armature
                if (sibling == armatureRoot)
                    continue;

                var smr = sibling.GetComponent<SkinnedMeshRenderer>();
                if (smr == null)
                    continue;

                var result = AnalyzeSingleMesh(smr, animator, manualExclusions);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Analiza un mesh individual y determina si debe excluirse.
        /// </summary>
        public static DetectionResult AnalyzeSingleMesh(
            SkinnedMeshRenderer mesh,
            Animator animator = null,
            HashSet<string> manualExclusions = null)
        {
            var result = new DetectionResult
            {
                Mesh = mesh,
                ShouldExclude = false,
                ExclusionReason = null,
                Method = DetectionMethod.None
            };

            if (mesh == null)
                return result;

            string meshName = mesh.gameObject.name.ToLowerInvariant();

            // Nivel 1: Exclusión manual
            if (manualExclusions != null && manualExclusions.Contains(mesh.gameObject.name))
            {
                result.ShouldExclude = true;
                result.ExclusionReason = "Exclusión manual";
                result.Method = DetectionMethod.Manual;
                return result;
            }

            // Nivel 2: Patrones de nombre
            string patternReason = CheckNamePatterns(meshName);
            if (patternReason != null)
            {
                result.ShouldExclude = true;
                result.ExclusionReason = patternReason;
                result.Method = DetectionMethod.NamePattern;
                return result;
            }

            // Nivel 3: Verificar si es claramente ropa/accesorio (no excluir por huesos)
            if (IsClothingByName(meshName))
            {
                // Es ropa/accesorio, NO excluir aunque tenga huesos humanoid
                return result;
            }

            // Nivel 4: Análisis de huesos (si hay animator humanoid)
            // Solo para meshes que no son claramente ropa
            if (animator != null && animator.isHuman)
            {
                if (IsBodyMeshByBones(mesh, animator))
                {
                    result.ShouldExclude = true;
                    result.ExclusionReason = "Detectado como body por análisis de huesos";
                    result.Method = DetectionMethod.BoneAnalysis;
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene los meshes que NO deben excluirse (para capturar en MRAgruparObjetos)
        /// </summary>
        public static List<SkinnedMeshRenderer> GetIncludedMeshes(
            Transform armatureRoot,
            Animator animator = null,
            HashSet<string> manualExclusions = null)
        {
            var results = AnalyzeMeshes(armatureRoot, animator, manualExclusions);
            return results
                .Where(r => !r.ShouldExclude && r.Mesh != null)
                .Select(r => r.Mesh)
                .ToList();
        }

        /// <summary>
        /// Obtiene los meshes que SÍ deben excluirse
        /// </summary>
        public static List<SkinnedMeshRenderer> GetExcludedMeshes(
            Transform armatureRoot,
            Animator animator = null,
            HashSet<string> manualExclusions = null)
        {
            var results = AnalyzeMeshes(armatureRoot, animator, manualExclusions);
            return results
                .Where(r => r.ShouldExclude && r.Mesh != null)
                .Select(r => r.Mesh)
                .ToList();
        }

        #endregion

        #region Detección por Nombre

        /// <summary>
        /// Verifica si el mesh es claramente ropa/accesorio por su nombre.
        /// Estos meshes NO deben excluirse por análisis de huesos.
        /// </summary>
        private static bool IsClothingByName(string meshName)
        {
            foreach (var pattern in ClothingPatterns)
            {
                if (meshName.Contains(pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica si el nombre del mesh coincide con patrones de exclusión
        /// </summary>
        private static string CheckNamePatterns(string meshName)
        {
            // Verificar patrones de body
            foreach (var pattern in BodyPatterns)
            {
                if (meshName.Contains(pattern))
                    return $"Patrón de body: '{pattern}'";
            }

            // Verificar patrones de head
            foreach (var pattern in HeadPatterns)
            {
                if (meshName.Contains(pattern))
                    return $"Patrón de cabeza: '{pattern}'";
            }

            // Verificar patrones de hair
            foreach (var pattern in HairPatterns)
            {
                if (meshName.Contains(pattern))
                    return $"Patrón de pelo: '{pattern}'";
            }

            // Verificar patrones de eyes
            foreach (var pattern in EyePatterns)
            {
                if (meshName.Contains(pattern))
                    return $"Patrón de ojos: '{pattern}'";
            }

            return null;
        }

        #endregion

        #region Detección por Huesos

        /// <summary>
        /// Determina si un mesh es body basándose en cuántos huesos humanoid lo influyen
        /// </summary>
        private static bool IsBodyMeshByBones(SkinnedMeshRenderer mesh, Animator animator)
        {
            if (mesh == null || animator == null || !animator.isHuman)
                return false;

            if (mesh.sharedMesh == null || mesh.bones == null || mesh.bones.Length == 0)
                return false;

            // Obtener todos los huesos que influyen en el mesh
            var meshBones = new HashSet<Transform>(mesh.bones.Where(b => b != null));

            // Contar cuántos huesos principales del body están en el mesh
            int matchedBones = 0;
            foreach (var boneType in MainBodyBones)
            {
                Transform bone = animator.GetBoneTransform(boneType);
                if (bone != null && meshBones.Contains(bone))
                {
                    matchedBones++;
                }
            }

            // Si tiene más del umbral de huesos principales, es body
            float ratio = (float)matchedBones / MainBodyBones.Length;
            return ratio >= BONE_WEIGHT_THRESHOLD;
        }

        #endregion

        #region Utilidades

        /// <summary>
        /// Encuentra el armature dentro de un contenedor
        /// </summary>
        public static Transform FindArmature(Transform container)
        {
            if (container == null)
                return null;

            // Nombres comunes de armature
            string[] armatureNames = { "Armature", "armature", "Skeleton", "skeleton", "Root", "Rig", "Bones" };

            foreach (var name in armatureNames)
            {
                var armature = container.Find(name);
                if (armature != null)
                    return armature;
            }

            // Buscar el primer hijo que tenga muchos hijos (probable armature)
            foreach (Transform child in container)
            {
                // Si tiene hijos y no es un mesh, probablemente es el armature
                if (child.childCount > 0 && child.GetComponent<Renderer>() == null)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// Obtiene todos los meshes hermanos del armature (sin filtrar)
        /// </summary>
        public static List<SkinnedMeshRenderer> GetAllSiblingMeshes(Transform armatureRoot)
        {
            var meshes = new List<SkinnedMeshRenderer>();

            if (armatureRoot == null || armatureRoot.parent == null)
                return meshes;

            Transform container = armatureRoot.parent;

            foreach (Transform sibling in container)
            {
                if (sibling == armatureRoot)
                    continue;

                var smr = sibling.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    meshes.Add(smr);
                }
            }

            return meshes;
        }

        #endregion
    }
}
