using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

namespace Bender_Dios.MenuRadial.Editor.Components.OrganizaPB
{
    /// <summary>
    /// Editor personalizado para MROrganizaPB.
    /// Muestra listas de PhysBones y Colliders detectados con controles para habilitarlos.
    /// </summary>
    [CustomEditor(typeof(MROrganizaPB))]
    public class MROrganizaPBEditor : UnityEditor.Editor
    {
        #region Private Fields

        private MROrganizaPB _target;
        private SerializedProperty _avatarRootProp;
        private SerializedProperty _physBonesProp;
        private SerializedProperty _collidersProp;

        private ReorderableList _physBonesList;
        private ReorderableList _collidersList;

        private bool _showPhysBonesFoldout = true;
        private bool _showCollidersFoldout = true;
        private bool _showStatsFoldout = false;

        #endregion

        #region Constants

        private const float ITEM_HEIGHT = 22f;
        private const float TOGGLE_WIDTH = 18f;
        private const float CONTEXT_WIDTH = 80f;
        private const float ROOT_BONE_WIDTH = 100f;

        private static readonly Color EnabledColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color DisabledColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color AvatarContextColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ClothingContextColor = new Color(1f, 0.7f, 0.4f);

        #endregion

        #region Initialization

        private void OnEnable()
        {
            _target = (MROrganizaPB)target;
            _avatarRootProp = serializedObject.FindProperty("_avatarRoot");
            _physBonesProp = serializedObject.FindProperty("_detectedPhysBones");
            _collidersProp = serializedObject.FindProperty("_detectedColliders");

            InitializePhysBonesList();
            InitializeCollidersList();
        }

        private void InitializePhysBonesList()
        {
            _physBonesList = new ReorderableList(serializedObject, _physBonesProp, false, true, false, false)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"PhysBones ({_physBonesProp.arraySize})"),
                drawElementCallback = DrawPhysBoneElement,
                elementHeight = ITEM_HEIGHT + 4f,
                drawElementBackgroundCallback = DrawElementBackground
            };
        }

        private void InitializeCollidersList()
        {
            _collidersList = new ReorderableList(serializedObject, _collidersProp, false, true, false, false)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Colliders ({_collidersProp.arraySize})"),
                drawElementCallback = DrawColliderElement,
                elementHeight = ITEM_HEIGHT + 4f,
                drawElementBackgroundCallback = DrawElementBackground
            };
        }

        #endregion

        #region Inspector GUI

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(5);

            DrawAvatarSection();

            if (_target.AvatarRoot != null)
            {
                EditorGUILayout.Space(8);
                DrawSDKStatus();
                EditorGUILayout.Space(5);
                DrawScanSection();

                if (_target.HasDetectedComponents)
                {
                    EditorGUILayout.Space(8);
                    DrawOrganizeSection();
                    EditorGUILayout.Space(8);
                    DrawPhysBonesSection();
                    EditorGUILayout.Space(5);
                    DrawCollidersSection();
                    EditorGUILayout.Space(8);
                    DrawStatsSection();
                }

                if (_target.LastResult != null)
                {
                    EditorGUILayout.Space(5);
                    DrawLastResult();
                }
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Arrastra tu avatar aquí para comenzar.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MR Organiza PhysBones", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Reorganiza PhysBones para control desde MRAgruparObjetos",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawAvatarSection()
        {
            EditorGUI.BeginChangeCheck();
            var newAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar", _target.AvatarRoot, typeof(GameObject), true);

            if (EditorGUI.EndChangeCheck() && newAvatar != _target.AvatarRoot)
            {
                Undo.RecordObject(_target, "Cambiar Avatar");
                _target.AvatarRoot = newAvatar;
                EditorUtility.SetDirty(_target);
            }
        }

        private void DrawSDKStatus()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("VRChat SDK:", GUILayout.Width(80));

                if (_target.IsSDKAvailable)
                {
                    GUI.contentColor = EnabledColor;
                    EditorGUILayout.LabelField("Disponible", EditorStyles.boldLabel);
                }
                else
                {
                    GUI.contentColor = WarningColor;
                    EditorGUILayout.LabelField("No disponible", EditorStyles.boldLabel);
                }
                GUI.contentColor = Color.white;
            }
        }

        private void DrawScanSection()
        {
            // No permitir escanear si ya está organizado
            using (new EditorGUI.DisabledGroupScope(_target.IsOrganized))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Escanear Avatar", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(_target, "Escanear PhysBones");
                        _target.ScanAvatar();
                        EditorUtility.SetDirty(_target);
                    }

                    if (_target.HasDetectedComponents && !_target.IsOrganized)
                    {
                        if (GUILayout.Button("Limpiar", GUILayout.Width(60), GUILayout.Height(25)))
                        {
                            Undo.RecordObject(_target, "Limpiar detecciones");
                            _target.ClearDetection();
                            EditorUtility.SetDirty(_target);
                        }
                    }
                }
            }

            if (_target.HasDetectedComponents)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    $"Detectados: {_target.DetectedPhysBones.Count} PhysBones, {_target.DetectedColliders.Count} Colliders",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawOrganizeSection()
        {
            EditorGUILayout.Space(5);

            // Mostrar estado actual
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Estado:", GUILayout.Width(50));

                switch (_target.State)
                {
                    case Bender_Dios.MenuRadial.Components.OrganizaPB.Models.OrganizationState.NotScanned:
                        GUI.contentColor = WarningColor;
                        EditorGUILayout.LabelField("No escaneado", EditorStyles.boldLabel);
                        break;
                    case Bender_Dios.MenuRadial.Components.OrganizaPB.Models.OrganizationState.Scanned:
                        GUI.contentColor = new Color(1f, 0.8f, 0.4f);
                        EditorGUILayout.LabelField("Escaneado (no organizado)", EditorStyles.boldLabel);
                        break;
                    case Bender_Dios.MenuRadial.Components.OrganizaPB.Models.OrganizationState.Organized:
                        GUI.contentColor = EnabledColor;
                        EditorGUILayout.LabelField("Organizado", EditorStyles.boldLabel);
                        break;
                }
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.Space(5);

            // Botones de acción
            using (new EditorGUILayout.HorizontalScope())
            {
                // Botón Organizar
                using (new EditorGUI.DisabledGroupScope(!_target.CanOrganize))
                {
                    var organizeStyle = new GUIStyle(GUI.skin.button);
                    organizeStyle.fontStyle = FontStyle.Bold;

                    if (GUILayout.Button("Organizar PhysBones", organizeStyle, GUILayout.Height(30)))
                    {
                        Undo.RecordObject(_target, "Organizar PhysBones");

                        // Registrar todos los objetos que podrían ser modificados
                        foreach (var pb in _target.DetectedPhysBones)
                        {
                            if (pb.OriginalComponent != null)
                                Undo.RecordObject(pb.OriginalComponent, "Organizar PhysBones");
                        }
                        foreach (var col in _target.DetectedColliders)
                        {
                            if (col.OriginalComponent != null)
                                Undo.RecordObject(col.OriginalComponent, "Organizar PhysBones");
                        }

                        var result = _target.Organize();
                        EditorUtility.SetDirty(_target);

                        if (result.Success)
                        {
                            Debug.Log($"[MROrganizaPB] Organización completada: {result.GetSummary()}");
                        }
                    }
                }

                // Botón Revertir
                using (new EditorGUI.DisabledGroupScope(!_target.CanRevert))
                {
                    if (GUILayout.Button("Revertir", GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        Undo.RecordObject(_target, "Revertir PhysBones");

                        var result = _target.Revert();
                        EditorUtility.SetDirty(_target);

                        if (result.Success)
                        {
                            Debug.Log($"[MROrganizaPB] Reversión completada: {result.GetSummary()}");
                        }
                    }
                }
            }

            // Info contextual
            if (_target.IsOrganized)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(
                    "Los PhysBones han sido organizados en contenedores.\n" +
                    "Ahora puedes usar MRAgruparObjetos para controlarlos.",
                    MessageType.Info);
            }
            else if (_target.CanOrganize)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(
                    "Presiona 'Organizar PhysBones' para mover los componentes a contenedores organizados.",
                    MessageType.Info);
            }
        }

        private void DrawPhysBonesSection()
        {
            _showPhysBonesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showPhysBonesFoldout,
                $"PhysBones ({_target.DetectedPhysBones.Count})");

            if (_showPhysBonesFoldout)
            {
                // Botones de selección
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Todos", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    {
                        Undo.RecordObject(_target, "Habilitar todos PhysBones");
                        _target.SetAllPhysBonesEnabled(true);
                        EditorUtility.SetDirty(_target);
                    }
                    if (GUILayout.Button("Ninguno", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        Undo.RecordObject(_target, "Deshabilitar todos PhysBones");
                        _target.SetAllPhysBonesEnabled(false);
                        EditorUtility.SetDirty(_target);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{_target.EnabledPhysBonesCount} habilitados",
                        EditorStyles.miniLabel, GUILayout.Width(80));
                }

                // Lista
                _physBonesList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCollidersSection()
        {
            _showCollidersFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showCollidersFoldout,
                $"Colliders ({_target.DetectedColliders.Count})");

            if (_showCollidersFoldout)
            {
                // Botones de selección
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Todos", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    {
                        Undo.RecordObject(_target, "Habilitar todos Colliders");
                        _target.SetAllCollidersEnabled(true);
                        EditorUtility.SetDirty(_target);
                    }
                    if (GUILayout.Button("Ninguno", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    {
                        Undo.RecordObject(_target, "Deshabilitar todos Colliders");
                        _target.SetAllCollidersEnabled(false);
                        EditorUtility.SetDirty(_target);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{_target.EnabledCollidersCount} habilitados",
                        EditorStyles.miniLabel, GUILayout.Width(80));
                }

                // Lista
                _collidersList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStatsSection()
        {
            _showStatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showStatsFoldout, "Estadísticas por contexto");

            if (_showStatsFoldout)
            {
                var stats = _target.GetStatsByContext();

                foreach (var kvp in stats)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var isAvatar = kvp.Key == "Avatar";
                        GUI.contentColor = isAvatar ? AvatarContextColor : ClothingContextColor;
                        EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(120));
                        GUI.contentColor = Color.white;

                        EditorGUILayout.LabelField($"{kvp.Value.physBones} PB, {kvp.Value.colliders} Col",
                            EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLastResult()
        {
            var result = _target.LastResult;
            var messageType = result.Success ? MessageType.Info : MessageType.Warning;

            if (result.HasErrors)
            {
                messageType = MessageType.Error;
            }

            EditorGUILayout.HelpBox(result.GetSummary(), messageType);
        }

        #endregion

        #region List Element Drawing

        private void DrawPhysBoneElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _target.DetectedPhysBones.Count) return;

            var entry = _target.DetectedPhysBones[index] as PhysBoneEntry;
            if (entry == null) return;

            rect.y += 2;
            rect.height = ITEM_HEIGHT;

            float x = rect.x;

            // Toggle
            var toggleRect = new Rect(x, rect.y, TOGGLE_WIDTH, rect.height);
            EditorGUI.BeginChangeCheck();
            var newEnabled = EditorGUI.Toggle(toggleRect, entry.Enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle PhysBone");
                entry.Enabled = newEnabled;
                EditorUtility.SetDirty(_target);
            }
            x += TOGGLE_WIDTH + 5;

            // Nombre
            var nameWidth = rect.width - TOGGLE_WIDTH - CONTEXT_WIDTH - ROOT_BONE_WIDTH - 20;
            var nameRect = new Rect(x, rect.y, nameWidth, rect.height);

            GUI.contentColor = entry.Enabled ? Color.white : DisabledColor;
            EditorGUI.LabelField(nameRect, entry.GeneratedName);
            x += nameWidth + 5;

            // Contexto
            var contextRect = new Rect(x, rect.y, CONTEXT_WIDTH, rect.height);
            GUI.contentColor = entry.Context?.IsAvatarContext == true ? AvatarContextColor : ClothingContextColor;
            EditorGUI.LabelField(contextRect, entry.Context?.ContextName ?? "?", EditorStyles.miniLabel);
            x += CONTEXT_WIDTH + 5;

            // Root bone
            var rootRect = new Rect(x, rect.y, ROOT_BONE_WIDTH, rect.height);
            GUI.contentColor = DisabledColor;
            EditorGUI.LabelField(rootRect, entry.RootBoneName, EditorStyles.miniLabel);

            GUI.contentColor = Color.white;
        }

        private void DrawColliderElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _target.DetectedColliders.Count) return;

            var entry = _target.DetectedColliders[index] as ColliderEntry;
            if (entry == null) return;

            rect.y += 2;
            rect.height = ITEM_HEIGHT;

            float x = rect.x;

            // Toggle
            var toggleRect = new Rect(x, rect.y, TOGGLE_WIDTH, rect.height);
            EditorGUI.BeginChangeCheck();
            var newEnabled = EditorGUI.Toggle(toggleRect, entry.Enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Collider");
                entry.Enabled = newEnabled;
                EditorUtility.SetDirty(_target);
            }
            x += TOGGLE_WIDTH + 5;

            // Nombre
            var nameWidth = rect.width - TOGGLE_WIDTH - CONTEXT_WIDTH - ROOT_BONE_WIDTH - 20;
            var nameRect = new Rect(x, rect.y, nameWidth, rect.height);

            GUI.contentColor = entry.Enabled ? Color.white : DisabledColor;
            EditorGUI.LabelField(nameRect, entry.GeneratedName);
            x += nameWidth + 5;

            // Contexto
            var contextRect = new Rect(x, rect.y, CONTEXT_WIDTH, rect.height);
            GUI.contentColor = entry.Context?.IsAvatarContext == true ? AvatarContextColor : ClothingContextColor;
            EditorGUI.LabelField(contextRect, entry.Context?.ContextName ?? "?", EditorStyles.miniLabel);
            x += CONTEXT_WIDTH + 5;

            // Root bone
            var rootRect = new Rect(x, rect.y, ROOT_BONE_WIDTH, rect.height);
            GUI.contentColor = DisabledColor;
            EditorGUI.LabelField(rootRect, entry.RootBoneName, EditorStyles.miniLabel);

            GUI.contentColor = Color.white;
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (Event.current.type != EventType.Repaint) return;

            var bgColor = index % 2 == 0
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.25f, 0.25f, 0.25f);

            if (isActive || isFocused)
            {
                bgColor = new Color(0.24f, 0.48f, 0.9f, 0.4f);
            }

            EditorGUI.DrawRect(rect, bgColor);
        }

        #endregion
    }
}
