using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Referencia a un armature (avatar o ropa) con su animator
    /// </summary>
    [Serializable]
    public class ArmatureReference
    {
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _armatureRoot;
        [SerializeField] private string _hierarchyPath = "";
        [SerializeField] private bool _isHumanoid;

        /// <summary>
        /// GameObject raiz del avatar o ropa
        /// </summary>
        public GameObject RootObject => _rootObject;

        /// <summary>
        /// Componente Animator (puede ser null en ropa sin configurar)
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// Transform raiz del armature (tipicamente llamado "Armature")
        /// </summary>
        public Transform ArmatureRoot => _armatureRoot;

        /// <summary>
        /// Indica si el rig esta configurado como Humanoid
        /// </summary>
        public bool IsHumanoid => _isHumanoid;

        /// <summary>
        /// Indica si la referencia es valida
        /// </summary>
        public bool IsValid => _rootObject != null;

        /// <summary>
        /// Ruta jerarquica del objeto raiz
        /// </summary>
        public string HierarchyPath => _hierarchyPath;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ArmatureReference()
        {
        }

        /// <summary>
        /// Constructor con objeto raiz
        /// </summary>
        public ArmatureReference(GameObject root)
        {
            _rootObject = root;
            Refresh();
        }

        /// <summary>
        /// Actualiza la informacion de la referencia
        /// </summary>
        public void Refresh()
        {
            if (_rootObject == null)
            {
                _animator = null;
                _armatureRoot = null;
                _isHumanoid = false;
                _hierarchyPath = "";
                return;
            }

            // Buscar Animator
            _animator = _rootObject.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _rootObject.GetComponentInChildren<Animator>();
            }

            // Verificar si es Humanoid
            _isHumanoid = _animator != null && _animator.isHuman;

            // Buscar raiz del armature
            TryFindArmatureRoot();

            // Actualizar ruta
            _hierarchyPath = GetHierarchyPath(_rootObject.transform);
        }

        /// <summary>
        /// Intenta encontrar el transform raiz del armature
        /// </summary>
        public bool TryFindArmatureRoot()
        {
            if (_rootObject == null) return false;

            // Si tenemos Animator humanoid, usar Hips como referencia
            if (_animator != null && _animator.isHuman)
            {
                var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null)
                {
                    _armatureRoot = hips.parent;
                    return true;
                }
            }

            // Lista de nombres comunes para la raiz del armature
            string[] commonArmatureNames = new[]
            {
                "Armature", "armature",
                "Skeleton", "skeleton",
                "Root", "root",
                "Rig", "rig",
                "Bones", "bones",
                "Bip01", "bip01",
                "mixamorig:Hips", // Mixamo
                "Hips", "hips" // Algunos modelos tienen Hips directamente
            };

            foreach (var name in commonArmatureNames)
            {
                _armatureRoot = FindChildByName(_rootObject.transform, name);
                if (_armatureRoot != null) return true;
            }

            // Si no encontramos por nombre, buscar el primer hijo que tenga hijos
            // (probablemente es el armature)
            foreach (Transform child in _rootObject.transform)
            {
                if (child.childCount > 0 && !child.name.Contains("mesh", StringComparison.OrdinalIgnoreCase))
                {
                    _armatureRoot = child;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Valida el estado de la referencia
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (_rootObject == null)
            {
                result.AddChild(ValidationResult.Error("Objeto no asignado"));
                return result;
            }

            if (_animator == null)
            {
                result.AddChild(ValidationResult.Warning(
                    $"'{_rootObject.name}' no tiene Animator. Se buscara armature por nombre."));
            }
            else if (!_isHumanoid)
            {
                result.AddChild(ValidationResult.Warning(
                    $"'{_rootObject.name}' no esta configurado como Humanoid. " +
                    "Se usara busqueda por nombre de huesos."));
            }

            if (_armatureRoot == null)
            {
                result.AddChild(ValidationResult.Warning(
                    $"No se encontro raiz de armature en '{_rootObject.name}'."));
            }

            return result;
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            // Buscar recursivamente
            foreach (Transform child in parent)
            {
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }

            return null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return "";

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
