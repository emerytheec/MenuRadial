using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers
{
    /// <summary>
    /// Escanea el avatar para detectar VRCPhysBone y VRCPhysBoneCollider.
    /// Usa reflexión para evitar dependencia directa del VRChat SDK.
    /// Solo detecta componentes que están DENTRO de armatures.
    /// </summary>
    public class PhysBoneScanner
    {
        #region Constants

        private const string PHYSBONE_FULL_TYPE = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";
        private const string PHYSBONE_COLLIDER_FULL_TYPE = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider";

        #endregion

        #region Private Fields

        private Type _physBoneType;
        private Type _physBoneColliderType;
        private bool _typesResolved;

        private FieldInfo _pbRootTransformField;
        private FieldInfo _pbCollidersField;
        private FieldInfo _pbIgnoreTransformsField;
        private FieldInfo _colliderRootTransformField;

        // Armatures pre-detectadas
        private List<ArmatureContext> _detectedArmatures;

        #endregion

        #region Inner Types

        /// <summary>
        /// Información de una armature detectada y su contexto.
        /// </summary>
        private class ArmatureContext
        {
            public GameObject ContextRoot;
            public Transform ArmatureTransform;
            public bool IsAvatarContext;
            public string ContextName;
        }

        /// <summary>
        /// Armature conocida externamente (ej: detectada por MRCoserRopa).
        /// </summary>
        public struct KnownArmature
        {
            public GameObject Root;
            public Transform Armature;
            public string Name;
        }

        #endregion

        #region Properties

        public bool IsSDKAvailable
        {
            get
            {
                EnsureTypesResolved();
                return _physBoneType != null;
            }
        }

        public Type PhysBoneType
        {
            get
            {
                EnsureTypesResolved();
                return _physBoneType;
            }
        }

        public Type PhysBoneColliderType
        {
            get
            {
                EnsureTypesResolved();
                return _physBoneColliderType;
            }
        }

        #endregion

        #region Constructor

        public PhysBoneScanner() { }

        #endregion

        #region Type Resolution

        private void EnsureTypesResolved()
        {
            if (_typesResolved) return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_physBoneType == null)
                {
                    _physBoneType = assembly.GetType(PHYSBONE_FULL_TYPE);
                }

                if (_physBoneColliderType == null)
                {
                    _physBoneColliderType = assembly.GetType(PHYSBONE_COLLIDER_FULL_TYPE);
                }

                if (_physBoneType != null && _physBoneColliderType != null)
                    break;
            }

            // Cache field info
            if (_physBoneType != null)
            {
                _pbRootTransformField = _physBoneType.GetField("rootTransform");
                _pbCollidersField = _physBoneType.GetField("colliders");
                _pbIgnoreTransformsField = _physBoneType.GetField("ignoreTransforms");
            }

            if (_physBoneColliderType != null)
            {
                _colliderRootTransformField = _physBoneColliderType.GetField("rootTransform");
            }

            _typesResolved = true;
        }

        #endregion

        #region Armature Detection

        /// <summary>
        /// Pre-detecta todas las armatures relevantes en el avatar.
        /// Incluye la armature del avatar y las armatures de hijos directos (ropas).
        /// </summary>
        private void DetectArmatures(GameObject avatarRoot, IReadOnlyList<KnownArmature> knownArmatures = null)
        {
            _detectedArmatures = new List<ArmatureContext>();

            if (avatarRoot == null) return;

            // Obtener Animator del avatar para Humanoid API
            var animator = avatarRoot.GetComponent<Animator>();

            // 1. Armature del avatar
            var avatarResult = ArmatureFinder.FindArmature(avatarRoot.transform, animator);
            if (avatarResult.Success)
            {
                _detectedArmatures.Add(new ArmatureContext
                {
                    ContextRoot = avatarRoot,
                    ArmatureTransform = avatarResult.Armature,
                    IsAvatarContext = true,
                    ContextName = "Avatar"
                });
            }

            // 2. Armatures de hijos directos del avatar root (ropas)
            foreach (Transform child in avatarRoot.transform)
            {
                // Saltar si es la propia armature del avatar
                if (avatarResult.Success && child == avatarResult.Armature)
                    continue;

                // Saltar si no tiene hijos (no puede ser un contenedor de ropa)
                if (child.childCount == 0)
                    continue;

                var childResult = ArmatureFinder.FindArmature(child);
                if (childResult.Success)
                {
                    _detectedArmatures.Add(new ArmatureContext
                    {
                        ContextRoot = child.gameObject,
                        ArmatureTransform = childResult.Armature,
                        IsAvatarContext = false,
                        ContextName = child.name
                    });
                }
            }

            // 3. Armatures conocidas externamente (ej: de MRCoserRopa, WigDetector)
            if (knownArmatures != null)
            {
                foreach (var known in knownArmatures)
                {
                    if (known.Root == null) continue;

                    // Usar Root.transform como pseudo-armature si no tiene armature propia
                    // (ej: pelucas con MA BoneProxy sin armature independiente)
                    Transform armatureTransform = known.Armature != null
                        ? known.Armature
                        : known.Root.transform;

                    bool alreadyDetected = false;
                    foreach (var existing in _detectedArmatures)
                    {
                        if (existing.ArmatureTransform == armatureTransform)
                        {
                            alreadyDetected = true;
                            break;
                        }
                    }

                    if (!alreadyDetected)
                    {
                        _detectedArmatures.Add(new ArmatureContext
                        {
                            ContextRoot = known.Root,
                            ArmatureTransform = armatureTransform,
                            IsAvatarContext = false,
                            ContextName = known.Name
                        });
                    }
                }
            }

            Debug.Log($"[PhysBoneScanner] Armatures detectadas: {_detectedArmatures.Count}");
        }

        /// <summary>
        /// Encuentra el contexto de armature para un transform dado.
        /// Retorna null si el transform no está dentro de ninguna armature.
        /// </summary>
        private ArmatureContext FindArmatureContextFor(Transform transform)
        {
            if (_detectedArmatures == null) return null;

            foreach (var ctx in _detectedArmatures)
            {
                if (ctx.ArmatureTransform != null && transform.IsChildOf(ctx.ArmatureTransform))
                {
                    return ctx;
                }
            }

            return null;
        }

        /// <summary>
        /// Crea un OrganizationContext a partir de un ArmatureContext.
        /// </summary>
        private OrganizationContext CreateOrganizationContext(ArmatureContext armCtx)
        {
            return new OrganizationContext(
                armCtx.ContextRoot,
                armCtx.ArmatureTransform,
                armCtx.ContextName,
                armCtx.IsAvatarContext
            );
        }

        /// <summary>
        /// Encuentra el contexto para un transform que está FUERA de cualquier armature.
        /// Prioriza contextos más específicos (ropas antes que avatar).
        /// </summary>
        private ArmatureContext FindContextRootFor(Transform transform)
        {
            if (_detectedArmatures == null) return null;

            ArmatureContext avatarCtx = null;

            foreach (var ctx in _detectedArmatures)
            {
                if (ctx.ContextRoot == null) continue;

                if (!transform.IsChildOf(ctx.ContextRoot.transform)) continue;

                // Si es contexto de avatar, guardar como fallback
                if (ctx.IsAvatarContext)
                {
                    avatarCtx = ctx;
                    continue;
                }

                // Contexto de ropa (más específico) → retornar directo
                return ctx;
            }

            return avatarCtx;
        }

        /// <summary>
        /// Retorna el ArmatureContext del avatar (IsAvatarContext == true).
        /// </summary>
        private ArmatureContext FindAvatarContext()
        {
            if (_detectedArmatures == null) return null;

            foreach (var ctx in _detectedArmatures)
            {
                if (ctx.IsAvatarContext) return ctx;
            }

            return null;
        }

        #endregion

        #region Scanning

        /// <summary>
        /// Escanea el avatar y detecta todos los VRCPhysBone que están dentro de armatures.
        /// </summary>
        public List<PhysBoneEntry> ScanPhysBones(GameObject avatarRoot, IReadOnlyList<KnownArmature> knownArmatures = null)
        {
            var entries = new List<PhysBoneEntry>();

            if (avatarRoot == null) return entries;

            EnsureTypesResolved();

            if (_physBoneType == null) return entries;

            // Siempre re-detectar armatures (ScanPhysBones es el primero del ciclo)
            _detectedArmatures = null;
            DetectArmatures(avatarRoot, knownArmatures);

            var components = avatarRoot.GetComponentsInChildren(_physBoneType, true);

            int insideCount = 0;
            int outsideCount = 0;

            foreach (var component in components)
            {
                var comp = component as Component;
                if (comp == null) continue;

                var rootTransform = GetPhysBoneRootTransform(comp);

                // Verificar si está dentro de alguna armature
                var armCtx = FindArmatureContextFor(comp.transform);
                if (armCtx != null)
                {
                    // Dentro del armature → necesita organización
                    var context = CreateOrganizationContext(armCtx);
                    var entry = new PhysBoneEntry(comp, comp.transform, rootTransform, context);
                    entries.Add(entry);
                    insideCount++;
                }
                else
                {
                    // Fuera de armature → buscar a qué contexto pertenece
                    var ctxRoot = FindContextRootFor(comp.transform);
                    if (ctxRoot != null)
                    {
                        var context = CreateOrganizationContext(ctxRoot);
                        var entry = new PhysBoneEntry(comp, comp.transform, rootTransform, context);
                        entry.IsAlreadyOrganized = true;
                        entry.Included = false;
                        entries.Add(entry);
                        outsideCount++;
                    }
                    else
                    {
                        // Fallback: asignar al contexto del avatar
                        var avatarCtx = FindAvatarContext();
                        if (avatarCtx != null)
                        {
                            var context = CreateOrganizationContext(avatarCtx);
                            var entry = new PhysBoneEntry(comp, comp.transform, rootTransform, context);
                            entries.Add(entry);
                            insideCount++;
                        }
                    }
                }
            }

            Debug.Log($"[PhysBoneScanner] PhysBones: {insideCount} dentro de armatures, {outsideCount} ya organizados");

            return entries;
        }

        /// <summary>
        /// Escanea el avatar y detecta todos los VRCPhysBoneCollider que están dentro de armatures.
        /// </summary>
        public List<ColliderEntry> ScanColliders(GameObject avatarRoot, IReadOnlyList<KnownArmature> knownArmatures = null)
        {
            var entries = new List<ColliderEntry>();

            if (avatarRoot == null) return entries;

            EnsureTypesResolved();

            if (_physBoneColliderType == null) return entries;

            // Pre-detectar armatures si no se ha hecho
            if (_detectedArmatures == null)
            {
                DetectArmatures(avatarRoot, knownArmatures);
            }

            var components = avatarRoot.GetComponentsInChildren(_physBoneColliderType, true);

            int insideCount = 0;
            int outsideCount = 0;

            foreach (var component in components)
            {
                var comp = component as Component;
                if (comp == null) continue;

                var rootTransform = GetColliderRootTransform(comp);

                // Verificar si está dentro de alguna armature
                var armCtx = FindArmatureContextFor(comp.transform);
                if (armCtx != null)
                {
                    // Dentro del armature → necesita organización
                    var context = CreateOrganizationContext(armCtx);
                    var entry = new ColliderEntry(comp, comp.transform, rootTransform, context);
                    entries.Add(entry);
                    insideCount++;
                }
                else
                {
                    // Fuera de armature → buscar a qué contexto pertenece
                    var ctxRoot = FindContextRootFor(comp.transform);
                    if (ctxRoot != null)
                    {
                        var context = CreateOrganizationContext(ctxRoot);
                        var entry = new ColliderEntry(comp, comp.transform, rootTransform, context);
                        entry.IsAlreadyOrganized = true;
                        entry.Included = false;
                        entries.Add(entry);
                        outsideCount++;
                    }
                    else
                    {
                        // Fallback: asignar al contexto del avatar
                        var avatarCtx = FindAvatarContext();
                        if (avatarCtx != null)
                        {
                            var context = CreateOrganizationContext(avatarCtx);
                            var entry = new ColliderEntry(comp, comp.transform, rootTransform, context);
                            entries.Add(entry);
                            insideCount++;
                        }
                    }
                }
            }

            Debug.Log($"[PhysBoneScanner] Colliders: {insideCount} dentro de armatures, {outsideCount} ya organizados");

            return entries;
        }

        /// <summary>
        /// Invalida la cache de armatures. Llamar si la jerarquía cambia.
        /// </summary>
        public void InvalidateArmatureCache()
        {
            _detectedArmatures = null;
        }

        #endregion

        #region Reflection Helpers

        /// <summary>
        /// Obtiene el rootTransform de un VRCPhysBone.
        /// Retorna null si el campo es null (no coalescer con transform).
        /// </summary>
        public Transform GetPhysBoneRootTransform(Component physBone)
        {
            if (physBone == null || _pbRootTransformField == null)
                return null;

            try
            {
                var value = _pbRootTransformField.GetValue(physBone) as Transform;
                // Unity "fake null": el campo existe pero no está asignado en el inspector.
                // 'as Transform' devuelve referencia no-null en C# pero Unity la trata como null.
                return value != null ? value : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo rootTransform: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el rootTransform de un VRCPhysBoneCollider.
        /// Retorna null si el campo es null (no coalescer con transform).
        /// </summary>
        public Transform GetColliderRootTransform(Component collider)
        {
            if (collider == null || _colliderRootTransformField == null)
                return null;

            try
            {
                var value = _colliderRootTransformField.GetValue(collider) as Transform;
                return value != null ? value : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo rootTransform de collider: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la lista de colliders de un VRCPhysBone.
        /// </summary>
        public List<Component> GetPhysBoneColliders(Component physBone)
        {
            var result = new List<Component>();

            if (physBone == null || _pbCollidersField == null)
                return result;

            try
            {
                var collidersList = _pbCollidersField.GetValue(physBone);
                if (collidersList is System.Collections.IList list)
                {
                    foreach (var item in list)
                    {
                        if (item is Component comp)
                        {
                            result.Add(comp);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo colliders: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Obtiene la lista de ignoreTransforms de un VRCPhysBone.
        /// </summary>
        public List<Transform> GetPhysBoneIgnoreTransforms(Component physBone)
        {
            var result = new List<Transform>();

            if (physBone == null || _pbIgnoreTransformsField == null)
                return result;

            try
            {
                var ignoreList = _pbIgnoreTransformsField.GetValue(physBone);
                if (ignoreList is System.Collections.IList list)
                {
                    foreach (var item in list)
                    {
                        if (item is Transform t)
                        {
                            result.Add(t);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo ignoreTransforms: {e.Message}");
            }

            return result;
        }

        #endregion
    }
}
