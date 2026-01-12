using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Models
{
    /// <summary>
    /// Informacion de bounds de un SkinnedMeshRenderer individual.
    /// Almacena tanto los bounds originales como los calculados.
    /// </summary>
    [Serializable]
    public class MeshBoundsInfo
    {
        [SerializeField]
        private SkinnedMeshRenderer _renderer;

        [SerializeField]
        private Bounds _originalBounds;

        [SerializeField]
        private Bounds _calculatedBounds;

        [SerializeField]
        private bool _isValid;

        [SerializeField]
        private string _meshName;

        [SerializeField]
        private string _hierarchyPath;

        /// <summary>
        /// Referencia al SkinnedMeshRenderer
        /// </summary>
        public SkinnedMeshRenderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }

        /// <summary>
        /// Bounds originales antes de modificar
        /// </summary>
        public Bounds OriginalBounds
        {
            get => _originalBounds;
            set => _originalBounds = value;
        }

        /// <summary>
        /// Bounds calculados que cubren exactamente el mesh
        /// </summary>
        public Bounds CalculatedBounds
        {
            get => _calculatedBounds;
            set => _calculatedBounds = value;
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
        /// Constructor por defecto
        /// </summary>
        public MeshBoundsInfo() { }

        /// <summary>
        /// Constructor con SkinnedMeshRenderer
        /// </summary>
        public MeshBoundsInfo(SkinnedMeshRenderer renderer)
        {
            _renderer = renderer;
            if (renderer != null)
            {
                _originalBounds = renderer.localBounds;
                _meshName = renderer.name;
                _hierarchyPath = GetHierarchyPath(renderer.transform);
                _isValid = true;
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
        /// Actualiza la referencia y recaptura datos
        /// </summary>
        public void Refresh()
        {
            if (_renderer != null)
            {
                _originalBounds = _renderer.localBounds;
                _meshName = _renderer.name;
                _hierarchyPath = GetHierarchyPath(_renderer.transform);
                _isValid = true;
            }
            else
            {
                _isValid = false;
            }
        }

        /// <summary>
        /// Aplica los bounds unificados al renderer
        /// </summary>
        public void ApplyUnifiedBounds(Bounds unifiedBounds)
        {
            if (_renderer != null)
            {
                _renderer.localBounds = unifiedBounds;
            }
        }

        /// <summary>
        /// Restaura los bounds originales
        /// </summary>
        public void RestoreOriginalBounds()
        {
            if (_renderer != null)
            {
                _renderer.localBounds = _originalBounds;
            }
        }
    }
}
