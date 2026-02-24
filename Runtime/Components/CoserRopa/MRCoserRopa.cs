using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;
using Bender_Dios.MenuRadial.Components.MenuRadial;

// Detector de Modular Avatar para prioridad de cosido

namespace Bender_Dios.MenuRadial.Components.CoserRopa
{
    /// <summary>
    /// MR Coser Ropa - Componente de Cosido de Ropa
    /// Detecta automaticamente las ropas dentro de un avatar y las cose al armature principal.
    ///
    /// Proceso:
    /// 1. Usuario arrastra el avatar (que contiene las ropas como hijos)
    /// 2. Sistema detecta automaticamente el avatar y las ropas con armature propio
    /// 3. Usuario puede excluir ropas de la seleccion
    /// 4. Un boton cose todas las ropas habilitadas
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Coser Ropa")]
    [Icon("Packages/com.bender-dios.menu-radial/Components/Menu/Resources/logo_MR.png")]
    public class MRCoserRopa : MRComponentBase
    {
        #region Serialized Fields

        [Header("Avatar")]
        [SerializeField]
        [Tooltip("GameObject raiz del avatar (arrastra aqui tu avatar con las ropas dentro)")]
        private GameObject _avatarRoot;

        [Header("Configuracion")]
        [SerializeField]
        [Tooltip("Modo de cosido:\n- Coser: Reparenta huesos (duplicados)\n- Fusionar: Usa huesos del avatar (sin duplicados, como Modular Avatar)")]
        private StitchingMode _stitchingMode = StitchingMode.Merge;

        [SerializeField]
        [Tooltip("Mostrar detalles de mapeos de huesos en el inspector")]
        private bool _showBoneMappings = false;

        [Header("Ropas Detectadas")]
        [FormerlySerializedAs("_detectedClothings")]
        [SerializeField]
        private List<PieceEntry> _detectedPieces = new List<PieceEntry>();

        [FormerlySerializedAs("_selectedClothingIndex")]
        [SerializeField, HideInInspector]
        private int _selectedPieceIndex = -1;

        // Estado interno
        [SerializeField, HideInInspector]
        private ArmatureReference _avatarReference;

        [SerializeField, HideInInspector]
        private StitchingResult _lastStitchingResult;

        #endregion

        #region Controllers (Lazy Loading)

        private HumanoidBoneMapper _boneMapper;
        private BoneStitchingController _stitchingController;

        private HumanoidBoneMapper BoneMapper =>
            _boneMapper ??= new HumanoidBoneMapper();

        private BoneStitchingController StitchingController =>
            _stitchingController ??= new BoneStitchingController();

        #endregion

        #region Public Properties

        /// <summary>
        /// GameObject raiz del avatar
        /// </summary>
        public GameObject AvatarRoot
        {
            get => _avatarRoot;
            set
            {
                if (_avatarRoot != value)
                {
                    _avatarRoot = value;
                    OnAvatarChanged();
                }
            }
        }

        /// <summary>
        /// Referencia al armature del avatar
        /// </summary>
        public ArmatureReference AvatarReference => _avatarReference;

        /// <summary>
        /// Lista de piezas detectadas
        /// </summary>
        public List<PieceEntry> DetectedPieces => _detectedPieces;

        /// <summary>
        /// Piezas habilitadas para coser por MRCoserRopa.
        /// Excluye las que tienen Modular Avatar configurado (MA tiene prioridad).
        /// </summary>
        public IEnumerable<PieceEntry> EnabledPieces =>
            _detectedPieces?.Where(c => c.Enabled && c.IsValid && c.ShouldProcessByMR) ?? Enumerable.Empty<PieceEntry>();

        /// <summary>
        /// Piezas que serán procesadas por Modular Avatar
        /// </summary>
        public IEnumerable<PieceEntry> ModularAvatarPieces =>
            _detectedPieces?.Where(c => c.HasModularAvatar) ?? Enumerable.Empty<PieceEntry>();

        /// <summary>
        /// Cantidad de piezas que serán procesadas por Modular Avatar
        /// </summary>
        public int ModularAvatarPieceCount => ModularAvatarPieces.Count();

        /// <summary>
        /// Cantidad de piezas detectadas
        /// </summary>
        public int DetectedPieceCount => _detectedPieces?.Count ?? 0;

        /// <summary>
        /// Cantidad de piezas habilitadas
        /// </summary>
        public int EnabledPieceCount => EnabledPieces.Count();

        /// <summary>
        /// Resultado del ultimo cosido
        /// </summary>
        public StitchingResult LastStitchingResult => _lastStitchingResult;

        /// <summary>
        /// Mostrar mapeos en inspector
        /// </summary>
        public bool ShowBoneMappings
        {
            get => _showBoneMappings;
            set => _showBoneMappings = value;
        }

        /// <summary>
        /// Modo de cosido (Stitch o Merge)
        /// </summary>
        public StitchingMode StitchingMode
        {
            get => _stitchingMode;
            set => _stitchingMode = value;
        }

        /// <summary>
        /// Indica si el avatar esta configurado como Humanoid
        /// </summary>
        public bool IsAvatarHumanoid => _avatarReference?.IsHumanoid ?? false;

        /// <summary>
        /// Indica si hay piezas listas para coser
        /// </summary>
        public bool HasPiecesToStitch => EnabledPieces.Any(c => c.HasValidMappings);

        /// <summary>
        /// Total de huesos mapeados en todas las piezas habilitadas
        /// </summary>
        public int TotalMappedBones => EnabledPieces.Sum(c => c.MappedBoneCount);

        /// <summary>
        /// Indice de la pieza seleccionada para mostrar detalles
        /// </summary>
        public int SelectedPieceIndex
        {
            get => _selectedPieceIndex;
            set => _selectedPieceIndex = Mathf.Clamp(value, -1, _detectedPieces.Count - 1);
        }

        /// <summary>
        /// Pieza actualmente seleccionada (puede ser null)
        /// </summary>
        public PieceEntry SelectedPiece =>
            _selectedPieceIndex >= 0 && _selectedPieceIndex < _detectedPieces.Count
                ? _detectedPieces[_selectedPieceIndex]
                : null;

        /// <summary>
        /// Cache de huesos del avatar para el selector
        /// </summary>
        private Transform[] _avatarBonesCache;
        private string[] _avatarBoneNamesCache;

        /// <summary>
        /// Obtiene todos los huesos del armature del avatar
        /// </summary>
        public Transform[] GetAvatarBones()
        {
            if (_avatarBonesCache != null)
                return _avatarBonesCache;

            if (_avatarReference?.ArmatureRoot == null)
                return new Transform[0];

            var bones = new List<Transform>();
            CollectBonesRecursive(_avatarReference.ArmatureRoot, bones);
            _avatarBonesCache = bones.ToArray();
            return _avatarBonesCache;
        }

        /// <summary>
        /// Obtiene los nombres de los huesos del avatar (para dropdowns)
        /// </summary>
        public string[] GetAvatarBoneNames()
        {
            if (_avatarBoneNamesCache != null)
                return _avatarBoneNamesCache;

            var bones = GetAvatarBones();
            _avatarBoneNamesCache = new string[bones.Length + 1];
            _avatarBoneNamesCache[0] = "(Ninguno)";

            for (int i = 0; i < bones.Length; i++)
            {
                _avatarBoneNamesCache[i + 1] = bones[i].name;
            }

            return _avatarBoneNamesCache;
        }

        /// <summary>
        /// Busca un hueso del avatar por nombre
        /// </summary>
        public Transform FindAvatarBoneByName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return null;

            var bones = GetAvatarBones();
            return bones.FirstOrDefault(b => b.name == boneName);
        }

        /// <summary>
        /// Obtiene el indice de un hueso en el array de huesos (para dropdown)
        /// </summary>
        public int GetAvatarBoneIndex(Transform bone)
        {
            if (bone == null)
                return 0; // (Ninguno)

            var bones = GetAvatarBones();
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == bone)
                    return i + 1; // +1 porque indice 0 es "(Ninguno)"
            }
            return 0;
        }

        /// <summary>
        /// Obtiene el hueso del avatar por indice del dropdown
        /// </summary>
        public Transform GetAvatarBoneByIndex(int index)
        {
            if (index <= 0)
                return null;

            var bones = GetAvatarBones();
            int boneIndex = index - 1; // -1 porque indice 0 es "(Ninguno)"

            if (boneIndex >= 0 && boneIndex < bones.Length)
                return bones[boneIndex];

            return null;
        }

        /// <summary>
        /// Invalida el cache de huesos (llamar cuando cambie el avatar)
        /// </summary>
        public void InvalidateBoneCache()
        {
            _avatarBonesCache = null;
            _avatarBoneNamesCache = null;
        }

        private void CollectBonesRecursive(Transform parent, List<Transform> bones)
        {
            bones.Add(parent);
            foreach (Transform child in parent)
            {
                CollectBonesRecursive(child, bones);
            }
        }

        #endregion

        #region Lifecycle Methods

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            _detectedPieces ??= new List<PieceEntry>();
            if (_avatarRoot == null)
                _avatarRoot = FindAvatarInParents();
        }

        protected override void CleanupComponent()
        {
            base.CleanupComponent();
            _boneMapper = null;
            _stitchingController = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Llamado cuando cambia el avatar asignado
        /// </summary>
        public void OnAvatarChanged()
        {
            _detectedPieces.Clear();
            _lastStitchingResult = null;
            InvalidateBoneCache();

            if (_avatarRoot == null)
            {
                _avatarReference = null;
                return;
            }

            // Crear referencia del avatar
            _avatarReference = new ArmatureReference(_avatarRoot);

            // Detectar piezas automaticamente
            DetectPiecesInAvatar();
        }

        /// <summary>
        /// Detecta todas las piezas dentro del avatar.
        /// Una pieza valida es un hijo directo del avatar que cumple al menos uno de:
        /// 1. Tiene SkinnedMeshRenderer con huesos propios (no del armature del avatar)
        /// 2. Tiene componentes de Modular Avatar (MergeArmature, BoneProxy)
        /// 3. Tiene armature propio (detectado por ArmatureFinder)
        /// Las piezas se auto-clasifican como Ropa, Pelo o Pieza segun sus huesos y MA.
        /// </summary>
        public void DetectPiecesInAvatar()
        {
            _detectedPieces.Clear();

            if (_avatarRoot == null || _avatarReference == null)
            {
                Debug.LogWarning("[MRCoserRopa] No hay avatar asignado");
                return;
            }

            Debug.Log($"[MRCoserRopa] Buscando ropas en '{_avatarRoot.name}'...");

            // Obtener el armature del avatar para comparar
            Transform avatarArmature = _avatarReference.ArmatureRoot;

            // Buscar todos los SkinnedMeshRenderer en el avatar
            var allSMRs = _avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // Diccionario para agrupar por GameObject contenedor de pieza
            var pieceCandidates = new Dictionary<GameObject, PieceCandidate>();

            foreach (var smr in allSMRs)
            {
                // Ignorar SMRs sin huesos
                if (smr.bones == null || smr.bones.Length == 0)
                    continue;

                // Obtener el primer hueso valido para analizar
                Transform firstBone = null;
                foreach (var bone in smr.bones)
                {
                    if (bone != null)
                    {
                        firstBone = bone;
                        break;
                    }
                }

                if (firstBone == null)
                    continue;

                // Verificar si los huesos estan dentro del armature del avatar
                // Si es asi, este SMR ya esta usando los huesos del avatar (ya cosido o es parte del avatar)
                if (avatarArmature != null && IsDescendantOf(firstBone, avatarArmature))
                {
                    continue; // Este SMR ya usa el armature del avatar
                }

                // Encontrar el GameObject contenedor de la pieza
                // Es el hijo directo del avatar que contiene este SMR
                GameObject pieceRoot = FindPieceRoot(smr.transform, _avatarRoot.transform);
                if (pieceRoot == null || pieceRoot == _avatarRoot)
                    continue;

                // Verificar que los huesos esten dentro del pieceRoot
                if (!IsDescendantOf(firstBone, pieceRoot.transform))
                    continue;

                // Agregar o actualizar candidato
                if (!pieceCandidates.ContainsKey(pieceRoot))
                {
                    pieceCandidates[pieceRoot] = new PieceCandidate
                    {
                        Root = pieceRoot,
                        SkinnedMeshRenderers = new List<SkinnedMeshRenderer>()
                    };
                }
                pieceCandidates[pieceRoot].SkinnedMeshRenderers.Add(smr);
            }

            // Procesar candidatos validos
            foreach (var kvp in pieceCandidates)
            {
                var candidate = kvp.Value;

                // Detectar si tiene Modular Avatar configurado
                var maResult = ModularAvatarDetector.Instance.DetectModularAvatar(candidate.Root);

                // Crear referencia de armature para esta pieza
                var pieceRef = new ArmatureReference(candidate.Root);

                var entry = new PieceEntry(candidate.Root)
                {
                    ArmatureReference = pieceRef,
                    Enabled = true
                };

                // Marcar como MA si tiene MergeArmature o BoneProxy
                if (maResult.HasModularAvatar)
                {
                    entry.HasModularAvatar = true;
                    entry.ModularAvatarComponentType = maResult.PrimaryComponent;
                    entry.MATargetInfo = ModularAvatarDetector.Instance.GetMATargetInfo(candidate.Root);
                    Debug.Log($"[MRCoserRopa] Pieza '{candidate.Root.name}' tiene Modular Avatar ({maResult.PrimaryComponent}). MA tendrá prioridad.");

                    // Detectar si BoneProxy esta mal ubicado (en la raiz en vez de un hijo)
                    if (maResult.PrimaryComponent == "ModularAvatarBoneProxy" ||
                        maResult.DetectedComponents.Contains("ModularAvatarBoneProxy"))
                    {
                        entry.IsBoneProxyMisplaced = ModularAvatarDetector.Instance.IsBoneProxyOnPieceRoot(candidate.Root);
                        if (entry.IsBoneProxyMisplaced)
                            Debug.LogWarning($"[MRCoserRopa] BoneProxy mal ubicado en '{candidate.Root.name}': esta en la raiz, deberia estar en un hijo (Armature)");
                    }
                }

                // Detectar si tiene MA Shape Changer (controla blendshapes)
                if (maResult.HasShapeChanger || maResult.HasBlendshapeControl)
                {
                    entry.HasMAShapeChanger = true;
                }

                // Detectar mapeos de huesos
                DetectBoneMappingsForPiece(entry);

                // Clasificar zona de cosido
                entry.StitchZone = PieceEntry.DetermineStitchZone(entry.BoneMappings);

                // Fallback: si los bone mappings no dieron zona concluyente,
                // usar la info de MA como hint (ej: BoneProxy→Head → zona Head)
                if ((entry.StitchZone == StitchZone.None || entry.StitchZone == StitchZone.FullBody)
                    && entry.HasModularAvatar && !string.IsNullOrEmpty(entry.MATargetInfo))
                {
                    var maZone = PieceEntry.InferZoneFromMATarget(entry.MATargetInfo);
                    if (maZone.HasValue)
                    {
                        Debug.Log($"[MRCoserRopa] '{entry.Name}': zona {entry.StitchZone} → {maZone.Value} (inferida de MA: {entry.MATargetInfo})");
                        entry.StitchZone = maZone.Value;
                    }
                }

                // Agregar solo si tiene mapeos validos O tiene Modular Avatar
                // El primer pase ya filtro SMRs cuyos huesos apuntan al avatar (linea 388),
                // asi que si llegamos aqui, la pieza tiene huesos propios.
                // NO usar pieceRef.ArmatureRoot como criterio — ArmatureFinder es demasiado
                // permisivo con su fallback y detecta "armatures" donde no los hay.
                if (entry.MappedBoneCount > 0 || entry.HasModularAvatar)
                {
                    _detectedPieces.Add(entry);

                    Debug.Log($"[MRCoserRopa] Pieza detectada: '{candidate.Root.name}' " +
                              $"({candidate.SkinnedMeshRenderers.Count} SMRs, {entry.MappedBoneCount} huesos" +
                              $"{(entry.HasModularAvatar ? $", MA: {entry.ModularAvatarComponentType}" : "")})");
                }
                else
                {
                    Debug.Log($"[MRCoserRopa] '{candidate.Root.name}' descartado: sin mapeos validos ni MA");
                }
            }

            // Segundo pase: detectar hijos directos con Modular Avatar que fueron filtrados
            // por el primer pase (e.g., pelucas con MA BoneProxy cuyos huesos SMR
            // apuntan al armature del avatar en vez de tener armature propio)
            DetectMAFilteredChildren(pieceCandidates, avatarArmature);

            Debug.Log($"[MRCoserRopa] Total: {_detectedPieces.Count} piezas detectadas");

            // Cross-referenciar con WigDetector para marcar pelucas
            CrossReferenceWigs();

            // Auto-clasificar PieceType para todas las piezas detectadas
            AutoClassifyPieceTypes();

            // Configurar visibilidad por defecto: roots activos, meshes desactivados
            SetDefaultPieceVisibility();
        }

        /// <summary>
        /// Segundo pase de deteccion: busca hijos directos del avatar con Modular Avatar
        /// que fueron filtrados por el loop principal de SMR.
        /// Esto ocurre cuando pelucas/accesorios usan MA BoneProxy y sus huesos SMR
        /// apuntan al armature del avatar en vez de tener armature propio.
        ///
        /// SOLO detecta hijos con componentes MA de armature (MergeArmature/BoneProxy).
        /// NO detecta hijos por tener "armature propio" — eso es demasiado permisivo
        /// y captura meshes del avatar base, herramientas, etc.
        /// </summary>
        private void DetectMAFilteredChildren(Dictionary<GameObject, PieceCandidate> pieceCandidates, Transform avatarArmature)
        {
            if (!ModularAvatarDetector.Instance.IsModularAvatarAvailable)
                return;

            // Recopilar GameObjects ya detectados (solo los que SÍ se agregaron a la lista)
            var alreadyDetected = new HashSet<GameObject>();
            foreach (var piece in _detectedPieces)
            {
                if (piece.GameObject != null)
                    alreadyDetected.Add(piece.GameObject);
            }
            // Tambien incluir candidatos del primer pase que no se agregaron (descartados)
            foreach (var kvp in pieceCandidates)
            {
                alreadyDetected.Add(kvp.Key);
            }

            // Iterar hijos directos del avatar
            for (int i = 0; i < _avatarRoot.transform.childCount; i++)
            {
                var child = _avatarRoot.transform.GetChild(i);
                if (child == null) continue;

                // Saltar el armature del avatar
                if (_avatarReference != null && child == _avatarReference.ArmatureRoot)
                    continue;

                // Saltar si ya fue procesado (detectado o descartado en el primer pase)
                if (alreadyDetected.Contains(child.gameObject))
                    continue;

                // Saltar si es un mesh del avatar base (sus SMRs usan huesos del armature del avatar)
                if (IsAvatarBaseMesh(child.gameObject, avatarArmature))
                    continue;

                // SOLO detectar hijos con componentes MA de armature (MergeArmature o BoneProxy)
                var maResult = ModularAvatarDetector.Instance.DetectModularAvatar(child.gameObject);
                if (!maResult.HasModularAvatar)
                    continue;

                // Crear entry para esta pieza
                var entry = new PieceEntry(child.gameObject)
                {
                    Enabled = true
                };

                entry.HasModularAvatar = true;
                entry.ModularAvatarComponentType = maResult.PrimaryComponent;
                entry.MATargetInfo = ModularAvatarDetector.Instance.GetMATargetInfo(child.gameObject);

                // Detectar si BoneProxy esta mal ubicado (en la raiz en vez de un hijo)
                if (maResult.PrimaryComponent == "ModularAvatarBoneProxy" ||
                    maResult.DetectedComponents.Contains("ModularAvatarBoneProxy"))
                {
                    entry.IsBoneProxyMisplaced = ModularAvatarDetector.Instance.IsBoneProxyOnPieceRoot(child.gameObject);
                    if (entry.IsBoneProxyMisplaced)
                        Debug.LogWarning($"[MRCoserRopa] BoneProxy mal ubicado en '{child.name}': esta en la raiz, deberia estar en un hijo (Armature)");
                }

                if (maResult.HasShapeChanger || maResult.HasBlendshapeControl)
                {
                    entry.HasMAShapeChanger = true;
                }

                // Intentar detectar mapeos de huesos (puede dar 0 para pelucas)
                DetectBoneMappingsForPiece(entry);

                // Clasificar zona de cosido
                entry.StitchZone = PieceEntry.DetermineStitchZone(entry.BoneMappings);

                // Fallback: si los bone mappings no dieron zona concluyente,
                // usar la info de MA como hint (ej: BoneProxy→Head → zona Head)
                if ((entry.StitchZone == StitchZone.None || entry.StitchZone == StitchZone.FullBody)
                    && !string.IsNullOrEmpty(entry.MATargetInfo))
                {
                    var maZone = PieceEntry.InferZoneFromMATarget(entry.MATargetInfo);
                    if (maZone.HasValue)
                    {
                        Debug.Log($"[MRCoserRopa] '{entry.Name}' (2do pase): zona {entry.StitchZone} → {maZone.Value} (inferida de MA: {entry.MATargetInfo})");
                        entry.StitchZone = maZone.Value;
                    }
                }

                _detectedPieces.Add(entry);
                Debug.Log($"[MRCoserRopa] Pieza detectada (2do pase): '{child.name}' (MA: {entry.ModularAvatarComponentType})");
            }
        }

        /// <summary>
        /// Verifica si un GameObject es un mesh del avatar base.
        /// Un mesh del avatar base tiene SMRs cuyos huesos apuntan al armature del avatar.
        /// Esto incluye Body, Hair, Face, etc.
        /// </summary>
        private bool IsAvatarBaseMesh(GameObject gameObject, Transform avatarArmature)
        {
            if (avatarArmature == null)
                return false;

            // Si el propio GameObject tiene un SMR con huesos en el armature del avatar, es del avatar base
            var smr = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.bones != null && smr.bones.Length > 0)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone != null)
                        return IsDescendantOf(bone, avatarArmature);
                }
            }

            // Si no tiene SMR propio, verificar si es un contenedor sin componentes de pieza
            // (ej: un GameObject vacio que no es ropa ni accesorio)
            if (smr == null)
            {
                // Sin SMR y sin hijos con SMR propio → no es una pieza
                var childSMRs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (childSMRs.Length == 0)
                    return false; // No tiene meshes, dejamos que MA lo filtre

                // Si todos sus SMRs usan huesos del avatar, es parte del avatar base
                foreach (var childSMR in childSMRs)
                {
                    if (childSMR.bones == null || childSMR.bones.Length == 0)
                        continue;

                    foreach (var bone in childSMR.bones)
                    {
                        if (bone != null)
                        {
                            if (!IsDescendantOf(bone, avatarArmature))
                                return false; // Tiene al menos un hueso fuera del avatar → no es del base
                            break; // Este SMR usa huesos del avatar, verificar el siguiente
                        }
                    }
                }
                return true; // Todos los SMRs usan huesos del avatar
            }

            return false;
        }

        /// <summary>
        /// Cross-referencia las piezas detectadas con WigDetector para marcar pelucas.
        /// </summary>
        private void CrossReferenceWigs()
        {
            if (_avatarRoot == null || _detectedPieces.Count == 0)
                return;

            var wigs = WigDetector.DetectWigs(_avatarRoot, _detectedPieces);

            foreach (var wig in wigs)
            {
                if (wig.PieceEntryIndex >= 0 && wig.PieceEntryIndex < _detectedPieces.Count)
                {
                    _detectedPieces[wig.PieceEntryIndex].IsWig = true;
                    Debug.Log($"[MRCoserRopa] '{_detectedPieces[wig.PieceEntryIndex].Name}' marcada como peluca (score={wig.Score})");
                }
            }
        }

        /// <summary>
        /// Auto-clasifica el PieceType de todas las piezas detectadas.
        /// Usa zona de cosido, estado de peluca, MA BoneProxy y análisis de bone weights
        /// para determinar el tipo. Solo clasifica si el usuario no lo ha cambiado manualmente.
        /// </summary>
        private void AutoClassifyPieceTypes()
        {
            // Obtener head bone del avatar para análisis de bone weights
            Transform avatarHeadBone = _avatarRoot != null
                ? BoneWeightAnalyzer.GetAvatarHeadBone(_avatarRoot)
                : null;

            foreach (var piece in _detectedPieces)
            {
                if (piece == null || piece.PieceTypeManuallySet)
                    continue;

                bool maToHead = piece.HasModularAvatar &&
                    ModularAvatarDetector.Instance.IsBoneProxyToHead(piece.GameObject);

                // Calcular si todos los meshes de la pieza pesan en la cabeza
                piece.AllMeshesHeadWeighted = ComputeAllMeshesHeadWeighted(piece, avatarHeadBone);

                piece.PieceType = PieceEntry.DeterminePieceType(
                    piece.StitchZone, piece.IsWig, maToHead, piece.Name, piece.AllMeshesHeadWeighted);
            }
        }

        /// <summary>
        /// Determina si TODOS los SkinnedMeshRenderers de una pieza tienen peso
        /// concentrado en huesos de la cabeza. Usado para distinguir pelucas de ropa
        /// cuando ambos tienen esqueleto completo (MergeArmature → FullBody).
        /// </summary>
        private bool ComputeAllMeshesHeadWeighted(PieceEntry piece, Transform avatarHeadBone)
        {
            if (piece?.GameObject == null)
                return false;

            var meshes = piece.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (meshes == null || meshes.Length == 0)
                return false;

            // Buscar head bone: primero en el armature de la ropa, luego del avatar
            Transform headBone = null;
            if (piece.ArmatureReference?.ArmatureRoot != null)
                headBone = BoneWeightAnalyzer.FindHeadBoneInArmature(piece.ArmatureReference.ArmatureRoot);
            if (headBone == null)
                headBone = avatarHeadBone;
            if (headBone == null)
                return false;

            // Verificar que TODOS los meshes sean head-weighted
            foreach (var smr in meshes)
            {
                if (smr == null || smr.sharedMesh == null)
                    continue;

                // Meshes sin bones se ignoran (MeshRenderer estáticos, partículas)
                if (smr.bones == null || smr.bones.Length == 0)
                    continue;

                if (!BoneWeightAnalyzer.IsHeadWeightedMesh(smr, headBone))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Configura la visibilidad por defecto de las piezas detectadas:
        /// - Root de la pieza: ACTIVO (para que MRAgruparObjetos pueda acceder)
        /// - Meshes de la pieza: DESACTIVADOS (para no ver todos los vestidos al tiempo)
        /// </summary>
        public void SetDefaultPieceVisibility()
        {
            if (_detectedPieces == null || _detectedPieces.Count == 0)
                return;

#if UNITY_EDITOR
            bool anyChange = false;
#endif

            foreach (var piece in _detectedPieces)
            {
                if (piece?.GameObject == null)
                    continue;

                // Activar el root de la pieza si está desactivado
                if (!piece.GameObject.activeSelf)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(piece.GameObject, "Activar root de ropa");
                    anyChange = true;
#endif
                    piece.GameObject.SetActive(true);
                }

                // Desactivar todos los meshes dentro de la pieza (excepto el root)
                var renderers = piece.GameObject.GetComponentsInChildren<Renderer>(true);

                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    // NO desactivar si el renderer está en el root de la pieza
                    // (algunas piezas tienen el mesh directamente en la raíz)
                    if (renderer.gameObject == piece.GameObject)
                        continue;

                    // Desactivar el GameObject del mesh si está activo
                    if (renderer.gameObject.activeSelf)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RecordObject(renderer.gameObject, "Desactivar mesh de ropa");
                        anyChange = true;
#endif
                        renderer.gameObject.SetActive(false);
                    }
                }
            }

#if UNITY_EDITOR
            if (anyChange && !Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        /// <summary>
        /// Estructura temporal para agrupar candidatos de pieza
        /// </summary>
        private class PieceCandidate
        {
            public GameObject Root;
            public List<SkinnedMeshRenderer> SkinnedMeshRenderers;
        }

        /// <summary>
        /// Verifica si un transform es descendiente de otro
        /// </summary>
        private bool IsDescendantOf(Transform child, Transform parent)
        {
            if (child == null || parent == null)
                return false;

            Transform current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.parent;
            }
            return false;
        }

        /// <summary>
        /// Encuentra el GameObject raiz de la pieza (hijo directo del avatar)
        /// </summary>
        private GameObject FindPieceRoot(Transform smrTransform, Transform avatarRoot)
        {
            Transform current = smrTransform;

            while (current != null && current.parent != null)
            {
                if (current.parent == avatarRoot)
                {
                    return current.gameObject;
                }
                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Verifica si el candidato tiene huesos con nombres humanoid
        /// </summary>
        private bool HasHumanoidBones(PieceCandidate candidate)
        {
            // Nombres de huesos humanoid comunes
            string[] humanoidPatterns = new[]
            {
                "hips", "hip", "pelvis",
                "spine",
                "chest",
                "neck",
                "head",
                "shoulder",
                "arm", "upperarm", "lowerarm", "forearm",
                "hand", "wrist",
                "leg", "upperleg", "lowerleg", "thigh", "calf",
                "foot", "ankle"
            };

            // Recopilar todos los huesos de todos los SMRs
            var allBones = new HashSet<Transform>();
            foreach (var smr in candidate.SkinnedMeshRenderers)
            {
                if (smr.bones == null) continue;
                foreach (var bone in smr.bones)
                {
                    if (bone != null)
                        allBones.Add(bone);
                }
            }

            // Contar cuantos huesos coinciden con patrones humanoid
            int humanoidCount = 0;
            foreach (var bone in allBones)
            {
                string boneName = bone.name.ToLowerInvariant()
                    .Replace("_", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .Replace(".", "");

                foreach (var pattern in humanoidPatterns)
                {
                    if (boneName.Contains(pattern))
                    {
                        humanoidCount++;
                        break;
                    }
                }
            }

            // Requerir al menos 1 hueso humanoid para considerarlo ropa valida
            // (pelucas y accesorios tienen pocos huesos: Head, Neck, etc.)
            return humanoidCount >= 1;
        }

        /// <summary>
        /// Detecta mapeos de huesos para una pieza especifica
        /// </summary>
        public void DetectBoneMappingsForPiece(PieceEntry piece)
        {
            if (_avatarReference == null || !_avatarReference.IsValid)
            {
                Debug.LogWarning("[MRCoserRopa] Avatar no valido para detectar huesos");
                return;
            }

            if (piece?.ArmatureReference == null)
            {
                Debug.LogWarning("[MRCoserRopa] Ropa no valida para detectar huesos");
                return;
            }

            // Usar prefijo/sufijo configurado por el usuario si existe
            // Si hay zona manual, filtrar solo huesos de esa zona
            if (piece.HasManualStitchZone)
            {
                piece.BoneMappings = BoneMapper.DetectBoneMappings(
                    _avatarReference,
                    piece.ArmatureReference,
                    piece.BonePrefix,
                    piece.BoneSuffix,
                    piece.ManualStitchZone);
            }
            else
            {
                piece.BoneMappings = BoneMapper.DetectBoneMappings(
                    _avatarReference,
                    piece.ArmatureReference,
                    piece.BonePrefix,
                    piece.BoneSuffix);
            }

            // Recalcular zona de cosido
            piece.StitchZone = PieceEntry.DetermineStitchZone(piece.BoneMappings);
        }

        /// <summary>
        /// Refresca la deteccion de piezas
        /// </summary>
        public void RefreshDetection()
        {
            if (_avatarRoot != null)
            {
                // Guardar estado de habilitacion actual
                var enabledStates = _detectedPieces
                    .ToDictionary(c => c.Name, c => c.Enabled);

                DetectPiecesInAvatar();

                // Restaurar estados de habilitacion
                foreach (var piece in _detectedPieces)
                {
                    if (enabledStates.TryGetValue(piece.Name, out bool wasEnabled))
                    {
                        piece.Enabled = wasEnabled;
                    }
                }
            }
        }

        /// <summary>
        /// Habilita o deshabilita una pieza por indice
        /// </summary>
        public void SetPieceEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _detectedPieces.Count)
            {
                _detectedPieces[index].Enabled = enabled;
            }
        }

        /// <summary>
        /// Quita una pieza de la lista de deteccion
        /// </summary>
        public void RemovePiece(int index)
        {
            if (index >= 0 && index < _detectedPieces.Count)
            {
                _detectedPieces.RemoveAt(index);
                // Ajustar indice seleccionado si es necesario
                if (_selectedPieceIndex >= _detectedPieces.Count)
                {
                    _selectedPieceIndex = _detectedPieces.Count - 1;
                }
            }
        }

        /// <summary>
        /// Agrega una pieza manualmente a la lista
        /// </summary>
        public bool AddPieceManually(GameObject pieceObject)
        {
            if (pieceObject == null || _avatarReference == null)
                return false;

            // Verificar que no este ya en la lista
            if (_detectedPieces.Any(c => c.GameObject == pieceObject))
                return false;

            var pieceRef = new ArmatureReference(pieceObject);
            var entry = new PieceEntry(pieceObject)
            {
                ArmatureReference = pieceRef,
                Enabled = true
            };

            // Detectar mapeos
            DetectBoneMappingsForPiece(entry);

            _detectedPieces.Add(entry);
            return true;
        }

        /// <summary>
        /// Habilita todas las piezas
        /// </summary>
        public void EnableAllPieces()
        {
            foreach (var piece in _detectedPieces)
            {
                piece.Enabled = true;
            }
        }

        /// <summary>
        /// Deshabilita todas las piezas
        /// </summary>
        public void DisableAllPieces()
        {
            foreach (var piece in _detectedPieces)
            {
                piece.Enabled = false;
            }
        }

        /// <summary>
        /// Ejecuta el cosido de todas las ropas habilitadas
        /// </summary>
        public StitchingResult ExecuteStitchingAll()
        {
            var enabledPiecesList = EnabledPieces.ToList();

            if (enabledPiecesList.Count == 0)
            {
                return StitchingResult.CreateFailure("No hay ropas habilitadas para coser");
            }

            var combinedResult = new StitchingResult { Success = true };
            int successCount = 0;
            int failCount = 0;

            Debug.Log($"[MRCoserRopa] Iniciando cosido de {enabledPiecesList.Count} ropas...");

            foreach (var piece in enabledPiecesList)
            {
                if (!piece.HasValidMappings)
                {
                    piece.LastResult = StitchingResult.CreateFailure($"'{piece.Name}': Sin mapeos validos");
                    combinedResult.AddWarning($"'{piece.Name}': Sin mapeos validos, saltando");
                    failCount++;
                    continue;
                }

                // Ejecutar cosido para esta pieza
                var result = StitchingController.ExecuteStitching(
                    piece.BoneMappings,
                    _stitchingMode,
                    piece.GameObject);

                piece.LastResult = result;

                if (result.Success)
                {
                    combinedResult.BonesStitched += result.BonesStitched;
                    combinedResult.BonesMerged += result.BonesMerged;
                    combinedResult.NonHumanoidBonesPreserved += result.NonHumanoidBonesPreserved;
                    successCount++;
                    Debug.Log($"[MRCoserRopa] '{piece.Name}': {result.GetSummary()}");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        combinedResult.AddError($"'{piece.Name}': {error}");
                    }
                    failCount++;
                }
            }

            // Resumen final
            if (failCount > 0 && successCount == 0)
            {
                combinedResult.Success = false;
            }

            _lastStitchingResult = combinedResult;

            string modeStr = _stitchingMode == StitchingMode.Merge ? "Fusion" : "Cosido";
            Debug.Log($"[MRCoserRopa] {modeStr} completado: {successCount} exitosos, {failCount} fallidos");

            return combinedResult;
        }

        /// <summary>
        /// Verifica si hay huesos cosidos pendientes de fusionar en alguna ropa
        /// </summary>
        public bool HasStitchedBones
        {
            get
            {
                foreach (var piece in _detectedPieces)
                {
                    if (piece.GameObject != null &&
                        StitchingController.HasStitchedBones(piece.GameObject))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Ejecuta fusion post-cosido en todas las piezas
        /// </summary>
        public StitchingResult ExecuteMergeAfterStitchAll()
        {
            var combinedResult = new StitchingResult { Success = true };

            foreach (var piece in _detectedPieces)
            {
                if (piece.GameObject != null &&
                    StitchingController.HasStitchedBones(piece.GameObject))
                {
                    var result = StitchingController.ExecuteMergeAfterStitch(piece.GameObject);
                    combinedResult.BonesMerged += result.BonesMerged;

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            combinedResult.AddError($"'{piece.Name}': {error}");
                        }
                    }
                }
            }

            _lastStitchingResult = combinedResult;
            return combinedResult;
        }

        #endregion

        #region IValidatable Implementation

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validacion de avatar
            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Error("Arrastra tu avatar aqui"));
                return result;
            }

            if (_avatarReference == null || !_avatarReference.IsValid)
            {
                result.AddChild(ValidationResult.Error("Avatar invalido o sin Animator"));
                return result;
            }

            if (!_avatarReference.IsHumanoid)
            {
                result.AddChild(ValidationResult.Warning(
                    $"Avatar '{_avatarRoot.name}' no es Humanoid. " +
                    "Configura Animation Type: Humanoid en Import Settings para mejores resultados."));
            }

            // Validacion de piezas
            if (_detectedPieces == null || _detectedPieces.Count == 0)
            {
                result.AddChild(ValidationResult.Warning(
                    "No se detectaron ropas. Asegurate de que las ropas esten dentro del avatar y tengan Armature."));
                return result;
            }

            int enabledCount = EnabledPieceCount;
            int withMappings = EnabledPieces.Count(c => c.HasValidMappings);

            if (enabledCount == 0)
            {
                result.AddChild(ValidationResult.Warning("Ninguna ropa esta habilitada"));
            }
            else if (withMappings == 0)
            {
                result.AddChild(ValidationResult.Warning(
                    "Las ropas habilitadas no tienen mapeos validos"));
            }
            else
            {
                result.AddChild(ValidationResult.Success(
                    $"{withMappings} de {enabledCount} ropas listas ({TotalMappedBones} huesos total)"));
            }

            return result;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();
            if (_avatarRoot == null) _avatarRoot = FindAvatarInParents();

            // Auto-refrescar si el avatar cambio
            if (_avatarRoot != null && (_avatarReference == null ||
                _avatarReference.RootObject != _avatarRoot))
            {
                OnAvatarChanged();
            }
        }
#endif

        #endregion
    }
}
