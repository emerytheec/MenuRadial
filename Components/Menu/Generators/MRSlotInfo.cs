using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Información recopilada de cada slot para la generación de archivos VRChat.
    /// DTO (Data Transfer Object) usado por todos los generadores.
    /// </summary>
    public class MRSlotInfo
    {
        /// <summary>
        /// Slot de origen
        /// </summary>
        public MRAnimationSlot Slot { get; set; }

        /// <summary>
        /// Tipo de animación del slot
        /// </summary>
        public AnimationType AnimationType { get; set; }

        /// <summary>
        /// Proveedor de animación (MRUnificarObjetos, MRIluminacionRadial, etc.)
        /// </summary>
        public IAnimationProvider AnimationProvider { get; set; }

        /// <summary>
        /// Clips de animación encontrados para este slot
        /// </summary>
        public List<AnimationClip> AnimationClips { get; set; } = new List<AnimationClip>();

        /// <summary>
        /// Si este slot es de tipo Illumination
        /// </summary>
        public bool IsIllumination { get; set; }

        /// <summary>
        /// Referencia al MRMenuControl hijo si es SubMenu
        /// </summary>
        public MRMenuControl SubMenuComponent { get; set; }

        /// <summary>
        /// Información de slots hijos (para SubMenus)
        /// </summary>
        public List<MRSlotInfo> ChildSlotInfos { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MRSlotInfo() { }

        /// <summary>
        /// Constructor con datos básicos
        /// </summary>
        public MRSlotInfo(MRAnimationSlot slot, AnimationType animationType, IAnimationProvider provider)
        {
            Slot = slot;
            AnimationType = animationType;
            AnimationProvider = provider;
        }

        /// <summary>
        /// Verifica si tiene clips de animación válidos
        /// </summary>
        public bool HasValidClips => AnimationClips != null && AnimationClips.Count > 0;

        /// <summary>
        /// Verifica si es un submenú con hijos
        /// </summary>
        public bool HasChildren => ChildSlotInfos != null && ChildSlotInfos.Count > 0;

        /// <summary>
        /// Nombre del slot para logs
        /// </summary>
        public string DisplayName => Slot?.slotName ?? "Sin nombre";
    }
}
