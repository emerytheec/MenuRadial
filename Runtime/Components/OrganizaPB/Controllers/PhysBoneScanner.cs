using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers
{
    /// <summary>
    /// Escanea el avatar para detectar VRCPhysBone y VRCPhysBoneCollider.
    /// Usa reflexión para evitar dependencia directa del VRChat SDK.
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

        private ContextDetector _contextDetector;

        #endregion

        #region Properties

        /// <summary>
        /// Indica si el VRChat SDK está disponible.
        /// </summary>
        public bool IsSDKAvailable
        {
            get
            {
                EnsureTypesResolved();
                return _physBoneType != null;
            }
        }

        /// <summary>
        /// Tipo de VRCPhysBone (para uso externo).
        /// </summary>
        public Type PhysBoneType
        {
            get
            {
                EnsureTypesResolved();
                return _physBoneType;
            }
        }

        /// <summary>
        /// Tipo de VRCPhysBoneCollider (para uso externo).
        /// </summary>
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

        public PhysBoneScanner()
        {
            _contextDetector = new ContextDetector();
        }

        public PhysBoneScanner(ContextDetector contextDetector)
        {
            _contextDetector = contextDetector ?? new ContextDetector();
        }

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

            // Cache field info para mejor rendimiento
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

            if (_physBoneType != null)
            {
                Debug.Log($"[PhysBoneScanner] VRChat SDK detectado: {_physBoneType.FullName}");
            }
            else
            {
                Debug.LogWarning("[PhysBoneScanner] VRChat SDK no disponible");
            }
        }

        #endregion

        #region Scanning

        /// <summary>
        /// Escanea el avatar y detecta todos los VRCPhysBone.
        /// </summary>
        public List<PhysBoneEntry> ScanPhysBones(GameObject avatarRoot)
        {
            var entries = new List<PhysBoneEntry>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[PhysBoneScanner] Avatar root es null");
                return entries;
            }

            EnsureTypesResolved();

            if (_physBoneType == null)
            {
                Debug.LogWarning("[PhysBoneScanner] No se pueden escanear PhysBones: SDK no disponible");
                return entries;
            }

            var components = avatarRoot.GetComponentsInChildren(_physBoneType, true);
            Debug.Log($"[PhysBoneScanner] Encontrados {components.Length} VRCPhysBone");

            foreach (var component in components)
            {
                var entry = CreatePhysBoneEntry(component as Component, avatarRoot);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        /// <summary>
        /// Escanea el avatar y detecta todos los VRCPhysBoneCollider.
        /// </summary>
        public List<ColliderEntry> ScanColliders(GameObject avatarRoot)
        {
            var entries = new List<ColliderEntry>();

            if (avatarRoot == null)
            {
                Debug.LogWarning("[PhysBoneScanner] Avatar root es null");
                return entries;
            }

            EnsureTypesResolved();

            if (_physBoneColliderType == null)
            {
                Debug.LogWarning("[PhysBoneScanner] No se pueden escanear Colliders: SDK no disponible");
                return entries;
            }

            var components = avatarRoot.GetComponentsInChildren(_physBoneColliderType, true);
            Debug.Log($"[PhysBoneScanner] Encontrados {components.Length} VRCPhysBoneCollider");

            foreach (var component in components)
            {
                var entry = CreateColliderEntry(component as Component, avatarRoot);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        #endregion

        #region Entry Creation

        private PhysBoneEntry CreatePhysBoneEntry(Component component, GameObject avatarRoot)
        {
            if (component == null) return null;

            var originalTransform = component.transform;
            var rootTransform = GetPhysBoneRootTransform(component);
            var context = _contextDetector.DetectContext(originalTransform, avatarRoot);

            var entry = new PhysBoneEntry(component, originalTransform, rootTransform, context);

            Debug.Log($"[PhysBoneScanner] PhysBone: {entry.GeneratedName} en contexto {context?.ContextName ?? "desconocido"}");

            return entry;
        }

        private ColliderEntry CreateColliderEntry(Component component, GameObject avatarRoot)
        {
            if (component == null) return null;

            var originalTransform = component.transform;
            var rootTransform = GetColliderRootTransform(component);
            var context = _contextDetector.DetectContext(originalTransform, avatarRoot);

            var entry = new ColliderEntry(component, originalTransform, rootTransform, context);

            Debug.Log($"[PhysBoneScanner] Collider: {entry.GeneratedName} en contexto {context?.ContextName ?? "desconocido"}");

            return entry;
        }

        #endregion

        #region Reflection Helpers

        /// <summary>
        /// Obtiene el rootTransform de un VRCPhysBone.
        /// </summary>
        public Transform GetPhysBoneRootTransform(Component physBone)
        {
            if (physBone == null || _pbRootTransformField == null)
                return physBone?.transform;

            try
            {
                var rootTransform = _pbRootTransformField.GetValue(physBone) as Transform;
                return rootTransform ?? physBone.transform;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo rootTransform: {e.Message}");
                return physBone.transform;
            }
        }

        /// <summary>
        /// Obtiene el rootTransform de un VRCPhysBoneCollider.
        /// </summary>
        public Transform GetColliderRootTransform(Component collider)
        {
            if (collider == null || _colliderRootTransformField == null)
                return collider?.transform;

            try
            {
                var rootTransform = _colliderRootTransformField.GetValue(collider) as Transform;
                return rootTransform ?? collider.transform;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhysBoneScanner] Error obteniendo rootTransform de collider: {e.Message}");
                return collider.transform;
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
