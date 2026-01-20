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
            "ModularAvatarMeshSettings"
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

                foreach (var componentName in ALL_MA_COMPONENTS)
                {
                    if (_maTypes.ContainsKey(componentName))
                        continue;

                    // Intentar encontrar el tipo
                    var fullTypeName = $"{MA_NAMESPACE}.{componentName}";
                    var type = assembly.GetType(fullTypeName);

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
                Debug.Log($"[ModularAvatarDetector] Modular Avatar detectado. Componentes encontrados: {string.Join(", ", _maTypes.Keys)}");
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
        /// Desactiva todos los componentes MA Mesh Settings en un GameObject.
        /// Retorna la cantidad de componentes desactivados.
        /// </summary>
        public int DisableMeshSettingsComponents(GameObject gameObject)
        {
            var components = GetMeshSettingsComponents(gameObject);
            int count = 0;

            foreach (var component in components)
            {
                if (component is MonoBehaviour mb && mb.enabled)
                {
                    mb.enabled = false;
                    count++;
                    Debug.Log($"[ModularAvatarDetector] Desactivado MA Mesh Settings en '{component.gameObject.name}'");
                }
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

        #endregion
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
