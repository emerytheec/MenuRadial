using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Metodo utilizado para mapear el hueso
    /// </summary>
    public enum BoneMappingMethod
    {
        None,
        HumanoidMapping,    // Primario: Animator.GetBoneTransform
        NameMatching,       // Fallback: Por nombre de hueso
        ManualAssignment    // Usuario asigno manualmente
    }

    /// <summary>
    /// Representa un mapeo entre un hueso del avatar y un hueso de la ropa
    /// </summary>
    [Serializable]
    public class BoneMapping
    {
        [SerializeField] private HumanBodyBones _boneType;
        [SerializeField] private Transform _avatarBone;
        [FormerlySerializedAs("_clothingBone")]
        [SerializeField] private Transform _pieceBone;
        [SerializeField] private string _avatarBonePath = "";
        [FormerlySerializedAs("_clothingBonePath")]
        [SerializeField] private string _pieceBonePath = "";
        [SerializeField] private BoneMappingMethod _mappingMethod = BoneMappingMethod.None;
        [SerializeField] private bool _wasStitched;

        /// <summary>
        /// Tipo de hueso humanoid (HumanBodyBones enum)
        /// </summary>
        public HumanBodyBones BoneType
        {
            get => _boneType;
            set => _boneType = value;
        }

        /// <summary>
        /// Transform del hueso en el avatar
        /// </summary>
        public Transform AvatarBone
        {
            get => _avatarBone;
            set
            {
                _avatarBone = value;
                UpdateAvatarPath();
            }
        }

        /// <summary>
        /// Transform del hueso en la pieza
        /// </summary>
        public Transform PieceBone
        {
            get => _pieceBone;
            set
            {
                _pieceBone = value;
                UpdatePiecePath();
            }
        }

        /// <summary>
        /// Ruta jerarquica del hueso del avatar
        /// </summary>
        public string AvatarBonePath => _avatarBonePath;

        /// <summary>
        /// Ruta jerarquica del hueso de la pieza
        /// </summary>
        public string PieceBonePath => _pieceBonePath;

        /// <summary>
        /// Metodo utilizado para detectar este mapeo
        /// </summary>
        public BoneMappingMethod MappingMethod
        {
            get => _mappingMethod;
            set => _mappingMethod = value;
        }

        /// <summary>
        /// Indica si el mapeo es valido (ambos huesos existen)
        /// </summary>
        public bool IsValid => _avatarBone != null && _pieceBone != null;

        /// <summary>
        /// Indica si este hueso ya fue cosido
        /// </summary>
        public bool WasStitched
        {
            get => _wasStitched;
            set => _wasStitched = value;
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BoneMapping()
        {
        }

        /// <summary>
        /// Constructor con tipo de hueso
        /// </summary>
        public BoneMapping(HumanBodyBones boneType)
        {
            _boneType = boneType;
        }

        /// <summary>
        /// Constructor completo
        /// </summary>
        public BoneMapping(HumanBodyBones boneType, Transform avatarBone, Transform pieceBone, BoneMappingMethod method)
        {
            _boneType = boneType;
            _avatarBone = avatarBone;
            _pieceBone = pieceBone;
            _mappingMethod = method;
            UpdatePaths();
        }

        /// <summary>
        /// Actualiza las rutas jerarquicas de ambos huesos
        /// </summary>
        public void UpdatePaths()
        {
            UpdateAvatarPath();
            UpdatePiecePath();
        }

        private void UpdateAvatarPath()
        {
            _avatarBonePath = _avatarBone != null ? GetHierarchyPath(_avatarBone) : "";
        }

        private void UpdatePiecePath()
        {
            _pieceBonePath = _pieceBone != null ? GetHierarchyPath(_pieceBone) : "";
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
