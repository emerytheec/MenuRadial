#if MR_NDMF_AVAILABLE

using System;
using UnityEngine;
using nadena.dev.ndmf;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.OrganizaPB.MROrganizaPBPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.OrganizaPB
{
    /// <summary>
    /// Plugin NDMF para MROrganizaPB.
    /// Si la organización ya fue realizada en el editor, solo limpia el componente.
    /// Si no fue realizada, auto-organiza sobre el clon del avatar como red de seguridad.
    /// </summary>
    public class MROrganizaPBPlugin : Plugin<MROrganizaPBPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.organiza_pb";
        public override string DisplayName => "MR Organiza PB";

        public override Color? ThemeColor => new Color(0.8f, 0.4f, 0.8f, 1f);

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run(MROrganizaPBPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MROrganizaPB] Error durante el procesamiento NDMF: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que auto-organiza PhysBones si no fue hecho en el editor.
    /// NDMF trabaja sobre un clon del avatar, así que es seguro modificarlo.
    /// </summary>
    internal class MROrganizaPBPass : Pass<MROrganizaPBPass>
    {
        public override string DisplayName => "Organiza PhysBones";

        protected override void Execute(BuildContext context)
        {
            var components = context.AvatarRootObject
                .GetComponentsInChildren<MROrganizaPB>(true);

            if (components.Length == 0)
            {
                return;
            }

            foreach (var organizaPB in components)
            {
                ProcessOrganizaPB(organizaPB);

                // Eliminar el componente (es IEditorOnly)
                UnityEngine.Object.DestroyImmediate(organizaPB);
            }
        }

        private void ProcessOrganizaPB(MROrganizaPB organizaPB)
        {
            // Si ya está organizado en editor → OK
            if (organizaPB.State == OrganizationState.Organized)
            {
                Debug.Log($"[MROrganizaPB NDMF] PhysBones ya organizados en el editor.");
                return;
            }

            // Si no está organizado → auto-organizar sobre el clon
            if (organizaPB.State == OrganizationState.NotScanned)
            {
                organizaPB.ScanAvatar();
            }

            if (organizaPB.CanOrganize)
            {
                Debug.Log("[MROrganizaPB NDMF] Auto-organizando PhysBones en build...");

                // Ejecutar sin Undo (es un clon temporal de NDMF)
                var result = organizaPB.OrganizeForBuild();

                if (result.Success)
                {
                    Debug.Log($"[MROrganizaPB NDMF] Auto-organización completada: {result.GetSummary()}");
                }
                else
                {
                    Debug.LogWarning($"[MROrganizaPB NDMF] Auto-organización falló: {result.GetSummary()}");
                }
            }
        }
    }
}

#endif
