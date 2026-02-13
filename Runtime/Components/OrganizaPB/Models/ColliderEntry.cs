using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Representa un VRCPhysBoneCollider detectado en el avatar.
    /// Almacena la información necesaria para reubicarlo en el contenedor Colliders.
    /// </summary>
    [Serializable]
    public class ColliderEntry : ComponentEntry
    {
        public ColliderEntry() { }

        public ColliderEntry(Component component, Transform originalTransform, Transform rootTransform, OrganizationContext context)
            : base(component, originalTransform, rootTransform, context) { }

        protected override string GenerateDefaultName()
        {
            var name = RootTransform?.name ?? OriginalTransform?.name ?? "Unknown";
            return $"Col_{name}";
        }
    }
}
