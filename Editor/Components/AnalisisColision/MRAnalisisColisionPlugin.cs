#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.AnalisisColision.MRAnalisisColisionPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.AnalisisColision
{
    /// <summary>
    /// Plugin NDMF para MRAnalisisColision.
    /// Se ejecuta en la fase Resolving, ANTES de que Modular Avatar procese el avatar.
    ///
    /// Acciones:
    /// - Desactiva componentes problematicos de MA en raiz de ropa
    /// - Desactiva componentes marcados por el usuario
    /// - Destruye el componente MRAnalisisColision
    /// </summary>
    public class MRAnalisisColisionPlugin : Plugin<MRAnalisisColisionPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.analisis_colision";
        public override string DisplayName => "MR Analisis Colision";

        // Color tema: Naranja (advertencia)
        public override Color? ThemeColor => new Color(0.9f, 0.6f, 0.2f, 1);

        protected override void Configure()
        {
            // Ejecutar en fase Resolving, ANTES de Modular Avatar
            // Esto es critico para que los componentes problematicos esten desactivados
            // cuando MA intente procesarlos
            InPhase(BuildPhase.Resolving)
                .BeforePlugin("nadena.dev.modular-avatar") // Antes de MA
                .Run(MRAnalisisColisionPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRAnalisisColision] Error durante el procesamiento NDMF: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que procesa los componentes de MA detectados.
    /// </summary>
    internal class MRAnalisisColisionPass : Pass<MRAnalisisColisionPass>
    {
        public override string DisplayName => "Analisis Colision (Desactivar MA)";

        protected override void Execute(BuildContext context)
        {
            Debug.Log($"[MRAnalisisColision NDMF] ========================================");
            Debug.Log($"[MRAnalisisColision NDMF] PLUGIN EJECUTÁNDOSE - Avatar: {context.AvatarRootObject.name}");
            Debug.Log($"[MRAnalisisColision NDMF] ========================================");

            // Buscar todos los componentes MRAnalisisColision en el avatar
            var analisisComponents = context.AvatarRootObject.GetComponentsInChildren<MRAnalisisColision>(true);

            if (analisisComponents.Length == 0)
            {
                Debug.Log($"[MRAnalisisColision NDMF] No se encontró ningún componente MRAnalisisColision en el avatar");
                return; // No hay nada que procesar
            }

            Debug.Log($"[MRAnalisisColision NDMF] Encontrados {analisisComponents.Length} componente(s) MRAnalisisColision");

            int totalDisabled = 0;

            foreach (var analisis in analisisComponents)
            {
                if (!analisis.enabled)
                {
                    Debug.Log($"[MRAnalisisColision NDMF] Saltando '{analisis.gameObject.name}' (deshabilitado)");
                    continue;
                }

                try
                {
                    // IMPORTANTE: NO re-escanear nunca en NDMF
                    // El escaneo debe hacerse en Edit Mode y las decisiones del usuario
                    // deben preservarse. Re-escanear aquí borraría todas las configuraciones.
                    if (!analisis.IsScanned)
                    {
                        Debug.LogWarning($"[MRAnalisisColision NDMF] El componente no fue escaneado en Edit Mode. " +
                            "Las decisiones del usuario no están disponibles. Saltando...");
                        continue;
                    }

                    // Log de debug: mostrar qué hay en el ScanResult
                    LogScanResultStatus(analisis);

                    // Desactivar componentes problematicos en raiz de ropa
                    if (analisis.AutoDisableProblematicOnRoot)
                    {
                        int disabled = DisableProblematicOnRoot(analisis, context.AvatarRootObject);
                        totalDisabled += disabled;

                        if (disabled > 0)
                        {
                            Debug.Log($"[MRAnalisisColision NDMF] Desactivados {disabled} componente(s) problematico(s) en raiz de ropa");
                        }
                    }

                    // Desactivar componentes marcados por el usuario
                    int userDisabled = DisableUserSelectedComponents(analisis, context.AvatarRootObject);
                    totalDisabled += userDisabled;

                    if (userDisabled > 0)
                    {
                        Debug.Log($"[MRAnalisisColision NDMF] Desactivados {userDisabled} componente(s) marcados por el usuario");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MRAnalisisColision NDMF] Error procesando '{analisis.gameObject.name}': {e.Message}");
                    Debug.LogException(e);
                }

                // Destruir el componente MRAnalisisColision despues de procesar
                UnityEngine.Object.DestroyImmediate(analisis);
            }

            Debug.Log($"[MRAnalisisColision NDMF] Procesamiento completado: {totalDisabled} componente(s) desactivado(s)");
        }

        /// <summary>
        /// Log de debug para ver el estado del ScanResult.
        /// </summary>
        private void LogScanResultStatus(MRAnalisisColision analisis)
        {
            var result = analisis.ScanResult;
            if (result == null)
            {
                Debug.LogWarning("[MRAnalisisColision NDMF] ScanResult es null");
                return;
            }

            Debug.Log($"[MRAnalisisColision NDMF] ====== ESTADO DEL SCANRESULT ======");
            Debug.Log($"[MRAnalisisColision NDMF] Total entradas: Problematic={result.ProblematicCount}, " +
                $"UserDecision={result.UserDecisionCount}, Compatible={result.CompatibleCount}");

            // Log detallado de TODAS las entradas problemáticas
            Debug.Log($"[MRAnalisisColision NDMF] --- Entradas Problematic ({result.ProblematicCount}) ---");
            foreach (var entry in result.ProblematicEntries)
            {
                Debug.Log($"[MRAnalisisColision NDMF]   - {entry.ComponentTypeName}: " +
                    $"UserWantsDisabled={entry.UserWantsDisabled}, " +
                    $"IsOnClothingRoot={entry.IsOnClothingRoot}, " +
                    $"IsValid={entry.IsValid}, " +
                    $"HasSearchableData={entry.HasSearchableData}, " +
                    $"GO='{entry.GameObjectName}', Path='{entry.HierarchyPath}'");
            }

            // Log detallado de TODAS las entradas UserDecision
            Debug.Log($"[MRAnalisisColision NDMF] --- Entradas UserDecision ({result.UserDecisionCount}) ---");
            foreach (var entry in result.UserDecisionEntries)
            {
                Debug.Log($"[MRAnalisisColision NDMF]   - {entry.ComponentTypeName}: " +
                    $"UserWantsDisabled={entry.UserWantsDisabled}, " +
                    $"IsValid={entry.IsValid}, " +
                    $"HasSearchableData={entry.HasSearchableData}, " +
                    $"GO='{entry.GameObjectName}', Path='{entry.HierarchyPath}'");
            }

            // Contar cuántas pasarán los filtros
            int problematicOnRoot = result.GetProblematicOnClothingRoot().Count();
            int userDecisionToDisable = result.GetUserDecisionToDisable().Count();
            int problematicToDisable = result.GetProblematicToDisable().Count();

            Debug.Log($"[MRAnalisisColision NDMF] --- FILTROS ---");
            Debug.Log($"[MRAnalisisColision NDMF] GetProblematicOnClothingRoot(): {problematicOnRoot} entradas");
            Debug.Log($"[MRAnalisisColision NDMF] GetUserDecisionToDisable(): {userDecisionToDisable} entradas");
            Debug.Log($"[MRAnalisisColision NDMF] GetProblematicToDisable(): {problematicToDisable} entradas");
            Debug.Log($"[MRAnalisisColision NDMF] ====================================");
        }

        /// <summary>
        /// Busca el componente equivalente en el clon del avatar.
        /// Usa múltiples estrategias de búsqueda para ser robusto.
        /// </summary>
        private MonoBehaviour FindComponentInClone(ColisionEntry entry, GameObject avatarClone)
        {
            if (entry == null || avatarClone == null)
            {
                Debug.LogWarning("[MRAnalisisColision NDMF] FindComponentInClone: entry o avatarClone es null");
                return null;
            }

            string typeName = entry.ComponentTypeName;
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("[MRAnalisisColision NDMF] FindComponentInClone: ComponentTypeName está vacío");
                return null;
            }

            // Estrategia 1: Buscar por HierarchyPath si está disponible
            if (!string.IsNullOrEmpty(entry.HierarchyPath))
            {
                var targetTransform = avatarClone.transform.Find(entry.HierarchyPath);
                if (targetTransform != null)
                {
                    var component = FindComponentByTypeName(targetTransform.gameObject, typeName);
                    if (component != null)
                    {
                        Debug.Log($"[MRAnalisisColision NDMF] Encontrado por path: {typeName} en {entry.HierarchyPath}");
                        return component;
                    }
                }
                else
                {
                    Debug.Log($"[MRAnalisisColision NDMF] Path no encontrado: '{entry.HierarchyPath}', intentando búsqueda alternativa...");
                }
            }

            // Estrategia 2: Buscar por nombre del GameObject original
            string gameObjectName = entry.GameObjectName;
            if (!string.IsNullOrEmpty(gameObjectName) && gameObjectName != "(null)")
            {
                // Buscar todos los GameObjects con ese nombre en el clon
                var matchingObjects = FindAllChildrenByName(avatarClone.transform, gameObjectName);

                foreach (var obj in matchingObjects)
                {
                    var component = FindComponentByTypeName(obj, typeName);
                    if (component != null)
                    {
                        Debug.Log($"[MRAnalisisColision NDMF] Encontrado por nombre de GameObject: {typeName} en {obj.name}");
                        return component;
                    }
                }
            }

            // Estrategia 3: Búsqueda global por tipo en todo el avatar
            var allComponents = avatarClone.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == typeName)
                {
                    // Si hay múltiples, intentar coincidir por nombre del GameObject
                    if (comp.gameObject.name == gameObjectName)
                    {
                        Debug.Log($"[MRAnalisisColision NDMF] Encontrado por búsqueda global (match exacto): {typeName} en {comp.gameObject.name}");
                        return comp;
                    }
                }
            }

            // Estrategia 4: Si no hay match exacto, usar el primero del tipo correcto
            foreach (var comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == typeName)
                {
                    Debug.Log($"[MRAnalisisColision NDMF] Encontrado por búsqueda global (primer match): {typeName} en {comp.gameObject.name}");
                    return comp;
                }
            }

            Debug.LogWarning($"[MRAnalisisColision NDMF] No se pudo encontrar {typeName} en el clon del avatar");
            return null;
        }

        /// <summary>
        /// Busca un componente por nombre de tipo en un GameObject.
        /// </summary>
        private MonoBehaviour FindComponentByTypeName(GameObject obj, string typeName)
        {
            if (obj == null || string.IsNullOrEmpty(typeName))
                return null;

            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType().Name == typeName)
                {
                    return comp;
                }
            }

            return null;
        }

        /// <summary>
        /// Encuentra todos los hijos (recursivo) con un nombre específico.
        /// </summary>
        private List<GameObject> FindAllChildrenByName(Transform root, string name)
        {
            var results = new List<GameObject>();
            FindAllChildrenByNameRecursive(root, name, results);
            return results;
        }

        private void FindAllChildrenByNameRecursive(Transform current, string name, List<GameObject> results)
        {
            if (current.name == name)
            {
                results.Add(current.gameObject);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                FindAllChildrenByNameRecursive(current.GetChild(i), name, results);
            }
        }

        /// <summary>
        /// DESTRUYE un componente en el clon del avatar.
        /// IMPORTANTE: Solo desactivar (enabled=false) NO funciona porque Modular Avatar
        /// procesa componentes incluso si están desactivados usando GetComponentsInChildren(true).
        /// Por eso debemos DESTRUIR el componente completamente.
        /// </summary>
        private bool DisableInClone(ColisionEntry entry, GameObject avatarClone, string reason)
        {
            var cloneComponent = FindComponentInClone(entry, avatarClone);

            if (cloneComponent == null)
            {
                Debug.LogWarning($"[MRAnalisisColision NDMF] No se pudo encontrar {entry.ShortTypeName} para destruir ({reason})");
                return false;
            }

            string gameObjectName = cloneComponent.gameObject.name;

            // DESTRUIR el componente, no solo desactivarlo
            UnityEngine.Object.DestroyImmediate(cloneComponent);

            Debug.Log($"[MRAnalisisColision NDMF] ✓ DESTRUIDO {entry.ShortTypeName} en '{gameObjectName}' ({reason})");
            return true;
        }

        /// <summary>
        /// Desactiva componentes problematicos en raiz de ropa.
        /// </summary>
        private int DisableProblematicOnRoot(MRAnalisisColision analisis, GameObject avatarClone)
        {
            if (analisis?.ScanResult == null) return 0;

            int count = 0;
            var entries = analisis.ScanResult.GetProblematicOnClothingRoot().ToList();

            Debug.Log($"[MRAnalisisColision NDMF] Procesando {entries.Count} componente(s) problemático(s) en raíz de ropa...");

            foreach (var entry in entries)
            {
                // No verificar IsValid aquí porque la referencia puede apuntar al original
                // En su lugar, intentamos encontrar el componente por nombre/tipo
                if (string.IsNullOrEmpty(entry.ComponentTypeName))
                {
                    Debug.LogWarning($"[MRAnalisisColision NDMF] Entry sin ComponentTypeName, saltando...");
                    continue;
                }

                if (DisableInClone(entry, avatarClone, "Problematic en raiz"))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Desactiva componentes marcados por el usuario (UserDecision y Problematic).
        /// </summary>
        private int DisableUserSelectedComponents(MRAnalisisColision analisis, GameObject avatarClone)
        {
            if (analisis?.ScanResult == null) return 0;

            int count = 0;

            // Desactivar UserDecision marcados
            var userDecisionEntries = analisis.ScanResult.GetUserDecisionToDisable().ToList();
            Debug.Log($"[MRAnalisisColision NDMF] Procesando {userDecisionEntries.Count} componente(s) UserDecision marcados para desactivar...");

            foreach (var entry in userDecisionEntries)
            {
                if (string.IsNullOrEmpty(entry.ComponentTypeName))
                    continue;

                if (DisableInClone(entry, avatarClone, "UserDecision marcado"))
                    count++;
            }

            // Desactivar Problematicos marcados por el usuario
            var problematicEntries = analisis.ScanResult.GetProblematicToDisable().ToList();
            Debug.Log($"[MRAnalisisColision NDMF] Procesando {problematicEntries.Count} componente(s) Problematic marcados para desactivar...");

            foreach (var entry in problematicEntries)
            {
                if (string.IsNullOrEmpty(entry.ComponentTypeName))
                    continue;

                if (DisableInClone(entry, avatarClone, "Problematic marcado"))
                    count++;
            }

            return count;
        }
    }
}
#endif
