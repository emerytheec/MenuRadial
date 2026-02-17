#if MR_NDMF_AVAILABLE

using System;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Controllers;
using Bender_Dios.MenuRadial.Components.AjustarBounds.Models;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.AjustarBounds.MRAjustarBoundsPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.AjustarBounds
{
    /// <summary>
    /// Plugin NDMF para MRAjustarBounds.
    /// Recalcula y aplica bounds unificados sobre el clon del avatar DESPUES de que
    /// MRCoserRopa y Modular Avatar hayan terminado de modificar renderers.
    /// Esto garantiza que los bounds finales sean correctos independientemente
    /// de los cambios que otros plugins hagan a rootBone/localBounds/transform.
    /// </summary>
    public class MRAjustarBoundsPlugin : Plugin<MRAjustarBoundsPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.ajustar_bounds";
        public override string DisplayName => "MR Ajustar Bounds";

        public override Color? ThemeColor => new Color(0x4C / 255f, 0xAF / 255f, 0x50 / 255f, 1);

        protected override void Configure()
        {
            // Ejecutar DESPUES de Modular Avatar para que todos los renderers
            // ya esten en su estado final (bones cosidos, meshes mergeados, etc.)
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run(MRAjustarBoundsPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRAjustarBounds NDMF] Error: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que recalcula bounds frescos sobre el clon del avatar.
    /// No reutiliza los datos del editor porque MRCoserRopa/MA pueden haber
    /// cambiado rootBone, bones[], localBounds y transform de los renderers.
    /// </summary>
    internal class MRAjustarBoundsPass : Pass<MRAjustarBoundsPass>
    {
        public override string DisplayName => "Ajustar Bounds";

        protected override void Execute(BuildContext context)
        {
            var avatarRoot = context.AvatarRootObject;
            var components = avatarRoot.GetComponentsInChildren<MRAjustarBounds>(true);

            if (components.Length == 0)
                return;

            foreach (var ajustarBounds in components)
            {
                ProcessBounds(ajustarBounds, avatarRoot);

                // Eliminar el componente del clon
                UnityEngine.Object.DestroyImmediate(ajustarBounds);
            }
        }

        private void ProcessBounds(MRAjustarBounds ajustarBounds, GameObject avatarRoot)
        {
            var calculator = new BoundsCalculator();

            // Detectar rootBone (Hips) en el clon
            var rootBone = calculator.DetectSharedRootBone(avatarRoot);
            if (rootBone == null)
            {
                Debug.LogWarning("[MRAjustarBounds NDMF] No se detecto rootBone (Hips)");
                return;
            }

            // Escanear meshes frescos del clon (post-cosido, post-MA)
            var meshInfos = calculator.ScanAvatar(avatarRoot);
            if (meshInfos.Count == 0)
            {
                Debug.LogWarning("[MRAjustarBounds NDMF] No se encontraron meshes");
                return;
            }

            // Calcular bounds unificados
            var result = calculator.CalculateUnifiedBounds(
                meshInfos, rootBone, ajustarBounds.MarginPercentage);

            if (!result.Success)
            {
                Debug.LogWarning($"[MRAjustarBounds NDMF] Calculo fallo: {result.GetSummary()}");
                return;
            }

            // Aplicar bounds unificados a todos los renderers
            int applied = calculator.ApplyUnifiedBounds(
                meshInfos, result.UnifiedBoundsWithMargin, rootBone);

            Debug.Log($"[MRAjustarBounds NDMF] Bounds aplicados a {applied} meshes " +
                      $"({result.FinalSize.x:F2}x{result.FinalSize.y:F2}x{result.FinalSize.z:F2}m)" +
                      (result.WasClamped ? " [LIMITADO]" : ""));

            // Particulas
            if (ajustarBounds.IncludeParticles)
            {
                var particleInfos = calculator.ScanParticles(avatarRoot);
                if (particleInfos.Count > 0)
                {
                    calculator.CalculateIndividualParticleBounds(
                        particleInfos, avatarRoot.transform,
                        ajustarBounds.ParticleMarginPercentage);
                    int particlesApplied = calculator.ApplyParticleBounds(particleInfos);
                    Debug.Log($"[MRAjustarBounds NDMF] Bounds aplicados a {particlesApplied} particulas");
                }
            }

            // Anchor Override (iluminacion)
            if (ajustarBounds.UnifyAnchorOverride)
            {
                var anchor = DetectAnchor(ajustarBounds, avatarRoot);
                if (anchor != null)
                {
                    int anchorApplied = 0;
                    foreach (var meshInfo in meshInfos)
                    {
                        if (meshInfo.IsValid && meshInfo.Renderer != null)
                        {
                            meshInfo.ApplyProbeAnchor(anchor);
                            anchorApplied++;
                        }
                    }
                    Debug.Log($"[MRAjustarBounds NDMF] Anchor '{anchor.name}' aplicado a {anchorApplied} meshes");
                }
            }
        }

        /// <summary>
        /// Detecta el anchor en el clon del avatar (no puede usar EffectiveAnchor
        /// porque las referencias serializadas pueden apuntar a la escena original).
        /// </summary>
        private Transform DetectAnchor(MRAjustarBounds ajustarBounds, GameObject avatarRoot)
        {
            // Si tiene anchor manual y es hijo del avatar (referencia valida en el clon)
            if (ajustarBounds.AnchorOverride != null &&
                ajustarBounds.AnchorOverride.IsChildOf(avatarRoot.transform))
            {
                return ajustarBounds.AnchorOverride;
            }

            // Auto-detectar Chest en el clon
            if (ajustarBounds.AutoDetectChest)
            {
                var animator = avatarRoot.GetComponent<Animator>();
                if (animator != null && animator.isHuman)
                {
                    var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (chest != null) return chest;

                    var upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                    if (upperChest != null) return upperChest;

                    var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                    if (spine != null) return spine;
                }
            }

            return null;
        }
    }
}

#endif
