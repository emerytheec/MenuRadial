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
            _isInitialized = true;
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
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
        }

        /// <summary>
        /// Recarga las traducciones del idioma actual
        /// </summary>
        public static void ReloadTranslations()
        {
            _isInitialized = false;
            _translations.Clear();
            Initialize();
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

            // Illumination
            public string rootObject;
            public string rootObjectTooltip;
            public string materialsDetected;
            public string noMaterialsFound;
            public string previewNotAvailable;
            public string illuminationValue;
            public string detectMaterials;

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

            // Nuevas claves para UI simplificada
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

            // AlternativeMaterial
            public string groupName;
            public string originalMaterial;
            public string alternativeMaterials;
            public string addMaterial;
            public string removeMaterial;
            public string linkToSlot;
            public string unlinkSlot;

            // Validation
            public string invalidReferences;
            public string avatarNotFound;
            public string validationSuccess;
            public string validationFailed;
            public string fixIssues;
        }

        #endregion
    }
}
