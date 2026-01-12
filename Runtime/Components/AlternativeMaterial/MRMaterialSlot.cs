using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Components.AlternativeMaterial
{
    /// <summary>
    /// Representa un slot de material en un Renderer (MeshRenderer o SkinnedMeshRenderer).
    /// Almacena la referencia al renderer, el índice del material y el grupo vinculado.
    /// </summary>
    [Serializable]
    public class MRMaterialSlot
    {
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private int _materialIndex;
        [SerializeField] private string _hierarchyPath;
        [SerializeField] private int _linkedGroupIndex = -1;

        /// <summary>
        /// Renderer objetivo (MeshRenderer o SkinnedMeshRenderer)
        /// </summary>
        public Renderer TargetRenderer
        {
            get => _targetRenderer;
            set
            {
                _targetRenderer = value;
                UpdateHierarchyPath();
            }
        }

        /// <summary>
        /// Índice del material dentro del array sharedMaterials del renderer
        /// </summary>
        public int MaterialIndex
        {
            get => _materialIndex;
            set => _materialIndex = Mathf.Max(0, value);
        }

        /// <summary>
        /// Ruta jerárquica del renderer para animaciones
        /// </summary>
        public string HierarchyPath
        {
            get => _hierarchyPath;
            set => _hierarchyPath = value;
        }

        /// <summary>
        /// Índice del grupo de materiales vinculado (-1 si no tiene grupo)
        /// </summary>
        public int LinkedGroupIndex
        {
            get => _linkedGroupIndex;
            set => _linkedGroupIndex = value;
        }

        /// <summary>
        /// Indica si el slot tiene un grupo vinculado
        /// </summary>
        public bool HasLinkedGroup => _linkedGroupIndex >= 0;

        /// <summary>
        /// Indica si la referencia al renderer es válida
        /// </summary>
        public bool IsValid => _targetRenderer != null &&
                               _materialIndex >= 0 &&
                               _materialIndex < GetMaterialCount();

        /// <summary>
        /// Obtiene el material actual en este slot
        /// </summary>
        public Material CurrentMaterial
        {
            get
            {
                if (!IsValid) return null;
                return _targetRenderer.sharedMaterials[_materialIndex];
            }
        }

        /// <summary>
        /// Obtiene el tipo de renderer (para animaciones)
        /// </summary>
        public Type RendererType
        {
            get
            {
                if (_targetRenderer == null) return null;
                if (_targetRenderer is SkinnedMeshRenderer) return typeof(SkinnedMeshRenderer);
                if (_targetRenderer is MeshRenderer) return typeof(MeshRenderer);
                return _targetRenderer.GetType();
            }
        }

        /// <summary>
        /// Nombre descriptivo del slot para mostrar en el inspector
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_targetRenderer == null) return "[Missing Renderer]";
                string rendererType = _targetRenderer is SkinnedMeshRenderer ? "SMR" : "MR";
                return $"{_targetRenderer.name} ({rendererType}) [{_materialIndex}]";
            }
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MRMaterialSlot()
        {
        }

        /// <summary>
        /// Constructor con renderer e índice
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        public MRMaterialSlot(Renderer renderer, int materialIndex)
        {
            _targetRenderer = renderer;
            _materialIndex = Mathf.Max(0, materialIndex);
            UpdateHierarchyPath();
        }

        /// <summary>
        /// Actualiza la ruta jerárquica basándose en el renderer actual
        /// </summary>
        /// <param name="root">Transform raíz opcional (avatar root)</param>
        public void UpdateHierarchyPath(Transform root = null)
        {
            if (_targetRenderer != null)
            {
                _hierarchyPath = HierarchyPathHelper.GetHierarchyPath(_targetRenderer.transform, root);
            }
            else
            {
                _hierarchyPath = "";
            }
        }

        /// <summary>
        /// Obtiene la cantidad de materiales en el renderer
        /// </summary>
        /// <returns>Cantidad de materiales o 0 si no hay renderer</returns>
        public int GetMaterialCount()
        {
            if (_targetRenderer == null) return 0;
            var materials = _targetRenderer.sharedMaterials;
            return materials != null ? materials.Length : 0;
        }

        /// <summary>
        /// Desvincula el grupo de materiales
        /// </summary>
        public void UnlinkGroup()
        {
            _linkedGroupIndex = -1;
        }

        /// <summary>
        /// Vincula a un grupo de materiales
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        public void LinkToGroup(int groupIndex)
        {
            _linkedGroupIndex = groupIndex;
        }

        /// <summary>
        /// Verifica si este slot corresponde al mismo renderer y slot que otro
        /// </summary>
        /// <param name="other">Otro slot a comparar</param>
        /// <returns>True si son el mismo slot</returns>
        public bool IsSameSlot(MRMaterialSlot other)
        {
            if (other == null) return false;
            return _targetRenderer == other._targetRenderer &&
                   _materialIndex == other._materialIndex;
        }

        /// <summary>
        /// Verifica si este slot corresponde al renderer e índice especificados
        /// </summary>
        /// <param name="renderer">Renderer a comparar</param>
        /// <param name="materialIndex">Índice a comparar</param>
        /// <returns>True si coincide</returns>
        public bool Matches(Renderer renderer, int materialIndex)
        {
            return _targetRenderer == renderer && _materialIndex == materialIndex;
        }

        public override string ToString()
        {
            string groupInfo = HasLinkedGroup ? $"Grupo {_linkedGroupIndex}" : "Sin grupo";
            return $"{DisplayName} - {groupInfo}";
        }
    }
}
