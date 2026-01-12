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
    }
}
