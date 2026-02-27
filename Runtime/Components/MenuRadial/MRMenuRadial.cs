using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.PesoTexturas;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.MenuRadial.Models;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// MR Menu Radial - Componente contenedor principal del sistema.
    /// Organiza todos los componentes MR y propaga el avatar a los hijos.
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Menu Radial")]
    [DisallowMultipleComponent]
    [Icon("Packages/com.bender-dios.menu-radial/Components/Menu/Resources/logo_MR.png")]
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

        [Header("Integración del Menú")]
        [SerializeField]
        [Tooltip("Nombre que aparecerá en el menú VRChat. Si está vacío, usa el prefijo.")]
        private string _menuName = "";

        [SerializeField]
        [Tooltip("Icono que aparecerá junto al nombre en el menú VRChat.")]
        private Texture2D _menuIcon;

        [SerializeField]
        [Tooltip("Modo de integración: Menú Raíz, Submenú existente, o Ruta personalizada.")]
        private MenuIntegrationMode _menuIntegrationMode = MenuIntegrationMode.RootMenu;

        [SerializeField]
        [Tooltip("Índice del submenú existente donde integrar (solo si el modo es ExistingSubMenu).")]
        private int _targetSubMenuIndex = -1;

        [SerializeField]
        [Tooltip("Ruta personalizada para crear menús anidados (ej: 'Outfits/Casual'). Solo si el modo es CustomPath.")]
        private string _customMenuPath = "";

        [Header("NDMF - Control de Procesos")]
        [SerializeField]
        [Tooltip("Desactiva el cosido automático de huesos de ropa durante el build (NDMF). Útil para probar sin merge de armatures.")]
        private bool _disableBoneStitchingNDMF = false;

        [SerializeField]
        [Tooltip("Desactiva el merge automático de archivos VRChat (FX, Parameters, Menu) durante el build (NDMF). Útil para probar sin integración.")]
        private bool _disableVRChatMergeNDMF = false;

        [SerializeField]
        [Tooltip("Desactiva TODOS los componentes de Modular Avatar durante el build (NDMF). Útil cuando hay conflictos entre MR y MA. Solo afecta al build, no modifica la escena.")]
        private bool _disableModularAvatarNDMF = false;

        #endregion

        #region Child Component References (cached)

        private MRAnalisisColision _analisisColision;
        private MRCoserRopa _coserRopa;
        private MROrganizaPB _organizaPB;
        private MRAjustarBounds _ajustarBounds;
        private MRPesoTexturas _pesoTexturas;
        private Component _menuControl; // Referencia genérica para evitar dependencia de assembly
        private bool _isPropagating; // Guardia de re-entrancia para PropagateAvatarToChildren

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
        /// Nombre del menú que aparecerá en VRChat.
        /// Si está vacío, usa el prefijo o "Menu Radial" por defecto.
        /// </summary>
        public string MenuName
        {
            get => _menuName;
            set => _menuName = value;
        }

        /// <summary>
        /// Obtiene el nombre efectivo del menú (MenuName, OutputPrefix, o "Menu Radial").
        /// </summary>
        public string EffectiveMenuName
        {
            get
            {
                if (!string.IsNullOrEmpty(_menuName))
                    return _menuName;
                if (!string.IsNullOrEmpty(_outputPrefix))
                    return _outputPrefix;
                return "Menu Radial";
            }
        }

        /// <summary>
        /// Icono del menú que aparecerá en VRChat.
        /// </summary>
        public Texture2D MenuIcon
        {
            get => _menuIcon;
            set => _menuIcon = value;
        }

        /// <summary>
        /// Modo de integración del menú en el avatar.
        /// </summary>
        public MenuIntegrationMode MenuIntegrationMode
        {
            get => _menuIntegrationMode;
            set => _menuIntegrationMode = value;
        }

        /// <summary>
        /// Índice del submenú existente donde integrar (para modo ExistingSubMenu).
        /// </summary>
        public int TargetSubMenuIndex
        {
            get => _targetSubMenuIndex;
            set => _targetSubMenuIndex = value;
        }

        /// <summary>
        /// Ruta personalizada para crear menús anidados (para modo CustomPath).
        /// </summary>
        public string CustomMenuPath
        {
            get => _customMenuPath;
            set => _customMenuPath = value;
        }

        /// <summary>
        /// Si está activado, desactiva el cosido automático de huesos durante NDMF build.
        /// </summary>
        public bool DisableBoneStitchingNDMF
        {
            get => _disableBoneStitchingNDMF;
            set => _disableBoneStitchingNDMF = value;
        }

        /// <summary>
        /// Si está activado, desactiva el merge de archivos VRChat durante NDMF build.
        /// </summary>
        public bool DisableVRChatMergeNDMF
        {
            get => _disableVRChatMergeNDMF;
            set => _disableVRChatMergeNDMF = value;
        }

        /// <summary>
        /// Si está activado, desactiva TODOS los componentes de Modular Avatar durante NDMF build.
        /// Útil cuando hay conflictos entre MR Menu Radial y Modular Avatar.
        /// Solo afecta al build (clon del avatar), no modifica la escena original.
        /// </summary>
        public bool DisableModularAvatarNDMF
        {
            get => _disableModularAvatarNDMF;
            set => _disableModularAvatarNDMF = value;
        }

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
        /// Referencia cacheada al componente MRAnalisisColision hijo.
        /// </summary>
        public MRAnalisisColision AnalisisColision => _analisisColision != null ? _analisisColision : (_analisisColision = GetComponentInChildren<MRAnalisisColision>());

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

        /// <summary>
        /// Referencia cacheada al componente MRPesoTexturas hijo.
        /// </summary>
        public MRPesoTexturas PesoTexturas => _pesoTexturas != null ? _pesoTexturas : (_pesoTexturas = GetComponentInChildren<MRPesoTexturas>());

        #endregion

        #region Status Properties

        /// <summary>
        /// Cantidad de ropas detectadas.
        /// </summary>
        public int DetectedPieceCount => CoserRopa?.DetectedPieceCount ?? 0;

        /// <summary>
        /// Cantidad de ropas habilitadas para coser.
        /// </summary>
        public int EnabledPieceCount => CoserRopa?.EnabledPieceCount ?? 0;

        /// <summary>
        /// Si hay ropas listas para coser.
        /// </summary>
        public bool HasPiecesToStitch => CoserRopa?.HasPiecesToStitch ?? false;

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
            AnalisisColision != null &&
            CoserRopa != null &&
            OrganizaPB != null &&
            MenuControl != null &&
            AjustarBounds != null &&
            PesoTexturas != null;

        /// <summary>
        /// Cantidad de colisiones detectadas con Modular Avatar.
        /// </summary>
        public int DetectedColisionCount => AnalisisColision?.TotalCount ?? 0;

        /// <summary>
        /// Si hay colisiones problematicas detectadas.
        /// </summary>
        public bool HasProblematicColisions => AnalisisColision?.HasProblematic ?? false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Propaga el avatar actual a todos los componentes hijos que lo necesitan.
        /// También ejecuta auto-detección si está habilitada.
        /// Auto-asigna OutputPrefix con el nombre del avatar si está vacío.
        /// </summary>
        public void PropagateAvatarToChildren()
        {
            // Guardia de re-entrancia: evita que llamadas recursivas/anidadas
            // creen duplicados (ej: OnValidate durante PropagateAvatarToChildren)
            if (_isPropagating) return;
            _isPropagating = true;

            try
            {
                InvalidateCache();

                // Auto-asignar OutputPrefix si está vacío y hay avatar
                if (string.IsNullOrEmpty(_outputPrefix) && _avatarRoot != null)
                {
                    _outputPrefix = _avatarRoot.name;
                }

                // NOTA: AnalisisColision se configura en AutoDetectAll() DESPUÉS de detectar ropas
                // para que tenga la lista de raíces de ropa correcta

                if (CoserRopa != null)
                    CoserRopa.AvatarRoot = _avatarRoot;

                if (OrganizaPB != null)
                    OrganizaPB.AvatarRoot = _avatarRoot;

                if (AjustarBounds != null)
                    AjustarBounds.AvatarRoot = _avatarRoot;

                if (PesoTexturas != null)
                    PesoTexturas.AvatarRoot = _avatarRoot;

                // Auto-detectar si está habilitado y hay avatar
                if (_autoDetectOnAvatarAssign && _avatarRoot != null)
                {
                    AutoDetectAll();
                }
            }
            finally
            {
                _isPropagating = false;
            }
        }

        /// <summary>
        /// Ejecuta auto-detección en todos los componentes.
        /// </summary>
        public void AutoDetectAll()
        {
            if (_avatarRoot == null)
                return;

            // Detectar ropas primero
            if (CoserRopa != null)
            {
                CoserRopa.DetectPiecesInAvatar();
            }

            // Configurar y escanear MRAnalisisColision DESPUÉS de detectar ropas
            // para que tenga la lista de raíces de ropa correcta
            if (AnalisisColision != null)
            {
                AnalisisColision.AvatarRoot = _avatarRoot;

                if (CoserRopa != null)
                {
                    var pieceRoots = new System.Collections.Generic.List<GameObject>();
                    foreach (var piece in CoserRopa.DetectedPieces)
                    {
                        if (piece?.GameObject != null)
                            pieceRoots.Add(piece.GameObject);
                    }
                    AnalisisColision.UpdatePieceRoots(pieceRoots);
                }

                // Solo escanear si no tiene datos previos
                // Esto preserva las decisiones del usuario sobre qué componentes desactivar
                if (!AnalisisColision.IsScanned)
                {
                    AnalisisColision.ScanAvatar();
                }
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

            // Vincular contenedores PhysBone pre-existentes a frames
            if (OrganizaPB != null && OrganizaPB.HasDetectedComponents)
            {
                PhysBoneFrameLinker.LinkContainersToFrames(OrganizaPB, this);
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
        /// Sincroniza la estructura de menú existente con las ropas detectadas.
        /// Agrega solo las ropas nuevas sin modificar lo existente.
        /// </summary>
        /// <returns>Resultado de la sincronización</returns>
        public AutoMenuGenerator.SyncResult SyncMenuStructure()
        {
            var generator = new AutoMenuGenerator(this);
            var result = generator.SyncStructure();
            if (result.Success && (result.FramesAdded > 0 || result.WigFramesAdded > 0))
                InvalidateCache();
            return result;
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
                CoserRopa.DetectPiecesInAvatar();
            }

            // 2. Escanear PhysBones (no organizar automáticamente - es destructivo)
            if (OrganizaPB != null)
            {
                if (OrganizaPB.State == OrganizationState.NotScanned)
                {
                    OrganizaPB.ScanAvatar();
                }

                if (OrganizaPB.CanOrganize)
                {
                    Debug.LogWarning("[MRMenuRadial] PhysBones no organizados. Organízalos desde el inspector de MROrganizaPB.");
                }
            }

            // 3. Aplicar bounds
            if (AjustarBounds != null)
            {
                AjustarBounds.ScanAvatar();
                AjustarBounds.CalculateBounds();
                AjustarBounds.ApplyBounds();

                if (AjustarBounds.IncludeParticles)
                {
                    AjustarBounds.ScanParticles();
                    AjustarBounds.CalculateParticleBounds();
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
            _analisisColision = null;
            _coserRopa = null;
            _organizaPB = null;
            _menuControl = null;
            _ajustarBounds = null;
            _pesoTexturas = null;
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
            if (AnalisisColision == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRAnalisisColision hijo."));

            if (CoserRopa == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRCoserRopa hijo."));

            if (OrganizaPB == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MROrganizaPB hijo."));

            if (MenuControl == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRMenuControl hijo."));

            if (AjustarBounds == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRAjustarBounds hijo."));

            if (PesoTexturas == null)
                result.AddChild(ValidationResult.Warning("No se encontró componente MRPesoTexturas hijo."));

            return result;
        }

        #endregion

        #region Unity Callbacks

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            TryAutoDetectAvatar();
            InvalidateCache();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset se llama al agregar el componente por primera vez.
        /// </summary>
        private void Reset()
        {
            // Solo detectar avatar, NO disparar generación.
            // En Reset los hijos (CoserRopa, etc.) no existen aún.
            // La generación se dispara desde RecreateChildComponents o al asignar avatar en inspector.
            var found = FindAvatarInParents();
            if (found != null)
            {
                _avatarRoot = found;
                if (string.IsNullOrEmpty(_outputPrefix))
                    _outputPrefix = found.name;
            }
        }

        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            TryAutoDetectAvatar();

            // NO llamar PropagateAvatarToChildren() desde OnValidate.
            // OnValidate NO debe crear GameObjects — causa duplicación de Menu Control.
            // La creación y generación se manejan desde el Editor (MRMenuRadialEditor).
            // Los hijos auto-detectan el avatar independientemente via FindAvatarInParents().
        }
#endif

        /// <summary>
        /// Busca el avatar (VRCAvatarDescriptor) en la jerarquía padre.
        /// Solo asigna si _avatarRoot es null (no sobreescribe asignación manual).
        /// </summary>
        private void TryAutoDetectAvatar()
        {
            if (_avatarRoot != null) return;

            var found = FindAvatarInParents();
            if (found != null)
            {
                _avatarRoot = found;
                // Auto-asignar OutputPrefix si está vacío
                if (string.IsNullOrEmpty(_outputPrefix))
                    _outputPrefix = found.name;
            }
        }

        #endregion
    }
}
