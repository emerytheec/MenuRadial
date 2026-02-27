using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.AnimationSystem;
using Bender_Dios.MenuRadial.Core.Common;
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
        private readonly SerializedProperty _defaultFrameIndexProp;
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
            SerializedProperty defaultFrameIndexProp,
            MRUnificarObjetosPreviewManager previewManager,
            MRUnificarObjetosReorderableController reorderableController)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _serializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            _activeFrameIndexProp = activeFrameIndexProp ?? throw new ArgumentNullException(nameof(activeFrameIndexProp));
            _autoUpdatePathsProp = autoUpdatePathsProp ?? throw new ArgumentNullException(nameof(autoUpdatePathsProp));
            _animationNameProp = animationNameProp ?? throw new ArgumentNullException(nameof(animationNameProp));
            _defaultFrameIndexProp = defaultFrameIndexProp; // Puede ser null si no se encuentra
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

                GUILayout.Space(5f);

                // Frame por Defecto en VRChat
                DrawDefaultFrameControl();

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
                labelText = MRLocalization.Get(L.RadialExtra.STATE_WITH_CURRENT, currentState);
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
                    MRLocalization.Get(L.RadialExtra.STATE_OFF_INFO) :
                    MRLocalization.Get(L.RadialExtra.STATE_ON_INFO);
                    
                EditorGUILayout.LabelField(infoText, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawDefaultFrameControl()
        {
            if (_defaultFrameIndexProp == null || _target.FrameCount == 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_TITLE), EditorStyles.boldLabel);

            if (_target.FrameCount == 1)
            {
                // OnOff: toggle simple
                bool isOn = _defaultFrameIndexProp.intValue > 0;
                bool newIsOn = EditorGUILayout.Toggle(
                    MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_ONOFF), isOn);
                if (newIsOn != isOn)
                {
                    _defaultFrameIndexProp.intValue = newIsOn ? 1 : 0;
                }

                string desc = newIsOn
                    ? MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_ON_DESC)
                    : MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_OFF_DESC);
                EditorGUILayout.HelpBox(desc, MessageType.Info);
            }
            else
            {
                // AB/Linear: popup con nombres de frames (numeración base 1 como la lista)
                var frameNames = new string[_target.FrameCount];
                var validFrames = _target.FrameObjects?.Where(f => f != null).ToArray();
                for (int i = 0; i < frameNames.Length; i++)
                {
                    string name = (validFrames != null && i < validFrames.Length && validFrames[i] != null)
                        ? validFrames[i].gameObject.name
                        : MRLocalization.Get(L.RadialExtra.FRAME_LABEL, i + 1);
                    frameNames[i] = $"{MRLocalization.Get(L.RadialExtra.FRAME_LABEL, i + 1)}: {name}";
                }

                int current = Mathf.Clamp(_defaultFrameIndexProp.intValue, 0, frameNames.Length - 1);
                int newIndex = EditorGUILayout.Popup(
                    MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_LABEL), current, frameNames);
                if (newIndex != _defaultFrameIndexProp.intValue)
                {
                    _defaultFrameIndexProp.intValue = newIndex;
                }

                // Info según tipo
                if (_target.AnimationType == AnimationType.AB)
                {
                    string frameName = (validFrames != null && newIndex < validFrames.Length && validFrames[newIndex] != null)
                        ? validFrames[newIndex].gameObject.name
                        : MRLocalization.Get(L.RadialExtra.FRAME_LABEL, newIndex + 1);
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_AB_INFO, frameName), MessageType.Info);
                }
                else
                {
                    float paramValue = _target.GetDefaultParameterValue();
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.RadialExtra.DEFAULT_FRAME_LINEAR_INFO, paramValue), MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
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
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f, 1f); // Naranja

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

            // Nota: AnimationPath ahora se configura desde MR Menu Radial
            // Nota: Default Frame se configura en la sección de Configuración General
        }
        
        private void DrawAnimationInfo()
        {
            if (_target.FrameCount == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.RadialExtra.ADD_FRAMES_HINT), MessageType.Info);
                return;
            }
            
            // Box de información
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.DURATION_TITLE), EditorStyles.boldLabel);

            // Información básica usando constantes estáticas
            const float TOTAL_DURATION = 4.25f;
            const int TOTAL_FRAMES = 255;
            EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.DURATION_TOTAL, TOTAL_DURATION, TOTAL_FRAMES));
            EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.DIVISION_COUNT, _target.FrameCount));

            // Cálculo de división automática
            if (_target.FrameCount > 0)
            {
                int framesPerSegment = TOTAL_FRAMES / _target.FrameCount;
                int remainingFrames = TOTAL_FRAMES % _target.FrameCount;

                if (remainingFrames > 0)
                {
                    EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.SEGMENTS_STANDARD, framesPerSegment, framesPerSegment + remainingFrames));
                }
                else
                {
                    EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.SEGMENTS_EQUAL, framesPerSegment));
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
                EditorGUILayout.HelpBox(MRLocalization.Get(L.RadialExtra.SCENE_MATERIALS_ERROR), MessageType.Error);
            }
            
            EditorGUI.BeginDisabledGroup(!canGenerate);
            
            // Botón generar animaciones
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
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
            GUI.backgroundColor = prevBg;

            EditorGUI.EndDisabledGroup();
            
            // Información sobre el sistema
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.RadialExtra.SYSTEM_INFO_TITLE), EditorStyles.boldLabel);

            string systemInfo = _target.FrameCount switch
            {
                0 => MRLocalization.Get(L.RadialExtra.SYSTEM_INFO_NO_FRAMES),
                1 => MRLocalization.Get(L.RadialExtra.SYSTEM_INFO_ONOFF),
                2 => MRLocalization.Get(L.RadialExtra.SYSTEM_INFO_AB),
                _ => MRLocalization.Get(L.RadialExtra.SYSTEM_INFO_LINEAR, _target.FrameCount)
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
