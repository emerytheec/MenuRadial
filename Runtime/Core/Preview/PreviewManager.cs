using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Manager central para gestionar la previsualización en el sistema MR Control Menu
    /// Asegura que solo un componente tenga preview activo a la vez
    /// PATRÓN: Singleton para gestión centralizada de estado
    /// </summary>
    public static class PreviewManager
    {
        
        /// <summary>
        /// Componente que tiene preview activo actualmente (solo uno a la vez)
        /// </summary>
        private static IPreviewable _currentActivePreview;
        
        /// <summary>
        /// MRMenuControl que está gestionando el preview actual
        /// Útil para tracking de contexto
        /// </summary>
        private static object _currentMenuContext;
        
        /// <summary>
        /// Timestamp de cuando se activó el preview actual
        /// </summary>
        private static DateTime _activationTime;
        
        /// <summary>
        /// Lista de componentes registrados usando WeakReferences para evitar memory leaks
        /// REFACTORIZADO [2025-07-04]: Evita referencias a objetos destruidos
        /// OPTIMIZADO [2025-07-04]: Con cache para evitar reconteos frecuentes
        /// </summary>
        private static readonly List<System.WeakReference> _registeredComponents = new List<System.WeakReference>();
        
        // Cache para conteo de componentes vivos - OPTIMIZACIÓN [2025-07-04]
        private static int _cachedAliveCount = -1;
        private static int _lastCleanupFrame = -1;
        
        
        
        // Control de registro de eventos para prevenir dobles suscripciones
        private static bool _previewEventsRegistered = false;
        
        /// <summary>
        /// Evento que se dispara cuando cambia el preview activo
        /// Útil para actualizar UI
        /// </summary>
        /// <param name="previousPreview">Preview anterior (null si no había)</param>
        /// <param name="newPreview">Nuevo preview activo (null si se desactiva)</param>
        public static event Action<IPreviewable, IPreviewable> OnPreviewChanged; // (anterior, nuevo)
        
        /// <summary>
        /// Evento que se dispara cuando se activa un preview
        /// </summary>
        /// <param name="preview">Preview que se activó</param>
        public static event Action<IPreviewable> OnPreviewActivated;
        
        /// <summary>
        /// Evento que se dispara cuando se desactiva un preview
        /// </summary>
        /// <param name="preview">Preview que se desactivó</param>
        public static event Action<IPreviewable> OnPreviewDeactivated;
        
        /// <summary>
        /// Limpia todas las suscripciones de eventos de preview para prevenir memory leaks
        /// IMPORTANTE: Llamar antes de recargar assemblies o cambiar escenas
        /// </summary>
        public static void CleanupAllPreviewEventSubscriptions()
        {
            OnPreviewChanged = null;
            OnPreviewActivated = null;
            OnPreviewDeactivated = null;
            _previewEventsRegistered = false;
        }
        
        /// <summary>
        /// Verifica si hay suscriptores activos en los eventos de preview
        /// </summary>
        /// <returns>True si hay al menos un suscriptor activo</returns>
        public static bool HasActivePreviewEventSubscriptions()
        {
            return OnPreviewChanged != null || OnPreviewActivated != null || OnPreviewDeactivated != null;
        }
        
        
        
        /// <summary>
        /// Componente que tiene preview activo actualmente
        /// </summary>
        public static IPreviewable CurrentActivePreview => _currentActivePreview;
        
        /// <summary>
        /// Si hay algún preview activo
        /// </summary>
        public static bool HasActivePreview => _currentActivePreview != null;
        
        /// <summary>
        /// Tipo de preview activo actualmente
        /// </summary>
        public static PreviewType ActivePreviewType => 
            _currentActivePreview?.GetPreviewType() ?? PreviewType.None;
        
        /// <summary>
        /// Tiempo que lleva activo el preview actual
        /// </summary>
        public static TimeSpan ActiveDuration => 
            HasActivePreview ? DateTime.Now - _activationTime : TimeSpan.Zero;
        
        /// <summary>
        /// Número de componentes registrados activos (limpia referencias muertas automáticamente)
        /// OPTIMIZADO [2025-07-04]: Cache por frame para evitar recálculos costosos
        /// </summary>
        public static int RegisteredComponentsCount 
        { 
            get 
            {
                // Cache por frame para evitar reconteos frecuentes en UI
                if (_lastCleanupFrame != Time.frameCount)
                {
                    CleanupDestroyedComponents();
                    _cachedAliveCount = _registeredComponents.CountWhere(wr => wr.IsAlive);
                    _lastCleanupFrame = Time.frameCount;
                }
                return _cachedAliveCount;
            }
        }
        
        
        
        /// <summary>
        /// Activa un preview específico, desactivando cualquier preview anterior
        /// </summary>
        /// <param name="preview">Componente IPreviewable a activar</param>
        /// <param name="menuContext">Contexto del menú que solicita la activación (opcional)</param>
        public static void ActivatePreview(IPreviewable preview, object menuContext = null)
        {
            if (preview == null)
            {
                return;
            }
            
            // Si el mismo preview ya está activo, no hacer nada
            if (_currentActivePreview == preview)
            {
                return;
            }
            
            // Validación defensiva sin try-catch silencioso
            if (preview != null && preview.GetType() != null)
            {
                // Guardar referencia al preview anterior para el evento
                var previousPreview = _currentActivePreview;
                
                // Desactivar preview anterior si existe
                DeactivateCurrentPreview();
                
                // Activar nuevo preview
                preview.ActivatePreview();
                
                // Actualizar estado interno
                _currentActivePreview = preview;
                _currentMenuContext = menuContext;
                _activationTime = DateTime.Now;
                
                // Registrar componente si no está ya registrado
                RegisterComponentIfNeeded(preview);
                
                // Disparar eventos
                OnPreviewActivated?.Invoke(preview);
                OnPreviewChanged?.Invoke(previousPreview, preview);
            }
        }
        
        /// <summary>
        /// Desactiva el preview activo actual
        /// </summary>
        public static void DeactivateCurrentPreview()
        {
            if (_currentActivePreview == null)
                return;
                
            // Validación defensiva sin try-catch silencioso
            if (_currentActivePreview != null)
            {
                var previewToDeactivate = _currentActivePreview;
                
                // Desactivar el preview
                _currentActivePreview.DeactivatePreview();
                
                // Limpiar estado interno
                _currentActivePreview = null;
                _currentMenuContext = null;
                
                // Disparar eventos
                OnPreviewDeactivated?.Invoke(previewToDeactivate);
                OnPreviewChanged?.Invoke(previewToDeactivate, null);
            }
        }
        
        /// <summary>
        /// Alterna el estado de un preview (activar si está inactivo, desactivar si está activo)
        /// </summary>
        /// <param name="preview">Componente a alternar</param>
        /// <param name="menuContext">Contexto del menú (opcional)</param>
        public static void TogglePreview(IPreviewable preview, object menuContext = null)
        {
            if (preview == null)
                return;
                
            if (_currentActivePreview == preview)
            {
                DeactivateCurrentPreview();
            }
            else
            {
                ActivatePreview(preview, menuContext);
            }
        }
        
        
        
        /// <summary>
        /// Registra un componente para tracking usando WeakReference (automático al activar)
        /// </summary>
        /// <param name="preview">Componente a registrar</param>
        public static void RegisterComponent(IPreviewable preview)
        {
            if (preview == null)
                return;
            
            // Limpiar referencias muertas primero
            CleanupDestroyedComponents();
            
            // Verificar si ya está registrado - OPTIMIZADO: sin LINQ
            if (_registeredComponents.AnyWhere(wr => wr.IsAlive && wr.Target == preview))
            {
                // Invalidar cache
                _lastCleanupFrame = -1;
                return;
            }
                
            _registeredComponents.Add(new System.WeakReference(preview));
            
            // Invalidar cache después de agregar
            _lastCleanupFrame = -1;
            
        }
        
        /// <summary>
        /// Desregistra un componente usando WeakReference y cleanup de eventos estáticos
        /// REFACTORIZADO [2025-07-04]: Agregado cleanup de eventos estáticos para evitar memory leaks
        /// </summary>
        /// <param name="preview">Componente a desregistrar</param>
        public static void UnregisterComponent(IPreviewable preview)
        {
            if (preview == null)
                return;
                
            // Si es el preview activo, desactivarlo primero
            if (_currentActivePreview == preview)
            {
                DeactivateCurrentPreview();
            }
            
            // NUEVO: Cleanup de eventos estáticos para evitar memory leaks
            CleanupStaticEventsForComponent(preview);
            
            // Remover WeakReference específica - OPTIMIZADO: sin listas temporales
            for (int i = _registeredComponents.Count - 1; i >= 0; i--)
            {
                var weakRef = _registeredComponents[i];
                if (weakRef.IsAlive && weakRef.Target == preview)
                {
                    _registeredComponents.RemoveAt(i);
                }
            }
            
            // Invalidar cache después de modificaciones
            _lastCleanupFrame = -1;
            
        }
        
        /// <summary>
        /// Registra un componente si no está ya registrado (compatible con WeakReference)
        /// </summary>
        private static void RegisterComponentIfNeeded(IPreviewable preview)
        {
            if (!_registeredComponents.Any(wr => wr.IsAlive && wr.Target == preview))
            {
                RegisterComponent(preview);
            }
        }
        
        /// <summary>
        /// Limpia automáticamente referencias a componentes destruidos
        /// NUEVO [2025-07-04]: Previene memory leaks
        /// </summary>
        private static void CleanupDestroyedComponents()
        {
            int originalCount = _registeredComponents.Count;
            _registeredComponents.RemoveAll(wr => !wr.IsAlive);
            
            int removedCount = originalCount - _registeredComponents.Count;
            if (removedCount > 0)
            {
            }
        }
        
        /// <summary>
        /// Limpia eventos estáticos específicos para un componente
        /// NUEVO [2025-07-04]: Previene memory leaks en eventos estáticos
        /// </summary>
        /// <param name="componentToCleanup">Componente del cual limpiar suscripciones</param>
        private static void CleanupStaticEventsForComponent(IPreviewable componentToCleanup)
        {
            int removedSubscriptions = 0;
            
            // Cleanup OnPreviewChanged (evento con 2 parámetros)
            removedSubscriptions += CleanupEventSubscriptions<IPreviewable, IPreviewable>(
                ref OnPreviewChanged, 
                componentToCleanup, 
                "OnPreviewChanged"
            );
            
            // Cleanup OnPreviewActivated
            removedSubscriptions += CleanupEventSubscriptions<IPreviewable>(
                ref OnPreviewActivated, 
                componentToCleanup, 
                "OnPreviewActivated"
            );
            
            // Cleanup OnPreviewDeactivated
            removedSubscriptions += CleanupEventSubscriptions<IPreviewable>(
                ref OnPreviewDeactivated, 
                componentToCleanup, 
                "OnPreviewDeactivated"
            );
            
            if (removedSubscriptions > 0)
            {
            }
        }
        
        /// <summary>
        /// Helper method para limpiar suscripciones específicas de un evento
        /// </summary>
        private static int CleanupEventSubscriptions<T>(ref System.Action<T> eventField, object targetComponent, string eventName)
        {
            if (eventField == null) return 0;
            
            int removedCount = 0;
            var handlersToRemove = new System.Collections.Generic.List<System.Action<T>>();
            
            foreach (var handler in eventField.GetInvocationList())
            {
                if (handler.Target == targetComponent || 
                    (handler.Target is UnityEngine.Object unityObj && unityObj == null))
                {
                    handlersToRemove.Add((System.Action<T>)handler);
                }
            }
            
            foreach (var handler in handlersToRemove)
            {
                eventField -= handler;
                removedCount++;
            }
            
            return removedCount;
        }
        
        /// <summary>
        /// Helper method para limpiar suscripciones de eventos con 2 parámetros
        /// </summary>
        private static int CleanupEventSubscriptions<T1, T2>(ref System.Action<T1, T2> eventField, object targetComponent, string eventName)
        {
            if (eventField == null) return 0;
            
            int removedCount = 0;
            var handlersToRemove = new System.Collections.Generic.List<System.Action<T1, T2>>();
            
            foreach (var handler in eventField.GetInvocationList())
            {
                if (handler.Target == targetComponent || 
                    (handler.Target is UnityEngine.Object unityObj && unityObj == null))
                {
                    handlersToRemove.Add((System.Action<T1, T2>)handler);
                }
            }
            
            foreach (var handler in handlersToRemove)
            {
                eventField -= handler;
                removedCount++;
            }
            
            return removedCount;
        }
        
        
        
        
        /// <summary>
        /// Obtiene lista de tipos de preview disponibles entre los componentes registrados vivos
        /// </summary>
        /// <returns>Lista de tipos únicos</returns>
        public static List<PreviewType> GetAvailablePreviewTypes()
        {
            var types = new HashSet<PreviewType>();
            
            CleanupDestroyedComponents();
            foreach (var weakRef in _registeredComponents)
            {
                if (weakRef.IsAlive && weakRef.Target is IPreviewable component)
                {
                    types.Add(component.GetPreviewType());
                }
            }
            
            return new List<PreviewType>(types);
        }
        
        
        
        
        /// <summary>
        /// Limpia completamente el estado del PreviewManager
        /// Útil para testing o reset del sistema
        /// </summary>
        public static void ClearAll()
        {
            // Validación defensiva sin try-catch silencioso
            DeactivateCurrentPreview();
            if (_registeredComponents != null)
            {
                _registeredComponents.Clear();
            }
            _currentMenuContext = null;
            
            // NUEVO: Limpiar eventos estáticos completamente
            OnPreviewChanged = null;
            OnPreviewActivated = null;
            OnPreviewDeactivated = null;
        }
        
        
        
        /// <summary>
        /// Método para llamar cuando Unity se cierra o cambia de escena
        /// Asegura limpieza apropiada
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            // Limpiar estado estático al cargar
            ClearAll();
        }
        
    }
}
