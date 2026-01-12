#if MR_NDMF_AVAILABLE
using System;
using nadena.dev.ndmf;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Controllers;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.AjustarBounds.MRAjustarBoundsPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.AjustarBounds
{
    /// <summary>
    /// Plugin NDMF para MRAjustarBounds.
    /// Ejecuta el ajuste de bounds de forma no-destructiva durante el build del avatar.
    ///
    /// Fases:
    /// - Optimizing: Ejecuta el ajuste de bounds (fase final, despues de todas las transformaciones)
    /// </summary>
    public class MRAjustarBoundsPlugin : Plugin<MRAjustarBoundsPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.ajustar_bounds";
        public override string DisplayName => "MR Ajustar Bounds";

        // Color tema: Azul
        public override Color? ThemeColor => new Color(0x33 / 255f, 0x99 / 255f, 0xFF / 255f, 1);

        protected override void Configure()
        {
            // El ajuste de bounds debe ejecutarse en la fase Optimizing
            // Esto asegura que se ejecute despues de todas las transformaciones
            // pero antes de que el avatar sea subido
            InPhase(BuildPhase.Optimizing)
                .Run(MRAjustarBoundsPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRAjustarBounds] Error durante el procesamiento NDMF: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que ejecuta el ajuste de bounds durante el build.
    /// Busca todos los MRAjustarBounds en el avatar y aplica los bounds unificados.
    /// </summary>
    internal class MRAjustarBoundsPass : Pass<MRAjustarBoundsPass>
    {
        public override string DisplayName => "Ajustar Bounds (Unificar)";

        protected override void Execute(BuildContext context)
        {
            // Buscar todos los componentes MRAjustarBounds en el avatar
            var ajustarBoundsComponents = context.AvatarRootObject.GetComponentsInChildren<MRAjustarBounds>(true);

            if (ajustarBoundsComponents.Length == 0)
            {
                return; // No hay nada que procesar
            }

            Debug.Log($"[MRAjustarBounds NDMF] Procesando {ajustarBoundsComponents.Length} componente(s) MRAjustarBounds...");

            var calculator = new BoundsCalculator();
            int totalProcessed = 0;
            int totalMeshes = 0;
            int totalParticles = 0;

            foreach (var ajustarBounds in ajustarBoundsComponents)
            {
                if (!ajustarBounds.enabled)
                {
                    Debug.Log($"[MRAjustarBounds NDMF] Saltando '{ajustarBounds.gameObject.name}' (deshabilitado)");
                    continue;
                }

                try
                {
                    // Obtener el avatar root (usar el del componente o el del contexto NDMF)
                    var avatarRoot = ajustarBounds.AvatarRoot ?? context.AvatarRootObject;

                    // Escanear meshes si no estan escaneados
                    if (ajustarBounds.DetectedMeshCount == 0)
                    {
                        Debug.Log($"[MRAjustarBounds NDMF] Escaneando meshes para '{ajustarBounds.gameObject.name}'");
                        var meshInfos = calculator.ScanAvatar(avatarRoot);

                        if (meshInfos.Count == 0)
                        {
                            Debug.LogWarning($"[MRAjustarBounds NDMF] No se encontraron meshes en '{avatarRoot.name}'");
                            continue;
                        }

                        // Calcular bounds unificados
                        var result = calculator.CalculateUnifiedBounds(
                            meshInfos,
                            avatarRoot.transform,
                            ajustarBounds.MarginPercentage
                        );

                        if (result.Success)
                        {
                            // Aplicar bounds
                            int applied = calculator.ApplyUnifiedBounds(meshInfos, result.UnifiedBoundsWithMargin);
                            totalMeshes += applied;
                            totalProcessed++;

                            Debug.Log($"[MRAjustarBounds NDMF] Bounds aplicados: {result.GetSummary()}");
                        }
                        else
                        {
                            Debug.LogWarning($"[MRAjustarBounds NDMF] Calculo fallido: {result.GetSummary()}");
                        }
                    }
                    else
                    {
                        // Usar datos pre-calculados si existen
                        if (ajustarBounds.HasValidCalculation)
                        {
                            // Aplicar bounds pre-calculados
                            calculator.ApplyUnifiedBounds(
                                ajustarBounds.DetectedMeshes,
                                ajustarBounds.LastCalculationResult.UnifiedBoundsWithMargin
                            );
                            totalMeshes += ajustarBounds.ValidMeshCount;
                            totalProcessed++;

                            Debug.Log($"[MRAjustarBounds NDMF] Usando bounds pre-calculados: {ajustarBounds.LastCalculationResult.GetSummary()}");
                        }
                        else
                        {
                            // Recalcular
                            var result = calculator.CalculateUnifiedBounds(
                                ajustarBounds.DetectedMeshes,
                                avatarRoot.transform,
                                ajustarBounds.MarginPercentage
                            );

                            if (result.Success)
                            {
                                calculator.ApplyUnifiedBounds(ajustarBounds.DetectedMeshes, result.UnifiedBoundsWithMargin);
                                totalMeshes += result.ValidMeshCount;
                                totalProcessed++;

                                Debug.Log($"[MRAjustarBounds NDMF] Bounds recalculados y aplicados: {result.GetSummary()}");
                            }
                        }
                    }

                    // Procesar particulas si esta habilitado
                    if (ajustarBounds.IncludeParticles)
                    {
                        int particlesProcessed = ProcessParticles(ajustarBounds, avatarRoot, calculator);
                        totalParticles += particlesProcessed;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MRAjustarBounds NDMF] Error procesando '{ajustarBounds.gameObject.name}': {e.Message}");
                    Debug.LogException(e);
                }

                // Destruir el componente MRAjustarBounds despues de procesar (ya no es necesario en runtime)
                UnityEngine.Object.DestroyImmediate(ajustarBounds);
            }

            string particleInfo = totalParticles > 0 ? $", {totalParticles} particula(s)" : "";
            Debug.Log($"[MRAjustarBounds NDMF] Procesamiento completado: {totalProcessed} componente(s), {totalMeshes} mesh(es){particleInfo} actualizados");
        }

        /// <summary>
        /// Procesa los sistemas de particulas de un componente MRAjustarBounds
        /// </summary>
        private int ProcessParticles(MRAjustarBounds ajustarBounds, GameObject avatarRoot, BoundsCalculator calculator)
        {
            try
            {
                // Escanear particulas si no estan escaneadas
                if (ajustarBounds.DetectedParticleCount == 0)
                {
                    Debug.Log($"[MRAjustarBounds NDMF] Escaneando particulas para '{ajustarBounds.gameObject.name}'");
                    var particleInfos = calculator.ScanParticles(avatarRoot);

                    if (particleInfos.Count == 0)
                    {
                        Debug.Log($"[MRAjustarBounds NDMF] No se encontraron particulas en '{avatarRoot.name}'");
                        return 0;
                    }

                    // Calcular bounds individuales para particulas
                    calculator.CalculateIndividualParticleBounds(
                        particleInfos,
                        avatarRoot.transform,
                        ajustarBounds.ParticleMarginPercentage
                    );

                    // Aplicar bounds
                    int applied = calculator.ApplyParticleBounds(particleInfos);
                    Debug.Log($"[MRAjustarBounds NDMF] Bounds aplicados a {applied} particulas");
                    return applied;
                }
                else
                {
                    // Usar datos pre-calculados
                    // Recalcular bounds si es necesario
                    calculator.CalculateIndividualParticleBounds(
                        ajustarBounds.DetectedParticles,
                        avatarRoot.transform,
                        ajustarBounds.ParticleMarginPercentage
                    );

                    int applied = calculator.ApplyParticleBounds(ajustarBounds.DetectedParticles);
                    Debug.Log($"[MRAjustarBounds NDMF] Bounds pre-calculados aplicados a {applied} particulas");
                    return applied;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MRAjustarBounds NDMF] Error procesando particulas: {e.Message}");
                Debug.LogException(e);
                return 0;
            }
        }
    }
}
#endif
