using UnityEngine;
using UnityEditor;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;

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
            
            var materialCount = _target.GetMaterialCount();
            var foldoutText = $"Materiales del Frame ({materialCount})";
            
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
                mainText = "Arrastra GameObjects aquí";
                subText = "Objetos con Renderer o SkinnedMeshRenderer";
            }
            else
            {
                mainText = "Arrastra más objetos";
                subText = "Añadir materiales al frame";
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
                ("Actualizar Originales", () => {
                    _target.UpdateAllOriginalMaterials();
                    EditorUtility.SetDirty(_target);
                }),
                ("Recalcular Rutas", () => {
                    _target.UpdateAllMaterialRendererPaths();
                    EditorUtility.SetDirty(_target);
                }),
                ("Limpiar Inválidos", () => {
                    _target.RemoveInvalidMaterialReferences();
                    EditorUtility.SetDirty(_target);
                })
            );
            
            // Botón de limpiar todos (con color rojo)
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("Limpiar Todos", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog("Confirmar", 
                        "¿Estás seguro de que quieres eliminar todos los materiales del frame?", 
                        "Sí", "Cancelar"))
                    {
                        _target.ClearAllMaterials();
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
                EditorGUILayout.HelpBox("No hay materiales en este frame. Arrastra GameObjects con Renderer al área superior para añadirlos.", MessageType.Info);
                return;
            }
            
            // Headers de tabla
            EditorStyleManager.DrawTableHeader(
                ("Renderer", 120),
                ("Idx", 30),
                ("Base", 80),
                ("Activo", 0),
                ("", 60) // Espacio para botones
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
            string rendererName = matRef.TargetRenderer != null ? matRef.TargetRenderer.name : "[Missing]";
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(rendererName, GUILayout.Width(120));
            EditorGUI.EndDisabledGroup();
            
            // Índice del material (solo lectura)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(matRef.MaterialIndex, GUILayout.Width(30));
            EditorGUI.EndDisabledGroup();
            
            // Material base (CLICKEABLE para seleccionar en Project)
            string originalMatName = matRef.OriginalMaterial != null ? matRef.OriginalMaterial.name : "[None]";
            
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
            if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", "Seleccionar Renderer"))
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
                EditorGUILayout.LabelField($"Última ruta conocida: {matRef.RendererPath}", EditorStyles.miniLabel);
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
