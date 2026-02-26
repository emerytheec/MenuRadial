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
        private SerializedProperty _folderStructureModeProperty;

        private bool _showSlots = true;
        private bool _showGroups = true;
        private Dictionary<int, bool> _groupFoldouts = new Dictionary<int, bool>();

        // Preview
        private bool _isPreviewActive = false;
        private int _previewFrame = 0;
        private Dictionary<MRMaterialSlot, Material> _originalMaterials = new Dictionary<MRMaterialSlot, Material>();

        // Sugerencias
        private MaterialSuggestionResult _suggestionResult;
        private bool _showSuggestions = false;
        private Dictionary<MRMaterialSlot, HashSet<Material>> _selectedSuggestions = new Dictionary<MRMaterialSlot, HashSet<Material>>();
        private Dictionary<MRMaterialSlot, bool> _slotSuggestionFoldouts = new Dictionary<MRMaterialSlot, bool>();
        private MaterialAlternativeDetector _detector;
        private FolderStructureAnalysis _lastStructureAnalysis;

        // Selector de carpetas
        private List<FolderMaterialInfo> _availableSiblingFolders = new List<FolderMaterialInfo>();
        private FolderStructureAnalyzer _folderAnalyzer = new FolderStructureAnalyzer();

        // Sección informativa de carpetas
        private List<FolderMaterialInfo> _cachedFolderInfos;
        private List<FolderMaterialInfo> _cachedSiblingFolders;
        private int _lastKnownSlotCount = -1;
        private bool _showFolderInfo = true;

        // Estilos
        private GUIStyle _dropAreaStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _suggestionHighStyle;
        private GUIStyle _suggestionMediumStyle;
        private GUIStyle _suggestionLowStyle;
        private bool _stylesInitialized;

        private void OnEnable()
        {
            _target = (MRAgruparMateriales)target;
            _componentNameProperty = serializedObject.FindProperty("_componentName");
            _slotsProperty = serializedObject.FindProperty("_slots");
            _groupsProperty = serializedObject.FindProperty("_groups");
            _folderStructureModeProperty = serializedObject.FindProperty("_folderStructureMode");
            _detector = new MaterialAlternativeDetector();
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

            // Estilos para niveles de confianza
            _suggestionHighStyle = new GUIStyle(EditorStyles.helpBox);
            _suggestionHighStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.2f, 0.3f));

            _suggestionMediumStyle = new GUIStyle(EditorStyles.helpBox);
            _suggestionMediumStyle.normal.background = MakeTex(2, 2, new Color(0.6f, 0.5f, 0.2f, 0.3f));

            _suggestionLowStyle = new GUIStyle(EditorStyles.helpBox);
            _suggestionLowStyle.normal.background = MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.2f));

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
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

            DrawActionsSection();
            EditorGUILayout.Space(5);

            DrawMaterialDropArea();
            EditorGUILayout.Space(5);

            DrawGroupsSection();
            EditorGUILayout.Space(5);

            DrawSuggestionsSection();
            EditorGUILayout.Space(5);

            DrawFolderInfoSection();

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
            string materialName = slot.CurrentMaterial != null ? slot.CurrentMaterial.name : "(Ninguno)";

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

        #region Suggestions Section

        private void DrawSuggestionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Nota siempre visible
            EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.SUGGESTIONS_HINT), MessageType.Info);

            // Header con foldout
            EditorGUILayout.BeginHorizontal();
            _showSuggestions = EditorGUILayout.Foldout(_showSuggestions,
                MRLocalization.Get(L.AlternativeMaterial.SUGGESTIONS_SECTION), true);

            // Botón detectar (verde)
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.DETECT_ALTERNATIVES),
                GUILayout.Width(150), GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                DetectAlternatives();
            }
            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            // Selector de modo de estructura
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Modo de detección:", GUILayout.Width(120));

            string[] modeNames = new string[]
            {
                "Automático",
                "Grupos en carpeta",
                "Estilo en carpeta",
                "Todo en carpeta"
            };

            EditorGUI.BeginChangeCheck();
            int currentIndex = (int)_target.FolderStructureMode;
            int newIndex = EditorGUILayout.Popup(currentIndex, modeNames);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Change Folder Structure Mode");
                _target.FolderStructureMode = (FolderStructureMode)newIndex;
                EditorUtility.SetDirty(_target);
                // Limpiar resultados anteriores para forzar nueva detección
                _suggestionResult = null;
                _lastStructureAnalysis = null;
            }
            EditorGUILayout.EndHorizontal();

            if (_target.FolderStructureMode != FolderStructureMode.Auto)
            {
                EditorGUILayout.HelpBox($"Modo forzado: {GetModeDescription(_target.FolderStructureMode)}", MessageType.Warning);
            }

            if (_showSuggestions)
            {
                // Mostrar información de estructura detectada
                DrawStructureAnalysisInfo();

                if (_suggestionResult == null || !_suggestionResult.HasAnySuggestions)
                {
                    if (_suggestionResult != null)
                    {
                        EditorGUILayout.HelpBox(MRLocalization.Get(L.AlternativeMaterial.NO_SUGGESTIONS), MessageType.Info);
                    }
                }
                else
                {
                    // Resumen
                    EditorGUILayout.LabelField(
                        MRLocalization.Get(L.AlternativeMaterial.SUGGESTIONS_FOUND,
                            _suggestionResult.TotalSuggestionsFound,
                            _suggestionResult.SlotsWithSuggestions),
                        EditorStyles.boldLabel);

                    EditorGUILayout.Space(5);

                    // Botones de acción global
                    DrawSuggestionGlobalActions();

                    // Verificar si se limpiaron las sugerencias (el botón pudo haber sido presionado)
                    if (_suggestionResult != null && _suggestionResult.HasAnySuggestions)
                    {
                        EditorGUILayout.Space(5);

                        // Dibujar sugerencias por slot
                        var slotsWithSuggestions = _suggestionResult.GetSlotsWithSuggestions();
                        if (slotsWithSuggestions != null)
                        {
                            foreach (var slotResult in slotsWithSuggestions)
                            {
                                if (slotResult != null)
                                {
                                    DrawSlotSuggestions(slotResult);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DetectAlternatives()
        {
            if (_target.SlotCount == 0)
            {
                Debug.LogWarning("[MR Alternative Material] No hay slots para analizar. Arrastra meshes primero.");
                return;
            }

            // Obtener análisis de estructura
            _lastStructureAnalysis = _detector.GetFolderStructureAnalysis(_target);

            _suggestionResult = _detector.DetectAlternatives(_target);
            _selectedSuggestions.Clear();
            _slotSuggestionFoldouts.Clear();

            // Expandir sección de sugerencias para mostrar resultados
            _showSuggestions = true;

            // Inicializar selecciones con sugerencias de alta confianza
            foreach (var slotResult in _suggestionResult.GetSlotsWithSuggestions())
            {
                _selectedSuggestions[slotResult.Slot] = new HashSet<Material>();
                _slotSuggestionFoldouts[slotResult.Slot] = false; // Cada slot colapsado por defecto

                // Pre-seleccionar alta confianza
                foreach (var suggestion in slotResult.GetHighConfidenceSuggestions())
                {
                    _selectedSuggestions[slotResult.Slot].Add(suggestion.Material);
                }
            }

            Debug.Log($"[MR Alternative Material] Detección completada: {_suggestionResult.TotalSuggestionsFound} sugerencias para {_suggestionResult.SlotsWithSuggestions} slots");
        }

        private void DrawStructureAnalysisInfo()
        {
            if (_lastStructureAnalysis == null || !_lastStructureAnalysis.IsValid)
                return;

            EditorGUILayout.Space(3);

            // Determinar color e icono según tipo de estructura
            string structureIcon;
            Color boxColor;
            string structureDescription;

            switch (_lastStructureAnalysis.StructureType)
            {
                case FolderStructureType.MaterialsGroupedByFolder:
                    structureIcon = "d_Folder Icon";
                    boxColor = new Color(0.3f, 0.6f, 0.3f, 0.3f);
                    structureDescription = "Grupos en carpeta - Cada carpeta define un grupo completo";
                    break;

                case FolderStructureType.MaterialsDistributedInSiblingFolders:
                    structureIcon = "d_FolderOpened Icon";
                    boxColor = new Color(0.3f, 0.4f, 0.7f, 0.3f);
                    structureDescription = "Estilo en carpeta - Emparejar materiales entre carpetas hermanas";
                    break;

                case FolderStructureType.MaterialsMixedInSingleFolder:
                default:
                    structureIcon = "d_TextAsset Icon";
                    boxColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    structureDescription = "Todo en carpeta - Agrupar por similitud de nombres";
                    break;
            }

            // Dibujar caja de información
            Color prevBgColor = GUI.backgroundColor;
            GUI.backgroundColor = boxColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Icono
            GUIContent icon = EditorGUIUtility.IconContent(structureIcon);
            EditorGUILayout.LabelField(icon, GUILayout.Width(20), GUILayout.Height(20));

            // Tipo de estructura
            EditorGUILayout.LabelField($"Estructura: {_lastStructureAnalysis.StructureType}", EditorStyles.boldLabel);

            // Confianza
            EditorGUILayout.LabelField($"({Mathf.RoundToInt(_lastStructureAnalysis.Confidence * 100)}%)", GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();

            // Descripción
            EditorGUILayout.LabelField(structureDescription, EditorStyles.wordWrappedMiniLabel);

            // Detalles adicionales
            if (_lastStructureAnalysis.UniqueFolderCount > 0)
            {
                EditorGUILayout.LabelField($"Carpetas analizadas: {_lastStructureAnalysis.UniqueFolderCount}", EditorStyles.miniLabel);
            }

            // Mostrar carpeta principal si existe (Caso 2 con carpeta dominante)
            if (_lastStructureAnalysis.PrimaryFolder != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUIContent primaryIcon = EditorGUIUtility.IconContent("d_Linked");
                EditorGUILayout.LabelField(primaryIcon, GUILayout.Width(16), GUILayout.Height(16));
                EditorGUILayout.LabelField(
                    $"Carpeta principal: {_lastStructureAnalysis.PrimaryFolder.FolderName} ({_lastStructureAnalysis.PrimaryFolder.SlotCount} slots)",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Mostrar carpetas con referencias puntuales si existen
            if (_lastStructureAnalysis.SecondaryFoldersWithSlots != null && _lastStructureAnalysis.SecondaryFoldersWithSlots.Count > 0)
            {
                string secondaryNames = string.Join(", ",
                    _lastStructureAnalysis.SecondaryFoldersWithSlots.Select(f => $"{f.FolderName}({f.SlotCount})"));
                EditorGUILayout.LabelField($"Referencias puntuales: {secondaryNames}", EditorStyles.miniLabel);
            }

            if (_lastStructureAnalysis.SiblingFolders != null && _lastStructureAnalysis.SiblingFolders.Count > 0)
            {
                string siblingNames = string.Join(", ",
                    _lastStructureAnalysis.SiblingFolders.Take(5).Select(f => f.FolderName));
                if (_lastStructureAnalysis.SiblingFolders.Count > 5)
                    siblingNames += $" (+{_lastStructureAnalysis.SiblingFolders.Count - 5} más)";

                EditorGUILayout.LabelField($"Carpetas hermanas: {siblingNames}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = prevBgColor;

            EditorGUILayout.Space(5);
        }

        private void DrawSuggestionGlobalActions()
        {
            EditorGUILayout.BeginHorizontal();

            // Aceptar todas las de alta confianza
            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.ACCEPT_ALL_HIGH),
                GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                AcceptAllHighConfidence();
            }

            // Limpiar sugerencias
            if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.CLEAR_SUGGESTIONS),
                GUILayout.Height(EditorStyleManager.SMALL_BUTTON_HEIGHT)))
            {
                _suggestionResult = null;
                _selectedSuggestions.Clear();
                _slotSuggestionFoldouts.Clear();
                _showSuggestions = false; // Colapsar sección al limpiar
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotSuggestions(SlotSuggestionResult slotResult)
        {
            if (slotResult.Slot == null) return;

            // Asegurar foldout existe (colapsado por defecto)
            if (!_slotSuggestionFoldouts.ContainsKey(slotResult.Slot))
                _slotSuggestionFoldouts[slotResult.Slot] = false;

            if (!_selectedSuggestions.ContainsKey(slotResult.Slot))
                _selectedSuggestions[slotResult.Slot] = new HashSet<Material>();

            // Determinar estilo según mejor confianza
            GUIStyle boxStyle = EditorStyles.helpBox;
            if (slotResult.Suggestions.Count > 0)
            {
                var bestConfidence = slotResult.Suggestions[0].ConfidenceLevel;
                boxStyle = bestConfidence switch
                {
                    ConfidenceLevel.High => _suggestionHighStyle ?? EditorStyles.helpBox,
                    ConfidenceLevel.Medium => _suggestionMediumStyle ?? EditorStyles.helpBox,
                    _ => _suggestionLowStyle ?? EditorStyles.helpBox
                };
            }

            EditorGUILayout.BeginVertical(boxStyle);

            // Header del slot
            EditorGUILayout.BeginHorizontal();

            string slotName = slotResult.Slot.TargetRenderer != null
                ? slotResult.Slot.TargetRenderer.name
                : "(Sin Renderer)";

            // Obtener el porcentaje más alto
            string bestConfidenceStr = "";
            if (slotResult.Suggestions.Count > 0)
            {
                bestConfidenceStr = $" - {slotResult.Suggestions[0].GetConfidencePercentage()}";
            }

            _slotSuggestionFoldouts[slotResult.Slot] = EditorGUILayout.Foldout(
                _slotSuggestionFoldouts[slotResult.Slot],
                $"{slotName} #{slotResult.Slot.MaterialIndex} - {slotResult.SuggestionCount} sugerencias{bestConfidenceStr}",
                true);

            // Botón crear grupo para este slot
            int selectedCount = _selectedSuggestions[slotResult.Slot].Count;
            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            if (GUILayout.Button($"{MRLocalization.Get(L.AlternativeMaterial.CREATE_GROUP_FROM_SELECTION)} ({selectedCount})",
                GUILayout.Width(180)))
            {
                CreateGroupFromSlotSuggestions(slotResult);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_slotSuggestionFoldouts[slotResult.Slot])
            {
                EditorGUI.indentLevel++;

                // Material actual
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(MRLocalization.Get(L.AlternativeMaterial.CURRENT_MATERIAL, ""),
                    GUILayout.Width(100));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(slotResult.CurrentMaterial, typeof(Material), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // Botones seleccionar/deseleccionar
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.SELECT_ALL_SUGGESTIONS), GUILayout.Width(120)))
                {
                    foreach (var s in slotResult.Suggestions)
                        _selectedSuggestions[slotResult.Slot].Add(s.Material);
                }
                if (GUILayout.Button(MRLocalization.Get(L.AlternativeMaterial.DESELECT_ALL_SUGGESTIONS), GUILayout.Width(120)))
                {
                    _selectedSuggestions[slotResult.Slot].Clear();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // Lista de sugerencias
                foreach (var suggestion in slotResult.Suggestions)
                {
                    DrawSuggestionItem(slotResult.Slot, suggestion);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawSuggestionItem(MRMaterialSlot slot, MaterialSuggestion suggestion)
        {
            EditorGUILayout.BeginHorizontal();

            // Checkbox
            bool isSelected = _selectedSuggestions[slot].Contains(suggestion.Material);
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
            if (newSelected != isSelected)
            {
                if (newSelected)
                    _selectedSuggestions[slot].Add(suggestion.Material);
                else
                    _selectedSuggestions[slot].Remove(suggestion.Material);
            }

            // Indicador de confianza con color
            Color confColor = suggestion.ConfidenceLevel switch
            {
                ConfidenceLevel.High => new Color(0.3f, 0.8f, 0.3f),
                ConfidenceLevel.Medium => new Color(0.8f, 0.7f, 0.3f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = confColor;
            GUILayout.Box(suggestion.GetConfidencePercentage(), GUILayout.Width(45), GUILayout.Height(18));
            GUI.backgroundColor = prevColor;

            // Material
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(suggestion.Material, typeof(Material), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Razones (más pequeño, debajo)
            if (suggestion.Reasons.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                EditorGUILayout.LabelField(suggestion.GetReasonsText(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AcceptAllHighConfidence()
        {
            if (_suggestionResult == null) return;

            Undo.RecordObject(_target, "Accept High Confidence Suggestions");

            int groupsCreated = 0;
            int slotsLinked = 0;

            foreach (var slotResult in _suggestionResult.GetSlotsWithSuggestions())
            {
                var highConfidence = slotResult.GetHighConfidenceSuggestions();
                if (highConfidence.Count == 0) continue;

                // Crear lista de materiales: actual + sugeridos
                var materials = new List<Material> { slotResult.CurrentMaterial };
                foreach (var suggestion in highConfidence)
                {
                    if (!materials.Contains(suggestion.Material))
                        materials.Add(suggestion.Material);
                }

                if (materials.Count >= 2)
                {
                    // Buscar grupo existente que contenga el material actual
                    var existingGroup = FindGroupContainingMaterial(slotResult.CurrentMaterial);

                    if (existingGroup != null)
                    {
                        // Añadir materiales que falten al grupo existente
                        foreach (var mat in materials)
                        {
                            if (!existingGroup.ContainsMaterial(mat))
                                existingGroup.AddMaterial(mat);
                        }
                        // Vincular slot al grupo existente
                        slotResult.Slot.LinkToGroup(existingGroup.GroupIndex);
                        slotsLinked++;
                    }
                    else
                    {
                        // Crear grupo nuevo
                        var group = _target.CreateGroup(materials);
                        groupsCreated++;
                        slotResult.Slot.LinkToGroup(group.GroupIndex);
                        slotsLinked++;
                    }
                }
            }

            EditorUtility.SetDirty(_target);

            if (groupsCreated > 0 || slotsLinked > 0)
            {
                Debug.Log($"[MR Alternative Material] {groupsCreated} grupos creados, {slotsLinked} slots vinculados");
                // Limpiar sugerencias de los slots ya procesados
                _suggestionResult = _detector.DetectAlternatives(_target);
            }
        }

        /// <summary>
        /// Busca un grupo existente que contenga el material especificado,
        /// o un material con el mismo nombre base.
        /// </summary>
        private MRMaterialGroup FindGroupContainingMaterial(Material material)
        {
            if (material == null) return null;

            // First, try exact match
            foreach (var group in _target.Groups)
            {
                if (group.ContainsMaterial(material))
                    return group;
            }

            // Second, try matching by base name
            string materialBaseName = MaterialAlternativeDetector.ExtractBaseName(material.name);
            if (string.IsNullOrEmpty(materialBaseName))
                return null;

            foreach (var group in _target.Groups)
            {
                foreach (var groupMaterial in group.Materials)
                {
                    if (groupMaterial == null) continue;

                    string groupMaterialBaseName = MaterialAlternativeDetector.ExtractBaseName(groupMaterial.name);
                    if (!string.IsNullOrEmpty(groupMaterialBaseName) &&
                        materialBaseName.Equals(groupMaterialBaseName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return group;
                    }
                }
            }

            return null;
        }

        private void CreateGroupFromSlotSuggestions(SlotSuggestionResult slotResult)
        {
            if (slotResult?.Slot == null) return;

            var selected = _selectedSuggestions.ContainsKey(slotResult.Slot)
                ? _selectedSuggestions[slotResult.Slot]
                : new HashSet<Material>();

            if (selected.Count == 0) return;

            Undo.RecordObject(_target, "Create Group from Suggestions");

            // Crear lista: material actual + seleccionados
            var materials = new List<Material> { slotResult.CurrentMaterial };
            foreach (var mat in selected)
            {
                if (!materials.Contains(mat))
                    materials.Add(mat);
            }

            // Buscar grupo existente que contenga el material actual
            var existingGroup = FindGroupContainingMaterial(slotResult.CurrentMaterial);
            string actionMessage;

            if (existingGroup != null)
            {
                // Añadir materiales que falten al grupo existente
                int added = 0;
                foreach (var mat in materials)
                {
                    if (!existingGroup.ContainsMaterial(mat))
                    {
                        existingGroup.AddMaterial(mat);
                        added++;
                    }
                }
                // Vincular slot al grupo existente
                slotResult.Slot.LinkToGroup(existingGroup.GroupIndex);
                actionMessage = $"Slot vinculado a {existingGroup.DisplayName} ({added} materiales añadidos)";
            }
            else
            {
                // Crear grupo nuevo
                var group = _target.CreateGroup(materials);
                slotResult.Slot.LinkToGroup(group.GroupIndex);
                actionMessage = $"Creado {group.DisplayName} con {materials.Count} materiales";
            }

            EditorUtility.SetDirty(_target);

            // Limpiar selecciones de este slot
            _selectedSuggestions[slotResult.Slot].Clear();

            Debug.Log($"[MR Alternative Material] {actionMessage}");

            // Refrescar sugerencias
            _suggestionResult = _detector.DetectAlternatives(_target);
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

            // Asegurar que existe el foldout para este grupo (colapsado por defecto)
            if (!_groupFoldouts.ContainsKey(group.GroupIndex))
                _groupFoldouts[group.GroupIndex] = false;

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

            // Botones de reordenar
            EditorGUI.BeginDisabledGroup(materialIndex == 0);
            if (GUILayout.Button("▲", GUILayout.Width(20)))
            {
                Undo.RecordObject(_target, "Move Material Up");
                SwapMaterials(group, materialIndex, materialIndex - 1);
                EditorUtility.SetDirty(_target);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(materialIndex >= group.Materials.Count - 1);
            if (GUILayout.Button("▼", GUILayout.Width(20)))
            {
                Undo.RecordObject(_target, "Move Material Down");
                SwapMaterials(group, materialIndex, materialIndex + 1);
                EditorUtility.SetDirty(_target);
            }
            EditorGUI.EndDisabledGroup();

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

        private void SwapMaterials(MRMaterialGroup group, int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= group.Materials.Count) return;
            if (indexB < 0 || indexB >= group.Materials.Count) return;

            var temp = group.Materials[indexA];
            group.Materials[indexA] = group.Materials[indexB];
            group.Materials[indexB] = temp;
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

        #region Folder Info Section

        /// <summary>
        /// Asegura que la información de carpetas esté cacheada.
        /// Solo re-ejecuta el análisis cuando cambia la cantidad de slots.
        /// </summary>
        private void EnsureFolderInfoCached()
        {
            int currentSlotCount = _target.SlotCount;
            if (currentSlotCount == _lastKnownSlotCount && _cachedFolderInfos != null)
                return;

            _lastKnownSlotCount = currentSlotCount;

            if (currentSlotCount == 0)
            {
                _cachedFolderInfos = null;
                _cachedSiblingFolders = null;
                return;
            }

            var slotList = _target.Slots.Where(s => s != null && s.IsValid).ToList();
            if (slotList.Count == 0)
            {
                _cachedFolderInfos = null;
                _cachedSiblingFolders = null;
                return;
            }

            _cachedSiblingFolders = _folderAnalyzer.GetAvailableSiblingFolders(_target);

            // Recopilar solo las carpetas que tienen slots (materiales capturados)
            _cachedFolderInfos = _cachedSiblingFolders?.Where(f => f.HasSlots).ToList()
                ?? new List<FolderMaterialInfo>();

            // También actualizar la lista del folder selector existente
            _availableSiblingFolders = _cachedSiblingFolders ?? new List<FolderMaterialInfo>();

            // Si no hay selección previa, seleccionar todas por defecto
            if (_target.SelectedSiblingFolders.Count == 0 && _cachedSiblingFolders.Count > 0)
            {
                Undo.RecordObject(_target, "Auto-select All Folders");
                foreach (var folder in _cachedSiblingFolders)
                    _target.SelectedSiblingFolders.Add(folder.FolderPath);
                EditorUtility.SetDirty(_target);
            }
        }

        /// <summary>
        /// Dibuja la sección informativa de carpetas entre Slots y Acciones.
        /// Muestra de solo lectura la estructura de carpetas donde viven los materiales.
        /// </summary>
        private void DrawFolderInfoSection()
        {
            if (_target.SlotCount == 0)
            {
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.AlternativeMaterial.FOLDER_INFO_NO_SLOTS),
                    MessageType.Info);
                return;
            }

            EnsureFolderInfoCached();

            if (_cachedSiblingFolders == null || _cachedSiblingFolders.Count == 0)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showFolderInfo = EditorGUILayout.Foldout(_showFolderInfo,
                MRLocalization.Get(L.AlternativeMaterial.FOLDER_INFO), true, EditorStyles.foldoutHeader);

            if (_showFolderInfo)
            {
                EditorGUILayout.Space(2);

                // Agrupar carpetas por ParentFolderPath
                var groupedByParent = _cachedSiblingFolders
                    .GroupBy(f => f.ParentFolderPath ?? "")
                    .ToList();

                bool singleFolderNoSiblings = _cachedSiblingFolders.Count == 1;

                if (singleFolderNoSiblings)
                {
                    var folder = _cachedSiblingFolders[0];
                    DrawFolderLine(folder);
                }
                else
                {
                    foreach (var parentGroup in groupedByParent)
                    {
                        string parentPath = parentGroup.Key;
                        string parentName = string.IsNullOrEmpty(parentPath) ? "?" :
                            System.IO.Path.GetFileName(parentPath);

                        // Carpeta padre (negrita)
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(24); // Alinear con los checkboxes
                        EditorGUILayout.LabelField(parentName + "/", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();

                        // Subcarpetas
                        foreach (var folder in parentGroup.OrderByDescending(f => f.HasSlots))
                        {
                            DrawFolderLine(folder);
                        }

                        EditorGUILayout.Space(2);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Dibuja una línea de carpeta replicando el layout de DrawSiblingFolderSelector:
        /// Checkbox(20) | LinkedIcon(20) | FolderName(150) | (X mats)
        /// </summary>
        private void DrawFolderLine(FolderMaterialInfo folder)
        {
            EditorGUILayout.BeginHorizontal();

            // Checkbox de selección para sugerencias
            bool isSelected = _target.SelectedSiblingFolders.Contains(folder.FolderPath);
            EditorGUI.BeginChangeCheck();
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Folder Selection");
                if (newSelected && !isSelected)
                    _target.SelectedSiblingFolders.Add(folder.FolderPath);
                else if (!newSelected && isSelected)
                    _target.SelectedSiblingFolders.Remove(folder.FolderPath);
                EditorUtility.SetDirty(_target);
                _suggestionResult = null;
                _lastStructureAnalysis = null;
            }

            // Indicador si tiene slots apuntando
            if (folder.HasSlots)
            {
                GUIContent icon = EditorGUIUtility.IconContent("d_Linked");
                EditorGUILayout.LabelField(icon, GUILayout.Width(20));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(20));
            }

            // Nombre de carpeta
            EditorGUILayout.LabelField(folder.FolderName, GUILayout.Width(150));

            // Cantidad de materiales
            EditorGUILayout.LabelField($"({folder.MaterialCount} mats)", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
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

        #region Helpers

        /// <summary>
        /// Obtiene la descripción legible de un modo de estructura.
        /// </summary>
        private string GetModeDescription(FolderStructureMode mode)
        {
            return mode switch
            {
                FolderStructureMode.Auto => "Automático",
                FolderStructureMode.Case1_GroupedByFolder => "Grupos en carpeta",
                FolderStructureMode.Case2_DistributedInSiblings => "Estilo en carpeta",
                FolderStructureMode.Case3_MixedInSingleFolder => "Todo en carpeta",
                _ => "Desconocido"
            };
        }

        #endregion
    }
}
#endif
