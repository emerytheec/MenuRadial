using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Sistema de eventos centralizado para frames
    /// Extraído de MRAgruparObjetos para cumplir con principio de responsabilidad única
    /// </summary>
    public static class FrameObjectEventSystem
    {
        // Control de registro de eventos para prevenir dobles suscripciones
        private static bool _eventsRegistered = false;
        
        /// <summary>
        /// Evento disparado cuando se agrega un GameObject a un frame
        /// </summary>
        /// <param name="frameObject">Frame que contiene el objeto</param>
        /// <param name="gameObject">GameObject agregado</param>
        public static event System.Action<MRAgruparObjetos, GameObject> OnObjectAdded;
        
        /// <summary>
        /// Evento disparado cuando se remueve un GameObject de un frame
        /// </summary>
        /// <param name="frameObject">Frame que contenía el objeto</param>
        /// <param name="gameObject">GameObject removido</param>
        public static event System.Action<MRAgruparObjetos, GameObject> OnObjectRemoved;
        
        /// <summary>
        /// Evento disparado cuando se agrega un material a un frame
        /// </summary>
        /// <param name="frameObject">Frame que contiene el material</param>
        /// <param name="renderer">Renderer del material</param>
        /// <param name="materialIndex">Índice del material</param>
        public static event System.Action<MRAgruparObjetos, Renderer, int> OnMaterialAdded;
        
        /// <summary>
        /// Evento disparado cuando se remueve un material de un frame
        /// </summary>
        /// <param name="frameObject">Frame que contenía el material</param>
        /// <param name="renderer">Renderer del material</param>
        /// <param name="materialIndex">Índice del material</param>
        public static event System.Action<MRAgruparObjetos, Renderer, int> OnMaterialRemoved;
        
        /// <summary>
        /// Evento disparado cuando se agrega un blendshape a un frame
        /// </summary>
        /// <param name="frameObject">Frame que contiene el blendshape</param>
        /// <param name="renderer">SkinnedMeshRenderer del blendshape</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        public static event System.Action<MRAgruparObjetos, SkinnedMeshRenderer, string> OnBlendshapeAdded;
        
        /// <summary>
        /// Evento disparado cuando se remueve un blendshape de un frame
        /// </summary>
        /// <param name="frameObject">Frame que contenía el blendshape</param>
        /// <param name="renderer">SkinnedMeshRenderer del blendshape</param>
        /// <param name="blendshapeName">Nombre del blendshape</param>
        public static event System.Action<MRAgruparObjetos, SkinnedMeshRenderer, string> OnBlendshapeRemoved;

        /// <summary>
        /// Evento disparado cuando cambia el estado de un frame
        /// </summary>
        /// <param name="frameObject">Frame que cambió</param>
        /// <param name="stateChange">Descripción del cambio</param>
        public static event System.Action<MRAgruparObjetos, string> OnStateChanged;

        /// <summary>
        /// Evento disparado cuando cambia el estado de preview de un frame
        /// </summary>
        /// <param name="frameObject">Frame que cambió</param>
        /// <param name="isPreviewActive">Si el preview está activo</param>
        public static event System.Action<MRAgruparObjetos, bool> OnPreviewStateChanged;

        /// <summary>
        /// Registra los eventos de sistema para prevenir dobles suscripciones
        /// </summary>
        public static void RegisterEvents()
        {
            if (_eventsRegistered) return;
            _eventsRegistered = true;
        }
        
        /// <summary>
        /// Desregistra todos los eventos para cleanup
        /// </summary>
        public static void UnregisterEvents()
        {
            OnObjectAdded = null;
            OnObjectRemoved = null;
            OnMaterialAdded = null;
            OnMaterialRemoved = null;
            OnBlendshapeAdded = null;
            OnBlendshapeRemoved = null;
            OnStateChanged = null;
            OnPreviewStateChanged = null;
            _eventsRegistered = false;
        }
        
        /// <summary>
        /// Dispara el evento de objeto agregado
        /// </summary>
        public static void NotifyObjectAdded(MRAgruparObjetos frameObject, GameObject gameObject)
        {
            OnObjectAdded?.Invoke(frameObject, gameObject);
        }
        
        /// <summary>
        /// Dispara el evento de objeto removido
        /// </summary>
        public static void NotifyObjectRemoved(MRAgruparObjetos frameObject, GameObject gameObject)
        {
            OnObjectRemoved?.Invoke(frameObject, gameObject);
        }
        
        /// <summary>
        /// Dispara el evento de material agregado
        /// </summary>
        public static void NotifyMaterialAdded(MRAgruparObjetos frameObject, Renderer renderer, int materialIndex)
        {
            OnMaterialAdded?.Invoke(frameObject, renderer, materialIndex);
        }
        
        /// <summary>
        /// Dispara el evento de material removido
        /// </summary>
        public static void NotifyMaterialRemoved(MRAgruparObjetos frameObject, Renderer renderer, int materialIndex)
        {
            OnMaterialRemoved?.Invoke(frameObject, renderer, materialIndex);
        }
        
        /// <summary>
        /// Dispara el evento de blendshape agregado
        /// </summary>
        public static void NotifyBlendshapeAdded(MRAgruparObjetos frameObject, SkinnedMeshRenderer renderer, string blendshapeName)
        {
            OnBlendshapeAdded?.Invoke(frameObject, renderer, blendshapeName);
        }
        
        /// <summary>
        /// Dispara el evento de blendshape removido
        /// </summary>
        public static void NotifyBlendshapeRemoved(MRAgruparObjetos frameObject, SkinnedMeshRenderer renderer, string blendshapeName)
        {
            OnBlendshapeRemoved?.Invoke(frameObject, renderer, blendshapeName);
        }
        
        /// <summary>
        /// Dispara el evento de cambio de estado
        /// </summary>
        public static void NotifyStateChanged(MRAgruparObjetos frameObject, string stateChange)
        {
            OnStateChanged?.Invoke(frameObject, stateChange);
        }
        
        /// <summary>
        /// Dispara el evento de cambio de estado de preview
        /// </summary>
        public static void NotifyPreviewStateChanged(MRAgruparObjetos frameObject, bool isPreviewActive)
        {
            OnPreviewStateChanged?.Invoke(frameObject, isPreviewActive);
        }
    }
}