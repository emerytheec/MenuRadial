using System;
using System.Text;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Gestor especializado para la integración con Unity en menús radiales
    /// REFACTORIZADO: Extraído de MRUnificarObjetos.cs para responsabilidad única
    /// 
    /// Responsabilidades:
    /// - Unity lifecycle events (Awake, Start, OnDestroy)
    /// - Context menus
    /// - Serialization helpers y validación
    /// - OnValidate() y Reset()
    /// - Integración con el editor de Unity
    /// </summary>
    public class RadialUnityIntegration
    {
        // Campos privados
        private readonly RadialFrameManager _frameManager;
        private readonly object _serviceCoordinator;
        private readonly RadialPropertyManager _propertyManager;
        private readonly RadialPreviewManager _previewManager;
        private readonly string _componentName;
        private readonly MonoBehaviour _ownerComponent;
        
        // Estado de inicialización Unity
        private bool _isAwakeCompleted = false;
        private bool _isStartCompleted = false;
        private bool _isDestroyInProgress = false;
        
        // Constructor
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameManager">Gestor de frames</param>
        /// <param name="serviceCoordinator">Coordinador de servicios</param>
        /// <param name="propertyManager">Gestor de propiedades</param>
        /// <param name="previewManager">Gestor de preview</param>
        /// <param name="ownerComponent">Componente MonoBehaviour propietario</param>
        public RadialUnityIntegration(RadialFrameManager frameManager,
                                    object serviceCoordinator,
                                    RadialPropertyManager propertyManager,
                                    RadialPreviewManager previewManager,
                                    MonoBehaviour ownerComponent)
        {
            _frameManager = frameManager ?? throw new ArgumentNullException(nameof(frameManager));
            _serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
            _propertyManager = propertyManager ?? throw new ArgumentNullException(nameof(propertyManager));
            _previewManager = previewManager ?? throw new ArgumentNullException(nameof(previewManager));
            _ownerComponent = ownerComponent ?? throw new ArgumentNullException(nameof(ownerComponent));
            
            _componentName = _ownerComponent.name;
            
        }
        
        // Propiedades públicas
        /// <summary>
        /// Indica si el ciclo de vida de Awake ha completado
        /// </summary>
        public bool IsAwakeCompleted => _isAwakeCompleted;
        
        /// <summary>
        /// Indica si el ciclo de vida de Start ha completado
        /// </summary>
        public bool IsStartCompleted => _isStartCompleted;
        
        /// <summary>
        /// Indica si el componente está en proceso de destrucción
        /// </summary>
        public bool IsDestroyInProgress => _isDestroyInProgress;
        
        // Gestión del ciclo de vida de Unity
        
        /// <summary>
        /// Maneja el evento Awake() del componente
        /// </summary>
        public void HandleAwake()
        {
            if (_isAwakeCompleted)
            {
                return;
            }
            
            // 1. Inicializar datos básicos
            InitializeBasicData();
            
            // 2. Validar y limpiar referencias
            ValidateAndCleanReferences();
            
            // 3. Auto-actualizar rutas si está habilitado
            AutoUpdatePathsIfEnabled();
            
            // 4. Registrar eventos internos
            RegisterInternalEvents();
            
            _isAwakeCompleted = true;
        }
        
        /// <summary>
        /// Maneja el evento Start() del componente
        /// </summary>
        public void HandleStart()
        {
            if (_isStartCompleted)
            {
                return;
            }
            
            if (!_isAwakeCompleted)
            {
                HandleAwake(); // Forzar Awake si no se completó
            }
            
            // 1. Inicializar servicios avanzados
            InitializeAdvancedServices();
            
            // 2. Verificar configuración completa
            VerifyCompleteConfiguration();
            
            // 3. Registrar en sistemas globales si es necesario
            RegisterInGlobalSystems();
            
            _isStartCompleted = true;
        }
        
        /// <summary>
        /// Maneja el evento OnDestroy() del componente
        /// </summary>
        public void HandleOnDestroy()
        {
            if (_isDestroyInProgress)
            {
                return;
            }
            
            _isDestroyInProgress = true;
            
            // 1. Desactivar preview si está activo
            CleanupPreviewSystem();
            
            // 2. Limpiar servicios
            CleanupServices();
            
            // 3. Desregistrar eventos
            UnregisterEvents();
            
            // 4. Desregistrar de sistemas globales
            UnregisterFromGlobalSystems();
        }
        
        /// <summary>
        /// Maneja el evento OnValidate() del componente (solo en editor) - OPTIMIZADO
        /// Solo ejecuta limpieza ligera, validación pesada delegada a ValidateIfNeeded
        /// </summary>
        public void HandleOnValidate()
        {
#if UNITY_EDITOR
            if (_isDestroyInProgress)
                return;
                
            // Solo limpieza ligera y rápida
            // La validación pesada se maneja en MRUnificarObjetos.ValidateIfNeeded()
            
            // 1. Auto-actualizar rutas si está habilitado (operación ligera)
            AutoUpdatePathsIfEnabled();
            
            // 2. Limpieza de referencias solo si es crítico
            // CleanupInvalidFrames solo si hay referencias nulas evidentes
#endif
        }
        
        /// <summary>
        /// Maneja el evento Reset() del componente (solo en editor)
        /// </summary>
        public void HandleReset()
        {
#if UNITY_EDITOR
            // 1. Restaurar propiedades a valores por defecto
            _propertyManager.ResetToDefaults();
            
            // 2. Limpiar frames existentes
            _frameManager.ClearAllFrames();
#endif
        }
        
        
        /// <summary>
        /// Valida el componente completo
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public ValidationResult ValidateComponent()
        {
            var result = new ValidationResult { IsValid = true, Message = "" };
            var issues = new System.Collections.Generic.List<string>();
            
            // Validar estado de Unity
            if (!_isAwakeCompleted)
                issues.Add("Awake no completado");
                
            // Validar frames
            if (!_frameManager.HasValidFrames())
                issues.Add("No hay frames válidos");
                
            // Validar propiedades
            if (!_propertyManager.ValidateAllProperties())
                issues.Add("Propiedades inválidas");
                
            
            // Construir resultado
            if (issues.Count > 0)
            {
                result.IsValid = false;
                result.Message = "Problemas encontrados:\n" + string.Join("\n", issues);
            }
            else
            {
                result.Message = "Componente válido y funcionando correctamente";
            }
            
            return result;
        }
        
        // Métodos privados - Implementación del ciclo de vida
        
        /// <summary>
        /// Inicializa datos básicos en Awake
        /// </summary>
        private void InitializeBasicData()
        {
            // Validar que los gestores estén inicializados
            if (_frameManager == null || _serviceCoordinator == null || 
                _propertyManager == null || _previewManager == null)
            {
                throw new InvalidOperationException("Gestores no inicializados correctamente");
            }
            
        }
        
        /// <summary>
        /// Valida y limpia referencias inválidas
        /// </summary>
        private void ValidateAndCleanReferences()
        {
            var removedFrames = _frameManager.CleanupInvalidFrames();
        }
        
        /// <summary>
        /// Auto-actualiza rutas si está habilitado
        /// </summary>
        private void AutoUpdatePathsIfEnabled()
        {
            if (_propertyManager.AutoUpdatePaths)
            {
                var hierarchyPath = GetGameObjectHierarchyPath();
                _propertyManager.AutoUpdatePathFromHierarchy(hierarchyPath);
            }
        }
        
        /// <summary>
        /// Registra eventos internos entre gestores
        /// </summary>
        private void RegisterInternalEvents()
        {
            // Registrar eventos del property manager
            _propertyManager.OnAnimationNameChanged += OnAnimationNameChanged;
            _propertyManager.OnAnimationPathChanged += OnAnimationPathChanged;
            
            // Registrar eventos del service coordinator
            
        }
        
        /// <summary>
        /// Inicializa servicios avanzados en Start
        /// </summary>
        private void InitializeAdvancedServices()
        {
            // Servicios no disponibles
        }
        
        /// <summary>
        /// Verifica que la configuración esté completa
        /// </summary>
        private void VerifyCompleteConfiguration()
        {
            var result = ValidateComponent();
        }
        
        /// <summary>
        /// Registra el componente en sistemas globales si es necesario
        /// </summary>
        private void RegisterInGlobalSystems()
        {
            // Actualmente no hay sistemas globales específicos para registrar
            // Este método está preparado para futuras integraciones
        }
        
        // Métodos privados - Limpieza
        
        /// <summary>
        /// Limpia el sistema de preview
        /// </summary>
        private void CleanupPreviewSystem()
        {
            _previewManager.Cleanup();
        }
        
        /// <summary>
        /// Limpia los servicios
        /// </summary>
        private void CleanupServices()
        {
        }
        
        /// <summary>
        /// Desregistra todos los eventos
        /// </summary>
        private void UnregisterEvents()
        {
            _propertyManager.OnAnimationNameChanged -= OnAnimationNameChanged;
            _propertyManager.OnAnimationPathChanged -= OnAnimationPathChanged;
        }
        
        /// <summary>
        /// Desregistra de sistemas globales
        /// </summary>
        private void UnregisterFromGlobalSystems()
        {
            // Preparado para futuras integraciones con sistemas globales
        }
        
        // Métodos privados - Manejadores de eventos
        
        /// <summary>
        /// Maneja cambios en el nombre de la animación
        /// </summary>
        /// <param name="newName">Nuevo nombre</param>
        private void OnAnimationNameChanged(string newName)
        {
        }
        
        /// <summary>
        /// Maneja cambios en la ruta de la animación
        /// </summary>
        /// <param name="newPath">Nueva ruta</param>
        private void OnAnimationPathChanged(string newPath)
        {
        }
        
        /// <summary>
        /// Maneja la invalidación de servicios
        /// </summary>
        private void OnServicesInvalidated()
        {
        }
        
        // Métodos privados - Utilidades
        
        /// <summary>
        /// Obtiene la ruta del GameObject en la jerarquía
        /// </summary>
        /// <returns>Ruta completa desde la raíz</returns>
        private string GetGameObjectHierarchyPath()
        {
            var path = new StringBuilder();
            var current = _ownerComponent.transform;
            
            while (current != null)
            {
                if (path.Length > 0)
                    path.Insert(0, "/");
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            return path.ToString();
        }
        
        // Métodos públicos - Operaciones manuales
        
        /// <summary>
        /// Fuerza la reinicialización completa del componente
        /// </summary>
        public void ForceReinitialize()
        {
            // 1. Invalidar servicios
            
            // 2. Limpiar y revalidar frames
            _frameManager.CleanupInvalidFrames();
            
            // 3. Validar propiedades
            _propertyManager.ValidateAllProperties();
            
            // 4. Reinicializar servicios
            // Servicios no disponibles
        }
        
    }
}
