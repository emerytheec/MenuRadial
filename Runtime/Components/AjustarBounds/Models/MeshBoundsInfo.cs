using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Models
{
    /// <summary>
    /// Informacion de bounds de un SkinnedMeshRenderer individual.
    /// Almacena el estado original completo (bounds, rootBone, transform)
    /// para poder restaurarlo despues de aplicar bounds unificados.
    /// </summary>
    [Serializable]
    public class MeshBoundsInfo
    {
        [SerializeField]
        private SkinnedMeshRenderer _renderer;

        [SerializeField]
        private Bounds _originalBounds;

        [SerializeField]
        private bool _isValid;

        [SerializeField]
        private string _meshName;

        [SerializeField]
        private string _hierarchyPath;

        [SerializeField]
        private Transform _originalProbeAnchor;

        [SerializeField]
        private bool _hadOriginalProbeAnchor;

        [SerializeField]
        private bool _boundsCurrentlyApplied;

        // Estado original del renderer para restaurar
        [SerializeField]
        private Transform _originalRootBone;

        [SerializeField]
        private Vector3 _originalLocalPosition;

        [SerializeField]
        private Quaternion _originalLocalRotation;

        [SerializeField]
        private Vector3 _originalLocalScale;

        /// <summary>
        /// Referencia al SkinnedMeshRenderer
        /// </summary>
        public SkinnedMeshRenderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }

        /// <summary>
        /// Bounds originales antes de modificar (en espacio del rootBone original)
        /// </summary>
        public Bounds OriginalBounds
        {
            get => _originalBounds;
            set => _originalBounds = value;
        }

        /// <summary>
        /// Indica si la referencia al renderer es valida
        /// </summary>
        public bool IsValid
        {
            get => _isValid && _renderer != null;
            set => _isValid = value;
        }

        /// <summary>
        /// Nombre del mesh para mostrar en UI
        /// </summary>
        public string MeshName
        {
            get => _meshName;
            set => _meshName = value;
        }

        /// <summary>
        /// Ruta jerarquica del objeto en la escena
        /// </summary>
        public string HierarchyPath
        {
            get => _hierarchyPath;
            set => _hierarchyPath = value;
        }

        /// <summary>
        /// Probe Anchor original del renderer
        /// </summary>
        public Transform OriginalProbeAnchor
        {
            get => _originalProbeAnchor;
            set => _originalProbeAnchor = value;
        }

        /// <summary>
        /// Indica si el renderer tenia un Probe Anchor antes de modificar
        /// </summary>
        public bool HadOriginalProbeAnchor
        {
            get => _hadOriginalProbeAnchor;
            set => _hadOriginalProbeAnchor = value;
        }

        /// <summary>
        /// rootBone original del renderer (antes de unificar)
        /// </summary>
        public Transform OriginalRootBone => _originalRootBone;

        /// <summary>
        /// Indica si el renderer tiene bones (meshes sin bones se saltan)
        /// </summary>
        public bool HasBones => _renderer != null && _renderer.bones != null && _renderer.bones.Length > 0;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MeshBoundsInfo() { }

        /// <summary>
        /// Constructor con SkinnedMeshRenderer.
        /// Captura todo el estado original del renderer para poder restaurarlo.
        /// </summary>
        public MeshBoundsInfo(SkinnedMeshRenderer renderer)
        {
            _renderer = renderer;
            if (renderer != null)
            {
                _originalBounds = renderer.localBounds;
                _originalRootBone = renderer.rootBone;
                _originalLocalPosition = renderer.transform.localPosition;
                _originalLocalRotation = renderer.transform.localRotation;
                _originalLocalScale = renderer.transform.localScale;

                _meshName = renderer.name;
                _hierarchyPath = GetHierarchyPath(renderer.transform);
                _isValid = true;

                _originalProbeAnchor = renderer.probeAnchor;
                _hadOriginalProbeAnchor = renderer.probeAnchor != null;
            }
        }

        /// <summary>
        /// Obtiene la ruta jerarquica de un transform
        /// </summary>
        private string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Actualiza la referencia y recaptura datos.
        /// No sobreescribe originales si los bounds unificados estan aplicados.
        /// </summary>
        public void Refresh()
        {
            if (_renderer != null)
            {
                if (!_boundsCurrentlyApplied)
                {
                    _originalBounds = _renderer.localBounds;
                    _originalRootBone = _renderer.rootBone;
                    _originalLocalPosition = _renderer.transform.localPosition;
                    _originalLocalRotation = _renderer.transform.localRotation;
                    _originalLocalScale = _renderer.transform.localScale;
                }
                _meshName = _renderer.name;
                _hierarchyPath = GetHierarchyPath(_renderer.transform);
                _isValid = true;

                if (!_boundsCurrentlyApplied)
                {
                    _originalProbeAnchor = _renderer.probeAnchor;
                    _hadOriginalProbeAnchor = _renderer.probeAnchor != null;
                }
            }
            else
            {
                _isValid = false;
            }
        }

        /// <summary>
        /// Aplica bounds unificados: asigna rootBone compartido, resetea transform,
        /// y establece los bounds unificados.
        /// Esto hace que TODOS los renderers usen el mismo volumen de culling.
        /// </summary>
        public void ApplyUnifiedBounds(Bounds unifiedBounds, Transform sharedRootBone)
        {
            if (_renderer != null)
            {
                // Resetear transform a identity para alineacion correcta con rootBone
                _renderer.transform.localPosition = Vector3.zero;
                _renderer.transform.localRotation = Quaternion.identity;
                _renderer.transform.localScale = Vector3.one;

                // Asignar rootBone compartido y bounds unificados
                _renderer.rootBone = sharedRootBone;
                _renderer.localBounds = unifiedBounds;
                _boundsCurrentlyApplied = true;
            }
        }

        /// <summary>
        /// Restaura el estado original completo: bounds, rootBone y transform.
        /// </summary>
        public void RestoreOriginalBounds()
        {
            if (_renderer != null)
            {
                // Restaurar transform original
                _renderer.transform.localPosition = _originalLocalPosition;
                _renderer.transform.localRotation = _originalLocalRotation;
                _renderer.transform.localScale = _originalLocalScale;

                // Restaurar rootBone y bounds originales
                _renderer.rootBone = _originalRootBone;
                _renderer.localBounds = _originalBounds;
                _boundsCurrentlyApplied = false;
            }
        }

        /// <summary>
        /// Aplica un probe anchor al renderer
        /// </summary>
        public void ApplyProbeAnchor(Transform anchor)
        {
            if (_renderer != null)
            {
                _renderer.probeAnchor = anchor;
            }
        }

        /// <summary>
        /// Restaura el probe anchor original
        /// </summary>
        public void RestoreOriginalProbeAnchor()
        {
            if (_renderer != null)
            {
                _renderer.probeAnchor = _hadOriginalProbeAnchor ? _originalProbeAnchor : null;
            }
        }

    }
}
