using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Editor.Components.Radial
{
    /// <summary>
    /// Controlador especializado para la gestión de ReorderableList de frames
    /// Responsabilidad única: Manejo de drag & drop, callbacks y operaciones de lista
    /// </summary>
    public class MRUnificarObjetosReorderableController
    {
        
        private readonly MRUnificarObjetos _target;
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _framesProp;
        private readonly SerializedProperty _activeFrameIndexProp;
        private readonly MRUnificarObjetosPreviewManager _previewManager;
        
        private ReorderableList _reorderableFramesList;
        
        // Constantes de diseño
        private const float FRAME_ITEM_HEIGHT = 22f;
        
        
        
        public ReorderableList ReorderableFramesList => _reorderableFramesList;
        
        
        
        public MRUnificarObjetosReorderableController(
            MRUnificarObjetos target, 
            SerializedObject serializedObject, 
            SerializedProperty framesProp,
            SerializedProperty activeFrameIndexProp,
            MRUnificarObjetosPreviewManager previewManager)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _serializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            _framesProp = framesProp ?? throw new ArgumentNullException(nameof(framesProp));
            _activeFrameIndexProp = activeFrameIndexProp ?? throw new ArgumentNullException(nameof(activeFrameIndexProp));
            _previewManager = previewManager ?? throw new ArgumentNullException(nameof(previewManager));
            
            InitializeReorderableList();
        }
        
        
        
        /// <summary>
        /// Maneja el área de drag & drop para frames
        /// </summary>
        public void HandleFrameDropArea(Rect dropRect)
        {
            Event evt = Event.current;
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (dropRect.Contains(evt.mousePosition))
                    {
                        // Verificar si los objetos arrastrados son válidos
                        bool isValid = DragAndDrop.objectReferences.All(obj => 
                            obj is GameObject go && go.GetComponent<MRAgruparObjetos>() != null);
                        
                        DragAndDrop.visualMode = isValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                        
                        if (evt.type == EventType.DragPerform && isValid)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            // Añadir frames
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (obj is GameObject go)
                                {
                                    var frameComponent = go.GetComponent<MRAgruparObjetos>();
                                    if (frameComponent != null)
                                    {
                                        _target.AddFrame(frameComponent);
                                    }
                                }
                            }
                            
                            evt.Use();
                        }
                    }
                    break;
            }
        }
        
        
        
        /// <summary>
        /// Inicializa la ReorderableList para permitir drag & drop de frames
        /// </summary>
        private void InitializeReorderableList()
        {
            _reorderableFramesList = new ReorderableList(_serializedObject, _framesProp, true, true, true, true)
            {
                drawHeaderCallback = DrawReorderableListHeader,
                drawElementCallback = DrawReorderableListElement,
                elementHeight = FRAME_ITEM_HEIGHT + 4f, // Padding adicional para mejor apariencia
                onAddCallback = OnAddFrameToReorderableList,
                onRemoveCallback = OnRemoveFrameFromReorderableList,
                onReorderCallback = OnReorderFramesInList,
                onSelectCallback = OnSelectFrameInList
            };
        }
        
        
        
        /// <summary>
        /// Dibuja el header de la ReorderableList
        /// </summary>
        private void DrawReorderableListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, $"Frames ({_target.FrameCount}) - Arrastra para reordenar", EditorStyles.boldLabel);
        }
        
        /// <summary>
        /// Dibuja cada elemento de la ReorderableList
        /// </summary>
        private void DrawReorderableListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _framesProp.arraySize) return;
            
            var element = _framesProp.GetArrayElementAtIndex(index);
            var frame = element.objectReferenceValue as MRAgruparObjetos;
            
            rect.y += 2f; // Padding vertical
            rect.height = FRAME_ITEM_HEIGHT;
            
            // Dividir el rect en secciones (simplificado - solo label, field y botón eliminar)
            var labelRect = new Rect(rect.x, rect.y, 60f, rect.height);
            var objectFieldRect = new Rect(rect.x + 65f, rect.y, rect.width - 95f, rect.height); // Más ancho
            var deleteButtonRect = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);
            
            // Indicador de frame activo
            Color originalColor = GUI.color;
            bool isActiveFrame = (index == _target.ActiveFrameIndex);
            
            if (isActiveFrame)
            {
                GUI.color = Color.green;
            }
            
            // Etiqueta del frame
            EditorGUI.LabelField(labelRect, $"Frame {index + 1}");
            
            GUI.color = originalColor;
            
            // Campo del objeto (más ancho, hace la función de selección automáticamente)
            var newFrame = (MRAgruparObjetos)EditorGUI.ObjectField(objectFieldRect, frame, typeof(MRAgruparObjetos), true);
            
            if (newFrame != frame)
            {
                element.objectReferenceValue = newFrame;
            }
            
            // Solo botón Eliminar (X)
            GUI.color = Color.red;
            if (GUI.Button(deleteButtonRect, "X"))
            {
                if (EditorUtility.DisplayDialog("Eliminar Frame", 
                    $"¿Estás seguro de que deseas eliminar el Frame {index + 1}?", 
                    "Eliminar", "Cancelar"))
                {
                    _framesProp.DeleteArrayElementAtIndex(index);
                    _serializedObject.ApplyModifiedProperties();
                }
            }
            GUI.color = originalColor;
        }
        
        
        
        /// <summary>
        /// Callback cuando se añade un nuevo frame a la lista
        /// </summary>
        private void OnAddFrameToReorderableList(ReorderableList list)
        {
            // Crear nuevo GameObject con MRAgruparObjetos
            var newFrameGO = new GameObject($"Frame {_target.FrameCount + 1}");
            var frameComponent = newFrameGO.AddComponent<MRAgruparObjetos>();
            
            // Añadir a la lista
            _framesProp.InsertArrayElementAtIndex(_framesProp.arraySize);
            _framesProp.GetArrayElementAtIndex(_framesProp.arraySize - 1).objectReferenceValue = frameComponent;
            
            _serializedObject.ApplyModifiedProperties();
            
            // Seleccionar el nuevo frame
            Selection.activeGameObject = newFrameGO;
            
        }
        
        /// <summary>
        /// Callback cuando se elimina un frame de la lista
        /// </summary>
        private void OnRemoveFrameFromReorderableList(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _framesProp.arraySize)
            {
                var element = _framesProp.GetArrayElementAtIndex(list.index);
                var frame = element.objectReferenceValue as MRAgruparObjetos;
                
                if (EditorUtility.DisplayDialog("Eliminar Frame", 
                    $"¿Estás seguro de que deseas eliminar el Frame {list.index + 1}?", 
                    "Eliminar", "Cancelar"))
                {
                    _framesProp.DeleteArrayElementAtIndex(list.index);
                    _serializedObject.ApplyModifiedProperties();
                    
                }
            }
        }
        
        /// <summary>
        /// Callback cuando se reordena la lista de frames
        /// </summary>
        private void OnReorderFramesInList(ReorderableList list)
        {
            _serializedObject.ApplyModifiedProperties();
            
            // Ajustar el índice del frame activo si es necesario
            if (_target.ActiveFrameIndex >= _target.FrameCount)
            {
                _activeFrameIndexProp.intValue = Mathf.Max(0, _target.FrameCount - 1);
                _serializedObject.ApplyModifiedProperties();
            }
            
            
            // Aplicar previsualización del frame activo después del reordenamiento
            EditorApplication.delayCall += () => {
                if (_target != null)
                {
                    _previewManager.ApplyFramePreview();
                }
            };
        }
        
        /// <summary>
        /// Callback cuando se selecciona un frame en la lista
        /// </summary>
        private void OnSelectFrameInList(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _target.FrameCount)
            {
                _activeFrameIndexProp.intValue = list.index;
                _serializedObject.ApplyModifiedProperties();
                
                // Aplicar previsualización del frame seleccionado
                _previewManager.ApplyFramePreview();
            }
        }
        
    }
}
