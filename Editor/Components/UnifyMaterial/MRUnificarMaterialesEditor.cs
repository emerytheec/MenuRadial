#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.AnimationSystem;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.UnifyMaterial
{
    /// <summary>
    /// Editor personalizado para MRUnificarMateriales.
    /// Estilo similar a MRUnificarObjetos con secciones plegables y lista reordenable.
    /// </summary>
    [CustomEditor(typeof(MRUnificarMateriales))]
    public class MRUnificarMaterialesEditor : UnityEditor.Editor
    {
        private MRUnificarMateriales _target;

        // Propiedades serializadas
        private SerializedProperty _animationNameProp;
        private SerializedProperty _animationPathProp;
        private SerializedProperty _alternativeMaterialsProp;

        // Lista reordenable
        private ReorderableList _reorderableList;

        // Secciones expandibles
        private bool _showMaterialsList = true;
        private bool _showAnimationSettings = true;

        // Preview
        private bool _isPreviewActive = false;
        private int _previewFrame = 0;
        private System.Collections.Generic.Dictionary<MRMaterialSlot, Material> _originalMaterials =
            new System.Collections.Generic.Dictionary<MRMaterialSlot, Material>();

        // Estilos
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        // Constantes
        private const float SECTION_SPACING = 10f;
        private const float BUTTON_HEIGHT = 25f;

        private void OnEnable()
        {
            _target = (MRUnificarMateriales)target;

            // Inicializar propiedades serializadas
            _animationNameProp = serializedObject.FindProperty("_animationName");
            _animationPathProp = serializedObject.FindProperty("_animationPath");
            _alternativeMaterialsProp = serializedObject.FindProperty("_alternativeMaterials");

            // Inicializar lista reordenable
            InitializeReorderableList();
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

        private void InitializeReorderableList()
        {
            _reorderableList = new ReorderableList(
                serializedObject,
                _alternativeMaterialsProp,
                true,  // draggable
                true,  // displayHeader
                true,  // displayAddButton
                true   // displayRemoveButton
            );

            // Header
            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, MRLocalization.Get(L.AlternativeMaterial.HEADER));
            };

            // Dibujar elemento
            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawListElement(rect, index);
            };

            // Altura del elemento
            _reorderableList.elementHeightCallback = (int index) =>
            {
                return EditorGUIUtility.singleLineHeight + 4;
            };

            // Al agregar
            _reorderableList.onAddCallback = (ReorderableList list) =>
            {
                _alternativeMaterialsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
            };

            // Al remover
            _reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < _alternativeMaterialsProp.arraySize)
                {
                    _alternativeMaterialsProp.DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                }
            };
        }

        private void DrawListElement(Rect rect, int index)
        {
            if (index >= _alternativeMaterialsProp.arraySize) return;

            var element = _alternativeMaterialsProp.GetArrayElementAtIndex(index);
            var altMat = element.objectReferenceValue as MRAgruparMateriales;

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            // Dividir rect
            float infoWidth = 120f;
            float buttonWidth = 30f;

            Rect objectRect = new Rect(rect.x, rect.y, rect.width - infoWidth - buttonWidth - 10, rect.height);
            Rect infoRect = new Rect(objectRect.xMax + 5, rect.y, infoWidth, rect.height);
            Rect selectRect = new Rect(infoRect.xMax + 5, rect.y, buttonWidth, rect.height);

            // Campo de objeto
            EditorGUI.PropertyField(objectRect, element, GUIContent.none);

            // Info del componente
            if (altMat != null)
            {
                int linkedSlots = altMat.LinkedSlotsCount;
                int groups = altMat.GroupCount;

                Color prevColor = GUI.color;
                GUI.color = linkedSlots > 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.8f, 0.5f);
                EditorGUI.LabelField(infoRect, $"{linkedSlots} slots, {groups} grupos", EditorStyles.miniLabel);
                GUI.color = prevColor;

                // Botón seleccionar (solo ping)
                if (GUI.Button(selectRect, EditorGUIUtility.IconContent("d_ViewToolOrbit"), EditorStyles.miniButton))
                {
                    EditorGUIUtility.PingObject(altMat.gameObject);
                }
            }
            else
            {
                EditorGUI.LabelField(infoRect, MRLocalization.Get(L.UnifyMaterial.EMPTY_SLOT), EditorStyles.miniLabel);
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _sectionStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };

            _stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            GUILayout.Space(SECTION_SPACING);

            // Sección: Preview
            DrawPreviewSection();

            GUILayout.Space(SECTION_SPACING);

            // Sección: Lista de MR Agrupar Materiales
            DrawMaterialsListSection();

            GUILayout.Space(SECTION_SPACING);

            // Sección: Ajustes de Animación
            DrawAnimationSettingsSection();

            serializedObject.ApplyModifiedProperties();
        }

        #region Preview

        private void DrawPreviewSection()
        {
            int totalLinkedSlots = _target.GetTotalLinkedSlots();
            bool canPreview = totalLinkedSlots > 0;

            // Slider con botones de navegación (estilo MRUnificarObjetos) - Siempre visible
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
                EditorGUILayout.HelpBox(MRLocalization.Get(L.UnifyMaterial.ADD_SLOTS_HINT), MessageType.Info);
            }
        }

        /// <summary>
        /// Asegura que el preview esté activo, capturando materiales originales si es necesario
        /// </summary>
        private void EnsurePreviewActive()
        {
            if (!_isPreviewActive)
            {
                CaptureOriginalMaterials();
                _isPreviewActive = true;
            }
        }

        private void TogglePreview()
        {
            if (_isPreviewActive)
            {
                // Desactivar
                RestoreOriginalMaterials();
                _isPreviewActive = false;
            }
            else
            {
                // Activar
                CaptureOriginalMaterials();
                _isPreviewActive = true;
                ApplyPreviewMaterials();
            }
        }

        private void CaptureOriginalMaterials()
        {
            _originalMaterials.Clear();

            var animationData = _target.CollectAnimationData();
            foreach (var data in animationData)
            {
                if (data.Slot == null || !data.Slot.IsValid) continue;
                if (data.Slot.TargetRenderer == null) continue;

                // Guardar material actual
                var renderer = data.Slot.TargetRenderer;
                var materials = renderer.sharedMaterials;

                if (data.Slot.MaterialIndex < materials.Length)
                {
                    _originalMaterials[data.Slot] = materials[data.Slot.MaterialIndex];
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

                var renderer = slot.TargetRenderer;
                var materials = renderer.sharedMaterials;

                if (slot.MaterialIndex < materials.Length)
                {
                    materials[slot.MaterialIndex] = originalMaterial;
                    renderer.sharedMaterials = materials;
                }
            }

            _originalMaterials.Clear();
            SceneView.RepaintAll();
        }

        private void ApplyPreviewMaterials()
        {
            var animationData = _target.CollectAnimationData();

            foreach (var data in animationData)
            {
                if (data.Slot == null || !data.Slot.IsValid) continue;
                if (data.Slot.TargetRenderer == null) continue;
                if (data.Materials == null || data.Materials.Count == 0) continue;

                // Encontrar qué material aplicar según el frame actual
                Material materialToApply = GetMaterialForFrame(data, _previewFrame);

                if (materialToApply != null)
                {
                    var renderer = data.Slot.TargetRenderer;
                    var materials = renderer.sharedMaterials;

                    if (data.Slot.MaterialIndex < materials.Length)
                    {
                        materials[data.Slot.MaterialIndex] = materialToApply;
                        renderer.sharedMaterials = materials;
                    }
                }
            }

            SceneView.RepaintAll();
        }

        private Material GetMaterialForFrame(UnifySlotAnimationData data, int frame)
        {
            // Buscar en qué rango de frames estamos
            foreach (var range in data.FrameDistribution)
            {
                if (frame >= range.StartFrame && frame <= range.EndFrame)
                {
                    if (range.MaterialIndex < data.Materials.Count)
                    {
                        return data.Materials[range.MaterialIndex];
                    }
                }
            }

            // Si no se encuentra, retornar el último material
            if (data.Materials.Count > 0)
            {
                return data.Materials[data.Materials.Count - 1];
            }

            return null;
        }

        #endregion

        #region Lista de MR Agrupar Materiales

        private void DrawMaterialsListSection()
        {
            string sectionTitle = MRLocalization.Get(L.UnifyMaterial.MATERIAL_GROUPS, _target.AlternativeMaterialCount);
            _showMaterialsList = EditorGUILayout.Foldout(_showMaterialsList, sectionTitle, _sectionStyle);

            if (_showMaterialsList)
            {
                EditorGUI.indentLevel++;

                // Área de drag & drop
                DrawDropArea();

                GUILayout.Space(10f);

                // Lista reordenable
                DrawMaterialsList();

                GUILayout.Space(5f);

                // Botones de gestión
                DrawManagementButtons();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawDropArea()
        {
            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));

            var dropStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray }
            };

            GUI.Box(dropRect, MRLocalization.Get(L.UnifyMaterial.DROP_ALTERNATIVE_MATERIALS), dropStyle);

            HandleDragAndDrop(dropRect);
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    bool hasValid = DragAndDrop.objectReferences.Any(obj =>
                        obj is GameObject || obj is MRAgruparMateriales);

                    DragAndDrop.visualMode = hasValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    PerformDrop(DragAndDrop.objectReferences);
                    evt.Use();
                    break;
            }
        }

        private void PerformDrop(Object[] objects)
        {
            Undo.RecordObject(_target, "Add Alternative Materials");

            int added = 0;
            foreach (var obj in objects)
            {
                if (obj is MRAgruparMateriales altMat)
                {
                    if (_target.AddAlternativeMaterial(altMat))
                        added++;
                }
                else if (obj is GameObject go)
                {
                    // Buscar en el GameObject y sus hijos
                    var altMats = go.GetComponentsInChildren<MRAgruparMateriales>(true);
                    foreach (var am in altMats)
                    {
                        if (_target.AddAlternativeMaterial(am))
                            added++;
                    }
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(_target);
                Debug.Log($"[MR Unify Material] Agregados {added} MR Alternative Material");
            }
        }

        private void DrawMaterialsList()
        {
            if (_target.AlternativeMaterialCount == 0)
            {
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.UnifyMaterial.DROP_OR_CREATE_HINT),
                    MessageType.Info);
                return;
            }

            _reorderableList.DoLayoutList();
        }

        private void DrawManagementButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Botón para crear nuevo Alternative Material
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f, 1f); // Verde

            if (GUILayout.Button(MRLocalization.Get(L.UnifyMaterial.CREATE_AGRUPAR_MATERIALES), _buttonStyle, GUILayout.Height(BUTTON_HEIGHT)))
            {
                _target.CreateAlternativeMaterial();
                serializedObject.Update();
            }

            GUI.backgroundColor = originalColor;

            if (GUILayout.Button(MRLocalization.Get(L.Radial.CLEANUP_NULL), _buttonStyle, GUILayout.Height(BUTTON_HEIGHT), GUILayout.Width(100)))
            {
                Undo.RecordObject(_target, "Remove Null References");
                int removed = _target.RemoveNullReferences();
                if (removed > 0)
                {
                    EditorUtility.SetDirty(_target);
                    Debug.Log($"[MR Unify Material] Eliminadas {removed} referencias nulas");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(MRLocalization.Get(L.UnifyMaterial.TIP_CREATE_CHILD), MessageType.Info);
        }

        #endregion

        #region Ajustes de Animación

        private void DrawAnimationSettingsSection()
        {
            _showAnimationSettings = EditorGUILayout.Foldout(_showAnimationSettings, MRLocalization.Get(L.Radial.ANIMATION_SETTINGS), _sectionStyle);

            if (_showAnimationSettings)
            {
                EditorGUI.indentLevel++;

                // Nombre y Ruta de animación
                DrawAnimationConfiguration();

                GUILayout.Space(10f);

                // Información de animación
                DrawAnimationInfo();

                GUILayout.Space(10f);

                // Botón generar
                DrawGenerateButton();

                EditorGUI.indentLevel--;
            }
        }

        private void DrawAnimationConfiguration()
        {
            // Nombre de Animación
            EditorGUILayout.PropertyField(_animationNameProp, new GUIContent(MRLocalization.Get(L.Radial.ANIMATION_NAME)));

            // Ruta de Animación
            EditorGUILayout.PropertyField(_animationPathProp, new GUIContent(MRLocalization.Get(L.Radial.ANIMATION_PATH)));
        }

        private void DrawAnimationInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(MRLocalization.Get(L.UnifyMaterial.ANIMATION_INFO), EditorStyles.boldLabel);

            int totalLinkedSlots = _target.GetTotalLinkedSlots();

            EditorGUILayout.LabelField(MRLocalization.Get(L.Radial.DURATION_INFO, 4.25f, 255));
            EditorGUILayout.LabelField(MRLocalization.Get(L.UnifyMaterial.ANIMATION_TYPE));
            EditorGUILayout.LabelField(MRLocalization.Get(L.UnifyMaterial.LINKED_SLOTS, totalLinkedSlots));

            if (totalLinkedSlots == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.UnifyMaterial.NO_LINKED_SLOTS), MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateButton()
        {
            bool canGenerate = _target.CanGenerateAnimation;

            EditorGUI.BeginDisabledGroup(!canGenerate);

            if (GUILayout.Button(MRLocalization.Get(L.Radial.GENERATE_ANIMATIONS), GUILayout.Height(35f)))
            {
                GenerateAnimation();
            }

            EditorGUI.EndDisabledGroup();

            // Info del sistema
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(MRLocalization.Get(L.UnifyMaterial.SYSTEM_INFO), EditorStyles.boldLabel);

            string info = _target.AlternativeMaterialCount == 0
                ? MRLocalization.Get(L.UnifyMaterial.ADD_ALTERNATIVE_MATERIAL_TO_START)
                : MRLocalization.Get(L.UnifyMaterial.CONFIGURED_WITH, _target.AlternativeMaterialCount, _target.GetTotalLinkedSlots());

            EditorGUILayout.LabelField(info, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private void GenerateAnimation()
        {
            if (!UnifyMaterialAnimationBuilder.CanGenerate(_target, out string errorMessage))
            {
                EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.ERROR), errorMessage, MRLocalization.Get(L.Common.OK));
                return;
            }

            try
            {
                var clip = UnifyMaterialAnimationBuilder.GenerateAnimation(_target);

                if (clip != null)
                {
                    EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.SUCCESS),
                        MRLocalization.Get(L.UnifyMaterial.ANIMATION_GENERATED, _target.FullAnimationPath), MRLocalization.Get(L.Common.OK));

                    var loadedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_target.FullAnimationPath);
                    if (loadedClip != null)
                    {
                        EditorGUIUtility.PingObject(loadedClip);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MR Unify Material] Error: {ex.Message}");
                EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.ERROR), MRLocalization.Get(L.UnifyMaterial.GENERATION_ERROR, ex.Message), MRLocalization.Get(L.Common.OK));
            }
        }

        #endregion
    }
}
#endif
