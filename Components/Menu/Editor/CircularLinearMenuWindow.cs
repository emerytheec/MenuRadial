using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Components.Menu.Editor
{
    /// <summary>
    /// Ventana de editor especializada para animaciones lineales con interfaz circular
    /// Reutiliza toda la lógica de MRUnificarObjetos pero con visualización radial
    /// VERSIÓN 0.046: Integración completa con MR Control Menu
    /// </summary>
    public class CircularLinearMenuWindow : EditorWindow
    {
        
        private MRUnificarObjetos _targetRadialMenu;
        private MRMenuControl _parentControlMenu;
        private string _slotName;
        private CircularLinearMenuRenderer _circularRenderer;
        
        // Control de preview
        private bool _previewEnabled = false;
        
        // Dimensiones de la ventana
        private const float WINDOW_WIDTH = 400f;
        private const float WINDOW_HEIGHT = 350f;
        private const float CIRCLE_AREA_HEIGHT = 200f;
        
        
        
        /// <summary>
        /// Abre la ventana circular para un MRUnificarObjetos específico
        /// </summary>
        /// <param name="targetRadialMenu">El MRUnificarObjetos a controlar</param>
        /// <param name="parentControlMenu">El MRMenuControl padre</param>
        /// <param name="slotName">Nombre del slot para el título</param>
        public static void OpenCircularMenu(MRUnificarObjetos targetRadialMenu, MRMenuControl parentControlMenu, string slotName)
        {
            if (targetRadialMenu == null)
            {
                return;
            }
            
            if (targetRadialMenu.AnimationType != AnimationType.Linear)
            {
                return;
            }
            
            // Crear o enfocar la ventana
            CircularLinearMenuWindow window = GetWindow<CircularLinearMenuWindow>(true, MRLocalization.Get(L.CircularMenu.WINDOW_TITLE, slotName), true);
            
            // Configurar la ventana
            window.position = new Rect(
                (Screen.currentResolution.width - WINDOW_WIDTH) * 0.5f,
                (Screen.currentResolution.height - WINDOW_HEIGHT) * 0.5f,
                WINDOW_WIDTH,
                WINDOW_HEIGHT
            );
            
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.maxSize = new Vector2(WINDOW_WIDTH + 100f, WINDOW_HEIGHT + 100f);
            
            // Configurar datos
            window._targetRadialMenu = targetRadialMenu;
            window._parentControlMenu = parentControlMenu;
            window._slotName = slotName ?? MRLocalization.Get(L.CircularMenu.LINEAR_ANIMATION);
            
            // Inicializar renderizador circular
            Vector2 center = new Vector2(WINDOW_WIDTH * 0.5f, CIRCLE_AREA_HEIGHT * 0.5f + 30f);
            window._circularRenderer = new CircularLinearMenuRenderer(targetRadialMenu, center);
            
            window.Show();
            
        }
        
        
        
        private void OnEnable()
        {
            // Configurar título de la ventana
            titleContent = new GUIContent($"🎯 {_slotName}", MRLocalization.Get(L.CircularMenu.WINDOW_TOOLTIP));
        }
        
        private void OnGUI()
        {
            if (_targetRadialMenu == null)
            {
                DrawErrorMessage(MRLocalization.Get(L.CircularMenu.ERROR_NO_TARGET));
                return;
            }
            
            if (_targetRadialMenu.AnimationType != AnimationType.Linear)
            {
                DrawErrorMessage(MRLocalization.Get(L.CircularMenu.ERROR_NOT_LINEAR, _targetRadialMenu.AnimationType));
                return;
            }
            
            if (_targetRadialMenu.FrameCount < 3)
            {
                DrawErrorMessage(MRLocalization.Get(L.CircularMenu.ERROR_MIN_FRAMES, _targetRadialMenu.FrameCount));
                return;
            }
            
            // Dibujar interfaz principal
            DrawHeader();
            DrawCircularInterface();
            DrawControls();
            DrawFooter();
        }
        
        private void OnDestroy()
        {
        }
        
        
        
        private void DrawErrorMessage(string message)
        {
            EditorGUILayout.Space(20f);
            EditorGUILayout.HelpBox(message, MessageType.Error);
            
            EditorGUILayout.Space(10f);
            if (GUILayout.Button(MRLocalization.Get(L.CircularMenu.CLOSE_WINDOW)))
            {
                Close();
            }
        }
        
        private void DrawHeader()
        {
            // Título con ícono
            EditorGUILayout.Space(10f);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField($"🎯 {_slotName}", titleStyle);

            // Información básica
            GUIStyle infoStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField(MRLocalization.Get(L.CircularMenu.HEADER_INFO, _targetRadialMenu.FrameCount, _targetRadialMenu.AnimationName), infoStyle);
            
            EditorGUILayout.Space(10f);
        }
        
        private void DrawCircularInterface()
        {
            // Área reservada para el círculo
            Rect circleArea = GUILayoutUtility.GetRect(WINDOW_WIDTH - 20f, CIRCLE_AREA_HEIGHT);
            
            // Fondo del área circular
            EditorGUI.DrawRect(circleArea, new Color(0.15f, 0.15f, 0.15f, 0.3f));
            
            // Renderizar interfaz circular
            if (_circularRenderer != null)
            {
                bool hasChanges = _circularRenderer.RenderCircularInterface(circleArea);
                
                if (hasChanges)
                {
                    // Forzar repaint para actualizar la interfaz
                    Repaint();
                    
                    // Aplicar preview si está habilitado
                    if (_previewEnabled)
                    {
                        ApplyFramePreview();
                    }
                }
            }
        }
        
        private void DrawControls()
        {
            EditorGUILayout.Space(10f);
            
            // Área de controles
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Control de preview
            EditorGUILayout.BeginHorizontal();
            
            bool newPreviewEnabled = EditorGUILayout.Toggle(MRLocalization.Get(L.CircularMenu.REALTIME_PREVIEW), _previewEnabled);
            if (newPreviewEnabled != _previewEnabled)
            {
                _previewEnabled = newPreviewEnabled;
                
                if (_previewEnabled)
                {
                    ApplyFramePreview();
                }
                else
                {
                    // No cancelamos el preview, solo dejamos de aplicar cambios automáticos
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Botón aplicar frame manualmente
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = _targetRadialMenu.ActiveFrame != null;
            if (GUILayout.Button($"🎨 {MRLocalization.Get(L.CircularMenu.APPLY_FRAME)}", GUILayout.Height(20f)))
            {
                ApplyFramePreview();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5f);
            
            // Información del frame actual
            if (_targetRadialMenu.ActiveFrame != null)
            {
                string frameInfo = MRLocalization.Get(L.CircularMenu.ACTIVE_FRAME_INFO, _targetRadialMenu.ActiveFrameIndex + 1, _targetRadialMenu.FrameCount, _targetRadialMenu.ActiveFrame.FrameName);
                EditorGUILayout.LabelField(frameInfo, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space(10f);
            
            // Botones de acción
            EditorGUILayout.BeginHorizontal();
            
            // Botón volver al MRMenuControl
            if (GUILayout.Button(MRLocalization.Get(L.CircularMenu.BACK_TO_MENU), GUILayout.Height(25f)))
            {
                Close();
                
                // Enfocar el MRMenuControl padre si existe
                if (_parentControlMenu != null)
                {
                    Selection.activeGameObject = _parentControlMenu.gameObject;
                }
            }
            
            GUILayout.FlexibleSpace();
            
            // Botón abrir MRUnificarObjetos completo
            if (GUILayout.Button(MRLocalization.Get(L.CircularMenu.OPEN_FULL_EDITOR), GUILayout.Height(25f)))
            {
                // Seleccionar el MRUnificarObjetos para abrir su editor completo
                Selection.activeGameObject = _targetRadialMenu.gameObject;
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5f);
        }
        
        
        
        /// <summary>
        /// Aplica el frame actual usando la lógica existente del MRUnificarObjetos
        /// NUEVA VERSIÓN: Con markeo automático como dirty y mejor logging
        /// </summary>
        private void ApplyFramePreview()
        {
            if (_targetRadialMenu == null || _targetRadialMenu.ActiveFrame == null)
                return;
                
            // Aplicar frame usando la lógica existente de MRUnificarObjetos
            _targetRadialMenu.ApplyCurrentFrame();
            
            
            // Marcar como dirty para asegurar que Unity reconozca los cambios
            UnityEditor.EditorUtility.SetDirty(_targetRadialMenu);
            
            // Marcar todos los frame objects como dirty también
            if (_targetRadialMenu.ActiveFrame != null)
            {
                UnityEditor.EditorUtility.SetDirty(_targetRadialMenu.ActiveFrame);
            }
        }
        
    }
}
