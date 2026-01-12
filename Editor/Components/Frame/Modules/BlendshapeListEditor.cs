using UnityEngine;
using UnityEditor;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Editor.Components.Frame;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame.Modules
{
    /// <summary>
    /// Módulo especializado en la gestión de blendshapes del frame
    /// Responsabilidad única: UI para BlendshapeReference
    /// </summary>
    public class BlendshapeListEditor
    {
        private readonly MRAgruparObjetos _target;
        
        // Altura compacta de una fila
        private static readonly float ROW_H = EditorGUIUtility.singleLineHeight;
        
        // Separación entre columnas
        private const int COL_SPACING = 6;
        
        // Anchos fijos
        private const int WIDTH_RENDERER = 90;
        private const int WIDTH_BASE = 45;
        private const int WIDTH_BTN_0 = 32;
        private const int WIDTH_ACTIVE_FIELD = 45;
        private const int WIDTH_BTN_100 = 32;
        private const int WIDTH_ACTIVE_CLUSTER = WIDTH_BTN_0 + WIDTH_ACTIVE_FIELD + WIDTH_BTN_100;
        
        // Anchos mínimos reales de los dos botones de acciones
        private const int WIDTH_BTN_EYE = 24;  // botón seleccionar renderer
        private const int WIDTH_BTN_X   = 24;  // botón eliminar
        private const int ACTIONS_INNER_PADDING = 4; // separación interior mínima
        
        // El ancho mínimo que garantiza que caben 👁 y X
        private const int WIDTH_ACTIONS_MIN = WIDTH_BTN_EYE + WIDTH_BTN_X + ACTIONS_INNER_PADDING;
        
        // Mínimo para la columna flexible (Blendshape)
        private const int MIN_BLENDSHAPE = 80;
        
        /// <summary>
        /// Estilo sin márgenes ni padding para alineación perfecta al borde
        /// </summary>
        private static GUIStyle NoPad => new GUIStyle() {
            margin = new RectOffset(0,0,0,0),
            padding = new RectOffset(0,0,0,0)
        };
        
        /// <summary>
        /// Constructor que recibe el target del editor
        /// </summary>
        /// <param name="target">MRAgruparObjetos objetivo</param>
        public BlendshapeListEditor(MRAgruparObjetos target)
        {
            _target = target;
        }
        
        /// <summary>
        /// Estructura para layout de columnas
        /// </summary>
        private struct ColLayout
        {
            public float rend, blend, baseW, active, actions;
        }
        
        /// <summary>
        /// Calcula los anchos de columnas para alineación elástica
        /// </summary>
        /// <param name="totalWidth">Ancho total disponible</param>
        /// <returns>Layout de columnas con anchos calculados</returns>
        private static ColLayout CalcCols(float totalWidth)
        {
            // Suma de columnas no elásticas + separadores
            float fixedNoActions = WIDTH_RENDERER + WIDTH_BASE + WIDTH_ACTIVE_CLUSTER + (4 * COL_SPACING);

            // La columna flexible principal sigue siendo "Blendshape"
            float blend = Mathf.Max(MIN_BLENDSHAPE, totalWidth - fixedNoActions - WIDTH_ACTIONS_MIN);

            // El resto se lo damos a "actions" para que empuje la X al borde
            float actions = Mathf.Max(WIDTH_ACTIONS_MIN, totalWidth - (WIDTH_RENDERER + blend + WIDTH_BASE + WIDTH_ACTIVE_CLUSTER + (4 * COL_SPACING)));

            return new ColLayout {
                rend = WIDTH_RENDERER,
                blend = blend,
                baseW = WIDTH_BASE,
                active = WIDTH_ACTIVE_CLUSTER, // 0 | campo | 100
                actions = actions               // ahora elástico
            };
        }
        
        /// <summary>
        /// Dibuja la sección completa de blendshapes
        /// </summary>
        public void DrawBlendshapeSection()
        {
            if (_target == null) return;
            
            var blendshapeCount = _target.GetBlendshapeCount();
            var foldoutText = $"Blendshapes del Frame ({blendshapeCount})";
            
            _target.ShowBlendshapeList = EditorGUILayout.Foldout(_target.ShowBlendshapeList, foldoutText, EditorStyleManager.FoldoutStyle);
            
            if (_target.ShowBlendshapeList)
            {
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawBlendshapeDropArea();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawBlendshapeManagementButtons();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawBlendshapeList();
            }
        }
        
        /// <summary>
        /// Dibuja el área de drag & drop para blendshapes
        /// </summary>
        private void DrawBlendshapeDropArea()
        {
            // Crear el cuadro de drag & drop con texto dinámico
            string mainText, subText;
            
            if (_target.BlendshapeReferences.Count == 0)
            {
                mainText = "Arrastra GameObjects aquí";
                subText = "Objetos con SkinnedMeshRenderer y blendshapes";
            }
            else
            {
                mainText = "Arrastra más objetos";
                subText = "Añadir blendshapes al frame";
            }
            
            // Crear rect para el área de drop
            var dropRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            
            // Dibujar el fondo del área
            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            
            // Dibujar el cuadro con texto
            GUI.Box(dropRect, $"{mainText}\n{subText}", boxStyle);
            
            // Manejar drag & drop
            HandleBlendshapeDragAndDrop(dropRect);
        }
        
        /// <summary>
        /// Maneja el drag & drop de SkinnedMeshRenderers
        /// </summary>
        /// <param name="dropArea">Área de drop</param>
        private void HandleBlendshapeDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    bool canAccept = DragAndDrop.objectReferences.OfType<GameObject>()
                        .Any(go => go.GetComponent<SkinnedMeshRenderer>() != null && 
                                   go.GetComponent<SkinnedMeshRenderer>().sharedMesh != null &&
                                   go.GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0);
                    DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var obj in DragAndDrop.objectReferences.OfType<GameObject>())
                    {
                        var skinnedRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null && 
                            skinnedRenderer.sharedMesh.blendShapeCount > 0)
                        {
                            // Mostrar ventana de selección de blendshapes
                            ShowBlendshapeSelectionWindow(skinnedRenderer);
                        }
                    }
                    
                    currentEvent.Use();
                    EditorUtility.SetDirty(_target);
                }
            }
        }
        
        /// <summary>
        /// Muestra la ventana de selección de blendshapes
        /// </summary>
        /// <param name="renderer">SkinnedMeshRenderer objetivo</param>
        private void ShowBlendshapeSelectionWindow(SkinnedMeshRenderer renderer)
        {
            BlendshapeSelectionWindow.ShowWindow(renderer, _target);
        }
        
        /// <summary>
        /// Dibuja los botones de gestión de blendshapes
        /// </summary>
        private void DrawBlendshapeManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Botones normales
            EditorStyleManager.DrawManagementButtons(
                ("Capturar Valores", () => {
                    _target.CaptureAllBlendshapeValues();
                    EditorUtility.SetDirty(_target);
                }),
                ("Recalcular Rutas", () => {
                    _target.UpdateAllBlendshapeRendererPaths();
                    EditorUtility.SetDirty(_target);
                }),
                ("Limpiar Inválidos", () => {
                    _target.RemoveInvalidBlendshapeReferences();
                    EditorUtility.SetDirty(_target);
                })
            );
            
            // Botón de limpiar todos (con color rojo)
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("Limpiar Todos", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog("Confirmar", 
                        "¿Estás seguro de que quieres eliminar todos los blendshapes del frame?", 
                        "Sí", "Cancelar"))
                    {
                        _target.ClearBlendshapes();
                        EditorUtility.SetDirty(_target);
                    }
                }
            });
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Dibuja la lista de blendshapes
        /// </summary>
        private void DrawBlendshapeList()
        {
            if (_target.BlendshapeReferences.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay blendshapes en este frame. Arrastra GameObjects con SkinnedMeshRenderer al área superior para añadirlos.", MessageType.Info);
                return;
            }
            
            // Headers elásticos - Base: valor actual en escena (solo lectura), Activo: 0 | campo | 100 — valor objetivo que se guardará/aplicará
            using (new EditorGUILayout.HorizontalScope(NoPad, GUILayout.Height(ROW_H)))
            {
                var total = EditorGUIUtility.currentViewWidth - 20f; // padding reducido para máxima alineación
                var cols = CalcCols(total);

                GUILayout.Space(0);
                EditorGUILayout.LabelField("Renderer", EditorStyles.boldLabel, GUILayout.Width(cols.rend));
                EditorGUILayout.LabelField("Blendshape", EditorStyles.boldLabel, GUILayout.Width(cols.blend)); // flexible
                EditorGUILayout.LabelField("Base", EditorStyles.boldLabel, GUILayout.Width(cols.baseW));       // solo lectura
                EditorGUILayout.LabelField("Activo", EditorStyles.boldLabel, GUILayout.Width(cols.active));    // 0 | campo | 100
                EditorGUILayout.LabelField("", EditorStyles.boldLabel, GUILayout.Width(cols.actions));         // acciones
            }
            
            // Línea separadora
            var sep = GUILayoutUtility.GetRect(0, 1);
            EditorGUI.DrawRect(sep, Color.gray);
            
            // Lista de blendshapes
            for (int i = 0; i < _target.BlendshapeReferences.Count; i++)
            {
                if (DrawBlendshapeReference(i))
                {
                    // Si retorna true, el blendshape fue eliminado, ajustar índice
                    i--;
                }
            }
        }
        
        /// <summary>
        /// Dibuja una referencia de blendshape individual - MEJORADO
        /// </summary>
        /// <param name="index">Índice del blendshape</param>
        /// <returns>True si el blendshape fue eliminado</returns>
        private bool DrawBlendshapeReference(int index)
        {
            var blendRef = _target.BlendshapeReferences[index];
            bool shouldRemove = false;
            
            using (new EditorGUILayout.HorizontalScope(NoPad, GUILayout.Height(ROW_H)))
            {
                var total = EditorGUIUtility.currentViewWidth - 20f; // Reducido para eliminar más padding
                var cols = CalcCols(total);

                // Renderer
                string rendererName = blendRef.TargetRenderer != null ? blendRef.TargetRenderer.name : "[Missing]";
                var rendererButtonStyle = new GUIStyle(EditorStyles.textField)
                {
                    normal = { textColor = blendRef.TargetRenderer != null ? Color.white : Color.red }
                };
                
                if (GUILayout.Button(rendererName, rendererButtonStyle, GUILayout.Width(cols.rend)))
                {
                    if (blendRef.TargetRenderer != null)
                    {
                        ShowBlendshapeSelectionWindow(blendRef.TargetRenderer);
                    }
                }

                // Blendshape (solo lectura)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(blendRef.BlendshapeName, GUILayout.Width(cols.blend));
                EditorGUI.EndDisabledGroup();

                // Base (solo lectura)
                EditorGUI.BeginDisabledGroup(true);
                float currentValue = blendRef.IsValid ? blendRef.GetCurrentValue() : 0f;
                EditorGUILayout.FloatField(currentValue, GUILayout.Width(cols.baseW));
                EditorGUI.EndDisabledGroup();

                // Activo (0 | campo | 100) con ancho exacto de columna y SIN padding
                var noPad = new GUIStyle { margin = new RectOffset(0,0,0,0), padding = new RectOffset(0,0,0,0) };
                EditorGUILayout.BeginHorizontal(noPad, GUILayout.Width(cols.active));
                if (GUILayout.Button("0", GUILayout.Width(WIDTH_BTN_0)))
                {
                    blendRef.Value = 0f;
                    EditorUtility.SetDirty(_target);
                    if (_target.IsPreviewActive) _target.RefreshPreview();
                }
                var newVal = EditorGUILayout.FloatField(blendRef.Value, GUILayout.Width(WIDTH_ACTIVE_FIELD));
                if (!Mathf.Approximately(newVal, blendRef.Value)) {
                    blendRef.Value = Mathf.Clamp(newVal, 0f, 100f);
                    EditorUtility.SetDirty(_target);
                    if (_target.IsPreviewActive) _target.RefreshPreview();
                }
                if (GUILayout.Button("100", GUILayout.Width(WIDTH_BTN_100)))
                {
                    blendRef.Value = 100f;
                    EditorUtility.SetDirty(_target);
                    if (_target.IsPreviewActive) _target.RefreshPreview();
                }
                EditorGUILayout.EndHorizontal();

                // Acciones (👁, X) — elástica para empujar X a la derecha
                EditorGUILayout.BeginHorizontal(NoPad, GUILayout.Width(cols.actions));
                
                // Empujar contenido hacia la derecha
                GUILayout.FlexibleSpace();
                
                // Botón de seleccionar renderer en hierarchy
                if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", "Seleccionar Renderer"))
                {
                    if (blendRef.TargetRenderer != null)
                    {
                        Selection.activeGameObject = blendRef.TargetRenderer.gameObject;
                        EditorGUIUtility.PingObject(blendRef.TargetRenderer.gameObject);
                    }
                }
                
                // Botón de eliminar
                EditorStyleManager.WithColor(Color.red, () => {
                    if (GUILayout.Button("X", GUILayout.Width(WIDTH_BTN_X), GUILayout.Height(EditorStyleManager.ICON_BUTTON_HEIGHT)))
                    {
                        shouldRemove = true;
                    }
                });
                
                EditorGUILayout.EndHorizontal();
            }
            // Mostrar ruta jerárquica si el renderer es inválido
            if (!blendRef.IsValid && !string.IsNullOrEmpty(blendRef.RendererPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(25); // Menos espacio ya que no hay ancho fijo
                EditorGUILayout.LabelField($"Última ruta conocida: {blendRef.RendererPath}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            // Procesar eliminación
            if (shouldRemove)
            {
                _target.BlendshapeReferences.RemoveAt(index);
                EditorUtility.SetDirty(_target);
                return true;
            }
            
            return false;
        }
    }
}
