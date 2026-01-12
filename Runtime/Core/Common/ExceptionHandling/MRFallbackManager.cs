using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Common.ExceptionHandling
{
    /// <summary>
    /// Gestor centralizado de fallbacks para el proyecto MenuRadial
    /// Proporciona mecanismos de recuperación ante fallos en servicios críticos
    /// </summary>
    public static class MRFallbackManager
    {
        
        private static readonly Dictionary<Type, object> _fallbackInstances = new Dictionary<Type, object>();
        private static readonly Dictionary<string, Func<object>> _fallbackFactories = new Dictionary<string, Func<object>>();
        private static readonly object _fallbackLock = new object();
        
        
        
        /// <summary>
        /// Registra una factory de fallback para un tipo específico
        /// </summary>
        /// <typeparam name="T">Tipo del servicio</typeparam>
        /// <param name="fallbackFactory">Factory que crea la instancia de fallback</param>
        public static void RegisterFallback<T>(Func<T> fallbackFactory) where T : class
        {
            if (fallbackFactory == null)
            {
                return;
            }
            
            lock (_fallbackLock)
            {
                var key = typeof(T).FullName;
                _fallbackFactories[key] = () => fallbackFactory();
            }
        }
        
        /// <summary>
        /// Obtiene o crea una instancia de fallback para un tipo específico
        /// </summary>
        /// <typeparam name="T">Tipo del servicio</typeparam>
        /// <returns>Instancia de fallback o null si no está registrada</returns>
        public static T GetFallback<T>() where T : class
        {
            lock (_fallbackLock)
            {
                var type = typeof(T);
                
                // Verificar si ya existe una instancia
                if (_fallbackInstances.TryGetValue(type, out object existingInstance))
                {
                    return existingInstance as T;
                }
                
                // Intentar crear nueva instancia usando factory
                var key = type.FullName;
                if (_fallbackFactories.TryGetValue(key, out Func<object> factory))
                {
                    var instance = factory() as T;
                    if (instance != null)
                    {
                        _fallbackInstances[type] = instance;
                        return instance;
                    }
                }
                
                // Intentar crear instancia por defecto si es posible
                return CreateDefaultFallback<T>();
            }
        }
        
        /// <summary>
        /// Verifica si existe un fallback registrado para un tipo
        /// </summary>
        /// <typeparam name="T">Tipo a verificar</typeparam>
        /// <returns>True si existe fallback</returns>
        public static bool HasFallback<T>()
        {
            lock (_fallbackLock)
            {
                var key = typeof(T).FullName;
                return _fallbackFactories.ContainsKey(key) || _fallbackInstances.ContainsKey(typeof(T));
            }
        }
        
        /// <summary>
        /// Limpia una instancia de fallback específica
        /// </summary>
        /// <typeparam name="T">Tipo a limpiar</typeparam>
        public static void ClearFallback<T>()
        {
            lock (_fallbackLock)
            {
                var type = typeof(T);
                _fallbackInstances.Remove(type);
            }
        }
        
        /// <summary>
        /// Limpia todas las instancias de fallback
        /// </summary>
        public static void ClearAllFallbacks()
        {
            lock (_fallbackLock)
            {
_fallbackInstances.Clear();
            }
        }
        
        
        
        
        
        
        
        /// <summary>
        /// Guarda el estado actual de un objeto para recuperación posterior
        /// </summary>
        /// <param name="key">Clave para identificar el estado</param>
        /// <param name="state">Estado a guardar</param>
        public static void SaveState(string key, object state)
        {
if (string.IsNullOrEmpty(key))
            {
                return;
            }
            
            lock (_fallbackLock)
            {
                if (!_fallbackInstances.ContainsKey(typeof(StateContainer)))
                {
                    _fallbackInstances[typeof(StateContainer)] = new StateContainer();
                }
                
                var container = _fallbackInstances[typeof(StateContainer)] as StateContainer;
                container?.SaveState(key, state);
            }
        }
        
        /// <summary>
        /// Recupera un estado guardado previamente
        /// </summary>
        /// <typeparam name="T">Tipo del estado</typeparam>
        /// <param name="key">Clave del estado</param>
        /// <returns>Estado recuperado o default si no existe</returns>
        public static T RestoreState<T>(string key)
        {
if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }
            
            lock (_fallbackLock)
            {
                if (_fallbackInstances.TryGetValue(typeof(StateContainer), out object containerObj))
                {
                    var container = containerObj as StateContainer;
                    if (container != null)
                    {
                        var state = container.RestoreState<T>(key);
                        
                        
                        return state != null ? state : default(T);
                    }
                }
                
                return default(T);
            }
        }
        
        /// <summary>
        /// Verifica si existe un estado guardado para una clave
        /// </summary>
        /// <param name="key">Clave a verificar</param>
        /// <returns>True si existe estado guardado</returns>
        public static bool HasSavedState(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            
            lock (_fallbackLock)
            {
                if (_fallbackInstances.TryGetValue(typeof(StateContainer), out object containerObj))
                {
                    var container = containerObj as StateContainer;
                    return container?.HasState(key) ?? false;
                }
                
                return false;
            }
        }
        
        
        
        /// <summary>
        /// Intenta crear un fallback por defecto para tipos conocidos
        /// </summary>
        /// <typeparam name="T">Tipo del fallback</typeparam>
        /// <returns>Instancia de fallback o null</returns>
        private static T CreateDefaultFallback<T>() where T : class
        {
            var type = typeof(T);
            
            // Para interfaces conocidas, devolver implementaciones básicas
            if (type.IsInterface)
            {
                return null;
            }
            
            // Para clases concretas, intentar constructor por defecto
            if (type.IsClass && !type.IsAbstract)
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var instance = Activator.CreateInstance(type) as T;
                    return instance;
                }
            }
            
            return null;
        }
        
        
        
        
    }
    
    
    /// <summary>
    /// Contenedor para gestionar estados guardados
    /// </summary>
    internal class StateContainer
    {
        private readonly Dictionary<string, object> _states = new Dictionary<string, object>();
        private readonly object _stateLock = new object();
        
        public void SaveState(string key, object state)
        {
            lock (_stateLock)
            {
                _states[key] = state;
            }
        }
        
        public T RestoreState<T>(string key)
        {
            lock (_stateLock)
            {
                if (_states.TryGetValue(key, out object state))
                {
                    return state is T ? (T)state : default(T);
                }
                return default(T);
            }
        }
        
        public bool HasState(string key)
        {
            lock (_stateLock)
            {
                return _states.ContainsKey(key);
            }
        }
        
        public void ClearState(string key)
        {
            lock (_stateLock)
            {
                _states.Remove(key);
            }
        }
        
        public void ClearAllStates()
        {
            lock (_stateLock)
            {
                _states.Clear();
            }
        }
    }
    
}
