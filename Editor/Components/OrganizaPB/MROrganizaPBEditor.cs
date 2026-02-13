using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;

namespace Bender_Dios.MenuRadial.Editor.Components.OrganizaPB
{
    /// <summary>
    /// Editor personalizado para MROrganizaPB.
    /// Muestra PhysBones y Colliders agrupados por contexto (avatar, ropas, pelucas).
    /// </summary>
    [CustomEditor(typeof(MROrganizaPB))]
    public class MROrganizaPBEditor : UnityEditor.Editor
    {
        #region Private Fields

        private MROrganizaPB _target;
        private Dictionary<string, bool> _contextFoldouts = new Dictionary<string, bool>();

        #endregion

        #region Constants

        private const float ITEM_HEIGHT = 20f;
        private const float TOGGLE_WIDTH = 18f;
        private const float TYPE_LABEL_WIDTH = 32f;
        private const float ROOT_BONE_WIDTH = 110f;

        private static readonly Color EnabledColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color DisabledColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color AvatarContextColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color ClothingContextColor = new Color(1f, 0.7f, 0.4f);
        private static readonly Color PhysBoneTypeColor = new Color(0.5f, 0.85f, 0.5f);
        private static readonly Color ColliderTypeColor = new Color(0.85f, 0.55f, 0.85f);
        private static readonly Color CardBgColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        private static readonly Color CardHeaderBgColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);

        #endregion

        #region Inner Types

        private class ContextGroup
        {
            public OrganizationContext Context;
            public List<ComponentEntry> Entries = new List<ComponentEntry>();
            public int PhysBoneCount;
            public int ColliderCount;
            public int IncludedCount;
            public int AlreadyOrganizedCount;
        }

        #endregion

        #region Initialization

        private void OnEnable()
        {
            _target = (MROrganizaPB)target;
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
                    DrawContextGroups();
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

            // Estado actual
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Estado:", GUILayout.Width(50));

                switch (_target.State)
                {
                    case OrganizationState.NotScanned:
                        GUI.contentColor = WarningColor;
                        EditorGUILayout.LabelField("No escaneado", EditorStyles.boldLabel);
                        break;
                    case OrganizationState.Scanned:
                        GUI.contentColor = new Color(1f, 0.8f, 0.4f);
                        EditorGUILayout.LabelField("Escaneado (no organizado)", EditorStyles.boldLabel);
                        break;
                    case OrganizationState.Organized:
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
                using (new EditorGUI.DisabledGroupScope(!_target.CanOrganize))
                {
                    var organizeStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

                    if (GUILayout.Button("Organizar PhysBones", organizeStyle, GUILayout.Height(30)))
                    {
                        int pbCount = _target.IncludedPhysBonesCount;
                        int colCount = _target.IncludedCollidersCount;

                        bool confirmed = EditorUtility.DisplayDialog(
                            "Organizar PhysBones",
                            $"Esta operación moverá {pbCount} PhysBones y {colCount} Colliders a contenedores organizados. La escena será modificada.\n\n¿Continuar?",
                            "Organizar",
                            "Cancelar");

                        if (confirmed)
                        {
                            Undo.RecordObject(_target, "Organizar PhysBones");
                            var result = _target.Organize();
                            EditorUtility.SetDirty(_target);

                            if (result.Success)
                            {
                                Debug.Log($"[MROrganizaPB] Organización completada: {result.GetSummary()}");
                            }
                        }
                    }
                }

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

        #region Context Groups

        private List<ContextGroup> BuildContextGroups()
        {
            var groupMap = new Dictionary<string, ContextGroup>();

            foreach (var pb in _target.DetectedPhysBones)
            {
                var key = pb.Context?.ContextName ?? "Desconocido";
                if (!groupMap.TryGetValue(key, out var group))
                {
                    group = new ContextGroup { Context = pb.Context };
                    groupMap[key] = group;
                }
                group.Entries.Add(pb);
                group.PhysBoneCount++;
                if (pb.IsAlreadyOrganized)
                    group.AlreadyOrganizedCount++;
                else if (pb.Included && !pb.WasRelocated)
                    group.IncludedCount++;
            }

            foreach (var col in _target.DetectedColliders)
            {
                var key = col.Context?.ContextName ?? "Desconocido";
                if (!groupMap.TryGetValue(key, out var group))
                {
                    group = new ContextGroup { Context = col.Context };
                    groupMap[key] = group;
                }
                group.Entries.Add(col);
                group.ColliderCount++;
                if (col.IsAlreadyOrganized)
                    group.AlreadyOrganizedCount++;
                else if (col.Included && !col.WasRelocated)
                    group.IncludedCount++;
            }

            // Ordenar: avatar primero, luego alfabético
            var result = new List<ContextGroup>(groupMap.Values);
            result.Sort((a, b) =>
            {
                bool aIsAvatar = a.Context?.IsAvatarContext == true;
                bool bIsAvatar = b.Context?.IsAvatarContext == true;
                if (aIsAvatar && !bIsAvatar) return -1;
                if (!aIsAvatar && bIsAvatar) return 1;
                return string.Compare(
                    a.Context?.ContextName ?? "",
                    b.Context?.ContextName ?? "",
                    System.StringComparison.Ordinal);
            });

            return result;
        }

        private void DrawContextGroups()
        {
            var groups = BuildContextGroups();

            // Resumen global
            EditorGUILayout.LabelField("Componentes por contexto", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            foreach (var group in groups)
            {
                DrawContextCard(group);
                EditorGUILayout.Space(4);
            }
        }

        private void DrawContextCard(ContextGroup group)
        {
            var contextName = group.Context?.ContextName ?? "Desconocido";
            bool isAvatar = group.Context?.IsAvatarContext == true;

            // Obtener/inicializar estado del foldout
            if (!_contextFoldouts.TryGetValue(contextName, out bool isExpanded))
            {
                isExpanded = false;
                _contextFoldouts[contextName] = false;
            }

            // Card container
            var cardRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // --- Header ---
            DrawCardHeader(group, contextName, isAvatar, ref isExpanded);

            // --- Body (entries) ---
            if (isExpanded)
            {
                EditorGUILayout.Space(2);

                // Origen
                if (group.Context?.ContextRoot != null)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.ObjectField("Origen", group.Context.ContextRoot, typeof(GameObject), true);
                    }
                }

                EditorGUILayout.Space(2);

                // Columna headers
                DrawColumnHeaders();

                // Entries: PhysBones primero, luego Colliders
                int rowIndex = 0;
                foreach (var entry in group.Entries)
                {
                    DrawEntryRow(entry, rowIndex);
                    rowIndex++;
                }
            }

            EditorGUILayout.EndVertical();

            _contextFoldouts[contextName] = isExpanded;
        }

        private void DrawCardHeader(ContextGroup group, string contextName, bool isAvatar, ref bool isExpanded)
        {
            var headerRect = EditorGUILayout.BeginHorizontal();

            // Foldout con nombre de contexto
            GUI.contentColor = isAvatar ? AvatarContextColor : ClothingContextColor;
            var contextLabel = isAvatar ? "Avatar" : contextName;
            isExpanded = EditorGUILayout.Foldout(isExpanded, contextLabel, true, EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            // Contadores
            GUILayout.FlexibleSpace();

            GUI.contentColor = PhysBoneTypeColor;
            EditorGUILayout.LabelField($"{group.PhysBoneCount} PB", EditorStyles.miniLabel, GUILayout.Width(35));
            GUI.contentColor = ColliderTypeColor;
            EditorGUILayout.LabelField($"{group.ColliderCount} Col", EditorStyles.miniLabel, GUILayout.Width(35));
            GUI.contentColor = Color.white;

            // Conteo de entries que necesitan organización (no ya-organizados, no reubicados)
            int totalActionable = 0;
            int includedActionable = 0;
            foreach (var entry in group.Entries)
            {
                if (!entry.IsAlreadyOrganized && !entry.WasRelocated)
                {
                    totalActionable++;
                    if (entry.Included) includedActionable++;
                }
            }

            // Si todo el grupo ya está organizado → mostrar etiqueta
            if (totalActionable == 0)
            {
                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField("OK", EditorStyles.miniBoldLabel, GUILayout.Width(22));
                GUI.contentColor = Color.white;
            }
            else
            {
                // Separador visual
                EditorGUILayout.LabelField("|", EditorStyles.miniLabel, GUILayout.Width(8));

                // Toggle incluir/excluir todos del grupo
                bool allIncluded = includedActionable == totalActionable;
                bool mixed = includedActionable > 0 && includedActionable < totalActionable;

                EditorGUI.showMixedValue = mixed;
                EditorGUI.BeginChangeCheck();
                var newToggle = EditorGUILayout.Toggle(allIncluded, GUILayout.Width(TOGGLE_WIDTH));
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Toggle grupo");
                    foreach (var entry in group.Entries)
                    {
                        if (!entry.IsAlreadyOrganized && !entry.WasRelocated)
                            entry.Included = newToggle;
                    }
                    EditorUtility.SetDirty(_target);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(TOGGLE_WIDTH + 4);

                GUI.contentColor = DisabledColor;
                EditorGUILayout.LabelField("Tipo", EditorStyles.miniLabel, GUILayout.Width(TYPE_LABEL_WIDTH));

                EditorGUILayout.LabelField("Nombre", EditorStyles.miniLabel);

                EditorGUILayout.LabelField("Root", EditorStyles.miniLabel, GUILayout.Width(ROOT_BONE_WIDTH));
                GUI.contentColor = Color.white;

                // Espacio para el toggle
                GUILayout.Space(TOGGLE_WIDTH + 4);
            }
        }

        private void DrawEntryRow(ComponentEntry entry, int rowIndex)
        {
            bool isPhysBone = entry is PhysBoneEntry;
            bool isOrganized = entry.IsAlreadyOrganized;

            // Fondo alternado
            var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(ITEM_HEIGHT));

            if (Event.current.type == EventType.Repaint)
            {
                var bgColor = rowIndex % 2 == 0
                    ? new Color(0.22f, 0.22f, 0.22f, 0.4f)
                    : new Color(0.26f, 0.26f, 0.26f, 0.4f);
                EditorGUI.DrawRect(rowRect, bgColor);
            }

            GUILayout.Space(4);

            if (isOrganized)
            {
                // Ya organizado: mostrar checkmark en lugar de toggle
                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField("\u2714", EditorStyles.miniLabel, GUILayout.Width(TOGGLE_WIDTH));
            }
            else
            {
                // Toggle incluir
                EditorGUI.BeginChangeCheck();
                var newIncluded = EditorGUILayout.Toggle(entry.Included, GUILayout.Width(TOGGLE_WIDTH));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, isPhysBone ? "Toggle PhysBone" : "Toggle Collider");
                    entry.Included = newIncluded;
                    EditorUtility.SetDirty(_target);
                }
            }

            // Tipo con color
            var typeColor = isOrganized
                ? DisabledColor
                : (entry.Included ? (isPhysBone ? PhysBoneTypeColor : ColliderTypeColor) : DisabledColor);
            GUI.contentColor = typeColor;
            EditorGUILayout.LabelField(isPhysBone ? "PB" : "Col", EditorStyles.miniLabel, GUILayout.Width(TYPE_LABEL_WIDTH));

            // Nombre (click para hacer ping en hierarchy)
            GUI.contentColor = isOrganized ? DisabledColor : (entry.Included ? Color.white : DisabledColor);
            var nameStyle = new GUIStyle(EditorStyles.miniLabel);
            if (entry.OriginalComponent != null)
            {
                nameStyle.normal.textColor = GUI.contentColor;
                if (GUILayout.Button(entry.GeneratedName, nameStyle))
                {
                    EditorGUIUtility.PingObject(entry.OriginalComponent.gameObject);
                }
            }
            else
            {
                EditorGUILayout.LabelField(entry.GeneratedName, nameStyle);
            }

            // Root bone o "ya organizado"
            GUI.contentColor = DisabledColor;
            if (isOrganized)
            {
                EditorGUILayout.LabelField("ya organizado", EditorStyles.miniLabel, GUILayout.Width(ROOT_BONE_WIDTH));
            }
            else
            {
                var rootLabel = entry.HadExplicitRootTransform
                    ? entry.RootBoneName
                    : $"({entry.RootBoneName})";
                EditorGUILayout.LabelField(rootLabel, EditorStyles.miniLabel, GUILayout.Width(ROOT_BONE_WIDTH));
            }

            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}
