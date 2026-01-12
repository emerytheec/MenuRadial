using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;
using Bender_Dios.MenuRadial.AnimationSystem.Interfaces;
using Bender_Dios.MenuRadial.AnimationSystem.Services;

namespace Bender_Dios.MenuRadial.Core.Services
{
    /// <summary>
    /// Punto de bootstrap centralizado para servicios del sistema MenuRadial
    /// Simplificado para ejecución single-thread en Editor Unity
    /// </summary>
    public static class MenuRadialServiceBootstrap
    {
        
        private static readonly Dictionary<Type, object> _singletonServices = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private static bool _isInitialized = false;
        
        
        
        /// <summary>
        /// Indica si el sistema está inicializado
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        
        
        /// <summary>
        /// Inicializa todos los servicios del sistema MenuRadial
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized) return;
            
            RegisterCoreServices();
            RegisterAnimationServices();
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Fuerza la reinicialización completa del sistema de servicios
        /// </summary>
        public static void ForceReinitialize()
        {
            _isInitialized = false;
            _singletonServices.Clear();
            _factories.Clear();
            
            EnsureInitialized();
        }
        
        /// <summary>
        /// Limpia todos los servicios registrados
        /// </summary>
        public static void Cleanup()
        {
            _singletonServices.Clear();
            _factories.Clear();
            _isInitialized = false;
        }
        
        
        /// <summary>
        /// Limpia todos los servicios (alias para Cleanup para compatibilidad)
        /// </summary>
        public static void ClearServices()
        {
            Cleanup();
        }
        
        
        
        
        /// <summary>
        /// Invalida servicios si es necesario (para compatibilidad con MRUnificarObjetosUIRenderer)
        /// </summary>
        /// <param name="forceReinitialize">Fuerza la reinicialización incluso si está inicializado</param>
        /// <returns>True si se invalidaron servicios</returns>
        public static bool InvalidateServicesIfNeeded(bool forceReinitialize = false)
        {
            if (!_isInitialized)
            {
                // Sistema no inicializado, consideramos que "se invalidó" (necesita inicialización)
                return true;
            }
            
            if (forceReinitialize)
            {
                // Forzar reinicialización: limpiar e inicializar de nuevo
                Cleanup();
                EnsureInitialized();
                return true;
            }
            
            // En esta implementación simplificada, los servicios están siempre válidos una vez inicializados
            // Esta función principalmente existe para compatibilidad con el código anterior
            return false;
        }
        
        
        
        /// <summary>
        /// Registra servicios principales del sistema
        /// </summary>
        private static void RegisterCoreServices()
        {
            // Illumination Services
            RegisterSingleton<IIlluminationMaterialScanner, IlluminationMaterialScanner>();
        }
        
        /// <summary>
        /// Registra servicios de animación especializados
        /// </summary>
        private static void RegisterAnimationServices()
        {
            // Specialized Animation Generators (solo Illumination)
            RegisterSingleton<IIlluminationAnimationGenerator, IlluminationAnimationGenerator>();
        }
        
        /// <summary>
        /// Registra un servicio singleton
        /// </summary>
        private static void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
            where TInterface : class
        {
            _factories[typeof(TInterface)] = () => new TImplementation();
        }
        
        /// <summary>
        /// Registra un servicio público (para compatibilidad con MRServiceInitializer)
        /// </summary>
        public static void RegisterService<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
            where TInterface : class
        {
            RegisterSingleton<TInterface, TImplementation>();
        }
        
        /// <summary>
        /// Registra una instancia de servicio directamente (para compatibilidad con MRServiceInitializer)
        /// </summary>
        public static void RegisterService(object serviceInstance)
        {
            if (serviceInstance == null) return;
            
            var serviceType = serviceInstance.GetType();
            _singletonServices[serviceType] = serviceInstance;
            
            // También registrar por todas las interfaces implementadas
            var interfaces = serviceType.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                _singletonServices[interfaceType] = serviceInstance;
            }
        }
        
        /// <summary>
        /// Registra un servicio con múltiples interfaces específicas (para compatibilidad)
        /// </summary>
        public static void RegisterServiceWithInterfaces(object serviceInstance, params System.Type[] interfaceTypes)
        {
            if (serviceInstance == null || interfaceTypes == null) return;
            
            foreach (var interfaceType in interfaceTypes)
            {
                if (interfaceType != null && interfaceType.IsInterface)
                {
                    _singletonServices[interfaceType] = serviceInstance;
                }
            }
        }
        
        
        
        /// <summary>
        /// Resuelve un servicio de forma segura con logging de errores
        /// </summary>
        /// <typeparam name="T">Tipo de servicio</typeparam>
        /// <returns>Instancia del servicio</returns>
        public static T GetService<T>() where T : class
        {
            EnsureInitialized();
            
            var serviceType = typeof(T);
            
            // Verificar si ya existe una instancia singleton
            if (_singletonServices.TryGetValue(serviceType, out var existingInstance))
            {
                return (T)existingInstance;
            }
            
            // Crear nueva instancia usando factory
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var newInstance = (T)factory();
                _singletonServices[serviceType] = newInstance;
                return newInstance;
            }
            
            throw new InvalidOperationException($"Servicio {typeof(T).Name} no registrado");
        }
        
        /// <summary>
        /// Intenta resolver un servicio sin lanzar excepción
        /// </summary>
        /// <typeparam name="T">Tipo de servicio</typeparam>
        /// <param name="service">Servicio resuelto</param>
        /// <returns>True si se resolvió exitosamente</returns>
        public static bool TryGetService<T>(out T service) where T : class
        {
            // Validación de inicialización sin excepción
            if (!_isInitialized)
            {
                EnsureInitialized();
            }
            
            var serviceType = typeof(T);
            
            // Verificar si ya existe una instancia singleton
            if (_singletonServices.TryGetValue(serviceType, out var existingInstance) && existingInstance is T)
            {
                service = (T)existingInstance;
                return true;
            }
            
            // Crear nueva instancia usando factory
            if (_factories.TryGetValue(serviceType, out var factory) && factory != null)
            {
                var newInstance = factory() as T;
                if (newInstance != null)
                {
                    _singletonServices[serviceType] = newInstance;
                    service = newInstance;
                    return true;
                }
            }
            
            service = null;
            return false;
        }
        
        /// <summary>
        /// Obtiene un servicio o usa el fallback si no está disponible
        /// </summary>
        /// <typeparam name="T">Tipo de servicio</typeparam>
        /// <param name="fallbackFactory">Factory para crear instancia de fallback</param>
        /// <returns>Servicio o fallback</returns>
        public static T GetServiceOrFallback<T>(Func<T> fallbackFactory) where T : class
        {
            if (TryGetService<T>(out var service))
            {
                return service;
            }
            
            // Validación defensiva antes de usar fallback
            if (fallbackFactory != null)
            {
                return fallbackFactory();
            }
            
            return null;
        }
        
    }
}
