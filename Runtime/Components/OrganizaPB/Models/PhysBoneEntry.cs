using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Representa un VRCPhysBone detectado en el avatar.
    /// Almacena la información necesaria para reubicarlo en el contenedor PhysBones.
    /// </summary>
    [Serializable]
    public class PhysBoneEntry : ComponentEntry
    {
        public PhysBoneEntry() { }

        public PhysBoneEntry(Component component, Transform originalTransform, Transform rootTransform, OrganizationContext context)
            : base(component, originalTransform, rootTransform, context) { }

        protected override string GenerateDefaultName()
        {
            if (RootTransform != null)
                return $"PB_{RootTransform.name}";
            if (OriginalTransform != null)
                return $"PB_{OriginalTransform.name}";
            return "PB_Unknown";
        }
    }
}
