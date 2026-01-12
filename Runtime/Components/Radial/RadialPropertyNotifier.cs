using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Notificador especializado para cambios en propiedades de animación radial
    /// REFACTORIZACIÓN [2025-07-04]: Extraído de RadialPropertyManager para cumplir SRP
    /// 
    /// Responsabilidad única: Gestión de eventos y notificaciones de cambios de propiedades
    /// </summary>
    public class RadialPropertyNotifier
    {
        // Campos privados
        
        private readonly string _componentName;
        private object _serviceCoordinator;
        private bool _suppressChangeNotifications = false;
        
        // Eventos
        
        /// <summary>
        /// Evento disparado cuando cambia el nombre de la animación
        /// </summary>
        public event System.Action<string> OnAnimationNameChanged;
        
        /// <summary>
        /// Evento disparado cuando cambia la ruta de la animación
        /// </summary>
        public event System.Action<string> OnAnimationPathChanged;
        
        /// <summary>
        /// Evento disparado cuando cambia el estado de auto-update
        /// </summary>
        public event System.Action<bool> OnAutoUpdatePathsChanged;
        
        /// <summary>
        /// Evento disparado cuando cambian propiedades críticas
        /// </summary>
        public event System.Action<string, string> OnCriticalPropertiesChanged;
        
        // Constructor
        
        /// <summary>
        /// Inicializa el notificador con dependencias necesarias
        /// </summary>
        /// <param name="componentName">Nombre del componente para logging</param>
        /// <param name="serviceCoordinator">Coordinador de servicios para notificar cambios</param>
        public RadialPropertyNotifier(string componentName, object serviceCoordinator)
        {
            _componentName = componentName ?? "Unknown";
            _serviceCoordinator = serviceCoordinator; // Puede ser null para testing
        }
        
        /// <summary>
        /// Establece o actualiza el coordinador de servicios
        /// REFACTORIZADO [2025-07-04]: Permite conexión post-inicialización para evitar dependencias circulares
        /// </summary>
        /// <param name="serviceCoordinator">Coordinador de servicios (puede ser null)</param>
        public void SetServiceCoordinator(object serviceCoordinator)
        {
            _serviceCoordinator = serviceCoordinator;
            
            if (serviceCoordinator != null)
            {
            }
            else
            {
            }
        }
        
        // Propiedades públicas
        
        /// <summary>
        /// Indica si las notificaciones están suprimidas
        /// </summary>
        public bool NotificationsSuppressed => _suppressChangeNotifications;
        
        // Métodos públicos - Control de notificaciones
        
        /// <summary>
        /// Suprime temporalmente las notificaciones (útil para actualizaciones en lote)
        /// </summary>
        /// <returns>IDisposable que restaura las notificaciones al hacer dispose</returns>
        public IDisposable SuppressNotifications()
        {
            return new NotificationSuppressionScope(this);
        }
        
        /// <summary>
        /// Habilita o deshabilita las notificaciones
        /// </summary>
        /// <param name="suppress">True para suprimir, false para habilitar</param>
        public void SetNotificationSuppression(bool suppress)
        {
            _suppressChangeNotifications = suppress;
        }
        
        // Métodos públicos - Disparar eventos
        
        /// <summary>
        /// Notifica cambio en el nombre de la animación
        /// </summary>
        /// <param name="newAnimationName">Nuevo nombre de la animación</param>
        /// <param name="animationPath">Ruta actual de la animación</param>
        public void NotifyAnimationNameChanged(string newAnimationName, string animationPath)
        {
            if (_suppressChangeNotifications)
            {
                return;
            }
            
            OnAnimationNameChanged?.Invoke(newAnimationName);
            NotifyCriticalPropertiesChanged(newAnimationName, animationPath);
        }
        
        /// <summary>
        /// Notifica cambio en la ruta de la animación
        /// </summary>
        /// <param name="animationName">Nombre actual de la animación</param>
        /// <param name="newAnimationPath">Nueva ruta de la animación</param>
        public void NotifyAnimationPathChanged(string animationName, string newAnimationPath)
        {
            if (_suppressChangeNotifications)
            {
                return;
            }
            
            OnAnimationPathChanged?.Invoke(newAnimationPath);
            NotifyCriticalPropertiesChanged(animationName, newAnimationPath);
        }
        
        /// <summary>
        /// Notifica cambio en auto-update paths
        /// </summary>
        /// <param name="newAutoUpdateValue">Nuevo valor de auto-update</param>
        public void NotifyAutoUpdatePathsChanged(bool newAutoUpdateValue)
        {
            if (_suppressChangeNotifications)
            {
                return;
            }
            
            OnAutoUpdatePathsChanged?.Invoke(newAutoUpdateValue);
        }
        
        /// <summary>
        /// Notifica cambios en propiedades críticas (nombre y ruta)
        /// </summary>
        /// <param name="animationName">Nombre actual de la animación</param>
        /// <param name="animationPath">Ruta actual de la animación</param>
        public void NotifyCriticalPropertiesChanged(string animationName, string animationPath)
        {
            if (_suppressChangeNotifications)
            {
                return;
            }
            
            // Notificar a suscriptores del evento
            OnCriticalPropertiesChanged?.Invoke(animationName, animationPath);
            
        }
        
        
        /// <summary>
        /// Notifica múltiples cambios en una sola operación (más eficiente)
        /// </summary>
        /// <param name="animationName">Nombre de la animación (null si no cambió)</param>
        /// <param name="animationPath">Ruta de la animación (null si no cambió)</param>
        /// <param name="autoUpdatePaths">Estado de auto-update (null si no cambió)</param>
        public void NotifyBatchChanges(string animationName = null, string animationPath = null, bool? autoUpdatePaths = null)
        {
            if (_suppressChangeNotifications)
            {
                return;
            }
            
            bool hasCriticalChanges = false;
            
            // Notificar cambios individuales
            if (animationName != null)
            {
                OnAnimationNameChanged?.Invoke(animationName);
                hasCriticalChanges = true;
            }
            
            if (animationPath != null)
            {
                OnAnimationPathChanged?.Invoke(animationPath);
                hasCriticalChanges = true;
            }
            
            if (autoUpdatePaths.HasValue)
            {
                OnAutoUpdatePathsChanged?.Invoke(autoUpdatePaths.Value);
            }
            
            // Notificar cambios críticos una sola vez
            if (hasCriticalChanges)
            {
                var finalName = animationName ?? "unchanged";
                var finalPath = animationPath ?? "unchanged";
                
                OnCriticalPropertiesChanged?.Invoke(finalName, finalPath);
                
            }
        }
        
        
        
        
        /// <summary>
        /// Clase para supresión temporal de notificaciones usando patrón RAII
        /// </summary>
        private class NotificationSuppressionScope : IDisposable
        {
            private readonly RadialPropertyNotifier _notifier;
            private readonly bool _previousState;
            
            public NotificationSuppressionScope(RadialPropertyNotifier notifier)
            {
                _notifier = notifier;
                _previousState = notifier._suppressChangeNotifications;
                notifier._suppressChangeNotifications = true;
            }
            
            public void Dispose()
            {
                _notifier._suppressChangeNotifications = _previousState;
            }
        }
    }
}
