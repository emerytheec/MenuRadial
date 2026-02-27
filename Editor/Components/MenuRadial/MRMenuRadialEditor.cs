using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.MenuRadial;
using Bender_Dios.MenuRadial.Components.MenuRadial.Models;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Controllers;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.PesoTexturas;
using VRC.SDK3.Avatars.ScriptableObjects;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Editor personalizado para MRMenuRadial.
    /// Muestra el campo de avatar, indicadores de estado y botones de acción.
    /// </summary>
    [CustomEditor(typeof(MRMenuRadial))]
    public class MRMenuRadialEditor : UnityEditor.Editor
    {
        private SerializedProperty _avatarRootProperty;
        private SerializedProperty _autoDetectProperty;
        private SerializedProperty _autoGenerateMenuProperty;
        private SerializedProperty _outputPathProperty;
        private SerializedProperty _outputPrefixProperty;
        private SerializedProperty _writeDefaultValuesProperty;
        private SerializedProperty _disableBoneStitchingProperty;
        private SerializedProperty _disableVRChatMergeProperty;
        private SerializedProperty _disableModularAvatarProperty;

        // Menu integration properties
        private SerializedProperty _menuNameProperty;
        private SerializedProperty _menuIconProperty;
        private SerializedProperty _menuIntegrationModeProperty;
        private SerializedProperty _targetSubMenuIndexProperty;
        private SerializedProperty _customMenuPathProperty;

        private MRMenuRadial _target;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _statusLabelStyle;
        private bool _stylesInitialized;

        // Cache tipado para MRMenuControl
        private MRMenuControl _menuControlTyped;

        // Flag one-shot por instancia del editor
        private bool _hasAutoCreatedChildren;
        private bool _showNDMFPanel;

        private void OnEnable()
        {
            _target = (MRMenuRadial)target;
            _avatarRootProperty = serializedObject.FindProperty("_avatarRoot");
            _autoDetectProperty = serializedObject.FindProperty("_autoDetectOnAvatarAssign");
            _autoGenerateMenuProperty = serializedObject.FindProperty("_autoGenerateMenuStructure");
            _outputPathProperty = serializedObject.FindProperty("_outputPath");
            _outputPrefixProperty = serializedObject.FindProperty("_outputPrefix");
            _writeDefaultValuesProperty = serializedObject.FindProperty("_writeDefaultValues");
            _disableBoneStitchingProperty = serializedObject.FindProperty("_disableBoneStitchingNDMF");
            _disableVRChatMergeProperty = serializedObject.FindProperty("_disableVRChatMergeNDMF");
            _disableModularAvatarProperty = serializedObject.FindProperty("_disableModularAvatarNDMF");

            // Menu integration properties
            _menuNameProperty = serializedObject.FindProperty("_menuName");
            _menuIconProperty = serializedObject.FindProperty("_menuIcon");
            _menuIntegrationModeProperty = serializedObject.FindProperty("_menuIntegrationMode");
            _targetSubMenuIndexProperty = serializedObject.FindProperty("_targetSubMenuIndex");
            _customMenuPathProperty = serializedObject.FindProperty("_customMenuPath");

            RefreshMenuControlCache();
        }

        private void RefreshMenuControlCache()
        {
            _menuControlTyped = _target.GetComponentInChildren<MRMenuControl>();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _statusLabelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            _stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            serializedObject.Update();

            // One-shot: al abrir el editor con avatar asignado, asegurar que la cadena
            // PropagateAvatarToChildren se ejecute al menos una vez.
            // Dos escenarios:
            //   1) Sin hijos (Add Component) → RecreateChildComponents crea hijos + propaga
            //   2) Con hijos (MRMenuRadialCreator) → solo propaga para generar estructura
            if (!_hasAutoCreatedChildren && _target.AvatarRoot != null)
            {
                _hasAutoCreatedChildren = true;

                bool allChildrenMissing = _target.AnalisisColision == null
                                       && _target.CoserRopa == null
                                       && _target.OrganizaPB == null
                                       && _menuControlTyped == null
                                       && _target.AjustarBounds == null
                                       && _target.PesoTexturas == null;

                if (allChildrenMissing)
                {
                    // Caso 1: crear hijos primero, luego propagar
                    RecreateChildComponents();
                }
                else
                {
                    // Caso 2: hijos ya existen (ej: MRMenuRadialCreator),
                    // pero Menu Control puede estar vacío — propagar y generar
                    _target.PropagateAvatarToChildren();
                }

                RefreshMenuControlCache();
                serializedObject.Update();
            }

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawAvatarField();
            EditorGUILayout.Space(10);

            DrawActionButtons();
            EditorGUILayout.Space(10);

            DrawStatusPanel();
            EditorGUILayout.Space(10);

            DrawChildComponentsPanel();
            EditorGUILayout.Space(10);

            DrawOutputPathField();
            EditorGUILayout.Space(10);

            DrawMenuIntegrationSection();
            EditorGUILayout.Space(10);

            DrawNDMFControlPanel();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.TITLE), _headerStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.SUBTITLE), EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawAvatarField()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.AVATAR_LABEL), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_avatarRootProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.AVATAR_LABEL), MRLocalization.Get(L.MenuRadialEditor.AVATAR_ROOT_TOOLTIP)));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _target.PropagateAvatarToChildren();
                // Refrescar serializedObject para mostrar el OutputPrefix auto-asignado
                serializedObject.Update();
                RefreshMenuControlCache();
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.PropertyField(_autoDetectProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.AUTO_DETECT), MRLocalization.Get(L.MenuRadialEditor.AUTO_DETECT_TOOLTIP)));

            EditorGUILayout.PropertyField(_autoGenerateMenuProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.AUTO_GENERATE_MENU), MRLocalization.Get(L.MenuRadialEditor.AUTO_GENERATE_MENU_TOOLTIP)));

            // Mostrar estado del avatar
            if (_avatarRootProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.DROP_AVATAR_HINT), MessageType.Info);
            }
            else
            {
                var avatarGO = _avatarRootProperty.objectReferenceValue as GameObject;
                if (avatarGO != null)
                {
                    var descriptor = avatarGO.GetComponent("VRC_AvatarDescriptor")
                                  ?? avatarGO.GetComponent("VRCAvatarDescriptor");

                    if (descriptor != null)
                    {
                        EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.AVATAR_DETECTED, avatarGO.name), MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.NO_DESCRIPTOR), MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputPathField()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.NAMESPACE), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_outputPrefixProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.OUTPUT_PREFIX), MRLocalization.Get(L.MenuRadialEditor.OUTPUT_PREFIX_TOOLTIP)));
            EditorGUILayout.PropertyField(_outputPathProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.OUTPUT_PATH), MRLocalization.Get(L.MenuRadialEditor.OUTPUT_PATH_TOOLTIP)));

            string outputDir = _target.GetVRChatOutputDirectory();
            EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.OUTPUT_PREVIEW, outputDir), MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_writeDefaultValuesProperty, new GUIContent(MRLocalization.Get(L.MenuRadialEditor.WRITE_DEFAULTS), MRLocalization.Get(L.MenuRadialEditor.WRITE_DEFAULTS_TOOLTIP)));

            EditorGUILayout.EndVertical();
        }

        private void DrawMenuIntegrationSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.MENU_INTEGRATION), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.MENU_INTEGRATION_HINT), MessageType.Info);

            EditorGUILayout.Space(5);

            // Nombre del menú
            EditorGUILayout.PropertyField(_menuNameProperty, new GUIContent(
                MRLocalization.Get(L.MenuRadialEditor.MENU_NAME),
                MRLocalization.Get(L.MenuRadialEditor.MENU_NAME_TOOLTIP)));

            // Preview del nombre efectivo
            string effectiveName = _target.EffectiveMenuName;
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.EFFECTIVE_NAME), effectiveName, EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Icono del menú
            EditorGUILayout.PropertyField(_menuIconProperty, new GUIContent(
                MRLocalization.Get(L.MenuRadialEditor.MENU_ICON),
                MRLocalization.Get(L.MenuRadialEditor.MENU_ICON_TOOLTIP)));

            EditorGUILayout.Space(10);

            // Modo de integración
            EditorGUILayout.PropertyField(_menuIntegrationModeProperty, new GUIContent(
                MRLocalization.Get(L.MenuRadialEditor.LOCATION),
                MRLocalization.Get(L.MenuRadialEditor.LOCATION_TOOLTIP)));

            EditorGUILayout.Space(5);

            // Mostrar opciones según el modo seleccionado
            var integrationMode = (MenuIntegrationMode)_menuIntegrationModeProperty.enumValueIndex;

            switch (integrationMode)
            {
                case MenuIntegrationMode.RootMenu:
                    EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.ROOT_MENU_HINT), MessageType.None);
                    break;

                case MenuIntegrationMode.ExistingSubMenu:
                    DrawExistingSubMenuSelector();
                    break;

                case MenuIntegrationMode.CustomPath:
                    EditorGUILayout.PropertyField(_customMenuPathProperty, new GUIContent(
                        MRLocalization.Get(L.MenuRadialEditor.CUSTOM_PATH),
                        MRLocalization.Get(L.MenuRadialEditor.CUSTOM_PATH_TOOLTIP)));

                    if (!string.IsNullOrEmpty(_customMenuPathProperty.stringValue))
                    {
                        EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.CUSTOM_PATH_PREVIEW, _customMenuPathProperty.stringValue, effectiveName), MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.ENTER_PATH), MessageType.Warning);
                    }
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExistingSubMenuSelector()
        {
            // Obtener el menú del avatar para listar los submenús existentes
            var avatarGO = _avatarRootProperty.objectReferenceValue as GameObject;
            if (avatarGO == null)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.ASSIGN_AVATAR_FIRST), MessageType.Warning);
                return;
            }

            // Buscar VRCAvatarDescriptor
            var descriptor = avatarGO.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.NO_DESCRIPTOR_MENU), MessageType.Warning);
                return;
            }

            var expressionsMenu = descriptor.expressionsMenu;
            if (expressionsMenu == null || expressionsMenu.controls == null || expressionsMenu.controls.Count == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.NO_EXPRESSIONS_MENU), MessageType.Warning);
                return;
            }

            // Construir lista de submenús
            var subMenuOptions = new System.Collections.Generic.List<string>();
            var subMenuIndices = new System.Collections.Generic.List<int>();

            for (int i = 0; i < expressionsMenu.controls.Count; i++)
            {
                var control = expressionsMenu.controls[i];
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                {
                    subMenuOptions.Add($"{i}: {control.name}");
                    subMenuIndices.Add(i);
                }
            }

            if (subMenuOptions.Count == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.NO_SUBMENUS), MessageType.Warning);
                return;
            }

            // Encontrar el índice actual en la lista
            int currentIndex = subMenuIndices.IndexOf(_targetSubMenuIndexProperty.intValue);
            if (currentIndex < 0) currentIndex = 0;

            // Dropdown para seleccionar
            int newIndex = EditorGUILayout.Popup(
                new GUIContent(MRLocalization.Get(L.MenuRadialEditor.TARGET_SUBMENU), MRLocalization.Get(L.MenuRadialEditor.TARGET_SUBMENU_TOOLTIP)),
                currentIndex,
                subMenuOptions.ToArray());

            if (newIndex >= 0 && newIndex < subMenuIndices.Count)
            {
                _targetSubMenuIndexProperty.intValue = subMenuIndices[newIndex];
            }

            // Mostrar info del submenú seleccionado
            if (_targetSubMenuIndexProperty.intValue >= 0 && _targetSubMenuIndexProperty.intValue < expressionsMenu.controls.Count)
            {
                var selectedControl = expressionsMenu.controls[_targetSubMenuIndexProperty.intValue];
                int controlCount = selectedControl.subMenu?.controls?.Count ?? 0;
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.SUBMENU_INFO, selectedControl.name, controlCount), MessageType.None);
            }
        }

        private void DrawNDMFControlPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showNDMFPanel = EditorGUILayout.Foldout(_showNDMFPanel, MRLocalization.Get(L.MenuRadialEditor.NDMF_CONTROL), true, EditorStyles.foldoutHeader);

            if (_showNDMFPanel)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.NDMF_CONTROL_HINT), MessageType.Info);

                EditorGUILayout.Space(5);

                // Checkbox para desactivar cosido de huesos
                EditorGUILayout.PropertyField(_disableBoneStitchingProperty, new GUIContent(
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_BONE_STITCHING),
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_BONE_STITCHING_TOOLTIP)));

                // Checkbox para desactivar merge de VRChat
                EditorGUILayout.PropertyField(_disableVRChatMergeProperty, new GUIContent(
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_VRCHAT_MERGE),
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_VRCHAT_MERGE_TOOLTIP)));

                EditorGUILayout.Space(10);

                // Separador visual
                EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.OTHER_PLUGINS), EditorStyles.boldLabel);

                // Checkbox para desactivar Modular Avatar
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_disableModularAvatarProperty, new GUIContent(
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_MA),
                    MRLocalization.Get(L.MenuRadialEditor.DISABLE_MA_TOOLTIP)));

                if (EditorGUI.EndChangeCheck() && _disableModularAvatarProperty.boolValue)
                {
                    // Mostrar advertencia al activar
                    EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.WARNING),
                        MRLocalization.Get(L.MenuRadialEditor.DISABLE_MA_WARNING),
                        MRLocalization.Get(L.AnalisisColision.UNDERSTOOD));
                }

                // Mostrar advertencia si alguno está activado
                bool anyDisabled = _disableBoneStitchingProperty.boolValue ||
                                  _disableVRChatMergeProperty.boolValue ||
                                  _disableModularAvatarProperty.boolValue;

                if (anyDisabled)
                {
                    EditorGUILayout.Space(5);
                    string warning = MRLocalization.Get(L.MenuRadialEditor.DISABLED_PROCESSES) + "\n";
                    if (_disableBoneStitchingProperty.boolValue)
                        warning += "• " + MRLocalization.Get(L.MenuRadialEditor.DISABLED_BONE_STITCHING) + "\n";
                    if (_disableVRChatMergeProperty.boolValue)
                        warning += "• " + MRLocalization.Get(L.MenuRadialEditor.DISABLED_VRCHAT_MERGE) + "\n";
                    if (_disableModularAvatarProperty.boolValue)
                        warning += "• " + MRLocalization.Get(L.MenuRadialEditor.DISABLED_MA) + "\n";
                    warning += "\n" + MRLocalization.Get(L.MenuRadialEditor.NOT_APPLIED);

                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.STATUS), EditorStyles.boldLabel);

            // Ropas
            DrawStatusLine(
                MRLocalization.Get(L.MenuRadialEditor.CLOTHING_DETECTED),
                _target.DetectedPieceCount > 0,
                $"{_target.EnabledPieceCount}/{_target.DetectedPieceCount}",
                _target.DetectedPieceCount == 0 ? MRLocalization.Get(L.MenuRadialEditor.NO_CLOTHING) : null
            );

            // PhysBones
            string pbStatus = GetPhysBonesStatusText();
            bool pbOk = _target.IsPhysBonesOrganized || _target.DetectedPhysBonesCount == 0;
            DrawStatusLine(
                MRLocalization.Get(L.MenuRadialEditor.PHYS_BONES),
                pbOk,
                pbStatus,
                null
            );

            // Bounds
            DrawStatusLine(
                MRLocalization.Get(L.MenuRadialEditor.BOUNDS),
                _target.IsBoundsApplied,
                _target.IsBoundsApplied ? MRLocalization.Get(L.MenuRadialEditor.BOUNDS_APPLIED, _target.DetectedMeshesCount) : MRLocalization.Get(L.MenuRadialEditor.PENDING),
                _target.DetectedMeshesCount == 0 ? "Sin meshes" : null
            );

            // Menú
            DrawStatusLine(
                MRLocalization.Get(L.MenuRadialEditor.MENU_CONFIGURED),
                _target.MenuSlotCount > 0,
                $"{_target.MenuSlotCount} slots",
                _target.MenuSlotCount == 0 ? MRLocalization.Get(L.MenuRadialEditor.NO_SLOTS) : null
            );

            EditorGUILayout.EndVertical();
        }

        private string GetPhysBonesStatusText()
        {
            switch (_target.PhysBonesState)
            {
                case OrganizationState.NotScanned:
                    return MRLocalization.Get(L.MenuRadialEditor.PB_NOT_SCANNED);
                case OrganizationState.Scanned:
                    // Si todos están pre-organizados, mostrar como organizados
                    if (_target.IsPhysBonesOrganized)
                        return MRLocalization.Get(L.MenuRadialEditor.PB_ORGANIZED, _target.DetectedPhysBonesCount);
                    return MRLocalization.Get(L.MenuRadialEditor.PB_DETECTED, _target.DetectedPhysBonesCount);
                case OrganizationState.Organized:
                    return MRLocalization.Get(L.MenuRadialEditor.PB_ORGANIZED, _target.DetectedPhysBonesCount);
                default:
                    return MRLocalization.Get(L.MenuRadialEditor.UNKNOWN);
            }
        }

        private void DrawStatusLine(string label, bool isOk, string value, string alternativeText = null)
        {
            EditorGUILayout.BeginHorizontal();

            // Icono
            var icon = isOk
                ? EditorGUIUtility.IconContent("d_greenLight")
                : EditorGUIUtility.IconContent("d_orangeLight");
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));

            // Label
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            // Valor
            string displayText = alternativeText ?? value;
            EditorGUILayout.LabelField(displayText, _statusLabelStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.ACTIONS), EditorStyles.boldLabel);

            bool hasAvatar = _avatarRootProperty.objectReferenceValue != null;
            bool hasExistingStructure = _target.MenuSlotCount > 0;
            bool hasPieces = _target.DetectedPieceCount > 0;

            EditorGUI.BeginDisabledGroup(!hasAvatar);

            // Botón Sincronizar: crea estructura si no existe, agrega lo que falte si existe
            EditorGUI.BeginDisabledGroup(!hasPieces && !hasExistingStructure);
            var prevBgSync = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.6f, 1f);
            if (GUILayout.Button(new GUIContent(
                MRLocalization.Get(L.MenuRadialEditor.SYNC_STRUCTURE),
                MRLocalization.Get(L.MenuRadialEditor.SYNC_STRUCTURE_TOOLTIP)),
                GUILayout.Height(28)))
            {
                Undo.RecordObject(_target, "Sync Menu Structure");

                if (!hasExistingStructure)
                {
                    // No hay estructura: crear desde cero
                    var genResult = _target.GenerateMenuStructure();
                    if (genResult.Success)
                    {
                        string infoMessage = genResult.WigFramesCreated > 0
                            ? MRLocalization.Get(L.MenuRadialEditor.STRUCTURE_GENERATED_INFO_WITH_WIGS, genResult.PieceFramesCreated, genResult.WigFramesCreated, genResult.AvatarMeshesIncluded, genResult.AvatarMeshesExcluded)
                            : MRLocalization.Get(L.MenuRadialEditor.STRUCTURE_GENERATED_INFO, genResult.PieceFramesCreated, genResult.AvatarMeshesIncluded, genResult.AvatarMeshesExcluded);
                        EditorUtility.DisplayDialog(MRLocalization.Get(L.MenuRadialEditor.STRUCTURE_GENERATED),
                            infoMessage,
                            MRLocalization.Get(L.Common.OK));
                    }
                }
                else
                {
                    // Ya hay estructura: sincronizar (agregar lo que falte)
                    var syncResult = _target.SyncMenuStructure();
                    if (syncResult.Success)
                    {
                        // Re-escanear AnalisisColision para detectar cambios en componentes MA
                        if (_target.AnalisisColision != null)
                        {
                            Undo.RecordObject(_target.AnalisisColision, "Refresh Analisis Colision");
                            _target.AnalisisColision.Refresh();
                            EditorUtility.SetDirty(_target.AnalisisColision);
                        }

                        string message;
                        if (syncResult.FramesAdded > 0 && syncResult.WigFramesAdded > 0)
                        {
                            message = MRLocalization.Get(L.MenuRadialEditor.SYNC_RESULT_ADDED_WITH_WIGS,
                                syncResult.FramesAdded, string.Join(", ", syncResult.AddedPieceNames),
                                syncResult.WigFramesAdded, string.Join(", ", syncResult.AddedWigNames));
                        }
                        else if (syncResult.WigFramesAdded > 0)
                        {
                            message = MRLocalization.Get(L.MenuRadialEditor.SYNC_RESULT_WIGS_ADDED,
                                syncResult.WigFramesAdded, string.Join(", ", syncResult.AddedWigNames));
                        }
                        else if (syncResult.FramesAdded > 0)
                        {
                            message = MRLocalization.Get(L.MenuRadialEditor.SYNC_RESULT_ADDED,
                                syncResult.FramesAdded, string.Join(", ", syncResult.AddedPieceNames));
                        }
                        else
                        {
                            message = MRLocalization.Get(L.MenuRadialEditor.SYNC_NO_CHANGES);
                        }
                        if (syncResult.OrphanedFrames > 0)
                            message += "\n\n" + MRLocalization.Get(L.MenuRadialEditor.SYNC_ORPHANED_WARNING, syncResult.OrphanedFrames, string.Join(", ", syncResult.OrphanedFrameNames));
                        EditorUtility.DisplayDialog(
                            MRLocalization.Get(L.MenuRadialEditor.SYNC_RESULT_TITLE),
                            message,
                            MRLocalization.Get(L.Common.OK));
                    }
                }

                // Organizar PhysBones si aún no están organizados
                RefreshOrganizaPB();

                // Escanear, calcular y aplicar bounds + anchor (en ambas ramas)
                RefreshAjustarBounds();

                RefreshMenuControlCache();
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = prevBgSync;
            EditorGUI.EndDisabledGroup();

            if (!hasPieces && hasAvatar)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.DETECT_CLOTHING_FIRST), MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Botón Generar Archivos VRChat
            bool canGenerate = hasAvatar && _target.MenuSlotCount > 0;
            EditorGUI.BeginDisabledGroup(!canGenerate);
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button(new GUIContent(MRLocalization.Get(L.MenuRadialEditor.GENERATE_VRCHAT), MRLocalization.Get(L.MenuRadialEditor.GENERATE_VRCHAT_TOOLTIP)), GUILayout.Height(30)))
            {
                bool success = _target.GenerateVRChatFiles();
                if (success)
                {
                    EditorUtility.DisplayDialog(
                        MRLocalization.Get(L.Common.SUCCESS),
                        MRLocalization.Get(L.MenuRadialEditor.GENERATE_VRCHAT_SUCCESS),
                        MRLocalization.Get(L.Common.OK));
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        MRLocalization.Get(L.Common.ERROR),
                        MRLocalization.Get(L.MenuRadialEditor.GENERATE_VRCHAT_ERROR),
                        MRLocalization.Get(L.Common.OK));
                }
            }
            GUI.backgroundColor = prevBg;
            EditorGUI.EndDisabledGroup();

            if (!canGenerate && hasAvatar)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.MenuRadialEditor.CONFIGURE_SLOT_FIRST), MessageType.Info);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void RefreshOrganizaPB()
        {
            if (_target.OrganizaPB == null) return;

            var pb = _target.OrganizaPB;
            Undo.RecordObject(pb, "Refresh Organiza PB");

            // Escanear si aún no se ha escaneado
            if (pb.State == OrganizationState.NotScanned)
                pb.ScanAvatar();

            // Organizar si está escaneado y hay componentes para organizar
            if (pb.CanOrganize)
            {
                var result = pb.Organize();
                if (result.Success)
                    Debug.Log($"[MRMenuRadial] PhysBones organizados: {result.GetSummary()}");
            }

            // Vincular contenedores a frames
            if (pb.HasDetectedComponents)
                PhysBoneFrameLinker.LinkContainersToFrames(pb, _target);

            EditorUtility.SetDirty(pb);
        }

        private void RefreshAjustarBounds()
        {
            if (_target.AjustarBounds == null) return;

            var ab = _target.AjustarBounds;
            Undo.RecordObject(ab, "Refresh Ajustar Bounds");

            // Registrar Undo en renderers existentes antes de que Refresh los modifique
            foreach (var meshInfo in ab.DetectedMeshes)
            {
                if (meshInfo.IsValid && meshInfo.Renderer != null)
                {
                    Undo.RecordObject(meshInfo.Renderer, "Refresh Ajustar Bounds");
                    Undo.RecordObject(meshInfo.Renderer.transform, "Refresh Ajustar Bounds");
                }
            }
            foreach (var particleInfo in ab.DetectedParticles)
            {
                if (particleInfo.IsValid && particleInfo.Renderer != null)
                    Undo.RecordObject(particleInfo.Renderer, "Refresh Ajustar Bounds");
            }

            // Refresh: restaurar → re-detectar rootBone → escanear → calcular → re-aplicar (si ya estaban aplicados)
            ab.Refresh();

            // Registrar Undo en renderers nuevos (descubiertos por el scan) antes de aplicar
            foreach (var meshInfo in ab.DetectedMeshes)
            {
                if (meshInfo.IsValid && meshInfo.Renderer != null)
                {
                    Undo.RecordObject(meshInfo.Renderer, "Refresh Ajustar Bounds");
                    Undo.RecordObject(meshInfo.Renderer.transform, "Refresh Ajustar Bounds");
                }
            }

            // Si Refresh no aplicó (primera vez), aplicar ahora
            if (!ab.BoundsApplied && ab.HasValidCalculation)
            {
                ab.ApplyBounds();
                ab.ApplyAnchorOverride();

                if (ab.IncludeParticles)
                {
                    foreach (var particleInfo in ab.DetectedParticles)
                    {
                        if (particleInfo.IsValid && particleInfo.Renderer != null)
                            Undo.RecordObject(particleInfo.Renderer, "Refresh Ajustar Bounds");
                    }
                    ab.ApplyParticleBounds();
                }
            }

            EditorUtility.SetDirty(ab);
        }

        private void DrawChildComponentsPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.CHILD_COMPONENTS), EditorStyles.boldLabel);

            // Analisis Colision con indicador especial si hay conflictos
            bool hasColisionComponent = _target.AnalisisColision != null;
            DrawComponentStatus("MR Analisis Colision", hasColisionComponent);

            // Mostrar indicador de conflictos si hay problematicos
            if (hasColisionComponent && _target.HasProblematicColisions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                GUI.contentColor = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField(MRLocalization.Get(L.MenuRadialEditor.CONFLICTS_DETECTED, _target.AnalisisColision.ProblematicCount), EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            DrawComponentStatus("MR Coser Ropa", _target.CoserRopa != null);
            DrawComponentStatus("MR Organiza PB", _target.OrganizaPB != null);
            DrawComponentStatus("MR Menu Control", _menuControlTyped != null);
            DrawComponentStatus("MR Ajustar Bounds", _target.AjustarBounds != null);
            DrawComponentStatus("MR Peso Texturas", _target.PesoTexturas != null);

            // Verificar si faltan componentes
            bool missingComponents = _target.AnalisisColision == null ||
                                     _target.CoserRopa == null ||
                                     _target.OrganizaPB == null ||
                                     _menuControlTyped == null ||
                                     _target.AjustarBounds == null ||
                                     _target.PesoTexturas == null;

            if (missingComponents)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button(MRLocalization.Get(L.MenuRadialEditor.RECREATE_COMPONENTS)))
                {
                    RecreateChildComponents();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentStatus(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();

            var icon = exists
                ? EditorGUIUtility.IconContent("d_greenLight")
                : EditorGUIUtility.IconContent("d_redLight");
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
            EditorGUILayout.LabelField(name);

            EditorGUILayout.EndHorizontal();
        }

        private void RecreateChildComponents()
        {
            Undo.RecordObject(_target.gameObject, "Recreate MR Child Components");

            // Crear componentes hijos que faltan (EXCEPTO Menu Control)
            // Menu Control se crea SOLO via la cadena:
            //   PropagateAvatarToChildren → AutoDetectAll → GenerateMenuStructure → GetOrCreateMenuControl
            // Esto garantiza UNA sola ruta de creación y evita duplicados.

            // Analisis Colision debe ser el primer hijo
            if (_target.AnalisisColision == null)
            {
                var go = new GameObject("Analisis Colision");
                go.transform.SetParent(_target.transform);
                go.transform.SetAsFirstSibling();
                go.AddComponent<MRAnalisisColision>();
                Undo.RegisterCreatedObjectUndo(go, "Create Analisis Colision");
            }

            if (_target.CoserRopa == null)
            {
                var go = new GameObject("Coser Ropa");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MRCoserRopa>();
                Undo.RegisterCreatedObjectUndo(go, "Create Coser Ropa");
            }

            if (_target.OrganizaPB == null)
            {
                var go = new GameObject("Organiza PB");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MROrganizaPB>();
                Undo.RegisterCreatedObjectUndo(go, "Create Organiza PB");
            }

            if (_target.AjustarBounds == null)
            {
                var go = new GameObject("Ajustar Bounds");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MRAjustarBounds>();
                Undo.RegisterCreatedObjectUndo(go, "Create Ajustar Bounds");
            }

            if (_target.PesoTexturas == null)
            {
                var go = new GameObject("Peso Texturas");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MRPesoTexturas>();
                Undo.RegisterCreatedObjectUndo(go, "Create Peso Texturas");
            }

            _target.InvalidateCache();

            // Con avatar: PropagateAvatarToChildren ejecuta la cadena completa
            // que crea Menu Control (si no existe) y genera su estructura.
            // Sin avatar: crear Menu Control vacío directamente (para uso posterior).
            if (_target.AvatarRoot != null)
            {
                _target.PropagateAvatarToChildren();
            }
            else
            {
                RefreshMenuControlCache();
                if (_menuControlTyped == null)
                {
                    var go = new GameObject("Menu Control");
                    go.transform.SetParent(_target.transform);
                    go.AddComponent<MRMenuControl>();
                    Undo.RegisterCreatedObjectUndo(go, "Create Menu Control");
                }
            }

            RefreshMenuControlCache();
        }
    }
}
