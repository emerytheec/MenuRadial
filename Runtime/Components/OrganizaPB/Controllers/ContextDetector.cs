using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers
{
    /// <summary>
    /// Detecta el contexto (avatar o ropa) al que pertenece cada PhysBone/Collider.
    /// Determina dónde crear los contenedores PhysBones/Colliders.
    /// </summary>
    public class ContextDetector
    {
        #region Constants

        /// <summary>
        /// Nombres comunes de armature.
        /// </summary>
        private static readonly string[] ArmatureNames = new[]
        {
            "Armature", "armature",
            "Skeleton", "skeleton",
            "Root", "root",
            "Rig", "rig"
        };

        /// <summary>
        /// Tipo de VRC_AvatarDescriptor para detectar el avatar root.
        /// </summary>
        private const string AVATAR_DESCRIPTOR_TYPE = "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor";
        private const string AVATAR_DESCRIPTOR_TYPE_ALT = "VRC.SDKBase.VRC_AvatarDescriptor";

        #endregion

        #region Private Fields

        private Type _avatarDescriptorType;
        private bool _typeResolved;

        #endregion

        #region Type Resolution

        private void EnsureTypeResolved()
        {
            if (_typeResolved) return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_avatarDescriptorType == null)
                {
                    _avatarDescriptorType = assembly.GetType(AVATAR_DESCRIPTOR_TYPE);
                }

                if (_avatarDescriptorType == null)
                {
                    _avatarDescriptorType = assembly.GetType(AVATAR_DESCRIPTOR_TYPE_ALT);
                }

                if (_avatarDescriptorType != null)
                    break;
            }

            _typeResolved = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Detecta el contexto al que pertenece un Transform.
        /// </summary>
        /// <param name="transform">Transform del PhysBone/Collider</param>
        /// <param name="avatarRoot">GameObject raíz del avatar</param>
        /// <returns>OrganizationContext con la información del contexto</returns>
        public OrganizationContext DetectContext(Transform transform, GameObject avatarRoot)
        {
            if (transform == null || avatarRoot == null)
            {
                return CreateDefaultContext(avatarRoot);
            }

            // Buscar el Armature más cercano
            var armatureInfo = FindNearestArmature(transform, avatarRoot.transform);

            if (armatureInfo.armature == null)
            {
                // No se encontró Armature, usar el avatar root
                return CreateAvatarContext(avatarRoot, FindArmatureInObject(avatarRoot));
            }

            // Determinar si el contexto es el avatar o una ropa
            var contextRoot = armatureInfo.contextRoot;

            if (IsAvatarRoot(contextRoot, avatarRoot))
            {
                return CreateAvatarContext(avatarRoot, armatureInfo.armature);
            }
            else
            {
                return CreateClothingContext(contextRoot, armatureInfo.armature);
            }
        }

        /// <summary>
        /// Busca el Armature más cercano subiendo en la jerarquía.
        /// </summary>
        public (Transform armature, GameObject contextRoot) FindNearestArmature(Transform from, Transform avatarRoot)
        {
            if (from == null) return (null, null);

            Transform current = from;

            while (current != null)
            {
                // Verificar si este objeto tiene un hijo llamado "Armature"
                var armature = FindArmatureChild(current);
                if (armature != null)
                {
                    return (armature, current.gameObject);
                }

                // Verificar si este objeto ES el Armature
                if (IsArmatureName(current.name))
                {
                    // El contexto es el padre del Armature
                    var contextRoot = current.parent != null ? current.parent.gameObject : current.gameObject;
                    return (current, contextRoot);
                }

                // Si llegamos al avatar root, parar
                if (current == avatarRoot)
                {
                    var avatarArmature = FindArmatureChild(current);
                    return (avatarArmature, current.gameObject);
                }

                current = current.parent;
            }

            return (null, null);
        }

        /// <summary>
        /// Verifica si un GameObject es el avatar root (tiene VRC_AvatarDescriptor).
        /// </summary>
        public bool IsAvatarRoot(GameObject obj, GameObject expectedAvatarRoot)
        {
            if (obj == null) return false;

            // Comparación directa
            if (obj == expectedAvatarRoot) return true;

            // Verificar si tiene VRC_AvatarDescriptor
            EnsureTypeResolved();

            if (_avatarDescriptorType != null)
            {
                return obj.GetComponent(_avatarDescriptorType) != null;
            }

            return false;
        }

        #endregion

        #region Private Helpers

        private Transform FindArmatureChild(Transform parent)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (IsArmatureName(child.name))
                {
                    return child;
                }
            }

            return null;
        }

        private Transform FindArmatureInObject(GameObject obj)
        {
            if (obj == null) return null;

            return FindArmatureChild(obj.transform);
        }

        private bool IsArmatureName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (var armatureName in ArmatureNames)
            {
                if (name.Equals(armatureName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private OrganizationContext CreateAvatarContext(GameObject avatarRoot, Transform armature)
        {
            return new OrganizationContext(
                avatarRoot,
                armature,
                "Avatar",
                true
            );
        }

        private OrganizationContext CreateClothingContext(GameObject clothingRoot, Transform armature)
        {
            return new OrganizationContext(
                clothingRoot,
                armature,
                clothingRoot.name,
                false
            );
        }

        private OrganizationContext CreateDefaultContext(GameObject avatarRoot)
        {
            if (avatarRoot == null)
            {
                return new OrganizationContext(null, null, "Desconocido", false);
            }

            var armature = FindArmatureInObject(avatarRoot);
            return CreateAvatarContext(avatarRoot, armature);
        }

        #endregion
    }
}
