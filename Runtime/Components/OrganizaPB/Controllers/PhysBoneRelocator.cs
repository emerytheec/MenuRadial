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

        public const string PHYSBONES_CONTAINER_NAME = "PhysBones";
        public const string COLLIDERS_CONTAINER_NAME = "Colliders";

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
        /// Reubica todos los PhysBones y Colliders habilitados.
        /// </summary>
        public OrganizationResult RelocateAll(List<PhysBoneEntry> physBones, List<ColliderEntry> colliders)
        {
            var result = new OrganizationResult();

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
                    if (!collider.Enabled || collider.WasRelocated)
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
                    if (!physBone.Enabled || physBone.WasRelocated)
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
                // Primero revertir PhysBones (para actualizar referencias a colliders)
                foreach (var physBone in physBones)
                {
                    if (!physBone.WasRelocated) continue;

                    if (RevertPhysBone(physBone, result))
                    {
                        result.PhysBonesRelocated++;
                    }
                }

                // Luego revertir Colliders
                foreach (var collider in colliders)
                {
                    if (!collider.WasRelocated) continue;

                    if (RevertCollider(collider, result))
                    {
                        result.CollidersRelocated++;
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
        /// Destruye todos los componentes originales marcados para destrucción.
        /// </summary>
        private void DestroyOriginalComponents()
        {
            Debug.Log($"[PhysBoneRelocator] Destruyendo {_componentsToDestroy.Count} componentes originales...");

            foreach (var component in _componentsToDestroy)
            {
                if (component != null && component)
                {
                    try
                    {
                        UnityEngine.Object.DestroyImmediate(component);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[PhysBoneRelocator] Error destruyendo componente: {e.Message}");
                    }
                }
            }

            _componentsToDestroy.Clear();
        }

        #endregion

        #region Collider Relocation

        private bool RelocateCollider(ColliderEntry entry, OrganizationResult result)
        {
            try
            {
                // Validación adicional
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

                // Verificar que OriginalTransform no esté destruido
                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"OriginalTransform es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                Debug.Log($"[PhysBoneRelocator] Iniciando reubicación de collider: {entry.GeneratedName}");

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

                // Determinar el rootTransform objetivo ANTES de copiar
                // entry.RootTransform = valor del campo rootTransform del collider original (puede ser null)
                // entry.OriginalTransform = el Transform donde estaba el componente original
                var targetRootTransform = entry.RootTransform;

                Debug.Log($"[PhysBoneRelocator] Collider '{entry.GeneratedName}' - DIAGNÓSTICO:");
                Debug.Log($"  entry.OriginalTransform = {SafeGetName(entry.OriginalTransform)} (GameObject donde estaba el collider)");
                Debug.Log($"  entry.RootTransform = {SafeGetName(entry.RootTransform)} (valor del campo rootTransform original)");

                // Si RootTransform es null, usar OriginalTransform
                if (targetRootTransform == null)
                {
                    Debug.Log($"  -> entry.RootTransform es null, usando entry.OriginalTransform");
                    targetRootTransform = entry.OriginalTransform;
                }

                Debug.Log($"  -> targetRootTransform OBJETIVO = {SafeGetName(targetRootTransform)}");

                // Copiar componente Y establecer rootTransform en un solo paso
                // Esto evita que el SDK sobrescriba el valor
                var newComponent = CopyComponentWithRootTransform(
                    entry.OriginalComponent,
                    newGameObject,
                    targetRootTransform,
                    _scanner.PhysBoneColliderType);

                if (newComponent == null)
                {
                    UnityEngine.Object.DestroyImmediate(newGameObject);
                    result.AddWarning($"No se pudo copiar componente para {entry.GeneratedName}");
                    return false;
                }

                Debug.Log($"  newComponent.transform = {SafeGetName(newComponent.transform)} (nuevo GameObject)");

                // Guardar referencias en la entrada para poder revertir
                entry.OriginalSiblingIndex = entry.OriginalTransform.GetSiblingIndex();
                entry.RelocatedGameObject = newGameObject;
                entry.RelocatedComponent = newComponent;

                // Guardar mapeo para actualizar referencias en PhysBones
                _colliderMapping[entry.OriginalComponent] = newComponent;

                // Marcar componente original para destrucción
                _componentsToDestroy.Add(entry.OriginalComponent);

                entry.WasRelocated = true;

                Debug.Log($"[PhysBoneRelocator] Collider reubicado: {entry.GeneratedName} -> {container.name}/{uniqueName}");
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
                // Validación adicional
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

                // Verificar que OriginalTransform no esté destruido
                if (entry.OriginalTransform == null || !entry.OriginalTransform)
                {
                    result.AddWarning($"OriginalTransform es null o destruido para {entry.GeneratedName}");
                    return false;
                }

                Debug.Log($"[PhysBoneRelocator] Iniciando reubicación de PhysBone: {entry.GeneratedName}");

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

                // El rootTransform debe apuntar al hueso original donde estaba el componente
                // Si entry.RootTransform es null o inválido, usar OriginalTransform
                var targetRootTransform = entry.RootTransform;
                if (targetRootTransform == null)
                {
                    targetRootTransform = entry.OriginalTransform;
                }

                Debug.Log($"[PhysBoneRelocator] PhysBone '{entry.GeneratedName}': " +
                          $"OriginalTransform={SafeGetName(entry.OriginalTransform)}, " +
                          $"RootTransform={SafeGetName(entry.RootTransform)}, " +
                          $"TargetRoot={SafeGetName(targetRootTransform)}");

                // Copiar componente Y establecer rootTransform en un solo paso
                var newComponent = CopyComponentWithRootTransform(
                    entry.OriginalComponent,
                    newGameObject,
                    targetRootTransform,
                    _scanner.PhysBoneType);

                if (newComponent == null)
                {
                    UnityEngine.Object.DestroyImmediate(newGameObject);
                    result.AddWarning($"No se pudo copiar componente para {entry.GeneratedName}");
                    return false;
                }

                // Actualizar referencias a colliders
                UpdatePhysBoneColliderReferences(newComponent);

                // Guardar referencias en la entrada para poder revertir
                entry.OriginalSiblingIndex = entry.OriginalTransform.GetSiblingIndex();
                entry.RelocatedGameObject = newGameObject;
                entry.RelocatedComponent = newComponent;

                // Guardar mapeo para actualizar referencias en VRCConstraints
                _physBoneMapping[entry.OriginalComponent] = newComponent;

                // Marcar componente original para destrucción
                _componentsToDestroy.Add(entry.OriginalComponent);

                entry.WasRelocated = true;

                Debug.Log($"[PhysBoneRelocator] PhysBone reubicado: {entry.GeneratedName} -> {container.name}/{uniqueName}");
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
                cache[context] = existing.gameObject;
                return existing.gameObject;
            }

            // Crear nuevo contenedor
            var container = new GameObject(containerName);
            container.transform.SetParent(containerParent);
            container.transform.SetSiblingIndex(GetArmatureSiblingIndex(context.ArmatureTransform) + 1);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;

            cache[context] = container;

            Debug.Log($"[PhysBoneRelocator] Contenedor creado: {containerParent.name}/{containerName}");
            return container;
        }

        private int GetArmatureSiblingIndex(Transform armature)
        {
            if (armature == null) return 0;
            return armature.GetSiblingIndex();
        }

        #endregion

        #region Component Operations

        /// <summary>
        /// Copia un componente y establece su rootTransform.
        /// IMPORTANTE: rootTransform se establece DURANTE la copia para evitar que VRChat SDK lo reinicialice.
        /// </summary>
        private Component CopyComponentWithRootTransform(Component source, GameObject target, Transform newRootTransform, Type componentType)
        {
            if (source == null || target == null)
            {
                Debug.LogWarning($"[PhysBoneRelocator] CopyComponentWithRootTransform: source o target es null");
                return null;
            }

            var sourceType = source.GetType();
            Debug.Log($"[PhysBoneRelocator] CopyComponentWithRootTransform: copiando {sourceType.Name} de {SafeGetName(source.gameObject)} a {SafeGetName(target)}");
            Debug.Log($"[PhysBoneRelocator] rootTransform objetivo: {SafeGetName(newRootTransform)}");

            // NUEVO ENFOQUE: Crear componente via AddComponent y copiar campos manualmente,
            // estableciendo rootTransform ANTES de copiar otros campos
            try
            {
                var newComponent = target.AddComponent(sourceType);
                if (newComponent == null)
                {
                    Debug.LogWarning($"[PhysBoneRelocator] CopyComponentWithRootTransform: AddComponent devolvió null para {sourceType.Name}");
                    return null;
                }

                // PRIMERO: Establecer rootTransform INMEDIATAMENTE después de crear el componente
                var rootTransformField = sourceType.GetField("rootTransform", BindingFlags.Public | BindingFlags.Instance);
                if (rootTransformField != null)
                {
                    Debug.Log($"[PhysBoneRelocator] Estableciendo rootTransform ANTES de copiar otros campos");
                    rootTransformField.SetValue(newComponent, newRootTransform);
                    var checkValue = rootTransformField.GetValue(newComponent) as Transform;
                    Debug.Log($"[PhysBoneRelocator] rootTransform establecido a: {SafeGetName(checkValue)}");
                }

                // SEGUNDO: Copiar TODOS los campos EXCEPTO rootTransform
                CopyFieldsExcludingRootTransform(source, newComponent, sourceType, rootTransformField);

                // TERCERO: Verificar que rootTransform NO fue sobrescrito
                if (rootTransformField != null)
                {
                    var finalValue = rootTransformField.GetValue(newComponent) as Transform;
                    Debug.Log($"[PhysBoneRelocator] rootTransform FINAL después de copiar campos: {SafeGetName(finalValue)}");

                    // Si fue sobrescrito, volver a establecerlo
                    if (finalValue != newRootTransform)
                    {
                        Debug.LogWarning($"[PhysBoneRelocator] rootTransform fue sobrescrito durante la copia! Re-estableciendo...");
                        rootTransformField.SetValue(newComponent, newRootTransform);
                        var recheck = rootTransformField.GetValue(newComponent) as Transform;
                        Debug.Log($"[PhysBoneRelocator] rootTransform re-establecido a: {SafeGetName(recheck)}");
                    }
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(newComponent);
#endif

                Debug.Log($"[PhysBoneRelocator] CopyComponentWithRootTransform: componente copiado exitosamente");
                return newComponent;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhysBoneRelocator] CopyComponentWithRootTransform: Error: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Copia todos los campos excepto rootTransform.
        /// </summary>
        private void CopyFieldsExcludingRootTransform(Component source, Component target, Type type, FieldInfo rootTransformField)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    // Saltar campos de solo lectura o constantes
                    if (field.IsLiteral || field.IsInitOnly) continue;

                    // IMPORTANTE: Saltar el campo rootTransform
                    if (rootTransformField != null && field == rootTransformField)
                    {
                        Debug.Log($"[PhysBoneRelocator] Saltando campo rootTransform durante la copia");
                        continue;
                    }

                    // Saltar campos que contengan "rootTransform" en el nombre (por si acaso)
                    if (field.Name.ToLower().Contains("roottransform"))
                    {
                        Debug.Log($"[PhysBoneRelocator] Saltando campo '{field.Name}' (contiene rootTransform)");
                        continue;
                    }

                    var value = field.GetValue(source);
                    field.SetValue(target, value);
                }
                catch
                {
                    // Ignorar errores de campos individuales
                }
            }
        }

        private void CopyFieldsViaReflection(Component source, Component target, Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    // Saltar campos de solo lectura o constantes
                    if (field.IsLiteral || field.IsInitOnly) continue;

                    var value = field.GetValue(source);
                    field.SetValue(target, value);
                }
                catch
                {
                    // Ignorar errores de campos individuales
                }
            }
        }

        private void SetRootTransform(Component component, Transform rootTransform, Type componentType)
        {
            if (component == null || componentType == null) return;

            Debug.Log($"[PhysBoneRelocator] SetRootTransform: component={SafeGetName(component)}, " +
                      $"targetRoot={SafeGetName(rootTransform)}, type={componentType.Name}");

            // NOTA: Usamos SOLO reflexión directa porque SerializedObject no persiste
            // correctamente en el contexto de NDMF build.
            try
            {
                var rootTransformField = componentType.GetField("rootTransform",
                    BindingFlags.Public | BindingFlags.Instance);

                if (rootTransformField == null)
                {
                    // Intentar con NonPublic también
                    rootTransformField = componentType.GetField("rootTransform",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (rootTransformField != null)
                {
                    var valueBefore = rootTransformField.GetValue(component) as Transform;
                    Debug.Log($"[PhysBoneRelocator] Reflexión - Valor ANTES: {SafeGetName(valueBefore)}");

                    // Establecer el valor
                    rootTransformField.SetValue(component, rootTransform);

                    // Verificar inmediatamente
                    var valueAfter = rootTransformField.GetValue(component) as Transform;
                    Debug.Log($"[PhysBoneRelocator] Reflexión - Valor DESPUÉS: {SafeGetName(valueAfter)}");

                    if (valueAfter == rootTransform)
                    {
                        Debug.Log($"[PhysBoneRelocator] rootTransform configurado EXITOSAMENTE");
                    }
                    else
                    {
                        Debug.LogError($"[PhysBoneRelocator] El valor NO se estableció correctamente!");
                        Debug.LogError($"  Esperado: {SafeGetName(rootTransform)}");
                        Debug.LogError($"  Actual: {SafeGetName(valueAfter)}");
                    }

#if UNITY_EDITOR
                    // Marcar el objeto como dirty para que Unity guarde los cambios
                    UnityEditor.EditorUtility.SetDirty(component);
#endif
                }
                else
                {
                    Debug.LogError($"[PhysBoneRelocator] No se encontró el campo 'rootTransform' en {componentType.Name}");

                    // Debug: listar todos los campos disponibles
                    var allFields = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Debug.Log($"[PhysBoneRelocator] Campos disponibles en {componentType.Name}:");
                    foreach (var f in allFields)
                    {
                        if (f.FieldType == typeof(Transform))
                        {
                            Debug.Log($"  - {f.Name} (Transform)");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhysBoneRelocator] Error configurando rootTransform: {e.Message}\n{e.StackTrace}");
            }
        }

        private void UpdatePhysBoneColliderReferences(Component physBone)
        {
            if (physBone == null || _scanner.PhysBoneType == null) return;

            if (_colliderMapping.Count == 0)
            {
                Debug.Log("[PhysBoneRelocator] No hay mapeo de colliders para actualizar");
                return;
            }

#if UNITY_EDITOR
            // En editor, usar SerializedObject para actualizar referencias de colliders
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
                            Debug.Log($"[PhysBoneRelocator] Referencia de collider actualizada: {SafeGetName(oldCollider?.gameObject)} -> {SafeGetName(newCollider?.gameObject)}");
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

                // Verificar el tipo de la lista
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
                        Debug.Log($"[PhysBoneRelocator] Referencia de collider actualizada via reflexión");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error actualizando referencias de colliders: {e.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Obtiene el nombre de un objeto de Unity de forma segura, manejando objetos destruidos.
        /// </summary>
        private string SafeGetName(UnityEngine.Object obj)
        {
            if (obj == null) return "null";

            try
            {
                // Verificar si el objeto ha sido destruido (Unity fake null)
                if (!obj) return "destroyed";
                return obj.name;
            }
            catch
            {
                return "error";
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
                    // Prevenir loop infinito
                    return $"{baseName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }
            }
        }

        #endregion

        #region Revert Methods

        /// <summary>
        /// Revierte un Collider a su ubicación original.
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

                Debug.Log($"[PhysBoneRelocator] Revirtiendo collider: {entry.GeneratedName}");

                // Crear el componente de vuelta en el GameObject original
                var newComponent = CopyComponentSimple(entry.RelocatedComponent, entry.OriginalTransform.gameObject);
                if (newComponent == null)
                {
                    result.AddWarning($"No se pudo restaurar componente para {entry.GeneratedName}");
                    return false;
                }

                // Limpiar rootTransform ya que ahora está en el hueso original
                ClearRootTransform(newComponent, _scanner.PhysBoneColliderType);

                // Destruir el GameObject del contenedor
                if (entry.RelocatedGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(entry.RelocatedGameObject);
                }

                // Restaurar referencias
                entry.OriginalComponent = newComponent;
                entry.RelocatedComponent = null;
                entry.RelocatedGameObject = null;
                entry.WasRelocated = false;

                Debug.Log($"[PhysBoneRelocator] Collider revertido: {entry.GeneratedName}");
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

                Debug.Log($"[PhysBoneRelocator] Revirtiendo PhysBone: {entry.GeneratedName}");

                // Crear el componente de vuelta en el GameObject original
                var newComponent = CopyComponentSimple(entry.RelocatedComponent, entry.OriginalTransform.gameObject);
                if (newComponent == null)
                {
                    result.AddWarning($"No se pudo restaurar componente para {entry.GeneratedName}");
                    return false;
                }

                // Limpiar rootTransform ya que ahora está en el hueso original
                ClearRootTransform(newComponent, _scanner.PhysBoneType);

                // Destruir el GameObject del contenedor
                if (entry.RelocatedGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(entry.RelocatedGameObject);
                }

                // Restaurar referencias
                entry.OriginalComponent = newComponent;
                entry.RelocatedComponent = null;
                entry.RelocatedGameObject = null;
                entry.WasRelocated = false;

                Debug.Log($"[PhysBoneRelocator] PhysBone revertido: {entry.GeneratedName}");
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
        /// Copia un componente de forma simple (sin establecer rootTransform).
        /// </summary>
        private Component CopyComponentSimple(Component source, GameObject target)
        {
            if (source == null || target == null) return null;

            var sourceType = source.GetType();

#if UNITY_EDITOR
            try
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(source);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);

                var components = target.GetComponents(sourceType);
                if (components.Length > 0)
                {
                    return components[components.Length - 1];
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error copiando componente: {e.Message}");
            }
#endif

            // Fallback: reflexión
            try
            {
                var newComponent = target.AddComponent(sourceType);
                if (newComponent != null)
                {
                    var fields = sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.IsLiteral || field.IsInitOnly) continue;
                        try
                        {
                            var value = field.GetValue(source);
                            field.SetValue(newComponent, value);
                        }
                        catch { }
                    }
                    return newComponent;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Limpia el campo rootTransform de un componente (lo pone a null).
        /// </summary>
        private void ClearRootTransform(Component component, Type componentType)
        {
            if (component == null || componentType == null) return;

            try
            {
                var rootTransformField = componentType.GetField("rootTransform",
                    BindingFlags.Public | BindingFlags.Instance);

                if (rootTransformField != null)
                {
                    rootTransformField.SetValue(component, null);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(component);
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneRelocator] Error limpiando rootTransform: {e.Message}");
            }
        }

        #endregion
    }
}
