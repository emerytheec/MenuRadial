#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.AlternativeMaterial
{
    /// <summary>
    /// Editor customizado para MRAgruparMateriales.
    /// Proporciona interfaz de drag & drop para capturar slots y crear grupos de materiales.
    /// </summary>
    [CustomEditor(typeof(MRAgruparMateriales))]
    public class MRAgruparMaterialesEditor : UnityEditor.Editor
    {
        private MRAgruparMateriales _target;
        private SerializedProperty _componentNameProperty;
        private SerializedProperty _slotsProperty;
        private SerializedProperty _groupsProperty;

        private bool _showSlots = true;
        private bool _showGroups = true;
        private Dictionary<int, bool> _groupFoldouts = new Dictionary<int, bool>();

        // Preview
        private bool _isPreviewActive = false;
        private int _previewFrame = 0;
        private Dictionary<MRMaterialSlot, Material> _originalMaterials = new Dictionary<MRMaterialSlot, Material>();

        // Estilos
        private GUIStyle _dropAreaStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized;

        private void OnEnable()
        {
            _target = (MRAgruparMateriales)target;
            _componentNameProperty = serializedObject.FindProperty("_componentName");
            _slotsProperty = serializedObject.FindProperty("_slots");
            _groupsProperty = serializedObject.FindProperty("_groups");
        }

        private void OnDisable()
        {
            // Restaurar materiales al cerrar el inspector
            if (_isPreviewActive)
            {
                RestoreOriginalMaterials();
                _isPreviewActive = false;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _dropAreaStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.gray }
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            _stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(5);

            DrawPreviewSection();
            EditorGUILayout.Space(5);

            DrawMeshDropArea();
            EditorGUILayout.Space(5);

            DrawSlotsSection();
            EditorGUILayout.Space(5);

            DrawMaterialDropArea();
            EditorGUILayout.Space(5);

            DrawGroupsSection();
            EditorGUILayout.Space(5);

            DrawActionsSection();
            EditorGUILayout.Space(5);

            DrawStatusSection();

            serializedObject.ApplyModifiedProperties();
        }

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.HEADER), _headerStyle);
            EditorGUILayout.PropertyField(_componentNameProperty, new GUIContent(MRLocalization.Get(L.AlternativeMaterial.NAME)));
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Preview

        private void DrawPreviewSection()
        {
            bool canPreview = _target.LinkedSlotsCount > 0 && _target.GroupCount > 0;

            // Slider con botones de navegación - Siempre visible
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(MRLocalization.Get(L.Radial.CURRENT_FRAME), GUILayout.Width(80));

            EditorGUI.BeginDisabledGroup(!canPreview);

            // Botón "< Anterior"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.PREVIOUS_FRAME), GUILayout.Width(70)))
            {
                _previewFrame = Mathf.Max(0, _previewFrame - 25);
                EnsurePreviewActive();
                ApplyPreviewMaterials();
            }

            // Slider de frame
            int newFrame = EditorGUILayout.IntSlider(_previewFrame, 0, 255);
            if (newFrame != _previewFrame)
            {
                _previewFrame = newFrame;
                EnsurePreviewActive();
                ApplyPreviewMaterials();
            }

            // Botón "Siguiente >"
            if (GUILayout.Button(MRLocalization.Get(L.Radial.NEXT_FRAME), GUILayout.Width(80)))
            {
                _previewFrame = Mathf.Min(255, _previewFrame + 25);
                EnsurePreviewActive();
                ApplyPreviewMaterials();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (!canPreview)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.LINK_SLOTS_HINT), MessageType.Info);
            }
        }

        private void EnsurePreviewActive()
        {
            if (!_isPreviewActive)
            {
                CaptureOriginalMaterials();
                _isPreviewActive = true;
            }
        }

        private void CaptureOriginalMaterials()
        {
            _originalMaterials.Clear();

            foreach (var slot in _target.Slots)
            {
                if (slot == null || !slot.IsValid || !slot.HasLinkedGroup) continue;
                if (slot.TargetRenderer == null) continue;

                var materials = slot.TargetRenderer.sharedMaterials;
                if (slot.MaterialIndex < materials.Length)
                {
                    _originalMaterials[slot] = materials[slot.MaterialIndex];
                }
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var kvp in _originalMaterials)
            {
                var slot = kvp.Key;
                var originalMaterial = kvp.Value;

                if (slot == null || !slot.IsValid) continue;
                if (slot.TargetRenderer == null) continue;

                var materials = slot.TargetRenderer.sharedMaterials;
                if (slot.MaterialIndex < materials.Length)
                {
                    materials[slot.MaterialIndex] = originalMaterial;
                    slot.TargetRenderer.sharedMaterials = materials;
                }
            }

            _originalMaterials.Clear();
            SceneView.RepaintAll();
        }

        private void ApplyPreviewMaterials()
        {
            foreach (var slot in _target.Slots)
            {
                if (slot == null || !slot.IsValid || !slot.HasLinkedGroup) continue;
                if (slot.TargetRenderer == null) continue;

                var group = _target.FindGroupByIndex(slot.LinkedGroupIndex);
                if (group == null || group.MaterialCount < 2) continue;

                // Calcular qué material aplicar según el frame actual
                Material materialToApply = GetMaterialForFrame(group, _previewFrame);

                if (materialToApply != null)
                {
                    var materials = slot.TargetRenderer.sharedMaterials;
                    if (slot.MaterialIndex < materials.Length)
                    {
                        materials[slot.MaterialIndex] = materialToApply;
                        slot.TargetRenderer.sharedMaterials = materials;
                    }
                }
            }

            SceneView.RepaintAll();
        }

        private Material GetMaterialForFrame(MRMaterialGroup group, int frame)
        {
            if (group == null || group.MaterialCount == 0) return null;

            var validMaterials = group.GetValidMaterials();
            if (validMaterials.Count == 0) return null;

            int materialCount = validMaterials.Count;
            int framesPerMaterial = 255 / materialCount;

            // Encontrar qué material corresponde al frame
            int materialIndex = Mathf.Min(frame / Mathf.Max(1, framesPerMaterial), materialCount - 1);

            return validMaterials[materialIndex];
        }

        #endregion

        #region Mesh Drop Area

        private void DrawMeshDropArea()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.MESH_CAPTURE), EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, MRLocalization.Get(L.AlternativeMaterial.DROP_MESHES_HERE), _dropAreaStyle);

            HandleMeshDrop(dropArea);
        }

        private void HandleMeshDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    bool hasValidObjects = DragAndDrop.objectReferences.Any(obj =>
                        obj is GameObject || obj is Renderer);

                    if (!hasValidObjects) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        PerformMeshDrop(DragAndDrop.objectReferences);
                    }
                    evt.Use();
                    break;
            }
        }

        private void PerformMeshDrop(Object[] objects)
        {
            Undo.RecordObject(_target, "Add Material Slots");

            int totalAdded = 0;
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    totalAdded += _target.ScanGameObject(go, true);
                }
                else if (obj is Renderer renderer)
                {
                    totalAdded += _target.AddRendererSlots(renderer);
                }
            }

            if (totalAdded > 0)
            {
                EditorUtility.SetDirty(_target);
                Debug.Log($"[MR Alternative Material] Añadidos {totalAdded} slots de material");
            }
        }

        #endregion

        #region Slots Section

        private void DrawSlotsSection()
        {
            _showSlots = EditorGUILayout.Foldout(_showSlots, MRLocalization.Get(L.AlternativeMaterial.MATERIAL_SLOTS, _target.SlotCount), EditorStyleManager.FoldoutStyle);

            if (_showSlots)
            {
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawSlotManagementButtons();
                EditorGUILayout.Space(EditorStyleManager.SPACING);
                DrawSlotList();
            }
        }

        private void DrawSlotManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorStyleManager.DrawManagementButtons(
                (MRLocalization.Get(L.AlternativeMaterial.UPDATE_PATHS), () => {
                    _target.UpdateAllHierarchyPaths();
                    EditorUtility.SetDirty(_target);
                }),
                (MRLocalization.Get(L.Frame.CLEAN_INVALID), () => {
                    Undo.RecordObject(_target, "Remove Invalid Slots");
                    int removed = _target.RemoveInvalidSlots();
                    if (removed > 0)
                    {
                        EditorUtility.SetDirty(_target);
                        Debug.Log($"[MR Alternative Material] Eliminados {removed} slots invalidos");
                    }
                })
            );

            // Botón limpiar todos (rojo)
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.CLEAR_ALL), GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
                {
                    if (EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.CONFIRM),
                        MRLocalization.Get(L.AlternativeMaterial.CLEAR_ALL_SLOTS_CONFIRM), MRLocalization.Get(L.Common.YES), MRLocalization.Get(L.Common.CANCEL)))
                    {
                        Undo.RecordObject(_target, "Clear All Slots");
                        _target.ClearAllSlots();
                        EditorUtility.SetDirty(_target);
                    }
                }
            });

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotList()
        {
            if (_target.Slots == null || _target.SlotCount == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.NO_SLOTS), MessageType.Info);
                return;
            }

            // Header de tabla
            EditorStyleManager.DrawTableHeader(
                (MRLocalization.Get(L.AlternativeMaterial.RENDERER), 120),
                (MRLocalization.Get(L.AlternativeMaterial.IDX), 30),
                (MRLocalization.Get(L.AlternativeMaterial.MATERIAL), 100),
                (MRLocalization.Get(L.AlternativeMaterial.GROUP), 0),
                ("", 50) // Espacio para botones
            );

            // Lista de slots
            for (int i = 0; i < _target.Slots.Count; i++)
            {
                if (DrawSlotItem(i, _target.Slots[i]))
                {
                    i--; // Si se elimino, ajustar indice
                }
            }
        }

        private bool DrawSlotItem(int index, MRMaterialSlot slot)
        {
            if (slot == null) return false;

            EditorGUILayout.BeginHorizontal();

            // Renderer (solo lectura, clickeable para hacer ping)
            string rendererName = slot.TargetRenderer != null ? slot.TargetRenderer.name : MRLocalization.Get(L.AlternativeMaterial.MISSING);

            var rendererButtonStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = slot.IsValid ? Color.white : Color.red }
            };

            if (GUILayout.Button(rendererName, rendererButtonStyle, GUILayout.Width(120)))
            {
                if (slot.TargetRenderer != null)
                {
                    // Solo ping, no cambiar seleccion
                    EditorGUIUtility.PingObject(slot.TargetRenderer.gameObject);
                }
            }

            // Indice del material (solo lectura)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(slot.MaterialIndex, GUILayout.Width(30));
            EditorGUI.EndDisabledGroup();

            // Material actual (clickeable para hacer ping en Project)
            string materialName = slot.CurrentMaterial != null ? slot.CurrentMaterial.name : "[None]";

            var materialButtonStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = slot.CurrentMaterial != null ? Color.white : Color.gray }
            };

            if (GUILayout.Button(materialName, materialButtonStyle, GUILayout.Width(100)))
            {
                if (slot.CurrentMaterial != null)
                {
                    // Solo ping, no cambiar seleccion
                    EditorGUIUtility.PingObject(slot.CurrentMaterial);
                }
            }

            // Dropdown para seleccionar grupo
            DrawGroupDropdown(slot);

            // Botón seleccionar renderer en hierarchy
            if (EditorStyleManager.DrawIconButton("d_ViewToolOrbit", MRLocalization.Get(L.AlternativeMaterial.SELECT_RENDERER)))
            {
                if (slot.TargetRenderer != null)
                {
                    Selection.activeGameObject = slot.TargetRenderer.gameObject;
                    EditorGUIUtility.PingObject(slot.TargetRenderer.gameObject);
                }
            }

            // Botón eliminar
            bool shouldRemove = false;
            EditorStyleManager.WithColor(Color.red, () => {
                if (GUILayout.Button("X", GUILayout.Width(EditorStyleManager.ICON_BUTTON_WIDTH), GUILayout.Height(EditorStyleManager.ICON_BUTTON_HEIGHT)))
                {
                    shouldRemove = true;
                }
            });

            EditorGUILayout.EndHorizontal();

            // Mostrar ruta jerárquica si el renderer es inválido
            if (!slot.IsValid && !string.IsNullOrEmpty(slot.HierarchyPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(125);
                EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.LAST_PATH, slot.HierarchyPath), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Procesar eliminacion
            if (shouldRemove)
            {
                Undo.RecordObject(_target, "Remove Slot");
                _target.RemoveSlot(slot);
                EditorUtility.SetDirty(_target);
                return true;
            }

            return false;
        }

        private void DrawGroupDropdown(MRMaterialSlot slot)
        {
            // Crear opciones para el dropdown
            var groupOptions = new List<string> { MRLocalization.Get(L.AlternativeMaterial.NO_GROUP) };
            var groupIndices = new List<int> { -1 };

            foreach (var group in _target.Groups)
            {
                groupOptions.Add(group.DisplayName);
                groupIndices.Add(group.GroupIndex);
            }

            // Encontrar indice actual
            int currentIndex = 0;
            if (slot.HasLinkedGroup)
            {
                int foundIndex = groupIndices.IndexOf(slot.LinkedGroupIndex);
                if (foundIndex >= 0) currentIndex = foundIndex;
            }

            // Dibujar popup con color
            Color prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = slot.HasLinkedGroup ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.8f, 0.5f);

            int newIndex = EditorGUILayout.Popup(currentIndex, groupOptions.ToArray());

            GUI.backgroundColor = prevBgColor;

            // Actualizar si cambio
            if (newIndex != currentIndex)
            {
                Undo.RecordObject(_target, "Change Slot Group");
                if (newIndex == 0)
                {
                    slot.UnlinkGroup();
                }
                else
                {
                    slot.LinkToGroup(groupIndices[newIndex]);
                }
                EditorUtility.SetDirty(_target);
            }
        }

        #endregion

        #region Material Drop Area

        private void DrawMaterialDropArea()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.MATERIAL_GROUPS), EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, MRLocalization.Get(L.AlternativeMaterial.DROP_MATERIALS_HERE), _dropAreaStyle);

            HandleMaterialDrop(dropArea);
        }

        private void HandleMaterialDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    bool hasMaterials = DragAndDrop.objectReferences.Any(obj => obj is Material);

                    if (!hasMaterials) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        PerformMaterialDrop(DragAndDrop.objectReferences);
                    }
                    evt.Use();
                    break;
            }
        }

        private void PerformMaterialDrop(Object[] objects)
        {
            var materials = objects.OfType<Material>().ToList();
            if (materials.Count == 0) return;

            Undo.RecordObject(_target, "Create Material Group");

            var newGroup = _target.CreateGroup(materials);
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MR Alternative Material] Creado {newGroup.DisplayName} con {materials.Count} materiales");
        }

        #endregion

        #region Groups Section

        private void DrawGroupsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showGroups = EditorGUILayout.Foldout(_showGroups, MRLocalization.Get(L.AlternativeMaterial.GROUPS_SECTION, _target.GroupCount), true);

            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.CLEAR_EMPTY), GUILayout.Width(100)))
            {
                Undo.RecordObject(_target, "Remove Empty Groups");
                int removed = _target.RemoveEmptyGroups();
                if (removed > 0)
                {
                    EditorUtility.SetDirty(_target);
                    Debug.Log($"[MR Alternative Material] Eliminados {removed} grupos vacios");
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_showGroups && _target.Groups != null)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < _target.Groups.Count; i++)
                {
                    DrawGroupItem(_target.Groups[i]);
                }

                if (_target.GroupCount == 0)
                {
                    EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.NO_GROUPS), MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGroupItem(MRMaterialGroup group)
        {
            if (group == null) return;

            // Asegurar que existe el foldout para este grupo
            if (!_groupFoldouts.ContainsKey(group.GroupIndex))
                _groupFoldouts[group.GroupIndex] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header del grupo
            EditorGUILayout.BeginHorizontal();

            _groupFoldouts[group.GroupIndex] = EditorGUILayout.Foldout(
                _groupFoldouts[group.GroupIndex],
                $"{group.DisplayName} ({group.MaterialCount} materiales)",
                true);

            // Campo para editar nombre
            string newName = EditorGUILayout.TextField(group.GroupName ?? "", GUILayout.Width(120));
            if (newName != group.GroupName)
            {
                Undo.RecordObject(_target, "Rename Group");
                group.GroupName = newName;
                EditorUtility.SetDirty(_target);
            }

            // Botón eliminar grupo
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                Undo.RecordObject(_target, "Remove Group");
                _target.RemoveGroup(group);
                EditorUtility.SetDirty(_target);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // Contenido del grupo
            if (_groupFoldouts[group.GroupIndex])
            {
                EditorGUI.indentLevel++;

                // Lista de materiales
                for (int i = 0; i < group.Materials.Count; i++)
                {
                    DrawMaterialInGroup(group, i);
                }

                // Drop area para añadir más materiales
                DrawGroupMaterialDropArea(group);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialInGroup(MRMaterialGroup group, int materialIndex)
        {
            Material material = group.Materials[materialIndex];

            EditorGUILayout.BeginHorizontal();

            // Indicador si es el material actual de algún slot
            bool isCurrentInSlot = _target.Slots.Any(s =>
                s != null && s.IsValid && s.LinkedGroupIndex == group.GroupIndex && s.CurrentMaterial == material);

            if (isCurrentInSlot)
            {
                GUIContent activeIcon = EditorGUIUtility.IconContent("d_greenLight");
                EditorGUILayout.LabelField(activeIcon, GUILayout.Width(20));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(20));
            }

            // Campo de material (editable)
            Material newMaterial = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);
            if (newMaterial != material)
            {
                Undo.RecordObject(_target, "Change Material in Group");
                group.Materials[materialIndex] = newMaterial;
                EditorUtility.SetDirty(_target);
            }

            // Botón eliminar material
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                Undo.RecordObject(_target, "Remove Material from Group");
                group.RemoveMaterialAt(materialIndex);
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupMaterialDropArea(MRMaterialGroup group)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 25, GUILayout.ExpandWidth(true));

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            GUI.Box(dropArea, MRLocalization.Get(L.AlternativeMaterial.DROP_MATERIALS_TO_GROUP), EditorStyles.helpBox);
            GUI.backgroundColor = prevColor;

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    bool hasMaterials = DragAndDrop.objectReferences.Any(obj => obj is Material);
                    if (!hasMaterials) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        Undo.RecordObject(_target, "Add Materials to Group");
                        var materials = DragAndDrop.objectReferences.OfType<Material>();
                        int added = group.AddMaterials(materials);

                        if (added > 0)
                        {
                            EditorUtility.SetDirty(_target);
                            Debug.Log($"[MR Alternative Material] Añadidos {added} materiales al {group.DisplayName}");
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        #endregion

        #region Actions Section

        private void DrawActionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.AUTO_LINKING), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.DETECT_LINKS), GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                Undo.RecordObject(_target, "Detect Links");
                int linked = _target.DetectAndLinkSlots();
                EditorUtility.SetDirty(_target);
                Debug.Log($"[MR Alternative Material] {linked} slots vinculados automaticamente");
            }

            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.UNLINK_ALL), GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                Undo.RecordObject(_target, "Unlink All");
                _target.UnlinkAllSlots();
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.DETECT_LINKS_HINT), MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Status Section

        private void DrawStatusSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.STATUS), EditorStyles.boldLabel);

            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.TOTAL_SLOTS, _target.SlotCount));
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.LINKED_SLOTS, _target.LinkedSlotsCount));
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.UNLINKED_SLOTS, _target.UnlinkedSlotsCount));
            EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.TOTAL_GROUPS, _target.GroupCount));

            // Validación
            var validation = _target.Validate();
            if (!validation.IsValid)
            {
                EditorGUILayout.HelpBox(validation.GetCompleteMessage(), MessageType.Warning);
            }
            else if (validation.Children.Any(c => c.Severity == Bender_Dios.MenuRadial.Validation.Models.ValidationSeverity.Warning))
            {
                EditorGUILayout.HelpBox(validation.GetCompleteMessage(), MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
#endif
