using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Controllers
{
    /// <summary>
    /// Retargetea SkinnedMeshRenderers para usar nuevos huesos después de una fusión.
    /// Maneja el recálculo de bind poses para preservar la deformación correcta de la malla.
    ///
    /// Proceso:
    /// 1. Identifica SMRs que necesitan retargeting
    /// 2. Reemplaza referencias de huesos antiguos por nuevos
    /// 3. Recalcula bind poses para cada hueso retargeteado
    /// 4. Actualiza bounds si es necesario
    /// </summary>
    public class MeshRetargeter
    {
        #region Public Methods

        /// <summary>
        /// Retargetea todos los SkinnedMeshRenderers de la ropa para usar huesos del avatar.
        /// </summary>
        /// <param name="clothingRoot">Raíz de la ropa</param>
        /// <param name="boneMapping">Diccionario de hueso ropa → hueso avatar</param>
        /// <returns>Número de SMRs retargeteados</returns>
        public int RetargetMeshes(GameObject clothingRoot, Dictionary<Transform, Transform> boneMapping)
        {
            if (clothingRoot == null || boneMapping == null || boneMapping.Count == 0)
            {
                Debug.LogWarning("[MeshRetargeter] Parámetros inválidos");
                return 0;
            }

            var smrs = clothingRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int retargetedCount = 0;

            foreach (var smr in smrs)
            {
                if (RetargetSingleMesh(smr, boneMapping))
                {
                    retargetedCount++;
                }
            }

            Debug.Log($"[MeshRetargeter] Retargeteados {retargetedCount}/{smrs.Length} SkinnedMeshRenderers");
            return retargetedCount;
        }

        /// <summary>
        /// Retargetea un SkinnedMeshRenderer específico.
        /// </summary>
        /// <param name="smr">SkinnedMeshRenderer a retargetear</param>
        /// <param name="boneMapping">Diccionario de hueso ropa → hueso avatar</param>
        /// <returns>true si se retargeteó correctamente</returns>
        public bool RetargetSingleMesh(SkinnedMeshRenderer smr, Dictionary<Transform, Transform> boneMapping)
        {
            if (smr == null || smr.sharedMesh == null)
                return false;

            var originalBones = smr.bones;
            if (originalBones == null || originalBones.Length == 0)
                return false;

            // Verificar si necesita retargeting
            bool needsRetargeting = originalBones.Any(b => b != null && boneMapping.ContainsKey(b));
            if (!needsRetargeting)
                return false;

            Debug.Log($"[MeshRetargeter] Retargeteando '{smr.name}' ({originalBones.Length} huesos)");

            // Clonar el mesh para no modificar el original (importante para prefabs)
            var newMesh = CloneMeshIfNeeded(smr);

            // Preparar nuevos arrays
            var newBones = new Transform[originalBones.Length];
            var newBindPoses = new Matrix4x4[originalBones.Length];
            var originalBindPoses = newMesh.bindposes;

            // Asegurar que tenemos bind poses válidas
            if (originalBindPoses == null || originalBindPoses.Length != originalBones.Length)
            {
                Debug.LogWarning($"[MeshRetargeter] Bind poses inválidas en '{smr.name}', reconstruyendo...");
                originalBindPoses = ReconstructBindPoses(originalBones);
            }

            int retargetedBones = 0;

            for (int i = 0; i < originalBones.Length; i++)
            {
                var originalBone = originalBones[i];

                if (originalBone != null && boneMapping.TryGetValue(originalBone, out Transform newBone) && newBone != null)
                {
                    // Retargetear este hueso
                    newBones[i] = newBone;
                    newBindPoses[i] = CalculateNewBindPose(originalBone, newBone, originalBindPoses[i]);
                    retargetedBones++;
                }
                else
                {
                    // Mantener hueso original (no fue mapeado)
                    newBones[i] = originalBone;
                    newBindPoses[i] = originalBindPoses[i];
                }
            }

            // Actualizar root bone si fue mapeado
            if (smr.rootBone != null && boneMapping.TryGetValue(smr.rootBone, out Transform newRootBone))
            {
                smr.rootBone = newRootBone;
            }

            // Aplicar cambios
            newMesh.bindposes = newBindPoses;
            smr.sharedMesh = newMesh;
            smr.bones = newBones;

            // Recalcular bounds
            smr.localBounds = CalculateNewBounds(smr, newBones);

            Debug.Log($"  → {retargetedBones} huesos retargeteados");
            return true;
        }

        /// <summary>
        /// Retargetea meshes usando una lista de BoneMappings.
        /// </summary>
        public int RetargetMeshes(GameObject clothingRoot, List<BoneMapping> mappings)
        {
            var boneDict = new Dictionary<Transform, Transform>();

            foreach (var mapping in mappings)
            {
                if (mapping.IsValid && mapping.ClothingBone != null && mapping.AvatarBone != null)
                {
                    boneDict[mapping.ClothingBone] = mapping.AvatarBone;
                }
            }

            return RetargetMeshes(clothingRoot, boneDict);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Clona el mesh si es un asset compartido para evitar modificar el original.
        /// </summary>
        private Mesh CloneMeshIfNeeded(SkinnedMeshRenderer smr)
        {
            var originalMesh = smr.sharedMesh;

            // Si el mesh ya es una instancia única, no clonar
            if (originalMesh.name.EndsWith("(Clone)") || originalMesh.name.EndsWith("_Retargeted"))
            {
                return originalMesh;
            }

            // Clonar el mesh
            var newMesh = Object.Instantiate(originalMesh);
            newMesh.name = originalMesh.name + "_Retargeted";

            return newMesh;
        }

        /// <summary>
        /// Calcula la nueva bind pose para un hueso retargeteado.
        ///
        /// La bind pose es la matriz que transforma vértices del espacio del mesh
        /// al espacio del hueso en su pose de referencia (T-pose).
        ///
        /// Fórmula: newBindPose = newBone.worldToLocal * oldBone.localToWorld * oldBindPose
        /// </summary>
        private Matrix4x4 CalculateNewBindPose(Transform oldBone, Transform newBone, Matrix4x4 oldBindPose)
        {
            // La nueva bind pose debe transformar los vértices como si estuvieran
            // unidos al nuevo hueso desde el principio.
            //
            // oldBindPose: mesh space → old bone space
            // oldBone.localToWorldMatrix: old bone space → world space
            // newBone.worldToLocalMatrix: world space → new bone space
            //
            // Resultado: mesh space → new bone space

            Matrix4x4 newBindPose = newBone.worldToLocalMatrix * oldBone.localToWorldMatrix * oldBindPose;

            return newBindPose;
        }

        /// <summary>
        /// Reconstruye bind poses desde las transformaciones actuales de los huesos.
        /// Usado cuando las bind poses originales están dañadas o ausentes.
        /// </summary>
        private Matrix4x4[] ReconstructBindPoses(Transform[] bones)
        {
            var bindPoses = new Matrix4x4[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    // La bind pose es la inversa de la transformación del hueso
                    bindPoses[i] = bones[i].worldToLocalMatrix;
                }
                else
                {
                    bindPoses[i] = Matrix4x4.identity;
                }
            }

            return bindPoses;
        }

        /// <summary>
        /// Calcula nuevos bounds para el mesh después del retargeting.
        /// </summary>
        private Bounds CalculateNewBounds(SkinnedMeshRenderer smr, Transform[] newBones)
        {
            var originalBounds = smr.localBounds;

            // Si hay root bone, calcular bounds relativos a él
            if (smr.rootBone != null)
            {
                // Mantener los bounds originales pero ajustar si hay cambio de escala
                float scaleFactor = GetAverageScale(newBones);
                if (scaleFactor > 0 && Mathf.Abs(scaleFactor - 1f) > 0.01f)
                {
                    return new Bounds(
                        originalBounds.center * scaleFactor,
                        originalBounds.size * scaleFactor
                    );
                }
            }

            return originalBounds;
        }

        /// <summary>
        /// Calcula el factor de escala promedio de los huesos.
        /// </summary>
        private float GetAverageScale(Transform[] bones)
        {
            float totalScale = 0f;
            int count = 0;

            foreach (var bone in bones)
            {
                if (bone != null)
                {
                    var lossyScale = bone.lossyScale;
                    totalScale += (lossyScale.x + lossyScale.y + lossyScale.z) / 3f;
                    count++;
                }
            }

            return count > 0 ? totalScale / count : 1f;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Verifica si un SMR necesita retargeting basado en los mapeos.
        /// </summary>
        public bool NeedsRetargeting(SkinnedMeshRenderer smr, Dictionary<Transform, Transform> boneMapping)
        {
            if (smr == null || smr.bones == null)
                return false;

            return smr.bones.Any(b => b != null && boneMapping.ContainsKey(b));
        }

        /// <summary>
        /// Obtiene estadísticas de retargeting para debug.
        /// </summary>
        public RetargetingStats GetRetargetingStats(GameObject root, Dictionary<Transform, Transform> boneMapping)
        {
            var stats = new RetargetingStats();
            var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            stats.TotalSMRs = smrs.Length;

            foreach (var smr in smrs)
            {
                if (smr.bones == null || smr.bones.Length == 0)
                {
                    stats.SMRsWithoutBones++;
                    continue;
                }

                bool needsRetargeting = false;
                int mappedBones = 0;

                foreach (var bone in smr.bones)
                {
                    if (bone != null && boneMapping.ContainsKey(bone))
                    {
                        needsRetargeting = true;
                        mappedBones++;
                    }
                }

                if (needsRetargeting)
                {
                    stats.SMRsNeedingRetargeting++;
                    stats.TotalBonestoRetarget += mappedBones;
                }

                stats.TotalBones += smr.bones.Length;
            }

            return stats;
        }

        #endregion
    }

    /// <summary>
    /// Estadísticas de retargeting para debug y UI.
    /// </summary>
    public struct RetargetingStats
    {
        public int TotalSMRs;
        public int SMRsNeedingRetargeting;
        public int SMRsWithoutBones;
        public int TotalBones;
        public int TotalBonestoRetarget;

        public override string ToString()
        {
            return $"SMRs: {SMRsNeedingRetargeting}/{TotalSMRs} necesitan retargeting, " +
                   $"Huesos: {TotalBonestoRetarget}/{TotalBones} a retargetear";
        }
    }
}
