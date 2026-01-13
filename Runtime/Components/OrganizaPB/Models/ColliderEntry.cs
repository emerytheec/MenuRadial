using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Representa un VRCPhysBoneCollider detectado en el avatar.
    /// Almacena la información necesaria para reubicarlo en el contenedor Colliders.
    /// </summary>
    [Serializable]
    public class ColliderEntry
    {
        [SerializeField] private Component _originalComponent;
        [SerializeField] private Transform _originalTransform;
        [SerializeField] private Transform _rootTransform;
        [SerializeField] private OrganizationContext _context;
        [SerializeField] private string _generatedName;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _wasRelocated;
        [SerializeField] private string _originalPath;

        // Campos para revertir la reorganización
        [SerializeField] private GameObject _relocatedGameObject;
        [SerializeField] private Component _relocatedComponent;
        [SerializeField] private int _originalSiblingIndex;

        /// <summary>
        /// El componente VRCPhysBoneCollider original.
        /// </summary>
        public Component OriginalComponent
        {
            get => _originalComponent;
            set => _originalComponent = value;
        }

        /// <summary>
        /// Transform donde estaba originalmente el componente.
        /// </summary>
        public Transform OriginalTransform
        {
            get => _originalTransform;
            set
            {
                _originalTransform = value;
                UpdateOriginalPath();
            }
        }

        /// <summary>
        /// El Transform raíz del collider (rootTransform del componente).
        /// Si no estaba configurado, será el mismo que OriginalTransform.
        /// </summary>
        public Transform RootTransform
        {
            get => _rootTransform;
            set => _rootTransform = value;
        }

        /// <summary>
        /// Contexto al que pertenece (avatar o ropa específica).
        /// </summary>
        public OrganizationContext Context
        {
            get => _context;
            set => _context = value;
        }

        /// <summary>
        /// Nombre generado para el nuevo GameObject (ej: "Col_Head").
        /// </summary>
        public string GeneratedName
        {
            get => _generatedName;
            set => _generatedName = value;
        }

        /// <summary>
        /// Si debe procesarse durante la reubicación.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Si ya fue reubicado.
        /// </summary>
        public bool WasRelocated
        {
            get => _wasRelocated;
            set => _wasRelocated = value;
        }

        /// <summary>
        /// El GameObject creado para contener el componente reubicado.
        /// </summary>
        public GameObject RelocatedGameObject
        {
            get => _relocatedGameObject;
            set => _relocatedGameObject = value;
        }

        /// <summary>
        /// El componente reubicado (copia del original).
        /// </summary>
        public Component RelocatedComponent
        {
            get => _relocatedComponent;
            set => _relocatedComponent = value;
        }

        /// <summary>
        /// Índice de hermano original para restaurar posición exacta.
        /// </summary>
        public int OriginalSiblingIndex
        {
            get => _originalSiblingIndex;
            set => _originalSiblingIndex = value;
        }

        /// <summary>
        /// Path jerárquico original para referencia.
        /// </summary>
        public string OriginalPath => _originalPath;

        /// <summary>
        /// Nombre del transform raíz para mostrar en UI.
        /// </summary>
        public string RootBoneName => _rootTransform != null ? _rootTransform.name : "(ninguno)";

        /// <summary>
        /// Verifica si la entrada es válida.
        /// </summary>
        public bool IsValid => _originalComponent != null && _originalTransform != null;

        public ColliderEntry() { }

        public ColliderEntry(Component component, Transform originalTransform, Transform rootTransform, OrganizationContext context)
        {
            _originalComponent = component;
            _originalTransform = originalTransform;
            _rootTransform = rootTransform ?? originalTransform;
            _context = context;
            _generatedName = GenerateDefaultName();
            UpdateOriginalPath();
        }

        private string GenerateDefaultName()
        {
            if (_rootTransform != null)
            {
                return $"Col_{_rootTransform.name}";
            }
            if (_originalTransform != null)
            {
                return $"Col_{_originalTransform.name}";
            }
            return "Col_Unknown";
        }

        private void UpdateOriginalPath()
        {
            if (_originalTransform != null)
            {
                _originalPath = GetHierarchyPath(_originalTransform);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            var path = transform.name;
            var parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public override string ToString()
        {
            var contextInfo = _context != null ? _context.ContextName : "Sin contexto";
            return $"[{contextInfo}] {_generatedName} -> {RootBoneName}";
        }
    }
}
