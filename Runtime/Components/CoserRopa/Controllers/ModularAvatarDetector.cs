using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Controllers
{
    /// <summary>
    /// Detecta componentes de Modular Avatar en GameObjects.
    /// Usa reflexión para evitar dependencia directa del paquete MA.
    ///
    /// Componentes detectados:
    /// - ModularAvatarMergeArmature: Fusiona armatures (misma función que MRCoserRopa)
    /// - ModularAvatarBoneProxy: Referencia a huesos del avatar
    /// - ModularAvatarMenuInstaller: Instalador de menús
    /// </summary>
    public class ModularAvatarDetector
    {
        #region Constants

        /// <summary>
        /// Namespace de Modular Avatar
        /// </summary>
        private const string MA_NAMESPACE = "nadena.dev.modular_avatar.core";

        /// <summary>
        /// Tipos de componentes de Modular Avatar relevantes para cosido de ropa
        /// </summary>
        private static readonly string[] MA_ARMATURE_COMPONENTS = new[]
        {
            "ModularAvatarMergeArmature",  // Principal: fusiona armatures
            "ModularAvatarBoneProxy"       // Secundario: referencia huesos
        };

        /// <summary>
        /// Tipos de componentes de Modular Avatar que controlan blendshapes
        /// </summary>
        private static readonly string[] MA_BLENDSHAPE_COMPONENTS = new[]
        {
            "ModularAvatarShapeChanger",   // Cambia blendshapes reactivamente
            "ModularAvatarBlendshapeSync"  // Sincroniza blendshapes entre meshes
        };

        /// <summary>
        /// Tipos de componentes de Modular Avatar que controlan mesh settings (bounds, anchor)
        /// </summary>
        private static readonly string[] MA_MESH_SETTINGS_COMPONENTS = new[]
        {
            "ModularAvatarMeshSettings"    // Configura bounds y anchor override
        };

        /// <summary>
        /// Componentes PROBLEMATICOS que causan conflictos directos con MR.
        /// Deben desactivarse automaticamente si estan en raiz de ropa.
        /// Si estan en otros GameObjects, requieren decision del usuario.
        /// </summary>
        public static readonly string[] MA_PROBLEMATIC_ON_ROOT = new[]
        {
            "ModularAvatarVertexColorRemover",  // Modifica vertices
            "ModularAvatarMeshCutter",          // Corta meshes
            // ShapeChanger removido: no causa conflictos reales con MR, MA lo procesa correctamente
            "ModularAvatarVertexFilter",        // Filtra vertices por eje
            "VertexFilterByAxisComponent",      // Filtro de vertices por eje (nombre real)
            "ModularAvatarBlendshapeSync"       // Sincroniza blendshapes en Edit Mode (afecta al body)
        };

        /// <summary>
        /// Componentes que requieren DECISION DEL USUARIO.
        /// Pueden causar conflictos pero el usuario debe decidir.
        /// </summary>
        public static readonly string[] MA_USER_DECISION = new[]
        {
            "Animator",                         // Puede tener animaciones conflictivas
            "ModularAvatarMergeAnimator",       // Mezcla animadores
            "ModularAvatarParameters",          // Parametros VRChat
            "ModularAvatarMenuInstaller",       // Instalador de menus
            "ModularAvatarMenuGroup",           // Grupo de menu
            "ModularAvatarMenuItem",            // Item de menu
            "ModularAvatarBoneProxy"            // Proxy de huesos - usuario decide
        };

        /// <summary>
        /// Componentes COMPATIBLES con MR (solo informativos).
        /// MR ya los respeta o desactiva correctamente.
        /// </summary>
        public static readonly string[] MA_COMPATIBLE = new[]
        {
            "ModularAvatarMergeArmature",       // MRCoserRopa lo respeta
            "ModularAvatarMeshSettings"         // MRAjustarBounds lo elimina al escanear
        };

        /// <summary>
        /// Todos los tipos de componentes de Modular Avatar conocidos
        /// </summary>
        private static readonly string[] ALL_MA_COMPONENTS = new[]
        {
            "ModularAvatarMergeArmature",
            "ModularAvatarBoneProxy",
            "ModularAvatarMenuInstaller",
            "ModularAvatarMenuGroup",
            "ModularAvatarMenuItem",
            "ModularAvatarParameters",
            "ModularAvatarMergeAnimator",
            "ModularAvatarBlendshapeSync",
            "ModularAvatarShapeChanger",
            "ModularAvatarObjectToggle",
            "ModularAvatarMeshSettings",
            "ModularAvatarMeshCutter",
            "ModularAvatarVertexColorRemover",
            "ModularAvatarVertexFilter",
            "VertexFilterByAxisComponent"   // Nombre real del componente de filtro de vertices
        };

        /// <summary>
        /// Namespaces adicionales de Modular Avatar que contienen componentes
        /// </summary>
        private static readonly string[] MA_ADDITIONAL_NAMESPACES = new[]
        {
            "nadena.dev.modular_avatar.core.vertex_filters"
        };

        #endregion

        #region Private Fields

        private Dictionary<string, Type> _maTypes = new Dictionary<string, Type>();
        private bool _typesResolved = false;
        private bool _maAvailable = false;

        #endregion

        #region Singleton

        private static ModularAvatarDetector _instance;
        public static ModularAvatarDetector Instance => _instance ??= new ModularAvatarDetector();

        /// <summary>
        /// Resetea el cache de tipos para forzar una nueva resolución.
        /// Útil después de cambios en las listas de componentes.
        /// </summary>
        public static void ResetCache()
        {
            if (_instance != null)
            {
                _instance._typesResolved = false;
                _instance._maTypes.Clear();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Intenta resolver los tipos de Modular Avatar por reflexión.
        /// </summary>
        private void EnsureTypesResolved()
        {
            if (_typesResolved) return;

            _maTypes.Clear();

            // Buscar en todos los assemblies cargados
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Buscar assemblies de Modular Avatar
                if (!assembly.FullName.Contains("modular") &&
                    !assembly.FullName.Contains("Modular"))
                    continue;

                try
                {
                    // Buscar todos los tipos que heredan de MonoBehaviour
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type == null || type.IsAbstract) continue;
                        if (!typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type)) continue;

                        string typeName = type.Name;
                        string typeNamespace = type.Namespace ?? "";

                        // Incluir si:
                        // 1. Empieza con "ModularAvatar" o "MA"
                        // 2. Está en el namespace principal de MA
                        // 3. Está en alguno de los namespaces adicionales de MA
                        bool isMAType = typeName.StartsWith("ModularAvatar") ||
                                        typeName.StartsWith("MA") ||
                                        typeNamespace == MA_NAMESPACE ||
                                        MA_ADDITIONAL_NAMESPACES.Any(ns => typeNamespace.StartsWith(ns));

                        if (isMAType && !_maTypes.ContainsKey(typeName))
                        {
                            _maTypes[typeName] = type;
                            _maAvailable = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ModularAvatarDetector] Error escaneando assembly {assembly.FullName}: {ex.Message}");
                }

                // También buscar por los nombres conocidos en todos los namespaces
                foreach (var componentName in ALL_MA_COMPONENTS)
                {
                    if (_maTypes.ContainsKey(componentName))
                        continue;

                    // Intentar en el namespace principal
                    var fullTypeName = $"{MA_NAMESPACE}.{componentName}";
                    var type = assembly.GetType(fullTypeName);

                    // Intentar en namespaces adicionales
                    if (type == null)
                    {
                        foreach (var ns in MA_ADDITIONAL_NAMESPACES)
                        {
                            fullTypeName = $"{ns}.{componentName}";
                            type = assembly.GetType(fullTypeName);
                            if (type != null) break;
                        }
                    }

                    if (type == null)
                    {
                        // Buscar por nombre simple
                        type = assembly.GetTypes()
                            .FirstOrDefault(t => t.Name == componentName);
                    }

                    if (type != null)
                    {
                        _maTypes[componentName] = type;
                        _maAvailable = true;
                    }
                }
            }

            _typesResolved = true;

            if (_maAvailable)
            {
                Debug.Log($"[ModularAvatarDetector] Modular Avatar detectado. Total: {_maTypes.Count} componentes");
                Debug.Log($"[ModularAvatarDetector] Componentes: {string.Join(", ", _maTypes.Keys)}");
            }
            else
            {
                Debug.LogWarning("[ModularAvatarDetector] Modular Avatar NO detectado");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Indica si Modular Avatar está instalado en el proyecto.
        /// </summary>
        public bool IsModularAvatarAvailable
        {
            get
            {
                EnsureTypesResolved();
                return _maAvailable;
            }
        }

        /// <summary>
        /// Detecta si un GameObject tiene componentes de Modular Avatar que manejan armatures.
        /// Estos son: ModularAvatarMergeArmature, ModularAvatarBoneProxy
        /// </summary>
        /// <param name="gameObject">GameObject a verificar</param>
        /// <returns>Resultado con información sobre MA detectado</returns>
        public MADetectionResult DetectModularAvatar(GameObject gameObject)
        {
            var result = new MADetectionResult();

            if (gameObject == null)
                return result;

            EnsureTypesResolved();

            if (!_maAvailable)
            {
                result.MAAvailable = false;
                return result;
            }

            result.MAAvailable = true;

            // Buscar componentes de MA que manejan armatures
            foreach (var componentName in MA_ARMATURE_COMPONENTS)
            {
                if (!_maTypes.TryGetValue(componentName, out var type))
                    continue;

                // Buscar en el GameObject y sus hijos
                var components = gameObject.GetComponentsInChildren(type, true);

                if (components.Length > 0)
                {
                    result.HasModularAvatar = true;
                    result.DetectedComponents.Add(componentName);
                    result.ComponentCount += components.Length;

                    // Si es MergeArmature, tiene prioridad absoluta
                    if (componentName == "ModularAvatarMergeArmature")
                    {
                        result.HasMergeArmature = true;
                    }
                }
            }

            // Buscar componentes de MA que controlan blendshapes
            foreach (var componentName in MA_BLENDSHAPE_COMPONENTS)
            {
                if (!_maTypes.TryGetValue(componentName, out var type))
                    continue;

                var components = gameObject.GetComponentsInChildren(type, true);

                if (components.Length > 0)
                {
                    result.HasBlendshapeControl = true;
                    if (!result.DetectedComponents.Contains(componentName))
                    {
                        result.DetectedComponents.Add(componentName);
                    }
                    result.ComponentCount += components.Length;

                    if (componentName == "ModularAvatarShapeChanger")
                    {
                        result.HasShapeChanger = true;
                    }
                }
            }

            // Buscar componentes de MA que controlan mesh settings (bounds, anchor)
            foreach (var componentName in MA_MESH_SETTINGS_COMPONENTS)
            {
                if (!_maTypes.TryGetValue(componentName, out var type))
                    continue;

                var components = gameObject.GetComponentsInChildren(type, true);

                if (components.Length > 0)
                {
                    result.HasMeshSettings = true;
                    if (!result.DetectedComponents.Contains(componentName))
                    {
                        result.DetectedComponents.Add(componentName);
                    }
                    result.ComponentCount += components.Length;
                }
            }

            // Determinar el componente principal encontrado
            if (result.HasMergeArmature)
            {
                result.PrimaryComponent = "ModularAvatarMergeArmature";
            }
            else if (result.HasShapeChanger)
            {
                result.PrimaryComponent = "ModularAvatarShapeChanger";
            }
            else if (result.DetectedComponents.Count > 0)
            {
                result.PrimaryComponent = result.DetectedComponents[0];
            }

            return result;
        }

        /// <summary>
        /// Verifica si un GameObject tiene MA Shape Changer.
        /// </summary>
        public bool HasShapeChanger(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            EnsureTypesResolved();

            if (!_maTypes.TryGetValue("ModularAvatarShapeChanger", out var type))
                return false;

            return gameObject.GetComponentInChildren(type, true) != null;
        }

        /// <summary>
        /// Verifica si un GameObject tiene MA Mesh Settings.
        /// </summary>
        public bool HasMeshSettings(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            EnsureTypesResolved();

            if (!_maTypes.TryGetValue("ModularAvatarMeshSettings", out var type))
                return false;

            return gameObject.GetComponentInChildren(type, true) != null;
        }

        /// <summary>
        /// Obtiene todos los componentes MA Mesh Settings en un GameObject.
        /// Útil para desactivarlos cuando MRAjustarBounds tiene prioridad.
        /// </summary>
        public Component[] GetMeshSettingsComponents(GameObject gameObject)
        {
            if (gameObject == null)
                return new Component[0];

            EnsureTypesResolved();

            if (!_maTypes.TryGetValue("ModularAvatarMeshSettings", out var type))
                return new Component[0];

            return gameObject.GetComponentsInChildren(type, true);
        }

        /// <summary>
        /// Destruye todos los componentes MA Mesh Settings en un GameObject.
        /// Retorna la cantidad de componentes destruidos.
        /// Usar desde Editor con Undo para que sea reversible (Ctrl+Z).
        /// </summary>
        public int DestroyMeshSettingsComponents(GameObject gameObject)
        {
            var components = GetMeshSettingsComponents(gameObject);
            int count = 0;

            foreach (var component in components)
            {
                if (component == null) continue;

                Debug.Log($"[ModularAvatarDetector] Eliminado MA Mesh Settings en '{component.gameObject.name}'");

                #if UNITY_EDITOR
                if (UnityEditor.Undo.isProcessing)
                    UnityEngine.Object.DestroyImmediate(component);
                else
                    UnityEditor.Undo.DestroyObjectImmediate(component);
                #else
                UnityEngine.Object.DestroyImmediate(component);
                #endif

                count++;
            }

            return count;
        }

        /// <summary>
        /// Verifica rápidamente si un GameObject tiene ModularAvatarMergeArmature.
        /// Este es el componente más importante ya que hace la misma función que MRCoserRopa.
        /// </summary>
        /// <param name="gameObject">GameObject a verificar</param>
        /// <returns>True si tiene MergeArmature</returns>
        public bool HasMergeArmature(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            EnsureTypesResolved();

            if (!_maTypes.TryGetValue("ModularAvatarMergeArmature", out var type))
                return false;

            return gameObject.GetComponentInChildren(type, true) != null;
        }

        /// <summary>
        /// Obtiene información detallada sobre los componentes de MA en un GameObject.
        /// </summary>
        public MAComponentInfo GetComponentInfo(GameObject gameObject)
        {
            var info = new MAComponentInfo();

            if (gameObject == null)
                return info;

            EnsureTypesResolved();

            if (!_maAvailable)
            {
                info.MAAvailable = false;
                return info;
            }

            info.MAAvailable = true;

            foreach (var kvp in _maTypes)
            {
                var components = gameObject.GetComponentsInChildren(kvp.Value, true);
                if (components.Length > 0)
                {
                    info.Components[kvp.Key] = components.Length;
                }
            }

            return info;
        }

        /// <summary>
        /// Obtiene el tipo de un componente de MA por nombre.
        /// Útil para operaciones avanzadas.
        /// </summary>
        public Type GetMAType(string componentName)
        {
            EnsureTypesResolved();
            return _maTypes.TryGetValue(componentName, out var type) ? type : null;
        }

        /// <summary>
        /// Debug: Lista todos los componentes en un GameObject y sus hijos.
        /// </summary>
        public void DebugListAllComponents(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.Log("[ModularAvatarDetector] GameObject es null");
                return;
            }

            var allComponents = gameObject.GetComponentsInChildren<Component>(true);
            var componentNames = new HashSet<string>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().FullName;
                if (typeName.Contains("modular") || typeName.Contains("Modular") ||
                    typeName.Contains("nadena"))
                {
                    componentNames.Add($"{comp.GetType().Name} ({typeName})");
                }
            }

            Debug.Log($"[ModularAvatarDetector] Componentes MA en '{gameObject.name}': {componentNames.Count}");
            foreach (var name in componentNames)
            {
                Debug.Log($"  - {name}");
            }
        }

        /// <summary>
        /// Clasifica un nombre de componente segun su categoria de colision.
        /// </summary>
        /// <param name="componentTypeName">Nombre del tipo de componente</param>
        /// <returns>Categoria de colision (Problematic, UserDecision, Compatible, o null si no es de MA)</returns>
        public ColisionClassification ClassifyComponent(string componentTypeName)
        {
            if (string.IsNullOrEmpty(componentTypeName))
                return ColisionClassification.Unknown;

            // Verificar listas estáticas primero
            if (MA_PROBLEMATIC_ON_ROOT.Contains(componentTypeName))
                return ColisionClassification.Problematic;

            if (MA_USER_DECISION.Contains(componentTypeName))
                return ColisionClassification.UserDecision;

            if (MA_COMPATIBLE.Contains(componentTypeName))
                return ColisionClassification.Compatible;

            // Clasificar dinámicamente por patrones de nombre
            // Componentes relacionados con Mesh/Vertex son problemáticos
            if (componentTypeName.Contains("MeshCutter") ||
                componentTypeName.Contains("VertexFilter") ||
                componentTypeName.Contains("VertexColor"))
                return ColisionClassification.Problematic;

            // Si es un componente de MA conocido pero no clasificado
            if (ALL_MA_COMPONENTS.Contains(componentTypeName))
                return ColisionClassification.UserDecision;

            // Si es cualquier componente de MA (detectado dinámicamente)
            if (componentTypeName.StartsWith("ModularAvatar") || componentTypeName.StartsWith("MA"))
                return ColisionClassification.UserDecision;

            // Si está en el diccionario de tipos (fue descubierto dinámicamente)
            EnsureTypesResolved();
            if (_maTypes.ContainsKey(componentTypeName))
                return ColisionClassification.UserDecision;

            return ColisionClassification.Unknown;
        }

        /// <summary>
        /// Obtiene todos los componentes de MA en un GameObject agrupados por categoria.
        /// </summary>
        public MAColisionScanResult ScanForColisions(GameObject gameObject)
        {
            var result = new MAColisionScanResult();

            if (gameObject == null)
                return result;

            EnsureTypesResolved();

            // Debug: listar todos los componentes MA encontrados en el GameObject
            DebugListAllComponents(gameObject);

            if (!_maAvailable)
            {
                result.MAAvailable = false;
                return result;
            }

            result.MAAvailable = true;

            // Escanear cada tipo de componente conocido
            foreach (var kvp in _maTypes)
            {
                var components = gameObject.GetComponentsInChildren(kvp.Value, true);
                foreach (var component in components)
                {
                    if (component == null) continue;

                    var classification = ClassifyComponent(kvp.Key);
                    var entry = new MAColisionEntry
                    {
                        Component = component,
                        TypeName = kvp.Key,
                        Classification = classification,
                        HierarchyPath = GetHierarchyPath(component.transform, gameObject.transform)
                    };

                    result.AddEntry(entry);
                }
            }

            // Buscar Animator standalone (no de MA)
            var animators = gameObject.GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                // Ignorar el Animator raiz del avatar
                if (animator.transform == gameObject.transform) continue;

                var entry = new MAColisionEntry
                {
                    Component = animator,
                    TypeName = "Animator",
                    Classification = ColisionClassification.UserDecision,
                    HierarchyPath = GetHierarchyPath(animator.transform, gameObject.transform)
                };

                result.AddEntry(entry);
            }

            return result;
        }

        /// <summary>
        /// Obtiene la ruta de jerarquia relativa.
        /// </summary>
        private string GetHierarchyPath(Transform target, Transform root)
        {
            if (target == null || root == null || target == root)
                return "";

            var path = new System.Collections.Generic.List<string>();
            var current = target;

            while (current != null && current != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        /// <summary>
        /// Desactiva componentes problematicos en un GameObject.
        /// </summary>
        /// <param name="gameObject">GameObject a procesar</param>
        /// <param name="onlyOnClothingRoots">Si es true, solo desactiva componentes en raices de ropa</param>
        /// <param name="clothingRoots">Lista de GameObjects que son raices de ropa</param>
        /// <returns>Cantidad de componentes desactivados</returns>
        public int DisableProblematicComponents(GameObject gameObject, bool onlyOnClothingRoots = false,
            System.Collections.Generic.List<GameObject> clothingRoots = null)
        {
            EnsureTypesResolved();

            if (!_maAvailable || gameObject == null)
                return 0;

            int count = 0;

            foreach (var typeName in MA_PROBLEMATIC_ON_ROOT)
            {
                if (!_maTypes.TryGetValue(typeName, out var type))
                    continue;

                var components = gameObject.GetComponentsInChildren(type, true);
                foreach (var component in components)
                {
                    if (component == null) continue;

                    // Si solo queremos los que estan en raiz de ropa
                    if (onlyOnClothingRoots && clothingRoots != null)
                    {
                        bool isOnClothingRoot = clothingRoots.Contains(component.gameObject);
                        if (!isOnClothingRoot) continue;
                    }

                    if (component is MonoBehaviour mb && mb.enabled)
                    {
                        mb.enabled = false;
                        count++;
                        Debug.Log($"[ModularAvatarDetector] Desactivado {typeName} en '{component.gameObject.name}'");
                    }
                }
            }

            return count;
        }

        #endregion
    }

    /// <summary>
    /// Clasificacion de colision para componentes de MA.
    /// </summary>
    public enum ColisionClassification
    {
        Unknown = 0,
        Problematic = 1,
        UserDecision = 2,
        Compatible = 3
    }

    /// <summary>
    /// Entrada de colision detectada.
    /// </summary>
    public class MAColisionEntry
    {
        public Component Component { get; set; }
        public string TypeName { get; set; }
        public ColisionClassification Classification { get; set; }
        public string HierarchyPath { get; set; }

        public bool IsValid => Component != null;
        public string GameObjectName => Component != null ? Component.gameObject.name : "(null)";
    }

    /// <summary>
    /// Resultado del escaneo de colisiones de MA.
    /// </summary>
    public class MAColisionScanResult
    {
        public bool MAAvailable { get; set; } = false;
        public System.Collections.Generic.List<MAColisionEntry> ProblematicEntries { get; } = new System.Collections.Generic.List<MAColisionEntry>();
        public System.Collections.Generic.List<MAColisionEntry> UserDecisionEntries { get; } = new System.Collections.Generic.List<MAColisionEntry>();
        public System.Collections.Generic.List<MAColisionEntry> CompatibleEntries { get; } = new System.Collections.Generic.List<MAColisionEntry>();

        public int TotalCount => ProblematicEntries.Count + UserDecisionEntries.Count + CompatibleEntries.Count;
        public bool HasAny => TotalCount > 0;

        public void AddEntry(MAColisionEntry entry)
        {
            if (entry == null) return;

            switch (entry.Classification)
            {
                case ColisionClassification.Problematic:
                    ProblematicEntries.Add(entry);
                    break;
                case ColisionClassification.UserDecision:
                    UserDecisionEntries.Add(entry);
                    break;
                case ColisionClassification.Compatible:
                    CompatibleEntries.Add(entry);
                    break;
            }
        }
    }

    /// <summary>
    /// Resultado de la detección de Modular Avatar en un GameObject.
    /// </summary>
    public class MADetectionResult
    {
        /// <summary>
        /// Si Modular Avatar está disponible en el proyecto
        /// </summary>
        public bool MAAvailable { get; set; } = false;

        /// <summary>
        /// Si el GameObject tiene algún componente de MA relevante
        /// </summary>
        public bool HasModularAvatar { get; set; } = false;

        /// <summary>
        /// Si tiene específicamente ModularAvatarMergeArmature
        /// (el componente que hace la misma función que MRCoserRopa)
        /// </summary>
        public bool HasMergeArmature { get; set; } = false;

        /// <summary>
        /// Si tiene ModularAvatarShapeChanger
        /// (controla blendshapes reactivamente)
        /// </summary>
        public bool HasShapeChanger { get; set; } = false;

        /// <summary>
        /// Si tiene algún componente de MA que controla blendshapes
        /// (ShapeChanger o BlendshapeSync)
        /// </summary>
        public bool HasBlendshapeControl { get; set; } = false;

        /// <summary>
        /// Si tiene ModularAvatarMeshSettings
        /// (controla bounds y anchor override)
        /// </summary>
        public bool HasMeshSettings { get; set; } = false;

        /// <summary>
        /// Lista de nombres de componentes detectados
        /// </summary>
        public List<string> DetectedComponents { get; set; } = new List<string>();

        /// <summary>
        /// Nombre del componente principal detectado
        /// </summary>
        public string PrimaryComponent { get; set; } = "";

        /// <summary>
        /// Número total de componentes de MA encontrados
        /// </summary>
        public int ComponentCount { get; set; } = 0;

        /// <summary>
        /// Indica si MRCoserRopa debe procesar este GameObject.
        /// False si MA tiene MergeArmature configurado.
        /// </summary>
        public bool ShouldMRProcess => !HasMergeArmature;

        public override string ToString()
        {
            if (!MAAvailable)
                return "Modular Avatar no instalado";

            if (!HasModularAvatar)
                return "Sin componentes de Modular Avatar";

            return $"MA: {string.Join(", ", DetectedComponents)} ({ComponentCount} componentes)";
        }
    }

    /// <summary>
    /// Información detallada sobre componentes de MA en un GameObject.
    /// </summary>
    public class MAComponentInfo
    {
        /// <summary>
        /// Si Modular Avatar está disponible
        /// </summary>
        public bool MAAvailable { get; set; } = false;

        /// <summary>
        /// Diccionario de componentes encontrados y sus cantidades
        /// </summary>
        public Dictionary<string, int> Components { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Total de componentes encontrados
        /// </summary>
        public int TotalCount => Components.Values.Sum();

        public override string ToString()
        {
            if (!MAAvailable)
                return "Modular Avatar no instalado";

            if (Components.Count == 0)
                return "Sin componentes de MA";

            return string.Join(", ", Components.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }
    }
}
