using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Shaders;
using Bender_Dios.MenuRadial.Shaders.Models;
using Bender_Dios.MenuRadial.Shaders.Strategies;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.Illumination
{
    /// <summary>
    /// Renderizador de interfaz de usuario para el componente MRIluminacionRadial
    /// Responsabilidad única: Solo renderizado de UI
    /// VERSIÓN 0.033: Slider "Frame Actual" normalizado (0-1) para consistencia con otros componentes
    /// </summary>
    public class IlluminationUIRenderer
    {
        private readonly MRIluminacionRadial _target;
        private readonly SerializedObject _serializedObject;
        private readonly IlluminationPreviewManager _previewManager;
        
        // Propiedades serializadas
        private SerializedProperty _rootObjectProperty;
        private SerializedProperty _animationNameProperty;
        
        // Estado de UI
        private bool _showAdvancedSettings = false;
        
        // Estilos de UI
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionStyle;
        
        // Constantes de diseño
        private const float SECTION_SPACING = 10f;
        private const float BUTTON_HEIGHT = 25f;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">Componente objetivo</param>
        /// <param name="serializedObject">Objeto serializado</param>
        /// <param name="previewManager">Gestor de preview</param>
        public IlluminationUIRenderer(MRIluminacionRadial target, SerializedObject serializedObject, IlluminationPreviewManager previewManager)
        {
            _target = target ?? throw new System.ArgumentNullException(nameof(target));
            _serializedObject = serializedObject ?? throw new System.ArgumentNullException(nameof(serializedObject));
            _previewManager = previewManager ?? throw new System.ArgumentNullException(nameof(previewManager));
            InitializeSerializedProperties();
        }        
        
        /// <summary>
        /// Inicializa los estilos de UI
        /// </summary>
        private void InitializeStyles()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal
                };
            }
            
            if (_sectionStyle == null)
            {
                _sectionStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }
        }
        
        /// <summary>
        /// Inicializa las propiedades serializadas
        /// </summary>
        private void InitializeSerializedProperties()
        {
            _rootObjectProperty = _serializedObject.FindProperty("_rootObject");
            _animationNameProperty = _serializedObject.FindProperty("_animationName");
        }
        
        /// <summary>
        /// Renderiza la interfaz de usuario completa
        /// </summary>
        public void RenderUI()
        {
            InitializeStyles();

            RenderHeader();
            RenderGeneralConfiguration();
            RenderPreviewSection();
            RenderAnimationSettings();
            RenderAdvancedSettings();
        }
        
        /// <summary>
        /// Renderiza el header del componente
        /// </summary>
        private void RenderHeader()
        {
            EditorGUILayout.Space(10);

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField(MRLocalization.Get(L.Illumination.HEADER), headerStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(MRLocalization.Get(L.Illumination.HEADER_SUBTITLE), EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(10);
        }
        
        /// <summary>
        /// Renderiza la sección de configuración general
        /// </summary>
        private void RenderGeneralConfiguration()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.Frame.GENERAL_CONFIG), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_rootObjectProperty, MRLocalization.GetContent(L.Illumination.ROOT_OBJECT, L.Illumination.ROOT_OBJECT_TOOLTIP));

            // Auto-actualizar Rutas (consistencia con otros componentes MR)
            var autoUpdatePathsProperty = _serializedObject.FindProperty("_autoUpdatePaths");
            if (autoUpdatePathsProperty != null)
            {
                EditorGUILayout.PropertyField(autoUpdatePathsProperty, MRLocalization.GetContent(L.Common.AUTO_UPDATE, L.Common.AUTO_UPDATE_TOOLTIP));
            }
            else
            {
                // Fallback si no encuentra la propiedad
                _target.AutoUpdatePaths = EditorGUILayout.Toggle(MRLocalization.GetContent(L.Common.AUTO_UPDATE, L.Common.AUTO_UPDATE_TOOLTIP),
                    _target.AutoUpdatePaths);
            }

            // Mostrar materiales detectados automáticamente
            if (_target.RootObject != null)
            {
                // Auto-escanear si no hay materiales detectados
                if (_target.DetectedMaterials.Count == 0)
                {
                    _target.ScanMaterials();
                }

                if (_target.DetectedMaterials.Count > 0)
                {
                    EditorGUILayout.HelpBox(MRLocalization.Get(L.Illumination.MATERIALS_DETECTED, _target.DetectedMaterials.Count), MessageType.Info);

                    // Verificar si hay materiales Poiyomi y mostrar advertencia
                    RenderPoiyomiWarningIfNeeded();
                }
                else
                {
                    EditorGUILayout.HelpBox(MRLocalization.Get(L.Illumination.NO_MATERIALS_FOUND), MessageType.Warning);
                }
            }

            EditorGUILayout.Space(SECTION_SPACING);
        }

        /// <summary>
        /// Muestra advertencia si hay materiales Poiyomi detectados
        /// Los materiales Poiyomi requieren configuracion especial para animaciones
        /// </summary>
        private void RenderPoiyomiWarningIfNeeded()
        {
            var poiyomiStrategy = ShaderStrategyFactory.Instance.GetStrategy(ShaderType.Poiyomi) as PoiyomiShaderStrategy;
            if (poiyomiStrategy == null) return;

            var poiyomiMaterials = new System.Collections.Generic.List<Material>();
            bool hasLockedWithMissingProps = false;
            bool allReady = true;

            foreach (var material in _target.DetectedMaterials)
            {
                if (material != null && poiyomiStrategy.IsCompatible(material))
                {
                    poiyomiMaterials.Add(material);

                    if (poiyomiStrategy.IsMaterialLocked(material))
                    {
                        var existingProps = poiyomiStrategy.GetExistingPropertyNames(material);
                        if (existingProps.Length < 3)
                        {
                            hasLockedWithMissingProps = true;
                            allReady = false;
                        }
                    }
                    else
                    {
                        if (!poiyomiStrategy.AreAllPropertiesMarkedAsAnimated(material))
                        {
                            allReady = false;
                        }
                    }
                }
            }

            if (poiyomiMaterials.Count == 0) return;

            // Mostrar estado
            if (allReady)
            {
                EditorGUILayout.HelpBox(
                    $"Poiyomi: {poiyomiMaterials.Count} material(es) listos para animacion.",
                    MessageType.Info);
            }
            else if (hasLockedWithMissingProps)
            {
                EditorGUILayout.HelpBox(
                    "Poiyomi: Algunos materiales bloqueados no tienen las propiedades configuradas para animacion.",
                    MessageType.Warning);

                // Boton para preparar automaticamente
                if (PoiyomiShaderStrategy.IsShaderOptimizerAvailable())
                {
                    if (GUILayout.Button("Preparar materiales Poiyomi (automatico)", GUILayout.Height(25)))
                    {
                        if (poiyomiStrategy.PrepareAndLockMaterials(poiyomiMaterials))
                        {
                            UnityEditor.AssetDatabase.SaveAssets();
                            _target.ScanMaterials(); // Re-escanear
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "ThryEditor/ShaderOptimizer no encontrado. Debes preparar los materiales manualmente:\n" +
                        "1. Desbloquea cada material\n" +
                        "2. Marca como 'Animated': _PPLightingMultiplier, _MinBrightness, _Grayscale_Lighting\n" +
                        "3. Vuelve a bloquear",
                        MessageType.Error);
                }
            }
            else
            {
                // Hay materiales desbloqueados sin marcar
                EditorGUILayout.HelpBox(
                    "Poiyomi: Hay materiales que necesitan configuracion.",
                    MessageType.Warning);

                if (GUILayout.Button("Marcar propiedades como Animated", GUILayout.Height(25)))
                {
                    int marked = 0;
                    foreach (var material in poiyomiMaterials)
                    {
                        if (!poiyomiStrategy.IsMaterialLocked(material))
                        {
                            if (poiyomiStrategy.MarkPropertiesAsAnimated(material))
                            {
                                EditorUtility.SetDirty(material);
                                marked++;
                            }
                        }
                    }

                    if (marked > 0)
                    {
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"[MR Iluminacion] Marcadas propiedades en {marked} material(es).");
                    }
                }
            }
        }
        
        /// <summary>
        /// Renderiza la sección de preview con slider (similar a MRUnificarObjetos)
        /// Siempre visible, se activa automáticamente al interactuar
        /// </summary>
        private void RenderPreviewSection()
        {
            bool canPreview = _target.DetectedMaterials.Count > 0;

            // Control de frame con slider - Siempre visible
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(MRLocalization.Get(L.Radial.CURRENT_FRAME), GUILayout.Width(80));

            EditorGUI.BeginDisabledGroup(!canPreview);

            // Botón "< Anterior"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.PREVIOUS_FRAME), _buttonStyle, GUILayout.Width(70f)))
            {
                EnsurePreviewActive();
                _previewManager.PreviousFrame();
            }

            // Conversión: Frame interno (0-255) → Valor normalizado (0-1)
            float normalizedValue = _previewManager.CurrentFrame / 255f;

            // Slider normalizado (0-1)
            float newNormalizedValue = EditorGUILayout.Slider(normalizedValue, 0f, 1f);

            // Conversión: Valor normalizado (0-1) → Frame interno (0-255)
            if (!Mathf.Approximately(newNormalizedValue, normalizedValue))
            {
                EnsurePreviewActive();
                int newFrame = Mathf.RoundToInt(newNormalizedValue * 255f);
                _previewManager.CurrentFrame = newFrame;
            }

            // Botón "Siguiente >"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.NEXT_FRAME), _buttonStyle, GUILayout.Width(80f)))
            {
                EnsurePreviewActive();
                _previewManager.NextFrame();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (!canPreview)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.Illumination.ASSIGN_ROOT_HINT), MessageType.Info);
            }

            EditorGUILayout.Space(SECTION_SPACING);
        }

        /// <summary>
        /// Asegura que el preview esté activo
        /// </summary>
        private void EnsurePreviewActive()
        {
            if (!_previewManager.IsPreviewActive)
            {
                _previewManager.StartPreview();
            }
        }
        
        /// <summary>
        /// Renderiza la sección de ajustes de animación
        /// </summary>
        private void RenderAnimationSettings()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.Radial.ANIMATION_SETTINGS), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_animationNameProperty, new GUIContent(MRLocalization.Get(L.Radial.ANIMATION_NAME)));
            // Nota: AnimationPath ahora se configura desde MR Menu Radial

            EditorGUILayout.Space(5);

            // Botón generar animación
            if (GUILayout.Button(MRLocalization.Get(L.Illumination.GENERATE_ANIMATION), GUILayout.Height(25)))
            {
                _target.GenerateIlluminationAnimation();
            }

            EditorGUILayout.Space(5);
        }
        

        

        
        /// <summary>
        /// Renderiza la sección de configuración avanzada
        /// </summary>
        private void RenderAdvancedSettings()
        {
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, MRLocalization.Get(L.Common.ADVANCED_SETTINGS), true);

            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;

                // Estadísticas de materiales
                if (_target.RootObject != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField(MRLocalization.Get(L.Illumination.STATISTICS), EditorStyles.miniBoldLabel);

                    var stats = _target.GetMaterialStats();
                    EditorGUILayout.LabelField(stats, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}