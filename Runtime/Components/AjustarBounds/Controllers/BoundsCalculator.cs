using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Controllers
{
    /// <summary>
    /// Calculador de bounds unificados para avatares.
    /// Usa BakeMesh para obtener la geometria real deformada de cada mesh,
    /// calcula un AABB en el espacio del rootBone compartido (Hips),
    /// y aplica ese mismo volumen a todos los renderers.
    ///
    /// Basado en el enfoque de WhiteFlare AvatarTools:
    /// - BakeMesh para geometria precisa
    /// - rootBone compartido (Hips) para todos los renderers
    /// - Reset de transform del renderer a identity
    /// - Sin clamp artificial a limites VRChat
    /// </summary>
    public class BoundsCalculator
    {
        /// <summary>
        /// Margen por defecto (10%)
        /// </summary>
        public const float DEFAULT_MARGIN_PERCENTAGE = MRBoundsConstants.DEFAULT_MARGIN_PERCENTAGE;

        /// <summary>
        /// Detecta el rootBone compartido (Hips) del avatar.
        /// Usa el Animator humanoid si existe, sino busca por nombre.
        /// </summary>
        public Transform DetectSharedRootBone(GameObject avatarRoot)
        {
            if (avatarRoot == null) return null;

            // Buscar Animator humanoid
            foreach (var anim in avatarRoot.GetComponentsInChildren<Animator>(true))
            {
                if (anim != null && anim.isHuman)
                {
                    var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips != null) return hips;
                }
            }

            // Fallback: buscar por nombre
            foreach (var child in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                var name = child.name.ToLowerInvariant();
                if (name == "hips" || name == "hip" || name == "pelvis")
                    return child;
            }

            // Ultimo fallback: usar el root del avatar
            Debug.LogWarning("[BoundsCalculator] No se encontro hueso Hips, usando avatar root como rootBone");
            return avatarRoot.transform;
        }

        /// <summary>
        /// Escanea un avatar y recopila informacion de todos los SkinnedMeshRenderer
        /// </summary>
        public List<MeshBoundsInfo> ScanAvatar(GameObject avatarRoot)
        {
            var result = new List<MeshBoundsInfo>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[BoundsCalculator] Avatar root es null");
                return result;
            }

            var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                {
                    Debug.LogWarning($"[BoundsCalculator] '{renderer.name}' no tiene mesh asignado, saltando");
                    continue;
                }

                var info = new MeshBoundsInfo(renderer);
                result.Add(info);
            }

            Debug.Log($"[BoundsCalculator] Escaneados {result.Count} SkinnedMeshRenderer en '{avatarRoot.name}'");
            return result;
        }

        /// <summary>
        /// Calcula el bounding box unificado usando BakeMesh para obtener
        /// la geometria real deformada. Todos los vertices se transforman
        /// al espacio local del rootBone compartido (Hips).
        ///
        /// El resultado es un AABB en rootBone-space que engloba todos los meshes.
        /// </summary>
        public BoundsCalculationResult CalculateUnifiedBounds(
            List<MeshBoundsInfo> meshInfos,
            Transform sharedRootBone,
            float marginPercentage = DEFAULT_MARGIN_PERCENTAGE)
        {
            if (meshInfos == null || meshInfos.Count == 0)
            {
                return BoundsCalculationResult.CreateFailure("No hay meshes para calcular");
            }

            if (sharedRootBone == null)
            {
                return BoundsCalculationResult.CreateFailure("rootBone compartido es null");
            }

            Matrix4x4 worldToRootBone = sharedRootBone.worldToLocalMatrix;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            int validCount = 0;
            int totalVertices = 0;

            foreach (var info in meshInfos)
            {
                if (!info.IsValid || info.Renderer == null)
                    continue;

                // Saltar renderers sin bones (meshes estaticos)
                if (!info.HasBones)
                    continue;

                // BakeMesh: hornea el mesh deformado segun los huesos actuales
                // El resultado esta en el espacio local del renderer
                var bakedMesh = new Mesh();
                info.Renderer.BakeMesh(bakedMesh);

                // Matriz combinada: renderer-local → world → rootBone-local
                Matrix4x4 toRootBone = worldToRootBone * info.Renderer.transform.localToWorldMatrix;

                var vertices = bakedMesh.vertices;
                foreach (var v in vertices)
                {
                    Vector3 rootBoneLocal = toRootBone.MultiplyPoint3x4(v);
                    min = Vector3.Min(min, rootBoneLocal);
                    max = Vector3.Max(max, rootBoneLocal);
                }

                totalVertices += vertices.Length;
                Object.DestroyImmediate(bakedMesh);
                validCount++;
            }

            if (totalVertices == 0)
            {
                return BoundsCalculationResult.CreateFailure("No se encontraron vertices validos");
            }

            // Crear AABB crudo
            Bounds rawBounds = new Bounds();
            rawBounds.SetMinMax(min, max);

            Vector3 center = rawBounds.center;
            Vector3 extents = rawBounds.extents;

            // Redondear centro a grid de 0.01m (centimetros)
            Vector3 roundedCenter = new Vector3(
                Mathf.Round(center.x * 100f) / 100f,
                Mathf.Round(center.y * 100f) / 100f,
                Mathf.Round(center.z * 100f) / 100f
            );

            // Expandir extents para compensar el error de redondeo
            extents.x += Mathf.Abs(roundedCenter.x - center.x);
            extents.y += Mathf.Abs(roundedCenter.y - center.y);
            extents.z += Mathf.Abs(roundedCenter.z - center.z);

            // Redondear extents hacia arriba a 0.01m
            extents.x = Mathf.Ceil(extents.x * 100f) / 100f;
            extents.y = Mathf.Ceil(extents.y * 100f) / 100f;
            extents.z = Mathf.Ceil(extents.z * 100f) / 100f;

            // Bounds exactos (sin margen extra)
            Bounds unified = new Bounds(roundedCenter, extents * 2f);

            // Aplicar margen de seguridad
            Vector3 extentsWithMargin = extents * (1f + marginPercentage);

            // Redondear hacia arriba despues de aplicar margen
            extentsWithMargin.x = Mathf.Ceil(extentsWithMargin.x * 100f) / 100f;
            extentsWithMargin.y = Mathf.Ceil(extentsWithMargin.y * 100f) / 100f;
            extentsWithMargin.z = Mathf.Ceil(extentsWithMargin.z * 100f) / 100f;

            Bounds unifiedWithMargin = new Bounds(roundedCenter, extentsWithMargin * 2f);

            // Advertencia VRChat
            Vector3 finalSize = unifiedWithMargin.size;
            if (finalSize.x > MRBoundsConstants.WARNING_BOUNDS_SIZE_XZ ||
                finalSize.y > MRBoundsConstants.WARNING_BOUNDS_SIZE_Y ||
                finalSize.z > MRBoundsConstants.WARNING_BOUNDS_SIZE_XZ)
            {
                Debug.LogWarning($"[BoundsCalculator] Bounds superan rank Good de VRChat: " +
                    $"{finalSize.x:F2}x{finalSize.y:F2}x{finalSize.z:F2}m " +
                    $"(Good max: {MRBoundsConstants.WARNING_BOUNDS_SIZE_XZ}x{MRBoundsConstants.WARNING_BOUNDS_SIZE_Y}x{MRBoundsConstants.WARNING_BOUNDS_SIZE_XZ}m)");
            }

            // Clamp a limites VRChat Very Poor menos 1cm
            float maxXZ = MRBoundsConstants.MAX_BOUNDS_SIZE_XZ - 0.01f;
            float maxY = MRBoundsConstants.MAX_BOUNDS_SIZE_Y - 0.01f;
            bool wasClamped = false;

            if (finalSize.x > MRBoundsConstants.MAX_BOUNDS_SIZE_XZ ||
                finalSize.y > MRBoundsConstants.MAX_BOUNDS_SIZE_Y ||
                finalSize.z > MRBoundsConstants.MAX_BOUNDS_SIZE_XZ)
            {
                Debug.LogWarning($"[BoundsCalculator] Bounds exceden Very Poor ({finalSize.x:F2}x{finalSize.y:F2}x{finalSize.z:F2}m), " +
                    $"limitando a {maxXZ:F2}x{maxY:F2}x{maxXZ:F2}m");

                Vector3 clampedSize = new Vector3(
                    Mathf.Min(finalSize.x, maxXZ),
                    Mathf.Min(finalSize.y, maxY),
                    Mathf.Min(finalSize.z, maxXZ)
                );
                unifiedWithMargin = new Bounds(roundedCenter, clampedSize);
                finalSize = clampedSize;
                wasClamped = true;
            }

            var result = BoundsCalculationResult.CreateSuccess(
                unified,
                unifiedWithMargin,
                meshInfos.Count,
                validCount,
                marginPercentage
            );
            result.WasClamped = wasClamped;

            Debug.Log($"[BoundsCalculator] Bounds calculados ({totalVertices} vertices, {validCount} meshes): " +
                      $"Centro={roundedCenter}, Tamanio={finalSize}" +
                      (wasClamped ? " [LIMITADO a max VRChat]" : ""));

            return result;
        }

        /// <summary>
        /// Aplica los bounds unificados a todos los meshes.
        /// Asigna el rootBone compartido, resetea el transform, y establece localBounds.
        /// Despues de esto, TODOS los renderers comparten el mismo volumen de culling.
        /// </summary>
        public int ApplyUnifiedBounds(List<MeshBoundsInfo> meshInfos, Bounds unifiedBounds, Transform sharedRootBone)
        {
            int appliedCount = 0;

            foreach (var info in meshInfos)
            {
                if (!info.IsValid || info.Renderer == null)
                    continue;

                // Meshes sin bones: no unificar, pero limitar si exceden VRChat
                if (!info.HasBones)
                {
                    if (info.ClampToVRChatLimits())
                        appliedCount++;
                    continue;
                }

                info.ApplyUnifiedBounds(unifiedBounds, sharedRootBone);
                appliedCount++;
            }

            Debug.Log($"[BoundsCalculator] Bounds unificados aplicados a {appliedCount} meshes " +
                      $"(rootBone: {sharedRootBone.name})");
            return appliedCount;
        }

        /// <summary>
        /// Restaura los bounds originales de todos los meshes
        /// </summary>
        public int RestoreOriginalBounds(List<MeshBoundsInfo> meshInfos)
        {
            int restoredCount = 0;

            foreach (var info in meshInfos)
            {
                if (info.IsValid && info.Renderer != null)
                {
                    info.RestoreOriginalBounds();
                    restoredCount++;
                }
            }

            Debug.Log($"[BoundsCalculator] Bounds originales restaurados en {restoredCount} meshes");
            return restoredCount;
        }

        /// <summary>
        /// Valida que todos los meshes tengan referencias validas
        /// </summary>
        public int ValidateMeshInfos(List<MeshBoundsInfo> meshInfos)
        {
            int invalidCount = 0;

            foreach (var info in meshInfos)
            {
                info.Refresh();
                if (!info.IsValid)
                {
                    invalidCount++;
                }
            }

            return invalidCount;
        }

        #region Particle System Methods

        /// <summary>
        /// Escanea un avatar y recopila informacion de todos los ParticleSystem
        /// </summary>
        public List<ParticleBoundsInfo> ScanParticles(GameObject avatarRoot)
        {
            var result = new List<ParticleBoundsInfo>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[BoundsCalculator] Avatar root es null");
                return result;
            }

            var particleSystems = avatarRoot.GetComponentsInChildren<ParticleSystem>(true);

            foreach (var ps in particleSystems)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"[BoundsCalculator] '{ps.name}' no tiene ParticleSystemRenderer, saltando");
                    continue;
                }

                var info = new ParticleBoundsInfo(ps);
                result.Add(info);
            }

            Debug.Log($"[BoundsCalculator] Escaneados {result.Count} ParticleSystem en '{avatarRoot.name}'");
            return result;
        }

        /// <summary>
        /// Calcula bounds individuales para cada sistema de particulas.
        /// </summary>
        public int CalculateIndividualParticleBounds(
            List<ParticleBoundsInfo> particleInfos,
            Transform avatarRoot,
            float marginPercentage = DEFAULT_MARGIN_PERCENTAGE)
        {
            if (particleInfos == null || particleInfos.Count == 0)
            {
                return 0;
            }

            int processedCount = 0;

            foreach (var info in particleInfos)
            {
                if (!info.IsValid || info.ParticleSystem == null)
                {
                    continue;
                }

                Bounds calculatedBounds = CalculateParticleBounds(info.ParticleSystem, marginPercentage);
                info.CalculatedBounds = calculatedBounds;

                processedCount++;
            }

            Debug.Log($"[BoundsCalculator] Bounds calculados para {processedCount} sistemas de particulas");
            return processedCount;
        }

        /// <summary>
        /// Calcula los bounds para un sistema de particulas individual
        /// </summary>
        private Bounds CalculateParticleBounds(ParticleSystem ps, float marginPercentage)
        {
            var main = ps.main;
            var shape = ps.shape;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();

            Bounds baseBounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, Vector3.one);

            float maxLifetime = main.startLifetime.constantMax;
            float maxSpeed = main.startSpeed.constantMax;
            float maxDistance = maxLifetime * maxSpeed;
            float maxSize = main.startSize.constantMax;

            Vector3 expansion = Vector3.one * (maxDistance + maxSize);

            if (shape.enabled)
            {
                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.Hemisphere:
                        expansion += Vector3.one * shape.radius;
                        break;
                    case ParticleSystemShapeType.Cone:
                        expansion += Vector3.one * (shape.radius + shape.length);
                        break;
                    case ParticleSystemShapeType.Box:
                        expansion += shape.scale;
                        break;
                    case ParticleSystemShapeType.Circle:
                        expansion += new Vector3(shape.radius, 0, shape.radius);
                        break;
                }
            }

            Vector3 center = baseBounds.center;
            Vector3 size = baseBounds.size + expansion * 2f;
            size *= (1f + marginPercentage);

            return new Bounds(center, size);
        }

        /// <summary>
        /// Aplica los bounds calculados a todos los sistemas de particulas
        /// </summary>
        public int ApplyParticleBounds(List<ParticleBoundsInfo> particleInfos)
        {
            int appliedCount = 0;

            foreach (var info in particleInfos)
            {
                if (info.IsValid && info.Renderer != null)
                {
                    info.ApplyCalculatedBounds();
                    appliedCount++;
                }
            }

            Debug.Log($"[BoundsCalculator] Bounds aplicados a {appliedCount} sistemas de particulas");
            return appliedCount;
        }

        /// <summary>
        /// Restaura los bounds originales de todos los sistemas de particulas
        /// </summary>
        public int RestoreParticleBounds(List<ParticleBoundsInfo> particleInfos)
        {
            int restoredCount = 0;

            foreach (var info in particleInfos)
            {
                if (info.IsValid && info.Renderer != null)
                {
                    info.RestoreOriginalBounds();
                    restoredCount++;
                }
            }

            Debug.Log($"[BoundsCalculator] Bounds originales restaurados en {restoredCount} sistemas de particulas");
            return restoredCount;
        }

        /// <summary>
        /// Valida que todas las particulas tengan referencias validas
        /// </summary>
        public int ValidateParticleInfos(List<ParticleBoundsInfo> particleInfos)
        {
            int invalidCount = 0;

            foreach (var info in particleInfos)
            {
                info.Refresh();
                if (!info.IsValid)
                {
                    invalidCount++;
                }
            }

            return invalidCount;
        }

        #endregion
    }
}
