using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AnalisisColision.Controllers
{
    /// <summary>
    /// Controlador para desactivar/restaurar ModularAvatarBlendshapeSync.
    ///
    /// PROBLEMA: BlendshapeSync tiene [ExecuteAlways] y se registra en un loop estático
    /// que sincroniza blendshapes en CADA frame del editor. Simplemente desactivar el
    /// componente (enabled=false) NO detiene la sincronización porque:
    /// 1. El loop no verifica isActiveAndEnabled
    /// 2. Los bindings (_editorBindings) persisten aunque el componente esté desactivado
    ///
    /// SOLUCIÓN: Usar reflexión para:
    /// 1. Desregistrar del loop (BlendshapeSyncUpdateLoop.Unregister)
    /// 2. Limpiar bindings (_editorBindings = null)
    /// 3. Desactivar el componente (enabled = false)
    ///
    /// ADVERTENCIA: Esta solución depende de la implementación interna de Modular Avatar
    /// y podría dejar de funcionar con actualizaciones futuras de MA.
    /// </summary>
    public static class BlendshapeSyncController
    {
        #region Constants

        private const string MA_NAMESPACE = "nadena.dev.modular_avatar.core";
        private const string BLENDSHAPE_SYNC_TYPE = "ModularAvatarBlendshapeSync";
        private const string UPDATE_LOOP_TYPE = "BlendshapeSyncUpdateLoop";
        private const string EDITOR_BINDINGS_FIELD = "_editorBindings";

        #endregion

        #region Cached Reflection Data

        private static Type _blendshapeSyncType;
        private static Type _updateLoopType;
        private static MethodInfo _registerMethod;
        private static MethodInfo _unregisterMethod;
        private static MethodInfo _rebindMethod;
        private static FieldInfo _editorBindingsField;
        private static bool _reflectionInitialized = false;
        private static bool _reflectionAvailable = false;

        #endregion

        #region State Tracking

        /// <summary>
        /// Almacena el estado de componentes que han sido "detenidos".
        /// Key: InstanceID del componente
        /// Value: True si estaba habilitado originalmente
        /// </summary>
        private static Dictionary<int, bool> _stoppedComponents = new Dictionary<int, bool>();

        /// <summary>
        /// Componentes que deben mantenerse detenidos (limpieza periódica de bindings).
        /// </summary>
        private static HashSet<int> _componentsToKeepStopped = new HashSet<int>();

        /// <summary>
        /// Indica si el callback de limpieza está registrado.
        /// </summary>
        private static bool _cleanupCallbackRegistered = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa la reflexión para acceder a los tipos internos de MA.
        /// </summary>
        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                // Buscar el assembly de Modular Avatar
                Assembly maAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName == "nadena.dev.modular-avatar.core" ||
                        assemblyName.Contains("modular-avatar"))
                    {
                        maAssembly = assembly;
                        Debug.Log($"[BlendshapeSyncController] Assembly encontrado: {assembly.FullName}");
                        break;
                    }
                }

                if (maAssembly == null)
                {
                    // Buscar por tipo conocido
                    var knownType = Type.GetType("nadena.dev.modular_avatar.core.ModularAvatarBlendshapeSync, nadena.dev.modular-avatar.core");
                    if (knownType != null)
                    {
                        maAssembly = knownType.Assembly;
                        Debug.Log($"[BlendshapeSyncController] Assembly encontrado via tipo: {maAssembly.FullName}");
                    }
                }

                if (maAssembly == null)
                {
                    Debug.LogWarning("[BlendshapeSyncController] Assembly de Modular Avatar no encontrado");
                    return;
                }

                // Obtener tipos
                _blendshapeSyncType = maAssembly.GetType($"{MA_NAMESPACE}.{BLENDSHAPE_SYNC_TYPE}");
                _updateLoopType = maAssembly.GetType($"{MA_NAMESPACE}.{UPDATE_LOOP_TYPE}");

                Debug.Log($"[BlendshapeSyncController] BlendshapeSyncType: {(_blendshapeSyncType != null ? "OK" : "NULL")}");
                Debug.Log($"[BlendshapeSyncController] UpdateLoopType: {(_updateLoopType != null ? "OK" : "NULL")}");

                if (_blendshapeSyncType == null)
                {
                    Debug.LogWarning($"[BlendshapeSyncController] Tipo {BLENDSHAPE_SYNC_TYPE} no encontrado");
                    return;
                }

                if (_updateLoopType == null)
                {
                    Debug.LogWarning($"[BlendshapeSyncController] Tipo {UPDATE_LOOP_TYPE} no encontrado");
                    return;
                }

                // Obtener métodos del loop (internal = NonPublic para reflexión)
                _registerMethod = _updateLoopType.GetMethod("Register",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                _unregisterMethod = _updateLoopType.GetMethod("Unregister",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                if (_registerMethod == null || _unregisterMethod == null)
                {
                    Debug.LogWarning("[BlendshapeSyncController] Métodos Register/Unregister no encontrados");
                    return;
                }

                // Obtener campo de bindings y método Rebind
                _editorBindingsField = _blendshapeSyncType.GetField(EDITOR_BINDINGS_FIELD,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _rebindMethod = _blendshapeSyncType.GetMethod("Rebind",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (_editorBindingsField == null)
                {
                    Debug.LogWarning("[BlendshapeSyncController] Campo _editorBindings no encontrado");
                    return;
                }

                _reflectionAvailable = true;
                Debug.Log("[BlendshapeSyncController] Reflexión inicializada correctamente");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlendshapeSyncController] Error inicializando reflexión: {ex.Message}");
                _reflectionAvailable = false;
            }
        }

        #endregion

        #region Cleanup Callback

#if UNITY_EDITOR
        /// <summary>
        /// Registra el callback de limpieza periódica.
        /// Necesario porque OnValidate() de MA vuelve a registrar componentes constantemente.
        /// </summary>
        private static void EnsureCleanupCallbackRegistered()
        {
            if (_cleanupCallbackRegistered) return;
            _cleanupCallbackRegistered = true;

            UnityEditor.EditorApplication.update += PeriodicCleanup;
            Debug.Log("[BlendshapeSyncController] Callback de limpieza periódica registrado");
        }

        /// <summary>
        /// Limpieza periódica: mantiene los bindings nulos para componentes detenidos.
        /// </summary>
        private static void PeriodicCleanup()
        {
            if (_componentsToKeepStopped.Count == 0) return;
            if (!_reflectionAvailable || _editorBindingsField == null) return;

            // Limpiar IDs de componentes destruidos
            var toRemove = new List<int>();

            foreach (var instanceId in _componentsToKeepStopped)
            {
                // Buscar el componente por InstanceID
                var obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId);
                if (obj == null)
                {
                    toRemove.Add(instanceId);
                    continue;
                }

                var component = obj as Component;
                if (component == null)
                {
                    toRemove.Add(instanceId);
                    continue;
                }

                // Mantener bindings limpios
                var currentBindings = _editorBindingsField.GetValue(component);
                if (currentBindings != null)
                {
                    _editorBindingsField.SetValue(component, null);
                }

                // También desregistrar del loop si es posible
                if (_unregisterMethod != null)
                {
                    try
                    {
                        _unregisterMethod.Invoke(null, new object[] { component });
                    }
                    catch { }
                }
            }

            foreach (var id in toRemove)
            {
                _componentsToKeepStopped.Remove(id);
                _stoppedComponents.Remove(id);
            }
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// Indica si la funcionalidad de control está disponible.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                InitializeReflection();
                return _reflectionAvailable;
            }
        }

        /// <summary>
        /// Verifica si un componente es un ModularAvatarBlendshapeSync.
        /// </summary>
        public static bool IsBlendshapeSync(Component component)
        {
            if (component == null) return false;
            InitializeReflection();
            if (!_reflectionAvailable || _blendshapeSyncType == null) return false;
            return _blendshapeSyncType.IsAssignableFrom(component.GetType());
        }

        /// <summary>
        /// Verifica si un componente está actualmente "detenido" (sincronización desactivada).
        /// </summary>
        public static bool IsStopped(Component component)
        {
            if (component == null) return false;
            return _stoppedComponents.ContainsKey(component.GetInstanceID());
        }

        /// <summary>
        /// Detiene la sincronización de un BlendshapeSync.
        /// 1. Desregistra del loop de actualización
        /// 2. Limpia los bindings
        /// 3. Desactiva el componente
        /// 4. Registra para limpieza periódica (porque OnValidate vuelve a registrar)
        /// </summary>
        /// <param name="component">El componente BlendshapeSync a detener</param>
        /// <returns>True si se detuvo exitosamente</returns>
        public static bool StopSync(Component component)
        {
            if (component == null) return false;

            InitializeReflection();
            if (!_reflectionAvailable)
            {
                Debug.LogError("[BlendshapeSyncController] Reflexión no disponible");
                return false;
            }

            if (!IsBlendshapeSync(component))
            {
                Debug.LogWarning($"[BlendshapeSyncController] El componente no es BlendshapeSync: {component.GetType().Name}");
                return false;
            }

            try
            {
                int instanceId = component.GetInstanceID();

                // Guardar estado original si no está ya detenido
                if (!_stoppedComponents.ContainsKey(instanceId))
                {
                    bool wasEnabled = (component as MonoBehaviour)?.enabled ?? true;
                    _stoppedComponents[instanceId] = wasEnabled;
                }

                // Agregar a la lista de "mantener detenidos"
                _componentsToKeepStopped.Add(instanceId);

#if UNITY_EDITOR
                // Asegurar que el callback de limpieza esté registrado
                EnsureCleanupCallbackRegistered();
#endif

                // 1. Desregistrar del loop
                if (_unregisterMethod != null)
                {
                    try
                    {
                        _unregisterMethod.Invoke(null, new object[] { component });
                        Debug.Log($"[BlendshapeSyncController] Desregistrado del loop: {component.gameObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BlendshapeSyncController] Error en Unregister: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[BlendshapeSyncController] Método Unregister no disponible");
                }

                // 2. Limpiar bindings
                if (_editorBindingsField != null)
                {
                    _editorBindingsField.SetValue(component, null);
                    Debug.Log($"[BlendshapeSyncController] Bindings limpiados: {component.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning("[BlendshapeSyncController] Campo _editorBindings no disponible");
                }

                // 3. Desactivar componente
                if (component is MonoBehaviour mb)
                {
                    mb.enabled = false;
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.EditorUtility.SetDirty(component.gameObject);
#endif

                Debug.Log($"[BlendshapeSyncController] Sincronización detenida: {component.gameObject.name} (ID: {instanceId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlendshapeSyncController] Error deteniendo sincronización: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Restaura la sincronización de un BlendshapeSync.
        /// 1. Quita de la lista de "mantener detenidos"
        /// 2. Activa el componente
        /// 3. Registra en el loop
        /// 4. Fuerza rebind de los bindings
        /// </summary>
        /// <param name="component">El componente BlendshapeSync a restaurar</param>
        /// <returns>True si se restauró exitosamente</returns>
        public static bool RestoreSync(Component component)
        {
            if (component == null) return false;

            InitializeReflection();
            if (!_reflectionAvailable) return false;

            if (!IsBlendshapeSync(component))
            {
                Debug.LogWarning($"[BlendshapeSyncController] El componente no es BlendshapeSync: {component.GetType().Name}");
                return false;
            }

            try
            {
                int instanceId = component.GetInstanceID();

                // Quitar de la lista de "mantener detenidos"
                _componentsToKeepStopped.Remove(instanceId);

                // Obtener estado original
                bool wasEnabled = true;
                if (_stoppedComponents.TryGetValue(instanceId, out bool originalState))
                {
                    wasEnabled = originalState;
                    _stoppedComponents.Remove(instanceId);
                }

                // 1. Activar componente (si estaba habilitado originalmente)
                if (component is MonoBehaviour mb)
                {
                    mb.enabled = wasEnabled;
                }

                // 2. Registrar en el loop
                if (_registerMethod != null && wasEnabled)
                {
                    try
                    {
                        _registerMethod.Invoke(null, new object[] { component });
                        Debug.Log($"[BlendshapeSyncController] Registrado en loop: {component.gameObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BlendshapeSyncController] Error en Register: {ex.Message}");
                    }
                }

                // 3. Forzar rebind (si el método está disponible y el componente está activo)
                if (_rebindMethod != null && wasEnabled)
                {
                    try
                    {
                        _rebindMethod.Invoke(component, null);
                        Debug.Log($"[BlendshapeSyncController] Rebind ejecutado: {component.gameObject.name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BlendshapeSyncController] Error en Rebind: {ex.Message}");
                    }
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.EditorUtility.SetDirty(component.gameObject);
#endif

                Debug.Log($"[BlendshapeSyncController] Sincronización restaurada: {component.gameObject.name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlendshapeSyncController] Error restaurando sincronización: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Detiene la sincronización de todos los BlendshapeSync en un GameObject y sus hijos.
        /// </summary>
        /// <param name="root">GameObject raíz para buscar</param>
        /// <returns>Cantidad de componentes detenidos</returns>
        public static int StopAllInHierarchy(GameObject root)
        {
            if (root == null) return 0;

            InitializeReflection();
            if (!_reflectionAvailable || _blendshapeSyncType == null) return 0;

            int count = 0;
            var components = root.GetComponentsInChildren(_blendshapeSyncType, true);

            foreach (var component in components)
            {
                if (StopSync(component))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Restaura la sincronización de todos los BlendshapeSync detenidos en un GameObject y sus hijos.
        /// </summary>
        /// <param name="root">GameObject raíz para buscar</param>
        /// <returns>Cantidad de componentes restaurados</returns>
        public static int RestoreAllInHierarchy(GameObject root)
        {
            if (root == null) return 0;

            InitializeReflection();
            if (!_reflectionAvailable || _blendshapeSyncType == null) return 0;

            int count = 0;
            var components = root.GetComponentsInChildren(_blendshapeSyncType, true);

            foreach (var component in components)
            {
                if (IsStopped(component) && RestoreSync(component))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Obtiene todos los BlendshapeSync en un GameObject y sus hijos.
        /// </summary>
        public static Component[] GetAllBlendshapeSyncs(GameObject root)
        {
            if (root == null) return new Component[0];

            InitializeReflection();
            if (!_reflectionAvailable || _blendshapeSyncType == null) return new Component[0];

            return root.GetComponentsInChildren(_blendshapeSyncType, true);
        }

        /// <summary>
        /// Limpia el estado de componentes detenidos (para reset).
        /// </summary>
        public static void ClearStoppedState()
        {
            _stoppedComponents.Clear();
            _componentsToKeepStopped.Clear();
        }

        /// <summary>
        /// Obtiene información sobre el estado del controlador.
        /// </summary>
        public static string GetStatusInfo()
        {
            InitializeReflection();

            if (!_reflectionAvailable)
            {
                return "BlendshapeSyncController: No disponible (reflexión falló)";
            }

            return $"BlendshapeSyncController: Disponible\n" +
                   $"  - Tipo BlendshapeSync: {(_blendshapeSyncType != null ? "OK" : "No encontrado")}\n" +
                   $"  - Tipo UpdateLoop: {(_updateLoopType != null ? "OK" : "No encontrado")}\n" +
                   $"  - Método Register: {(_registerMethod != null ? "OK" : "No encontrado")}\n" +
                   $"  - Método Unregister: {(_unregisterMethod != null ? "OK" : "No encontrado")}\n" +
                   $"  - Campo _editorBindings: {(_editorBindingsField != null ? "OK" : "No encontrado")}\n" +
                   $"  - Método Rebind: {(_rebindMethod != null ? "OK" : "No encontrado")}\n" +
                   $"  - Componentes detenidos: {_stoppedComponents.Count}\n" +
                   $"  - En limpieza periódica: {_componentsToKeepStopped.Count}";
        }

        #endregion
    }
}
