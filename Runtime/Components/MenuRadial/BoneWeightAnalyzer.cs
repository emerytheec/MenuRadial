using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// Analizador de pesos de huesos (bone weights) para identificar meshes
    /// cuya geometría está influenciada principalmente por huesos de la cabeza.
    /// Usado para detección precisa de pelucas/pelo.
    /// </summary>
    public static class BoneWeightAnalyzer
    {
        #region Configuración

        /// <summary>
        /// Umbral mínimo de ratio de peso en head bones para considerar un mesh como pelo.
        /// Un mesh con más del 60% de su peso total en huesos de la cabeza se considera pelo.
        /// </summary>
        public const float HEAD_WEIGHT_THRESHOLD = 0.6f;

        /// <summary>
        /// Peso mínimo para considerar que un hueso influye significativamente en un vértice.
        /// Pesos menores a este valor se ignoran para evitar ruido.
        /// </summary>
        private const float MIN_SIGNIFICANT_WEIGHT = 0.01f;

        #endregion

        #region API Pública

        /// <summary>
        /// Resultado del análisis de pesos de un mesh.
        /// </summary>
        public struct WeightProfile
        {
            /// <summary>Mesh analizado.</summary>
            public SkinnedMeshRenderer Mesh;

            /// <summary>Ratio de peso total en huesos de la cabeza (0-1).</summary>
            public float HeadWeightRatio;

            /// <summary>True si el mesh se considera influenciado por la cabeza (ratio > threshold).</summary>
            public bool IsHeadWeighted;

            /// <summary>Cantidad de vértices analizados.</summary>
            public int VertexCount;

            /// <summary>Cantidad de huesos del mesh que son descendientes del Head.</summary>
            public int HeadBoneCount;

            /// <summary>Cantidad total de huesos del mesh.</summary>
            public int TotalBoneCount;
        }

        /// <summary>
        /// Analiza un SkinnedMeshRenderer para determinar qué porcentaje de su peso
        /// de vértices proviene de huesos de la cabeza.
        /// </summary>
        /// <param name="smr">El SkinnedMeshRenderer a analizar.</param>
        /// <param name="headBone">Transform del hueso Head del avatar. Si es null, se intenta detectar por nombre.</param>
        /// <param name="threshold">Umbral para considerar el mesh como head-weighted.</param>
        /// <returns>Perfil de pesos del mesh.</returns>
        public static WeightProfile AnalyzeMesh(SkinnedMeshRenderer smr, Transform headBone, float threshold = HEAD_WEIGHT_THRESHOLD)
        {
            var profile = new WeightProfile
            {
                Mesh = smr,
                HeadWeightRatio = 0f,
                IsHeadWeighted = false,
                VertexCount = 0,
                HeadBoneCount = 0,
                TotalBoneCount = 0
            };

            if (smr == null || smr.sharedMesh == null)
                return profile;

            var mesh = smr.sharedMesh;
            var bones = smr.bones;

            if (bones == null || bones.Length == 0)
                return profile;

            profile.TotalBoneCount = bones.Length;

            // Si no se proporcionó headBone, intentar encontrarlo por nombre en el armature
            if (headBone == null)
                headBone = FindHeadBoneInBones(bones);

            if (headBone == null)
                return profile;

            // Construir set de índices de huesos que son descendientes del Head
            var headBoneIndices = new HashSet<int>();
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && IsDescendantOfOrEqual(bones[i], headBone))
                {
                    headBoneIndices.Add(i);
                }
            }

            profile.HeadBoneCount = headBoneIndices.Count;

            if (headBoneIndices.Count == 0)
                return profile;

            // Analizar bone weights de los vértices
            var boneWeights = mesh.boneWeights;
            if (boneWeights == null || boneWeights.Length == 0)
                return profile;

            profile.VertexCount = boneWeights.Length;

            float totalWeight = 0f;
            float headWeight = 0f;

            for (int v = 0; v < boneWeights.Length; v++)
            {
                var bw = boneWeights[v];

                // Peso 0
                if (bw.weight0 > MIN_SIGNIFICANT_WEIGHT)
                {
                    totalWeight += bw.weight0;
                    if (headBoneIndices.Contains(bw.boneIndex0))
                        headWeight += bw.weight0;
                }

                // Peso 1
                if (bw.weight1 > MIN_SIGNIFICANT_WEIGHT)
                {
                    totalWeight += bw.weight1;
                    if (headBoneIndices.Contains(bw.boneIndex1))
                        headWeight += bw.weight1;
                }

                // Peso 2
                if (bw.weight2 > MIN_SIGNIFICANT_WEIGHT)
                {
                    totalWeight += bw.weight2;
                    if (headBoneIndices.Contains(bw.boneIndex2))
                        headWeight += bw.weight2;
                }

                // Peso 3
                if (bw.weight3 > MIN_SIGNIFICANT_WEIGHT)
                {
                    totalWeight += bw.weight3;
                    if (headBoneIndices.Contains(bw.boneIndex3))
                        headWeight += bw.weight3;
                }
            }

            if (totalWeight > 0f)
            {
                profile.HeadWeightRatio = headWeight / totalWeight;
                profile.IsHeadWeighted = profile.HeadWeightRatio >= threshold;
            }

            return profile;
        }

        /// <summary>
        /// Verifica rápidamente si un mesh es head-weighted sin retornar el perfil completo.
        /// </summary>
        public static bool IsHeadWeightedMesh(SkinnedMeshRenderer smr, Transform headBone, float threshold = HEAD_WEIGHT_THRESHOLD)
        {
            return AnalyzeMesh(smr, headBone, threshold).IsHeadWeighted;
        }

        /// <summary>
        /// Filtra una lista de meshes y retorna solo los que tienen peso concentrado
        /// en huesos de la cabeza.
        /// </summary>
        /// <param name="meshes">Lista de meshes a analizar.</param>
        /// <param name="headBone">Transform del hueso Head del avatar.</param>
        /// <param name="threshold">Umbral para considerar el mesh como head-weighted.</param>
        /// <returns>Lista de meshes que son head-weighted.</returns>
        public static List<SkinnedMeshRenderer> GetHeadWeightedMeshes(
            IList<SkinnedMeshRenderer> meshes, Transform headBone, float threshold = HEAD_WEIGHT_THRESHOLD)
        {
            var result = new List<SkinnedMeshRenderer>();

            if (meshes == null || headBone == null)
                return result;

            foreach (var smr in meshes)
            {
                if (smr != null && IsHeadWeightedMesh(smr, headBone, threshold))
                    result.Add(smr);
            }

            return result;
        }

        /// <summary>
        /// Separa una lista de meshes en dos grupos: head-weighted y body-weighted.
        /// </summary>
        /// <param name="meshes">Lista de meshes a separar.</param>
        /// <param name="headBone">Transform del hueso Head del avatar.</param>
        /// <param name="headMeshes">Meshes con peso concentrado en la cabeza (pelo).</param>
        /// <param name="bodyMeshes">Meshes con peso en el cuerpo (ropa).</param>
        /// <param name="threshold">Umbral para clasificación.</param>
        public static void SeparateMeshes(
            IList<SkinnedMeshRenderer> meshes,
            Transform headBone,
            out List<SkinnedMeshRenderer> headMeshes,
            out List<SkinnedMeshRenderer> bodyMeshes,
            float threshold = HEAD_WEIGHT_THRESHOLD)
        {
            headMeshes = new List<SkinnedMeshRenderer>();
            bodyMeshes = new List<SkinnedMeshRenderer>();

            if (meshes == null)
                return;

            foreach (var smr in meshes)
            {
                if (smr == null)
                    continue;

                // Si no tiene bones o mesh, va al grupo body por defecto
                if (smr.sharedMesh == null || smr.bones == null || smr.bones.Length == 0)
                {
                    bodyMeshes.Add(smr);
                    continue;
                }

                if (headBone != null && IsHeadWeightedMesh(smr, headBone, threshold))
                    headMeshes.Add(smr);
                else
                    bodyMeshes.Add(smr);
            }
        }

        /// <summary>
        /// Obtiene el Head bone del avatar usando el Animator.
        /// Retorna null si no se puede determinar.
        /// </summary>
        public static Transform GetAvatarHeadBone(GameObject avatarRoot)
        {
            if (avatarRoot == null)
                return null;

            var animator = avatarRoot.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                    return head;
            }

            return null;
        }

        /// <summary>
        /// Obtiene el Head bone buscando en los huesos de un armature de ropa.
        /// Útil cuando el mesh pertenece a un armature de ropa (no el del avatar).
        /// </summary>
        public static Transform FindHeadBoneInArmature(Transform armatureRoot)
        {
            if (armatureRoot == null)
                return null;

            var children = armatureRoot.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                string normalized = child.name.ToLowerInvariant()
                    .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                if (normalized == "head" || normalized.EndsWith("head"))
                    return child;
            }

            return null;
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Verifica si un Transform es descendiente de (o igual a) otro Transform.
        /// </summary>
        private static bool IsDescendantOfOrEqual(Transform child, Transform parent)
        {
            if (child == parent)
                return true;

            Transform current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Busca un hueso Head en el array de bones de un SkinnedMeshRenderer.
        /// Usado como fallback cuando no se proporciona headBone.
        /// </summary>
        private static Transform FindHeadBoneInBones(Transform[] bones)
        {
            if (bones == null)
                return null;

            foreach (var bone in bones)
            {
                if (bone == null)
                    continue;

                string normalized = bone.name.ToLowerInvariant()
                    .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");
                if (normalized == "head" || normalized.EndsWith("head"))
                    return bone;
            }

            return null;
        }

        #endregion
    }
}
