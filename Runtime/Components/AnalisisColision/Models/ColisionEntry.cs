using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bender_Dios.MenuRadial.Components.AnalisisColision.Models
{
    /// <summary>
    /// Representa un componente de Modular Avatar detectado que puede colisionar con Menu Radial.
    /// </summary>
    [Serializable]
    public class ColisionEntry
    {
        #region Serialized Fields

        [SerializeField]
        [Tooltip("Componente detectado")]
        private Component _component;

        [SerializeField]
        [Tooltip("Nombre del tipo de componente")]
        private string _componentTypeName;

        [SerializeField]
        [Tooltip("Categoria de colision")]
        private ColisionCategory _category;

        [SerializeField]
        [Tooltip("Ruta en la jerarquia relativa al avatar")]
        private string _hierarchyPath;

        [SerializeField]
        [Tooltip("Si el usuario ha decidido desactivar este componente")]
        private bool _userWantsDisabled;

        [SerializeField]
        [Tooltip("Si el componente estaba habilitado originalmente")]
        private bool _wasOriginallyEnabled;

        [SerializeField]
        [Tooltip("Si esta en la raiz de una prenda de ropa detectada")]
        private bool _isOnClothingRoot;

        [SerializeField]
        [Tooltip("Nombre de la prenda de ropa si aplica")]
        private string _clothingName;

        #endregion

        #region Properties

        /// <summary>
        /// Componente de Modular Avatar detectado.
        /// </summary>
        public Component Component
        {
            get => _component;
            set => _component = value;
        }

        /// <summary>
        /// Nombre del tipo de componente (ej: "ModularAvatarShapeChanger").
        /// </summary>
        public string ComponentTypeName
        {
            get => _componentTypeName;
            set => _componentTypeName = value;
        }

        /// <summary>
        /// Categoria de colision del componente.
        /// </summary>
        public ColisionCategory Category
        {
            get => _category;
            set => _category = value;
        }

        /// <summary>
        /// Ruta en la jerarquia relativa al avatar.
        /// </summary>
        public string HierarchyPath
        {
            get => _hierarchyPath;
            set => _hierarchyPath = value;
        }

        /// <summary>
        /// Si el usuario ha marcado este componente para desactivar.
        /// Solo aplica para componentes UserDecision.
        /// </summary>
        public bool UserWantsDisabled
        {
            get => _userWantsDisabled;
            set => _userWantsDisabled = value;
        }

        /// <summary>
        /// Si el componente estaba habilitado antes de que MR lo detectara.
        /// </summary>
        public bool WasOriginallyEnabled
        {
            get => _wasOriginallyEnabled;
            set => _wasOriginallyEnabled = value;
        }

        /// <summary>
        /// Si el componente esta en la raiz de una prenda de ropa detectada por MRCoserRopa.
        /// </summary>
        public bool IsOnClothingRoot
        {
            get => _isOnClothingRoot;
            set => _isOnClothingRoot = value;
        }

        /// <summary>
        /// Nombre de la prenda de ropa donde se encontro el componente.
        /// </summary>
        public string ClothingName
        {
            get => _clothingName;
            set => _clothingName = value;
        }

        /// <summary>
        /// Si el componente es valido (no ha sido destruido).
        /// </summary>
        public bool IsValid => _component != null;

        /// <summary>
        /// Si el componente esta actualmente habilitado.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                if (_component == null) return false;
                if (_component is MonoBehaviour mb) return mb.enabled;
                if (_component is Behaviour b) return b.enabled;
                return true;
            }
        }

        /// <summary>
        /// Nombre del GameObject donde esta el componente.
        /// </summary>
        public string GameObjectName => _component != null ? _component.gameObject.name : "(null)";

        /// <summary>
        /// Nombre corto del tipo de componente (sin prefijo "ModularAvatar").
        /// </summary>
        public string ShortTypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_componentTypeName)) return "(unknown)";
                return _componentTypeName.Replace("ModularAvatar", "MA ");
            }
        }

        #endregion

        #region Constructors

        public ColisionEntry()
        {
        }

        public ColisionEntry(Component component, string typeName, ColisionCategory category, string hierarchyPath)
        {
            _component = component;
            _componentTypeName = typeName;
            _category = category;
            _hierarchyPath = hierarchyPath;
            // Problemáticos: UserWantsDisabled=true → checkbox desmarcado → se desactivarán
            // Decisión Usuario: UserWantsDisabled=false → checkbox marcado → se mantienen activos
            // Compatibles: no aplica (solo informativos)
            _userWantsDisabled = category == ColisionCategory.Problematic;
            _wasOriginallyEnabled = component is MonoBehaviour mb ? mb.enabled : true;
            _isOnClothingRoot = false;
            _clothingName = "";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Desactiva el componente.
        /// </summary>
        /// <returns>True si se desactivo exitosamente.</returns>
        public bool Disable()
        {
            if (_component == null) return false;

            bool disabled = false;

            if (_component is MonoBehaviour mb)
            {
                mb.enabled = false;
                disabled = true;
            }
            else if (_component is Behaviour b)
            {
                b.enabled = false;
                disabled = true;
            }

#if UNITY_EDITOR
            // Marcar como dirty para que Unity guarde los cambios en Edit Mode
            if (disabled && !Application.isPlaying)
            {
                EditorUtility.SetDirty(_component);
                EditorUtility.SetDirty(_component.gameObject);
            }
#endif

            return disabled;
        }

        /// <summary>
        /// Restaura el componente a su estado original.
        /// </summary>
        /// <returns>True si se restauro exitosamente.</returns>
        public bool Restore()
        {
            if (_component == null) return false;

            bool restored = false;

            if (_component is MonoBehaviour mb)
            {
                mb.enabled = _wasOriginallyEnabled;
                restored = true;
            }
            else if (_component is Behaviour b)
            {
                b.enabled = _wasOriginallyEnabled;
                restored = true;
            }

#if UNITY_EDITOR
            // Marcar como dirty para que Unity guarde los cambios en Edit Mode
            if (restored && !Application.isPlaying)
            {
                EditorUtility.SetDirty(_component);
                EditorUtility.SetDirty(_component.gameObject);
            }
#endif

            return restored;
        }

        public override string ToString()
        {
            return $"[{_category}] {ShortTypeName} en {GameObjectName}";
        }

        #endregion
    }
}
