using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Representa el contexto de organización: avatar base o una ropa específica.
    /// El contexto determina dónde se crearán los contenedores PhysBones/Colliders.
    /// </summary>
    [Serializable]
    public class OrganizationContext
    {
        [SerializeField] private GameObject _contextRoot;
        [SerializeField] private Transform _armatureTransform;
        [SerializeField] private string _contextName;
        [SerializeField] private bool _isAvatarContext;

        /// <summary>
        /// GameObject raíz del contexto (avatar root o ropa root).
        /// </summary>
        public GameObject ContextRoot
        {
            get => _contextRoot;
            set => _contextRoot = value;
        }

        /// <summary>
        /// Transform del Armature de este contexto.
        /// Los contenedores PhysBones/Colliders se crearán como hermanos de este.
        /// </summary>
        public Transform ArmatureTransform
        {
            get => _armatureTransform;
            set => _armatureTransform = value;
        }

        /// <summary>
        /// Nombre descriptivo del contexto ("Avatar" o nombre de la ropa).
        /// </summary>
        public string ContextName
        {
            get => _contextName;
            set => _contextName = value;
        }

        /// <summary>
        /// True si este contexto es el avatar base (tiene VRC_AvatarDescriptor).
        /// </summary>
        public bool IsAvatarContext
        {
            get => _isAvatarContext;
            set => _isAvatarContext = value;
        }

        /// <summary>
        /// Verifica si el contexto es válido.
        /// </summary>
        public bool IsValid => _contextRoot != null && _armatureTransform != null;

        public OrganizationContext() { }

        public OrganizationContext(GameObject contextRoot, Transform armatureTransform, string contextName, bool isAvatarContext)
        {
            _contextRoot = contextRoot;
            _armatureTransform = armatureTransform;
            _contextName = contextName;
            _isAvatarContext = isAvatarContext;
        }

        public override string ToString()
        {
            return $"[{(_isAvatarContext ? "Avatar" : "Ropa")}] {_contextName}";
        }

        public override bool Equals(object obj)
        {
            if (obj is OrganizationContext other)
            {
                return _contextRoot == other._contextRoot;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _contextRoot != null ? _contextRoot.GetHashCode() : 0;
        }
    }
}
