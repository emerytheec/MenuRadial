using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bender_Dios.MenuRadial.Localization
{
    /// <summary>
    /// Sistema de localización centralizado para Menu Radial.
    /// Patrón Singleton estático con cache de traducciones.
    /// </summary>
    public static class MRLocalization
    {
        #region Constants

        private const string FALLBACK_LOCALE = "es";
        private const string LOCALES_PATH = "Locales/";
        private const string EDITOR_PREFS_KEY = "MRLocalization_Locale";

        #endregion

        #region Private State

        private static Dictionary<string, string> _translations = new Dictionary<string, string>();
        private static string _currentLocale = null;
        private static bool _isInitialized = false;
        private static string[] _availableLocales = null;
        private static int _retryCount = 0;
        private const int MAX_RETRIES = 5;

        #endregion

        #region Public Properties

        /// <summary>
        /// Idioma actual del sistema de localización
        /// </summary>
        public static string CurrentLocale => _currentLocale ?? FALLBACK_LOCALE;

        /// <summary>
        /// Indica si el sistema ha sido inicializado
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Evento disparado cuando cambia el idioma
        /// </summary>
        public static event Action OnLocaleChanged;

        #endregion

        #region Initialization

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Initialize();
        }
#endif

        private static void Initialize()
        {
            if (_isInitialized) return;

            string detectedLocale = DetectEditorLocale();
            LoadTranslations(detectedLocale);

            // Solo marcar inicializado si se cargaron traducciones
            _isInitialized = _translations.Count > 0;
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            // Reintento si se inicializó pero no cargó traducciones (race condition)
            else if (_translations.Count == 0 && _retryCount < MAX_RETRIES)
            {
                _retryCount++;
                _isInitialized = false;
                Initialize();
            }
        }

        private static string DetectEditorLocale()
        {
#if UNITY_EDITOR
            // Check for saved preference
            string savedLocale = EditorPrefs.GetString(EDITOR_PREFS_KEY, null);
            if (!string.IsNullOrEmpty(savedLocale) && IsLocaleAvailable(savedLocale))
            {
                return savedLocale;
            }

            // Use system language as fallback
            string mappedLocale = MapSystemLanguageToLocale(Application.systemLanguage);

            if (IsLocaleAvailable(mappedLocale))
            {
                return mappedLocale;
            }
#endif
            return FALLBACK_LOCALE;
        }

        private static string MapSystemLanguageToLocale(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.Spanish => "es",
                SystemLanguage.English => "en",
                SystemLanguage.Chinese => "zh",
                SystemLanguage.ChineseSimplified => "zh",
                SystemLanguage.ChineseTraditional => "zh",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Russian => "ru",
                _ => FALLBACK_LOCALE
            };
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Obtiene una cadena localizada por su key
        /// </summary>
        /// <param name="key">Key de la cadena (ej: "common.confirm")</param>
        /// <returns>Cadena localizada o [key] si no existe</returns>
        public static string Get(string key)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                return "[null_key]";
            }

            if (_translations.TryGetValue(key, out string value))
            {
                return value;
            }

            // Fallback: return key wrapped for debugging
            Debug.LogWarning($"[MRLocalization] Missing key: {key}");
            return $"[{key}]";
        }

        /// <summary>
        /// Obtiene una cadena localizada con parámetros
        /// </summary>
        /// <param name="key">Key de la cadena</param>
        /// <param name="args">Parámetros para string.Format</param>
        /// <returns>Cadena localizada formateada</returns>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);

            if (args == null || args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[MRLocalization] Format error for key: {key}");
                return template;
            }
        }

        /// <summary>
        /// Obtiene un GUIContent con label y tooltip localizados
        /// </summary>
        /// <param name="labelKey">Key del label</param>
        /// <param name="tooltipKey">Key del tooltip (opcional)</param>
        /// <returns>GUIContent con textos localizados</returns>
        public static GUIContent GetContent(string labelKey, string tooltipKey = null)
        {
            string label = Get(labelKey);
            string tooltip = !string.IsNullOrEmpty(tooltipKey) ? Get(tooltipKey) : "";
            return new GUIContent(label, tooltip);
        }

        /// <summary>
        /// Obtiene un GUIContent con label, tooltip e icono
        /// </summary>
        /// <param name="labelKey">Key del label</param>
        /// <param name="tooltipKey">Key del tooltip</param>
        /// <param name="icon">Icono del GUIContent</param>
        /// <returns>GUIContent con textos localizados e icono</returns>
        public static GUIContent GetContent(string labelKey, string tooltipKey, Texture icon)
        {
            string label = Get(labelKey);
            string tooltip = !string.IsNullOrEmpty(tooltipKey) ? Get(tooltipKey) : "";
            return new GUIContent(label, icon, tooltip);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Cambia el idioma actual
        /// </summary>
        /// <param name="locale">Código de idioma (es, en, zh, ko, ja, ru)</param>
        public static void SetLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                locale = FALLBACK_LOCALE;
            }

            if (_currentLocale == locale && _isInitialized)
            {
                return;
            }

            LoadTranslations(locale);

#if UNITY_EDITOR
            EditorPrefs.SetString(EDITOR_PREFS_KEY, locale);
#endif

            OnLocaleChanged?.Invoke();

#if UNITY_EDITOR
            // Repaint global: fuerza redibujar todos los inspectores y ventanas abiertas
            EditorApplication.delayCall += () =>
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    window.Repaint();
                }
            };
#endif
        }

        /// <summary>
        /// Recarga las traducciones del idioma actual
        /// </summary>
        public static void ReloadTranslations()
        {
            _isInitialized = false;
            _translations.Clear();
            Initialize();

            OnLocaleChanged?.Invoke();

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    window.Repaint();
                }
            };
#endif
        }

        /// <summary>
        /// Obtiene la lista de idiomas disponibles
        /// </summary>
        /// <returns>Array de códigos de idioma</returns>
        public static string[] GetAvailableLocales()
        {
            if (_availableLocales != null)
            {
                return _availableLocales;
            }

            var locales = new List<string>();
            var resources = Resources.LoadAll<TextAsset>(LOCALES_PATH.TrimEnd('/'));

            foreach (var resource in resources)
            {
                if (resource != null)
                {
                    locales.Add(resource.name);
                }
            }

            // Ensure fallback is always available
            if (!locales.Contains(FALLBACK_LOCALE))
            {
                locales.Insert(0, FALLBACK_LOCALE);
            }

            _availableLocales = locales.ToArray();
            return _availableLocales;
        }

        /// <summary>
        /// Verifica si un idioma está disponible
        /// </summary>
        /// <param name="locale">Código de idioma</param>
        /// <returns>true si el idioma está disponible</returns>
        public static bool IsLocaleAvailable(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                return false;
            }

            var resource = Resources.Load<TextAsset>($"{LOCALES_PATH}{locale}");
            return resource != null;
        }

        #endregion

        #region Translation Loading

        private static void LoadTranslations(string locale)
        {
            _translations.Clear();
            _currentLocale = locale;

            // Try to load requested locale
            var jsonAsset = Resources.Load<TextAsset>($"{LOCALES_PATH}{locale}");

            // Fallback to Spanish if not found
            if (jsonAsset == null && locale != FALLBACK_LOCALE)
            {
                Debug.Log($"[MRLocalization] Locale '{locale}' not found, falling back to '{FALLBACK_LOCALE}'");
                jsonAsset = Resources.Load<TextAsset>($"{LOCALES_PATH}{FALLBACK_LOCALE}");
                _currentLocale = FALLBACK_LOCALE;
            }

            if (jsonAsset == null)
            {
                Debug.LogWarning("[MRLocalization] Could not load any locale file. Using empty translations.");
                return;
            }

            ParseJsonToFlatDictionary(jsonAsset.text);
            Debug.Log($"[MRLocalization] Loaded {_translations.Count} translations for locale '{_currentLocale}'");
        }

        private static void ParseJsonToFlatDictionary(string json)
        {
            try
            {
                // Simple JSON parser for nested structure
                // Converts {"common": {"confirm": "OK"}} to "common.confirm" = "OK"
                var wrapper = JsonUtility.FromJson<LocaleWrapper>(json);

                if (wrapper != null)
                {
                    FlattenSection("common", wrapper.common);
                    FlattenSection("frame", wrapper.frame);
                    FlattenSection("radial", wrapper.radial);
                    FlattenSection("illumination", wrapper.illumination);
                    FlattenSection("menu", wrapper.menu);
                    FlattenSection("coserRopa", wrapper.coserRopa);
                    FlattenSection("unifyMaterial", wrapper.unifyMaterial);
                    FlattenSection("alternativeMaterial", wrapper.alternativeMaterial);
                    FlattenSection("validation", wrapper.validation);
                    FlattenSection("analisisColision", wrapper.analisisColision);
                    FlattenSection("menuRadial", wrapper.menuRadial);
                    FlattenSection("pesoTexturas", wrapper.pesoTexturas);
                    FlattenSection("organizaPB", wrapper.organizaPB);
                    FlattenSection("circularMenu", wrapper.circularMenu);
                    FlattenSection("localizationWindow", wrapper.localizationWindow);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MRLocalization] Error parsing JSON: {ex.Message}");
            }
        }

        private static void FlattenSection(string prefix, LocaleSection section)
        {
            if (section == null) return;

            var fields = typeof(LocaleSection).GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(section) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    string key = $"{prefix}.{field.Name}";
                    _translations[key] = value;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene el nombre legible de un idioma
        /// </summary>
        /// <param name="locale">Código de idioma</param>
        /// <returns>Nombre del idioma</returns>
        public static string GetLocaleName(string locale)
        {
            return locale switch
            {
                "es" => "Espanol",
                "en" => "English",
                "zh" => "Chinese",
                "ko" => "Korean",
                "ja" => "Japanese",
                "ru" => "Russian",
                _ => locale
            };
        }

        #endregion

        #region JSON Wrapper Classes

        [Serializable]
        private class LocaleWrapper
        {
            public LocaleMeta _meta;
            public LocaleSection common;
            public LocaleSection frame;
            public LocaleSection radial;
            public LocaleSection illumination;
            public LocaleSection menu;
            public LocaleSection coserRopa;
            public LocaleSection unifyMaterial;
            public LocaleSection alternativeMaterial;
            public LocaleSection validation;
            public LocaleSection analisisColision;
            public LocaleSection menuRadial;
            public LocaleSection pesoTexturas;
            public LocaleSection organizaPB;
            public LocaleSection circularMenu;
            public LocaleSection localizationWindow;
        }

        [Serializable]
        private class LocaleMeta
        {
            public string locale;
            public string language;
            public string version;
        }

        [Serializable]
        private class LocaleSection
        {
            // Common
            public string confirm;
            public string cancel;
            public string yes;
            public string no;
            public string ok;
            public string error;
            public string success;
            public string warning;
            public string info;
            public string create;
            public string delete;
            public string edit;
            public string save;
            public string load;
            public string clear;
            public string reset;
            public string apply;
            public string preview;
            public string cancelPreview;
            public string generate;
            public string refresh;
            public string autoUpdate;
            public string autoUpdateTooltip;
            public string deleteItems;
            public string noItemsFound;
            public string invalidReference;
            public string missingComponent;
            public string advancedSettings;

            // Frame
            public string header;
            public string generalConfig;
            public string previewButton;
            public string noObjects;
            public string noMaterials;
            public string noBlendshapes;
            public string objectsSection;
            public string materialsSection;
            public string blendshapesSection;
            public string dropObjectsHere;
            public string dropMaterialsHere;
            public string addBlendshapes;
            public string cleanInvalid;
            public string captureState;
            public string restoreState;
            public string frameName;
            public string frameNameTooltip;

            // Radial
            public string framesSection;
            public string animationSettings;
            public string animationName;
            public string animationPath;
            public string generateAnimations;
            public string animationsGenerated;
            public string dropFramesHere;
            public string createAgruparObjetos;
            public string cleanupNull;
            public string tipCreateChild;
            public string durationInfo;
            public string divisionInfo;
            public string segmentInfo;
            public string frameActive;
            public string stateOn;
            public string stateOff;
            public string previousFrame;
            public string nextFrame;
            public string defaultStateIsOn;
            public string defaultStateIsOnTooltip;
            public string currentFrame;

            // Illumination
            public string rootObject;
            public string rootObjectTooltip;
            public string materialsDetected;
            public string noMaterialsFound;
            public string previewNotAvailable;
            public string illuminationValue;
            public string detectMaterials;
            public string headerSubtitle;
            public string assignRootHint;
            public string generateAnimation;
            public string statistics;

            // Menu
            public string previewTitle;
            public string resetPreviews;
            public string backTo;
            public string rootButton;
            public string pathLabel;
            public string namespaceSection;
            public string outputPrefix;
            public string outputPrefixTooltip;
            public string outputPath;
            public string createVRChatFiles;
            public string createFilesConfirm;
            public string slotsNotConfigured;
            public string nameConflicts;
            public string readyToCreate;
            public string isSubmenu;
            public string dropComponents;
            public string maxSlotsReached;
            public string createChildHelp;
            public string slotName;
            public string slotIcon;
            public string invalidObjects;
            public string writeDefaultValues;
            public string syncSlotNames;
            public string syncSlotNamesTooltip;
            public string viewConflicts;
            public string filesCreated;
            public string createUnificarObjetos;
            public string createIluminacion;
            public string createUnificarMateriales;
            public string createSubmenu;
            public string nameConflictsResolve;
            public string autoResolve;
            public string createUnificarObjetosDesc;
            public string createIluminacionDesc;
            public string createUnificarMaterialesDesc;
            public string createSubmenuDesc;
            public string submenuTitle;
            public string continueQuestion;

            // CoserRopa
            public string subtitle;
            public string avatarSection;
            public string dropAvatar;
            public string notAssigned;
            public string clothingSection;
            public string dropClothing;
            public string configSection;
            public string autoDetectBones;
            public string autoDetectBonesTooltip;
            public string mode;
            public string modeTooltip;
            public string modeStitch;
            public string modeMerge;
            public string detectBones;
            public string clearMappings;
            public string clearMappingsConfirm;
            public string stitchButton;
            public string mergeButton;
            public string stitchConfirm;
            public string mergeConfirm;
            public string boneMappingsSection;
            public string noMappings;
            public string statsValid;
            public string actionsSection;
            public string stitchedBonesDetected;
            public string mergeStitchedButton;
            public string mergeStitchedConfirm;
            public string notHumanoid;
            public string fallbackByName;
            public string searchByName;
            public string mergeAction;
            public string stitchAction;

            // CoserRopa - UI simplificada
            public string subtitleNew;
            public string dropAvatarHere;
            public string avatarLabel;
            public string detectedClothings;
            public string noClothingsDetected;
            public string selectAll;
            public string deselectAll;
            public string showMappings;
            public string showMappingsTooltip;
            public string mergeAllButton;
            public string stitchAllButton;
            public string stitchAllConfirm;

            // UnifyMaterial
            public string addSlotsHint;
            public string noLinkedSlots;
            public string createAgruparMateriales;
            public string linkedSlots;
            public string materialGroups;
            public string emptySlot;
            public string dropAlternativeMaterials;
            public string dropOrCreateHint;
            public string animationInfo;
            public string animationType;
            public string systemInfo;
            public string addAlternativeMaterialToStart;
            public string configuredWith;
            public string animationGenerated;
            public string generationError;

            // AlternativeMaterial
            public string groupName;
            public string originalMaterial;
            public string alternativeMaterials;
            public string addMaterial;
            public string removeMaterial;
            public string linkToSlot;
            public string unlinkSlot;
            public string name;
            public string linkSlotsHint;
            public string meshCapture;
            public string dropMeshesHere;
            public string materialSlots;
            public string updatePaths;
            public string clearAll;
            public string clearAllSlotsConfirm;
            public string noSlots;
            public string renderer;
            public string idx;
            public string material;
            public string group;
            public string missing;
            public string selectRenderer;
            public string lastPath;
            public string noGroup;
            public string groupsSection;
            public string clearEmpty;
            public string noGroups;
            public string groupMaterials;
            public string dropMaterialsToGroup;
            public string autoLinking;
            public string detectLinks;
            public string unlinkAll;
            public string detectLinksHint;
            public string status;
            public string totalSlots;
            public string unlinkedSlots;
            public string totalGroups;

            // AlternativeMaterial - Sugerencias automáticas
            public string suggestionsSection;
            public string detectAlternatives;
            public string detecting;
            public string noSuggestions;
            public string suggestionsFound;
            public string confidence;
            public string reasons;
            public string acceptSelected;
            public string acceptAllHigh;
            public string clearSuggestions;
            public string highConfidence;
            public string mediumConfidence;
            public string lowConfidence;
            public string suggestionForSlot;
            public string currentMaterial;
            public string suggestedAlternatives;
            public string selectAllSuggestions;
            public string deselectAllSuggestions;
            public string createGroupFromSelection;
            public string suggestionsHint;
            public string sameFolder;
            public string nearbyFolder;
            public string sharedPrefix;
            public string colorVariant;
            public string versionVariant;
            public string styleVariant;
            public string sameShader;
            public string sameVariantType;

            // AlternativeMaterial - Sección informativa de carpetas
            public string folderInfo;
            public string folderInfoParent;
            public string folderInfoNoSlots;

            // Validation
            public string invalidReferences;
            public string avatarNotFound;
            public string validationSuccess;
            public string validationFailed;
            public string fixIssues;

            // AnalisisColision
            public string title;
            public string dropAvatarHint;
            public string maDetected;
            public string maNotInstalled;
            public string maPreviewEditMode;
            public string maPreviewTooltip;
            public string maNotInstalledTitle;
            public string noComponentsToAnalyze;
            public string notScanned;
            public string autoScanHint;
            public string noCollisions;
            public string noCollisionsHint;
            public string onRootCount;
            public string onRootWarning;
            public string checkboxHint;
            public string othersNoClothing;
            public string meshOnRootTitle;
            public string meshOnRootProblem;
            public string meshOnRootConsequence;
            public string meshOnRootSolution;
            public string inClothing;
            public string selectInHierarchy;
            public string blendshapeSyncTitle;
            public string stoppedCount;
            public string activeCount;
            public string blendshapeSyncProblem;
            public string blendshapeSyncSolution;
            public string blendshapeSyncTempSolution;
            public string reflectionWarning;
            public string controlNotAvailable;
            public string showDiagnostic;
            public string diagnostic;
            public string stopSync;
            public string stopSyncTooltip;
            public string restoreSync;
            public string restoreSyncTooltip;
            public string stopSyncResult;
            public string restoreSyncResult;
            public string understood;
            public string ndmfBehavior;
            public string ndmfDuringBuild;
            public string ndmfInfo;
            public string ndmfBlendshapeSyncInfo;
            public string ndmfDestroyed;
            public string categoryProblematic;
            public string categoryDecision;
            public string categoryCompatible;
            public string categoryUnknown;
            public string menuToggleOff;
            public string menuToggleOn;
            public string menuDisabledTooltip;
            public string menuActiveTooltip;
            public string syncStopped;
            public string restoreSyncBtnTooltip;
            public string stopSyncBtnTooltip;
            public string blendshapeSyncStatus;

            // MenuRadialEditor
            public string avatarRootTooltip;
            public string autoDetect;
            public string autoDetectTooltip;
            public string autoGenerateMenu;
            public string autoGenerateMenuTooltip;
            public string avatarDetected;
            public string noDescriptor;
            public string outputPathTooltip;
            public string outputPathHint;
            public string @namespace;
            public string outputPreview;
            public string writeDefaults;
            public string writeDefaultsTooltip;
            public string menuIntegration;
            public string menuIntegrationHint;
            public string menuName;
            public string menuNameTooltip;
            public string effectiveName;
            public string menuIcon;
            public string menuIconTooltip;
            public string location;
            public string locationTooltip;
            public string rootMenuHint;
            public string assignAvatarFirst;
            public string noDescriptorMenu;
            public string noExpressionsMenu;
            public string targetSubMenu;
            public string targetSubMenuTooltip;
            public string noSubMenus;
            public string subMenuInfo;
            public string customPath;
            public string customPathTooltip;
            public string customPathPreview;
            public string enterPath;
            public string ndmfControl;
            public string ndmfControlHint;
            public string disableBoneStitching;
            public string disableBoneStitchingTooltip;
            public string disableVRChatMerge;
            public string disableVRChatMergeTooltip;
            public string otherPlugins;
            public string disableMA;
            public string disableMATooltip;
            public string disableMAWarning;
            public string disabledProcesses;
            public string disabledBoneStitching;
            public string disabledVRChatMerge;
            public string disabledMA;
            public string notApplied;
            public string clothingDetected;
            public string physBones;
            public string bounds;
            public string menuConfigured;
            public string noClothing;
            public string pbNotScanned;
            public string pbDetected;
            public string pbOrganized;
            public string unknown;
            public string pending;
            public string boundsApplied;
            public string actions;
            public string prepareAll;
            public string prepareAllTooltip;
            public string generateStructure;
            public string generateStructureTooltip;
            public string regenerate;
            public string regenerateTooltip;
            public string regenerateTitle;
            public string regenerateConfirm;
            public string structureGenerated;
            public string structureGeneratedInfo;
            public string structureRegenerated;
            public string detectClothingFirst;
            public string generateVRChat;
            public string generateVRChatTooltip;
            public string configureSlotFirst;
            public string childComponents;
            public string conflictsDetected;
            public string recreateComponents;
            public string syncStructure;
            public string syncStructureTooltip;
            public string syncResultTitle;
            public string syncResultAdded;
            public string syncNoChanges;
            public string syncOrphanedWarning;
            public string structureGeneratedInfoWithWigs;
            public string syncResultAddedWithWigs;
            public string syncResultWigsAdded;

            // PesoTexturas
            public string scanOptions;
            public string includeAvatarBase;
            public string includeAvatarBaseTooltip;
            public string includeClothing;
            public string includeClothingTooltip;
            public string includeAltMaterials;
            public string includeAltMaterialsTooltip;
            public string scanButton;
            public string scanButtonTooltip;
            public string clearButton;
            public string clearButtonTooltip;
            public string summary;
            public string vramBreakdown;
            public string textures;
            public string meshes;
            public string blendShapes;
            public string animations;
            public string materials;
            public string audio;
            public string totalBundle;
            public string exceedsLimit;
            public string mipStreamingDisabled;
            public string mipStreamingCount;
            public string mipStreamingRequired;
            public string enableMipStreaming;
            public string enableMipStreamingTooltip;
            public string mipStreamingEnabled;
            public string mipStreamingResult;
            public string optimization;
            public string stepDownPreview;
            public string savings;
            public string modifiedTextures;
            public string options;
            public string reduceAll;
            public string reduceAllTooltip;
            public string confirmReduction;
            public string confirmReductionMsg;
            public string reduce;
            public string restoreAll;
            public string restoreAllTooltip;
            public string confirmRestore;
            public string confirmRestoreMsg;
            public string restore;
            public string restoreCompleted;
            public string restoreResult;
            public string textureGroups;
            public string noTexturesFound;
            public string avatarBase;
            public string clothing;
            public string altMaterials;
            public string typeLabel;
            public string source;
            public string colTexture;
            public string colResolution;
            public string colWeight;
            public string colAlpha;
            public string colMips;
            public string restoreGroupTooltip;
            public string stepDownTooltip;
            public string confirmRestoreGroup;
            public string confirmReduceGroup;
            public string includeWigs;
            public string includeWigsTooltip;
            public string wig;

            // OrganizaPB
            public string sdkLabel;
            public string available;
            public string notAvailable;
            public string detectedCount;
            public string stateLabel;
            public string scanned;
            public string organized;
            public string organizeButton;
            public string organizeConfirm;
            public string revertButton;
            public string organizedHint;
            public string canOrganizeHint;
            public string contextGroups;
            public string avatarContext;
            public string origin;
            public string colType;
            public string colName;
            public string colRoot;
            public string alreadyOrganized;
            public string linkContainersLinked;
            public string linkFramesNotFound;
            public string linkNoMenuRadial;

            // CircularMenu
            public string windowTitle;
            public string headerTitle;
            public string windowTooltip;
            public string errorNoTarget;
            public string errorNotLinear;
            public string errorMinFrames;
            public string closeWindow;
            public string headerInfo;
            public string realtimePreview;
            public string applyFrame;
            public string activeFrameInfo;
            public string backToMenu;
            public string openFullEditor;
            public string linearAnimation;

            // LocalizationWindow
            public string currentLanguage;
            public string codeLabel;
            public string nameLabel;
            public string statusInitialized;
            public string statusNotInitialized;
            public string availableLanguages;
            public string noLocaleFiles;
            public string currentMarker;
            public string selectButton;

            // FrameModules (ObjectListEditor, MaterialListEditor, BlendshapeListEditor, BlendshapeSelectionWindow)
            public string objectsFoldout;
            public string dropObjectsMain;
            public string dropObjectsSub;
            public string dropMoreObjectsMain;
            public string dropMoreObjectsSub;
            public string selectAllBtn;
            public string deselectAllBtn;
            public string recalculatePaths;
            public string removeAllBtn;
            public string removeAllObjectsConfirm;
            public string noObjectsHint;
            public string colActive;
            public string colObject;
            public string lastKnownPath;
            public string materialsFoldout;
            public string dropMaterialsMain;
            public string dropMaterialsSub;
            public string dropMoreMaterialsMain;
            public string dropMoreMaterialsSub;
            public string updateOriginals;
            public string cleanInvalids;
            public string cleanAll;
            public string cleanAllMaterialsConfirm;
            public string noMaterialsHint;
            public string colRenderer;
            public string colIdx;
            public string colBase;
            public string colActiveMat;
            public string noRenderer;
            public string noNone;
            public string blendshapesFoldout;
            public string dropBsMain;
            public string dropBsSub;
            public string dropMoreBsMain;
            public string dropMoreBsSub;
            public string captureValues;
            public string cleanAllBsConfirm;
            public string noBlendshapesHint;
            public string colBlendshape;
            public string bswTitle;
            public string bswRendererLabel;
            public string bswHelpText;
            public string bswSelectAll;
            public string bswNoBlendshapes;
            public string bswColCheck;
            public string bswColName;
            public string bswColBase;
            public string bswColActive;
            public string bswColStatus;
            public string bswAlreadyExists;
            public string bswNew;
            public string bswCaptureValues;
            public string bswAddCount;
            public string bswAdd;
            public string bswResultTitle;
            public string bswResultMsg;
            public string bswErrorRefs;

            // RadialExtra (UIRenderer, ReorderableController)
            public string stateWithCurrent;
            public string stateOffInfo;
            public string stateOnInfo;
            public string defaultStateTitle;
            public string defaultStateOnDesc;
            public string defaultStateOffDesc;
            public string addFramesHint;
            public string durationTitle;
            public string durationTotal;
            public string divisionCount;
            public string segmentsStandard;
            public string segmentsEqual;
            public string sceneMaterialsError;
            public string systemInfoTitle;
            public string systemInfoNoFrames;
            public string systemInfoOnOff;
            public string systemInfoAB;
            public string systemInfoLinear;
            public string framesHeader;
            public string frameLabel;
            public string duplicateFrame;
            public string deleteFrameTitle;
            public string deleteFrameConfirm;

            // CoserRopaExtra
            public string humanoid;
            public string genericFallback;
            public string clothingListHeader;
            public string clothingLabel;
            public string boneCount;
            public string detailsTitle;
            public string maDetectedLabel;
            public string maWillProcess;
            public string maMergeArmature;
            public string maRecommended;
            public string maNoMergeArmature;
            public string maManualNeeded;
            public string boneStats;
            public string prefixSection;
            public string suffixSection;
            public string prefixKeep;
            public string prefixRemove;
            public string suffixKeep;
            public string suffixRemove;
            public string mappingHeaderAvatar;
            public string mappingHeaderClothing;
            public string mappingHeaderMethod;
            public string mappingHeaderStatus;
            public string addMapping;
            public string ndmfEnabledInfo;
            public string ndmfDisabledInfo;
            public string ndmfScope;
            public string ndmfSceneSafe;
            public string ndmfDisableMAInfo;
            public string ndmfNoPiecesEnabled;

            // PieceType
            public string pieceTypeLabel;
            public string pieceTypeRopa;
            public string pieceTypePelo;
            public string pieceTypePieza;

            // DisableMA
            public string disableMAToggle;
            public string disableMAWigWarning;
            public string disableMAMRWillProcess;

            // BoneProxy misplacement
            public string boneProxyMisplaced;
            public string boneProxyRelocate;
            public string boneProxyRelocateTo;

            // Manual zone
            public string manualZoneLabel;
            public string manualZoneAuto;
            public string manualZoneActive;

            // StitchZone
            public string zoneFullBody;
            public string zoneTorso;
            public string zoneHead;
            public string zoneUpperLimb;
            public string zoneLowerLimb;
            public string zoneHip;
            public string zoneRightHand;
            public string zoneLeftHand;
            public string zoneRightFoot;
            public string zoneLeftFoot;
            public string zoneChest;
            public string zoneNone;
            public string zoneFullBodyShort;
            public string zoneTorsoShort;
            public string zoneHeadShort;
            public string zoneUpperLimbShort;
            public string zoneLowerLimbShort;
            public string zoneHipShort;
            public string zoneRightHandShort;
            public string zoneLeftHandShort;
            public string zoneRightFootShort;
            public string zoneLeftFootShort;
            public string zoneChestShort;
            public string zoneNoneShort;
            public string zoneLabel;
            public string zoneWigLabel;
        }

        #endregion
    }
}
