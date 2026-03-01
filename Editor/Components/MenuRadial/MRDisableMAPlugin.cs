#if MR_NDMF_AVAILABLE
using System;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Bender_Dios.MenuRadial.Components.MenuRadial;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.MenuRadial.MRDisableMAPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Plugin NDMF que desactiva Modular Avatar si está configurado en MRMenuRadial.
    /// Se ejecuta en la fase Resolving (la más temprana) ANTES que cualquier otro plugin.
    ///
    /// SEGURIDAD: Solo afecta al CLON del avatar durante el build, no modifica la escena original.
    /// </summary>
    public class MRDisableMAPlugin : Plugin<MRDisableMAPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.disable_ma";
        public override string DisplayName => "MR - Desactivar Modular Avatar";

        // Color tema: Rojo (advertencia)
        public override Color? ThemeColor => new Color(0xE5 / 255f, 0x39 / 255f, 0x35 / 255f, 1);

        protected override void Configure()
        {
            // Ejecutar en la fase más temprana posible, ANTES de todo
            InPhase(BuildPhase.Resolving)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run(MRDisableMAPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRDisableMA NDMF] Error: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que desactiva componentes de Modular Avatar en el clon del avatar.
    /// </summary>
    internal class MRDisableMAPass : Pass<MRDisableMAPass>
    {
        public override string DisplayName => "Desactivar Modular Avatar (si configurado)";

        // Namespace de Modular Avatar
        private const string MA_NAMESPACE = "nadena.dev.modular_avatar";

        protected override void Execute(BuildContext context)
        {
            // Buscar MRMenuRadial que tenga DisableModularAvatarNDMF = true
            var menuRadials = FindMenuRadials(context);

            bool shouldDisableMA = false;
            foreach (var menuRadial in menuRadials)
            {
                if (menuRadial != null && menuRadial.DisableModularAvatarNDMF)
                {
                    shouldDisableMA = true;
                    break;
                }
            }

            if (!shouldDisableMA)
            {
                return; // No hacer nada si no está configurado
            }

            Debug.Log("[MRDisableMA NDMF] Destruyendo componentes de Modular Avatar del clon...");

            int disabledCount = 0;

            // Obtener TODOS los MonoBehaviour del avatar
            var allBehaviours = context.AvatarRootObject.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var behaviour in allBehaviours)
            {
                if (behaviour == null) continue;

                // Verificar si es un componente de Modular Avatar
                var typeName = behaviour.GetType().FullName;
                if (typeName != null && IsModularAvatarComponent(typeName))
                {
                    // Destruir el componente del clon - desactivar no es suficiente
                    // porque MA usa GetComponentsInChildren(true) que encuentra componentes desactivados
                    Debug.Log($"[MRDisableMA NDMF] Destruyendo: {behaviour.GetType().Name} en '{behaviour.gameObject.name}'");
                    UnityEngine.Object.DestroyImmediate(behaviour);
                    disabledCount++;
                }
            }

            if (disabledCount > 0)
            {
                Debug.Log($"[MRDisableMA NDMF] Total: {disabledCount} componentes de Modular Avatar destruidos del clon");
            }
            else
            {
                Debug.Log("[MRDisableMA NDMF] No se encontraron componentes de Modular Avatar para destruir");
            }
        }

        /// <summary>
        /// Verifica si un tipo pertenece a Modular Avatar.
        /// </summary>
        private bool IsModularAvatarComponent(string typeName)
        {
            // Modular Avatar usa estos namespaces:
            // - nadena.dev.modular_avatar.core
            // - nadena.dev.modular_avatar.editor
            return typeName.StartsWith("nadena.dev.modular_avatar", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Busca MRMenuRadial en el avatar o externos que referencien el avatar.
        /// </summary>
        private MRMenuRadial[] FindMenuRadials(BuildContext context)
        {
            // Primero buscar dentro del avatar
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
                        && mr.AvatarRoot.name == avatarName
                        && !mr.AvatarRoot.name.EndsWith("(Clone)"))
                    .ToArray();

                if (matches.Length > 1)
                {
                    Debug.LogWarning($"[MRDisableMA NDMF] Se encontraron {matches.Length} MRMenuRadial externos para '{avatarName}'. " +
                        "Usando solo el primero para evitar conflictos.");
                    matches = new[] { matches[0] };
                }

                menuRadials = matches;
            }

            return menuRadials;
        }
    }
}
#endif
