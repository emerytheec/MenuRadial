using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB
{
    /// <summary>
    /// Componente que reorganiza los VRCPhysBone y VRCPhysBoneCollider del avatar,
    /// moviéndolos a contenedores organizados (PhysBones y Colliders) como hermanos
    /// del Armature correspondiente.
    ///
    /// Esto permite controlar las dinámicas desde MRAgruparObjetos.
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Organiza PB")]
    [DisallowMultipleComponent]
    public class MROrganizaPB : MonoBehaviour, IValidatable, IEditorOnly
    {
        #region Serialized Fields

        [SerializeField]
        [Tooltip("GameObject raíz del avatar (con VRC_AvatarDescriptor)")]
        private GameObject _avatarRoot;

        [SerializeField]
        private List<PhysBoneEntry> _detectedPhysBones = new List<PhysBoneEntry>();

        [SerializeField]
        private List<ColliderEntry> _detectedColliders = new List<ColliderEntry>();

        [SerializeField, HideInInspector]
        private OrganizationResult _lastResult;

        [SerializeField, HideInInspector]
        private bool _autoScanOnAvatarChange = true;

        [SerializeField, HideInInspector]
        private OrganizationState _state = OrganizationState.NotScanned;

        [SerializeField, HideInInspector]
        private List<GameObject> _createdContainers = new List<GameObject>();

        #endregion

        #region Private Fields

        private PhysBoneScanner _scanner;
        private ContextDetector _contextDetector;
        private PhysBoneRelocator _relocator;

        #endregion

        #region Properties

        /// <summary>
        /// GameObject raíz del avatar.
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
        /// Lista de PhysBones detectados.
        /// </summary>
        public IReadOnlyList<PhysBoneEntry> DetectedPhysBones => _detectedPhysBones;

        /// <summary>
        /// Lista de Colliders detectados.
        /// </summary>
        public IReadOnlyList<ColliderEntry> DetectedColliders => _detectedColliders;

        /// <summary>
        /// Último resultado de ejecución.
        /// </summary>
        public OrganizationResult LastResult => _lastResult;

        /// <summary>
        /// Indica si hay componentes detectados.
        /// </summary>
        public bool HasDetectedComponents => _detectedPhysBones.Count > 0 || _detectedColliders.Count > 0;

        /// <summary>
        /// Indica si el VRChat SDK está disponible.
        /// </summary>
        public bool IsSDKAvailable => Scanner.IsSDKAvailable;

        /// <summary>
        /// Número total de PhysBones habilitados.
        /// </summary>
        public int EnabledPhysBonesCount
        {
            get
            {
                int count = 0;
                foreach (var pb in _detectedPhysBones)
                {
                    if (pb.Enabled && !pb.WasRelocated) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Número total de Colliders habilitados.
        /// </summary>
        public int EnabledCollidersCount
        {
            get
            {
                int count = 0;
                foreach (var col in _detectedColliders)
                {
                    if (col.Enabled && !col.WasRelocated) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Si debe escanear automáticamente al cambiar el avatar.
        /// </summary>
        public bool AutoScanOnAvatarChange
        {
            get => _autoScanOnAvatarChange;
            set => _autoScanOnAvatarChange = value;
        }

        /// <summary>
        /// Estado actual de la organización.
        /// </summary>
        public OrganizationState State => _state;

        /// <summary>
        /// Indica si los PhysBones ya están organizados.
        /// </summary>
        public bool IsOrganized => _state == OrganizationState.Organized;

        /// <summary>
        /// Indica si se puede organizar (hay componentes escaneados y no están organizados).
        /// </summary>
        public bool CanOrganize => HasDetectedComponents && _state == OrganizationState.Scanned;

        /// <summary>
        /// Indica si se puede revertir la organización.
        /// </summary>
        public bool CanRevert => _state == OrganizationState.Organized;

        /// <summary>
        /// Lista de contenedores creados durante la organización.
        /// </summary>
        public IReadOnlyList<GameObject> CreatedContainers => _createdContainers;

        #endregion

        #region Lazy-Loaded Controllers

        private PhysBoneScanner Scanner => _scanner ??= new PhysBoneScanner(ContextDetector);
        private ContextDetector ContextDetector => _contextDetector ??= new ContextDetector();
        private PhysBoneRelocator Relocator => _relocator ??= new PhysBoneRelocator(Scanner);

        #endregion

        #region Public Methods

        /// <summary>
        /// Escanea el avatar y detecta todos los PhysBones y Colliders.
        /// </summary>
        public void ScanAvatar()
        {
            // Si está organizado, no permitir re-escanear
            if (_state == OrganizationState.Organized)
            {
                Debug.LogWarning("[MROrganizaPB] Los PhysBones ya están organizados. Revierte primero para re-escanear.");
                return;
            }

            ClearDetection();

            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MROrganizaPB] No hay avatar asignado");
                return;
            }

            if (!IsSDKAvailable)
            {
                Debug.LogWarning("[MROrganizaPB] VRChat SDK no disponible");
                return;
            }

            Debug.Log($"[MROrganizaPB] Escaneando avatar: {_avatarRoot.name}");

            // Escanear PhysBones
            var physBones = Scanner.ScanPhysBones(_avatarRoot);
            _detectedPhysBones.AddRange(physBones);

            // Escanear Colliders
            var colliders = Scanner.ScanColliders(_avatarRoot);
            _detectedColliders.AddRange(colliders);

            // Cambiar estado a Scanned
            _state = OrganizationState.Scanned;

            Debug.Log($"[MROrganizaPB] Detectados: {_detectedPhysBones.Count} PhysBones, {_detectedColliders.Count} Colliders");
        }

        /// <summary>
        /// Limpia las detecciones actuales.
        /// </summary>
        public void ClearDetection()
        {
            _detectedPhysBones.Clear();
            _detectedColliders.Clear();
            _createdContainers.Clear();
            _lastResult = null;
            _state = OrganizationState.NotScanned;
        }

        /// <summary>
        /// Organiza los PhysBones y Colliders en contenedores.
        /// Esta operación modifica la escena real (no solo durante build).
        /// </summary>
        public OrganizationResult Organize()
        {
            if (_avatarRoot == null)
            {
                _lastResult = OrganizationResult.CreateFailure("No hay avatar asignado");
                return _lastResult;
            }

            if (!IsSDKAvailable)
            {
                _lastResult = OrganizationResult.CreateFailure("VRChat SDK no disponible");
                return _lastResult;
            }

            if (_state == OrganizationState.Organized)
            {
                _lastResult = OrganizationResult.CreateFailure("Los PhysBones ya están organizados. Revierte primero.");
                return _lastResult;
            }

            // Si no hay detecciones, escanear primero
            if (!HasDetectedComponents)
            {
                ScanAvatar();
            }

            if (!HasDetectedComponents)
            {
                _lastResult = OrganizationResult.CreateEmpty();
                _lastResult.AddWarning("No se encontraron PhysBones ni Colliders");
                return _lastResult;
            }

            Debug.Log($"[MROrganizaPB] Organizando: {EnabledPhysBonesCount} PB, {EnabledCollidersCount} Col");

            _lastResult = Relocator.RelocateAll(_detectedPhysBones, _detectedColliders);

            if (_lastResult.Success)
            {
                _state = OrganizationState.Organized;

                // Recopilar contenedores creados
                _createdContainers.Clear();
                foreach (var container in Relocator.GetCreatedContainers())
                {
                    if (container != null)
                    {
                        _createdContainers.Add(container);
                    }
                }
            }

            Debug.Log($"[MROrganizaPB] {_lastResult.GetSummary()}");

            return _lastResult;
        }

        /// <summary>
        /// Revierte la organización, devolviendo los componentes a su ubicación original.
        /// </summary>
        public OrganizationResult Revert()
        {
            if (_state != OrganizationState.Organized)
            {
                return OrganizationResult.CreateFailure("No hay nada que revertir");
            }

            Debug.Log("[MROrganizaPB] Revirtiendo organización...");

            var result = Relocator.RevertAll(_detectedPhysBones, _detectedColliders);

            if (result.Success)
            {
                // Destruir contenedores vacíos
                foreach (var container in _createdContainers)
                {
                    if (container != null && container.transform.childCount == 0)
                    {
                        DestroyImmediate(container);
                    }
                }
                _createdContainers.Clear();

                _state = OrganizationState.Scanned;
            }

            _lastResult = result;
            Debug.Log($"[MROrganizaPB] {result.GetSummary()}");

            return result;
        }

        /// <summary>
        /// Ejecuta la reorganización (alias para compatibilidad con NDMF).
        /// </summary>
        public OrganizationResult Execute()
        {
            return Organize();
        }

        /// <summary>
        /// Habilita o deshabilita todos los PhysBones.
        /// </summary>
        public void SetAllPhysBonesEnabled(bool enabled)
        {
            foreach (var pb in _detectedPhysBones)
            {
                pb.Enabled = enabled;
            }
        }

        /// <summary>
        /// Habilita o deshabilita todos los Colliders.
        /// </summary>
        public void SetAllCollidersEnabled(bool enabled)
        {
            foreach (var col in _detectedColliders)
            {
                col.Enabled = enabled;
            }
        }

        /// <summary>
        /// Obtiene estadísticas agrupadas por contexto.
        /// </summary>
        public Dictionary<string, (int physBones, int colliders)> GetStatsByContext()
        {
            var stats = new Dictionary<string, (int physBones, int colliders)>();

            foreach (var pb in _detectedPhysBones)
            {
                var contextName = pb.Context?.ContextName ?? "Desconocido";
                if (!stats.ContainsKey(contextName))
                {
                    stats[contextName] = (0, 0);
                }
                var current = stats[contextName];
                stats[contextName] = (current.physBones + 1, current.colliders);
            }

            foreach (var col in _detectedColliders)
            {
                var contextName = col.Context?.ContextName ?? "Desconocido";
                if (!stats.ContainsKey(contextName))
                {
                    stats[contextName] = (0, 0);
                }
                var current = stats[contextName];
                stats[contextName] = (current.physBones, current.colliders + 1);
            }

            return stats;
        }

        #endregion

        #region Validation

        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Error("Arrastra tu avatar aquí"));
                return result;
            }

            if (!IsSDKAvailable)
            {
                result.AddChild(ValidationResult.Warning("VRChat SDK no disponible"));
            }

            if (!HasDetectedComponents)
            {
                result.AddChild(ValidationResult.Info("Presiona 'Escanear' para detectar PhysBones"));
            }
            else
            {
                result.AddChild(ValidationResult.Success($"{_detectedPhysBones.Count} PhysBones, {_detectedColliders.Count} Colliders detectados"));
            }

            return result;
        }

        #endregion

        #region Private Methods

        private void OnAvatarChanged()
        {
            ClearDetection();

            if (_autoScanOnAvatarChange && _avatarRoot != null)
            {
                ScanAvatar();
            }
        }

        #endregion

        #region Unity Lifecycle

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Validar cambios en el editor
            if (Application.isPlaying) return;
        }

        private void Reset()
        {
            // Intentar auto-detectar el avatar al agregar el componente
            var avatarDescriptor = GetComponentInParent<VRC_AvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                _avatarRoot = avatarDescriptor.gameObject;
            }
            else
            {
                _avatarRoot = gameObject;
            }
        }
#endif

        #endregion
    }
}
