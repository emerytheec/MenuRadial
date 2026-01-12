using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;

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
        [SerializeField]
        private List<ClothingEntry> _detectedClothings = new List<ClothingEntry>();

        [SerializeField, HideInInspector]
        private int _selectedClothingIndex = -1;

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
        /// Lista de ropas detectadas
        /// </summary>
        public List<ClothingEntry> DetectedClothings => _detectedClothings;

        /// <summary>
        /// Ropas habilitadas para coser
        /// </summary>
        public IEnumerable<ClothingEntry> EnabledClothings =>
            _detectedClothings?.Where(c => c.Enabled && c.IsValid) ?? Enumerable.Empty<ClothingEntry>();

        /// <summary>
        /// Cantidad de ropas detectadas
        /// </summary>
        public int DetectedClothingCount => _detectedClothings?.Count ?? 0;

        /// <summary>
        /// Cantidad de ropas habilitadas
        /// </summary>
        public int EnabledClothingCount => EnabledClothings.Count();

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
        /// Indica si hay ropas listas para coser
        /// </summary>
        public bool HasClothingsToStitch => EnabledClothings.Any(c => c.HasValidMappings);

        /// <summary>
        /// Total de huesos mapeados en todas las ropas habilitadas
        /// </summary>
        public int TotalMappedBones => EnabledClothings.Sum(c => c.MappedBoneCount);

        /// <summary>
        /// Indice de la ropa seleccionada para mostrar detalles
        /// </summary>
        public int SelectedClothingIndex
        {
            get => _selectedClothingIndex;
            set => _selectedClothingIndex = Mathf.Clamp(value, -1, _detectedClothings.Count - 1);
        }

        /// <summary>
        /// Ropa actualmente seleccionada (puede ser null)
        /// </summary>
        public ClothingEntry SelectedClothing =>
            _selectedClothingIndex >= 0 && _selectedClothingIndex < _detectedClothings.Count
                ? _detectedClothings[_selectedClothingIndex]
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
            UpdateVersion("0.002");

            _detectedClothings ??= new List<ClothingEntry>();
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
            _detectedClothings.Clear();
            _lastStitchingResult = null;
            InvalidateBoneCache();

            if (_avatarRoot == null)
            {
                _avatarReference = null;
                return;
            }

            // Crear referencia del avatar
            _avatarReference = new ArmatureReference(_avatarRoot);

            // Detectar ropas automaticamente
            DetectClothingsInAvatar();
        }

        /// <summary>
        /// Detecta todas las ropas dentro del avatar
        /// Una ropa valida es un GameObject que:
        /// 1. Tiene SkinnedMeshRenderer con huesos
        /// 2. Los huesos estan dentro del propio GameObject (no en el armature del avatar)
        /// 3. Los huesos tienen nombres humanoid
        /// </summary>
        public void DetectClothingsInAvatar()
        {
            _detectedClothings.Clear();

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

            // Diccionario para agrupar por GameObject contenedor de ropa
            var clothingCandidates = new Dictionary<GameObject, ClothingCandidate>();

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

                // Encontrar el GameObject contenedor de la ropa
                // Es el hijo directo del avatar que contiene este SMR
                GameObject clothingRoot = FindClothingRoot(smr.transform, _avatarRoot.transform);
                if (clothingRoot == null || clothingRoot == _avatarRoot)
                    continue;

                // Verificar que los huesos esten dentro del clothingRoot
                if (!IsDescendantOf(firstBone, clothingRoot.transform))
                    continue;

                // Agregar o actualizar candidato
                if (!clothingCandidates.ContainsKey(clothingRoot))
                {
                    clothingCandidates[clothingRoot] = new ClothingCandidate
                    {
                        Root = clothingRoot,
                        SkinnedMeshRenderers = new List<SkinnedMeshRenderer>()
                    };
                }
                clothingCandidates[clothingRoot].SkinnedMeshRenderers.Add(smr);
            }

            // Procesar candidatos validos
            foreach (var kvp in clothingCandidates)
            {
                var candidate = kvp.Value;

                // Verificar que tenga huesos humanoid
                if (!HasHumanoidBones(candidate))
                {
                    Debug.Log($"[MRCoserRopa] '{candidate.Root.name}' descartado: no tiene huesos humanoid");
                    continue;
                }

                // Crear referencia de armature para esta ropa
                var clothingRef = new ArmatureReference(candidate.Root);

                var entry = new ClothingEntry(candidate.Root)
                {
                    ArmatureReference = clothingRef,
                    Enabled = true
                };

                // Detectar mapeos de huesos
                DetectBoneMappingsForClothing(entry);

                // Solo agregar si tiene mapeos validos
                if (entry.MappedBoneCount > 0)
                {
                    _detectedClothings.Add(entry);
                    Debug.Log($"[MRCoserRopa] Ropa detectada: '{candidate.Root.name}' " +
                              $"({candidate.SkinnedMeshRenderers.Count} SMRs, {entry.MappedBoneCount} huesos)");
                }
                else
                {
                    Debug.Log($"[MRCoserRopa] '{candidate.Root.name}' descartado: sin mapeos validos con el avatar");
                }
            }

            Debug.Log($"[MRCoserRopa] Total: {_detectedClothings.Count} ropas detectadas");
        }

        /// <summary>
        /// Estructura temporal para agrupar candidatos de ropa
        /// </summary>
        private class ClothingCandidate
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
        /// Encuentra el GameObject raiz de la ropa (hijo directo del avatar)
        /// </summary>
        private GameObject FindClothingRoot(Transform smrTransform, Transform avatarRoot)
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
        private bool HasHumanoidBones(ClothingCandidate candidate)
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

            // Requerir al menos 3 huesos humanoid para considerarlo ropa valida
            return humanoidCount >= 3;
        }

        /// <summary>
        /// Detecta mapeos de huesos para una ropa especifica
        /// </summary>
        public void DetectBoneMappingsForClothing(ClothingEntry clothing)
        {
            if (_avatarReference == null || !_avatarReference.IsValid)
            {
                Debug.LogWarning("[MRCoserRopa] Avatar no valido para detectar huesos");
                return;
            }

            if (clothing?.ArmatureReference == null)
            {
                Debug.LogWarning("[MRCoserRopa] Ropa no valida para detectar huesos");
                return;
            }

            // Usar prefijo/sufijo configurado por el usuario si existe
            clothing.BoneMappings = BoneMapper.DetectBoneMappings(
                _avatarReference,
                clothing.ArmatureReference,
                clothing.BonePrefix,
                clothing.BoneSuffix);
        }

        /// <summary>
        /// Refresca la deteccion de ropas
        /// </summary>
        public void RefreshDetection()
        {
            if (_avatarRoot != null)
            {
                // Guardar estado de habilitacion actual
                var enabledStates = _detectedClothings
                    .ToDictionary(c => c.Name, c => c.Enabled);

                DetectClothingsInAvatar();

                // Restaurar estados de habilitacion
                foreach (var clothing in _detectedClothings)
                {
                    if (enabledStates.TryGetValue(clothing.Name, out bool wasEnabled))
                    {
                        clothing.Enabled = wasEnabled;
                    }
                }
            }
        }

        /// <summary>
        /// Habilita o deshabilita una ropa por indice
        /// </summary>
        public void SetClothingEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _detectedClothings.Count)
            {
                _detectedClothings[index].Enabled = enabled;
            }
        }

        /// <summary>
        /// Quita una ropa de la lista de deteccion
        /// </summary>
        public void RemoveClothing(int index)
        {
            if (index >= 0 && index < _detectedClothings.Count)
            {
                _detectedClothings.RemoveAt(index);
                // Ajustar indice seleccionado si es necesario
                if (_selectedClothingIndex >= _detectedClothings.Count)
                {
                    _selectedClothingIndex = _detectedClothings.Count - 1;
                }
            }
        }

        /// <summary>
        /// Agrega una ropa manualmente a la lista
        /// </summary>
        public bool AddClothingManually(GameObject clothingObject)
        {
            if (clothingObject == null || _avatarReference == null)
                return false;

            // Verificar que no este ya en la lista
            if (_detectedClothings.Any(c => c.GameObject == clothingObject))
                return false;

            var clothingRef = new ArmatureReference(clothingObject);
            var entry = new ClothingEntry(clothingObject)
            {
                ArmatureReference = clothingRef,
                Enabled = true
            };

            // Detectar mapeos
            DetectBoneMappingsForClothing(entry);

            _detectedClothings.Add(entry);
            return true;
        }

        /// <summary>
        /// Habilita todas las ropas
        /// </summary>
        public void EnableAllClothings()
        {
            foreach (var clothing in _detectedClothings)
            {
                clothing.Enabled = true;
            }
        }

        /// <summary>
        /// Deshabilita todas las ropas
        /// </summary>
        public void DisableAllClothings()
        {
            foreach (var clothing in _detectedClothings)
            {
                clothing.Enabled = false;
            }
        }

        /// <summary>
        /// Ejecuta el cosido de todas las ropas habilitadas
        /// </summary>
        public StitchingResult ExecuteStitchingAll()
        {
            var enabledClothings = EnabledClothings.ToList();

            if (enabledClothings.Count == 0)
            {
                return StitchingResult.CreateFailure("No hay ropas habilitadas para coser");
            }

            var combinedResult = new StitchingResult { Success = true };
            int successCount = 0;
            int failCount = 0;

            Debug.Log($"[MRCoserRopa] Iniciando cosido de {enabledClothings.Count} ropas...");

            foreach (var clothing in enabledClothings)
            {
                if (!clothing.HasValidMappings)
                {
                    clothing.LastResult = StitchingResult.CreateFailure($"'{clothing.Name}': Sin mapeos validos");
                    combinedResult.AddWarning($"'{clothing.Name}': Sin mapeos validos, saltando");
                    failCount++;
                    continue;
                }

                // Ejecutar cosido para esta ropa
                var result = StitchingController.ExecuteStitching(
                    clothing.BoneMappings,
                    _stitchingMode,
                    clothing.GameObject);

                clothing.LastResult = result;

                if (result.Success)
                {
                    combinedResult.BonesStitched += result.BonesStitched;
                    combinedResult.BonesMerged += result.BonesMerged;
                    combinedResult.NonHumanoidBonesPreserved += result.NonHumanoidBonesPreserved;
                    successCount++;
                    Debug.Log($"[MRCoserRopa] '{clothing.Name}': {result.GetSummary()}");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        combinedResult.AddError($"'{clothing.Name}': {error}");
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
                foreach (var clothing in _detectedClothings)
                {
                    if (clothing.GameObject != null &&
                        StitchingController.HasStitchedBones(clothing.GameObject))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Ejecuta fusion post-cosido en todas las ropas
        /// </summary>
        public StitchingResult ExecuteMergeAfterStitchAll()
        {
            var combinedResult = new StitchingResult { Success = true };

            foreach (var clothing in _detectedClothings)
            {
                if (clothing.GameObject != null &&
                    StitchingController.HasStitchedBones(clothing.GameObject))
                {
                    var result = StitchingController.ExecuteMergeAfterStitch(clothing.GameObject);
                    combinedResult.BonesMerged += result.BonesMerged;

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            combinedResult.AddError($"'{clothing.Name}': {error}");
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

            // Validacion de ropas
            if (_detectedClothings == null || _detectedClothings.Count == 0)
            {
                result.AddChild(ValidationResult.Warning(
                    "No se detectaron ropas. Asegurate de que las ropas esten dentro del avatar y tengan Armature."));
                return result;
            }

            int enabledCount = EnabledClothingCount;
            int withMappings = EnabledClothings.Count(c => c.HasValidMappings);

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
