using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

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
            EditorGUI.LabelField(rect, MRLocalization.Get(L.RadialExtra.FRAMES_HEADER, _target.FrameCount), EditorStyles.boldLabel);
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

            // Dividir el rect en secciones (label, field, botón duplicar, botón eliminar)
            var labelRect = new Rect(rect.x, rect.y, 60f, rect.height);
            var objectFieldRect = new Rect(rect.x + 65f, rect.y, rect.width - 120f, rect.height);
            var duplicateButtonRect = new Rect(rect.x + rect.width - 50f, rect.y, 22f, rect.height);
            var deleteButtonRect = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);

            // Indicador de frame activo
            Color originalColor = GUI.color;
            bool isActiveFrame = (index == _target.ActiveFrameIndex);

            if (isActiveFrame)
            {
                GUI.color = Color.green;
            }

            // Etiqueta del frame
            EditorGUI.LabelField(labelRect, MRLocalization.Get(L.RadialExtra.FRAME_LABEL, index + 1));

            GUI.color = originalColor;

            // Campo del objeto (más ancho, hace la función de selección automáticamente)
            var newFrame = (MRAgruparObjetos)EditorGUI.ObjectField(objectFieldRect, frame, typeof(MRAgruparObjetos), true);

            if (newFrame != frame)
            {
                element.objectReferenceValue = newFrame;
            }

            // Botón Duplicar
            GUI.enabled = frame != null;
            GUI.color = new Color(0.5f, 0.8f, 1f);
            if (GUI.Button(duplicateButtonRect, new GUIContent("+", MRLocalization.Get(L.RadialExtra.DUPLICATE_FRAME))))
            {
                DuplicateFrame(index, frame);
            }
            GUI.color = originalColor;
            GUI.enabled = true;

            // Botón Eliminar (X)
            GUI.color = Color.red;
            if (GUI.Button(deleteButtonRect, "X"))
            {
                if (EditorUtility.DisplayDialog(MRLocalization.Get(L.RadialExtra.DELETE_FRAME_TITLE),
                    MRLocalization.Get(L.RadialExtra.DELETE_FRAME_CONFIRM, index + 1),
                    MRLocalization.Get(L.Common.DELETE), MRLocalization.Get(L.Common.CANCEL)))
                {
                    _framesProp.DeleteArrayElementAtIndex(index);
                    _serializedObject.ApplyModifiedProperties();
                }
            }
            GUI.color = originalColor;
        }

        /// <summary>
        /// Duplica un frame y lo inserta debajo del original
        /// </summary>
        private void DuplicateFrame(int sourceIndex, MRAgruparObjetos sourceFrame)
        {
            if (sourceFrame == null) return;

            Transform parent = sourceFrame.transform.parent;

            // Generar nombres secuenciales
            var existingGONames = _target.FrameObjects
                .Where(f => f != null)
                .Select(f => f.gameObject.name);
            var existingFrameNames = _target.FrameObjects
                .Where(f => f != null)
                .Select(f => f.FrameName);

            string newGOName = GenerateSequentialName(sourceFrame.gameObject.name, existingGONames);
            string newFrameName = GenerateSequentialName(sourceFrame.FrameName, existingFrameNames);

            // Crear nuevo GameObject
            var newFrameGO = new GameObject(newGOName);
            newFrameGO.transform.SetParent(parent);

            // Posicionar justo debajo del original en la jerarquía
            int siblingIndex = sourceFrame.transform.GetSiblingIndex() + 1;
            newFrameGO.transform.SetSiblingIndex(siblingIndex);

            // Agregar componente y copiar datos
            var newFrame = newFrameGO.AddComponent<MRAgruparObjetos>();
            CopyFrameDataTo(sourceFrame, newFrame, newFrameName);

            // Registrar para Undo
            Undo.RegisterCreatedObjectUndo(newFrameGO, "Duplicar Frame");

            // Insertar en la lista justo debajo del original
            int insertIndex = sourceIndex + 1;
            _framesProp.InsertArrayElementAtIndex(insertIndex);
            _framesProp.GetArrayElementAtIndex(insertIndex).objectReferenceValue = newFrame;

            _serializedObject.ApplyModifiedProperties();

            // Seleccionar el nuevo frame
            Selection.activeGameObject = newFrameGO;
        }

        /// <summary>
        /// Extracts the base name by stripping trailing sequential numbers (2+ digits) or "(Copy)" suffixes.
        /// E.g., "Outfit 03" → "Outfit", "Outfit (Copy)" → "Outfit", "Outfit 01 (Copy)" → "Outfit"
        /// </summary>
        internal static string ExtractBaseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            string result = name;

            // Strip trailing " (Copy)" patterns (legacy duplicates, could be nested)
            while (result.EndsWith(" (Copy)"))
            {
                result = result.Substring(0, result.Length - 7);
            }

            // Strip trailing " XX" number suffix (2+ digits from our sequential format)
            var match = Regex.Match(result, @"^(.+?)\s+(\d{2,})$");
            if (match.Success)
            {
                result = match.Groups[1].Value;
            }

            return result.TrimEnd();
        }

        /// <summary>
        /// Generates a sequential name based on existing sibling names.
        /// Finds the highest existing number for the same base name and increments.
        /// E.g., "Outfit" with existing "Outfit 01", "Outfit 02" → "Outfit 03"
        /// </summary>
        internal static string GenerateSequentialName(string sourceName, IEnumerable<string> existingNames)
        {
            string baseName = ExtractBaseName(sourceName);

            int maxNumber = 0;
            string escapedBase = Regex.Escape(baseName);
            var pattern = new Regex($@"^{escapedBase}\s+(\d{{2,}})$");

            foreach (var existing in existingNames)
            {
                var match = pattern.Match(existing);
                if (match.Success)
                {
                    int num = int.Parse(match.Groups[1].Value);
                    if (num > maxNumber)
                        maxNumber = num;
                }
            }

            return $"{baseName} {maxNumber + 1:00}";
        }

        /// <summary>
        /// Copia los datos de un frame a otro con nombre específico
        /// </summary>
        internal static void CopyFrameDataTo(MRAgruparObjetos source, MRAgruparObjetos destination, string newFrameName)
        {
            if (source == null || destination == null) return;

            destination.FrameName = newFrameName;

            foreach (var objRef in source.ObjectReferences)
            {
                if (objRef?.GameObject != null)
                {
                    destination.AddGameObject(objRef.GameObject, objRef.IsActive);
                }
            }

            foreach (var matRef in source.MaterialReferences)
            {
                if (matRef?.TargetRenderer != null)
                {
                    destination.AddMaterialReference(
                        matRef.TargetRenderer,
                        matRef.MaterialIndex,
                        matRef.AlternativeMaterial);
                }
            }

            foreach (var blendRef in source.BlendshapeReferences)
            {
                if (blendRef?.TargetRenderer != null)
                {
                    destination.AddBlendshapeReference(
                        blendRef.TargetRenderer,
                        blendRef.BlendshapeName,
                        blendRef.Value);
                }
            }
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
                
                if (EditorUtility.DisplayDialog(MRLocalization.Get(L.RadialExtra.DELETE_FRAME_TITLE),
                    MRLocalization.Get(L.RadialExtra.DELETE_FRAME_CONFIRM, list.index + 1),
                    MRLocalization.Get(L.Common.DELETE), MRLocalization.Get(L.Common.CANCEL)))
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
