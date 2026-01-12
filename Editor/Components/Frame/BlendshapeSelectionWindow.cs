using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame
{
    /// <summary>
    /// Ventana popup para seleccionar blendshapes específicos de un SkinnedMeshRenderer
    /// </summary>
    public class BlendshapeSelectionWindow : EditorWindow
    {
        private SkinnedMeshRenderer _targetRenderer;
        private MRAgruparObjetos _frameObject;
        private List<BlendshapeSelectionData> _blendshapeOptions = new List<BlendshapeSelectionData>();
        private Vector2 _scrollPosition;
        private bool _selectAll = false;
        
        // Estilos
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized = false;
        
        private const float WINDOW_WIDTH = 400f;
        private const float WINDOW_HEIGHT = 500f;
        private const float BUTTON_HEIGHT = 25f;
        
        /// <summary>
        /// Datos para cada blendshape disponible
        /// </summary>
        private class BlendshapeSelectionData
        {
            public string Name;
            public bool IsSelected;
            /// <summary>
            /// "Base": valor actual leído del renderer (solo lectura en la UI)
            /// </summary>
            public float CurrentValue;
            /// <summary>
            /// "Activo": valor objetivo que se guardará en el frame (editable en la UI)
            /// </summary>
            public float TargetValue;
            public bool AlreadyExists;
            
            public BlendshapeSelectionData(string name, float currentValue, bool alreadyExists = false)
            {
                Name = name;
                CurrentValue = currentValue;
                TargetValue = currentValue; // Inicializar con valor actual
                IsSelected = alreadyExists; // Pre-seleccionar si ya existe
                AlreadyExists = alreadyExists;
            }
        }
        
        /// <summary>
        /// Muestra la ventana de selección de blendshapes
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        /// <param name="frameObject">MRAgruparObjetos donde añadir los blendshapes</param>
        public static void ShowWindow(SkinnedMeshRenderer renderer, MRAgruparObjetos frameObject)
        {
            var window = GetWindow<BlendshapeSelectionWindow>(true, "Seleccionar Blendshapes", true);
            window.Initialize(renderer, frameObject);
            
            // Centrar ventana
            var rect = new Rect(
                (Screen.currentResolution.width - WINDOW_WIDTH) / 2,
                (Screen.currentResolution.height - WINDOW_HEIGHT) / 2,
                WINDOW_WIDTH,
                WINDOW_HEIGHT
            );
            window.position = rect;
            window.minSize = new Vector2(WINDOW_WIDTH, 300f);
            window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            
            window.Show();
        }
        
        /// <summary>
        /// Inicializa la ventana con los datos del renderer
        /// </summary>
        private void Initialize(SkinnedMeshRenderer renderer, MRAgruparObjetos frameObject)
        {
            _targetRenderer = renderer;
            _frameObject = frameObject;
            
            // Obtener todos los blendshapes disponibles
            var availableBlendshapes = FrameData.GetAvailableBlendshapes(renderer);
            var existingBlendshapes = frameObject.BlendshapeReferences
                .Where(br => br.TargetRenderer == renderer)
                .ToDictionary(br => br.BlendshapeName, br => br);
            
            _blendshapeOptions.Clear();
            
            foreach (var blendshapeName in availableBlendshapes)
            {
                float currentValue = 0f;
                bool alreadyExists = existingBlendshapes.ContainsKey(blendshapeName);
                
                // Obtener valor actual del blendshape
                var blendshapeIndex = GetBlendshapeIndex(renderer, blendshapeName);
                if (blendshapeIndex >= 0)
                {
                    currentValue = renderer.GetBlendShapeWeight(blendshapeIndex);
                }
                
                var selectionData = new BlendshapeSelectionData(blendshapeName, currentValue, alreadyExists);
                
                // Si ya existe, usar el valor configurado
                if (alreadyExists)
                {
                    selectionData.TargetValue = existingBlendshapes[blendshapeName].Value;
                }
                
                _blendshapeOptions.Add(selectionData);
            }
            
            // Verificar si todos están seleccionados
            UpdateSelectAllState();
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            if (_targetRenderer == null || _frameObject == null)
            {
                EditorGUILayout.HelpBox("Error: Referencias inválidas. Cerrando ventana.", MessageType.Error);
                Close();
                return;
            }
            
            DrawHeader();
            DrawSelectAllToggle();
            DrawBlendshapeList();
            DrawButtons();
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = BUTTON_HEIGHT
            };
            
            _stylesInitialized = true;
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Seleccionar Blendshapes", _headerStyle);
            EditorGUILayout.LabelField("Renderer: " + _targetRenderer.name, EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "Selecciona los blendshapes que quieres añadir al frame. " +
                "Los valores se inicializarán con los valores actuales de la escena.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawSelectAllToggle()
        {
            EditorGUILayout.BeginHorizontal();
            
            var newSelectAll = EditorGUILayout.Toggle("Seleccionar Todos", _selectAll);
            if (newSelectAll != _selectAll)
            {
                _selectAll = newSelectAll;
                foreach (var option in _blendshapeOptions)
                {
                    option.IsSelected = _selectAll;
                }
            }
            
            // Mostrar contador
            var selectedCount = _blendshapeOptions.Count(o => o.IsSelected);
            EditorGUILayout.LabelField($"({selectedCount}/{_blendshapeOptions.Count})", GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private void DrawBlendshapeList()
        {
            if (_blendshapeOptions.Count == 0)
            {
                EditorGUILayout.HelpBox("No se encontraron blendshapes en este renderer.", MessageType.Warning);
                return;
            }
            
            // Headers
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✓", EditorStyles.boldLabel, GUILayout.Width(20));
            EditorGUILayout.LabelField("Nombre", EditorStyles.boldLabel, GUILayout.Width(150));
            // Columna "Base": valor actual leído del renderer
            EditorGUILayout.LabelField("Base", EditorStyles.boldLabel, GUILayout.Width(50));
            // Columna "Activo": valor que se guardará en el frame
            EditorGUILayout.LabelField("Activo", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Estado", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            // Separador
            var rect = GUILayoutUtility.GetRect(0, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            
            EditorGUILayout.Space(2);
            
            // Lista con scroll
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            
            for (int i = 0; i < _blendshapeOptions.Count; i++)
            {
                DrawBlendshapeOption(_blendshapeOptions[i]);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawBlendshapeOption(BlendshapeSelectionData option)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Checkbox
            var newSelected = EditorGUILayout.Toggle(option.IsSelected, GUILayout.Width(20));
            if (newSelected != option.IsSelected)
            {
                option.IsSelected = newSelected;
                UpdateSelectAllState();
            }
            
            // Nombre del blendshape
            var style = option.AlreadyExists ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUI.BeginDisabledGroup(!option.IsSelected);
            EditorGUILayout.LabelField(option.Name, style, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();
            
            // Columna "Base": valor actual leído del renderer (solo lectura)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField(option.CurrentValue, GUILayout.Width(50));
            EditorGUI.EndDisabledGroup();
            
            // Columna "Activo": valor que se guardará en el frame (editable si está seleccionado)
            EditorGUI.BeginDisabledGroup(!option.IsSelected);
            var newValue = EditorGUILayout.FloatField(option.TargetValue, GUILayout.Width(60));
            if (newValue != option.TargetValue)
            {
                option.TargetValue = Mathf.Clamp(newValue, 0f, 100f);
            }
            EditorGUI.EndDisabledGroup();
            
            // Estado
            string statusText = option.AlreadyExists ? "Ya existe" : "Nuevo";
            var statusStyle = option.AlreadyExists ? EditorStyles.helpBox : EditorStyles.miniLabel;
            EditorGUILayout.LabelField(statusText, statusStyle);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawButtons()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            // Botón Capturar Valores Actuales
            if (GUILayout.Button("Capturar Valores Actuales", _buttonStyle))
            {
                CaptureCurrentValues();
            }
            
            EditorGUILayout.Space(10);
            
            // Botón Cancelar
            if (GUILayout.Button("Cancelar", _buttonStyle))
            {
                Close();
            }
            
            // Botón Aplicar
            GUI.backgroundColor = Color.green;
            var selectedCount = _blendshapeOptions.Count(o => o.IsSelected);
            var buttonText = selectedCount > 0 ? $"Añadir ({selectedCount})" : "Añadir";
            
            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            if (GUILayout.Button(buttonText, _buttonStyle))
            {
                ApplySelection();
                Close();
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Captura los valores actuales de todos los blendshapes seleccionados
        /// </summary>
        private void CaptureCurrentValues()
        {
            foreach (var option in _blendshapeOptions.Where(o => o.IsSelected))
            {
                var blendshapeIndex = GetBlendshapeIndex(_targetRenderer, option.Name);
                if (blendshapeIndex >= 0)
                {
                    option.TargetValue = _targetRenderer.GetBlendShapeWeight(blendshapeIndex);
                    option.CurrentValue = option.TargetValue; // Actualizar también el valor actual mostrado
                }
            }
            
        }
        
        /// <summary>
        /// Aplica la selección al MRAgruparObjetos
        /// </summary>
        private void ApplySelection()
        {
            var selectedOptions = _blendshapeOptions.Where(o => o.IsSelected).ToList();

            if (selectedOptions.Count == 0)
            {
                return;
            }

            int successCount = 0;
            var failedBlendshapes = new List<string>();

            // Añadir o actualizar blendshapes seleccionados
            foreach (var option in selectedOptions)
            {
                bool success = _frameObject.AddBlendshapeReference(_targetRenderer, option.Name, option.TargetValue);
                if (success)
                {
                    successCount++;
                }
                else
                {
                    failedBlendshapes.Add(option.Name);
                }
            }

            // Marcar como dirty para que Unity guarde los cambios
            EditorUtility.SetDirty(_frameObject);

            // Mostrar resultado al usuario
            if (failedBlendshapes.Count > 0)
            {
                string failedList = string.Join(", ", failedBlendshapes);
                EditorUtility.DisplayDialog(
                    "Blendshapes - Resultado",
                    $"Se añadieron {successCount} de {selectedOptions.Count} blendshapes.\n\n" +
                    $"No se pudieron añadir:\n{failedList}\n\n" +
                    "Revisa la consola para más detalles.",
                    "OK"
                );
            }
        }
        
        /// <summary>
        /// Actualiza el estado del toggle "Seleccionar Todos"
        /// </summary>
        private void UpdateSelectAllState()
        {
            if (_blendshapeOptions.Count == 0)
            {
                _selectAll = false;
                return;
            }
            
            _selectAll = _blendshapeOptions.All(o => o.IsSelected);
        }
        
        /// <summary>
        /// Obtiene el índice de un blendshape por nombre
        /// </summary>
        private int GetBlendshapeIndex(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || renderer.sharedMesh == null) return -1;
            
            var mesh = renderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i) == blendshapeName)
                    return i;
            }
            
            return -1;
        }
    }
}