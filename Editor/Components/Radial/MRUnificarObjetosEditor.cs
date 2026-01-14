using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.AnimationSystem;

namespace Bender_Dios.MenuRadial.Editor.Components.Radial
{
    /// <summary>
    /// Editor personalizado para MRUnificarObjetos - REFACTORIZADO
    /// Responsabilidad única: Coordinación entre módulos especializados
    /// Sigue principios SOLID definidos en la guía estructural del proyecto
    /// Versión: 1.1 - FIX: Preview persiste al cambiar a componentes no-MR
    /// </summary>
    [CustomEditor(typeof(MRUnificarObjetos))]
    public class MRUnificarObjetosEditor : UnityEditor.Editor
    {

        private MRUnificarObjetos _target;

        // Propiedades serializadas
        private SerializedProperty _activeFrameIndexProp;
        private SerializedProperty _autoUpdatePathsProp;
        private SerializedProperty _framesProp;
        private SerializedProperty _animationNameProp;
        private SerializedProperty _defaultStateIsOnProp;

        // Módulos especializados (Patrón de Delegación)
        private MRUnificarObjetosPreviewManager _previewManager;
        private MRUnificarObjetosReorderableController _reorderableController;
        private MRUnificarObjetosUIRenderer _uiRenderer;

        // Para manejo correcto de selección
        private static bool _isSelectionChangeHandlerRegistered = false;
        private static MRUnificarObjetos _lastActiveRadialMenu = null;
        private static MRUnificarObjetosPreviewManager _lastPreviewManager = null;



        private void OnEnable()
        {
            _target = (MRUnificarObjetos)target;

            // Inicializar propiedades serializadas
            InitializeSerializedProperties();

            // Inicializar módulos especializados
            InitializeModules();

            // Registrar el handler de cambio de selección (una sola vez)
            if (!_isSelectionChangeHandlerRegistered)
            {
                Selection.selectionChanged += OnSelectionChanged;
                _isSelectionChangeHandlerRegistered = true;
            }

            // Guardar referencia al objeto activo actual
            _lastActiveRadialMenu = _target;
            _lastPreviewManager = _previewManager;
        }

        /// <summary>
        /// Limpieza al desactivar el editor.
        /// La lógica de cancelación condicional se maneja en OnSelectionChanged.
        /// </summary>
        private void OnDisable()
        {
            // Actualizar el preview manager para la instancia actual
            if (_target != null && _previewManager != null)
            {
                _lastActiveRadialMenu = _target;
                _lastPreviewManager = _previewManager;
            }
        }

        /// <summary>
        /// Handler para cambios de selección en el Editor.
        /// Se ejecuta DESPUÉS de que la selección cambia, permitiendo verificar
        /// correctamente si el nuevo objeto tiene componentes MR conflictivos.
        /// </summary>
        private static void OnSelectionChanged()
        {
            // Si no hay un RadialMenu anterior o su preview manager, no hay nada que hacer
            if (_lastActiveRadialMenu == null || _lastPreviewManager == null)
            {
                // Actualizar referencia al nuevo objeto si hay uno seleccionado
                UpdateLastActiveRadialMenu();
                return;
            }

            // Verificar si el nuevo objeto seleccionado tiene componentes MR que podrían solaparse
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                // Solo restaurar si se va a otro componente del sistema MR
                // Nota: MRMenuControl está en Assembly-CSharp, usamos búsqueda por nombre
                bool hasConflictingComponent =
                    selectedObject.GetComponent<MRAgruparObjetos>() != null ||
                    selectedObject.GetComponent("MRMenuControl") != null ||
                    selectedObject.GetComponent<MRUnificarObjetos>() != null;

                if (hasConflictingComponent)
                {
                    _lastPreviewManager.RestoreAllObjectsToOriginalState();
                }
                // Si no tiene componentes conflictivos, mantener el preview activo
            }
            else
            {
                // Si no hay nada seleccionado, restaurar estados
                _lastPreviewManager.RestoreAllObjectsToOriginalState();
            }

            // Actualizar referencia al nuevo objeto
            UpdateLastActiveRadialMenu();
        }

        /// <summary>
        /// Actualiza la referencia al último MRUnificarObjetos activo
        /// </summary>
        private static void UpdateLastActiveRadialMenu()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                var radialMenu = selectedObject.GetComponent<MRUnificarObjetos>();
                if (radialMenu != null)
                {
                    _lastActiveRadialMenu = radialMenu;
                    // El preview manager se actualizará cuando se cree la instancia del editor
                }
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Delegar renderizado completo al módulo UI
            _uiRenderer.RenderUI();
            
            
            // Aplicar cambios
            HandlePropertyChanges();
        }
        
        
        
        /// <summary>
        /// Inicializa las propiedades serializadas
        /// </summary>
        private void InitializeSerializedProperties()
        {
            _activeFrameIndexProp = serializedObject.FindProperty("_activeFrameIndex");
            _autoUpdatePathsProp = serializedObject.FindProperty("_autoUpdatePaths");
            _framesProp = serializedObject.FindProperty("_frames");
            _animationNameProp = serializedObject.FindProperty("_animationName");
            _defaultStateIsOnProp = serializedObject.FindProperty("_defaultStateIsOn");

            // Validar que todas las propiedades se encontraron
            ValidateSerializedProperties();
        }
        
        /// <summary>
        /// Valida que las propiedades serializadas se encontraron correctamente
        /// </summary>
        private void ValidateSerializedProperties()
        {
        }
        
        /// <summary>
        /// Inicializa los módulos especializados siguiendo principios de Inyección de Dependencias
        /// </summary>
        private void InitializeModules()
        {
            // Validación previa
            if (_target == null || serializedObject == null || _framesProp == null || _activeFrameIndexProp == null)
            {
                InitializeFallbackModules();
                return;
            }

            // 1. Crear gestor de preview (independiente)
            _previewManager = new MRUnificarObjetosPreviewManager(_target);
            
            // 2. Crear controlador de ReorderableList (depende de preview manager)
            if (_previewManager != null)
            {
                _reorderableController = new MRUnificarObjetosReorderableController(
                    _target,
                    serializedObject,
                    _framesProp,
                    _activeFrameIndexProp,
                    _previewManager
                );
            }
            
            // 3. Crear renderizador de UI (depende de preview manager y controlador)
            if (_previewManager != null && _reorderableController != null)
            {
                _uiRenderer = new MRUnificarObjetosUIRenderer(
                    _target,
                    serializedObject,
                    _activeFrameIndexProp,
                    _autoUpdatePathsProp,
                    _animationNameProp,
                    _defaultStateIsOnProp,
                    _previewManager,
                    _reorderableController
                );
            }
            
            // Si falta algún componente, usar fallback
            if (_previewManager == null || _reorderableController == null || _uiRenderer == null)
            {
                InitializeFallbackModules();
            }
        }
        
        /// <summary>
        /// Inicialización de emergencia si falla la inicialización normal
        /// </summary>
        private void InitializeFallbackModules()
        {
            // Validación defensiva antes de la inicialización
            if (_target != null)
            {
                _previewManager = new MRUnificarObjetosPreviewManager(_target);
            }
        }
        
        
        
        /// <summary>
        /// Maneja los cambios en las propiedades y ejecuta acciones correspondientes
        /// </summary>
        private void HandlePropertyChanges()
        {
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                
                // Auto-actualizar rutas si está habilitado
                HandleAutoUpdatePaths();
            }
        }
        
        /// <summary>
        /// Maneja la auto-actualización de rutas
        /// NOTA: Esta versión no incluye comparación de estado porque opera sobre múltiples frames.
        /// Cada frame individual ya optimiza sus llamadas RecalculatePaths() en sus respectivos editores.
        /// </summary>
        private void HandleAutoUpdatePaths()
        {
            if (_target?.AutoUpdatePaths == true && _target.FrameObjects != null)
            {
                // Validación y recálculo de rutas con verificaciones defensivas
                var validFrames = _target.FrameObjects.Where(f => f != null).ToList();
                
                foreach (var frame in validFrames)
                {
                    // Cada frame optimiza internamente sus llamadas RecalculatePaths()
                    frame?.RecalculatePaths();
                }
            }
        }
        
        
        
        /// <summary>
        /// API pública para que los módulos accedan al target
        /// Método de acceso controlado siguiendo principio de Encapsulación
        /// </summary>
        public MRUnificarObjetos GetTarget() => _target;
        
        /// <summary>
        /// API pública para que los módulos accedan al serializedObject
        /// </summary>
        public SerializedObject GetSerializedObject() => serializedObject;
        
        /// <summary>
        /// Fuerza la actualización de la UI
        /// Usado por módulos cuando necesitan refrescar la interfaz
        /// </summary>
        public void ForceUIUpdate()
        {
            Repaint();
        }
        
    }
}
