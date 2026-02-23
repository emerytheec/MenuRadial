namespace Bender_Dios.MenuRadial.Localization
{
    /// <summary>
    /// Constantes de keys de localización organizadas por componente.
    /// Sigue el patrón de MRConstants para mantenibilidad.
    ///
    /// Uso recomendado:
    /// using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;
    /// MRLocalization.Get(L.Common.CONFIRM);
    /// </summary>
    public static class MRLocalizationKeys
    {
        /// <summary>
        /// Strings comunes usados en múltiples componentes
        /// </summary>
        public static class Common
        {
            public const string CONFIRM = "common.confirm";
            public const string CANCEL = "common.cancel";
            public const string YES = "common.yes";
            public const string NO = "common.no";
            public const string OK = "common.ok";
            public const string ERROR = "common.error";
            public const string SUCCESS = "common.success";
            public const string WARNING = "common.warning";
            public const string INFO = "common.info";
            public const string CREATE = "common.create";
            public const string DELETE = "common.delete";
            public const string EDIT = "common.edit";
            public const string SAVE = "common.save";
            public const string LOAD = "common.load";
            public const string CLEAR = "common.clear";
            public const string RESET = "common.reset";
            public const string APPLY = "common.apply";
            public const string PREVIEW = "common.preview";
            public const string CANCEL_PREVIEW = "common.cancelPreview";
            public const string GENERATE = "common.generate";
            public const string REFRESH = "common.refresh";
            public const string AUTO_UPDATE = "common.autoUpdate";
            public const string AUTO_UPDATE_TOOLTIP = "common.autoUpdateTooltip";
            public const string DELETE_ITEMS = "common.deleteItems";
            public const string NO_ITEMS_FOUND = "common.noItemsFound";
            public const string INVALID_REFERENCE = "common.invalidReference";
            public const string MISSING_COMPONENT = "common.missingComponent";
            public const string ADVANCED_SETTINGS = "common.advancedSettings";
        }

        /// <summary>
        /// Strings de MRAgruparObjetos
        /// </summary>
        public static class Frame
        {
            public const string HEADER = "frame.header";
            public const string GENERAL_CONFIG = "frame.generalConfig";
            public const string PREVIEW_BUTTON = "frame.previewButton";
            public const string NO_OBJECTS = "frame.noObjects";
            public const string NO_MATERIALS = "frame.noMaterials";
            public const string NO_BLENDSHAPES = "frame.noBlendshapes";
            public const string OBJECTS_SECTION = "frame.objectsSection";
            public const string MATERIALS_SECTION = "frame.materialsSection";
            public const string BLENDSHAPES_SECTION = "frame.blendshapesSection";
            public const string DROP_OBJECTS_HERE = "frame.dropObjectsHere";
            public const string DROP_MATERIALS_HERE = "frame.dropMaterialsHere";
            public const string ADD_BLENDSHAPES = "frame.addBlendshapes";
            public const string CLEAN_INVALID = "frame.cleanInvalid";
            public const string CAPTURE_STATE = "frame.captureState";
            public const string RESTORE_STATE = "frame.restoreState";
            public const string FRAME_NAME = "frame.frameName";
            public const string FRAME_NAME_TOOLTIP = "frame.frameNameTooltip";
        }

        /// <summary>
        /// Strings de MRUnificarObjetos
        /// </summary>
        public static class Radial
        {
            public const string HEADER = "radial.header";
            public const string FRAMES_SECTION = "radial.framesSection";
            public const string ANIMATION_SETTINGS = "radial.animationSettings";
            public const string ANIMATION_NAME = "radial.animationName";
            public const string ANIMATION_PATH = "radial.animationPath";
            public const string GENERATE_ANIMATIONS = "radial.generateAnimations";
            public const string ANIMATIONS_GENERATED = "radial.animationsGenerated";
            public const string DROP_FRAMES_HERE = "radial.dropFramesHere";
            public const string CREATE_AGRUPAR_OBJETOS = "radial.createAgruparObjetos";
            public const string CLEANUP_NULL = "radial.cleanupNull";
            public const string TIP_CREATE_CHILD = "radial.tipCreateChild";
            public const string DURATION_INFO = "radial.durationInfo";
            public const string DIVISION_INFO = "radial.divisionInfo";
            public const string SEGMENT_INFO = "radial.segmentInfo";
            public const string FRAME_ACTIVE = "radial.frameActive";
            public const string STATE_ON = "radial.stateOn";
            public const string STATE_OFF = "radial.stateOff";
            public const string PREVIOUS_FRAME = "radial.previousFrame";
            public const string NEXT_FRAME = "radial.nextFrame";
            public const string DEFAULT_STATE_IS_ON = "radial.defaultStateIsOn";
            public const string DEFAULT_STATE_IS_ON_TOOLTIP = "radial.defaultStateIsOnTooltip";
            public const string CURRENT_FRAME = "radial.currentFrame";
        }

        /// <summary>
        /// Strings de MRIluminacionRadial
        /// </summary>
        public static class Illumination
        {
            public const string HEADER = "illumination.header";
            public const string HEADER_SUBTITLE = "illumination.headerSubtitle";
            public const string ROOT_OBJECT = "illumination.rootObject";
            public const string ROOT_OBJECT_TOOLTIP = "illumination.rootObjectTooltip";
            public const string MATERIALS_DETECTED = "illumination.materialsDetected";
            public const string NO_MATERIALS_FOUND = "illumination.noMaterialsFound";
            public const string PREVIEW_NOT_AVAILABLE = "illumination.previewNotAvailable";
            public const string ILLUMINATION_VALUE = "illumination.illuminationValue";
            public const string DETECT_MATERIALS = "illumination.detectMaterials";
            public const string ASSIGN_ROOT_HINT = "illumination.assignRootHint";
            public const string GENERATE_ANIMATION = "illumination.generateAnimation";
            public const string STATISTICS = "illumination.statistics";
        }

        /// <summary>
        /// Strings de MRMenuControl
        /// </summary>
        public static class Menu
        {
            public const string HEADER = "menu.header";
            public const string PREVIEW_TITLE = "menu.previewTitle";
            public const string RESET_PREVIEWS = "menu.resetPreviews";
            public const string BACK_TO = "menu.backTo";
            public const string ROOT_BUTTON = "menu.rootButton";
            public const string PATH_LABEL = "menu.pathLabel";
            public const string NAMESPACE_SECTION = "menu.namespaceSection";
            public const string OUTPUT_PREFIX = "menu.outputPrefix";
            public const string OUTPUT_PREFIX_TOOLTIP = "menu.outputPrefixTooltip";
            public const string OUTPUT_PATH = "menu.outputPath";
            public const string CREATE_VRCHAT_FILES = "menu.createVRChatFiles";
            public const string CREATE_FILES_CONFIRM = "menu.createFilesConfirm";
            public const string SLOTS_NOT_CONFIGURED = "menu.slotsNotConfigured";
            public const string NAME_CONFLICTS = "menu.nameConflicts";
            public const string READY_TO_CREATE = "menu.readyToCreate";
            public const string IS_SUBMENU = "menu.isSubmenu";
            public const string DROP_COMPONENTS = "menu.dropComponents";
            public const string MAX_SLOTS_REACHED = "menu.maxSlotsReached";
            public const string CREATE_CHILD_HELP = "menu.createChildHelp";
            public const string SLOT_NAME = "menu.slotName";
            public const string SLOT_ICON = "menu.slotIcon";
            public const string INVALID_OBJECTS = "menu.invalidObjects";
            public const string WRITE_DEFAULT_VALUES = "menu.writeDefaultValues";
            public const string SYNC_SLOT_NAMES = "menu.syncSlotNames";
            public const string SYNC_SLOT_NAMES_TOOLTIP = "menu.syncSlotNamesTooltip";
            public const string VIEW_CONFLICTS = "menu.viewConflicts";
            public const string FILES_CREATED = "menu.filesCreated";
            public const string CREATE_UNIFICAR_OBJETOS = "menu.createUnificarObjetos";
            public const string CREATE_ILUMINACION = "menu.createIluminacion";
            public const string CREATE_UNIFICAR_MATERIALES = "menu.createUnificarMateriales";
            public const string CREATE_SUBMENU = "menu.createSubmenu";
            public const string NAME_CONFLICTS_RESOLVE = "menu.nameConflictsResolve";
            public const string AUTO_RESOLVE = "menu.autoResolve";
            public const string CREATE_UNIFICAR_OBJETOS_DESC = "menu.createUnificarObjetosDesc";
            public const string CREATE_ILUMINACION_DESC = "menu.createIluminacionDesc";
            public const string CREATE_UNIFICAR_MATERIALES_DESC = "menu.createUnificarMaterialesDesc";
            public const string CREATE_SUBMENU_DESC = "menu.createSubmenuDesc";
            public const string SUBMENU_TITLE = "menu.submenuTitle";
            public const string CONTINUE_QUESTION = "menu.continueQuestion";
        }

        /// <summary>
        /// Strings de MRCoserRopa
        /// </summary>
        public static class CoserRopa
        {
            public const string HEADER = "coserRopa.header";
            public const string SUBTITLE = "coserRopa.subtitle";
            public const string AVATAR_SECTION = "coserRopa.avatarSection";
            public const string DROP_AVATAR = "coserRopa.dropAvatar";
            public const string NOT_ASSIGNED = "coserRopa.notAssigned";
            public const string CLOTHING_SECTION = "coserRopa.clothingSection";
            public const string DROP_CLOTHING = "coserRopa.dropClothing";
            public const string CONFIG_SECTION = "coserRopa.configSection";
            public const string AUTO_DETECT_BONES = "coserRopa.autoDetectBones";
            public const string AUTO_DETECT_BONES_TOOLTIP = "coserRopa.autoDetectBonesTooltip";
            public const string MODE = "coserRopa.mode";
            public const string MODE_TOOLTIP = "coserRopa.modeTooltip";
            public const string MODE_STITCH = "coserRopa.modeStitch";
            public const string MODE_MERGE = "coserRopa.modeMerge";
            public const string DETECT_BONES = "coserRopa.detectBones";
            public const string CLEAR_MAPPINGS = "coserRopa.clearMappings";
            public const string CLEAR_MAPPINGS_CONFIRM = "coserRopa.clearMappingsConfirm";
            public const string STITCH_BUTTON = "coserRopa.stitchButton";
            public const string MERGE_BUTTON = "coserRopa.mergeButton";
            public const string STITCH_CONFIRM = "coserRopa.stitchConfirm";
            public const string MERGE_CONFIRM = "coserRopa.mergeConfirm";
            public const string BONE_MAPPINGS_SECTION = "coserRopa.boneMappingsSection";
            public const string NO_MAPPINGS = "coserRopa.noMappings";
            public const string STATS_VALID = "coserRopa.statsValid";
            public const string ACTIONS_SECTION = "coserRopa.actionsSection";
            public const string STITCHED_BONES_DETECTED = "coserRopa.stitchedBonesDetected";
            public const string MERGE_STITCHED_BUTTON = "coserRopa.mergeStitchedButton";
            public const string MERGE_STITCHED_CONFIRM = "coserRopa.mergeStitchedConfirm";
            public const string NOT_HUMANOID = "coserRopa.notHumanoid";
            public const string FALLBACK_BY_NAME = "coserRopa.fallbackByName";
            public const string SEARCH_BY_NAME = "coserRopa.searchByName";
            public const string MERGE_ACTION = "coserRopa.mergeAction";
            public const string STITCH_ACTION = "coserRopa.stitchAction";

            // Nuevas claves para UI simplificada
            public const string SUBTITLE_NEW = "coserRopa.subtitleNew";
            public const string DROP_AVATAR_HERE = "coserRopa.dropAvatarHere";
            public const string AVATAR_LABEL = "coserRopa.avatarLabel";
            public const string DETECTED_CLOTHINGS = "coserRopa.detectedClothings";
            public const string NO_CLOTHINGS_DETECTED = "coserRopa.noClothingsDetected";
            public const string SELECT_ALL = "coserRopa.selectAll";
            public const string DESELECT_ALL = "coserRopa.deselectAll";
            public const string SHOW_MAPPINGS = "coserRopa.showMappings";
            public const string SHOW_MAPPINGS_TOOLTIP = "coserRopa.showMappingsTooltip";
            public const string MERGE_ALL_BUTTON = "coserRopa.mergeAllButton";
            public const string STITCH_ALL_BUTTON = "coserRopa.stitchAllButton";
            public const string STITCH_ALL_CONFIRM = "coserRopa.stitchAllConfirm";
        }

        /// <summary>
        /// Strings de MRUnificarMateriales
        /// </summary>
        public static class UnifyMaterial
        {
            public const string HEADER = "unifyMaterial.header";
            public const string ADD_SLOTS_HINT = "unifyMaterial.addSlotsHint";
            public const string NO_LINKED_SLOTS = "unifyMaterial.noLinkedSlots";
            public const string CREATE_AGRUPAR_MATERIALES = "unifyMaterial.createAgruparMateriales";
            public const string LINKED_SLOTS = "unifyMaterial.linkedSlots";
            public const string MATERIAL_GROUPS = "unifyMaterial.materialGroups";
            public const string EMPTY_SLOT = "unifyMaterial.emptySlot";
            public const string DROP_ALTERNATIVE_MATERIALS = "unifyMaterial.dropAlternativeMaterials";
            public const string DROP_OR_CREATE_HINT = "unifyMaterial.dropOrCreateHint";
            public const string TIP_CREATE_CHILD = "unifyMaterial.tipCreateChild";
            public const string ANIMATION_INFO = "unifyMaterial.animationInfo";
            public const string ANIMATION_TYPE = "unifyMaterial.animationType";
            public const string SYSTEM_INFO = "unifyMaterial.systemInfo";
            public const string ADD_ALTERNATIVE_MATERIAL_TO_START = "unifyMaterial.addAlternativeMaterialToStart";
            public const string CONFIGURED_WITH = "unifyMaterial.configuredWith";
            public const string ANIMATION_GENERATED = "unifyMaterial.animationGenerated";
            public const string GENERATION_ERROR = "unifyMaterial.generationError";
        }

        /// <summary>
        /// Strings de MRAgruparMateriales
        /// </summary>
        public static class AlternativeMaterial
        {
            public const string HEADER = "alternativeMaterial.header";
            public const string GROUP_NAME = "alternativeMaterial.groupName";
            public const string ORIGINAL_MATERIAL = "alternativeMaterial.originalMaterial";
            public const string ALTERNATIVE_MATERIALS = "alternativeMaterial.alternativeMaterials";
            public const string ADD_MATERIAL = "alternativeMaterial.addMaterial";
            public const string REMOVE_MATERIAL = "alternativeMaterial.removeMaterial";
            public const string LINK_TO_SLOT = "alternativeMaterial.linkToSlot";
            public const string UNLINK_SLOT = "alternativeMaterial.unlinkSlot";
            public const string NAME = "alternativeMaterial.name";
            public const string LINK_SLOTS_HINT = "alternativeMaterial.linkSlotsHint";
            public const string MESH_CAPTURE = "alternativeMaterial.meshCapture";
            public const string DROP_MESHES_HERE = "alternativeMaterial.dropMeshesHere";
            public const string MATERIAL_SLOTS = "alternativeMaterial.materialSlots";
            public const string UPDATE_PATHS = "alternativeMaterial.updatePaths";
            public const string CLEAR_ALL = "alternativeMaterial.clearAll";
            public const string CLEAR_ALL_SLOTS_CONFIRM = "alternativeMaterial.clearAllSlotsConfirm";
            public const string NO_SLOTS = "alternativeMaterial.noSlots";
            public const string RENDERER = "alternativeMaterial.renderer";
            public const string IDX = "alternativeMaterial.idx";
            public const string MATERIAL = "alternativeMaterial.material";
            public const string GROUP = "alternativeMaterial.group";
            public const string MISSING = "alternativeMaterial.missing";
            public const string SELECT_RENDERER = "alternativeMaterial.selectRenderer";
            public const string LAST_PATH = "alternativeMaterial.lastPath";
            public const string NO_GROUP = "alternativeMaterial.noGroup";
            public const string MATERIAL_GROUPS = "alternativeMaterial.materialGroups";
            public const string DROP_MATERIALS_HERE = "alternativeMaterial.dropMaterialsHere";
            public const string GROUPS_SECTION = "alternativeMaterial.groupsSection";
            public const string CLEAR_EMPTY = "alternativeMaterial.clearEmpty";
            public const string NO_GROUPS = "alternativeMaterial.noGroups";
            public const string GROUP_MATERIALS = "alternativeMaterial.groupMaterials";
            public const string DROP_MATERIALS_TO_GROUP = "alternativeMaterial.dropMaterialsToGroup";
            public const string AUTO_LINKING = "alternativeMaterial.autoLinking";
            public const string DETECT_LINKS = "alternativeMaterial.detectLinks";
            public const string UNLINK_ALL = "alternativeMaterial.unlinkAll";
            public const string DETECT_LINKS_HINT = "alternativeMaterial.detectLinksHint";
            public const string STATUS = "alternativeMaterial.status";
            public const string TOTAL_SLOTS = "alternativeMaterial.totalSlots";
            public const string LINKED_SLOTS = "alternativeMaterial.linkedSlots";
            public const string UNLINKED_SLOTS = "alternativeMaterial.unlinkedSlots";
            public const string TOTAL_GROUPS = "alternativeMaterial.totalGroups";

            // Sugerencias automáticas
            public const string SUGGESTIONS_SECTION = "alternativeMaterial.suggestionsSection";
            public const string DETECT_ALTERNATIVES = "alternativeMaterial.detectAlternatives";
            public const string DETECTING = "alternativeMaterial.detecting";
            public const string NO_SUGGESTIONS = "alternativeMaterial.noSuggestions";
            public const string SUGGESTIONS_FOUND = "alternativeMaterial.suggestionsFound";
            public const string CONFIDENCE = "alternativeMaterial.confidence";
            public const string REASONS = "alternativeMaterial.reasons";
            public const string ACCEPT_SELECTED = "alternativeMaterial.acceptSelected";
            public const string ACCEPT_ALL_HIGH = "alternativeMaterial.acceptAllHigh";
            public const string CLEAR_SUGGESTIONS = "alternativeMaterial.clearSuggestions";
            public const string HIGH_CONFIDENCE = "alternativeMaterial.highConfidence";
            public const string MEDIUM_CONFIDENCE = "alternativeMaterial.mediumConfidence";
            public const string LOW_CONFIDENCE = "alternativeMaterial.lowConfidence";
            public const string SUGGESTION_FOR_SLOT = "alternativeMaterial.suggestionForSlot";
            public const string CURRENT_MATERIAL = "alternativeMaterial.currentMaterial";
            public const string SUGGESTED_ALTERNATIVES = "alternativeMaterial.suggestedAlternatives";
            public const string SELECT_ALL_SUGGESTIONS = "alternativeMaterial.selectAllSuggestions";
            public const string DESELECT_ALL_SUGGESTIONS = "alternativeMaterial.deselectAllSuggestions";
            public const string CREATE_GROUP_FROM_SELECTION = "alternativeMaterial.createGroupFromSelection";
            public const string SUGGESTIONS_HINT = "alternativeMaterial.suggestionsHint";
            public const string SAME_FOLDER = "alternativeMaterial.sameFolder";
            public const string NEARBY_FOLDER = "alternativeMaterial.nearbyFolder";
            public const string SHARED_PREFIX = "alternativeMaterial.sharedPrefix";
            public const string COLOR_VARIANT = "alternativeMaterial.colorVariant";
            public const string VERSION_VARIANT = "alternativeMaterial.versionVariant";
            public const string STYLE_VARIANT = "alternativeMaterial.styleVariant";
            public const string SAME_SHADER = "alternativeMaterial.sameShader";
            public const string SAME_VARIANT_TYPE = "alternativeMaterial.sameVariantType";
        }

        /// <summary>
        /// Strings de validación
        /// </summary>
        public static class Validation
        {
            public const string INVALID_REFERENCES = "validation.invalidReferences";
            public const string AVATAR_NOT_FOUND = "validation.avatarNotFound";
            public const string VALIDATION_SUCCESS = "validation.validationSuccess";
            public const string VALIDATION_FAILED = "validation.validationFailed";
            public const string FIX_ISSUES = "validation.fixIssues";
        }

        /// <summary>
        /// Strings de MRAnalisisColisionEditor
        /// </summary>
        public static class AnalisisColision
        {
            public const string TITLE = "analisisColision.title";
            public const string SUBTITLE = "analisisColision.subtitle";
            public const string DROP_AVATAR_HINT = "analisisColision.dropAvatarHint";
            public const string MA_DETECTED = "analisisColision.maDetected";
            public const string MA_NOT_INSTALLED = "analisisColision.maNotInstalled";
            public const string MA_PREVIEW_EDIT_MODE = "analisisColision.maPreviewEditMode";
            public const string MA_PREVIEW_TOOLTIP = "analisisColision.maPreviewTooltip";
            public const string MA_NOT_INSTALLED_TITLE = "analisisColision.maNotInstalledTitle";
            public const string NO_COMPONENTS_TO_ANALYZE = "analisisColision.noComponentsToAnalyze";
            public const string NOT_SCANNED = "analisisColision.notScanned";
            public const string AUTO_SCAN_HINT = "analisisColision.autoScanHint";
            public const string NO_COLLISIONS = "analisisColision.noCollisions";
            public const string NO_COLLISIONS_HINT = "analisisColision.noCollisionsHint";
            public const string ON_ROOT_COUNT = "analisisColision.onRootCount";
            public const string ON_ROOT_WARNING = "analisisColision.onRootWarning";
            public const string CHECKBOX_HINT = "analisisColision.checkboxHint";
            public const string OTHERS_NO_CLOTHING = "analisisColision.othersNoClothing";
            public const string MESH_ON_ROOT_TITLE = "analisisColision.meshOnRootTitle";
            public const string MESH_ON_ROOT_PROBLEM = "analisisColision.meshOnRootProblem";
            public const string MESH_ON_ROOT_CONSEQUENCE = "analisisColision.meshOnRootConsequence";
            public const string MESH_ON_ROOT_SOLUTION = "analisisColision.meshOnRootSolution";
            public const string IN_CLOTHING = "analisisColision.inClothing";
            public const string SELECT_IN_HIERARCHY = "analisisColision.selectInHierarchy";
            public const string BLENDSHAPE_SYNC_TITLE = "analisisColision.blendshapeSyncTitle";
            public const string STOPPED_COUNT = "analisisColision.stoppedCount";
            public const string ACTIVE_COUNT = "analisisColision.activeCount";
            public const string BLENDSHAPE_SYNC_PROBLEM = "analisisColision.blendshapeSyncProblem";
            public const string BLENDSHAPE_SYNC_SOLUTION = "analisisColision.blendshapeSyncSolution";
            public const string BLENDSHAPE_SYNC_TEMP_SOLUTION = "analisisColision.blendshapeSyncTempSolution";
            public const string REFLECTION_WARNING = "analisisColision.reflectionWarning";
            public const string CONTROL_NOT_AVAILABLE = "analisisColision.controlNotAvailable";
            public const string SHOW_DIAGNOSTIC = "analisisColision.showDiagnostic";
            public const string DIAGNOSTIC = "analisisColision.diagnostic";
            public const string STOP_SYNC = "analisisColision.stopSync";
            public const string STOP_SYNC_TOOLTIP = "analisisColision.stopSyncTooltip";
            public const string RESTORE_SYNC = "analisisColision.restoreSync";
            public const string RESTORE_SYNC_TOOLTIP = "analisisColision.restoreSyncTooltip";
            public const string STOP_SYNC_RESULT = "analisisColision.stopSyncResult";
            public const string RESTORE_SYNC_RESULT = "analisisColision.restoreSyncResult";
            public const string UNDERSTOOD = "analisisColision.understood";
            public const string NDMF_BEHAVIOR = "analisisColision.ndmfBehavior";
            public const string NDMF_DURING_BUILD = "analisisColision.ndmfDuringBuild";
            public const string NDMF_INFO = "analisisColision.ndmfInfo";
            public const string NDMF_BLENDSHAPE_SYNC_INFO = "analisisColision.ndmfBlendshapeSyncInfo";
            public const string NDMF_DESTROYED = "analisisColision.ndmfDestroyed";
            public const string CATEGORY_PROBLEMATIC = "analisisColision.categoryProblematic";
            public const string CATEGORY_DECISION = "analisisColision.categoryDecision";
            public const string CATEGORY_COMPATIBLE = "analisisColision.categoryCompatible";
            public const string CATEGORY_UNKNOWN = "analisisColision.categoryUnknown";
            public const string MENU_TOGGLE_OFF = "analisisColision.menuToggleOff";
            public const string MENU_TOGGLE_ON = "analisisColision.menuToggleOn";
            public const string MENU_DISABLED_TOOLTIP = "analisisColision.menuDisabledTooltip";
            public const string MENU_ACTIVE_TOOLTIP = "analisisColision.menuActiveTooltip";
            public const string SYNC_STOPPED = "analisisColision.syncStopped";
            public const string RESTORE_SYNC_BTN_TOOLTIP = "analisisColision.restoreSyncBtnTooltip";
            public const string STOP_SYNC_BTN_TOOLTIP = "analisisColision.stopSyncBtnTooltip";
            public const string BLENDSHAPE_SYNC_STATUS = "analisisColision.blendshapeSyncStatus";
        }

        /// <summary>
        /// Strings de MRMenuRadialEditor
        /// </summary>
        public static class MenuRadialEditor
        {
            public const string TITLE = "menuRadial.title";
            public const string SUBTITLE = "menuRadial.subtitle";
            public const string AVATAR_LABEL = "menuRadial.avatarLabel";
            public const string AVATAR_ROOT_TOOLTIP = "menuRadial.avatarRootTooltip";
            public const string AUTO_DETECT = "menuRadial.autoDetect";
            public const string AUTO_DETECT_TOOLTIP = "menuRadial.autoDetectTooltip";
            public const string AUTO_GENERATE_MENU = "menuRadial.autoGenerateMenu";
            public const string AUTO_GENERATE_MENU_TOOLTIP = "menuRadial.autoGenerateMenuTooltip";
            public const string DROP_AVATAR_HINT = "menuRadial.dropAvatarHint";
            public const string AVATAR_DETECTED = "menuRadial.avatarDetected";
            public const string NO_DESCRIPTOR = "menuRadial.noDescriptor";
            public const string OUTPUT_PATH = "menuRadial.outputPath";
            public const string OUTPUT_PATH_TOOLTIP = "menuRadial.outputPathTooltip";
            public const string OUTPUT_PATH_HINT = "menuRadial.outputPathHint";
            public const string NAMESPACE = "menuRadial.namespace";
            public const string OUTPUT_PREFIX = "menuRadial.outputPrefix";
            public const string OUTPUT_PREFIX_TOOLTIP = "menuRadial.outputPrefixTooltip";
            public const string OUTPUT_PREVIEW = "menuRadial.outputPreview";
            public const string WRITE_DEFAULTS = "menuRadial.writeDefaults";
            public const string WRITE_DEFAULTS_TOOLTIP = "menuRadial.writeDefaultsTooltip";
            public const string MENU_INTEGRATION = "menuRadial.menuIntegration";
            public const string MENU_INTEGRATION_HINT = "menuRadial.menuIntegrationHint";
            public const string MENU_NAME = "menuRadial.menuName";
            public const string MENU_NAME_TOOLTIP = "menuRadial.menuNameTooltip";
            public const string EFFECTIVE_NAME = "menuRadial.effectiveName";
            public const string MENU_ICON = "menuRadial.menuIcon";
            public const string MENU_ICON_TOOLTIP = "menuRadial.menuIconTooltip";
            public const string LOCATION = "menuRadial.location";
            public const string LOCATION_TOOLTIP = "menuRadial.locationTooltip";
            public const string ROOT_MENU_HINT = "menuRadial.rootMenuHint";
            public const string ASSIGN_AVATAR_FIRST = "menuRadial.assignAvatarFirst";
            public const string NO_DESCRIPTOR_MENU = "menuRadial.noDescriptorMenu";
            public const string NO_EXPRESSIONS_MENU = "menuRadial.noExpressionsMenu";
            public const string TARGET_SUBMENU = "menuRadial.targetSubMenu";
            public const string TARGET_SUBMENU_TOOLTIP = "menuRadial.targetSubMenuTooltip";
            public const string NO_SUBMENUS = "menuRadial.noSubMenus";
            public const string SUBMENU_INFO = "menuRadial.subMenuInfo";
            public const string CUSTOM_PATH = "menuRadial.customPath";
            public const string CUSTOM_PATH_TOOLTIP = "menuRadial.customPathTooltip";
            public const string CUSTOM_PATH_PREVIEW = "menuRadial.customPathPreview";
            public const string ENTER_PATH = "menuRadial.enterPath";
            public const string NDMF_CONTROL = "menuRadial.ndmfControl";
            public const string NDMF_CONTROL_HINT = "menuRadial.ndmfControlHint";
            public const string DISABLE_BONE_STITCHING = "menuRadial.disableBoneStitching";
            public const string DISABLE_BONE_STITCHING_TOOLTIP = "menuRadial.disableBoneStitchingTooltip";
            public const string DISABLE_VRCHAT_MERGE = "menuRadial.disableVRChatMerge";
            public const string DISABLE_VRCHAT_MERGE_TOOLTIP = "menuRadial.disableVRChatMergeTooltip";
            public const string OTHER_PLUGINS = "menuRadial.otherPlugins";
            public const string DISABLE_MA = "menuRadial.disableMA";
            public const string DISABLE_MA_TOOLTIP = "menuRadial.disableMATooltip";
            public const string DISABLE_MA_WARNING = "menuRadial.disableMAWarning";
            public const string DISABLED_PROCESSES = "menuRadial.disabledProcesses";
            public const string DISABLED_BONE_STITCHING = "menuRadial.disabledBoneStitching";
            public const string DISABLED_VRCHAT_MERGE = "menuRadial.disabledVRChatMerge";
            public const string DISABLED_MA = "menuRadial.disabledMA";
            public const string NOT_APPLIED = "menuRadial.notApplied";
            public const string STATUS = "menuRadial.status";
            public const string CLOTHING_DETECTED = "menuRadial.clothingDetected";
            public const string PHYS_BONES = "menuRadial.physBones";
            public const string BOUNDS = "menuRadial.bounds";
            public const string MENU_CONFIGURED = "menuRadial.menuConfigured";
            public const string NO_CLOTHING = "menuRadial.noClothing";
            public const string NO_SLOTS = "menuRadial.noSlots";
            public const string PB_NOT_SCANNED = "menuRadial.pbNotScanned";
            public const string PB_DETECTED = "menuRadial.pbDetected";
            public const string PB_ORGANIZED = "menuRadial.pbOrganized";
            public const string UNKNOWN = "menuRadial.unknown";
            public const string PENDING = "menuRadial.pending";
            public const string BOUNDS_APPLIED = "menuRadial.boundsApplied";
            public const string ACTIONS = "menuRadial.actions";
            public const string PREPARE_ALL = "menuRadial.prepareAll";
            public const string PREPARE_ALL_TOOLTIP = "menuRadial.prepareAllTooltip";
            public const string GENERATE_STRUCTURE = "menuRadial.generateStructure";
            public const string GENERATE_STRUCTURE_TOOLTIP = "menuRadial.generateStructureTooltip";
            public const string REGENERATE = "menuRadial.regenerate";
            public const string REGENERATE_TOOLTIP = "menuRadial.regenerateTooltip";
            public const string REGENERATE_TITLE = "menuRadial.regenerateTitle";
            public const string REGENERATE_CONFIRM = "menuRadial.regenerateConfirm";
            public const string STRUCTURE_GENERATED = "menuRadial.structureGenerated";
            public const string STRUCTURE_GENERATED_INFO = "menuRadial.structureGeneratedInfo";
            public const string STRUCTURE_REGENERATED = "menuRadial.structureRegenerated";
            public const string DETECT_CLOTHING_FIRST = "menuRadial.detectClothingFirst";
            public const string GENERATE_VRCHAT = "menuRadial.generateVRChat";
            public const string GENERATE_VRCHAT_TOOLTIP = "menuRadial.generateVRChatTooltip";
            public const string CONFIGURE_SLOT_FIRST = "menuRadial.configureSlotFirst";
            public const string CHILD_COMPONENTS = "menuRadial.childComponents";
            public const string CONFLICTS_DETECTED = "menuRadial.conflictsDetected";
            public const string RECREATE_COMPONENTS = "menuRadial.recreateComponents";
            public const string SYNC_STRUCTURE = "menuRadial.syncStructure";
            public const string SYNC_STRUCTURE_TOOLTIP = "menuRadial.syncStructureTooltip";
            public const string SYNC_RESULT_TITLE = "menuRadial.syncResultTitle";
            public const string SYNC_RESULT_ADDED = "menuRadial.syncResultAdded";
            public const string SYNC_NO_CHANGES = "menuRadial.syncNoChanges";
            public const string SYNC_ORPHANED_WARNING = "menuRadial.syncOrphanedWarning";
            public const string STRUCTURE_GENERATED_INFO_WITH_WIGS = "menuRadial.structureGeneratedInfoWithWigs";
            public const string SYNC_RESULT_ADDED_WITH_WIGS = "menuRadial.syncResultAddedWithWigs";
            public const string SYNC_RESULT_WIGS_ADDED = "menuRadial.syncResultWigsAdded";
        }

        /// <summary>
        /// Strings de MRPesoTexturasEditor
        /// </summary>
        public static class PesoTexturas
        {
            public const string TITLE = "pesoTexturas.title";
            public const string SUBTITLE = "pesoTexturas.subtitle";
            public const string DROP_AVATAR_HINT = "pesoTexturas.dropAvatarHint";
            public const string SCAN_OPTIONS = "pesoTexturas.scanOptions";
            public const string INCLUDE_AVATAR_BASE = "pesoTexturas.includeAvatarBase";
            public const string INCLUDE_AVATAR_BASE_TOOLTIP = "pesoTexturas.includeAvatarBaseTooltip";
            public const string INCLUDE_CLOTHING = "pesoTexturas.includeClothing";
            public const string INCLUDE_CLOTHING_TOOLTIP = "pesoTexturas.includeClothingTooltip";
            public const string INCLUDE_ALT_MATERIALS = "pesoTexturas.includeAltMaterials";
            public const string INCLUDE_ALT_MATERIALS_TOOLTIP = "pesoTexturas.includeAltMaterialsTooltip";
            public const string SCAN_BUTTON = "pesoTexturas.scanButton";
            public const string SCAN_BUTTON_TOOLTIP = "pesoTexturas.scanButtonTooltip";
            public const string CLEAR_BUTTON = "pesoTexturas.clearButton";
            public const string CLEAR_BUTTON_TOOLTIP = "pesoTexturas.clearButtonTooltip";
            public const string SUMMARY = "pesoTexturas.summary";
            public const string VRAM_BREAKDOWN = "pesoTexturas.vramBreakdown";
            public const string TEXTURES = "pesoTexturas.textures";
            public const string MESHES = "pesoTexturas.meshes";
            public const string BLEND_SHAPES = "pesoTexturas.blendShapes";
            public const string ANIMATIONS = "pesoTexturas.animations";
            public const string MATERIALS = "pesoTexturas.materials";
            public const string AUDIO = "pesoTexturas.audio";
            public const string TOTAL_BUNDLE = "pesoTexturas.totalBundle";
            public const string EXCEEDS_LIMIT = "pesoTexturas.exceedsLimit";
            public const string MIP_STREAMING_DISABLED = "pesoTexturas.mipStreamingDisabled";
            public const string MIP_STREAMING_COUNT = "pesoTexturas.mipStreamingCount";
            public const string MIP_STREAMING_REQUIRED = "pesoTexturas.mipStreamingRequired";
            public const string ENABLE_MIP_STREAMING = "pesoTexturas.enableMipStreaming";
            public const string ENABLE_MIP_STREAMING_TOOLTIP = "pesoTexturas.enableMipStreamingTooltip";
            public const string MIP_STREAMING_ENABLED = "pesoTexturas.mipStreamingEnabled";
            public const string MIP_STREAMING_RESULT = "pesoTexturas.mipStreamingResult";
            public const string OPTIMIZATION = "pesoTexturas.optimization";
            public const string STEP_DOWN_PREVIEW = "pesoTexturas.stepDownPreview";
            public const string SAVINGS = "pesoTexturas.savings";
            public const string MODIFIED_TEXTURES = "pesoTexturas.modifiedTextures";
            public const string OPTIONS = "pesoTexturas.options";
            public const string REDUCE_ALL = "pesoTexturas.reduceAll";
            public const string REDUCE_ALL_TOOLTIP = "pesoTexturas.reduceAllTooltip";
            public const string CONFIRM_REDUCTION = "pesoTexturas.confirmReduction";
            public const string CONFIRM_REDUCTION_MSG = "pesoTexturas.confirmReductionMsg";
            public const string REDUCE = "pesoTexturas.reduce";
            public const string RESTORE_ALL = "pesoTexturas.restoreAll";
            public const string RESTORE_ALL_TOOLTIP = "pesoTexturas.restoreAllTooltip";
            public const string CONFIRM_RESTORE = "pesoTexturas.confirmRestore";
            public const string CONFIRM_RESTORE_MSG = "pesoTexturas.confirmRestoreMsg";
            public const string RESTORE = "pesoTexturas.restore";
            public const string RESTORE_COMPLETED = "pesoTexturas.restoreCompleted";
            public const string RESTORE_RESULT = "pesoTexturas.restoreResult";
            public const string TEXTURE_GROUPS = "pesoTexturas.textureGroups";
            public const string NO_TEXTURES_FOUND = "pesoTexturas.noTexturesFound";
            public const string AVATAR_BASE = "pesoTexturas.avatarBase";
            public const string CLOTHING = "pesoTexturas.clothing";
            public const string ALT_MATERIALS = "pesoTexturas.altMaterials";
            public const string TYPE_LABEL = "pesoTexturas.typeLabel";
            public const string SOURCE = "pesoTexturas.source";
            public const string COL_TEXTURE = "pesoTexturas.colTexture";
            public const string COL_RESOLUTION = "pesoTexturas.colResolution";
            public const string COL_WEIGHT = "pesoTexturas.colWeight";
            public const string COL_ALPHA = "pesoTexturas.colAlpha";
            public const string COL_MIPS = "pesoTexturas.colMips";
            public const string RESTORE_GROUP_TOOLTIP = "pesoTexturas.restoreGroupTooltip";
            public const string STEP_DOWN_TOOLTIP = "pesoTexturas.stepDownTooltip";
            public const string CONFIRM_RESTORE_GROUP = "pesoTexturas.confirmRestoreGroup";
            public const string CONFIRM_REDUCE_GROUP = "pesoTexturas.confirmReduceGroup";
            public const string INCLUDE_WIGS = "pesoTexturas.includeWigs";
            public const string INCLUDE_WIGS_TOOLTIP = "pesoTexturas.includeWigsTooltip";
            public const string WIG = "pesoTexturas.wig";
        }

        /// <summary>
        /// Strings de MROrganizaPBEditor
        /// </summary>
        public static class OrganizaPB
        {
            public const string TITLE = "organizaPB.title";
            public const string SUBTITLE = "organizaPB.subtitle";
            public const string DROP_AVATAR_HINT = "organizaPB.dropAvatarHint";
            public const string SDK_LABEL = "organizaPB.sdkLabel";
            public const string AVAILABLE = "organizaPB.available";
            public const string NOT_AVAILABLE = "organizaPB.notAvailable";
            public const string SCAN_BUTTON = "organizaPB.scanButton";
            public const string CLEAR_BUTTON = "organizaPB.clearButton";
            public const string DETECTED_COUNT = "organizaPB.detectedCount";
            public const string STATE_LABEL = "organizaPB.stateLabel";
            public const string NOT_SCANNED = "organizaPB.notScanned";
            public const string SCANNED = "organizaPB.scanned";
            public const string ORGANIZED = "organizaPB.organized";
            public const string ORGANIZE_BUTTON = "organizaPB.organizeButton";
            public const string ORGANIZE_CONFIRM = "organizaPB.organizeConfirm";
            public const string REVERT_BUTTON = "organizaPB.revertButton";
            public const string ORGANIZED_HINT = "organizaPB.organizedHint";
            public const string CAN_ORGANIZE_HINT = "organizaPB.canOrganizeHint";
            public const string CONTEXT_GROUPS = "organizaPB.contextGroups";
            public const string UNKNOWN = "organizaPB.unknown";
            public const string AVATAR_CONTEXT = "organizaPB.avatarContext";
            public const string ORIGIN = "organizaPB.origin";
            public const string COL_TYPE = "organizaPB.colType";
            public const string COL_NAME = "organizaPB.colName";
            public const string COL_ROOT = "organizaPB.colRoot";
            public const string ALREADY_ORGANIZED = "organizaPB.alreadyOrganized";
            public const string LINK_CONTAINERS_LINKED = "organizaPB.linkContainersLinked";
            public const string LINK_FRAMES_NOT_FOUND = "organizaPB.linkFramesNotFound";
            public const string LINK_NO_MENU_RADIAL = "organizaPB.linkNoMenuRadial";
        }

        /// <summary>
        /// Strings de CircularLinearMenuWindow
        /// </summary>
        public static class CircularMenu
        {
            public const string WINDOW_TITLE = "circularMenu.windowTitle";
            public const string HEADER_TITLE = "circularMenu.headerTitle";
            public const string WINDOW_TOOLTIP = "circularMenu.windowTooltip";
            public const string ERROR_NO_TARGET = "circularMenu.errorNoTarget";
            public const string ERROR_NOT_LINEAR = "circularMenu.errorNotLinear";
            public const string ERROR_MIN_FRAMES = "circularMenu.errorMinFrames";
            public const string CLOSE_WINDOW = "circularMenu.closeWindow";
            public const string HEADER_INFO = "circularMenu.headerInfo";
            public const string REALTIME_PREVIEW = "circularMenu.realtimePreview";
            public const string APPLY_FRAME = "circularMenu.applyFrame";
            public const string ACTIVE_FRAME_INFO = "circularMenu.activeFrameInfo";
            public const string BACK_TO_MENU = "circularMenu.backToMenu";
            public const string OPEN_FULL_EDITOR = "circularMenu.openFullEditor";
            public const string LINEAR_ANIMATION = "circularMenu.linearAnimation";
        }

        /// <summary>
        /// Strings de MRLocalizationWindow
        /// </summary>
        public static class LocalizationWindow
        {
            public const string TITLE = "localizationWindow.title";
            public const string SUBTITLE = "localizationWindow.subtitle";
            public const string CURRENT_LANGUAGE = "localizationWindow.currentLanguage";
            public const string CODE_LABEL = "localizationWindow.codeLabel";
            public const string NAME_LABEL = "localizationWindow.nameLabel";
            public const string STATUS_INITIALIZED = "localizationWindow.statusInitialized";
            public const string STATUS_NOT_INITIALIZED = "localizationWindow.statusNotInitialized";
            public const string AVAILABLE_LANGUAGES = "localizationWindow.availableLanguages";
            public const string NO_LOCALE_FILES = "localizationWindow.noLocaleFiles";
            public const string CURRENT_MARKER = "localizationWindow.currentMarker";
            public const string SELECT_BUTTON = "localizationWindow.selectButton";
        }

        // ==========================================
        // Extensiones a clases existentes
        // ==========================================

        /// <summary>
        /// Keys adicionales para Frame (ObjectListEditor, MaterialListEditor, BlendshapeListEditor, BlendshapeSelectionWindow)
        /// </summary>
        public static class FrameModules
        {
            // ObjectListEditor
            public const string OBJECTS_FOLDOUT = "frame.objectsFoldout";
            public const string DROP_OBJECTS_MAIN = "frame.dropObjectsMain";
            public const string DROP_OBJECTS_SUB = "frame.dropObjectsSub";
            public const string DROP_MORE_OBJECTS_MAIN = "frame.dropMoreObjectsMain";
            public const string DROP_MORE_OBJECTS_SUB = "frame.dropMoreObjectsSub";
            public const string SELECT_ALL_BTN = "frame.selectAllBtn";
            public const string DESELECT_ALL_BTN = "frame.deselectAllBtn";
            public const string RECALCULATE_PATHS = "frame.recalculatePaths";
            public const string REMOVE_ALL_BTN = "frame.removeAllBtn";
            public const string REMOVE_ALL_OBJECTS_CONFIRM = "frame.removeAllObjectsConfirm";
            public const string NO_OBJECTS_HINT = "frame.noObjectsHint";
            public const string COL_ACTIVE = "frame.colActive";
            public const string COL_OBJECT = "frame.colObject";
            public const string SELECT_IN_HIERARCHY = "frame.selectInHierarchy";
            public const string LAST_KNOWN_PATH = "frame.lastKnownPath";

            // MaterialListEditor
            public const string MATERIALS_FOLDOUT = "frame.materialsFoldout";
            public const string DROP_MATERIALS_MAIN = "frame.dropMaterialsMain";
            public const string DROP_MATERIALS_SUB = "frame.dropMaterialsSub";
            public const string DROP_MORE_MATERIALS_MAIN = "frame.dropMoreMaterialsMain";
            public const string DROP_MORE_MATERIALS_SUB = "frame.dropMoreMaterialsSub";
            public const string UPDATE_ORIGINALS = "frame.updateOriginals";
            public const string CLEAN_INVALIDS = "frame.cleanInvalids";
            public const string CLEAN_ALL = "frame.cleanAll";
            public const string CLEAN_ALL_MATERIALS_CONFIRM = "frame.cleanAllMaterialsConfirm";
            public const string NO_MATERIALS_HINT = "frame.noMaterialsHint";
            public const string COL_RENDERER = "frame.colRenderer";
            public const string COL_IDX = "frame.colIdx";
            public const string COL_BASE = "frame.colBase";
            public const string COL_ACTIVE_MAT = "frame.colActiveMat";
            public const string NO_RENDERER = "frame.noRenderer";
            public const string NO_NONE = "frame.noNone";
            public const string SELECT_RENDERER = "frame.selectRenderer";

            // BlendshapeListEditor
            public const string BLENDSHAPES_FOLDOUT = "frame.blendshapesFoldout";
            public const string DROP_BS_MAIN = "frame.dropBsMain";
            public const string DROP_BS_SUB = "frame.dropBsSub";
            public const string DROP_MORE_BS_MAIN = "frame.dropMoreBsMain";
            public const string DROP_MORE_BS_SUB = "frame.dropMoreBsSub";
            public const string CAPTURE_VALUES = "frame.captureValues";
            public const string CLEAN_ALL_BS_CONFIRM = "frame.cleanAllBsConfirm";
            public const string NO_BLENDSHAPES_HINT = "frame.noBlendshapesHint";
            public const string COL_BLENDSHAPE = "frame.colBlendshape";

            // BlendshapeSelectionWindow
            public const string BSW_TITLE = "frame.bswTitle";
            public const string BSW_RENDERER_LABEL = "frame.bswRendererLabel";
            public const string BSW_HELP_TEXT = "frame.bswHelpText";
            public const string BSW_SELECT_ALL = "frame.bswSelectAll";
            public const string BSW_NO_BLENDSHAPES = "frame.bswNoBlendshapes";
            public const string BSW_COL_CHECK = "frame.bswColCheck";
            public const string BSW_COL_NAME = "frame.bswColName";
            public const string BSW_COL_BASE = "frame.bswColBase";
            public const string BSW_COL_ACTIVE = "frame.bswColActive";
            public const string BSW_COL_STATUS = "frame.bswColStatus";
            public const string BSW_ALREADY_EXISTS = "frame.bswAlreadyExists";
            public const string BSW_NEW = "frame.bswNew";
            public const string BSW_CAPTURE_VALUES = "frame.bswCaptureValues";
            public const string BSW_ADD_COUNT = "frame.bswAddCount";
            public const string BSW_ADD = "frame.bswAdd";
            public const string BSW_RESULT_TITLE = "frame.bswResultTitle";
            public const string BSW_RESULT_MSG = "frame.bswResultMsg";
            public const string BSW_ERROR_REFS = "frame.bswErrorRefs";
        }

        /// <summary>
        /// Keys adicionales para Radial (MRUnificarObjetosUIRenderer, ReorderableController)
        /// </summary>
        public static class RadialExtra
        {
            // MRUnificarObjetosUIRenderer
            public const string STATE_WITH_CURRENT = "radial.stateWithCurrent";
            public const string STATE_OFF_INFO = "radial.stateOffInfo";
            public const string STATE_ON_INFO = "radial.stateOnInfo";
            public const string DEFAULT_STATE_TITLE = "radial.defaultStateTitle";
            public const string DEFAULT_STATE_ON_DESC = "radial.defaultStateOnDesc";
            public const string DEFAULT_STATE_OFF_DESC = "radial.defaultStateOffDesc";
            public const string ADD_FRAMES_HINT = "radial.addFramesHint";
            public const string DURATION_TITLE = "radial.durationTitle";
            public const string DURATION_TOTAL = "radial.durationTotal";
            public const string DIVISION_COUNT = "radial.divisionCount";
            public const string SEGMENTS_STANDARD = "radial.segmentsStandard";
            public const string SEGMENTS_EQUAL = "radial.segmentsEqual";
            public const string SCENE_MATERIALS_ERROR = "radial.sceneMaterialsError";
            public const string SYSTEM_INFO_TITLE = "radial.systemInfoTitle";
            public const string SYSTEM_INFO_NO_FRAMES = "radial.systemInfoNoFrames";
            public const string SYSTEM_INFO_ONOFF = "radial.systemInfoOnOff";
            public const string SYSTEM_INFO_AB = "radial.systemInfoAB";
            public const string SYSTEM_INFO_LINEAR = "radial.systemInfoLinear";

            // MRUnificarObjetosReorderableController
            public const string FRAMES_HEADER = "radial.framesHeader";
            public const string FRAME_LABEL = "radial.frameLabel";
            public const string DUPLICATE_FRAME = "radial.duplicateFrame";
            public const string DELETE_FRAME_TITLE = "radial.deleteFrameTitle";
            public const string DELETE_FRAME_CONFIRM = "radial.deleteFrameConfirm";
        }

        /// <summary>
        /// Keys adicionales para CoserRopa (strings hardcodeados restantes en MRCoserRopaEditor)
        /// </summary>
        public static class CoserRopaExtra
        {
            public const string HUMANOID = "coserRopa.humanoid";
            public const string GENERIC_FALLBACK = "coserRopa.genericFallback";
            public const string CLOTHING_LIST_HEADER = "coserRopa.clothingListHeader";
            public const string CLOTHING_LABEL = "coserRopa.clothingLabel";
            public const string BONE_COUNT = "coserRopa.boneCount";
            public const string DETAILS_TITLE = "coserRopa.detailsTitle";
            public const string MA_DETECTED = "coserRopa.maDetectedLabel";
            public const string MA_WILL_PROCESS = "coserRopa.maWillProcess";
            public const string MA_MERGE_ARMATURE = "coserRopa.maMergeArmature";
            public const string MA_RECOMMENDED = "coserRopa.maRecommended";
            public const string MA_NO_MERGE_ARMATURE = "coserRopa.maNoMergeArmature";
            public const string MA_MANUAL_NEEDED = "coserRopa.maManualNeeded";
            public const string BONE_STATS = "coserRopa.boneStats";
            public const string PREFIX_SECTION = "coserRopa.prefixSection";
            public const string SUFFIX_SECTION = "coserRopa.suffixSection";
            public const string PREFIX_KEEP = "coserRopa.prefixKeep";
            public const string PREFIX_REMOVE = "coserRopa.prefixRemove";
            public const string SUFFIX_KEEP = "coserRopa.suffixKeep";
            public const string SUFFIX_REMOVE = "coserRopa.suffixRemove";
            public const string MAPPING_HEADER_AVATAR = "coserRopa.mappingHeaderAvatar";
            public const string MAPPING_HEADER_CLOTHING = "coserRopa.mappingHeaderClothing";
            public const string MAPPING_HEADER_METHOD = "coserRopa.mappingHeaderMethod";
            public const string MAPPING_HEADER_STATUS = "coserRopa.mappingHeaderStatus";
            public const string ADD_MAPPING = "coserRopa.addMapping";
            public const string NDMF_BEHAVIOR = "coserRopa.ndmfBehavior";
            public const string NDMF_DURING_BUILD = "coserRopa.ndmfDuringBuild";
            public const string NDMF_ENABLED_INFO = "coserRopa.ndmfEnabledInfo";
            public const string NDMF_DISABLED_INFO = "coserRopa.ndmfDisabledInfo";
            public const string NDMF_SCOPE = "coserRopa.ndmfScope";
            public const string NDMF_SCENE_SAFE = "coserRopa.ndmfSceneSafe";

            // PieceType labels
            public const string PIECE_TYPE_LABEL = "coserRopa.pieceTypeLabel";
            public const string PIECE_TYPE_ROPA = "coserRopa.pieceTypeRopa";
            public const string PIECE_TYPE_PELO = "coserRopa.pieceTypePelo";
            public const string PIECE_TYPE_PIEZA = "coserRopa.pieceTypePieza";

            // DisableMA
            public const string DISABLE_MA_TOGGLE = "coserRopa.disableMAToggle";
            public const string DISABLE_MA_WIG_WARNING = "coserRopa.disableMAWigWarning";
            public const string DISABLE_MA_MR_WILL_PROCESS = "coserRopa.disableMAMRWillProcess";

            // BoneProxy misplacement
            public const string BONE_PROXY_MISPLACED = "coserRopa.boneProxyMisplaced";
            public const string BONE_PROXY_RELOCATE = "coserRopa.boneProxyRelocate";
            public const string BONE_PROXY_RELOCATE_TO = "coserRopa.boneProxyRelocateTo";

            // Manual zone
            public const string MANUAL_ZONE_LABEL = "coserRopa.manualZoneLabel";
            public const string MANUAL_ZONE_AUTO = "coserRopa.manualZoneAuto";
            public const string MANUAL_ZONE_ACTIVE = "coserRopa.manualZoneActive";

            // NDMF extras
            public const string NDMF_DISABLE_MA_INFO = "coserRopa.ndmfDisableMAInfo";
            public const string NDMF_NO_PIECES_ENABLED = "coserRopa.ndmfNoPiecesEnabled";

            // StitchZone labels (originales)
            public const string ZONE_FULL_BODY = "coserRopa.zoneFullBody";
            public const string ZONE_TORSO = "coserRopa.zoneTorso";
            public const string ZONE_HEAD = "coserRopa.zoneHead";
            public const string ZONE_UPPER_LIMB = "coserRopa.zoneUpperLimb";
            public const string ZONE_LOWER_LIMB = "coserRopa.zoneLowerLimb";
            public const string ZONE_HIP = "coserRopa.zoneHip";
            public const string ZONE_FULL_BODY_SHORT = "coserRopa.zoneFullBodyShort";
            public const string ZONE_TORSO_SHORT = "coserRopa.zoneTorsoShort";
            public const string ZONE_HEAD_SHORT = "coserRopa.zoneHeadShort";
            public const string ZONE_UPPER_LIMB_SHORT = "coserRopa.zoneUpperLimbShort";
            public const string ZONE_LOWER_LIMB_SHORT = "coserRopa.zoneLowerLimbShort";
            public const string ZONE_HIP_SHORT = "coserRopa.zoneHipShort";
            public const string ZONE_LABEL = "coserRopa.zoneLabel";
            public const string ZONE_WIG_LABEL = "coserRopa.zoneWigLabel";

            // Zonas nuevas
            public const string ZONE_RIGHT_HAND = "coserRopa.zoneRightHand";
            public const string ZONE_LEFT_HAND = "coserRopa.zoneLeftHand";
            public const string ZONE_RIGHT_FOOT = "coserRopa.zoneRightFoot";
            public const string ZONE_LEFT_FOOT = "coserRopa.zoneLeftFoot";
            public const string ZONE_CHEST = "coserRopa.zoneChest";
            public const string ZONE_NONE = "coserRopa.zoneNone";
            public const string ZONE_RIGHT_HAND_SHORT = "coserRopa.zoneRightHandShort";
            public const string ZONE_LEFT_HAND_SHORT = "coserRopa.zoneLeftHandShort";
            public const string ZONE_RIGHT_FOOT_SHORT = "coserRopa.zoneRightFootShort";
            public const string ZONE_LEFT_FOOT_SHORT = "coserRopa.zoneLeftFootShort";
            public const string ZONE_CHEST_SHORT = "coserRopa.zoneChestShort";
            public const string ZONE_NONE_SHORT = "coserRopa.zoneNoneShort";
        }
    }
}
