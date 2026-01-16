using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.Radial;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// MR Menu Radial - Componente contenedor principal del sistema.
    /// Organiza todos los componentes MR y propaga el avatar a los hijos.
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Menu Radial")]
    [DisallowMultipleComponent]
    public class MRMenuRadial : MRComponentBase
    {
        #region Serialized Fields

        [Header("Avatar")]
        [SerializeField]
        [Tooltip("GameObject raíz del avatar (con VRC_AvatarDescriptor)")]
        private GameObject _avatarRoot;

        [Header("Configuración")]
        [SerializeField]
        [Tooltip("Auto-detectar ropas y escanear al asignar avatar")]
        private bool _autoDetectOnAvatarAssign = true;

        [SerializeField]
        [Tooltip("Generar automáticamente la estructura de menú (MRUnificarObjetos y MRAgruparObjetos) basada en las ropas detectadas")]
        private bool _autoGenerateMenuStructure = true;

        [Header("Rutas de Salida")]
        [SerializeField]
        [Tooltip("Ruta donde se guardarán las animaciones y archivos VRChat generados")]
        private string _outputPath = MRConstants.ANIMATION_OUTPUT_PATH;

        [Header("Configuración VRChat")]
        [SerializeField]
        [Tooltip("Prefijo único para este avatar. Crea subcarpeta y prefija nombres de archivo. Dejar vacío para comportamiento legacy.")]
        private string _outputPrefix = "";

        [SerializeField]
        [Tooltip("writeDefaultValues para las capas del controlador FX")]
        private bool _writeDefaultValues = true;

        #endregion

        #region Child Component References (cached)

        private MRCoserRopa _coserRopa;
        private MROrganizaPB _organizaPB;
        private MRAjustarBounds _ajustarBounds;
        private Component _menuControl; // Referencia genérica para evitar dependencia de assembly

        #endregion

        #region Properties

        /// <summary>
        /// GameObject raíz del avatar.
        /// Al asignar, propaga automáticamente a todos los componentes hijos.
        /// </summary>
        public GameObject AvatarRoot
        {
            get => _avatarRoot;
            set
            {
                if (_avatarRoot != value)
                {
                    _avatarRoot = value;
                    PropagateAvatarToChildren();
                }
            }
        }

        /// <summary>
        /// Auto-detectar al asignar avatar.
        /// </summary>
        public bool AutoDetectOnAvatarAssign
        {
            get => _autoDetectOnAvatarAssign;
            set => _autoDetectOnAvatarAssign = value;
        }

        /// <summary>
        /// Generar automáticamente la estructura de menú al asignar avatar.
        /// </summary>
        public bool AutoGenerateMenuStructure
        {
            get => _autoGenerateMenuStructure;
            set => _autoGenerateMenuStructure = value;
        }

        /// <summary>
        /// Ruta de salida para animaciones y archivos VRChat generados.
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                }
            }
        }

        /// <summary>
        /// Prefijo de salida para archivos VRChat. Crea subcarpeta y prefija nombres de archivo.
        /// </summary>
        public string OutputPrefix
        {
            get => _outputPrefix;
            set => _outputPrefix = value;
        }

        /// <summary>
        /// WriteDefaultValues para las capas del controlador FX.
        /// </summary>
        public bool WriteDefaultValues
        {
            get => _writeDefaultValues;
            set => _writeDefaultValues = value;
        }

        /// <summary>
        /// Verifica si hay un prefijo configurado.
        /// </summary>
        public bool HasPrefix => !string.IsNullOrEmpty(_outputPrefix);

        /// <summary>
        /// Obtiene el directorio de salida incluyendo subcarpeta si hay prefijo.
        /// </summary>
        public string GetVRChatOutputDirectory()
        {
            string effectiveBasePath = string.IsNullOrEmpty(_outputPath)
                ? MRConstants.VRCHAT_OUTPUT_PATH
                : _outputPath.TrimEnd('/') + "/";

            if (string.IsNullOrEmpty(_outputPrefix))
                return effectiveBasePath;
            return $"{effectiveBasePath}{_outputPrefix}/";
        }

        /// <summary>
        /// Referencia cacheada al componente MRCoserRopa hijo.
        /// </summary>
        public MRCoserRopa CoserRopa => _coserRopa != null ? _coserRopa : (_coserRopa = GetComponentInChildren<MRCoserRopa>());

        /// <summary>
        /// Referencia cacheada al componente MROrganizaPB hijo.
        /// </summary>
        public MROrganizaPB OrganizaPB => _organizaPB != null ? _organizaPB : (_organizaPB = GetComponentInChildren<MROrganizaPB>());

        /// <summary>
        /// Referencia cacheada al componente MRMenuControl hijo (como Component genérico).
        /// </summary>
        public Component MenuControl => _menuControl != null ? _menuControl : (_menuControl = FindMenuControlInChildren());

        /// <summary>
        /// Referencia cacheada al componente MRAjustarBounds hijo.
        /// </summary>
        public MRAjustarBounds AjustarBounds => _ajustarBounds != null ? _ajustarBounds : (_ajustarBounds = GetComponentInChildren<MRAjustarBounds>());

        #endregion

        #region Status Properties

        /// <summary>
        /// Cantidad de ropas detectadas.
        /// </summary>
        public int DetectedClothingCount => CoserRopa?.DetectedClothingCount ?? 0;

        /// <summary>
        /// Cantidad de ropas habilitadas para coser.
        /// </summary>
        public int EnabledClothingCount => CoserRopa?.EnabledClothingCount ?? 0;

        /// <summary>
        /// Si hay ropas listas para coser.
        /// </summary>
        public bool HasClothingsToStitch => CoserRopa?.HasClothingsToStitch ?? false;

        /// <summary>
        /// Cantidad de PhysBones detectados.
        /// </summary>
        public int DetectedPhysBonesCount => OrganizaPB?.DetectedPhysBones?.Count ?? 0;

        /// <summary>
        /// Si los PhysBones están organizados.
        /// </summary>
        public bool IsPhysBonesOrganized => OrganizaPB?.IsOrganized ?? false;

        /// <summary>
        /// Estado de organización de PhysBones.
        /// </summary>
        public OrganizationState PhysBonesState => OrganizaPB?.State ?? OrganizationState.NotScanned;

        /// <summary>
        /// Cantidad de meshes detectados para bounds.
        /// </summary>
        public int DetectedMeshesCount => AjustarBounds?.DetectedMeshes?.Count ?? 0;

        /// <summary>
        /// Si los bounds fueron aplicados exitosamente.
        /// </summary>
        public bool IsBoundsApplied => AjustarBounds?.LastCalculationResult?.Success ?? false;

        /// <summary>
        /// Cantidad de slots configurados en el menú.
        /// </summary>
        public int MenuSlotCount => GetMenuSlotCount();

        /// <summary>
        /// Si todos los componentes están listos.
        /// </summary>
        public bool IsFullyConfigured =>
            _avatarRoot != null &&
            CoserRopa != null &&
            OrganizaPB != null &&
            MenuControl != null &&
            AjustarBounds != null;

        #endregion

        #region Public Methods

        /// <summary>
        /// Propaga el avatar actual a todos los componentes hijos que lo necesitan.
        /// También ejecuta auto-detección si está habilitada.
        /// Auto-asigna OutputPrefix con el nombre del avatar si está vacío.
        /// </summary>
        public void PropagateAvatarToChildren()
        {
            InvalidateCache();

            // Auto-asignar OutputPrefix si está vacío y hay avatar
            if (string.IsNullOrEmpty(_outputPrefix) && _avatarRoot != null)
            {
                _outputPrefix = _avatarRoot.name;
            }

            if (CoserRopa != null)
                CoserRopa.AvatarRoot = _avatarRoot;

            if (OrganizaPB != null)
                OrganizaPB.AvatarRoot = _avatarRoot;

            if (AjustarBounds != null)
                AjustarBounds.AvatarRoot = _avatarRoot;

            // Auto-detectar si está habilitado y hay avatar
            if (_autoDetectOnAvatarAssign && _avatarRoot != null)
            {
                AutoDetectAll();
            }
        }

        /// <summary>
        /// Ejecuta auto-detección en todos los componentes.
        /// </summary>
        public void AutoDetectAll()
        {
            if (_avatarRoot == null)
                return;

            // Detectar ropas
            if (CoserRopa != null)
            {
                CoserRopa.DetectClothingsInAvatar();
            }

            // Escanear PhysBones
            if (OrganizaPB != null && OrganizaPB.State == OrganizationState.NotScanned)
            {
                OrganizaPB.ScanAvatar();
            }

            // Escanear meshes para bounds
            if (AjustarBounds != null)
            {
                AjustarBounds.ScanAvatar();
            }

            // Generar estructura de menú automáticamente
            if (_autoGenerateMenuStructure)
            {
                GenerateMenuStructure();
            }
        }

        /// <summary>
        /// Genera automáticamente la estructura de menú basada en las ropas detectadas.
        /// Crea MRUnificarObjetos con MRAgruparObjetos para cada ropa y el avatar.
        /// </summary>
        /// <returns>Resultado de la generación</returns>
        public AutoMenuGenerator.GenerationResult GenerateMenuStructure()
        {
            var generator = new AutoMenuGenerator(this);

            // Si ya existe estructura, no regenerar automáticamente
            if (generator.HasExistingStructure())
            {
                return new AutoMenuGenerator.GenerationResult
                {
                    Success = false,
                    Message = "Ya existe estructura de menú"
                };
            }

            var result = generator.Generate();

            if (result.Success)
            {
                InvalidateCache();
            }

            return result;
        }

        /// <summary>
        /// Fuerza la regeneración de la estructura de menú, eliminando la existente.
        /// </summary>
        /// <returns>Resultado de la generación</returns>
        public AutoMenuGenerator.GenerationResult RegenerateMenuStructure()
        {
            // Eliminar estructura existente (MRUnificarObjetos que son hijos de MRMenuControl)
            var existingUnificar = GetComponentsInChildren<MRUnificarObjetos>();
            foreach (var unificar in existingUnificar)
            {
                if (unificar != null && unificar.transform.parent != null)
                {
                    // Verificar si el padre es MRMenuControl usando el nombre del tipo
                    var parentComponents = unificar.transform.parent.GetComponents<MonoBehaviour>();
                    bool parentIsMenuControl = false;
                    foreach (var comp in parentComponents)
                    {
                        if (comp != null && comp.GetType().Name == "MRMenuControl")
                        {
                            parentIsMenuControl = true;
                            break;
                        }
                    }

                    if (parentIsMenuControl)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.DestroyObjectImmediate(unificar.gameObject);
#else
                        DestroyImmediate(unificar.gameObject);
#endif
                    }
                }
            }

            InvalidateCache();

            // Regenerar
            var generator = new AutoMenuGenerator(this);
            return generator.Generate();
        }

        /// <summary>
        /// Prepara todo el avatar ejecutando la secuencia completa.
        /// 1. Detectar y preparar ropas
        /// 2. Organizar PhysBones
        /// 3. Aplicar bounds
        /// </summary>
        /// <returns>True si todas las operaciones fueron exitosas</returns>
        public bool PrepareAll()
        {
            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MRMenuRadial] No hay avatar asignado.");
                return false;
            }

            bool success = true;

            // 1. Detectar ropas (MRCoserRopa - el merge es automático via NDMF)
            if (CoserRopa != null)
            {
                CoserRopa.DetectClothingsInAvatar();
            }

            // 2. Organizar PhysBones
            if (OrganizaPB != null)
            {
                if (OrganizaPB.State == OrganizationState.NotScanned)
                {
                    OrganizaPB.ScanAvatar();
                }

                if (OrganizaPB.CanOrganize)
                {
                    var result = OrganizaPB.Organize();
                    if (!result.Success)
                    {
                        success = false;
                    }
                }
            }

            // 3. Aplicar bounds
            if (AjustarBounds != null)
            {
                AjustarBounds.ScanAvatar();
                AjustarBounds.ApplyBounds();

                if (AjustarBounds.IncludeParticles)
                {
                    AjustarBounds.ScanParticles();
                    AjustarBounds.ApplyParticleBounds();
                }
            }

            return success;
        }

        /// <summary>
        /// Genera los archivos VRChat (FX Controller, Parameters, Menu).
        /// </summary>
        public void GenerateVRChatFiles()
        {
            if (MenuControl == null)
            {
                Debug.LogWarning("[MRMenuRadial] No se encontró MRMenuControl.");
                return;
            }

            // Llamar CreateVRChatFiles via reflexión
            var method = MenuControl.GetType().GetMethod("CreateVRChatFiles");
            if (method != null)
            {
                method.Invoke(MenuControl, null);
            }
        }

        /// <summary>
        /// Invalida el cache de referencias a componentes hijos.
        /// Útil después de crear/destruir hijos.
        /// </summary>
        public void InvalidateCache()
        {
            _coserRopa = null;
            _organizaPB = null;
            _menuControl = null;
            _ajustarBounds = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Busca el componente MRMenuControl en los hijos usando reflexión para evitar dependencia de assembly.
        /// </summary>
        private Component FindMenuControlInChildren()
        {
            var children = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                if (child != null && child.GetType().Name == "MRMenuControl")
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Obtiene la cantidad de slots del menú via reflexión.
        /// </summary>
        private int GetMenuSlotCount()
        {
            if (MenuControl == null) return 0;

            var property = MenuControl.GetType().GetProperty("SlotCount");
            if (property != null)
            {
                return (int)property.GetValue(MenuControl);
            }

            return 0;
        }

        #endregion

        #region Validation

        public override ValidationResult Validate()
        {
            var result = new ValidationResult("MRMenuRadial Validation");

            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Warning("No se ha asignado un avatar. Arrastra tu avatar al campo 'Avatar Root'."));
            }
            else
            {
                // Verificar que tiene VRC_AvatarDescriptor
                var descriptor = _avatarRoot.GetComponent("VRC_AvatarDescriptor")
                              ?? _avatarRoot.GetComponent("VRCAvatarDescriptor");

                if (descriptor == null)
                {
                    result.AddChild(ValidationResult.Warning("El GameObject asignado no tiene VRC_AvatarDescriptor."));
                }
            }

            // Verificar que los hijos existen
            if (CoserRopa == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRCoserRopa hijo."));

            if (OrganizaPB == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MROrganizaPB hijo."));

            if (MenuControl == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRMenuControl hijo."));

            if (AjustarBounds == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRAjustarBounds hijo."));

            return result;
        }

        #endregion

        #region Unity Callbacks

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            InvalidateCache();
        }

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Propagar avatar cuando cambie en el inspector
            PropagateAvatarToChildren();
        }
#endif

        #endregion
    }
}
