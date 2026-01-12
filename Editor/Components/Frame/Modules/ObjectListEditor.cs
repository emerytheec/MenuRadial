using UnityEngine;
using UnityEditor;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame.Modules
{
    /// <summary>
    /// Módulo especializado en la gestión de objetos del frame
    /// Responsabilidad única: UI para ObjectReference
    /// </summary>
    public class ObjectListEditor
    {
        private readonly MRAgruparObjetos _target;
        
        /// <summary>
        /// Constructor que recibe el target del editor
        /// </summary>
        /// <param name="target">MRAgruparObjetos objetivo</param>
        public ObjectListEditor(MRAgruparObjetos target)
        {
            _target = target;
        }
        
        /// <summary>
        /// Dibuja la sección completa de objetos
        /// </summary>
        public void DrawObjectSection()
        {
            if (_target == null) return;
            
            var objectCount = _target.GetObjectCount();
            var foldoutText = $"Objetos en el Frame ({objectCount})";
            
            _target.ShowObjectList = EditorGUILayout.Foldout(_target.ShowObjectList, foldoutText, EditorStyleManager.FoldoutStyle);
            
            if (_target.ShowObjectList)
            {
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawObjectDropArea();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawObjectManagementButtons();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawObjectList();
            }
        }
        
        /// <summary>
        /// Dibuja el área de drag & drop para objetos
        /// </summary>
        private void DrawObjectDropArea()
        {
            // Crear el cuadro de drag & drop con texto dinámico
            string mainText, subText;
            
            if (_target.ObjectReferences.Count == 0)
            {
                mainText = "Arrastra GameObjects aquí";
                subText = "Captura sus estados para el frame";
            }
            else
            {
                mainText = "Arrastra más objetos";
                subText = "Añadir al frame actual";
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
            HandleObjectDragAndDrop(dropRect);
        }
        
        /// <summary>
        /// Maneja el drag & drop de GameObjects
        /// </summary>
        /// <param name="dropArea">Área de drop</param>
        private void HandleObjectDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    bool canAccept = DragAndDrop.objectReferences.OfType<GameObject>().Any();
                    DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var obj in DragAndDrop.objectReferences.OfType<GameObject>())
                    {
                        // Capturar el estado actual del objeto en la escena
                        _target.AddGameObject(obj, obj.activeSelf);
                    }
                    
                    currentEvent.Use();
                    EditorUtility.SetDirty(_target);
                }
            }
        }
        
        /// <summary>
        /// Dibuja los botones de gestión de objetos
        /// </summary>
        private void DrawObjectManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Botones normales
            if (GUILayout.Button("Seleccionar Todo", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                _target.SelectAllObjects();
                EditorUtility.SetDirty(_target);
                if (_target.IsPreviewActive) _target.RefreshPreview();
            }

            if (GUILayout.Button("Deseleccionar Todo", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                _target.DeselectAllObjects();
                EditorUtility.SetDirty(_target);
                if (_target.IsPreviewActive) _target.RefreshPreview();
            }
            
            if (GUILayout.Button("Recalcular Rutas", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                _target.RecalculatePaths();
                EditorUtility.SetDirty(_target);
            }
            
            // Botón de eliminar todos (con color rojo)
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("Eliminar Todos", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog("Confirmar", 
                        "¿Estás seguro de que quieres eliminar todos los objetos del frame?", 
                        "Sí", "Cancelar"))
                    {
                        _target.ClearAllObjects();
                        EditorUtility.SetDirty(_target);
                    }
                }
            });
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Dibuja la lista de objetos
        /// </summary>
        private void DrawObjectList()
        {
            if (_target.ObjectReferences.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay objetos en este frame. Arrastra objetos al área superior para añadirlos.", MessageType.Info);
                return;
            }
            
            // Headers de tabla
            EditorStyleManager.DrawTableHeader(
                ("Activo", 50),
                ("Objeto", 0),
                ("", 60) // Espacio para botones
            );
            
            // Lista de objetos
            for (int i = 0; i < _target.ObjectReferences.Count; i++)
            {
                if (DrawObjectReference(i))
                {
                    // Si retorna true, el objeto fue eliminado, ajustar índice
                    i--;
                }
            }
        }
        
        /// <summary>
        /// Dibuja una referencia de objeto individual
        /// </summary>
        /// <param name="index">Índice del objeto</param>
        /// <returns>True si el objeto fue eliminado</returns>
        private bool DrawObjectReference(int index)
        {
            var objRef = _target.ObjectReferences[index];
            
            EditorGUILayout.BeginHorizontal();
            
            // Checkbox para activo/inactivo
            var newIsActive = EditorGUILayout.Toggle(objRef.IsActive, GUILayout.Width(50));
            if (newIsActive != objRef.IsActive)
            {
                objRef.IsActive = newIsActive;
                EditorUtility.SetDirty(_target);
                // Refrescar preview si está activo para mostrar el cambio inmediatamente
                if (_target.IsPreviewActive)
                {
                    _target.RefreshPreview();
                }
            }
            
            // Campo de objeto
            var newObj = (GameObject)EditorGUILayout.ObjectField(objRef.GameObject, typeof(GameObject), true);
            if (newObj != objRef.GameObject)
            {
                objRef.GameObject = newObj;
                EditorUtility.SetDirty(_target);
            }
            
            // Botón de seleccionar en hierarchy
            if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", "Seleccionar en Hierarchy"))
            {
                if (objRef.GameObject != null)
                {
                    Selection.activeGameObject = objRef.GameObject;
                    EditorGUIUtility.PingObject(objRef.GameObject);
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
            
            // Mostrar ruta jerárquica si el objeto es inválido
            if (!objRef.IsValid && !string.IsNullOrEmpty(objRef.HierarchyPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(55); // Alinear con el campo de objeto
                EditorGUILayout.LabelField($"Última ruta conocida: {objRef.HierarchyPath}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            
            // Procesar eliminación
            if (shouldRemove)
            {
                _target.ObjectReferences.RemoveAt(index);
                EditorUtility.SetDirty(_target);
                return true;
            }
            
            return false;
        }
    }
}
