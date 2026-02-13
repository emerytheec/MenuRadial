using UnityEngine;
using UnityEditor;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame.Modules
{
    /// <summary>
    /// Módulo especializado en la gestión de materiales del frame
    /// Responsabilidad única: UI para MaterialReference
    /// </summary>
    public class MaterialListEditor
    {
        private readonly MRAgruparObjetos _target;
        
        /// <summary>
        /// Constructor que recibe el target del editor
        /// </summary>
        /// <param name="target">MRAgruparObjetos objetivo</param>
        public MaterialListEditor(MRAgruparObjetos target)
        {
            _target = target;
        }
        
        /// <summary>
        /// Dibuja la sección completa de materiales
        /// </summary>
        public void DrawMaterialSection()
        {
            if (_target == null) return;
            
            var materialCount = _target.GetCounts().Materials;
            var foldoutText = MRLocalization.Get(L.FrameModules.MATERIALS_FOLDOUT, materialCount);
            
            _target.ShowMaterialList = EditorGUILayout.Foldout(_target.ShowMaterialList, foldoutText, EditorStyleManager.FoldoutStyle);
            
            if (_target.ShowMaterialList)
            {
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawMaterialDropArea();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawMaterialManagementButtons();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawMaterialList();
            }
        }
        
        /// <summary>
        /// Dibuja el área de drag & drop para materiales
        /// </summary>
        private void DrawMaterialDropArea()
        {
            // Crear el cuadro de drag & drop con texto dinámico
            string mainText, subText;
            
            if (_target.MaterialReferences.Count == 0)
            {
                mainText = MRLocalization.Get(L.FrameModules.DROP_MATERIALS_MAIN);
                subText = MRLocalization.Get(L.FrameModules.DROP_MATERIALS_SUB);
            }
            else
            {
                mainText = MRLocalization.Get(L.FrameModules.DROP_MORE_MATERIALS_MAIN);
                subText = MRLocalization.Get(L.FrameModules.DROP_MORE_MATERIALS_SUB);
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
            HandleMaterialDragAndDrop(dropRect);
        }
        
        /// <summary>
        /// Maneja el drag & drop de renderers para materiales
        /// </summary>
        /// <param name="dropArea">Área de drop</param>
        private void HandleMaterialDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    bool canAccept = DragAndDrop.objectReferences.OfType<GameObject>()
                        .Any(go => go.GetComponent<Renderer>() != null);
                    DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var obj in DragAndDrop.objectReferences.OfType<GameObject>())
                    {
                        var renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            // Añadir cada material del renderer
                            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                            {
                                _target.AddMaterialReference(renderer, i, null);
                            }
                        }
                    }
                    
                    currentEvent.Use();
                    EditorUtility.SetDirty(_target);
                }
            }
        }
        
        /// <summary>
        /// Dibuja los botones de gestión de materiales
        /// </summary>
        private void DrawMaterialManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Botones normales
            EditorStyleManager.DrawManagementButtons(
                (MRLocalization.Get(L.FrameModules.UPDATE_ORIGINALS), () => {
                    _target.UpdateAllOriginalMaterials();
                    EditorUtility.SetDirty(_target);
                }),
                (MRLocalization.Get(L.FrameModules.RECALCULATE_PATHS), () => {
                    _target.UpdateAllMaterialRendererPaths();
                    EditorUtility.SetDirty(_target);
                }),
                (MRLocalization.Get(L.FrameModules.CLEAN_INVALIDS), () => {
                    _target.RemoveInvalidMaterialReferences();
                    EditorUtility.SetDirty(_target);
                })
            );
            
            // Botón de limpiar todos (con color rojo)
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button(MRLocalization.Get(L.FrameModules.CLEAN_ALL), GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.CONFIRM),
                        MRLocalization.Get(L.FrameModules.CLEAN_ALL_MATERIALS_CONFIRM),
                        MRLocalization.Get(L.Common.YES), MRLocalization.Get(L.Common.CANCEL)))
                    {
                        _target.ClearMaterials();
                        EditorUtility.SetDirty(_target);
                    }
                }
            });
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Dibuja la lista de materiales
        /// </summary>
        private void DrawMaterialList()
        {
            if (_target.MaterialReferences.Count == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.FrameModules.NO_MATERIALS_HINT), MessageType.Info);
                return;
            }
            
            // Headers de tabla
            EditorStyleManager.DrawTableHeader(
                (MRLocalization.Get(L.FrameModules.COL_RENDERER), 120),
                (MRLocalization.Get(L.FrameModules.COL_IDX), 30),
                (MRLocalization.Get(L.FrameModules.COL_BASE), 80),
                (MRLocalization.Get(L.FrameModules.COL_ACTIVE_MAT), 0),
                ("", 60)
            );
            
            // Lista de materiales
            for (int i = 0; i < _target.MaterialReferences.Count; i++)
            {
                if (DrawMaterialReference(i))
                {
                    // Si retorna true, el material fue eliminado, ajustar índice
                    i--;
                }
            }
        }
        
        /// <summary>
        /// Dibuja una referencia de material individual
        /// </summary>
        /// <param name="index">Índice del material</param>
        /// <returns>True si el material fue eliminado</returns>
        private bool DrawMaterialReference(int index)
        {
            var matRef = _target.MaterialReferences[index];
            
            EditorGUILayout.BeginHorizontal();
            
            // Campo de renderer (solo lectura, mostramos el nombre)
            string rendererName = matRef.TargetRenderer != null ? matRef.TargetRenderer.name : MRLocalization.Get(L.FrameModules.NO_RENDERER);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(rendererName, GUILayout.Width(120));
            EditorGUI.EndDisabledGroup();
            
            // Índice del material (solo lectura)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(matRef.MaterialIndex, GUILayout.Width(30));
            EditorGUI.EndDisabledGroup();
            
            // Material base (CLICKEABLE para seleccionar en Project)
            string originalMatName = matRef.OriginalMaterial != null ? matRef.OriginalMaterial.name : MRLocalization.Get(L.FrameModules.NO_NONE);
            
            // Crear estilo para botón que parece campo de texto
            var materialButtonStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = matRef.OriginalMaterial != null ? Color.white : Color.gray }
            };
            
            // Campo de material base como botón clickeable
            if (GUILayout.Button(originalMatName, materialButtonStyle, GUILayout.Width(80)))
            {
                if (matRef.OriginalMaterial != null)
                {
                    // Seleccionar y resaltar el material en el Project window
                    Selection.activeObject = matRef.OriginalMaterial;
                    EditorGUIUtility.PingObject(matRef.OriginalMaterial);
                }
            }
            
            // Campo de material alternativo (editable)
            var newAltMat = (Material)EditorGUILayout.ObjectField(matRef.AlternativeMaterial, typeof(Material), false);
            if (newAltMat != matRef.AlternativeMaterial)
            {
                matRef.AlternativeMaterial = newAltMat;
                EditorUtility.SetDirty(_target);
                // Refrescar preview si está activo para mostrar el cambio inmediatamente
                if (_target.IsPreviewActive)
                {
                    _target.RefreshPreview();
                }
            }
            
            // Botón de seleccionar renderer en hierarchy
            if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", MRLocalization.Get(L.FrameModules.SELECT_RENDERER)))
            {
                if (matRef.TargetRenderer != null)
                {
                    Selection.activeGameObject = matRef.TargetRenderer.gameObject;
                    EditorGUIUtility.PingObject(matRef.TargetRenderer.gameObject);
                }
            }
            
            // Botón de eliminar
            bool shouldRemove = false;
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("X", GUILayout.Width(EditorStyleManager.ICON_BUTTON_WIDTH), GUILayout.Height(EditorStyleManager.ICON_BUTTON_HEIGHT)))
                {
                    shouldRemove = true;
                }
            });
            
            EditorGUILayout.EndHorizontal();
            
            // Mostrar ruta jerárquica si el renderer es inválido
            if (!matRef.IsValid && !string.IsNullOrEmpty(matRef.RendererPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(125); // Alinear con el campo de renderer
                EditorGUILayout.LabelField(MRLocalization.Get(L.FrameModules.LAST_KNOWN_PATH, matRef.RendererPath), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            // Procesar eliminación
            if (shouldRemove)
            {
                _target.MaterialReferences.RemoveAt(index);
                EditorUtility.SetDirty(_target);
                return true;
            }
            
            return false;
        }
    }
}
