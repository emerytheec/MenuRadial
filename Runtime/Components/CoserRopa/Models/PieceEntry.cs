using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Bender_Dios.MenuRadial.Components.MenuRadial;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Representa una pieza detectada dentro del avatar (ropa, peluca, accesorio, etc.).
    /// Contiene la referencia al GameObject, su armature y estado de seleccion.
    /// </summary>
    [Serializable]
    public class PieceEntry
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

        [SerializeField] private PieceType _pieceType = PieceType.Ropa;
        [SerializeField] private bool _pieceTypeManuallySet = false;
        [SerializeField] private string _maTargetInfo = "";
        [SerializeField] private StitchZone _manualStitchZone = StitchZone.FullBody;
        [SerializeField] private bool _hasManualStitchZone = false;
        [SerializeField] private bool _disableMA = false;
        [SerializeField] private bool _allMeshesHeadWeighted = false;
        [SerializeField] private bool _isBoneProxyMisplaced = false;

        /// <summary>
        /// GameObject raiz de la pieza
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
        /// Nombre de la pieza (para mostrar en UI)
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Indica si esta pieza esta habilitada para coser
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Referencia al armature de la pieza
        /// </summary>
        public ArmatureReference ArmatureReference
        {
            get => _armatureReference;
            set => _armatureReference = value;
        }

        /// <summary>
        /// Mapeos de huesos detectados para esta pieza
        /// </summary>
        public List<BoneMapping> BoneMappings
        {
            get => _boneMappings;
            set => _boneMappings = value;
        }

        /// <summary>
        /// Resultado del ultimo cosido de esta pieza
        /// </summary>
        public StitchingResult LastResult
        {
            get => _lastResult;
            set => _lastResult = value;
        }

        /// <summary>
        /// Indica si la pieza es valida (tiene GameObject y armature)
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
        /// Prefijo en los nombres de huesos de esta pieza (ej: "Outfit_")
        /// Se elimina durante el matching para mejor deteccion
        /// </summary>
        public string BonePrefix
        {
            get => _bonePrefix ?? "";
            set => _bonePrefix = value ?? "";
        }

        /// <summary>
        /// Sufijo en los nombres de huesos de esta pieza (ej: ".001")
        /// Se elimina durante el matching para mejor deteccion
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
        /// Indica si esta pieza tiene componentes de Modular Avatar configurados.
        /// Si es true, MRCoserRopa no procesara esta pieza y dejara que MA lo haga.
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
        /// Indica si esta pieza debe ser procesada por MRCoserRopa.
        /// False si tiene Modular Avatar configurado (MA tiene prioridad),
        /// a menos que el usuario haya desactivado MA para esta pieza.
        /// </summary>
        public bool ShouldProcessByMR => _disableMA || !_hasModularAvatar;

        /// <summary>
        /// Indica si esta pieza tiene MA Shape Changer configurado.
        /// Si es true, los blendshapes son controlados por MA.
        /// </summary>
        public bool HasMAShapeChanger
        {
            get => _hasMAShapeChanger;
            set => _hasMAShapeChanger = value;
        }

        /// <summary>
        /// Zona del cuerpo donde se cose la pieza (clasificada por huesos detectados)
        /// </summary>
        public StitchZone StitchZone
        {
            get => _stitchZone;
            set => _stitchZone = value;
        }

        /// <summary>
        /// Indica si esta pieza fue identificada como peluca por WigDetector
        /// </summary>
        public bool IsWig
        {
            get => _isWig;
            set => _isWig = value;
        }

        /// <summary>
        /// Tipo de pieza auto-clasificado (Ropa, Pelo, Pieza)
        /// </summary>
        public PieceType PieceType
        {
            get => _pieceType;
            set => _pieceType = value;
        }

        /// <summary>
        /// Indica si el usuario cambio manualmente el tipo de pieza
        /// </summary>
        public bool PieceTypeManuallySet
        {
            get => _pieceTypeManuallySet;
            set => _pieceTypeManuallySet = value;
        }

        /// <summary>
        /// Informacion legible del destino MA (ej: "Head", "Armature")
        /// </summary>
        public string MATargetInfo
        {
            get => _maTargetInfo ?? "";
            set => _maTargetInfo = value ?? "";
        }

        /// <summary>
        /// Zona de cosido elegida manualmente por el usuario
        /// </summary>
        public StitchZone ManualStitchZone
        {
            get => _manualStitchZone;
            set => _manualStitchZone = value;
        }

        /// <summary>
        /// Indica si el usuario eligio una zona manual
        /// </summary>
        public bool HasManualStitchZone
        {
            get => _hasManualStitchZone;
            set => _hasManualStitchZone = value;
        }

        /// <summary>
        /// Si true, desactiva MA para usar cosido MR en esta pieza
        /// </summary>
        public bool DisableMA
        {
            get => _disableMA;
            set => _disableMA = value;
        }

        /// <summary>
        /// Indica si TODOS los meshes de esta pieza tienen peso concentrado en huesos de la cabeza.
        /// Calculado por BoneWeightAnalyzer durante la detección.
        /// True = peluca pura (todos los meshes pesan en Head), incluso con esqueleto completo.
        /// False = ropa (al menos un mesh tiene peso en el cuerpo).
        /// </summary>
        public bool AllMeshesHeadWeighted
        {
            get => _allMeshesHeadWeighted;
            set => _allMeshesHeadWeighted = value;
        }

        /// <summary>
        /// Indica si el MA BoneProxy esta mal ubicado (en la raiz de la pieza en vez de un hijo).
        /// Cuando BoneProxy esta en la raiz, MA reparenta todo el GameObject bajo el hueso destino,
        /// lo cual puede causar problemas. Lo correcto es que este en el Armature hijo.
        /// </summary>
        public bool IsBoneProxyMisplaced
        {
            get => _isBoneProxyMisplaced;
            set => _isBoneProxyMisplaced = value;
        }

        /// <summary>
        /// Zona efectiva de cosido: manual si el usuario la eligio, automatica si no
        /// </summary>
        public StitchZone EffectiveStitchZone =>
            _hasManualStitchZone ? _manualStitchZone : _stitchZone;

        /// <summary>
        /// Determina el tipo de pieza basado en zona, estado de peluca, MA BoneProxy,
        /// nombre y análisis de bone weights.
        /// Lógica:
        /// - isWig + zona Head/None → Pelo (confiar en WigDetector)
        /// - isWig + zona cuerpo + todos meshes head-weighted → Pelo (peluca con MA skeleton)
        /// - isWig + zona cuerpo + nombre pelo (fallback) → Pelo
        /// - isWig + zona cuerpo + meshes mixtos → Ropa (falso positivo)
        /// - BoneProxy→Head + nombre pelo → Pelo
        /// - BoneProxy→Head + zona Head → Pelo
        /// - Zona Head + nombre pelo → Pelo
        /// - Zona de cuerpo → Ropa
        /// - Zona Head sin señales → Pieza (sombreros)
        /// - Zona None → Pieza (accesorios)
        /// </summary>
        public static PieceType DeterminePieceType(
            StitchZone zone, bool isWig, bool hasMABoneProxyToHead,
            string pieceName = null, bool allMeshesHeadWeighted = false)
        {
            bool nameMatchesHair = MatchesHairPattern(pieceName);

            if (isWig)
            {
                // Zona Head/None: confiar en WigDetector
                if (!IsBodyZone(zone))
                    return PieceType.Pelo;

                // Zona de cuerpo (FullBody, etc.): pelucas con MergeArmature tienen
                // esqueleto completo. Confirmar con bone weights o nombre.

                // Dato objetivo: si TODOS los meshes pesan en la cabeza → peluca real
                if (allMeshesHeadWeighted)
                    return PieceType.Pelo;

                // Fallback: nombre contiene patrón de pelo
                if (nameMatchesHair)
                    return PieceType.Pelo;

                // Sin confirmación → falso positivo (ropa con accesorio de cabeza)
                return PieceType.Ropa;
            }

            // BoneProxy→Head con señales de pelo
            if (hasMABoneProxyToHead && (nameMatchesHair || allMeshesHeadWeighted))
                return PieceType.Pelo;

            // BoneProxy→Head + zona Head
            if (hasMABoneProxyToHead && zone == StitchZone.Head)
                return PieceType.Pelo;

            // Zona Head con nombre de pelo (sin BoneProxy)
            if (zone == StitchZone.Head && (nameMatchesHair || allMeshesHeadWeighted))
                return PieceType.Pelo;

            // Cualquier zona de cuerpo → Ropa
            if (IsBodyZone(zone))
                return PieceType.Ropa;

            // Zona Head sin señales de pelo → Pieza (sombreros, cascos)
            // Zona None → Pieza (accesorios sin huesos humanoid)
            return PieceType.Pieza;
        }

        /// <summary>
        /// Verifica si la zona corresponde a partes del cuerpo (no cabeza ni vacía).
        /// </summary>
        private static bool IsBodyZone(StitchZone zone)
        {
            switch (zone)
            {
                case StitchZone.FullBody:
                case StitchZone.Torso:
                case StitchZone.Chest:
                case StitchZone.Hip:
                case StitchZone.UpperLimb:
                case StitchZone.LowerLimb:
                case StitchZone.RightHand:
                case StitchZone.LeftHand:
                case StitchZone.RightFoot:
                case StitchZone.LeftFoot:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifica si un nombre coincide con patrones de pelo/peluca.
        /// Mismos patrones que WigDetector.HairNamePatterns.
        /// </summary>
        private static bool MatchesHairPattern(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string lower = name.ToLowerInvariant();
            string[] hairPatterns = {
                "hair", "wig", "pelo", "cabello", "peluca",
                "bangs", "fringe", "ponytail", "braid", "strand",
                "flequillo", "coleta", "trenza"
            };

            foreach (var pattern in hairPatterns)
            {
                if (lower.Contains(pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Infiere StitchZone a partir del target de Modular Avatar (BoneProxy o MergeArmature).
        /// Usado como fallback cuando los bone mappings no dan zona concluyente (None o FullBody
        /// por defecto) pero MA tiene información valiosa del destino.
        /// </summary>
        /// <param name="maTargetInfo">Nombre del destino MA (ej: "Head", "Hips", "Armature")</param>
        /// <returns>StitchZone inferida, o null si no se puede determinar</returns>
        public static StitchZone? InferZoneFromMATarget(string maTargetInfo)
        {
            if (string.IsNullOrEmpty(maTargetInfo))
                return null;

            string normalized = maTargetInfo.ToLowerInvariant()
                .Replace("_", "").Replace(".", "").Replace("-", "").Replace(" ", "");

            // Head / Neck
            if (normalized == "head" || normalized.EndsWith("head"))
                return StitchZone.Head;
            if (normalized == "neck" || normalized.EndsWith("neck"))
                return StitchZone.Head;

            // Hips
            if (normalized == "hips" || normalized.EndsWith("hips"))
                return StitchZone.Hip;

            // Chest
            if (normalized == "chest" || normalized.EndsWith("chest") ||
                normalized == "upperchest" || normalized.EndsWith("upperchest"))
                return StitchZone.Chest;

            // Spine / Torso
            if (normalized == "spine" || normalized.EndsWith("spine"))
                return StitchZone.Torso;

            // Hands
            if (normalized == "lefthand" || normalized.EndsWith("lefthand"))
                return StitchZone.LeftHand;
            if (normalized == "righthand" || normalized.EndsWith("righthand"))
                return StitchZone.RightHand;

            // Feet
            if (normalized == "leftfoot" || normalized.EndsWith("leftfoot") ||
                normalized == "lefttoes" || normalized.EndsWith("lefttoes"))
                return StitchZone.LeftFoot;
            if (normalized == "rightfoot" || normalized.EndsWith("rightfoot") ||
                normalized == "righttoes" || normalized.EndsWith("righttoes"))
                return StitchZone.RightFoot;

            // Arms
            if (normalized.Contains("leftarm") || normalized.Contains("leftshoulder") ||
                normalized.Contains("leftlowerarm") || normalized.Contains("leftupperarm"))
                return StitchZone.UpperLimb;
            if (normalized.Contains("rightarm") || normalized.Contains("rightshoulder") ||
                normalized.Contains("rightlowerarm") || normalized.Contains("rightupperarm"))
                return StitchZone.UpperLimb;

            // Legs
            if (normalized.Contains("leftleg") || normalized.Contains("leftupperleg") ||
                normalized.Contains("leftlowerleg"))
                return StitchZone.LowerLimb;
            if (normalized.Contains("rightleg") || normalized.Contains("rightupperleg") ||
                normalized.Contains("rightlowerleg"))
                return StitchZone.LowerLimb;

            // "Armature" genérico no da info específica
            return null;
        }

        /// <summary>
        /// Determina la zona de cosido basada en los huesos mapeados.
        /// </summary>
        public static StitchZone DetermineStitchZone(List<BoneMapping> boneMappings)
        {
            if (boneMappings == null || boneMappings.Count == 0)
                return StitchZone.None;

            bool hasTorso = false;
            bool hasChest = false;
            bool hasHips = false;
            bool hasHead = false;
            bool hasNeck = false;
            bool hasLeftArm = false;
            bool hasRightArm = false;
            bool hasLeftHand = false;
            bool hasRightHand = false;
            bool hasLeftLeg = false;
            bool hasRightLeg = false;
            bool hasLeftFoot = false;
            bool hasRightFoot = false;

            foreach (var mapping in boneMappings)
            {
                if (!mapping.IsValid)
                    continue;

                var bone = mapping.BoneType;

                switch (bone)
                {
                    case HumanBodyBones.Spine:
                        hasTorso = true;
                        break;

                    case HumanBodyBones.Chest:
                    case HumanBodyBones.UpperChest:
                        hasTorso = true;
                        hasChest = true;
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
                    case HumanBodyBones.LeftUpperArm:
                    case HumanBodyBones.LeftLowerArm:
                        hasLeftArm = true;
                        break;

                    case HumanBodyBones.RightShoulder:
                    case HumanBodyBones.RightUpperArm:
                    case HumanBodyBones.RightLowerArm:
                        hasRightArm = true;
                        break;

                    case HumanBodyBones.LeftHand:
                        hasLeftHand = true;
                        hasLeftArm = true;
                        break;

                    case HumanBodyBones.RightHand:
                        hasRightHand = true;
                        hasRightArm = true;
                        break;

                    case HumanBodyBones.LeftUpperLeg:
                    case HumanBodyBones.LeftLowerLeg:
                        hasLeftLeg = true;
                        break;

                    case HumanBodyBones.RightUpperLeg:
                    case HumanBodyBones.RightLowerLeg:
                        hasRightLeg = true;
                        break;

                    case HumanBodyBones.LeftFoot:
                    case HumanBodyBones.LeftToes:
                        hasLeftFoot = true;
                        hasLeftLeg = true;
                        break;

                    case HumanBodyBones.RightFoot:
                    case HumanBodyBones.RightToes:
                        hasRightFoot = true;
                        hasRightLeg = true;
                        break;
                }
            }

            bool hasUpperLimb = hasLeftArm || hasRightArm;
            bool hasLowerLimb = hasLeftLeg || hasRightLeg;

            if ((hasTorso || hasHips) && (hasUpperLimb || hasLowerLimb))
                return StitchZone.FullBody;

            if (hasTorso && !hasUpperLimb && !hasLowerLimb && !hasChest)
                return StitchZone.Torso;

            if (hasChest && !hasUpperLimb && !hasLowerLimb && !hasHips)
                return StitchZone.Chest;

            if ((hasHead || hasNeck) && !hasTorso && !hasHips && !hasUpperLimb && !hasLowerLimb)
                return StitchZone.Head;

            if (hasRightHand && !hasLeftHand && !hasTorso && !hasHips && !hasLowerLimb)
                return StitchZone.RightHand;

            if (hasLeftHand && !hasRightHand && !hasTorso && !hasHips && !hasLowerLimb)
                return StitchZone.LeftHand;

            if (hasUpperLimb && !hasTorso && !hasHips && !hasLowerLimb)
                return StitchZone.UpperLimb;

            if (hasRightFoot && !hasLeftFoot && !hasTorso && !hasHips && !hasUpperLimb)
                return StitchZone.RightFoot;

            if (hasLeftFoot && !hasRightFoot && !hasTorso && !hasHips && !hasUpperLimb)
                return StitchZone.LeftFoot;

            if (hasLowerLimb && !hasTorso && !hasHips && !hasUpperLimb)
                return StitchZone.LowerLimb;

            if (hasHips && !hasTorso && !hasUpperLimb && !hasLowerLimb)
                return StitchZone.Hip;

            return StitchZone.FullBody;
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public PieceEntry()
        {
            _boneMappings = new List<BoneMapping>();
        }

        /// <summary>
        /// Constructor con GameObject
        /// </summary>
        public PieceEntry(GameObject gameObject) : this()
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
