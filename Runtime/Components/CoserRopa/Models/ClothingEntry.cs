using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Representa una prenda de ropa detectada dentro del avatar.
    /// Contiene la referencia al GameObject, su armature y estado de seleccion.
    /// </summary>
    [Serializable]
    public class ClothingEntry
    {
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private string _name;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private ArmatureReference _armatureReference;
        [SerializeField] private List<BoneMapping> _boneMappings;
        [SerializeField] private StitchingResult _lastResult;

        [SerializeField] private string _bonePrefix = "";
        [SerializeField] private string _boneSuffix = "";

        [SerializeField] private bool _hasModularAvatar = false;
        [SerializeField] private string _modularAvatarComponentType = "";
        [SerializeField] private bool _hasMAShapeChanger = false;

        [SerializeField] private StitchZone _stitchZone = StitchZone.FullBody;
        [SerializeField] private bool _isWig = false;

        /// <summary>
        /// GameObject raiz de la ropa
        /// </summary>
        public GameObject GameObject
        {
            get => _gameObject;
            set
            {
                _gameObject = value;
                _name = value != null ? value.name : "";
            }
        }

        /// <summary>
        /// Nombre de la ropa (para mostrar en UI)
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Indica si esta ropa esta habilitada para coser
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Referencia al armature de la ropa
        /// </summary>
        public ArmatureReference ArmatureReference
        {
            get => _armatureReference;
            set => _armatureReference = value;
        }

        /// <summary>
        /// Mapeos de huesos detectados para esta ropa
        /// </summary>
        public List<BoneMapping> BoneMappings
        {
            get => _boneMappings;
            set => _boneMappings = value;
        }

        /// <summary>
        /// Resultado del ultimo cosido de esta ropa
        /// </summary>
        public StitchingResult LastResult
        {
            get => _lastResult;
            set => _lastResult = value;
        }

        /// <summary>
        /// Indica si la ropa es valida (tiene GameObject y armature)
        /// </summary>
        public bool IsValid => _gameObject != null &&
                               _armatureReference != null &&
                               _armatureReference.IsValid;

        /// <summary>
        /// Cantidad de huesos mapeados correctamente
        /// </summary>
        public int MappedBoneCount => _boneMappings?.FindAll(m => m.IsValid).Count ?? 0;

        /// <summary>
        /// Cantidad total de mapeos
        /// </summary>
        public int TotalBoneCount => _boneMappings?.Count ?? 0;

        /// <summary>
        /// Indica si tiene mapeos validos
        /// </summary>
        public bool HasValidMappings => MappedBoneCount > 0;

        /// <summary>
        /// Indica si fue cosida exitosamente
        /// </summary>
        public bool WasStitched => _lastResult != null && _lastResult.Success;

        /// <summary>
        /// Prefijo en los nombres de huesos de esta ropa (ej: "Outfit_")
        /// Se elimina durante el matching para mejor detección
        /// </summary>
        public string BonePrefix
        {
            get => _bonePrefix ?? "";
            set => _bonePrefix = value ?? "";
        }

        /// <summary>
        /// Sufijo en los nombres de huesos de esta ropa (ej: ".001")
        /// Se elimina durante el matching para mejor detección
        /// </summary>
        public string BoneSuffix
        {
            get => _boneSuffix ?? "";
            set => _boneSuffix = value ?? "";
        }

        /// <summary>
        /// Indica si tiene prefijo o sufijo configurado
        /// </summary>
        public bool HasCustomNaming => !string.IsNullOrEmpty(_bonePrefix) || !string.IsNullOrEmpty(_boneSuffix);

        /// <summary>
        /// Indica si esta ropa tiene componentes de Modular Avatar configurados.
        /// Si es true, MRCoserRopa no procesará esta ropa y dejará que MA lo haga.
        /// </summary>
        public bool HasModularAvatar
        {
            get => _hasModularAvatar;
            set => _hasModularAvatar = value;
        }

        /// <summary>
        /// Tipo de componente de Modular Avatar detectado (ej: "ModularAvatarMergeArmature")
        /// </summary>
        public string ModularAvatarComponentType
        {
            get => _modularAvatarComponentType ?? "";
            set => _modularAvatarComponentType = value ?? "";
        }

        /// <summary>
        /// Indica si esta ropa debe ser procesada por MRCoserRopa.
        /// False si tiene Modular Avatar configurado (MA tiene prioridad).
        /// </summary>
        public bool ShouldProcessByMR => !_hasModularAvatar;

        /// <summary>
        /// Indica si esta ropa tiene MA Shape Changer configurado.
        /// Si es true, los blendshapes son controlados por MA.
        /// </summary>
        public bool HasMAShapeChanger
        {
            get => _hasMAShapeChanger;
            set => _hasMAShapeChanger = value;
        }

        /// <summary>
        /// Zona del cuerpo donde se cose la ropa (clasificada por huesos detectados)
        /// </summary>
        public StitchZone StitchZone
        {
            get => _stitchZone;
            set => _stitchZone = value;
        }

        /// <summary>
        /// Indica si esta ropa fue identificada como peluca por WigDetector
        /// </summary>
        public bool IsWig
        {
            get => _isWig;
            set => _isWig = value;
        }

        /// <summary>
        /// Determina la zona de cosido basada en los huesos mapeados.
        /// Analiza los HumanBodyBones de los mapeos válidos.
        /// </summary>
        public static StitchZone DetermineStitchZone(List<BoneMapping> boneMappings)
        {
            if (boneMappings == null || boneMappings.Count == 0)
                return StitchZone.FullBody;

            bool hasTorso = false;
            bool hasHips = false;
            bool hasHead = false;
            bool hasNeck = false;
            bool hasUpperLimb = false;
            bool hasLowerLimb = false;

            foreach (var mapping in boneMappings)
            {
                if (!mapping.IsValid)
                    continue;

                var bone = mapping.BoneType;

                switch (bone)
                {
                    case HumanBodyBones.Spine:
                    case HumanBodyBones.Chest:
                    case HumanBodyBones.UpperChest:
                        hasTorso = true;
                        break;

                    case HumanBodyBones.Hips:
                        hasHips = true;
                        break;

                    case HumanBodyBones.Head:
                        hasHead = true;
                        break;

                    case HumanBodyBones.Neck:
                        hasNeck = true;
                        break;

                    case HumanBodyBones.LeftShoulder:
                    case HumanBodyBones.RightShoulder:
                    case HumanBodyBones.LeftUpperArm:
                    case HumanBodyBones.RightUpperArm:
                    case HumanBodyBones.LeftLowerArm:
                    case HumanBodyBones.RightLowerArm:
                    case HumanBodyBones.LeftHand:
                    case HumanBodyBones.RightHand:
                        hasUpperLimb = true;
                        break;

                    case HumanBodyBones.LeftUpperLeg:
                    case HumanBodyBones.RightUpperLeg:
                    case HumanBodyBones.LeftLowerLeg:
                    case HumanBodyBones.RightLowerLeg:
                    case HumanBodyBones.LeftFoot:
                    case HumanBodyBones.RightFoot:
                    case HumanBodyBones.LeftToes:
                    case HumanBodyBones.RightToes:
                        hasLowerLimb = true;
                        break;
                }
            }

            // Clasificación por prioridad
            if ((hasTorso || hasHips) && (hasUpperLimb || hasLowerLimb))
                return StitchZone.FullBody;

            if (hasTorso && !hasUpperLimb && !hasLowerLimb)
                return StitchZone.Torso;

            if ((hasHead || hasNeck) && !hasTorso && !hasHips && !hasUpperLimb && !hasLowerLimb)
                return StitchZone.Head;

            if (hasUpperLimb && !hasTorso && !hasHips && !hasLowerLimb)
                return StitchZone.UpperLimb;

            if (hasLowerLimb && !hasTorso && !hasHips && !hasUpperLimb)
                return StitchZone.LowerLimb;

            if (hasHips && !hasTorso && !hasUpperLimb && !hasLowerLimb)
                return StitchZone.Hip;

            return StitchZone.FullBody;
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ClothingEntry()
        {
            _boneMappings = new List<BoneMapping>();
        }

        /// <summary>
        /// Constructor con GameObject
        /// </summary>
        public ClothingEntry(GameObject gameObject) : this()
        {
            GameObject = gameObject;
            _armatureReference = new ArmatureReference(gameObject);
        }

        /// <summary>
        /// Limpia los mapeos de huesos
        /// </summary>
        public void ClearMappings()
        {
            _boneMappings?.Clear();
            _lastResult = null;
        }
    }
}
