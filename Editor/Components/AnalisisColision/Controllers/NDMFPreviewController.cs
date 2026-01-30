using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Editor.Components.AnalisisColision.Controllers
{
    /// <summary>
    /// Controlador para manejar los toggles de preview de NDMF via reflexión.
    /// Permite desactivar las previsualizaciones de Shape Changer y Mesh Deleter
    /// que afectan al body del avatar en Edit Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class NDMFPreviewController
    {
        /// <summary>
        /// Constructor estático que se ejecuta cuando Unity carga.
        /// Desactiva los previews por defecto para evitar conflictos con MR.
        /// </summary>
        static NDMFPreviewController()
        {
            // Esperar un frame para que Unity termine de cargar
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            // Solo ejecutar si el controlador está disponible
            if (!IsAvailable) return;

            // Desactivar previews por defecto si están activos
            if (IsShapeChangerEnabled || IsMeshDeleterEnabled)
            {
                DisableAllBodyAffectingPreviews();
                Debug.Log("[NDMFPreviewController] Previews de MA desactivados automáticamente al cargar Unity");
            }
        }

        #region Constantes

        // Nombres calificados usados por NDMF para persistir el estado
        private const string SHAPE_CHANGER_QUALIFIED_NAME = "nadena.dev.modular-avatar/ShapeChangerPreview";
        private const string MESH_DELETER_QUALIFIED_NAME = "nadena.dev.modular-avatar/MeshDeleterPreview";

        // Tipos y campos a acceder via reflexión
        private const string NDMF_PREVIEW_NAMESPACE = "nadena.dev.ndmf.preview";
        private const string NDMF_PREVIEW_UI_NAMESPACE = "nadena.dev.ndmf.preview.UI";
        private const string MA_EDITOR_NAMESPACE = "nadena.dev.modular_avatar.core.editor";

        #endregion

        #region Datos de Reflexión en Caché

        private static bool _initialized = false;
        private static bool _isAvailable = false;

        // Singleton de PreviewPrefs
        private static Type _previewPrefsType;
        private static PropertyInfo _previewPrefsInstanceProperty;
        private static MethodInfo _getNodeStateMethod;
        private static MethodInfo _setNodeStateMethod;

        // TogglablePreviewNode y PublishedValue para acceso directo
        private static Type _togglablePreviewNodeType;
        private static Type _publishedValueBoolType;

        // Campos estáticos de los previews (acceso directo a EnableNode)
        private static FieldInfo _shapeChangerEnableNodeField;
        private static FieldInfo _meshDeleterEnableNodeField;
        private static PropertyInfo _isEnabledProperty;
        private static PropertyInfo _publishedValueProperty;

        #endregion

        #region Propiedades Públicas

        /// <summary>
        /// Indica si el controlador está disponible (reflexión exitosa).
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (!_initialized) Initialize();
                return _isAvailable;
            }
        }

        /// <summary>
        /// Indica si el preview de Shape Changer está habilitado.
        /// </summary>
        public static bool IsShapeChangerEnabled
        {
            get
            {
                if (!IsAvailable) return true; // Default a true si no está disponible
                return GetPreviewState(SHAPE_CHANGER_QUALIFIED_NAME, true);
            }
        }

        /// <summary>
        /// Indica si el preview de Mesh Deleter está habilitado.
        /// </summary>
        public static bool IsMeshDeleterEnabled
        {
            get
            {
                if (!IsAvailable) return true;
                return GetPreviewState(MESH_DELETER_QUALIFIED_NAME, true);
            }
        }

        #endregion

        #region Inicialización

        /// <summary>
        /// Inicializa la reflexión a los tipos de NDMF.
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // Buscar el assembly de NDMF Editor
                Assembly ndmfAssembly = null;
                Assembly maEditorAssembly = null;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var name = assembly.GetName().Name;
                    if (name == "nadena.dev.ndmf" || name.Contains("ndmf"))
                    {
                        // Verificar si tiene el tipo PreviewPrefs
                        var testType = assembly.GetType($"{NDMF_PREVIEW_UI_NAMESPACE}.PreviewPrefs");
                        if (testType != null)
                        {
                            ndmfAssembly = assembly;
                        }
                    }
                    else if (name == "nadena.dev.modular-avatar.core.editor" ||
                             name.Contains("modular-avatar") && name.Contains("editor"))
                    {
                        var testType = assembly.GetType($"{MA_EDITOR_NAMESPACE}.ShapeChangerPreview");
                        if (testType != null)
                        {
                            maEditorAssembly = assembly;
                        }
                    }
                }

                if (ndmfAssembly == null)
                {
                    Debug.LogWarning("[NDMFPreviewController] No se encontró el assembly de NDMF");
                    return;
                }

                // Obtener PreviewPrefs
                _previewPrefsType = ndmfAssembly.GetType($"{NDMF_PREVIEW_UI_NAMESPACE}.PreviewPrefs");
                if (_previewPrefsType == null)
                {
                    Debug.LogWarning("[NDMFPreviewController] No se encontró el tipo PreviewPrefs");
                    return;
                }

                // PreviewPrefs es un ScriptableSingleton<PreviewPrefs>, tiene propiedad 'instance'
                _previewPrefsInstanceProperty = _previewPrefsType.GetProperty("instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (_previewPrefsInstanceProperty == null)
                {
                    Debug.LogWarning("[NDMFPreviewController] No se encontró la propiedad 'instance' de PreviewPrefs");
                    return;
                }

                // Obtener métodos GetNodeState y SetNodeState
                _getNodeStateMethod = _previewPrefsType.GetMethod("GetNodeState",
                    BindingFlags.Public | BindingFlags.Instance);
                _setNodeStateMethod = _previewPrefsType.GetMethod("SetNodeState",
                    BindingFlags.Public | BindingFlags.Instance);

                if (_getNodeStateMethod == null || _setNodeStateMethod == null)
                {
                    Debug.LogWarning("[NDMFPreviewController] No se encontraron los métodos GetNodeState/SetNodeState");
                    return;
                }

                // Obtener TogglablePreviewNode para acceso directo
                _togglablePreviewNodeType = ndmfAssembly.GetType($"{NDMF_PREVIEW_NAMESPACE}.TogglablePreviewNode");
                if (_togglablePreviewNodeType != null)
                {
                    _isEnabledProperty = _togglablePreviewNodeType.GetProperty("IsEnabled",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                // Intentar acceso directo a los EnableNode de ShapeChanger y MeshDeleter
                if (maEditorAssembly != null)
                {
                    var shapeChangerType = maEditorAssembly.GetType($"{MA_EDITOR_NAMESPACE}.ShapeChangerPreview");
                    var meshDeleterType = maEditorAssembly.GetType($"{MA_EDITOR_NAMESPACE}.MeshDeleterPreview");

                    if (shapeChangerType != null)
                    {
                        _shapeChangerEnableNodeField = shapeChangerType.GetField("EnableNode",
                            BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    if (meshDeleterType != null)
                    {
                        _meshDeleterEnableNodeField = meshDeleterType.GetField("EnableNode",
                            BindingFlags.NonPublic | BindingFlags.Static);
                    }
                }

                // Obtener PublishedValue<bool>.Value property
                if (_isEnabledProperty != null)
                {
                    _publishedValueBoolType = _isEnabledProperty.PropertyType;
                    _publishedValueProperty = _publishedValueBoolType?.GetProperty("Value",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                _isAvailable = true;
                Debug.Log("[NDMFPreviewController] Inicializado correctamente");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NDMFPreviewController] Error durante inicialización: {ex.Message}");
                _isAvailable = false;
            }
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Desactiva el preview de Shape Changer.
        /// </summary>
        public static bool DisableShapeChangerPreview()
        {
            return SetPreviewState(SHAPE_CHANGER_QUALIFIED_NAME, false, _shapeChangerEnableNodeField);
        }

        /// <summary>
        /// Activa el preview de Shape Changer.
        /// </summary>
        public static bool EnableShapeChangerPreview()
        {
            return SetPreviewState(SHAPE_CHANGER_QUALIFIED_NAME, true, _shapeChangerEnableNodeField);
        }

        /// <summary>
        /// Desactiva el preview de Mesh Deleter.
        /// </summary>
        public static bool DisableMeshDeleterPreview()
        {
            return SetPreviewState(MESH_DELETER_QUALIFIED_NAME, false, _meshDeleterEnableNodeField);
        }

        /// <summary>
        /// Activa el preview de Mesh Deleter.
        /// </summary>
        public static bool EnableMeshDeleterPreview()
        {
            return SetPreviewState(MESH_DELETER_QUALIFIED_NAME, true, _meshDeleterEnableNodeField);
        }

        /// <summary>
        /// Desactiva ambos previews (Shape Changer y Mesh Deleter).
        /// Esto evita que afecten al body del avatar en Edit Mode.
        /// </summary>
        public static bool DisableAllBodyAffectingPreviews()
        {
            bool result1 = DisableShapeChangerPreview();
            bool result2 = DisableMeshDeleterPreview();

            if (result1 || result2)
            {
                // Forzar repaint de Scene views para que el cambio sea visible
                EditorApplication.delayCall += () => SceneView.RepaintAll();
            }

            return result1 && result2;
        }

        /// <summary>
        /// Activa ambos previews (Shape Changer y Mesh Deleter).
        /// </summary>
        public static bool EnableAllBodyAffectingPreviews()
        {
            bool result1 = EnableShapeChangerPreview();
            bool result2 = EnableMeshDeleterPreview();

            if (result1 || result2)
            {
                EditorApplication.delayCall += () => SceneView.RepaintAll();
            }

            return result1 && result2;
        }

        /// <summary>
        /// Obtiene información de diagnóstico del controlador.
        /// </summary>
        public static string GetStatusInfo()
        {
            if (!_initialized) Initialize();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Estado de NDMFPreviewController ===");
            sb.AppendLine($"Inicializado: {_initialized}");
            sb.AppendLine($"Disponible: {_isAvailable}");
            sb.AppendLine();
            sb.AppendLine("Estado de Reflexión:");
            sb.AppendLine($"  PreviewPrefsType: {(_previewPrefsType != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  InstanceProperty: {(_previewPrefsInstanceProperty != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  GetNodeStateMethod: {(_getNodeStateMethod != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  SetNodeStateMethod: {(_setNodeStateMethod != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  TogglablePreviewNodeType: {(_togglablePreviewNodeType != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  ShapeChangerEnableNodeField: {(_shapeChangerEnableNodeField != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  MeshDeleterEnableNodeField: {(_meshDeleterEnableNodeField != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine($"  PublishedValueProperty: {(_publishedValueProperty != null ? "OK" : "NO ENCONTRADO")}");
            sb.AppendLine();

            if (_isAvailable)
            {
                sb.AppendLine("Estado Actual:");
                sb.AppendLine($"  Shape Changer Preview: {(IsShapeChangerEnabled ? "ACTIVO" : "DESACTIVADO")}");
                sb.AppendLine($"  Mesh Deleter Preview: {(IsMeshDeleterEnabled ? "ACTIVO" : "DESACTIVADO")}");
            }

            return sb.ToString();
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Obtiene el estado de un preview desde PreviewPrefs.
        /// </summary>
        private static bool GetPreviewState(string qualifiedName, bool defaultValue)
        {
            if (!_isAvailable) return defaultValue;

            try
            {
                var instance = _previewPrefsInstanceProperty.GetValue(null);
                if (instance == null) return defaultValue;

                var result = _getNodeStateMethod.Invoke(instance, new object[] { qualifiedName, defaultValue });
                return (bool)result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NDMFPreviewController] Error al obtener estado de {qualifiedName}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Establece el estado de un preview usando PreviewPrefs y acceso directo al EnableNode.
        /// </summary>
        private static bool SetPreviewState(string qualifiedName, bool enabled, FieldInfo enableNodeField)
        {
            if (!_isAvailable) return false;

            try
            {
                // Método 1: Usar PreviewPrefs para persistir el estado
                var prefsInstance = _previewPrefsInstanceProperty.GetValue(null);
                if (prefsInstance != null)
                {
                    _setNodeStateMethod.Invoke(prefsInstance, new object[] { qualifiedName, enabled });
                }

                // Método 2: Acceso directo al EnableNode.IsEnabled.Value para efecto inmediato
                if (enableNodeField != null && _publishedValueProperty != null)
                {
                    var enableNode = enableNodeField.GetValue(null);
                    if (enableNode != null && _isEnabledProperty != null)
                    {
                        var isEnabledPublishedValue = _isEnabledProperty.GetValue(enableNode);
                        if (isEnabledPublishedValue != null)
                        {
                            _publishedValueProperty.SetValue(isEnabledPublishedValue, enabled);
                        }
                    }
                }

                Debug.Log($"[NDMFPreviewController] Preview {qualifiedName}: {(enabled ? "ACTIVADO" : "DESACTIVADO")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NDMFPreviewController] Error al establecer estado de {qualifiedName}: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
