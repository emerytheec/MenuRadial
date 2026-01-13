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
    /// La organización ahora ocurre en el editor.
    /// NDMF solo verifica el estado y elimina el componente.
    /// </summary>
    public class MROrganizaPBPlugin : Plugin<MROrganizaPBPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.organiza_pb";
        public override string DisplayName => "MR Organiza PB";

        public override Color? ThemeColor => new Color(0.8f, 0.4f, 0.8f, 1f); // Purple

        protected override void Configure()
        {
            // Ejecutar en fase Transforming para limpiar componentes
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
    /// Pass que verifica el estado y elimina el componente MROrganizaPB.
    /// La organización real ya ocurrió en el editor.
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
                // Verificar si la organización ya fue realizada en el editor
                if (organizaPB.State == OrganizationState.Organized)
                {
                    Debug.Log($"[MROrganizaPB NDMF] PhysBones ya organizados en el editor. Contenedores: {organizaPB.CreatedContainers.Count}");
                }
                else if (organizaPB.State == OrganizationState.Scanned)
                {
                    Debug.LogWarning($"[MROrganizaPB NDMF] PhysBones escaneados pero NO organizados. " +
                                     $"Presiona 'Organizar PhysBones' en el inspector antes de subir el avatar.");
                }
                else
                {
                    Debug.Log($"[MROrganizaPB NDMF] Componente sin escanear, ignorando.");
                }

                // Eliminar el componente (es IEditorOnly)
                UnityEngine.Object.DestroyImmediate(organizaPB);
            }
        }
    }
}

#endif
