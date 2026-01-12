using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Controllers
{
    /// <summary>
    /// Calculador de bounds unificados para avatares.
    /// Escanea todos los SkinnedMeshRenderer y calcula un bounding box que englobe todo.
    /// </summary>
    public class BoundsCalculator
    {
        /// <summary>
        /// Margen por defecto (10%)
        /// </summary>
        public const float DEFAULT_MARGIN_PERCENTAGE = MRBoundsConstants.DEFAULT_MARGIN_PERCENTAGE;

        /// <summary>
        /// Escanea un avatar y recopila informacion de todos los SkinnedMeshRenderer
        /// </summary>
        /// <param name="avatarRoot">GameObject raiz del avatar</param>
        /// <returns>Lista de informacion de bounds por mesh</returns>
        public List<MeshBoundsInfo> ScanAvatar(GameObject avatarRoot)
        {
            var result = new List<MeshBoundsInfo>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[BoundsCalculator] Avatar root es null");
                return result;
            }

            // Buscar todos los SkinnedMeshRenderer (incluidos inactivos)
            var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                {
                    Debug.LogWarning($"[BoundsCalculator] '{renderer.name}' no tiene mesh asignado, saltando");
                    continue;
                }

                var info = new MeshBoundsInfo(renderer);

                // Calcular bounds que cubren exactamente el mesh
                info.CalculatedBounds = CalculateMeshBounds(renderer);

                result.Add(info);
            }

            Debug.Log($"[BoundsCalculator] Escaneados {result.Count} SkinnedMeshRenderer en '{avatarRoot.name}'");
            return result;
        }

        /// <summary>
        /// Calcula los bounds exactos de un SkinnedMeshRenderer basandose en su mesh
        /// </summary>
        private Bounds CalculateMeshBounds(SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMesh == null)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            // Usar los bounds del mesh compartido como base
            // Estos bounds son en espacio local del mesh
            return renderer.sharedMesh.bounds;
        }

        /// <summary>
        /// Calcula el bounding box unificado que engloba todos los meshes del avatar
        /// </summary>
        /// <param name="meshInfos">Lista de informacion de meshes</param>
        /// <param name="avatarRoot">Transform raiz del avatar para conversiones de espacio</param>
        /// <param name="marginPercentage">Porcentaje de margen adicional (0.10 = 10%)</param>
        /// <returns>Resultado del calculo con bounds unificados</returns>
        public BoundsCalculationResult CalculateUnifiedBounds(
            List<MeshBoundsInfo> meshInfos,
            Transform avatarRoot,
            float marginPercentage = DEFAULT_MARGIN_PERCENTAGE)
        {
            if (meshInfos == null || meshInfos.Count == 0)
            {
                return BoundsCalculationResult.CreateFailure("No hay meshes para calcular");
            }

            if (avatarRoot == null)
            {
                return BoundsCalculationResult.CreateFailure("Avatar root es null");
            }

            // Inicializar bounds con el primer mesh valido
            Bounds? globalBounds = null;
            int validCount = 0;

            foreach (var info in meshInfos)
            {
                if (!info.IsValid || info.Renderer == null)
                {
                    continue;
                }

                // Convertir bounds locales a world space y luego a espacio del avatar
                Bounds worldBounds = TransformBoundsToAvatarSpace(info.Renderer, avatarRoot);

                if (!globalBounds.HasValue)
                {
                    globalBounds = worldBounds;
                }
                else
                {
                    // Expandir para incluir este mesh
                    globalBounds.Value.Encapsulate(worldBounds);
                }

                validCount++;
            }

            if (!globalBounds.HasValue)
            {
                return BoundsCalculationResult.CreateFailure("No se encontraron meshes validos");
            }

            Bounds unified = globalBounds.Value;

            // Calcular centro geometrico real (Opcion B)
            // El centro Y es el punto medio entre el minimo y maximo reales
            Vector3 center = unified.center;
            Vector3 size = unified.size;

            // Aplicar margen de seguridad
            Vector3 sizeWithMargin = size * (1f + marginPercentage);

            // El centro no cambia, solo el tamanio
            Bounds unifiedWithMargin = new Bounds(center, sizeWithMargin);

            var result = BoundsCalculationResult.CreateSuccess(
                unified,
                unifiedWithMargin,
                meshInfos.Count,
                validCount,
                marginPercentage
            );

            Debug.Log($"[BoundsCalculator] Bounds calculados: Centro={center}, Tamanio={sizeWithMargin}, " +
                      $"MinY={unifiedWithMargin.min.y:F2}, MaxY={unifiedWithMargin.max.y:F2}");

            return result;
        }

        /// <summary>
        /// Transforma los bounds de un renderer al espacio local del avatar
        /// </summary>
        private Bounds TransformBoundsToAvatarSpace(SkinnedMeshRenderer renderer, Transform avatarRoot)
        {
            // Obtener bounds locales del renderer
            Bounds localBounds = renderer.localBounds;

            // Obtener los 8 vertices del bounding box
            Vector3[] corners = GetBoundsCorners(localBounds);

            // Transformar cada esquina a world space y luego a espacio del avatar
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
            Matrix4x4 worldToAvatar = avatarRoot.worldToLocalMatrix;
            Matrix4x4 localToAvatar = worldToAvatar * localToWorld;

            foreach (var corner in corners)
            {
                Vector3 avatarSpacePoint = localToAvatar.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, avatarSpacePoint);
                max = Vector3.Max(max, avatarSpacePoint);
            }

            // Crear bounds a partir de min/max
            Bounds avatarSpaceBounds = new Bounds();
            avatarSpaceBounds.SetMinMax(min, max);

            return avatarSpaceBounds;
        }

        /// <summary>
        /// Obtiene las 8 esquinas de un bounding box
        /// </summary>
        private Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            return new Vector3[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        /// <summary>
        /// Aplica los bounds unificados a todos los meshes
        /// </summary>
        /// <param name="meshInfos">Lista de informacion de meshes</param>
        /// <param name="unifiedBounds">Bounds unificados a aplicar</param>
        /// <returns>Numero de meshes actualizados</returns>
        public int ApplyUnifiedBounds(List<MeshBoundsInfo> meshInfos, Bounds unifiedBounds)
        {
            int appliedCount = 0;

            foreach (var info in meshInfos)
            {
                if (info.IsValid && info.Renderer != null)
                {
                    info.ApplyUnifiedBounds(unifiedBounds);
                    appliedCount++;
                }
            }

            Debug.Log($"[BoundsCalculator] Bounds aplicados a {appliedCount} meshes");
            return appliedCount;
        }

        /// <summary>
        /// Restaura los bounds originales de todos los meshes
        /// </summary>
        /// <param name="meshInfos">Lista de informacion de meshes</param>
        /// <returns>Numero de meshes restaurados</returns>
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
        /// <param name="avatarRoot">GameObject raiz del avatar</param>
        /// <returns>Lista de informacion de bounds por particula</returns>
        public List<ParticleBoundsInfo> ScanParticles(GameObject avatarRoot)
        {
            var result = new List<ParticleBoundsInfo>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[BoundsCalculator] Avatar root es null");
                return result;
            }

            // Buscar todos los ParticleSystem (incluidos inactivos)
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
        /// Cada particula obtiene su propio bound basado en sus propiedades.
        /// </summary>
        /// <param name="particleInfos">Lista de informacion de particulas</param>
        /// <param name="avatarRoot">Transform raiz del avatar</param>
        /// <param name="marginPercentage">Porcentaje de margen adicional</param>
        /// <returns>Numero de particulas procesadas</returns>
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

                // Calcular bounds basados en las propiedades del ParticleSystem
                Bounds calculatedBounds = CalculateParticleBounds(info.ParticleSystem, marginPercentage);
                info.CalculatedBounds = calculatedBounds;

                processedCount++;
            }

            Debug.Log($"[BoundsCalculator] Bounds calculados para {processedCount} sistemas de particulas");
            return processedCount;
        }

        /// <summary>
        /// Calcula los bounds para un sistema de particulas individual
        /// basandose en sus propiedades de emision y movimiento.
        /// </summary>
        private Bounds CalculateParticleBounds(ParticleSystem ps, float marginPercentage)
        {
            var main = ps.main;
            var shape = ps.shape;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();

            // Obtener bounds actuales como base
            Bounds baseBounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, Vector3.one);

            // Calcular el alcance maximo de las particulas
            float maxLifetime = main.startLifetime.constantMax;
            float maxSpeed = main.startSpeed.constantMax;
            float maxDistance = maxLifetime * maxSpeed;

            // Considerar el tamanio de las particulas
            float maxSize = main.startSize.constantMax;

            // Expandir bounds basandose en el alcance
            Vector3 expansion = Vector3.one * (maxDistance + maxSize);

            // Considerar la forma del emisor
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

            // Crear bounds expandidos
            Vector3 center = baseBounds.center;
            Vector3 size = baseBounds.size + expansion * 2f;

            // Aplicar margen adicional
            size *= (1f + marginPercentage);

            return new Bounds(center, size);
        }

        /// <summary>
        /// Aplica los bounds calculados a todos los sistemas de particulas
        /// </summary>
        /// <param name="particleInfos">Lista de informacion de particulas</param>
        /// <returns>Numero de particulas actualizadas</returns>
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
        /// <param name="particleInfos">Lista de informacion de particulas</param>
        /// <returns>Numero de particulas restauradas</returns>
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
