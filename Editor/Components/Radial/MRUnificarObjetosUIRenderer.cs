using System;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.AnimationSystem;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.Radial
{
    /// <summary>
    /// Renderizador especializado para la interfaz de usuario del editor de MRUnificarObjetos
    /// Responsabilidad única: Renderizado de secciones, estilos y controles de UI
    /// </summary>
    public class MRUnificarObjetosUIRenderer
    {
        
        private readonly MRUnificarObjetos _target;
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _activeFrameIndexProp;
        private readonly SerializedProperty _autoUpdatePathsProp;
        private readonly SerializedProperty _animationNameProp;
        private readonly SerializedProperty _animationPathProp;
        private readonly SerializedProperty _defaultStateIsOnProp;
        private readonly MRUnificarObjetosPreviewManager _previewManager;
        private readonly MRUnificarObjetosReorderableController _reorderableController;
        
        // Secciones expandibles
        private bool _showGeneralConfig = true;
        private bool _showFramesList = true;
        private bool _showAnimationSettings = true;
        
        // Recursos visuales
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _frameItemStyle;
        
        // Constantes de diseño
        private const float SECTION_SPACING = 10f;
        private const float BUTTON_HEIGHT = 25f;
        
        
        
        public MRUnificarObjetosUIRenderer(
            MRUnificarObjetos target,
            SerializedObject serializedObject,
            SerializedProperty activeFrameIndexProp,
            SerializedProperty autoUpdatePathsProp,
            SerializedProperty animationNameProp,
            SerializedProperty animationPathProp,
            SerializedProperty defaultStateIsOnProp,
            MRUnificarObjetosPreviewManager previewManager,
            MRUnificarObjetosReorderableController reorderableController)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _serializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            _activeFrameIndexProp = activeFrameIndexProp ?? throw new ArgumentNullException(nameof(activeFrameIndexProp));
            _autoUpdatePathsProp = autoUpdatePathsProp ?? throw new ArgumentNullException(nameof(autoUpdatePathsProp));
            _animationNameProp = animationNameProp ?? throw new ArgumentNullException(nameof(animationNameProp));
            _animationPathProp = animationPathProp ?? throw new ArgumentNullException(nameof(animationPathProp));
            _defaultStateIsOnProp = defaultStateIsOnProp; // Puede ser null si no se encuentra
            _previewManager = previewManager ?? throw new ArgumentNullException(nameof(previewManager));
            _reorderableController = reorderableController ?? throw new ArgumentNullException(nameof(reorderableController));
        }
        
        
        
        /// <summary>
        /// Renderiza toda la interfaz del editor
        /// </summary>
        public void RenderUI()
        {
            // Aplicar estilos
            InitializeStyles();
            
            GUILayout.Space(SECTION_SPACING);
            
            // Sección: Configuración General
            DrawGeneralConfigurationSection();
            
            GUILayout.Space(SECTION_SPACING);
            
            // Sección: Lista de Frames
            DrawFramesListSection();
            
            GUILayout.Space(SECTION_SPACING);
            
            // Sección: Ajustes de Animación
            DrawAnimationSettingsSection();
        }
        
        
        
        private void InitializeStyles()
        {
            if (_sectionStyle == null)
            {
                _sectionStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }
            
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Normal
                };
            }
            
            if (_frameItemStyle == null)
            {
                _frameItemStyle = new GUIStyle()
                {
                    normal = { background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.5f)) },
                    border = new RectOffset(1, 1, 1, 1),
                    padding = new RectOffset(5, 5, 2, 2)
                };
            }
        }
        
        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }
        
        
        
        private void DrawGeneralConfigurationSection()
        {
            _showGeneralConfig = EditorGUILayout.Foldout(_showGeneralConfig, MRLocalization.Get(L.Frame.GENERAL_CONFIG), _sectionStyle);

            if (_showGeneralConfig)
            {
                EditorGUI.indentLevel++;

                // Frame Activo con slider y botones
                DrawActiveFrameControl();

                GUILayout.Space(5f);

                // Auto-actualizar Rutas
                EditorGUILayout.PropertyField(_autoUpdatePathsProp, MRLocalization.GetContent(L.Common.AUTO_UPDATE));

                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawActiveFrameControl()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Label "Frame Activo" con estado especial para On/Off
            string labelText = MRLocalization.Get(L.Radial.FRAME_ACTIVE);
            if (_target.FrameCount == 1)
            {
                string currentState = _target.ActiveFrameIndex == 0
                    ? MRLocalization.Get(L.Radial.STATE_OFF)
                    : MRLocalization.Get(L.Radial.STATE_ON);
                labelText = $"Estado ({currentState})";
            }
            EditorGUILayout.LabelField(labelText, GUILayout.Width(100f));

            // Botón "< Anterior"
            GUI.enabled = _target.FrameCount > 0;
            if (GUILayout.Button(MRLocalization.Get(L.Radial.PREVIOUS_FRAME), _buttonStyle, GUILayout.Width(70f)))
            {
                if (_target.FrameCount == 1)
                {
                    // Para On/Off: alternar entre 0 (OFF) y 1 (ON)
                    _target.ActiveFrameIndex = _target.ActiveFrameIndex == 0 ? 1 : 0;
                }
                else
                {
                    // Para múltiples frames: navegación normal
                    _target.SelectPreviousFrame();
                }
                _activeFrameIndexProp.intValue = _target.ActiveFrameIndex;
                
                // Aplicar previsualización del frame activo
                _previewManager.ApplyFramePreview();
            }
            
            // Slider para frame activo con lógica especial para On/Off
            int maxSliderValue;
            if (_target.FrameCount == 1)
            {
                // Para On/Off: rango 0-1 (OFF-ON)
                maxSliderValue = 1;
            }
            else
            {
                // Para múltiples frames: rango normal
                maxSliderValue = Mathf.Max(0, _target.FrameCount - 1);
            }
            
            int newFrameIndex = EditorGUILayout.IntSlider(_activeFrameIndexProp.intValue, 0, maxSliderValue);
            if (newFrameIndex != _activeFrameIndexProp.intValue)
            {
                
                _activeFrameIndexProp.intValue = newFrameIndex;
                _target.ActiveFrameIndex = newFrameIndex;
                
                
                // Aplicar previsualización al cambiar con slider
                _previewManager.ApplyFramePreview();
            }
            
            // Botón "Siguiente >"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.NEXT_FRAME), _buttonStyle, GUILayout.Width(80f)))
            {
                if (_target.FrameCount == 1)
                {
                    // Para On/Off: alternar entre 0 (OFF) y 1 (ON)
                    _target.ActiveFrameIndex = _target.ActiveFrameIndex == 0 ? 1 : 0;
                }
                else
                {
                    // Para múltiples frames: navegación normal
                    _target.SelectNextFrame();
                }
                _activeFrameIndexProp.intValue = _target.ActiveFrameIndex;
                
                // Aplicar previsualización del frame activo
                _previewManager.ApplyFramePreview();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // NUEVO: Información especial para animaciones On/Off
            if (_target.FrameCount == 1)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(110f); // Alinear con el label
                
                string infoText = _target.ActiveFrameIndex == 0 ? 
                    "🟥 Estado OFF: Todos los elementos apagados/neutrales" : 
                    "🟢 Estado ON: Frame activo aplicado";
                    
                EditorGUILayout.LabelField(infoText, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        
        
        private void DrawFramesListSection()
        {
            // Título con contador
            string framesSectionTitle = MRLocalization.Get(L.Radial.FRAMES_SECTION, _target.FrameCount);
            _showFramesList = EditorGUILayout.Foldout(_showFramesList, framesSectionTitle, _sectionStyle);
            
            if (_showFramesList)
            {
                EditorGUI.indentLevel++;
                
                // Área de drag & drop ENCIMA de la lista
                DrawFrameDropArea();
                
                GUILayout.Space(10f);
                
                // Lista de frames
                DrawFramesList();
                
                GUILayout.Space(5f);
                
                // Botones de gestión
                DrawFrameManagementButtons();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawFramesList()
        {
            if (_target.FrameCount == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.Radial.DROP_FRAMES_HERE), MessageType.Info);
                return;
            }
            
            // Usar ReorderableList del controlador
            _reorderableController.ReorderableFramesList.DoLayoutList();
        }
        
        private void DrawFrameDropArea()
        {
            // Área de drop
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            
            // Estilo del área de drop
            var dropStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray }
            };
            
            GUI.Box(dropRect, MRLocalization.Get(L.Radial.DROP_FRAMES_HERE), dropStyle);
            
            // Delegar manejo del drag & drop al controlador
            _reorderableController.HandleFrameDropArea(dropRect);
        }
        
        private void DrawFrameManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Botón para crear nuevo Agrupar Objetos
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f, 1f); // Verde

            if (GUILayout.Button(MRLocalization.Get(L.Radial.CREATE_AGRUPAR_OBJETOS), GUILayout.Height(25f)))
            {
                _target.CreateFrameObject();
                _serializedObject.Update();
            }

            GUI.backgroundColor = originalColor;

            // Botón "Limpiar Frames Null"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.CLEANUP_NULL), GUILayout.Height(25f), GUILayout.Width(100f)))
            {
                _target.CleanupInvalidFrames();
                EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.SUCCESS),
                    MRLocalization.Get(L.Radial.CLEANUP_NULL), MRLocalization.Get(L.Common.OK));
            }

            EditorGUILayout.EndHorizontal();

            // Información sobre drag & drop
            EditorGUILayout.HelpBox(MRLocalization.Get(L.Radial.TIP_CREATE_CHILD), MessageType.Info);
        }
        
        
        
        private void DrawAnimationSettingsSection()
        {
            _showAnimationSettings = EditorGUILayout.Foldout(_showAnimationSettings, MRLocalization.Get(L.Radial.ANIMATION_SETTINGS), _sectionStyle);
            
            if (_showAnimationSettings)
            {
                EditorGUI.indentLevel++;
                
                // Configuración de animación
                DrawAnimationConfiguration();
                
                GUILayout.Space(10f);
                
                // Información de duración
                DrawAnimationInfo();
                
                GUILayout.Space(10f);
                
                // Botón generar animación
                DrawGenerateAnimationButton();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAnimationConfiguration()
        {
            // Nombre de Animación
            EditorGUILayout.PropertyField(_animationNameProp, MRLocalization.GetContent(L.Radial.ANIMATION_NAME));

            // Ruta de Animación
            EditorGUILayout.PropertyField(_animationPathProp, MRLocalization.GetContent(L.Radial.ANIMATION_PATH));

            // Mostrar opción de Default State solo para OnOff (1 frame)
            if (_target.FrameCount == 1 && _defaultStateIsOnProp != null)
            {
                GUILayout.Space(5f);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Estado por Defecto en FX", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_defaultStateIsOnProp,
                    MRLocalization.GetContent(L.Radial.DEFAULT_STATE_IS_ON, L.Radial.DEFAULT_STATE_IS_ON_TOOLTIP));

                // Mostrar información del estado actual
                string stateInfo = _defaultStateIsOnProp.boolValue
                    ? "El avatar iniciará con este efecto ACTIVADO"
                    : "El avatar iniciará con este efecto DESACTIVADO";
                EditorGUILayout.HelpBox(stateInfo, MessageType.Info);

                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawAnimationInfo()
        {
            if (_target.FrameCount == 0)
            {
                EditorGUILayout.HelpBox("Añade frames para ver información de duración", MessageType.Info);
                return;
            }
            
            // Box de información
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Información de Duración", EditorStyles.boldLabel);
            
            // Información básica usando constantes estáticas
            const float TOTAL_DURATION = 4.25f;
            const int TOTAL_FRAMES = 255;
            EditorGUILayout.LabelField($"Duración total: {TOTAL_DURATION:F2} segundos ({TOTAL_FRAMES} frames a 60 FPS)");
            EditorGUILayout.LabelField($"División: {_target.FrameCount} segmentos");
            
            // Cálculo de división automática
            if (_target.FrameCount > 0)
            {
                int framesPerSegment = TOTAL_FRAMES / _target.FrameCount;
                int remainingFrames = TOTAL_FRAMES % _target.FrameCount;
                
                if (remainingFrames > 0)
                {
                    EditorGUILayout.LabelField($"Segmentos estándar: {framesPerSegment} frames, último segmento: {framesPerSegment + remainingFrames} frames");
                }
                else
                {
                    EditorGUILayout.LabelField($"Todos los segmentos: {framesPerSegment} frames");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGenerateAnimationButton()
        {
            // Validar materiales antes de habilitar el botón
            bool hasSceneMaterials = CheckForSceneMaterials();
            bool canGenerate = _target.FrameCount > 0 && !hasSceneMaterials;
            
            // Mostrar mensaje de error si hay materiales de escena
            if (hasSceneMaterials)
            {
                EditorGUILayout.HelpBox("Existen materiales instancia de escena. Asigna assets antes de generar.", MessageType.Error);
            }
            
            EditorGUI.BeginDisabledGroup(!canGenerate);
            
            // Botón generar animaciones
            if (GUILayout.Button(MRLocalization.Get(L.Radial.GENERATE_ANIMATIONS), GUILayout.Height(35f)))
            {
                // Generar animaciones usando RadialAnimationBuilder
                if (_target.FrameCount > 0)
                {
                    try
                    {
                        RadialAnimationBuilder.GenerateAnimations(_target);
                        EditorUtility.DisplayDialog(MRLocalization.Get(L.Radial.GENERATE_ANIMATIONS),
                            MRLocalization.Get(L.Radial.ANIMATIONS_GENERATED), MRLocalization.Get(L.Common.OK));
                    }
                    catch (System.Exception ex)
                    {
                        EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.ERROR),
                            $"Error: {ex.Message}", MRLocalization.Get(L.Common.OK));
                    }
                }
            }
            
            EditorGUI.EndDisabledGroup();
            
            // Información sobre el sistema
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ℹ️ Información del Sistema:", EditorStyles.boldLabel);
            
            string systemInfo = _target.FrameCount switch
            {
                0 => "Configure frames para comenzar",
                1 => "• Configurado para efectos ON/OFF",
                2 => "• Configurado para alternar entre dos estados", 
                _ => $"• Configurado con {_target.FrameCount} frames"
            };
            
            EditorGUILayout.LabelField(systemInfo, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Verifica si hay materiales que son instancias de escena (no assets)
        /// </summary>
        private bool CheckForSceneMaterials()
        {
            if (_target?.FrameObjects == null) return false;
            
            foreach (var frame in _target.FrameObjects)
            {
                if (frame?.MaterialReferences == null) continue;
                
                foreach (var matRef in frame.MaterialReferences)
                {
                    // Verificar material alternativo
                    if (matRef.AlternativeMaterial != null)
                    {
                        string path = AssetDatabase.GetAssetPath(matRef.AlternativeMaterial);
                        if (string.IsNullOrEmpty(path)) return true;
                    }
                    
                    // Verificar material original
                    if (matRef.OriginalMaterial != null)
                    {
                        string path = AssetDatabase.GetAssetPath(matRef.OriginalMaterial);
                        if (string.IsNullOrEmpty(path)) return true;
                    }
                }
            }
            
            return false;
        }
        
    }
}
