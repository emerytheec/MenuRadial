using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Interfaz para notificación de eventos de frame.
    /// Permite desacoplar el sistema de eventos de la implementación concreta.
    /// </summary>
    public interface IFrameEventNotifier
    {
        #region Eventos de Objetos

        /// <summary>
        /// Notifica que un objeto fue añadido
        /// </summary>
        void NotifyObjectAdded(MRAgruparObjetos frame, GameObject obj);

        /// <summary>
        /// Notifica que un objeto fue removido
        /// </summary>
        void NotifyObjectRemoved(MRAgruparObjetos frame, GameObject obj);

        #endregion

        #region Eventos de Materiales

        /// <summary>
        /// Notifica que un material fue añadido
        /// </summary>
        void NotifyMaterialAdded(MRAgruparObjetos frame, Renderer renderer, int materialIndex);

        /// <summary>
        /// Notifica que un material fue removido
        /// </summary>
        void NotifyMaterialRemoved(MRAgruparObjetos frame, Renderer renderer, int materialIndex);

        #endregion

        #region Eventos de Blendshapes

        /// <summary>
        /// Notifica que un blendshape fue añadido
        /// </summary>
        void NotifyBlendshapeAdded(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName);

        /// <summary>
        /// Notifica que un blendshape fue removido
        /// </summary>
        void NotifyBlendshapeRemoved(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName);

        #endregion

        #region Eventos Generales

        /// <summary>
        /// Notifica un cambio general en el estado del frame
        /// </summary>
        void NotifyStateChanged(MRAgruparObjetos frame, string changeDescription);

        /// <summary>
        /// Notifica cambio en el estado de preview
        /// </summary>
        void NotifyPreviewStateChanged(MRAgruparObjetos frame, bool isPreviewActive);

        #endregion
    }

    /// <summary>
    /// Implementación por defecto del notificador de eventos.
    /// Delega al sistema de eventos estáticos existente para compatibilidad.
    /// </summary>
    public class DefaultFrameEventNotifier : IFrameEventNotifier
    {
        /// <summary>
        /// Instancia singleton por defecto
        /// </summary>
        public static readonly DefaultFrameEventNotifier Instance = new DefaultFrameEventNotifier();

        private DefaultFrameEventNotifier() { }

        public void NotifyObjectAdded(MRAgruparObjetos frame, GameObject obj)
        {
            FrameObjectEventSystem.NotifyObjectAdded(frame, obj);
        }

        public void NotifyObjectRemoved(MRAgruparObjetos frame, GameObject obj)
        {
            FrameObjectEventSystem.NotifyObjectRemoved(frame, obj);
        }

        public void NotifyMaterialAdded(MRAgruparObjetos frame, Renderer renderer, int materialIndex)
        {
            FrameObjectEventSystem.NotifyMaterialAdded(frame, renderer, materialIndex);
        }

        public void NotifyMaterialRemoved(MRAgruparObjetos frame, Renderer renderer, int materialIndex)
        {
            FrameObjectEventSystem.NotifyMaterialRemoved(frame, renderer, materialIndex);
        }

        public void NotifyBlendshapeAdded(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName)
        {
            FrameObjectEventSystem.NotifyBlendshapeAdded(frame, renderer, blendshapeName);
        }

        public void NotifyBlendshapeRemoved(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName)
        {
            FrameObjectEventSystem.NotifyBlendshapeRemoved(frame, renderer, blendshapeName);
        }

        public void NotifyStateChanged(MRAgruparObjetos frame, string changeDescription)
        {
            FrameObjectEventSystem.NotifyStateChanged(frame, changeDescription);
        }

        public void NotifyPreviewStateChanged(MRAgruparObjetos frame, bool isPreviewActive)
        {
            FrameObjectEventSystem.NotifyPreviewStateChanged(frame, isPreviewActive);
        }
    }

    /// <summary>
    /// Implementación nula del notificador (patrón Null Object).
    /// Útil para testing o cuando no se necesitan eventos.
    /// </summary>
    public class NullFrameEventNotifier : IFrameEventNotifier
    {
        /// <summary>
        /// Instancia singleton
        /// </summary>
        public static readonly NullFrameEventNotifier Instance = new NullFrameEventNotifier();

        private NullFrameEventNotifier() { }

        public void NotifyObjectAdded(MRAgruparObjetos frame, GameObject obj) { }
        public void NotifyObjectRemoved(MRAgruparObjetos frame, GameObject obj) { }
        public void NotifyMaterialAdded(MRAgruparObjetos frame, Renderer renderer, int materialIndex) { }
        public void NotifyMaterialRemoved(MRAgruparObjetos frame, Renderer renderer, int materialIndex) { }
        public void NotifyBlendshapeAdded(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName) { }
        public void NotifyBlendshapeRemoved(MRAgruparObjetos frame, SkinnedMeshRenderer renderer, string blendshapeName) { }
        public void NotifyStateChanged(MRAgruparObjetos frame, string changeDescription) { }
        public void NotifyPreviewStateChanged(MRAgruparObjetos frame, bool isPreviewActive) { }
    }
}
