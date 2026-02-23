using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.CoserRopa
{
    /// <summary>
    /// Editor personalizado para MRCoserRopa
    /// Estilo similar a MRUnificarObjetos con ReorderableList compacta
    /// </summary>
    [CustomEditor(typeof(MRCoserRopa))]
    public class MRCoserRopaEditor : UnityEditor.Editor
    {
        private MRCoserRopa _target;
        private SerializedProperty _detectedPiecesProp;
        private SerializedProperty _selectedIndexProp;
        private ReorderableList _pieceList;
        private bool _showNamingFoldout = false;

        // Constantes de diseño
        private const float ITEM_HEIGHT = 22f;
        private const float TOGGLE_WIDTH = 18f;
        private const float BONES_INFO_WIDTH = 70f;
        private const float DELETE_BUTTON_WIDTH = 20f;

        // Colores
        private static readonly Color EnabledColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color DisabledColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color SelectedBgColor = new Color(0.24f, 0.48f, 0.9f, 0.4f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color ModularAvatarColor = new Color(0.4f, 0.7f, 1f); // Azul para MA
        private static readonly Color WigColor = new Color(0.9f, 0.5f, 0.9f); // Magenta para pelucas

        // Auto-detección de cambios en jerarquía
        private int _lastAvatarChildCount = -1;
        private GameObject _lastAvatarRoot = null;

        private void OnEnable()
        {
            _target = (MRCoserRopa)target;
            _detectedPiecesProp = serializedObject.FindProperty("_detectedPieces");
            _selectedIndexProp = serializedObject.FindProperty("_selectedPieceIndex");

            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            _pieceList = new ReorderableList(serializedObject, _detectedPiecesProp, false, true, true, true)
            {
                drawHeaderCallback = DrawListHeader,
                drawElementCallback = DrawListElement,
                elementHeight = ITEM_HEIGHT + 4f,
                onAddCallback = OnAddPiece,
                onRemoveCallback = OnRemovePiece,
                onSelectCallback = OnSelectPiece,
                drawElementBackgroundCallback = DrawElementBackground
            };
        }

        /// <summary>
        /// Detecta cambios en la jerarquía del avatar y refresca automáticamente.
        /// </summary>
        private void CheckForHierarchyChanges()
        {
            if (_target.AvatarRoot == null)
            {
                _lastAvatarChildCount = -1;
                _lastAvatarRoot = null;
                return;
            }

            // Si cambió el avatar, resetear conteo y aplicar visibilidad
            if (_lastAvatarRoot != _target.AvatarRoot)
            {
                _lastAvatarRoot = _target.AvatarRoot;
                _lastAvatarChildCount = _target.AvatarRoot.transform.childCount;

                // Aplicar visibilidad por defecto a ropas existentes
                if (_target.DetectedPieceCount > 0)
                {
                    _target.SetDefaultPieceVisibility();
                    EditorUtility.SetDirty(_target);
                }
                return;
            }

            // Verificar si cambió el número de hijos
            int currentChildCount = _target.AvatarRoot.transform.childCount;
            if (currentChildCount != _lastAvatarChildCount && _lastAvatarChildCount >= 0)
            {
                _lastAvatarChildCount = currentChildCount;
                _target.RefreshDetection();
                EditorUtility.SetDirty(_target);
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            // Auto-detectar cambios en la jerarquía del avatar
            CheckForHierarchyChanges();

            // Header
            DrawHeader();
            EditorGUILayout.Space(5);

            // Avatar
            DrawAvatarSection();

            // Solo mostrar el resto si hay avatar
            if (_target.AvatarRoot != null)
            {
                EditorGUILayout.Space(8);

                // Lista de ropas (ReorderableList)
                DrawPieceList();

                // Detalles de la ropa seleccionada
                DrawSelectedPieceDetails();

                EditorGUILayout.Space(8);

                // Info NDMF
                DrawNDMFInfo();
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(MRLocalization.Get(L.CoserRopa.DROP_AVATAR_HERE), MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Header & Avatar

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopa.HEADER), EditorStyleManager.HeaderStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopa.SUBTITLE_NEW),
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawAvatarSection()
        {
            EditorGUI.BeginChangeCheck();
            var newAvatar = (GameObject)EditorGUILayout.ObjectField(
                MRLocalization.Get(L.CoserRopa.AVATAR_LABEL),
                _target.AvatarRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newAvatar != _target.AvatarRoot)
            {
                Undo.RecordObject(_target, "Cambiar Avatar");
                _target.AvatarRoot = newAvatar;
                EditorUtility.SetDirty(_target);
            }

            // Info del avatar
            if (_target.AvatarReference != null && _target.AvatarRoot != null)
            {
                var color = _target.IsAvatarHumanoid ? EnabledColor : WarningColor;
                var label = _target.IsAvatarHumanoid ? MRLocalization.Get(L.CoserRopaExtra.HUMANOID) : MRLocalization.Get(L.CoserRopaExtra.GENERIC_FALLBACK);

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth + 2);
                GUI.contentColor = color;
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        #endregion

        #region ReorderableList

        private void DrawPieceList()
        {
            // Header con boton refrescar
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.CoserRopa.DETECTED_CLOTHINGS, _target.DetectedPieceCount),
                    EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(MRLocalization.Get(L.Common.REFRESH), EditorStyles.miniButton, GUILayout.Width(65)))
                {
                    Undo.RecordObject(_target, "Refrescar");
                    _target.RefreshDetection();
                    EditorUtility.SetDirty(_target);
                }
            }

            // Sincronizar indice
            _pieceList.index = _target.SelectedPieceIndex;

            // Dibujar lista
            _pieceList.DoLayoutList();

            // Botones de seleccion rapida
            if (_target.DetectedPieceCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(MRLocalization.Get(L.CoserRopa.SELECT_ALL), EditorStyles.miniButtonLeft))
                    {
                        Undo.RecordObject(_target, "Seleccionar todas");
                        _target.EnableAllPieces();
                        EditorUtility.SetDirty(_target);
                    }
                    if (GUILayout.Button(MRLocalization.Get(L.CoserRopa.DESELECT_ALL), EditorStyles.miniButtonRight))
                    {
                        Undo.RecordObject(_target, "Deseleccionar todas");
                        _target.DisableAllPieces();
                        EditorUtility.SetDirty(_target);
                    }
                }
            }
        }

        private void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, MRLocalization.Get(L.CoserRopaExtra.CLOTHING_LIST_HEADER, _target.EnabledPieceCount, _target.DetectedPieceCount));
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _target.DetectedPieceCount) return;

            if (isActive || isFocused)
            {
                EditorGUI.DrawRect(rect, SelectedBgColor);
            }
        }

        /// <summary>
        /// Obtiene nombres localizados para el popup de PieceType
        /// </summary>
        private string[] GetPieceTypePopupNames()
        {
            return new[] {
                MRLocalization.Get(L.CoserRopaExtra.PIECE_TYPE_ROPA),
                MRLocalization.Get(L.CoserRopaExtra.PIECE_TYPE_PELO),
                MRLocalization.Get(L.CoserRopaExtra.PIECE_TYPE_PIEZA)
            };
        }

        /// <summary>
        /// Colores por PieceType para el popup
        /// </summary>
        private static readonly Color[] PieceTypeColors = {
            new Color(0.6f, 0.8f, 1f),  // Ropa - azul claro
            new Color(1f, 0.7f, 0.9f),  // Pelo - rosa
            new Color(0.8f, 0.8f, 0.8f) // Pieza - gris
        };

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _target.DetectedPieceCount) return;

            var piece = _target.DetectedPieces[index];
            if (piece == null) return;

            rect.y += 2f;
            rect.height = ITEM_HEIGHT;

            // Layout: [Check 18px][Tipo 65px][GameObject flex][MA 40px][→Target 75px][Fix 30px][Zona 75px][X 20px]
            const float TYPE_WIDTH = 65f;
            const float MA_WIDTH = 40f;
            const float TARGET_WIDTH = 75f;
            const float FIX_WIDTH = 30f;
            const float ZONE_WIDTH = 75f;
            const float OBJECT_FIELD_MIN = 80f;

            float x = rect.x;
            Color originalColor = GUI.contentColor;
            Color originalBgColor = GUI.backgroundColor;

            // --- Checkbox ---
            var toggleRect = new Rect(x, rect.y, TOGGLE_WIDTH, rect.height);
            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUI.Toggle(toggleRect, piece.Enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle pieza");
                _target.SetPieceEnabled(index, enabled);
                EditorUtility.SetDirty(_target);
            }
            x += TOGGLE_WIDTH + 2f;

            // --- PieceType button with numbered label ---
            var typeRect = new Rect(x, rect.y, TYPE_WIDTH, rect.height);
            GUI.backgroundColor = PieceTypeColors[(int)piece.PieceType];
            string numberedLabel = GetNumberedPieceTypeLabel(index);
            if (GUI.Button(typeRect, numberedLabel, EditorStyles.miniButton))
            {
                ShowPieceTypeMenu(index);
            }
            GUI.backgroundColor = originalBgColor;
            x += TYPE_WIDTH + 2f;

            // --- ObjectField (flex width) ---
            float objectFieldWidth = rect.width - TOGGLE_WIDTH - TYPE_WIDTH - MA_WIDTH - TARGET_WIDTH - FIX_WIDTH - ZONE_WIDTH - DELETE_BUTTON_WIDTH - 24f;
            objectFieldWidth = Mathf.Max(objectFieldWidth, OBJECT_FIELD_MIN);
            var objectFieldRect = new Rect(x, rect.y, objectFieldWidth, rect.height);

            GUI.contentColor = piece.Enabled ? Color.white : DisabledColor;
            EditorGUI.BeginChangeCheck();
            var newGameObject = (GameObject)EditorGUI.ObjectField(
                objectFieldRect, piece.GameObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newGameObject != piece.GameObject)
            {
                Undo.RecordObject(_target, "Cambiar pieza");
                if (newGameObject != null)
                {
                    piece.GameObject = newGameObject;
                    piece.ArmatureReference = new ArmatureReference(newGameObject);
                    _target.DetectBoneMappingsForPiece(piece);
                }
                EditorUtility.SetDirty(_target);
            }
            GUI.contentColor = originalColor;
            x += objectFieldWidth + 2f;

            // --- MA label ---
            var maRect = new Rect(x, rect.y, MA_WIDTH, rect.height);
            if (piece.HasModularAvatar)
            {
                string maLabel = piece.ModularAvatarComponentType.Contains("BoneProxy") ? "BP" : "MA";
                // BoneProxy mal ubicado: mostrar en color de advertencia
                if (piece.IsBoneProxyMisplaced)
                    GUI.contentColor = WarningColor;
                else
                    GUI.contentColor = piece.DisableMA ? DisabledColor : ModularAvatarColor;
                EditorGUI.LabelField(maRect, piece.IsBoneProxyMisplaced ? "BP!" : maLabel, EditorStyles.miniLabel);
            }
            else
            {
                GUI.contentColor = DisabledColor;
                EditorGUI.LabelField(maRect, "\u2014", EditorStyles.miniLabel);
            }
            GUI.contentColor = originalColor;
            x += MA_WIDTH + 2f;

            // --- MA Target ---
            var targetRect = new Rect(x, rect.y, TARGET_WIDTH, rect.height);
            if (piece.HasModularAvatar && !string.IsNullOrEmpty(piece.MATargetInfo))
            {
                GUI.contentColor = piece.DisableMA ? DisabledColor : ModularAvatarColor;
                EditorGUI.LabelField(targetRect, $"\u2192{piece.MATargetInfo}", EditorStyles.miniLabel);
            }
            else
            {
                GUI.contentColor = DisabledColor;
                EditorGUI.LabelField(targetRect, "\u2014", EditorStyles.miniLabel);
            }
            GUI.contentColor = originalColor;
            x += TARGET_WIDTH + 2f;

            // --- Fix BoneProxy button ---
            var fixRect = new Rect(x, rect.y, FIX_WIDTH, rect.height);
            if (piece.IsBoneProxyMisplaced)
            {
                GUI.backgroundColor = WarningColor;
                if (GUI.Button(fixRect, "Fix", EditorStyles.miniButton))
                {
                    // Buscar armature de la pieza como destino
                    var armatureTarget = FindArmatureForRelocation(piece);
                    if (armatureTarget != null)
                    {
                        Undo.RecordObject(_target, "Reubicar BoneProxy");
                        if (ModularAvatarDetector.Instance.RelocateBoneProxy(piece.GameObject, armatureTarget))
                        {
                            piece.IsBoneProxyMisplaced = false;
                            _target.RefreshDetection();
                            EditorUtility.SetDirty(_target);
                        }
                    }
                }
                GUI.backgroundColor = originalBgColor;
            }
            x += FIX_WIDTH + 2f;

            // --- Zone dropdown ---
            var zoneRect = new Rect(x, rect.y, ZONE_WIDTH, rect.height);
            var effectiveZone = piece.EffectiveStitchZone;

            // Build zone popup options: (Auto) + 12 zones
            string[] zoneShortNames = GetZoneShortPopupNames();
            int currentZoneIdx = piece.HasManualStitchZone ? (int)piece.ManualStitchZone + 1 : 0;
            GUI.backgroundColor = piece.HasManualStitchZone ? new Color(0.5f, 0.9f, 0.5f) : originalBgColor;
            EditorGUI.BeginChangeCheck();
            int newZoneIdx = EditorGUI.Popup(zoneRect, currentZoneIdx, zoneShortNames, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar zona");
                if (newZoneIdx == 0)
                {
                    piece.HasManualStitchZone = false;
                }
                else
                {
                    piece.HasManualStitchZone = true;
                    piece.ManualStitchZone = (StitchZone)(newZoneIdx - 1);
                }
                // Re-clasificar PieceType si no es manual
                if (!piece.PieceTypeManuallySet)
                {
                    bool maToHead = piece.HasModularAvatar &&
                        ModularAvatarDetector.Instance.IsBoneProxyToHead(piece.GameObject);
                    piece.PieceType = PieceEntry.DeterminePieceType(
                        piece.EffectiveStitchZone, piece.IsWig, maToHead, piece.Name, piece.AllMeshesHeadWeighted);
                }
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = originalBgColor;

            // --- Delete button ---
            var deleteRect = new Rect(rect.x + rect.width - DELETE_BUTTON_WIDTH - 2f, rect.y + 1f, DELETE_BUTTON_WIDTH, rect.height - 2f);
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUI.Button(deleteRect, "X", EditorStyles.miniButton))
            {
                Undo.RecordObject(_target, "Quitar pieza");
                _target.RemovePiece(index);
                EditorUtility.SetDirty(_target);
            }
            GUI.color = Color.white;
        }

        private void OnAddPiece(ReorderableList list)
        {
            // Agregar entrada vacia (el usuario arrastrara el objeto al ObjectField)
            Undo.RecordObject(_target, "Agregar pieza");

            var newEntry = new PieceEntry();
            _target.DetectedPieces.Add(newEntry);

            // Seleccionar la nueva entrada
            _target.SelectedPieceIndex = _target.DetectedPieceCount - 1;

            EditorUtility.SetDirty(_target);
        }

        private void OnRemovePiece(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _target.DetectedPieceCount)
            {
                Undo.RecordObject(_target, "Quitar pieza");
                _target.RemovePiece(list.index);
                EditorUtility.SetDirty(_target);
            }
        }

        private void OnSelectPiece(ReorderableList list)
        {
            _selectedIndexProp.intValue = list.index;
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Selected Piece Details

        private Vector2 _mappingsScrollPos;

        /// <summary>
        /// Obtiene nombres localizados para el popup de StitchZone.
        /// Orden debe coincidir con el enum StitchZone.
        /// </summary>
        private string[] GetStitchZonePopupNames()
        {
            return new[] {
                MRLocalization.Get(L.CoserRopaExtra.ZONE_FULL_BODY),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_TORSO),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_HEAD),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_UPPER_LIMB),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LOWER_LIMB),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_HIP),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_HAND),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_HAND),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_FOOT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_FOOT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_CHEST),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_NONE)
            };
        }

        private void DrawSelectedPieceDetails()
        {
            var selected = _target.SelectedPiece;
            if (selected == null) return;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Titulo
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.DETAILS_TITLE, selected.Name), EditorStyles.boldLabel);

            // --- Tipo de pieza con boton Auto ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.PIECE_TYPE_LABEL), GUILayout.Width(EditorGUIUtility.labelWidth));
            GUI.backgroundColor = PieceTypeColors[(int)selected.PieceType];
            string[] pieceTypeNames = GetPieceTypePopupNames();
            EditorGUI.BeginChangeCheck();
            var newPieceType = (PieceType)EditorGUILayout.Popup((int)selected.PieceType, pieceTypeNames, GUILayout.Width(70));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar tipo pieza");
                selected.PieceType = newPieceType;
                selected.PieceTypeManuallySet = true;
                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = Color.white;

            if (selected.PieceTypeManuallySet)
            {
                if (GUILayout.Button("Auto", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    Undo.RecordObject(_target, "Auto-clasificar tipo pieza");
                    selected.PieceTypeManuallySet = false;
                    bool maToHead = selected.HasModularAvatar &&
                        ModularAvatarDetector.Instance.IsBoneProxyToHead(selected.GameObject);
                    selected.PieceType = PieceEntry.DeterminePieceType(
                        selected.EffectiveStitchZone, selected.IsWig, maToHead, selected.Name, selected.AllMeshesHeadWeighted);
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // --- Info MA: tipo + destino + DisableMA toggle ---
            if (selected.HasModularAvatar)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // MA tipo y destino
                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = selected.DisableMA ? DisabledColor : ModularAvatarColor;
                string maTypeLabel = selected.ModularAvatarComponentType.Contains("BoneProxy") ? "BoneProxy" : "MergeArmature";
                string maTargetStr = !string.IsNullOrEmpty(selected.MATargetInfo) ? $" \u2192 {selected.MATargetInfo}" : "";
                EditorGUILayout.LabelField($"MA: {maTypeLabel}{maTargetStr}", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // BoneProxy mal ubicado: solo warning informativo (el boton Fix esta en la lista)
                if (selected.IsBoneProxyMisplaced)
                {
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.CoserRopaExtra.BONE_PROXY_MISPLACED),
                        MessageType.Warning);
                }

                // Toggle DisableMA
                EditorGUI.BeginChangeCheck();
                var disableMA = EditorGUILayout.ToggleLeft(
                    MRLocalization.Get(L.CoserRopaExtra.DISABLE_MA_TOGGLE),
                    selected.DisableMA);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Toggle DisableMA");
                    selected.DisableMA = disableMA;
                    EditorUtility.SetDirty(_target);
                }

                // Warning para pelo
                if (selected.DisableMA && selected.PieceType == PieceType.Pelo)
                {
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.CoserRopaExtra.DISABLE_MA_WIG_WARNING),
                        MessageType.Warning);
                }

                // Info de procesamiento
                if (!selected.DisableMA)
                {
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.CoserRopaExtra.MA_WILL_PROCESS, selected.ModularAvatarComponentType),
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        MRLocalization.Get(L.CoserRopaExtra.DISABLE_MA_MR_WILL_PROCESS),
                        MessageType.Info);
                }

                EditorGUILayout.EndVertical();

                // Si MA activo (no desactivado), no mostrar mas detalles de cosido
                if (!selected.DisableMA)
                {
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            // Mostrar si solo tiene MA Shape Changer (sin Merge Armature)
            if (selected.HasMAShapeChanger && !selected.HasModularAvatar)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = ModularAvatarColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.MA_NO_MERGE_ARMATURE), EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.CoserRopaExtra.MA_MANUAL_NEEDED),
                    MessageType.Warning);
            }

            // Info de huesos
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.BONE_STATS, selected.MappedBoneCount, selected.TotalBoneCount), EditorStyles.miniLabel);

            // --- Zona de cosido manual ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.MANUAL_ZONE_LABEL), GUILayout.Width(EditorGUIUtility.labelWidth));

            // Popup con todas las zonas + opcion "(Auto)" al inicio
            string[] stitchZoneNames = GetStitchZonePopupNames();
            string[] zoneOptions = new string[stitchZoneNames.Length + 1];
            zoneOptions[0] = MRLocalization.Get(L.CoserRopaExtra.MANUAL_ZONE_AUTO);
            for (int i = 0; i < stitchZoneNames.Length; i++)
                zoneOptions[i + 1] = stitchZoneNames[i];

            int currentZoneIndex = selected.HasManualStitchZone ? (int)selected.ManualStitchZone + 1 : 0;
            EditorGUI.BeginChangeCheck();
            int newZoneIndex = EditorGUILayout.Popup(currentZoneIndex, zoneOptions, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar zona manual");
                if (newZoneIndex == 0)
                {
                    selected.HasManualStitchZone = false;
                }
                else
                {
                    selected.HasManualStitchZone = true;
                    selected.ManualStitchZone = (StitchZone)(newZoneIndex - 1);
                }
                EditorUtility.SetDirty(_target);
            }

            // Mostrar zona actual (auto o manual)
            string zoneInfo = selected.HasManualStitchZone
                ? MRLocalization.Get(L.CoserRopaExtra.MANUAL_ZONE_ACTIVE)
                : $"({GetZoneFullLabel(selected.StitchZone)})";
            GUI.contentColor = selected.HasManualStitchZone ? EnabledColor : DisabledColor;
            EditorGUILayout.LabelField(zoneInfo, EditorStyles.miniLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Zona wig label
            if (selected.IsWig && !selected.HasManualStitchZone)
            {
                GUI.contentColor = WigColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.ZONE_WIG_LABEL, GetZoneFullLabel(selected.StitchZone)), EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }

            // Detectar y mostrar prefijos/sufijos
            DrawBoneNamingInfo(selected);

            // Foldout mapeos editables
            _target.ShowBoneMappings = EditorGUILayout.Foldout(_target.ShowBoneMappings,
                $"Editar mapeos de huesos ({selected.BoneMappings?.Count ?? 0})", true);

            if (_target.ShowBoneMappings && selected.BoneMappings != null)
            {
                DrawBoneMappingsEditor(selected);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Muestra campos editables para prefijo/sufijo de nombres de huesos
        /// </summary>
        private void DrawBoneNamingInfo(PieceEntry piece)
        {
            EditorGUILayout.Space(3);

            // Detectar automáticamente para mostrar como placeholder/sugerencia
            var detectedPrefix = DetectCommonPrefix(piece);
            var detectedSuffix = DetectCommonSuffix(piece);

            // Foldout para la sección
            bool showSection = piece.HasCustomNaming ||
                               !string.IsNullOrEmpty(detectedPrefix) ||
                               !string.IsNullOrEmpty(detectedSuffix);

            string foldoutLabel = MRLocalization.Get(L.CoserRopaExtra.PREFIX_SECTION);
            if (piece.HasCustomNaming)
                foldoutLabel += " (configurado)";
            else if (showSection)
                foldoutLabel += " (detectado)";

            _showNamingFoldout = EditorGUILayout.Foldout(_showNamingFoldout, foldoutLabel, true);

            if (!_showNamingFoldout)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Campo Prefijo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.PREFIX_REMOVE), GUILayout.Width(50));

            EditorGUI.BeginChangeCheck();
            string newPrefix = EditorGUILayout.TextField(piece.BonePrefix);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar prefijo");
                piece.BonePrefix = newPrefix;
                EditorUtility.SetDirty(_target);
            }

            // Botón para usar el detectado
            if (!string.IsNullOrEmpty(detectedPrefix) && detectedPrefix != piece.BonePrefix)
            {
                if (GUILayout.Button($"Usar: {detectedPrefix}", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Undo.RecordObject(_target, "Usar prefijo detectado");
                    piece.BonePrefix = detectedPrefix;
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Campo Sufijo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.SUFFIX_SECTION), GUILayout.Width(50));

            EditorGUI.BeginChangeCheck();
            string newSuffix = EditorGUILayout.TextField(piece.BoneSuffix);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar sufijo");
                piece.BoneSuffix = newSuffix;
                EditorUtility.SetDirty(_target);
            }

            // Botón para usar el detectado
            if (!string.IsNullOrEmpty(detectedSuffix) && detectedSuffix != piece.BoneSuffix)
            {
                if (GUILayout.Button($"Usar: {detectedSuffix}", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Undo.RecordObject(_target, "Usar sufijo detectado");
                    piece.BoneSuffix = detectedSuffix;
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Botón para re-detectar con los nuevos valores
            EditorGUILayout.Space(3);
            if (GUILayout.Button(MRLocalization.Get(L.CoserRopaExtra.SUFFIX_REMOVE), EditorStyles.miniButton))
            {
                Undo.RecordObject(_target, "Re-detectar huesos");
                _target.DetectBoneMappingsForPiece(piece);
                EditorUtility.SetDirty(_target);
            }

            // Mensaje informativo
            if (piece.HasCustomNaming)
            {
                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.PREFIX_KEEP), EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = Color.white;
            }
            else
            {
                GUI.contentColor = new Color(0.6f, 0.6f, 0.6f);
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.SUFFIX_KEEP), EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Detecta prefijo común en los nombres de huesos de la ropa
        /// </summary>
        private string DetectCommonPrefix(PieceEntry piece)
        {
            var pieceBoneNames = piece.BoneMappings
                .Where(m => m.PieceBone != null)
                .Select(m => m.PieceBone.name)
                .ToList();

            if (pieceBoneNames.Count < 3)
                return null;

            // Buscar prefijos comunes conocidos
            string[] commonPrefixes = new[]
            {
                "Outfit_", "outfit_", "Clothing_", "clothing_", "Body_", "body_",
                "Avatar_", "avatar_", "Armature_", "armature_", "Rig_", "rig_",
                "DEF_", "def_", "DEF-", "def-", "MCH_", "mch_", "MCH-", "mch-",
                "ORG_", "org_", "ORG-", "org-", "Bone_", "bone_",
                "J_", "j_", "Bip_", "bip_", "Bip01_", "bip01_",
                "mixamorig:", "mixamorig_", "Mixamorig:",
                "CC_Base_", "cc_base_", "Genesis_", "genesis_"
            };

            foreach (var prefix in commonPrefixes)
            {
                int matchCount = pieceBoneNames.Count(name => name.StartsWith(prefix));
                // Si más del 50% de los huesos tienen este prefijo, lo consideramos común
                if (matchCount > pieceBoneNames.Count / 2)
                {
                    return prefix;
                }
            }

            // Intentar detectar prefijo personalizado
            // Buscar el prefijo más largo común entre los primeros huesos
            if (pieceBoneNames.Count >= 2)
            {
                string first = pieceBoneNames[0];
                string commonPrefix = "";

                for (int i = 1; i <= first.Length && i <= 20; i++)
                {
                    string candidate = first.Substring(0, i);
                    // Verificar si termina en separador común
                    if (!candidate.EndsWith("_") && !candidate.EndsWith("-") && !candidate.EndsWith(".") && !candidate.EndsWith(":"))
                        continue;

                    int matchCount = pieceBoneNames.Count(name => name.StartsWith(candidate));
                    if (matchCount > pieceBoneNames.Count / 2)
                    {
                        commonPrefix = candidate;
                    }
                }

                if (commonPrefix.Length >= 2)
                    return commonPrefix;
            }

            return null;
        }

        /// <summary>
        /// Detecta sufijo común en los nombres de huesos de la ropa
        /// </summary>
        private string DetectCommonSuffix(PieceEntry piece)
        {
            var pieceBoneNames = piece.BoneMappings
                .Where(m => m.PieceBone != null)
                .Select(m => m.PieceBone.name)
                .ToList();

            if (pieceBoneNames.Count < 3)
                return null;

            // Buscar sufijos comunes conocidos
            string[] commonSuffixes = new[]
            {
                "_Bone", "_bone", ".Bone", ".bone",
                "_001", "_002", ".001", ".002",
                "_jnt", ".jnt", "_JNT", ".JNT",
                "_def", ".def", "_DEF", ".DEF",
                "_end", ".end", "_End", ".End",
                "_ctrl", ".ctrl", "_CTRL", ".CTRL"
            };

            foreach (var suffix in commonSuffixes)
            {
                int matchCount = pieceBoneNames.Count(name => name.EndsWith(suffix));
                if (matchCount > pieceBoneNames.Count / 2)
                {
                    return suffix;
                }
            }

            return null;
        }

        private void DrawBoneMappingsEditor(PieceEntry piece)
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.MAPPING_HEADER_AVATAR), EditorStyles.miniLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("←", GUILayout.Width(20));
            EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.MAPPING_HEADER_CLOTHING), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("", GUILayout.Width(50)); // Espacio para indicador
            EditorGUILayout.EndHorizontal();

            // Lista con scroll
            _mappingsScrollPos = EditorGUILayout.BeginScrollView(_mappingsScrollPos,
                GUILayout.MaxHeight(200));

            for (int i = 0; i < piece.BoneMappings.Count; i++)
            {
                var mapping = piece.BoneMappings[i];
                DrawSingleMappingEditor(mapping);
            }

            EditorGUILayout.EndScrollView();

            // Boton para agregar mapeo manual (selecciona hueso del avatar)
            EditorGUILayout.Space(3);
            if (GUILayout.Button(MRLocalization.Get(L.CoserRopaExtra.ADD_MAPPING), EditorStyles.miniButton))
            {
                AddManualMappingFromAvatar(piece);
            }
        }

        private void DrawSingleMappingEditor(BoneMapping mapping)
        {
            EditorGUILayout.BeginHorizontal();

            // Nombre del hueso del AVATAR (fijo, referencia confiable)
            string avatarBoneName = mapping.AvatarBone != null
                ? mapping.AvatarBone.name
                : "(sin destino)";

            GUI.enabled = false;
            EditorGUILayout.TextField(avatarBoneName, GUILayout.Width(140));
            GUI.enabled = true;

            // Flecha (invertida: ropa -> avatar)
            EditorGUILayout.LabelField("←", GUILayout.Width(20));

            // ObjectField para hueso de la ROPA (clic para ping, drag para cambiar)
            EditorGUI.BeginChangeCheck();
            var newBone = (Transform)EditorGUILayout.ObjectField(
                mapping.PieceBone,
                typeof(Transform),
                true);

            if (EditorGUI.EndChangeCheck() && newBone != mapping.PieceBone)
            {
                Undo.RecordObject(_target, "Cambiar mapeo de hueso");
                mapping.PieceBone = newBone;
                mapping.MappingMethod = newBone != null
                    ? BoneMappingMethod.ManualAssignment
                    : BoneMappingMethod.None;
                EditorUtility.SetDirty(_target);
            }

            // Indicador de metodo
            Color indicatorColor = mapping.MappingMethod switch
            {
                BoneMappingMethod.HumanoidMapping => EnabledColor,
                BoneMappingMethod.NameMatching => WarningColor,
                BoneMappingMethod.ManualAssignment => new Color(0.5f, 0.7f, 1f),
                _ => DisabledColor
            };

            string indicator = mapping.MappingMethod switch
            {
                BoneMappingMethod.HumanoidMapping => "Auto",
                BoneMappingMethod.NameMatching => "Nombre",
                BoneMappingMethod.ManualAssignment => "Manual",
                _ => "-"
            };

            GUI.contentColor = indicatorColor;
            EditorGUILayout.LabelField(indicator, EditorStyles.miniLabel, GUILayout.Width(45));
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void AddManualMappingFromAvatar(PieceEntry piece)
        {
            // Obtener huesos del avatar que no estan mapeados
            var avatarBones = _target.GetAvatarBones();
            if (avatarBones.Length == 0)
            {
                EditorUtility.DisplayDialog("Aviso",
                    "No se encontraron huesos en el avatar.", "OK");
                return;
            }

            // Usar GenericMenu para seleccionar hueso del avatar
            var menu = new GenericMenu();
            foreach (var avatarBone in avatarBones)
            {
                // Verificar que no este ya mapeado
                bool alreadyMapped = piece.BoneMappings.Any(m => m.AvatarBone == avatarBone);
                if (!alreadyMapped)
                {
                    var capturedBone = avatarBone;
                    menu.AddItem(new GUIContent(avatarBone.name), false, () =>
                    {
                        Undo.RecordObject(_target, "Agregar mapeo manual");
                        var mapping = new BoneMapping
                        {
                            AvatarBone = capturedBone,
                            PieceBone = null, // Usuario arrastrara el hueso de la ropa
                            MappingMethod = BoneMappingMethod.ManualAssignment
                        };
                        piece.BoneMappings.Add(mapping);
                        EditorUtility.SetDirty(_target);
                    });
                }
            }

            if (menu.GetItemCount() == 0)
            {
                EditorUtility.DisplayDialog("Aviso",
                    "Todos los huesos del avatar ya estan mapeados.", "OK");
                return;
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Busca el Armature hijo de una pieza para reubicar BoneProxy.
        /// Prioriza ArmatureReference.ArmatureRoot, fallback por nombre "Armature".
        /// </summary>
        private GameObject FindArmatureForRelocation(PieceEntry piece)
        {
            if (piece?.GameObject == null)
                return null;

            // Prioridad 1: ArmatureReference ya detectado
            if (piece.ArmatureReference?.ArmatureRoot != null)
            {
                var armRoot = piece.ArmatureReference.ArmatureRoot;
                // Verificar que es hijo directo de la pieza
                if (armRoot.parent == piece.GameObject.transform)
                    return armRoot.gameObject;
            }

            // Prioridad 2: buscar hijo con nombre "Armature" (case-insensitive)
            for (int i = 0; i < piece.GameObject.transform.childCount; i++)
            {
                var child = piece.GameObject.transform.GetChild(i);
                if (child.name.ToLowerInvariant().Contains("armature"))
                    return child.gameObject;
            }

            // Prioridad 3: primer hijo que no sea un SkinnedMeshRenderer directo
            for (int i = 0; i < piece.GameObject.transform.childCount; i++)
            {
                var child = piece.GameObject.transform.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() == null &&
                    child.GetComponent<MeshRenderer>() == null)
                    return child.gameObject;
            }

            return null;
        }

        #endregion

        #region Zone Helpers

        private string GetZoneShortLabel(StitchZone zone)
        {
            return zone switch
            {
                StitchZone.FullBody => MRLocalization.Get(L.CoserRopaExtra.ZONE_FULL_BODY_SHORT),
                StitchZone.Torso => MRLocalization.Get(L.CoserRopaExtra.ZONE_TORSO_SHORT),
                StitchZone.Head => MRLocalization.Get(L.CoserRopaExtra.ZONE_HEAD_SHORT),
                StitchZone.UpperLimb => MRLocalization.Get(L.CoserRopaExtra.ZONE_UPPER_LIMB_SHORT),
                StitchZone.LowerLimb => MRLocalization.Get(L.CoserRopaExtra.ZONE_LOWER_LIMB_SHORT),
                StitchZone.Hip => MRLocalization.Get(L.CoserRopaExtra.ZONE_HIP_SHORT),
                StitchZone.RightHand => MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_HAND_SHORT),
                StitchZone.LeftHand => MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_HAND_SHORT),
                StitchZone.RightFoot => MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_FOOT_SHORT),
                StitchZone.LeftFoot => MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_FOOT_SHORT),
                StitchZone.Chest => MRLocalization.Get(L.CoserRopaExtra.ZONE_CHEST_SHORT),
                StitchZone.None => MRLocalization.Get(L.CoserRopaExtra.ZONE_NONE_SHORT),
                _ => ""
            };
        }

        private string GetZoneFullLabel(StitchZone zone)
        {
            return zone switch
            {
                StitchZone.FullBody => MRLocalization.Get(L.CoserRopaExtra.ZONE_FULL_BODY),
                StitchZone.Torso => MRLocalization.Get(L.CoserRopaExtra.ZONE_TORSO),
                StitchZone.Head => MRLocalization.Get(L.CoserRopaExtra.ZONE_HEAD),
                StitchZone.UpperLimb => MRLocalization.Get(L.CoserRopaExtra.ZONE_UPPER_LIMB),
                StitchZone.LowerLimb => MRLocalization.Get(L.CoserRopaExtra.ZONE_LOWER_LIMB),
                StitchZone.Hip => MRLocalization.Get(L.CoserRopaExtra.ZONE_HIP),
                StitchZone.RightHand => MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_HAND),
                StitchZone.LeftHand => MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_HAND),
                StitchZone.RightFoot => MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_FOOT),
                StitchZone.LeftFoot => MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_FOOT),
                StitchZone.Chest => MRLocalization.Get(L.CoserRopaExtra.ZONE_CHEST),
                StitchZone.None => MRLocalization.Get(L.CoserRopaExtra.ZONE_NONE),
                _ => ""
            };
        }

        /// <summary>
        /// Obtiene nombres cortos localizados para el popup de zona en la lista.
        /// Incluye "(Auto)" como primera opción, seguido de las 12 zonas.
        /// </summary>
        private string[] GetZoneShortPopupNames()
        {
            return new[] {
                MRLocalization.Get(L.CoserRopaExtra.MANUAL_ZONE_AUTO),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_FULL_BODY_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_TORSO_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_HEAD_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_UPPER_LIMB_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LOWER_LIMB_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_HIP_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_HAND_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_HAND_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_RIGHT_FOOT_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_LEFT_FOOT_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_CHEST_SHORT),
                MRLocalization.Get(L.CoserRopaExtra.ZONE_NONE_SHORT)
            };
        }

        /// <summary>
        /// Genera etiqueta numerada para PieceType (ej: "Ropa 1", "Pelo 2").
        /// Cuenta cuántas piezas del mismo tipo hay antes del índice dado.
        /// </summary>
        private string GetNumberedPieceTypeLabel(int index)
        {
            if (index < 0 || index >= _target.DetectedPieceCount)
                return "";

            var piece = _target.DetectedPieces[index];
            var pieceType = piece.PieceType;
            string[] typeNames = GetPieceTypePopupNames();
            string typeName = typeNames[(int)pieceType];

            // Contar cuántas piezas del mismo tipo hay en total
            int sameTypeCount = 0;
            int numberInType = 0;
            for (int i = 0; i < _target.DetectedPieceCount; i++)
            {
                if (_target.DetectedPieces[i].PieceType == pieceType)
                {
                    sameTypeCount++;
                    if (i < index)
                        numberInType++;
                }
            }
            numberInType++; // 1-based

            // Solo numerar si hay más de 1 pieza del mismo tipo
            if (sameTypeCount > 1)
                return $"{typeName} {numberInType}";
            return typeName;
        }

        /// <summary>
        /// Muestra GenericMenu para cambiar PieceType de una pieza.
        /// </summary>
        private void ShowPieceTypeMenu(int index)
        {
            if (index < 0 || index >= _target.DetectedPieceCount) return;

            var piece = _target.DetectedPieces[index];
            string[] typeNames = GetPieceTypePopupNames();
            var menu = new GenericMenu();

            for (int i = 0; i < typeNames.Length; i++)
            {
                int capturedType = i;
                bool isActive = (int)piece.PieceType == i;
                menu.AddItem(new GUIContent(typeNames[i]), isActive, () =>
                {
                    Undo.RecordObject(_target, "Cambiar tipo pieza");
                    piece.PieceType = (PieceType)capturedType;
                    piece.PieceTypeManuallySet = true;
                    EditorUtility.SetDirty(_target);
                });
            }

            menu.ShowAsContext();
        }

        #endregion

        #region NDMF Info

        private void DrawNDMFInfo()
        {
            // Resumen de estado
            int enabledCount = _target.EnabledPieceCount;
            int totalBones = _target.TotalMappedBones;
            int maCount = _target.ModularAvatarPieceCount;

            // Separar piezas MA activas de las que tienen DisableMA
            var maPieces = _target.ModularAvatarPieces;
            int maActiveCount = 0;
            int maDisabledCount = 0;
            foreach (var piece in maPieces)
            {
                if (piece.DisableMA)
                    maDisabledCount++;
                else
                    maActiveCount++;
            }

            // Mostrar info de Modular Avatar si hay prendas con MA activo
            if (maActiveCount > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = ModularAvatarColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.NDMF_BEHAVIOR, maActiveCount), EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.CoserRopaExtra.NDMF_DURING_BUILD),
                    EditorStyles.wordWrappedMiniLabel);

                foreach (var piece in maPieces)
                {
                    if (!piece.DisableMA)
                    {
                        EditorGUILayout.LabelField($"  \u2022 {piece.Name} ({piece.ModularAvatarComponentType})",
                            EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // Mostrar piezas con DisableMA (MR procesara)
            if (maDisabledCount > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.NDMF_DISABLE_MA_INFO, maDisabledCount), EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                foreach (var piece in maPieces)
                {
                    if (piece.DisableMA)
                    {
                        string label = piece.PieceType == PieceType.Pelo
                            ? $"  \u26a0 {piece.Name} ({MRLocalization.Get(L.CoserRopaExtra.PIECE_TYPE_PELO)})"
                            : $"  \u2022 {piece.Name}";
                        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (enabledCount > 0 && totalBones > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.CoserRopaExtra.NDMF_ENABLED_INFO, enabledCount, totalBones), EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.CoserRopaExtra.NDMF_DISABLED_INFO),
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.CoserRopaExtra.NDMF_SCOPE),
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
            }
            else if (enabledCount > 0)
            {
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.CoserRopaExtra.NDMF_SCENE_SAFE),
                    MessageType.Warning);
            }
            else if (maActiveCount == 0 && maDisabledCount == 0)
            {
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.CoserRopaExtra.NDMF_NO_PIECES_ENABLED),
                    MessageType.Info);
            }
        }

        #endregion
    }
}
