using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.MenuRadial;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers
{
    /// <summary>
    /// Detecta contenedores PhysBone existentes (ya organizados) y busca los frames
    /// correspondientes para vincularlos automáticamente.
    /// </summary>
    public static class PhysBoneFrameLinker
    {
        /// <summary>
        /// Detecta contenedores existentes a partir de entries ya organizados.
        /// Para cada entry con IsAlreadyOrganized, busca el contenedor hermano del armature.
        /// </summary>
        public static IEnumerable<(OrganizationContext context, GameObject container)> DetectExistingContainers(
            IReadOnlyList<PhysBoneEntry> physBones, IReadOnlyList<ColliderEntry> colliders)
        {
            var seen = new HashSet<GameObject>();

            foreach (var entry in physBones)
            {
                if (!entry.IsAlreadyOrganized || !entry.IsValid) continue;
                var container = FindSiblingContainer(entry);
                if (container != null && seen.Add(container))
                    yield return (entry.Context, container);
            }

            foreach (var entry in colliders)
            {
                if (!entry.IsAlreadyOrganized || !entry.IsValid) continue;
                var container = FindSiblingContainer(entry);
                if (container != null && seen.Add(container))
                    yield return (entry.Context, container);
            }
        }

        /// <summary>
        /// Busca el frame (MRAgruparObjetos) que corresponde a un contexto de organización.
        /// Para avatar busca por nombre "Avatar", para ropa busca por ContextName.
        /// </summary>
        public static MRAgruparObjetos FindFrameForContext(OrganizationContext context, MRMenuRadial menuRadial)
        {
            if (context == null || menuRadial == null) return null;

            var unificadores = menuRadial.GetComponentsInChildren<MRUnificarObjetos>(true);

            foreach (var unificador in unificadores)
            {
                if (unificador.FrameObjects == null) continue;

                foreach (var frame in unificador.FrameObjects)
                {
                    if (frame == null) continue;

                    if (context.IsAvatarContext)
                    {
                        if (frame.gameObject.name == "Avatar")
                            return frame;
                    }
                    else
                    {
                        if (frame.gameObject.name == context.ContextName)
                            return frame;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Camina desde el OriginalTransform del entry hacia arriba hasta encontrar
        /// un ancestro que sea hermano del armature (su parent == armature.parent).
        /// Ese ancestro es el contenedor (VRCPB, VRCPBC, etc.).
        /// </summary>
        private static GameObject FindSiblingContainer(ComponentEntry entry)
        {
            if (entry.Context?.ArmatureTransform == null) return null;

            var armatureParent = entry.Context.ArmatureTransform.parent;
            if (armatureParent == null) return null;

            var current = entry.OriginalTransform;

            while (current != null)
            {
                if (current.parent == armatureParent)
                    return current.gameObject;

                current = current.parent;
            }

            return null;
        }
    }
}
