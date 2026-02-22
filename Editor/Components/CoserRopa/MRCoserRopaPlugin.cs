#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using VRC.SDK3.Avatars.Components;
using Bender_Dios.MenuRadial.Components.MenuRadial;

using MADetector = Bender_Dios.MenuRadial.Components.CoserRopa.Controllers.ModularAvatarDetector;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.CoserRopa.MRCoserRopaPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.CoserRopa
{
    /// <summary>
    /// Plugin NDMF para MRCoserRopa.
    /// Ejecuta el cosido de ropa de forma no-destructiva durante el build del avatar.
    ///
    /// Fases:
    /// - Transforming: Ejecuta el merge de armatures similar a Modular Avatar
    /// </summary>
    public class MRCoserRopaPlugin : Plugin<MRCoserRopaPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.coser_ropa";
        public override string DisplayName => "MR Coser Ropa";

        // Color tema: Verde azulado
        public override Color? ThemeColor => new Color(0x00 / 255f, 0x96 / 255f, 0x88 / 255f, 1);

        protected override void Configure()
        {
            // El merge de armature debe ejecutarse en la fase Transforming
            // Similar a MergeArmaturePluginPass de Modular Avatar
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar") // Ejecutar antes de MA si está presente
                .Run(MRCoserRopaPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRCoserRopa] Error durante el procesamiento NDMF: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que ejecuta el cosido de ropa durante el build.
    /// Busca todos los MRCoserRopa en el avatar y ejecuta el merge.
    /// </summary>
    internal class MRCoserRopaPass : Pass<MRCoserRopaPass>
    {
        public override string DisplayName => "Coser Ropa (Merge Armature)";

        protected override void Execute(BuildContext context)
        {
            // Verificar si el cosido está desactivado desde MRMenuRadial
            var menuRadials = context.AvatarRootObject.GetComponentsInChildren<MRMenuRadial>(true);
            if (menuRadials.Length == 0)
            {
                // Buscar MRMenuRadial externo que referencie este avatar
                string avatarName = context.AvatarRootObject.name;
                if (avatarName.EndsWith("(Clone)"))
                {
                    avatarName = avatarName.Substring(0, avatarName.Length - 7).Trim();
                }

                var allMenuRadials = UnityEngine.Object.FindObjectsByType<MRMenuRadial>(FindObjectsSortMode.None);
                var matches = allMenuRadials
                    .Where(mr => mr != null
                        && mr.AvatarRoot != null
                        && mr.AvatarRoot != context.AvatarRootObject
                        && mr.AvatarRoot.GetComponent<VRCAvatarDescriptor>() != null
                        && mr.AvatarRoot.name == avatarName)
                    .ToArray();

                if (matches.Length > 1)
                {
                    Debug.LogWarning($"[MRCoserRopa NDMF] Se encontraron {matches.Length} MRMenuRadial externos para '{avatarName}'. " +
                        "Esto puede causar conflictos si apuntan a avatares diferentes con el mismo nombre.");
                }

                menuRadials = matches;
            }

            // Si algún MRMenuRadial tiene el cosido desactivado, saltar el proceso
            foreach (var menuRadial in menuRadials)
            {
                if (menuRadial != null && menuRadial.DisableBoneStitchingNDMF)
                {
                    Debug.Log("[MRCoserRopa NDMF] Cosido de huesos DESACTIVADO desde MRMenuRadial. Saltando proceso.");
                    return;
                }
            }

            // Buscar todos los componentes MRCoserRopa en el avatar
            var coserRopaComponents = context.AvatarRootObject.GetComponentsInChildren<MRCoserRopa>(true);

            if (coserRopaComponents.Length == 0)
            {
                return; // No hay nada que procesar
            }

            Debug.Log($"[MRCoserRopa NDMF] Procesando {coserRopaComponents.Length} componente(s) MRCoserRopa...");

            var boneMapper = new HumanoidBoneMapper();
            var stitchingController = new BoneStitchingController();
            int totalProcessed = 0;
            int totalFailed = 0;

            foreach (var coserRopa in coserRopaComponents)
            {
                if (!coserRopa.enabled)
                {
                    Debug.Log($"[MRCoserRopa NDMF] Saltando '{coserRopa.gameObject.name}' (deshabilitado)");
                    continue;
                }

                // Verificar que tenemos datos válidos
                var detectedPieces = coserRopa.DetectedPieces;
                if (detectedPieces == null || detectedPieces.Count == 0)
                {
                    Debug.LogWarning($"[MRCoserRopa NDMF] '{coserRopa.gameObject.name}' no tiene prendas de ropa configuradas");
                    continue;
                }

                // El avatar root es el objeto raíz del contexto NDMF (o el configurado en el componente)
                var avatarRoot = coserRopa.AvatarRoot ?? context.AvatarRootObject;
                var avatarRef = new ArmatureReference(avatarRoot);

                foreach (var pieceEntry in detectedPieces)
                {
                    // Solo procesar ropas habilitadas
                    if (pieceEntry == null || !pieceEntry.Enabled || pieceEntry.GameObject == null)
                    {
                        continue;
                    }

                    // Verificar si tiene Modular Avatar configurado
                    if (pieceEntry.HasModularAvatar && !pieceEntry.DisableMA)
                    {
                        Debug.Log($"[MRCoserRopa NDMF] Saltando '{pieceEntry.Name}': tiene Modular Avatar ({pieceEntry.ModularAvatarComponentType}). MA lo procesará.");
                        continue;
                    }

                    // Si DisableMA, destruir componentes MA en el clon para que MR pueda coser
                    if (pieceEntry.DisableMA && pieceEntry.HasModularAvatar)
                    {
                        int destroyed = MADetector.Instance.DestroyArmatureComponents(pieceEntry.GameObject);
                        Debug.Log($"[MRCoserRopa NDMF] DisableMA: eliminados {destroyed} componentes MA en '{pieceEntry.Name}'. MR procesará.");

                        if (pieceEntry.PieceType == PieceType.Pelo)
                        {
                            Debug.LogWarning($"[MRCoserRopa NDMF] ADVERTENCIA: '{pieceEntry.Name}' es tipo Pelo. Desactivar MA puede causar pérdida de la peluca.");
                        }
                    }

                    // Re-verificar MA en runtime por si se agregó después de la detección
                    if (!pieceEntry.DisableMA)
                    {
                        var maResult = MADetector.Instance.DetectModularAvatar(pieceEntry.GameObject);
                        if (maResult.HasMergeArmature)
                        {
                            Debug.Log($"[MRCoserRopa NDMF] Saltando '{pieceEntry.Name}': ModularAvatarMergeArmature detectado en runtime. MA lo procesará.");
                            continue;
                        }
                    }

                    Debug.Log($"[MRCoserRopa NDMF] Procesando ropa: '{pieceEntry.Name}'");

                    try
                    {
                        // Detectar mapeos de huesos
                        List<BoneMapping> mappings;

                        // Usar mapeos personalizados si existen, sino detectar automáticamente
                        if (pieceEntry.BoneMappings != null && pieceEntry.BoneMappings.Count > 0)
                        {
                            mappings = pieceEntry.BoneMappings;
                            Debug.Log($"[MRCoserRopa NDMF] Usando {mappings.Count} mapeos configurados");
                        }
                        else
                        {
                            // Crear referencia de armature para la ropa
                            var clothingRef = new ArmatureReference(pieceEntry.GameObject);
                            mappings = boneMapper.DetectBoneMappings(avatarRef, clothingRef);
                            Debug.Log($"[MRCoserRopa NDMF] Detectados {mappings.Count} mapeos automáticamente");
                        }

                        // Ejecutar el merge
                        var result = stitchingController.ExecuteStitching(mappings, StitchingMode.Merge, pieceEntry.GameObject);

                        if (result.Success)
                        {
                            Debug.Log($"[MRCoserRopa NDMF] Merge completado: {result.GetSummary()}");
                            totalProcessed++;
                        }
                        else
                        {
                            Debug.LogWarning($"[MRCoserRopa NDMF] Merge falló para '{pieceEntry.Name}': {result.GetSummary()}");
                            totalFailed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MRCoserRopa NDMF] Error procesando '{pieceEntry.Name}': {e.Message}");
                        Debug.LogException(e);
                        totalFailed++;
                    }
                }

                // Destruir el componente MRCoserRopa después de procesar (ya no es necesario en runtime)
                UnityEngine.Object.DestroyImmediate(coserRopa);
            }

            Debug.Log($"[MRCoserRopa NDMF] Procesamiento completado: {totalProcessed} exitosos, {totalFailed} fallidos");
        }
    }
}
#endif
