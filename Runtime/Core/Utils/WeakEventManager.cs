using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Preview;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Manager de eventos usando WeakReferences para prevenir memory leaks
    /// NUEVO [2025-07-04]: Alternativa a eventos estáticos que mantienen referencias fuertes
    /// </summary>
    /// <typeparam name="TEventArgs">Tipo de argumentos del evento</typeparam>
    public class WeakEventManager<TEventArgs>
    {
        
        private readonly List<WeakEventSubscription> _subscriptions = new List<WeakEventSubscription>();
        private readonly object _lock = new object();
        private readonly string _eventName;
        
        
        
        /// <summary>
        /// Constructor para crear un manager de eventos específico
        /// </summary>
        /// <param name="eventName">Nombre del evento</param>
        public WeakEventManager(string eventName)
        {
            _eventName = eventName ?? "UnnamedEvent";
        }
        
        
        
        /// <summary>
        /// Suscribe un handler usando WeakReference
        /// </summary>
        /// <param name="target">Objeto target del handler</param>
        /// <param name="handler">Handler a ejecutar</param>
        /// <param name="description">Descripción del evento</param>
        public void Subscribe(object target, Action<TEventArgs> handler, string description = "")
        {
            if (target == null || handler == null)
            {
                return;
            }
            
            lock (_lock)
            {
                // Verificar si ya existe una suscripción para este target
                for (int i = 0; i < _subscriptions.Count; i++)
                {
                    var existing = _subscriptions[i];
                    if (existing.WeakTarget.IsAlive && existing.WeakTarget.Target == target)
                    {
                        return;
                    }
                }
                
                var subscription = new WeakEventSubscription(target, handler, description);
                _subscriptions.Add(subscription);
                
            }
        }
        
        /// <summary>
        /// Desuscribe un target específico
        /// </summary>
        /// <param name="target">Target a desuscribir</param>
        /// <returns>True si se removió alguna suscripción</returns>
        public bool Unsubscribe(object target)
        {
            if (target == null) return false;
            
            lock (_lock)
            {
                int removedCount = 0;
                for (int i = _subscriptions.Count - 1; i >= 0; i--)
                {
                    var subscription = _subscriptions[i];
                    if (subscription.WeakTarget.IsAlive && subscription.WeakTarget.Target == target)
                    {
                        _subscriptions.RemoveAt(i);
                        removedCount++;
                    }
                }
                
                return removedCount > 0;
            }
        }
        
        /// <summary>
        /// Invoca el evento para todos los suscriptores vivos
        /// </summary>
        /// <param name="eventArgs">Argumentos del evento</param>
        public void Invoke(TEventArgs eventArgs)
        {
            List<WeakEventSubscription> aliveSubscriptions;
            
            lock (_lock)
            {
                // Limpiar suscripciones muertas y obtener las vivas
                CleanupDeadSubscriptions();
                aliveSubscriptions = new List<WeakEventSubscription>(_subscriptions);
            }
            
            // Ejecutar handlers fuera del lock para evitar deadlocks
            foreach (var subscription in aliveSubscriptions)
            {
                if (subscription.WeakTarget.IsAlive)
                {
                    subscription.Handler.Invoke(eventArgs);
                }
            }
        }
        
        /// <summary>
        /// Limpia todas las suscripciones
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
            }
        }
        
        
        
        
        
        
        
        /// <summary>
        /// Limpia suscripciones a objetos que han sido garbage collected
        /// </summary>
        private void CleanupDeadSubscriptions()
        {
            _subscriptions.RemoveAll(sub => !sub.WeakTarget.IsAlive);
        }
        
        
        
        /// <summary>
        /// Representa una suscripción individual usando WeakReference
        /// </summary>
        private class WeakEventSubscription
        {
            public WeakReference WeakTarget { get; }
            public Action<TEventArgs> Handler { get; }
            public string Description { get; }
            
            public WeakEventSubscription(object target, Action<TEventArgs> handler, string description)
            {
                WeakTarget = new WeakReference(target);
                Handler = handler;
                Description = description;
            }
        }
        
    }
    
    
    /// <summary>
    /// Factory para crear WeakEventManagers comunes
    /// </summary>
    public static class WeakEventFactory
    {
        /// <summary>
        /// Crea un WeakEventManager para el patrón PreviewManager
        /// </summary>
        public static class PreviewEvents
        {
            public static WeakEventManager<(IPreviewable previous, IPreviewable current)> CreatePreviewChangedManager()
            {
                return new WeakEventManager<(IPreviewable, IPreviewable)>("PreviewChanged");
            }
            
            public static WeakEventManager<IPreviewable> CreatePreviewActivatedManager()
            {
                return new WeakEventManager<IPreviewable>("PreviewActivated");
            }
            
            public static WeakEventManager<IPreviewable> CreatePreviewDeactivatedManager()
            {
                return new WeakEventManager<IPreviewable>("PreviewDeactivated");
            }
        }
        
        /// <summary>
        /// Crea un WeakEventManager para servicios
        /// </summary>
        public static class ServiceEvents
        {
            public static WeakEventManager<string> CreateServiceInitializedManager()
            {
                return new WeakEventManager<string>("ServiceInitialized");
            }
            
            public static WeakEventManager<object> CreateServicesInvalidatedManager()
            {
                return new WeakEventManager<object>("ServicesInvalidated");
            }
        }
    }
    
    /// <summary>
    /// Interfaz para objetos que pueden ser notificados vía WeakEventManager
    /// </summary>
    public interface IWeakEventTarget
    {
        /// <summary>
        /// Identifier único para este target
        /// </summary>
        string GetEventTargetId();
    }
}
