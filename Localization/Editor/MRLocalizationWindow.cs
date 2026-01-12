using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Localization.Editor
{
    /// <summary>
    /// Ventana de configuracion del sistema de localizacion.
    /// Permite cambiar idioma, recargar traducciones y ver estadisticas.
    /// </summary>
    public class MRLocalizationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _testKey = "common.confirm";
        private string _testResult = "";

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
            EditorGUILayout.Space(10);

            DrawTestSection();
            EditorGUILayout.Space(10);

            DrawActionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MR Localization", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sistema de internacionalizacion para Menu Radial",
                EditorStyles.miniLabel);
        }

        private void DrawCurrentLocaleSection()
        {
            EditorGUILayout.LabelField("Idioma Actual", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Codigo:", GUILayout.Width(60));
                EditorGUILayout.LabelField(MRLocalization.CurrentLocale, EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Nombre:", GUILayout.Width(60));
                EditorGUILayout.LabelField(MRLocalization.GetLocaleName(MRLocalization.CurrentLocale));
            }

            EditorGUILayout.LabelField($"Estado: {(MRLocalization.IsInitialized ? "Inicializado" : "No inicializado")}",
                EditorStyles.miniLabel);
        }

        private void DrawAvailableLocalesSection()
        {
            EditorGUILayout.LabelField("Idiomas Disponibles", EditorStyles.boldLabel);

            var locales = MRLocalization.GetAvailableLocales();

            if (locales == null || locales.Length == 0)
            {
                EditorGUILayout.HelpBox("No se encontraron archivos de idioma en Resources/Locales/",
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
                            EditorGUILayout.LabelField("< Actual", EditorStyles.miniLabel, GUILayout.Width(60));
                        }
                        else
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Seleccionar", GUILayout.Width(80)))
                            {
                                MRLocalization.SetLocale(locale);
                            }
                        }
                    }
                }
            }
        }

        private void DrawTestSection()
        {
            EditorGUILayout.LabelField("Probar Traduccion", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _testKey = EditorGUILayout.TextField("Key:", _testKey);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Obtener", GUILayout.Width(80)))
                    {
                        _testResult = MRLocalization.Get(_testKey);
                    }

                    if (GUILayout.Button("Con Param", GUILayout.Width(80)))
                    {
                        _testResult = MRLocalization.Get(_testKey, 5, "test");
                    }
                }

                if (!string.IsNullOrEmpty(_testResult))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Resultado:", EditorStyles.miniLabel);
                    EditorGUILayout.TextArea(_testResult, EditorStyles.wordWrappedLabel);
                }
            }
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Recargar Traducciones"))
                {
                    MRLocalization.ReloadTranslations();
                    Debug.Log("[MRLocalization] Traducciones recargadas");
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Abrir Carpeta de Locales"))
                {
                    string path = "Assets/Bender_Dios/MenuRadial/Localization/Resources/Locales";
                    var folder = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (folder != null)
                    {
                        EditorGUIUtility.PingObject(folder);
                        Selection.activeObject = folder;
                    }
                    else
                    {
                        Debug.LogWarning($"[MRLocalization] Carpeta no encontrada: {path}");
                    }
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Resetear a Idioma de Unity"))
                {
                    EditorPrefs.DeleteKey("MRLocalization_Locale");
                    MRLocalization.ReloadTranslations();
                    Debug.Log("[MRLocalization] Idioma reseteado a configuracion de Unity Editor");
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
