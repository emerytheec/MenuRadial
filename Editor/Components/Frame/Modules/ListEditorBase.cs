using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame.Modules
{
    /// <summary>
    /// Clase base genérica para editores de listas
    /// FASE 2: Unifica lógica duplicada entre ObjectListEditor, MaterialListEditor y BlendshapeListEditor
    /// ANTES: 614 líneas duplicadas | DESPUÉS: ~400 líneas unificadas | AHORRO: 35%
    /// </summary>
    /// <typeparam name="T">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    public abstract class ListEditorBase<T>
    {
        protected readonly UnityEngine.Object _target;
        
        /// <summary>
        /// Constructor base
        /// </summary>
        /// <param name="target">Target del editor</param>
        protected ListEditorBase(UnityEngine.Object target)
        {
            _target = target;
        }
        
        
        /// <summary>
        /// Obtiene la lista de referencias del target
        /// </summary>
        protected abstract List<T> GetReferenceList();
        
        /// <summary>
        /// Obtiene/establece la visibilidad del foldout
        /// </summary>
        protected abstract bool GetFoldoutState();
        protected abstract void SetFoldoutState(bool state);
        
        /// <summary>
        /// Obtiene el nombre del tipo para mostrar en UI
        /// </summary>
        protected abstract string GetTypeName();
        
        /// <summary>
        /// Obtiene el ícono para el área de drop
        /// </summary>
        protected abstract string GetDropAreaIcon();
        
        /// <summary>
        /// Obtiene el texto del área de drop
        /// </summary>
        protected abstract string GetDropAreaText();
        
        /// <summary>
        /// Obtiene el subtexto del área de drop
        /// </summary>
        protected abstract string GetDropAreaSubtext();
        
        /// <summary>
        /// Valida si un objeto puede ser aceptado en drag & drop
        /// </summary>
        protected abstract bool ValidateDroppedObject(UnityEngine.Object obj);
        
        /// <summary>
        /// Procesa un objeto válido en drag & drop
        /// </summary>
        protected abstract void ProcessDroppedObject(UnityEngine.Object obj);
        
        /// <summary>
        /// Obtiene los botones de gestión específicos del tipo
        /// </summary>
        protected abstract (string name, Action action)[] GetManagementButtons();
        
        /// <summary>
        /// Obtiene las columnas de la tabla
        /// </summary>
        protected abstract (string name, float width)[] GetTableColumns();
        
        /// <summary>
        /// Dibuja una fila de referencia individual
        /// </summary>
        /// <param name="reference">Referencia a dibujar</param>
        /// <param name="index">Índice en la lista</param>
        /// <returns>True si la referencia fue eliminada</returns>
        protected abstract bool DrawReferenceRow(T reference, int index);
        
        /// <summary>
        /// Obtiene el mensaje de ayuda cuando la lista está vacía
        /// </summary>
        protected abstract string GetEmptyListMessage();
        
        
        
        /// <summary>
        /// Dibuja la sección completa del editor
        /// Método principal que debe ser llamado desde el editor principal
        /// </summary>
        public void DrawSection()
        {
            var references = GetReferenceList();
            var count = references?.Count ?? 0;
            var foldoutText = $"{GetTypeName()} ({count})";
            
            var newFoldoutState = EditorGUILayout.Foldout(GetFoldoutState(), foldoutText, EditorStyleManager.FoldoutStyle);
            SetFoldoutState(newFoldoutState);
            
            if (GetFoldoutState())
            {
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawDropArea();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawManagementButtons();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawReferenceList();
            }
        }
        
        
        
        /// <summary>
        /// Dibuja el área de drag & drop unificada
        /// </summary>
        protected void DrawDropArea()
        {
            var dropAreaRect = EditorStyleManager.DrawDropArea(
                GetDropAreaIcon(),
                GetDropAreaText(),
                GetDropAreaSubtext()
            );
            
            HandleDragAndDrop(dropAreaRect);
        }
        
        /// <summary>
        /// Maneja el drag & drop unificado
        /// </summary>
        /// <param name="dropArea">Área de drop</param>
        protected void HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    bool canAccept = DragAndDrop.objectReferences.Any(ValidateDroppedObject);
                    DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (ValidateDroppedObject(obj))
                        {
                            ProcessDroppedObject(obj);
                        }
                    }
                    
                    currentEvent.Use();
                    EditorUtility.SetDirty(_target);
                }
            }
        }
        
        /// <summary>
        /// Dibuja los botones de gestión unificados
        /// </summary>
        protected void DrawManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Botones específicos del tipo
            var buttons = GetManagementButtons();
            if (buttons != null)
            {
                foreach (var (name, action) in buttons)
                {
                    if (GUILayout.Button(name, GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                    {
                        action?.Invoke();
                        EditorUtility.SetDirty(_target);
                    }
                }
            }
            
            // Botón de limpiar todos (común a todos)
            DrawClearAllButton();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Dibuja el botón de limpiar todos (común)
        /// </summary>
        protected void DrawClearAllButton()
        {
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("Limpiar Todos", GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog("Confirmar", 
                        $"¿Estás seguro de que quieres eliminar todos los {GetTypeName().ToLower()}?", 
                        "Sí", "Cancelar"))
                    {
                        ClearAllReferences();
                        EditorUtility.SetDirty(_target);
                    }
                }
            });
        }
        
        /// <summary>
        /// Dibuja la lista de referencias unificada
        /// </summary>
        protected void DrawReferenceList()
        {
            var references = GetReferenceList();
            
            if (references == null || references.Count == 0)
            {
                EditorGUILayout.HelpBox(GetEmptyListMessage(), MessageType.Info);
                return;
            }
            
            // Headers de tabla
            var columns = GetTableColumns();
            if (columns != null && columns.Length > 0)
            {
                EditorStyleManager.DrawTableHeader(columns);
            }
            
            // Lista de referencias
            for (int i = references.Count - 1; i >= 0; i--)
            {
                if (DrawReferenceRow(references[i], i))
                {
                    references.RemoveAt(i);
                    EditorUtility.SetDirty(_target);
                }
            }
        }
        
        
        
        /// <summary>
        /// Limpia todas las referencias (implementación por defecto)
        /// </summary>
        protected virtual void ClearAllReferences()
        {
            var references = GetReferenceList();
            references?.Clear();
        }
        
        /// <summary>
        /// Dibuja botón de eliminar estándar
        /// </summary>
        /// <returns>True si se debe eliminar la referencia</returns>
        protected virtual bool DrawDeleteButton()
        {
            bool shouldRemove = false;
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("X", GUILayout.Width(EditorStyleManager.ICON_BUTTON_WIDTH), GUILayout.Height(EditorStyleManager.ICON_BUTTON_HEIGHT)))
                {
                    shouldRemove = true;
                }
            });
            return shouldRemove;
        }
        
        /// <summary>
        /// Dibuja botón de seleccionar objeto estándar
        /// </summary>
        /// <param name="target">Objeto a seleccionar</param>
        /// <returns>True si se hizo clic</returns>
        protected virtual bool DrawSelectButton(UnityEngine.Object target)
        {
            if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", "Seleccionar en Hierarchy"))
            {
                if (target != null)
                {
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                }
                return true;
            }
            return false;
        }
        
        
        
        
        /// <summary>
        /// Convierte GameObject a componente específico
        /// </summary>
        /// <typeparam name="TComponent">Tipo de componente</typeparam>
        /// <param name="gameObject">GameObject a convertir</param>
        /// <returns>Componente o null</returns>
        protected static TComponent GetComponentFromGameObject<TComponent>(GameObject gameObject) 
            where TComponent : Component
        {
            return gameObject?.GetComponent<TComponent>();
        }
        
        /// <summary>
        /// Valida si un GameObject tiene un componente específico
        /// </summary>
        /// <typeparam name="TComponent">Tipo de componente</typeparam>
        /// <param name="obj">Objeto a validar</param>
        /// <returns>True si tiene el componente</returns>
        protected static bool HasComponent<TComponent>(UnityEngine.Object obj) 
            where TComponent : Component
        {
            if (obj is GameObject go)
                return go.GetComponent<TComponent>() != null;
            return false;
        }
        
    }
}
