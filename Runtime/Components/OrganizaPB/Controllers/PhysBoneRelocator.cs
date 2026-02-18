using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers
{
    /// <summary>
    /// Reubica los componentes VRCPhysBone y VRCPhysBoneCollider a contenedores organizados.
    /// </summary>
    public class PhysBoneRelocator
    {
        #region Constants

        public const string PHYSBONES_CONTAINER_NAME = "VRCPB";
        public const string COLLIDERS_CONTAINER_NAME = "VRCPBC";

        #endregion

        #region Private Fields

        private PhysBoneScanner _scanner;
        private Dictionary<OrganizationContext, GameObject> _physBonesContainers;
        private Dictionary<OrganizationContext, GameObject> _collidersContainers;
        private Dictionary<Component, Component> _colliderMapping;
        private Dictionary<Component, Component> _physBoneMapping;
        private HashSet<string> _usedNames;
        private List<Component> _componentsToDestroy;

        // Cache de FieldInfo para mejor rendimiento
        private FieldInfo _pbRootTransformField;
        private FieldInfo _colliderRootTransformField;
        private bool _fieldsResolved;

        // Configuración de la operación actual
        private bool _useUndo = true;
        private GameObject _avatarRoot;

        #endregion

        #region Constructor

        public PhysBoneRelocator()
        {
            _scanner = new PhysBoneScanner();
            _physBonesContainers = new Dictionary<OrganizationContext, GameObject>();
            _collidersContainers = new Dictionary<OrganizationContext, GameObject>();
            _colliderMapping = new Dictionary<Component, Component>();
            _physBoneMapping = new Dictionary<Component, Component>();
            _usedNames = new HashSet<string>();
            _componentsToDestroy = new List<Component>();
        }

        public PhysBoneRelocator(PhysBoneScanner scanner)
        {
            _scanner = scanner ?? new PhysBoneScanner();
            _physBonesContainers = new Dictionary<OrganizationContext, GameObject>();
            _collidersContainers = new Dictionary<OrganizationContext, GameObject>();
            _colliderMapping = new Dictionary<Component, Component>();
            _physBoneMapping = new Dictionary<Component, Component>();
            _usedNames = new HashSet<string>();
            _componentsToDestroy = new List<Component>();
        }

        #endregion

        #region Field Resolution

        private void EnsureFieldsResolved()
        {
            if (_fieldsResolved) return;

            if (_scanner.PhysBoneType != null)
            {
                _pbRootTransformField = _scanner.PhysBoneType.GetField("rootTransform",
                    BindingFlags.Public | BindingFlags.Instance);

                if (_pbRootTransformField == null)
                {
                    Debug.LogWarning("[PhysBoneRelocator] No se encontró campo rootTransform en VRCPhysBone");
                }
            }

            if (_scanner.PhysBoneColliderType != null)
            {
                _colliderRootTransformField = _scanner.PhysBoneColliderType.GetField("rootTransform",
                    BindingFlags.Public | BindingFlags.Instance);

                if (_colliderRootTransformField == null)
                {
                    Debug.LogWarning("[PhysBoneRelocator] No se encontró campo rootTransform en VRCPhysBoneCollider");
                }
            }

            _fieldsResolved = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reubica todos los PhysBones y Colliders incluidos.
        /// </summary>
        /// <param name="physBones">Lista de PhysBones a reubicar</param>
        /// <param name="colliders">Lista de Colliders a reubicar</param>
        /// <param name="avatarRoot">Avatar root para actualizar referencias externas</param>
        /// <param name="useUndo">Si true, registra operaciones para Undo (false durante NDMF build)</param>
        public OrganizationResult RelocateAll(List<PhysBoneEntry> physBones, List<ColliderEntry> colliders,
            GameObject avatarRoot = null, bool useUndo = true)
        {
            var result = new OrganizationResult();

            // Guardar configuración de la operación
            _useUndo = useUndo;
            _avatarRoot = avatarRoot;

            // Limpiar estado
            _physBonesContainers.Clear();
            _collidersContainers.Clear();
            _colliderMapping.Clear();
            _physBoneMapping.Clear();
            _usedNames.Clear();
            _componentsToDestroy.Clear();

            if (!_scanner.IsSDKAvailable)
            {
                result.AddError("VRChat SDK no disponible. No se pueden reubicar los PhysBones.");
                return result;
            }

            // Resolver campos antes de procesar
            EnsureFieldsResolved();

            try
            {
                // Primero procesar Colliders para tener el mapeo
                foreach (var collider in colliders)
                {
                    if (!collider.Included || collider.WasRelocated)
                    {
                        result.CollidersSkipped++;
                        continue;
                    }

                    if (!collider.IsValid)
                    {
                        result.AddWarning($"Collider inválido: {collider.GeneratedName}");
                        result.CollidersSkipped++;
                        continue;
                    }

                    if (RelocateCollider(collider, result))
                    {
                        result.CollidersRelocated++;
                    }
                    else
                    {
                        result.CollidersSkipped++;
                    }
                }

                // Luego procesar PhysBones
                foreach (var physBone in physBones)
                {
                    if (!physBone.Included || physBone.WasRelocated)
                    {
                        result.PhysBonesSkipped++;
                        continue;
                    }

                    if (!physBone.IsValid)
                    {
                        result.AddWarning($"PhysBone inválido: {physBone.GeneratedName}");
                        result.PhysBonesSkipped++;
                        continue;
                    }

                    if (RelocatePhysBone(physBone, result))
                    {
                        result.PhysBonesRelocated++;
                    }
                    else
                    {
                        result.PhysBonesSkipped++;
                    }
                }

                // Destruir los componentes originales ahora que ya fueron copiados
                DestroyOriginalComponents();

                // Actualizar referencias de colliders en PhysBones no escaneados/excluidos
                if (_avatarRoot != null && _colliderMapping.Count > 0)
                {
                    UpdateExternalPhysBoneColliderReferences();
                }

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception e)
            {
                result.AddError($"Error durante la reubicación: {e.Message}");
                Debug.LogException(e);
            }

            return result;
        }

        /// <summary>
        /// Revierte la reorganización, devolviendo los componentes a su ubicación original.
        /// Bug 1 fix: Revertir Colliders PRIMERO, luego PhysBones, y actualizar las
        /// referencias de colliders en los PhysBones revertidos.
        /// </summary>
        public OrganizationResult RevertAll(List<PhysBoneEntry> physBones, List<ColliderEntry> colliders)
        {
            var result = new OrganizationResult();

            if (!_scanner.IsSDKAvailable)
            {
                result.AddError("VRChat SDK no disponible.");
                return result;
            }

            EnsureFieldsResolved();

            try
            {
                // Bug 1 fix: Construir mapeo de colliders revertidos (relocated → reverted)
                var colliderRevertMapping = new Dictionary<Component, Component>();

                // Primero revertir Colliders (orden inverso al de organización)
                foreach (var collider in colliders)
                {
                    if (!collider.WasRelocated) continue;

                    var relocatedCollider = collider.RelocatedComponent;
                    if (RevertCollider(collider, result))
                    {
                        result.CollidersRelocated++;
                        // Guardar mapeo: componente reubicado → componente revertido
                        if (relocatedCollider != null)
                        {
                            colliderRevertMapping[relocatedCollider] = collider.OriginalComponent;
                        }
                    }
                }

                // Luego revertir PhysBones
                foreach (var physBone in physBones)
                {
                    if (!physBone.WasRelocated) continue;

                    if (RevertPhysBone(physBone, result))
                    {
                        result.PhysBonesRelocated++;

                        // Bug 1 fix: Actualizar referencias de colliders en el PhysBone revertido
                        if (colliderRevertMapping.Count > 0)
                        {
                            UpdateRevertedPhysBoneColliderReferences(physBone.OriginalComponent, colliderRevertMapping);
                        }
                    }
                }

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception e)
            {
                result.AddError($"Error durante la reversión: {e.Message}");
                Debug.LogException(e);
            }

            return result;
        }

        /// <summary>
        /// Obtiene todos los contenedores creados durante la organización.
        /// </summary>
        public IEnumerable<GameObject> GetCreatedContainers()
        {
            foreach (var container in _physBonesContainers.Values)
            {
                if (container != null)
                    yield return container;
            }

            foreach (var container in _collidersContainers.Values)
            {
                if (container != null)
                    yield return container;
            }
        }

        /// <summary>
        /// Obtiene todos los contenedores con su contexto de organización.
        /// </summary>
        public IEnumerable<(OrganizationContext context, GameObject container)> GetContainersWithContext()
        {
            foreach (var kvp in _physBonesContainers)
            {
                if (kvp.Key != null && kvp.Value != null)
                    yield return (kvp.Key, kvp.Value);
            }

            foreach (var kvp in _collidersContainers)
            {
                if (kvp.Key != null && kvp.Value != null)
                    yield return (kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Destruye todos los componentes originales marcados para destrucción.
        /// </summary>
        private void DestroyOriginalComponents()
        {
            foreach (var component in _componentsToDestroy)
            {
                if (component != null && component)
                {
                    try
                    {
                        SafeDestroyImmediate(component);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[PhysBoneRelocator] Error destruyendo componente: {e.Message}");
                    }
                }
            }

            _componentsToDestroy.Clear();
        }

        /// <summary>
        /// Destruye un objeto usando Undo si está disponible y habilitado.
        /// </summary>
        private void SafeDestroyImmediate(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (_useUndo)
            {
                Undo.DestroyObjectImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.DestroyImmediate(obj);
        }

        #endregion

        #region Collider Relocation

        private bool RelocateCollider(ColliderEntry entry, OrganizationResult result)
        {
            try
            {
                if (entry == null)
                {
                    result.AddWarning("Entry de collider es null");
                    return false;
                }

                if (entry.OriginalComponent == null || !entry.OriginalComponent)
                {
                    result.AddWarning($"Componente original es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                if (entry.Context == null)
                {
                    result.AddWarning($"Contexto es null para {entry.GeneratedName}");
                    return false;
                }

                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"OriginalTransform es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                // Obtener o crear contenedor
                var container = GetOrCreateContainer(entry.Context, COLLIDERS_CONTAINER_NAME, _collidersContainers);
                if (container == null)
                {
                    result.AddWarning($"No se pudo crear contenedor para {entry.GeneratedName}");
                    return false;
                }

                // Crear nuevo GameObject
                var uniqueName = GetUniqueName(entry.GeneratedName, container.transform);
                var newGameObject = new GameObject(uniqueName);
                newGameObject.transform.SetParent(container.transform);
                newGameObject.transform.localPosition = Vector3.zero;
                newGameObject.transform.localRotation = Quaternion.identity;
                newGameObject.transform.localScale = Vector3.one;

#if UNITY_EDITOR
                if (_useUndo)
                {
                    Undo.RegisterCreatedObjectUndo(newGameObject, "Organizar PhysBones");
                }
#endif

                // Determinar el rootTransform objetivo
                var targetRootTransform = entry.HadExplicitRootTransform
                    ? entry.RootTransform
                    : entry.OriginalTransform;

                // Copiar componente Y establecer rootTransform
                var newComponent = CopyComponentWithRootTransform(
                    entry.OriginalComponent,
                    newGameObject,
                    targetRootTransform,
                    _scanner.PhysBoneColliderType);

                if (newComponent == null)
                {
                    SafeDestroyImmediate(newGameObject);
                    result.AddWarning($"No se pudo copiar componente para {entry.GeneratedName}");
                    return false;
                }

                // Guardar referencias en la entrada para poder revertir
                entry.OriginalSiblingIndex = entry.OriginalTransform.GetSiblingIndex();
                entry.RelocatedGameObject = newGameObject;
                entry.RelocatedComponent = newComponent;

                // Guardar mapeo para actualizar referencias en PhysBones
                _colliderMapping[entry.OriginalComponent] = newComponent;

                // Marcar componente original para destrucción
                _componentsToDestroy.Add(entry.OriginalComponent);

                entry.WasRelocated = true;
                return true;
            }
            catch (Exception e)
            {
                result.AddWarning($"Error reubicando collider {entry.GeneratedName}: {e.Message}");
                Debug.LogError($"[PhysBoneRelocator] Excepción reubicando collider {entry.GeneratedName}:\n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        #endregion

        #region PhysBone Relocation

        private bool RelocatePhysBone(PhysBoneEntry entry, OrganizationResult result)
        {
            try
            {
                if (entry == null)
                {
                    result.AddWarning("Entry de PhysBone es null");
                    return false;
                }

                if (entry.OriginalComponent == null || !entry.OriginalComponent)
                {
                    result.AddWarning($"Componente original es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                if (entry.Context == null)
                {
                    result.AddWarning($"Contexto es null para {entry.GeneratedName}");
                    return false;
                }

                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"OriginalTransform es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                // Obtener o crear contenedor
                var container = GetOrCreateContainer(entry.Context, PHYSBONES_CONTAINER_NAME, _physBonesContainers);
                if (container == null)
                {
                    result.AddWarning($"No se pudo crear contenedor para {entry.GeneratedName}");
                    return false;
                }

                // Crear nuevo GameObject
                var uniqueName = GetUniqueName(entry.GeneratedName, container.transform);
                var newGameObject = new GameObject(uniqueName);
                newGameObject.transform.SetParent(container.transform);
                newGameObject.transform.localPosition = Vector3.zero;
                newGameObject.transform.localRotation = Quaternion.identity;
                newGameObject.transform.localScale = Vector3.one;

#if UNITY_EDITOR
                if (_useUndo)
                {
                    Undo.RegisterCreatedObjectUndo(newGameObject, "Organizar PhysBones");
                }
#endif

                // El rootTransform debe apuntar al hueso original
                var targetRootTransform = entry.HadExplicitRootTransform
                    ? entry.RootTransform
                    : entry.OriginalTransform;

                // Copiar componente Y establecer rootTransform
                var newComponent = CopyComponentWithRootTransform(
                    entry.OriginalComponent,
                    newGameObject,
                    targetRootTransform,
                    _scanner.PhysBoneType);

                if (newComponent == null)
                {
                    SafeDestroyImmediate(newGameObject);
                    result.AddWarning($"No se pudo copiar componente para {entry.GeneratedName}");
                    return false;
                }

                // Actualizar referencias a colliders
                UpdatePhysBoneColliderReferences(newComponent);

                // Guardar referencias en la entrada para poder revertir
                entry.OriginalSiblingIndex = entry.OriginalTransform.GetSiblingIndex();
                entry.RelocatedGameObject = newGameObject;
                entry.RelocatedComponent = newComponent;

                // Guardar mapeo
                _physBoneMapping[entry.OriginalComponent] = newComponent;

                // Marcar componente original para destrucción
                _componentsToDestroy.Add(entry.OriginalComponent);

                entry.WasRelocated = true;
                return true;
            }
            catch (Exception e)
            {
                result.AddWarning($"Error reubicando PhysBone {entry.GeneratedName}: {e.Message}");
                Debug.LogError($"[PhysBoneRelocator] Excepción reubicando PhysBone {entry.GeneratedName}:\n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        #endregion

        #region Container Management

        private GameObject GetOrCreateContainer(OrganizationContext context, string containerName,
            Dictionary<OrganizationContext, GameObject> cache)
        {
            if (context == null || context.ContextRoot == null)
            {
                Debug.LogWarning("[PhysBoneRelocator] Contexto inválido");
                return null;
            }

            // Verificar cache
            if (cache.TryGetValue(context, out var cached) && cached != null)
            {
                return cached;
            }

            // Determinar padre del contenedor (hermano del Armature)
            Transform containerParent;
            if (context.ArmatureTransform != null && context.ArmatureTransform.parent != null)
            {
                containerParent = context.ArmatureTransform.parent;
            }
            else
            {
                containerParent = context.ContextRoot.transform;
            }

            // Buscar contenedor existente
            var existing = containerParent.Find(containerName);
            if (existing != null)
            {
                // Reutilizar solo si está vacío (creado por ejecución anterior)
                if (existing.childCount == 0)
                {
                    cache[context] = existing.gameObject;
                    return existing.gameObject;
                }

                // Verificar si todos los hijos parecen ser de MROrganizaPB (prefijos PB_ o Col_)
                bool allMRChildren = true;
                foreach (Transform child in existing)
                {
                    if (!child.name.StartsWith("PB_") && !child.name.StartsWith("Col_"))
                    {
                        allMRChildren = false;
                        break;
                    }
                }

                if (allMRChildren)
                {
                    cache[context] = existing.gameObject;
                    return existing.gameObject;
                }

                // Contenedor existente tiene contenido ajeno → usar nombre alternativo
                containerName = GetUniqueContainerName(containerName, containerParent);
            }

            // Crear nuevo contenedor
            var container = new GameObject(containerName);
            container.transform.SetParent(containerParent);
            container.transform.SetSiblingIndex(GetArmatureSiblingIndex(context.ArmatureTransform) + 1);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            if (_useUndo)
            {
                Undo.RegisterCreatedObjectUndo(container, "Organizar PhysBones");
            }
#endif

            cache[context] = container;
            return container;
        }

        private int GetArmatureSiblingIndex(Transform armature)
        {
            if (armature == null) return 0;
            return armature.GetSiblingIndex();
        }

        private string GetUniqueContainerName(string baseName, Transform parent)
        {
            int counter = 1;
            while (counter <= 100)
            {
                var candidate = $"{baseName}_{counter}";
                if (parent.Find(candidate) == null)
                    return candidate;
                counter++;
            }
            return $"{baseName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        #endregion

        #region Component Operations

        /// <summary>
        /// Copia un componente y establece su rootTransform.
        /// </summary>
        private Component CopyComponentWithRootTransform(Component source, GameObject target, Transform newRootTransform, Type componentType)
        {
            if (source == null || target == null) return null;

            var sourceType = source.GetType();

            try
            {
                var newComponent = target.AddComponent(sourceType);
                if (newComponent == null) return null;

                // PRIMERO: Establecer rootTransform antes de copiar campos
                var rootTransformField = sourceType.GetField("rootTransform", BindingFlags.Public | BindingFlags.Instance);
                if (rootTransformField != null)
                {
                    rootTransformField.SetValue(newComponent, newRootTransform);
                }

                // SEGUNDO: Copiar todos los campos excepto rootTransform
                CopyAllFields(source, newComponent, sourceType, rootTransformField);

                // TERCERO: Verificar que rootTransform no fue sobrescrito
                if (rootTransformField != null)
                {
                    var finalValue = rootTransformField.GetValue(newComponent) as Transform;
                    if (finalValue != newRootTransform)
                    {
                        rootTransformField.SetValue(newComponent, newRootTransform);
                    }
                }

                // Copiar el estado enabled
                if (source is Behaviour srcBehaviour && newComponent is Behaviour newBehaviour)
                {
                    newBehaviour.enabled = srcBehaviour.enabled;
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(newComponent);
#endif

                return newComponent;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhysBoneRelocator] Error copiando componente: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Copia todos los campos de un componente via reflexión.
        /// Itera toda la cadena de herencia usando DeclaredOnly para evitar duplicados.
        /// </summary>
        /// <param name="excludeField">Campo a excluir de la copia (ej: rootTransform). Puede ser null.</param>
        private void CopyAllFields(Component source, Component target, Type type, FieldInfo excludeField = null)
        {
            var currentType = type;

            while (currentType != null && currentType != typeof(Component))
            {
                var fields = currentType.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    try
                    {
                        if (field.IsLiteral || field.IsInitOnly) continue;

                        // Saltar campo excluido
                        if (excludeField != null && field == excludeField) continue;
                        if (excludeField != null && field.Name.Equals("rootTransform", StringComparison.OrdinalIgnoreCase)) continue;

                        var value = field.GetValue(source);
                        field.SetValue(target, value);
                    }
                    catch
                    {
                        // Ignorar errores de campos individuales
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private void UpdatePhysBoneColliderReferences(Component physBone)
        {
            if (physBone == null || _scanner.PhysBoneType == null) return;
            if (_colliderMapping.Count == 0) return;

#if UNITY_EDITOR
            try
            {
                var serializedObject = new SerializedObject(physBone);
                var collidersProp = serializedObject.FindProperty("colliders");

                if (collidersProp != null && collidersProp.isArray)
                {
                    bool anyUpdated = false;

                    for (int i = 0; i < collidersProp.arraySize; i++)
                    {
                        var elementProp = collidersProp.GetArrayElementAtIndex(i);
                        var oldCollider = elementProp.objectReferenceValue as Component;

                        if (oldCollider != null && _colliderMapping.TryGetValue(oldCollider, out var newCollider))
                        {
                            elementProp.objectReferenceValue = newCollider;
                            anyUpdated = true;
                        }
                    }

                    if (anyUpdated)
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error actualizando colliders via SerializedObject: {e.Message}, intentando reflexión");
            }
#endif

            // Fallback: usar reflexión
            try
            {
                var collidersField = _scanner.PhysBoneType.GetField("colliders");
                if (collidersField == null) return;

                var collidersList = collidersField.GetValue(physBone);
                if (collidersList == null) return;

                var listType = collidersList.GetType();
                if (!listType.IsGenericType) return;

                var countProp = listType.GetProperty("Count");
                var itemProp = listType.GetProperty("Item");

                if (countProp == null || itemProp == null) return;

                int count = (int)countProp.GetValue(collidersList);

                for (int i = 0; i < count; i++)
                {
                    var oldCollider = itemProp.GetValue(collidersList, new object[] { i }) as Component;

                    if (oldCollider != null && _colliderMapping.TryGetValue(oldCollider, out var newCollider))
                    {
                        itemProp.SetValue(collidersList, newCollider, new object[] { i });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error actualizando referencias de colliders: {e.Message}");
            }
        }

        /// <summary>
        /// Bug 1 fix: Actualiza las referencias de colliders en un PhysBone revertido
        /// usando el mapeo de colliders (relocated → reverted).
        /// </summary>
        private void UpdateRevertedPhysBoneColliderReferences(Component physBone, Dictionary<Component, Component> colliderRevertMapping)
        {
            if (physBone == null || _scanner.PhysBoneType == null) return;

#if UNITY_EDITOR
            try
            {
                var serializedObject = new SerializedObject(physBone);
                var collidersProp = serializedObject.FindProperty("colliders");

                if (collidersProp != null && collidersProp.isArray)
                {
                    bool anyUpdated = false;

                    for (int i = 0; i < collidersProp.arraySize; i++)
                    {
                        var elementProp = collidersProp.GetArrayElementAtIndex(i);
                        var oldCollider = elementProp.objectReferenceValue as Component;

                        if (oldCollider != null && colliderRevertMapping.TryGetValue(oldCollider, out var revertedCollider))
                        {
                            elementProp.objectReferenceValue = revertedCollider;
                            anyUpdated = true;
                        }
                    }

                    if (anyUpdated)
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error actualizando colliders revertidos via SerializedObject: {e.Message}");
            }
#endif

            // Fallback: reflexión
            try
            {
                var collidersField = _scanner.PhysBoneType.GetField("colliders");
                if (collidersField == null) return;

                var collidersList = collidersField.GetValue(physBone);
                if (collidersList == null) return;

                var listType = collidersList.GetType();
                if (!listType.IsGenericType) return;

                var countProp = listType.GetProperty("Count");
                var itemProp = listType.GetProperty("Item");

                if (countProp == null || itemProp == null) return;

                int count = (int)countProp.GetValue(collidersList);

                for (int i = 0; i < count; i++)
                {
                    var oldCollider = itemProp.GetValue(collidersList, new object[] { i }) as Component;

                    if (oldCollider != null && colliderRevertMapping.TryGetValue(oldCollider, out var revertedCollider))
                    {
                        itemProp.SetValue(collidersList, revertedCollider, new object[] { i });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error actualizando colliders revertidos via reflexión: {e.Message}");
            }
        }

        /// <summary>
        /// Actualiza referencias de colliders en TODOS los VRCPhysBone del avatar,
        /// incluyendo los que no fueron escaneados o fueron excluidos.
        /// Esto asegura que PBs fuera de armatures o excluidos mantengan
        /// referencias válidas a colliders que fueron reubicados.
        /// </summary>
        private void UpdateExternalPhysBoneColliderReferences()
        {
            if (_avatarRoot == null || _scanner.PhysBoneType == null) return;
            if (_colliderMapping.Count == 0) return;

            var allPhysBones = _avatarRoot.GetComponentsInChildren(_scanner.PhysBoneType, true);

            foreach (var pb in allPhysBones)
            {
                // Saltar PBs que ya fueron procesados (están en _physBoneMapping como originales)
                if (_physBoneMapping.ContainsKey(pb) || _physBoneMapping.ContainsValue(pb))
                    continue;

                UpdatePhysBoneColliderReferences(pb);
            }
        }

        #endregion

        #region Name Generation

        private string GetUniqueName(string baseName, Transform parent)
        {
            var name = baseName;
            var key = $"{parent.GetInstanceID()}_{name}";

            if (!_usedNames.Contains(key) && parent.Find(name) == null)
            {
                _usedNames.Add(key);
                return name;
            }

            int counter = 1;
            while (true)
            {
                var newName = $"{baseName}_{counter}";
                var newKey = $"{parent.GetInstanceID()}_{newName}";

                if (!_usedNames.Contains(newKey) && parent.Find(newName) == null)
                {
                    _usedNames.Add(newKey);
                    return newName;
                }

                counter++;
                if (counter > 1000)
                {
                    return $"{baseName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }
            }
        }

        #endregion

        #region Revert Methods

        /// <summary>
        /// Revierte un Collider a su ubicación original.
        /// Bug 2 fix: Restaura rootTransform original en vez de limpiar incondicionalmente.
        /// Bug 3 fix: Copia el estado enabled.
        /// </summary>
        private bool RevertCollider(ColliderEntry entry, OrganizationResult result)
        {
            try
            {
                if (entry.RelocatedComponent == null || !entry.RelocatedComponent)
                {
                    result.AddWarning($"Componente reubicado no existe para {entry.GeneratedName}");
                    return false;
                }

                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"Transform original no existe para {entry.GeneratedName}");
                    return false;
                }

                // Crear el componente de vuelta en el GameObject original
                var newComponent = CopyComponentSimple(entry.RelocatedComponent, entry.OriginalTransform.gameObject);
                if (newComponent == null)
                {
                    result.AddWarning($"No se pudo restaurar componente para {entry.GeneratedName}");
                    return false;
                }

                // Restaurar rootTransform original
                RestoreRootTransform(newComponent, entry, _scanner.PhysBoneColliderType);

                // Destruir el GameObject reubicado
                if (entry.RelocatedGameObject != null)
                {
                    SafeDestroyImmediate(entry.RelocatedGameObject);
                }

                // Restaurar referencias
                entry.OriginalComponent = newComponent;
                entry.RelocatedComponent = null;
                entry.RelocatedGameObject = null;
                entry.WasRelocated = false;
                return true;
            }
            catch (Exception e)
            {
                result.AddWarning($"Error revirtiendo collider {entry.GeneratedName}: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// Revierte un PhysBone a su ubicación original.
        /// Bug 2 fix: Restaura rootTransform original en vez de limpiar incondicionalmente.
        /// Bug 3 fix: Copia el estado enabled.
        /// </summary>
        private bool RevertPhysBone(PhysBoneEntry entry, OrganizationResult result)
        {
            try
            {
                if (entry.RelocatedComponent == null || !entry.RelocatedComponent)
                {
                    result.AddWarning($"Componente reubicado no existe para {entry.GeneratedName}");
                    return false;
                }

                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"Transform original no existe para {entry.GeneratedName}");
                    return false;
                }

                // Crear el componente de vuelta en el GameObject original
                var newComponent = CopyComponentSimple(entry.RelocatedComponent, entry.OriginalTransform.gameObject);
                if (newComponent == null)
                {
                    result.AddWarning($"No se pudo restaurar componente para {entry.GeneratedName}");
                    return false;
                }

                // Restaurar rootTransform original
                RestoreRootTransform(newComponent, entry, _scanner.PhysBoneType);

                // Destruir el GameObject reubicado
                if (entry.RelocatedGameObject != null)
                {
                    SafeDestroyImmediate(entry.RelocatedGameObject);
                }

                // Restaurar referencias
                entry.OriginalComponent = newComponent;
                entry.RelocatedComponent = null;
                entry.RelocatedGameObject = null;
                entry.WasRelocated = false;
                return true;
            }
            catch (Exception e)
            {
                result.AddWarning($"Error revirtiendo PhysBone {entry.GeneratedName}: {e.Message}");
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// Copia un componente de forma simple (sin modificar rootTransform).
        /// Usa reflexión para copiar todos los campos.
        /// </summary>
        private Component CopyComponentSimple(Component source, GameObject target)
        {
            if (source == null || target == null) return null;

            var sourceType = source.GetType();

            try
            {
                var newComponent = target.AddComponent(sourceType);
                if (newComponent == null) return null;

                // Copiar todos los campos (sin excluir ninguno)
                CopyAllFields(source, newComponent, sourceType);

                // Copiar estado enabled
                if (source is Behaviour srcBehaviour && newComponent is Behaviour newBehaviour)
                {
                    newBehaviour.enabled = srcBehaviour.enabled;
                }

                return newComponent;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error copiando componente: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Restaura el rootTransform original de un componente.
        /// Usa HadExplicitRootTransform para determinar si poner null o restaurar el valor.
        /// </summary>
        private void RestoreRootTransform(Component component, ComponentEntry entry, Type componentType)
        {
            if (component == null || componentType == null) return;

            try
            {
                var rootTransformField = componentType.GetField("rootTransform",
                    BindingFlags.Public | BindingFlags.Instance);

                if (rootTransformField == null) return;

                if (!entry.HadExplicitRootTransform)
                {
                    // No tenía rootTransform explícito → poner null
                    rootTransformField.SetValue(component, null);
                }
                else
                {
                    // Tenía un rootTransform explícito → restaurar el valor original
                    rootTransformField.SetValue(component, entry.RootTransform);
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(component);
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error restaurando rootTransform: {e.Message}");
            }
        }

        #endregion
    }
}
