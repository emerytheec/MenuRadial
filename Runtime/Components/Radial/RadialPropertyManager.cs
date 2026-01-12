using System;
using System.IO;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Gestor especializado para propiedades y sus cambios en menús radiales
    /// REFACTORIZADO [2025-07-04]: Extraído de MRUnificarObjetos.cs y reorganizado para cumplir SRP
    /// 
    /// Responsabilidad única: Coordinación y gestión de propiedades de animación
    /// - Gestión centralizada de propiedades de animación (nombre, ruta, auto-update)
    /// - Coordinación entre servicios especializados
    /// - API unificada para operaciones de propiedades
    /// 
    /// DELEGACIÓN: Validación → RadialPropertyValidator, Rutas → RadialPathProcessor, 
    /// Notificaciones → RadialPropertyNotifier
    /// </summary>
    public class RadialPropertyManager
    {
        // Campos privados
        private readonly string _componentName;
        
        // Propiedades principales
        private string _animationName;
        private string _animationPath;
        private bool _autoUpdatePaths;
        
        // Servicios especializados (SRP)
        private readonly RadialPropertyValidator _validator;
        private readonly RadialPathProcessor _pathProcessor;
        private readonly RadialPropertyNotifier _notifier;
        
        // Eventos (Delegados a RadialPropertyNotifier)
        /// <summary>
        /// Evento disparado cuando cambia el nombre de la animación
        /// </summary>
        public event System.Action<string> OnAnimationNameChanged
        {
            add => _notifier.OnAnimationNameChanged += value;
            remove => _notifier.OnAnimationNameChanged -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando cambia la ruta de la animación
        /// </summary>
        public event System.Action<string> OnAnimationPathChanged
        {
            add => _notifier.OnAnimationPathChanged += value;
            remove => _notifier.OnAnimationPathChanged -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando cambia el estado de auto-update
        /// </summary>
        public event System.Action<bool> OnAutoUpdatePathsChanged
        {
            add => _notifier.OnAutoUpdatePathsChanged += value;
            remove => _notifier.OnAutoUpdatePathsChanged -= value;
        }
        
        // Constructor
        /// <summary>
        /// Constructor sin dependencias directas para evitar ciclos de inicialización
        /// REFACTORIZADO [2025-07-04]: ServiceCoordinator se conecta post-inicialización
        /// </summary>
        /// <param name="componentName">Nombre del componente para logging</param>
        /// <param name="initialAnimationName">Nombre inicial de la animación</param>
        /// <param name="initialAnimationPath">Ruta inicial de la animación</param>
        /// <param name="initialAutoUpdatePaths">Estado inicial de auto-update</param>
        public RadialPropertyManager(string componentName,
                                   string initialAnimationName = "RadialToggle",
                                   string initialAnimationPath = null,
                                   bool initialAutoUpdatePaths = true)
        {
            _componentName = componentName ?? "Unknown";
            
            // Inicializar servicios especializados sin dependencias externas
            _validator = new RadialPropertyValidator(_componentName);
            _pathProcessor = new RadialPathProcessor();
                _notifier = new RadialPropertyNotifier(_componentName, null); // Sin coordinador inicialmente
            
            // Inicializar propiedades sin disparar eventos
            using (_notifier.SuppressNotifications())
            {
                _animationName = initialAnimationName ?? "RadialToggle";
                _animationPath = _pathProcessor.NormalizePath(initialAnimationPath ?? MRConstants.ANIMATION_OUTPUT_PATH);
                _autoUpdatePaths = initialAutoUpdatePaths;
            }
            
        }
        
        /// <summary>
        /// Conecta el coordinador de servicios post-inicialización para evitar dependencias circulares
        /// REFACTORIZADO [2025-07-04]: Separación de inicialización y conexión de dependencias
        /// </summary>
        /// <param name="serviceCoordinator">Coordinador de servicios a conectar</param>
        public void ConnectServiceCoordinator(object serviceCoordinator)
        {
            if (serviceCoordinator == null)
            {
                return;
            }
            
            // Conectar el coordinador al notificador
            _notifier.SetServiceCoordinator(serviceCoordinator);
            
        }
        
        /// <summary>
        /// Desconecta el coordinador de servicios (útil para cleanup)
        /// </summary>
        public void DisconnectServiceCoordinator()
        {
            _notifier.SetServiceCoordinator(null);
        }
        
        /// <summary>
        /// Cleanup completo del manager incluyendo eventos
        /// NUEVO [2025-07-04]: Previene memory leaks en eventos delegados
        /// </summary>
        public void Cleanup()
        {
            if (_notifier == null) return;
            
            // Desconectar coordinador de servicios
            DisconnectServiceCoordinator();
            
            // El cleanup de eventos se hace implícitamente porque los eventos 
            // están delegados al _notifier, y cuando se destruye el manager
            // se pierde la referencia al notifier
        }
        
        // Propiedades públicas
        
        /// <summary>
        /// Nombre de la animación con validación y notificación
        /// </summary>
        public string AnimationName
        {
            get => _animationName;
            set => SetAnimationName(value);
        }
        
        /// <summary>
        /// Ruta de la animación con validación y notificación
        /// </summary>
        public string AnimationPath
        {
            get => _animationPath;
            set => SetAnimationPath(value);
        }
        
        /// <summary>
        /// Estado de auto-actualización de rutas
        /// </summary>
        public bool AutoUpdatePaths
        {
            get => _autoUpdatePaths;
            set => SetAutoUpdatePaths(value);
        }
        
        /// <summary>
        /// Ruta completa del archivo de animación (solo lectura)
        /// DELEGADO: RadialPathProcessor maneja el cálculo
        /// </summary>
        public string FullAnimationPath => _pathProcessor.CalculateFullAnimationPath(_animationPath, _animationName);
        
        /// <summary>
        /// Nombre del archivo de animación sin extensión (solo lectura)
        /// DELEGADO: RadialPathProcessor maneja la extracción
        /// </summary>
        public string AnimationFileName => _pathProcessor.GetAnimationFileName(_animationName);
        
        /// <summary>
        /// Directorio de la animación normalizado (solo lectura)
        /// DELEGADO: RadialPathProcessor maneja la normalización
        /// </summary>
        public string NormalizedAnimationDirectory => _pathProcessor.GetNormalizedAnimationDirectory(_animationPath);
        
        // Métodos públicos - Gestión de propiedades
        
        /// <summary>
        /// Establece múltiples propiedades en una sola operación (más eficiente)
        /// </summary>
        /// <param name="animationName">Nuevo nombre de animación (null para mantener actual)</param>
        /// <param name="animationPath">Nueva ruta de animación (null para mantener actual)</param>
        /// <param name="autoUpdatePaths">Nuevo estado de auto-update (null para mantener actual)</param>
        public void SetProperties(string animationName = null, string animationPath = null, bool? autoUpdatePaths = null)
        {
            string finalAnimationName = null;
            string finalAnimationPath = null;
            
            using (_notifier.SuppressNotifications())
            {
                // Actualizar nombre si se proporciona
                if (animationName != null && animationName != _animationName)
                {
                    if (_validator.ValidateAnimationName(animationName))
                    {
                        _animationName = animationName;
                        finalAnimationName = animationName;
                    }
                }
                
                // Actualizar ruta si se proporciona
                if (animationPath != null && animationPath != _animationPath)
                {
                    var normalizedPath = _pathProcessor.NormalizePath(animationPath);
                    if (_validator.ValidateAnimationPath(normalizedPath))
                    {
                        _animationPath = normalizedPath;
                        finalAnimationPath = normalizedPath;
                    }
                }
                
                // Actualizar auto-update si se proporciona
                if (autoUpdatePaths.HasValue && autoUpdatePaths.Value != _autoUpdatePaths)
                {
                    _autoUpdatePaths = autoUpdatePaths.Value;
                }
            }
            
            // Notificar cambios en lote
            _notifier.NotifyBatchChanges(finalAnimationName, finalAnimationPath, autoUpdatePaths);
        }
        
        /// <summary>
        /// Restaura las propiedades a valores por defecto
        /// </summary>
        public void ResetToDefaults()
        {
            SetProperties(
                animationName: "RadialToggle",
                animationPath: MRConstants.ANIMATION_OUTPUT_PATH,
                autoUpdatePaths: true
            );
            
        }
        
        /// <summary>
        /// Auto-actualiza la ruta basada en la jerarquía del GameObject (si está habilitado)
        /// DELEGADO: RadialPathProcessor maneja la generación de rutas
        /// </summary>
        /// <param name="gameObjectPath">Ruta del GameObject en la jerarquía</param>
        public void AutoUpdatePathFromHierarchy(string gameObjectPath)
        {
            if (!_autoUpdatePaths || string.IsNullOrEmpty(gameObjectPath))
                return;
                
            if (_pathProcessor == null) throw new InvalidOperationException("PathProcessor no ha sido inicializado");
            
            // Usar el procesador de rutas para generar automáticamente
            var newPath = _pathProcessor.AutoGeneratePathFromHierarchy(gameObjectPath, _animationPath);
            
            if (newPath != null)
            {
                AnimationPath = newPath;
            }
        }
        
        // Métodos públicos - Validación e información
        
        /// <summary>
        /// Valida todas las propiedades actuales
        /// DELEGADO: RadialPropertyValidator maneja la validación
        /// </summary>
        /// <returns>True si todas las propiedades son válidas</returns>
        public bool ValidateAllProperties()
        {
            return _validator.ValidateAllProperties(_animationName, _animationPath);
        }
        
        
        /// <summary>
        /// Obtiene un resumen de las propiedades para UI
        /// </summary>
        /// <returns>String con resumen legible</returns>
        public string GetPropertiesSummary()
        {
            var exists = "❓"; // Sistema de archivos no disponible
            return $"'{AnimationFileName}' en '{NormalizedAnimationDirectory}' {exists} " +
                   $"(Auto-update: {(_autoUpdatePaths ? "ON" : "OFF")})";
        }
        
        // Métodos privados - Setters de propiedades
        
        /// <summary>
        /// Establece el nombre de la animación con validación
        /// DELEGADO: RadialPropertyValidator maneja validación, RadialPropertyNotifier maneja notificaciones
        /// </summary>
        /// <param name="value">Nuevo nombre</param>
        private void SetAnimationName(string value)
        {
            if (value == _animationName)
                return;
                
            if (_validator.ValidateAnimationName(value))
            {
                _animationName = value;
                _notifier.NotifyAnimationNameChanged(_animationName, _animationPath);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Establece la ruta de la animación con validación y normalización
        /// DELEGADO: RadialPathProcessor normaliza, RadialPropertyValidator valida, RadialPropertyNotifier notifica
        /// </summary>
        /// <param name="value">Nueva ruta</param>
        private void SetAnimationPath(string value)
        {
            var normalizedValue = _pathProcessor.NormalizePath(value);
            
            if (normalizedValue == _animationPath)
                return;
                
            if (_validator.ValidateAnimationPath(normalizedValue))
            {
                _animationPath = normalizedValue;
                _notifier.NotifyAnimationPathChanged(_animationName, _animationPath);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Establece el estado de auto-actualización
        /// DELEGADO: RadialPropertyNotifier maneja las notificaciones
        /// </summary>
        /// <param name="value">Nuevo estado</param>
        private void SetAutoUpdatePaths(bool value)
        {
            if (value == _autoUpdatePaths)
                return;
                
            _autoUpdatePaths = value;
            
            
            _notifier.NotifyAutoUpdatePathsChanged(_autoUpdatePaths);
        }
        
        // Métodos públicos - Operaciones delegadas
        
        
        /// <summary>
        /// Genera un nombre único basado en el nombre actual
        /// DELEGADO: RadialPathProcessor maneja la generación
        /// </summary>
        /// <param name="suffix">Sufijo opcional (por defecto usa timestamp)</param>
        /// <returns>Nuevo nombre único</returns>
        public string GenerateUniqueName(string suffix = null)
        {
            return _pathProcessor.GenerateUniqueName(_animationName, suffix);
        }
        
        /// <summary>
        /// Crea una ruta sugerida basada en la jerarquía del GameObject
        /// DELEGADO: RadialPathProcessor maneja las sugerencias
        /// </summary>
        /// <param name="gameObjectPath">Ruta del GameObject en la jerarquía</param>
        /// <returns>Ruta sugerida</returns>
        public string SuggestPathFromHierarchy(string gameObjectPath)
        {
            return _pathProcessor.SuggestPathFromHierarchy(gameObjectPath);
        }
        
        /// <summary>
        /// Verifica si la ruta actual existe en el sistema de archivos
        /// Verificaciones de sistema de archivos
        /// </summary>
        /// <returns>True si el directorio existe</returns>
        public bool DoesPathExist()
        {
            return false; // Sistema de archivos no disponible
        }
        
        /// <summary>
        /// Obtiene estadísticas de archivos en la ruta actual
        /// Estadísticas de sistema de archivos
        /// </summary>
        /// <returns>String con estadísticas</returns>
        public string GetPathStatistics()
        {
            return "Sistema de archivos no disponible";
        }
    }
}
