using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AnalisisColision.Models
{
    /// <summary>
    /// Representa un Renderer (mesh) detectado directamente en el GameObject raíz de una ropa.
    /// Esto es un error del autor de la ropa - los meshes deberían estar en GameObjects hijos.
    ///
    /// Problema: Cuando el sistema de auto-generación de menú escanea meshes, puede confundir
    /// este mesh (que está al mismo nivel jerárquico que Body, Head, etc.) con un mesh del avatar base,
    /// causando que se apague incorrectamente.
    /// </summary>
    [Serializable]
    public class MeshOnRootEntry
    {
        #region Serialized Fields

        [SerializeField]
        [Tooltip("Renderer detectado en la raíz de la ropa")]
        private Renderer _renderer;

        [SerializeField]
        [Tooltip("Nombre del GameObject raíz de la ropa")]
        private string _clothingRootName;

        [SerializeField]
        [Tooltip("Nombre del mesh/renderer")]
        private string _meshName;

        [SerializeField]
        [Tooltip("Tipo de renderer (SkinnedMeshRenderer, MeshRenderer, etc.)")]
        private string _rendererType;

        [SerializeField]
        [Tooltip("Ruta en la jerarquía relativa al avatar")]
        private string _hierarchyPath;

        #endregion

        #region Properties

        /// <summary>
        /// Renderer detectado en la raíz de la ropa.
        /// </summary>
        public Renderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }

        /// <summary>
        /// Nombre del GameObject raíz de la ropa donde se encontró el mesh.
        /// </summary>
        public string ClothingRootName
        {
            get => _clothingRootName;
            set => _clothingRootName = value;
        }

        /// <summary>
        /// Nombre del mesh/renderer.
        /// </summary>
        public string MeshName
        {
            get => _meshName;
            set => _meshName = value;
        }

        /// <summary>
        /// Tipo de renderer.
        /// </summary>
        public string RendererType
        {
            get => _rendererType;
            set => _rendererType = value;
        }

        /// <summary>
        /// Ruta en la jerarquía.
        /// </summary>
        public string HierarchyPath
        {
            get => _hierarchyPath;
            set => _hierarchyPath = value;
        }

        /// <summary>
        /// Si la entrada es válida (renderer no destruido).
        /// </summary>
        public bool IsValid => _renderer != null;

        /// <summary>
        /// Nombre para mostrar en el inspector.
        /// </summary>
        public string DisplayName => $"{_meshName} ({_rendererType})";

        #endregion

        #region Constructors

        public MeshOnRootEntry()
        {
        }

        public MeshOnRootEntry(Renderer renderer, GameObject clothingRoot, string hierarchyPath)
        {
            _renderer = renderer;
            _clothingRootName = clothingRoot != null ? clothingRoot.name : "";
            _meshName = renderer != null ? renderer.gameObject.name : "";
            _rendererType = renderer != null ? renderer.GetType().Name : "";
            _hierarchyPath = hierarchyPath;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Selecciona el GameObject del renderer en el editor.
        /// </summary>
        public void SelectInHierarchy()
        {
#if UNITY_EDITOR
            if (_renderer != null)
            {
                UnityEditor.Selection.activeGameObject = _renderer.gameObject;
                UnityEditor.EditorGUIUtility.PingObject(_renderer.gameObject);
            }
#endif
        }

        public override string ToString()
        {
            return $"Mesh '{_meshName}' en raíz de ropa '{_clothingRootName}'";
        }

        #endregion
    }
}
