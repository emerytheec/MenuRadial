using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Manager para automatizar el cleanup de suscripciones de eventos
    /// NUEVO [2025-07-04]: Previene memory leaks por eventos sin cleanup
    /// </summary>
    public class EventSubscriptionManager : IDisposable
    {
        
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private readonly string _ownerName;
        private bool _disposed = false;
        
        
        
        /// <summary>
        /// Constructor que crea un manager para un componente específico
        /// </summary>
        /// <param name="ownerName">Nombre del componente propietario</param>
        public EventSubscriptionManager(string ownerName)
        {
            _ownerName = ownerName ?? "Unknown";
        }
        
        /// <summary>
        /// Destructor que asegura cleanup automático
        /// </summary>
        ~EventSubscriptionManager()
        {
            Dispose(false);
        }
        
        
        
        /// <summary>
        /// Registra una suscripción para cleanup automático
        /// </summary>
        /// <param name="subscribe">Acción para suscribirse</param>
        /// <param name="unsubscribe">Acción para desuscribirse</param>
        /// <param name="description">Descripción del evento</param>
        public void RegisterSubscription(Action subscribe, Action unsubscribe, string description = "")
        {
            if (subscribe == null || unsubscribe == null)
            {
                return;
            }
            
            var subscription = new EventSubscription(subscribe, unsubscribe, description);
            _subscriptions.Add(subscription);
            
            // Ejecutar suscripción inmediatamente
            subscribe.Invoke();
            subscription.IsSubscribed = true;
        }
        
        /// <summary>
        /// Método helper para suscripciones de Action<T> usando delegate tracking
        /// NOTA: Los parámetros ref no funcionan en lambdas, usar RegisterSubscription directamente
        /// </summary>
        public void Subscribe<T>(Action<T> handler, Action<Action<T>> subscribeAction, Action<Action<T>> unsubscribeAction, string description = "")
        {
            RegisterSubscription(
                () => subscribeAction(handler),
                () => unsubscribeAction(handler),
                description
            );
        }
        
        /// <summary>
        /// Método helper para suscripciones de Action<T1, T2> usando delegate tracking
        /// NOTA: Los parámetros ref no funcionan en lambdas, usar RegisterSubscription directamente
        /// </summary>
        public void Subscribe<T1, T2>(Action<T1, T2> handler, Action<Action<T1, T2>> subscribeAction, Action<Action<T1, T2>> unsubscribeAction, string description = "")
        {
            RegisterSubscription(
                () => subscribeAction(handler),
                () => unsubscribeAction(handler),
                description
            );
        }
        
        /// <summary>
        /// Método helper para suscripciones de Action sin parámetros usando delegate tracking
        /// NOTA: Los parámetros ref no funcionan en lambdas, usar RegisterSubscription directamente
        /// </summary>
        public void Subscribe(Action handler, Action<Action> subscribeAction, Action<Action> unsubscribeAction, string description = "")
        {
            RegisterSubscription(
                () => subscribeAction(handler),
                () => unsubscribeAction(handler),
                description
            );
        }
        
        /// <summary>
        /// Desuscribe una suscripción específica por descripción
        /// </summary>
        public bool UnsubscribeByDescription(string description)
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                var subscription = _subscriptions[i];
                if (subscription.Description == description && subscription.IsSubscribed)
                {
                    subscription.Unsubscribe.Invoke();
                    subscription.IsSubscribed = false;
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Obtiene estadísticas de las suscripciones
        /// </summary>
        public string GetSubscriptionStats()
        {
            int activeCount = 0;
            int totalCount = _subscriptions.Count;
            
            foreach (var sub in _subscriptions)
            {
                if (sub.IsSubscribed) activeCount++;
            }
            
            return $"Suscripciones para '{_ownerName}': {activeCount}/{totalCount} activas";
        }
        
        /// <summary>
        /// Lista todas las suscripciones
        /// </summary>
        public List<string> GetSubscriptionList()
        {
            var list = new List<string>();
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                var sub = _subscriptions[i];
                string status = sub.IsSubscribed ? "ACTIVA" : "Inactiva";
                list.Add($"[{i}] {sub.Description} - {status}");
            }
            return list;
        }
        
        
        
        /// <summary>
        /// Limpia todas las suscripciones activas
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Implementación del patrón Dispose
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupAllSubscriptions();
                }
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Limpia todas las suscripciones registradas
        /// </summary>
        private void CleanupAllSubscriptions()
        {
            foreach (var subscription in _subscriptions)
            {
                if (subscription.IsSubscribed)
                {
                    subscription.Unsubscribe.Invoke();
                    subscription.IsSubscribed = false;
                }
            }

            _subscriptions.Clear();
        }
        
        
        
        /// <summary>
        /// Representa una suscripción individual a un evento
        /// </summary>
        private class EventSubscription
        {
            public Action Subscribe { get; }
            public Action Unsubscribe { get; }
            public string Description { get; }
            public bool IsSubscribed { get; set; }
            
            public EventSubscription(Action subscribe, Action unsubscribe, string description)
            {
                Subscribe = subscribe;
                Unsubscribe = unsubscribe;
                Description = description;
                IsSubscribed = false;
            }
        }
        
    }
    
    /// <summary>
    /// Extensiones para facilitar el uso de EventSubscriptionManager
    /// </summary>
    public static class EventSubscriptionExtensions
    {
        /// <summary>
        /// Crea un EventSubscriptionManager para un MonoBehaviour
        /// </summary>
        public static EventSubscriptionManager CreateEventManager(this MonoBehaviour monoBehaviour)
        {
            return new EventSubscriptionManager(monoBehaviour.name);
        }
        
        /// <summary>
        /// Crea un EventSubscriptionManager con nombre personalizado
        /// </summary>
        public static EventSubscriptionManager CreateEventManager(this object obj, string customName = null)
        {
            string name = customName ?? obj.GetType().Name;
            return new EventSubscriptionManager(name);
        }
    }
}
