using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Services
{
    /// <summary>
    /// Inicializador automático de servicios usando reflexión
    /// Registra todos los servicios marcados con MRServiceAttribute
    /// </summary>
    public static class MRServiceInitializer
    {
        
        private static bool _isInitialized = false;
        
        
        
        /// <summary>
        /// Asegura que todos los servicios están inicializados
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized) return;
            
            RegisterServicesViaReflection();
            _isInitialized = true;
        }
        
        /// <summary>
        /// Fuerza la reinicialización de todos los servicios
        /// </summary>
        public static void ForceReinitialize()
        {
            _isInitialized = false;
            MenuRadialServiceBootstrap.ClearServices();
            EnsureInitialized();
        }
        
        
        
        /// <summary>
        /// Registra servicios automáticamente usando reflexión
        /// </summary>
        private static void RegisterServicesViaReflection()
        {
            
            // Validación defensiva sin try-catch silencioso
            var assembly = typeof(MRServiceInitializer).Assembly;
            if (assembly != null)
            {
                // Obtener todos los tipos del assembly
                var allTypes = assembly.GetTypes();
                if (allTypes != null)
                {
                    // Filtrar tipos marcados con MRServiceAttribute
                    var serviceTypes = allTypes
                        .Where(t => t.GetCustomAttribute<MRServiceAttribute>() != null)
                        .Where(t => !t.IsAbstract && !t.IsInterface)
                        .ToArray();
                        
                    
                    // Listar los tipos encontrados
                    foreach (var type in serviceTypes)
                    {
                        if (type != null)
                        {
                            var attribute = type.GetCustomAttribute<MRServiceAttribute>();
                        }
                    }
                    
                    foreach (var type in serviceTypes)
                    {
                        if (type != null)
                        {
                            RegisterServiceWithInterfaces(type);
                        }
                    }
                }
            }
            
        }
        
        /// <summary>
        /// Registra un servicio por su clase y todas sus interfaces especificadas
        /// </summary>
        /// <param name="serviceType">Tipo del servicio</param>
        private static void RegisterServiceWithInterfaces(Type serviceType)
        {
            var attribute = serviceType.GetCustomAttribute<MRServiceAttribute>();
            if (attribute == null) 
            {
                return;
            }
            
            
            // Crear instancia con validación defensiva
            if (!serviceType.IsClass || serviceType.IsAbstract || !serviceType.GetConstructors().Any(c => c.GetParameters().Length == 0))
            {
                return;
            }
            
            var instance = Activator.CreateInstance(serviceType);
            
            if (instance == null)
            {
                return;
            }
            
            // Registrar por el tipo de la clase
            MenuRadialServiceBootstrap.RegisterService(instance);
            
            // Registrar por interfaces especificadas
            if (attribute.ServiceInterfaces != null && attribute.ServiceInterfaces.Length > 0)
            {
                MenuRadialServiceBootstrap.RegisterServiceWithInterfaces(instance, attribute.ServiceInterfaces);
            }
            else
            {
                // Si no se especificaron interfaces, usar todas las interfaces implementadas
                var interfaces = serviceType.GetInterfaces();
                if (interfaces.Length > 0)
                {
                    MenuRadialServiceBootstrap.RegisterServiceWithInterfaces(instance, interfaces);
                }
            }
            
        }
        
    }
}
