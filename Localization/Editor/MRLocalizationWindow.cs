using UnityEngine;
using UnityEditor;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Localization.Editor
{
    /// <summary>
    /// Ventana de configuracion del sistema de localizacion.
    /// Permite cambiar idioma, recargar traducciones y ver estadisticas.
    /// </summary>
    public class MRLocalizationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Menu Radial/Localization Settings", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<MRLocalizationWindow>("MR Localization");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            MRLocalization.OnLocaleChanged += OnLocaleChanged;
        }

        private void OnDisable()
        {
            MRLocalization.OnLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawCurrentLocaleSection();
            EditorGUILayout.Space(10);

            DrawAvailableLocalesSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.TITLE), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.SUBTITLE),
                EditorStyles.miniLabel);
        }

        private void DrawCurrentLocaleSection()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.CURRENT_LANGUAGE), EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.CODE_LABEL), GUILayout.Width(60));
                EditorGUILayout.LabelField(MRLocalization.CurrentLocale, EditorStyles.boldLabel);

                EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.NAME_LABEL), GUILayout.Width(60));
                EditorGUILayout.LabelField(MRLocalization.GetLocaleName(MRLocalization.CurrentLocale));
            }

            var statusText = MRLocalization.IsInitialized ? MRLocalization.Get(L.LocalizationWindow.STATUS_INITIALIZED) : MRLocalization.Get(L.LocalizationWindow.STATUS_NOT_INITIALIZED);
            EditorGUILayout.LabelField($"Estado: {statusText}", EditorStyles.miniLabel);
        }

        private void DrawAvailableLocalesSection()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.AVAILABLE_LANGUAGES), EditorStyles.boldLabel);

            var locales = MRLocalization.GetAvailableLocales();

            if (locales == null || locales.Length == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.LocalizationWindow.NO_LOCALE_FILES),
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var locale in locales)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool isCurrent = locale == MRLocalization.CurrentLocale;

                        // Flag/indicator
                        string flag = GetLocaleFlag(locale);
                        EditorGUILayout.LabelField(flag, GUILayout.Width(30));

                        // Locale name
                        EditorGUILayout.LabelField(MRLocalization.GetLocaleName(locale), GUILayout.Width(80));

                        // Code
                        EditorGUILayout.LabelField($"({locale})", EditorStyles.miniLabel, GUILayout.Width(40));

                        // Current indicator or select button
                        if (isCurrent)
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(MRLocalization.Get(L.LocalizationWindow.CURRENT_MARKER), EditorStyles.miniLabel, GUILayout.Width(60));
                        }
                        else
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(MRLocalization.Get(L.LocalizationWindow.SELECT_BUTTON), GUILayout.Width(80)))
                            {
                                MRLocalization.SetLocale(locale);
                            }
                        }
                    }
                }
            }
        }

        private string GetLocaleFlag(string locale)
        {
            return locale switch
            {
                "es" => "ES",
                "en" => "EN",
                "zh" => "ZH",
                "ko" => "KO",
                "ja" => "JA",
                "ru" => "RU",
                _ => locale.ToUpper()
            };
        }
    }
}
