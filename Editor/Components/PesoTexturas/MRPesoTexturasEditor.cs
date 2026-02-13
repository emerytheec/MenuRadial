using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.PesoTexturas;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Editor personalizado para MRPesoTexturas.
    /// Proporciona interfaz visual para escanear, analizar y optimizar el peso de texturas.
    /// </summary>
    [CustomEditor(typeof(MRPesoTexturas))]
    public class MRPesoTexturasEditor : UnityEditor.Editor
    {
        private MRPesoTexturas _target;
        private Vector2 _groupListScrollPos;
        private Vector2 _textureListScrollPos;
        private bool _showReduceOptions = false;

        // Colores
        private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color HighlightColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color SavingsColor = new Color(0.2f, 0.9f, 0.4f);

        private void OnEnable()
        {
            _target = (MRPesoTexturas)target;
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            // Header
            DrawHeader();
            EditorGUILayout.Space(5);

            // Avatar
            DrawAvatarSection();

            // Solo mostrar el resto si hay avatar
            if (_target.AvatarRoot != null)
            {
                EditorGUILayout.Space(8);

                // Opciones de escaneo
                DrawScanOptions();

                EditorGUILayout.Space(8);

                // Boton de escaneo
                DrawScanButton();

                // Si ya hay escaneo, mostrar resultados
                if (_target.IsScanned)
                {
                    EditorGUILayout.Space(8);

                    // Resumen general
                    DrawSummarySection();

                    EditorGUILayout.Space(8);

                    // Advertencia de Mip Streaming
                    DrawMipStreamingWarning();

                    EditorGUILayout.Space(8);

                    // Preview de ahorro (incluye boton de reducir)
                    DrawSavingsPreview();

                    EditorGUILayout.Space(8);

                    // Lista de grupos
                    DrawGroupList();
                }
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.PesoTexturas.DROP_AVATAR_HINT),
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Header & Avatar

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.TITLE), EditorStyleManager.HeaderStyle);
            EditorGUILayout.LabelField(
                MRLocalization.Get(L.PesoTexturas.SUBTITLE),
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawAvatarSection()
        {
            EditorGUI.BeginChangeCheck();
            var newAvatar = (GameObject)EditorGUILayout.ObjectField(
                "Avatar",
                _target.AvatarRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newAvatar != _target.AvatarRoot)
            {
                Undo.RecordObject(_target, "Cambiar Avatar");
                _target.AvatarRoot = newAvatar;
                EditorUtility.SetDirty(_target);
            }
        }

        #endregion

        #region Scan Options

        private void DrawScanOptions()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.SCAN_OPTIONS), EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Incluir avatar base
            EditorGUI.BeginChangeCheck();
            bool newIncludeBase = EditorGUILayout.Toggle(
                new GUIContent(MRLocalization.Get(L.PesoTexturas.INCLUDE_AVATAR_BASE), MRLocalization.Get(L.PesoTexturas.INCLUDE_AVATAR_BASE_TOOLTIP)),
                _target.IncludeAvatarBase);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Incluir Avatar Base");
                _target.IncludeAvatarBase = newIncludeBase;
                EditorUtility.SetDirty(_target);
            }

            // Incluir ropas
            EditorGUI.BeginChangeCheck();
            bool newIncludeClothing = EditorGUILayout.Toggle(
                new GUIContent(MRLocalization.Get(L.PesoTexturas.INCLUDE_CLOTHING), MRLocalization.Get(L.PesoTexturas.INCLUDE_CLOTHING_TOOLTIP)),
                _target.IncludeClothing);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Incluir Ropas");
                _target.IncludeClothing = newIncludeClothing;
                EditorUtility.SetDirty(_target);
            }

            // Incluir materiales alternativos
            EditorGUI.BeginChangeCheck();
            bool newIncludeAltMaterials = EditorGUILayout.Toggle(
                new GUIContent(MRLocalization.Get(L.PesoTexturas.INCLUDE_ALT_MATERIALS), MRLocalization.Get(L.PesoTexturas.INCLUDE_ALT_MATERIALS_TOOLTIP)),
                _target.IncludeAlternativeMaterials);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Incluir Mat. Alternativos");
                _target.IncludeAlternativeMaterials = newIncludeAltMaterials;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Scan Button

        private void DrawScanButton()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = HighlightColor;
            if (GUILayout.Button(
                new GUIContent(MRLocalization.Get(L.PesoTexturas.SCAN_BUTTON), MRLocalization.Get(L.PesoTexturas.SCAN_BUTTON_TOOLTIP)),
                GUILayout.Height(30)))
            {
                ScanTextures();
            }
            GUI.backgroundColor = Color.white;

            // Boton Limpiar (solo visible si hay escaneo)
            if (_target.IsScanned)
            {
                if (GUILayout.Button(
                    new GUIContent(MRLocalization.Get(L.PesoTexturas.CLEAR_BUTTON), MRLocalization.Get(L.PesoTexturas.CLEAR_BUTTON_TOOLTIP)),
                    GUILayout.Height(30),
                    GUILayout.Width(80)))
                {
                    Undo.RecordObject(_target, "Limpiar Escaneo");
                    _target.ClearScanResults();
                    EditorUtility.SetDirty(_target);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Summary Section

        private void DrawSummarySection()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.SUMMARY), EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Determinar si excede el limite de 500 MB
            bool exceedsLimit = _target.ExceedsVRChatLimit;

            // === DESGLOSE ===
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.VRAM_BREAKDOWN), EditorStyles.miniBoldLabel);

            // Texturas
            DrawAssetRow(MRLocalization.Get(L.PesoTexturas.TEXTURES), _target.TotalEstimatedVRAM, $"{_target.TotalTextureCount} texturas");

            // Meshes
            if (_target.MeshVRAM > 0)
            {
                DrawAssetRow(MRLocalization.Get(L.PesoTexturas.MESHES), _target.MeshVRAM, $"{_target.MeshCount} meshes");
            }

            // Blend Shapes
            if (_target.BlendShapeVRAM > 0)
            {
                DrawAssetRow(MRLocalization.Get(L.PesoTexturas.BLEND_SHAPES), _target.BlendShapeVRAM, $"{_target.BlendShapeCount} shapes");
            }

            // Animaciones
            if (_target.AnimationSize > 0)
            {
                DrawAssetRow(MRLocalization.Get(L.PesoTexturas.ANIMATIONS), _target.AnimationSize, $"{_target.AnimationClipCount} clips");
            }

            // Materiales
            if (_target.MaterialSize > 0)
            {
                DrawAssetRow(MRLocalization.Get(L.PesoTexturas.MATERIALS), _target.MaterialSize, null);
            }

            // Audio
            if (_target.AudioSize > 0)
            {
                DrawAssetRow(MRLocalization.Get(L.PesoTexturas.AUDIO), _target.AudioSize, null);
            }

            EditorGUILayout.Space(5);

            // Linea separadora
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            EditorGUILayout.Space(3);

            // === TOTAL DEL BUNDLE ===
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.TOTAL_BUNDLE), EditorStyles.boldLabel, GUILayout.Width(100));
            GUI.contentColor = exceedsLimit ? ErrorColor : Color.white;
            EditorGUILayout.LabelField(_target.TotalBundleSizeLabel, EditorStyles.boldLabel);
            GUI.contentColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Advertencia si excede el limite
            if (exceedsLimit)
            {
                EditorGUILayout.Space(2);
                GUI.contentColor = ErrorColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.EXCEEDS_LIMIT), EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.Space(3);

            // Info adicional compacta
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Grupos: {_target.GroupCount}", EditorStyles.miniLabel, GUILayout.Width(80));
            if (_target.MaxResolutionFound > 0)
            {
                EditorGUILayout.LabelField($"Max res: {_target.MaxResolutionFound}px", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Dibuja una fila de asset en el desglose.
        /// </summary>
        private void DrawAssetRow(string label, long bytes, string detail)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {label}:", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(VRChatTextureWeightCalculator.FormatBytes(bytes), EditorStyles.miniLabel, GUILayout.Width(70));
            if (!string.IsNullOrEmpty(detail))
            {
                GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField($"({detail})", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Mip Streaming Warning

        private void DrawMipStreamingWarning()
        {
            int missingCount = _target.TexturesWithoutMipStreamingCount;

            if (missingCount == 0)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Icono de advertencia y mensaje
            GUI.contentColor = ErrorColor;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                EditorGUIUtility.IconContent("console.erroricon.sml"),
                GUILayout.Width(20));
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.MIP_STREAMING_DISABLED), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.contentColor = Color.white;

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField(
                MRLocalization.Get(L.PesoTexturas.MIP_STREAMING_COUNT, missingCount),
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                MRLocalization.Get(L.PesoTexturas.MIP_STREAMING_REQUIRED),
                EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Boton para habilitar Mip Streaming
            GUI.backgroundColor = ErrorColor;
            if (GUILayout.Button(
                new GUIContent(MRLocalization.Get(L.PesoTexturas.ENABLE_MIP_STREAMING, missingCount),
                    MRLocalization.Get(L.PesoTexturas.ENABLE_MIP_STREAMING_TOOLTIP)),
                GUILayout.Height(25)))
            {
                EnableMipStreamingInAllTextures();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void EnableMipStreamingInAllTextures()
        {
            var texturesToFix = _target.GetTexturesWithoutMipStreaming().ToList();

            if (texturesToFix.Count == 0)
            {
                Debug.Log("[MRPesoTexturas] No hay texturas que requieran Mip Streaming.");
                return;
            }

            int fixedCount = TextureScanner.EnableMipStreamingInList(texturesToFix);

            EditorUtility.SetDirty(_target);

            if (fixedCount > 0)
            {
                Debug.Log($"[MRPesoTexturas] Se habilito Mip Streaming en {fixedCount} textura(s).");
                EditorUtility.DisplayDialog(
                    MRLocalization.Get(L.PesoTexturas.MIP_STREAMING_ENABLED),
                    MRLocalization.Get(L.PesoTexturas.MIP_STREAMING_RESULT, fixedCount),
                    MRLocalization.Get(L.Common.OK));
            }
            else
            {
                Debug.LogWarning("[MRPesoTexturas] No se pudo habilitar Mip Streaming en ninguna textura.");
            }
        }

        #endregion

        #region Savings Preview

        private void DrawSavingsPreview()
        {
            // Verificar si hay algo que mostrar
            bool canStepDown = _target.CanStepDown();
            int historyCount = TextureHistoryManager.GetHistoryCount();

            if (!canStepDown && historyCount == 0)
                return;

            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.OPTIMIZATION), EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Mostrar preview de ahorro si se puede reducir
            if (canStepDown)
            {
                long currentTotal = _target.TotalEstimatedVRAM;
                long afterStepDown = _target.GetTotalAfterStepDown();
                long savings = currentTotal - afterStepDown;
                float savingsPercent = _target.GetOverallSavingsPercentage();

                // Preview visual
                GUI.contentColor = SavingsColor;
                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.PesoTexturas.STEP_DOWN_PREVIEW, VRChatTextureWeightCalculator.FormatBytes(currentTotal), VRChatTextureWeightCalculator.FormatBytes(afterStepDown)),
                    EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.PesoTexturas.SAVINGS, VRChatTextureWeightCalculator.FormatBytes(savings), savingsPercent),
                    EditorStyles.miniLabel);
            }

            // Mostrar info de texturas modificadas si hay historial
            if (historyCount > 0)
            {
                if (canStepDown)
                    EditorGUILayout.Space(3);

                GUI.contentColor = WarningColor;
                EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.MODIFIED_TEXTURES, historyCount), EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.Space(5);

            // Foldout para mostrar botones
            _showReduceOptions = EditorGUILayout.Foldout(_showReduceOptions, MRLocalization.Get(L.PesoTexturas.OPTIONS), true);

            if (_showReduceOptions)
            {
                EditorGUILayout.Space(3);

                // Boton reducir (si se puede)
                if (canStepDown)
                {
                    GUI.backgroundColor = SavingsColor;
                    if (GUILayout.Button(
                        new GUIContent(MRLocalization.Get(L.PesoTexturas.REDUCE_ALL), MRLocalization.Get(L.PesoTexturas.REDUCE_ALL_TOOLTIP)),
                        GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog(
                            MRLocalization.Get(L.PesoTexturas.CONFIRM_REDUCTION),
                            MRLocalization.Get(L.PesoTexturas.CONFIRM_REDUCTION_MSG, VRChatTextureWeightCalculator.FormatBytes(_target.GetPotentialSavings())),
                            MRLocalization.Get(L.PesoTexturas.REDUCE), MRLocalization.Get(L.Common.CANCEL)))
                        {
                            StepDownAllTextures();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                // Boton restaurar (si hay historial)
                if (historyCount > 0)
                {
                    EditorGUILayout.Space(3);
                    GUI.backgroundColor = WarningColor;
                    if (GUILayout.Button(
                        new GUIContent(MRLocalization.Get(L.PesoTexturas.RESTORE_ALL, historyCount), MRLocalization.Get(L.PesoTexturas.RESTORE_ALL_TOOLTIP)),
                        GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog(
                            MRLocalization.Get(L.PesoTexturas.CONFIRM_RESTORE),
                            MRLocalization.Get(L.PesoTexturas.CONFIRM_RESTORE_MSG, historyCount),
                            MRLocalization.Get(L.PesoTexturas.RESTORE), MRLocalization.Get(L.Common.CANCEL)))
                        {
                            RestoreAllTextures();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Group List

        private void DrawGroupList()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.TEXTURE_GROUPS), EditorStyles.boldLabel);

            if (_target.GroupCount == 0)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.PesoTexturas.NO_TEXTURES_FOUND), MessageType.Info);
                return;
            }

            _groupListScrollPos = EditorGUILayout.BeginScrollView(_groupListScrollPos, GUILayout.MaxHeight(400));

            foreach (var group in _target.TextureGroups)
            {
                DrawGroupEntry(group);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupEntry(TextureGroupEntry group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header del grupo
            EditorGUILayout.BeginHorizontal();

            // Toggle habilitado
            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUILayout.Toggle(group.IsEnabled, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle Grupo");
                group.IsEnabled = newEnabled;
                EditorUtility.SetDirty(_target);
            }

            // Foldout
            group.IsExpanded = EditorGUILayout.Foldout(group.IsExpanded,
                $"{group.SourceName} ({group.TextureCount})", true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            // Peso del grupo
            string weightLabel = group.TotalWeightLabel;
            GUI.contentColor = group.HasHighWeightTextures ? WarningColor : Color.white;
            EditorGUILayout.LabelField(weightLabel, EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.contentColor = Color.white;

            // Max res
            EditorGUILayout.LabelField($"{group.MaxResolution}px", EditorStyles.miniLabel, GUILayout.Width(50));

            // Boton restaurar grupo (si hay texturas modificadas)
            int restorableCount = TextureScanner.GetRestorableCountInGroup(group);
            GUI.enabled = restorableCount > 0;
            GUI.backgroundColor = WarningColor;
            if (GUILayout.Button(new GUIContent("↺", MRLocalization.Get(L.PesoTexturas.RESTORE_GROUP_TOOLTIP, restorableCount)), GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog(
                    MRLocalization.Get(L.PesoTexturas.CONFIRM_RESTORE),
                    MRLocalization.Get(L.PesoTexturas.CONFIRM_RESTORE_GROUP, restorableCount, group.SourceName),
                    MRLocalization.Get(L.PesoTexturas.RESTORE), MRLocalization.Get(L.Common.CANCEL)))
                {
                    RestoreGroup(group);
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            // Boton step-down individual
            GUI.enabled = group.IsEnabled && group.CanStepDown();
            GUI.backgroundColor = SavingsColor;
            if (GUILayout.Button(new GUIContent("↓", MRLocalization.Get(L.PesoTexturas.STEP_DOWN_TOOLTIP)), GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog(
                    MRLocalization.Get(L.PesoTexturas.CONFIRM_REDUCTION),
                    MRLocalization.Get(L.PesoTexturas.CONFIRM_REDUCE_GROUP, group.SourceName, VRChatTextureWeightCalculator.FormatBytes(group.GetPotentialSavings())),
                    MRLocalization.Get(L.PesoTexturas.REDUCE), MRLocalization.Get(L.Common.CANCEL)))
                {
                    StepDownGroup(group);
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Contenido expandido
            if (group.IsExpanded)
            {
                EditorGUI.indentLevel++;
                DrawGroupDetails(group);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGroupDetails(TextureGroupEntry group)
        {
            EditorGUILayout.Space(3);

            // Info del grupo
            string typeLabel = group.GroupType switch
            {
                TextureGroupType.AvatarBase => MRLocalization.Get(L.PesoTexturas.AVATAR_BASE),
                TextureGroupType.Clothing => MRLocalization.Get(L.PesoTexturas.CLOTHING),
                TextureGroupType.AlternativeMaterials => MRLocalization.Get(L.PesoTexturas.ALT_MATERIALS),
                _ => MRLocalization.Get(L.MenuRadialEditor.UNKNOWN)
            };
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.TYPE_LABEL, typeLabel), EditorStyles.miniLabel);

            if (group.SourceObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.SOURCE), EditorStyles.miniLabel, GUILayout.Width(50));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(group.SourceObject, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Calcular ancho disponible para la tabla
            float totalWidth = EditorGUIUtility.currentViewWidth - 70f;
            float fixedColumnsWidth = 110f + 55f + 45f + 38f; // Resolucion + Peso + Alpha + Mips
            float nameWidth = Mathf.Max(80f, totalWidth - fixedColumnsWidth);

            // Header de tabla
            var headerStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 20f };
            EditorGUILayout.BeginHorizontal(headerStyle);
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.COL_TEXTURE), EditorStyles.miniBoldLabel, GUILayout.Width(nameWidth));
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.COL_RESOLUTION), EditorStyles.miniBoldLabel, GUILayout.Width(110));
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.COL_WEIGHT), EditorStyles.miniBoldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.COL_ALPHA), EditorStyles.miniBoldLabel, GUILayout.Width(45));
            EditorGUILayout.LabelField(MRLocalization.Get(L.PesoTexturas.COL_MIPS), EditorStyles.miniBoldLabel, GUILayout.Width(38));
            EditorGUILayout.EndHorizontal();

            // Lista de texturas
            int index = 0;
            foreach (var texture in group.GetTexturesByWeight())
            {
                DrawTextureRow(texture, index % 2 == 1, nameWidth);
                index++;
            }
        }

        private void DrawTextureRow(TextureEntry texture, bool alternate, float nameWidth)
        {
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(18));
            if (alternate)
            {
                EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.22f, 0.3f));
            }

            // Preparar nombre con prefijo de tipo
            string prefix = "";
            Color nameColor = Color.white;
            string tooltip = texture.TextureName;

            if (texture.IsFromAlternativeMaterial)
            {
                prefix = "[A] ";
                tooltip = "Material alternativo: " + texture.TextureName;
                nameColor = new Color(0.6f, 0.6f, 0.6f);
            }
            else if (texture.IsNormalMap)
            {
                prefix = "[N] ";
                tooltip = "Normal Map: " + texture.TextureName;
                nameColor = new Color(0.5f, 0.7f, 1f);
            }

            bool isHeavy = VRChatTextureWeightCalculator.IsHighWeight(texture.EstimatedVRAMBytes);
            if (isHeavy && !texture.IsFromAlternativeMaterial)
            {
                nameColor = WarningColor;
            }

            // Columna: Nombre
            GUI.contentColor = nameColor;
            string displayName = prefix + texture.TextureName;
            var content = new GUIContent(displayName, tooltip);

            if (texture.Texture != null)
            {
                if (GUILayout.Button(content, EditorStyles.linkLabel, GUILayout.Width(nameWidth)))
                {
                    EditorGUIUtility.PingObject(texture.Texture);
                }
            }
            else
            {
                EditorGUILayout.LabelField(content, EditorStyles.miniLabel, GUILayout.Width(nameWidth));
            }
            GUI.contentColor = Color.white;

            // Columna: Resolucion (con indicador de modificada)
            string resLabel = texture.ResolutionLabel;
            if (TextureScanner.GetTextureHistoryInfo(texture, out int origSize, out int stepCount))
            {
                // Mostrar resolucion original entre parentesis
                resLabel = $"{texture.ResolutionLabel} ({origSize})";
                GUI.contentColor = WarningColor;
            }
            EditorGUILayout.LabelField(resLabel, EditorStyles.miniLabel, GUILayout.Width(110));
            GUI.contentColor = Color.white;

            // Columna: Peso
            GUI.contentColor = isHeavy ? WarningColor : Color.white;
            EditorGUILayout.LabelField(
                VRChatTextureWeightCalculator.FormatBytesCompact(texture.EstimatedVRAMBytes),
                EditorStyles.miniLabel, GUILayout.Width(55));
            GUI.contentColor = Color.white;

            // Columna: Alpha
            EditorGUILayout.LabelField(texture.HasAlpha ? "Si" : "-", EditorStyles.miniLabel, GUILayout.Width(45));

            // Columna: Mips
            bool needsMipStreaming = texture.HasMipmaps && !texture.HasMipStreaming;
            if (needsMipStreaming)
            {
                GUI.contentColor = ErrorColor;
                EditorGUILayout.LabelField(
                    new GUIContent("!", "Mip Streaming desactivado"),
                    EditorStyles.miniBoldLabel, GUILayout.Width(38));
            }
            else
            {
                GUI.contentColor = SuccessColor;
                EditorGUILayout.LabelField("OK", EditorStyles.miniLabel, GUILayout.Width(38));
            }
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scan Logic

        private void ScanTextures()
        {
            Undo.RecordObject(_target, "Escanear Texturas");

            _target.ClearScanResults();

            var processedPaths = new HashSet<string>();

            // Obtener los GUIDs de materiales referenciados en las animaciones
            // Solo las texturas de estos materiales se incluiran en el build de VRChat
            HashSet<string> referencedMaterialGuids = null;
            if (_target.IncludeAlternativeMaterials)
            {
                referencedMaterialGuids = AnimationMaterialAnalyzer.GetReferencedMaterialGuids(_target.AvatarRoot);
            }

            // Escanear avatar base
            if (_target.IncludeAvatarBase)
            {
                var avatarGroup = ScanAvatarBase(processedPaths, referencedMaterialGuids);
                if (avatarGroup != null && avatarGroup.TextureCount > 0)
                {
                    _target.AddTextureGroup(avatarGroup);
                }
            }

            // Escanear ropas (incluye materiales alternativos si esta habilitado)
            if (_target.IncludeClothing)
            {
                ScanClothings(processedPaths, referencedMaterialGuids);
            }

            // Escanear otros assets (meshes, animaciones, materiales, audio)
            ScanOtherAssets();

            _target.MarkAsScanned();
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MRPesoTexturas] Escaneo completado: {_target.TotalTextureCount} texturas ({_target.TotalSizeLabel}), " +
                      $"Otros assets: {_target.TotalNonTextureSizeLabel}, " +
                      $"Total Bundle: {_target.TotalBundleSizeLabel}");
        }

        /// <summary>
        /// Escanea meshes, animaciones, materiales y audio del avatar.
        /// </summary>
        private void ScanOtherAssets()
        {
            if (_target.AvatarRoot == null)
                return;

            // Escanear todos los assets
            var assetResult = AssetSizeCalculator.ScanAllAssets(_target.AvatarRoot);

            // Actualizar datos de meshes
            _target.MeshVRAM = assetResult.Meshes.TotalVertexVRAM + assetResult.Meshes.TotalIndexVRAM;
            _target.BlendShapeVRAM = assetResult.Meshes.TotalBlendShapeVRAM;
            _target.MeshCount = assetResult.Meshes.MeshCount;
            _target.BlendShapeCount = assetResult.Meshes.TotalBlendShapeCount;

            // Actualizar datos de animaciones
            _target.AnimationSize = assetResult.Animations.TotalSize;
            _target.AnimationClipCount = assetResult.Animations.ClipCount;

            // Actualizar datos de materiales
            _target.MaterialSize = assetResult.Materials.TotalSize;

            // Actualizar datos de audio
            _target.AudioSize = assetResult.Audio.TotalSize;
        }

        private TextureGroupEntry ScanAvatarBase(HashSet<string> processedPaths, HashSet<string> referencedMaterialGuids)
        {
            var group = new TextureGroupEntry(
                _target.AvatarRoot.name,
                _target.AvatarRoot,
                TextureGroupType.AvatarBase);

            // Buscar MRCoserRopa para excluir ropas del avatar base
            var coserRopa = _target.AvatarRoot.GetComponentInChildren<MRCoserRopa>();
            var clothingObjects = new HashSet<GameObject>();

            if (coserRopa != null)
            {
                foreach (var clothing in coserRopa.DetectedClothings)
                {
                    if (clothing.GameObject != null)
                    {
                        clothingObjects.Add(clothing.GameObject);
                    }
                }
            }

            // Escanear solo los renderers que NO son de ropa
            var renderers = _target.AvatarRoot.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                // Verificar si este renderer pertenece a una ropa
                bool isClothing = false;
                Transform current = renderer.transform;
                while (current != null && current != _target.AvatarRoot.transform)
                {
                    if (clothingObjects.Contains(current.gameObject))
                    {
                        isClothing = true;
                        break;
                    }
                    current = current.parent;
                }

                if (isClothing)
                    continue;

                // Escanear texturas de este renderer
                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                foreach (var material in materials)
                {
                    ScanMaterialForGroup(material, group, processedPaths);
                }
            }

            // Escanear materiales alternativos del avatar base
            if (_target.IncludeAlternativeMaterials)
            {
                ScanAlternativeMaterialsForAvatarBase(group, clothingObjects, processedPaths, referencedMaterialGuids);
            }

            return group;
        }

        /// <summary>
        /// Escanea materiales alternativos que pertenecen al avatar base (no a ropas).
        /// Solo incluye texturas de materiales que estan referenciados en animaciones.
        /// </summary>
        private void ScanAlternativeMaterialsForAvatarBase(
            TextureGroupEntry group,
            HashSet<GameObject> clothingObjects,
            HashSet<string> processedPaths,
            HashSet<string> referencedMaterialGuids)
        {
            var allAgruparMateriales = GetAllAgruparMateriales();

            foreach (var agrupar in allAgruparMateriales)
            {
                if (agrupar == null || agrupar.SourceGameObject == null)
                    continue;

                // Verificar si el SourceGameObject es una ropa - si lo es, ignorar
                if (clothingObjects.Contains(agrupar.SourceGameObject))
                    continue;

                // Verificar si el SourceGameObject esta dentro del avatar pero no es una ropa
                // (es decir, es parte del avatar base)
                if (!IsDescendantOf(agrupar.SourceGameObject.transform, _target.AvatarRoot.transform))
                    continue;

                // Escanear todos los materiales de los grupos
                foreach (var materialGroup in agrupar.Groups)
                {
                    if (materialGroup == null)
                        continue;

                    var validMaterials = materialGroup.GetValidMaterials();

                    // Filtrar solo materiales referenciados en animaciones
                    IEnumerable<Material> materialsToScan;
                    if (referencedMaterialGuids != null && referencedMaterialGuids.Count > 0)
                    {
                        materialsToScan = AnimationMaterialAnalyzer.FilterReferencedMaterials(validMaterials, referencedMaterialGuids);
                    }
                    else
                    {
                        materialsToScan = validMaterials;
                    }

                    var textures = TextureScanner.ScanMaterials(materialsToScan, processedPaths, isFromAlternativeMaterial: true);

                    foreach (var texture in textures)
                    {
                        group.AddTexture(texture);
                    }
                }
            }
        }

        /// <summary>
        /// Verifica si un transform es descendiente de otro
        /// </summary>
        private bool IsDescendantOf(Transform child, Transform parent)
        {
            if (child == null || parent == null)
                return false;

            Transform current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private void ScanClothings(HashSet<string> processedPaths, HashSet<string> referencedMaterialGuids)
        {
            var coserRopa = _target.AvatarRoot.GetComponentInChildren<MRCoserRopa>();
            if (coserRopa == null)
                return;

            // Obtener todos los MRAgruparMateriales para buscar materiales alternativos
            var allAgruparMateriales = GetAllAgruparMateriales();

            foreach (var clothing in coserRopa.DetectedClothings)
            {
                if (clothing.GameObject == null)
                    continue;

                var group = new TextureGroupEntry(
                    clothing.Name,
                    clothing.GameObject,
                    TextureGroupType.Clothing);

                // 1. Escanear texturas actuales de la ropa
                var textures = TextureScanner.ScanTextures(clothing.GameObject, processedPaths);
                foreach (var texture in textures)
                {
                    group.AddTexture(texture);
                }

                // 2. Escanear texturas de materiales alternativos de esta ropa
                if (_target.IncludeAlternativeMaterials)
                {
                    ScanAlternativeMaterialsForClothing(clothing.GameObject, group, allAgruparMateriales, processedPaths, referencedMaterialGuids);
                }

                if (group.TextureCount > 0)
                {
                    _target.AddTextureGroup(group);
                }
            }
        }

        /// <summary>
        /// Obtiene todos los MRAgruparMateriales de la jerarquia del MRMenuRadial y del avatar
        /// </summary>
        private List<MRAgruparMateriales> GetAllAgruparMateriales()
        {
            var result = new List<MRAgruparMateriales>();

            // Buscar en la jerarquia del MRMenuRadial (parent de MRPesoTexturas)
            var menuRadialTransform = _target.transform.parent;
            if (menuRadialTransform != null)
            {
                result.AddRange(menuRadialTransform.GetComponentsInChildren<MRAgruparMateriales>(true));
            }

            // Buscar en el avatar root tambien
            if (_target.AvatarRoot != null)
            {
                var avatarAgrupars = _target.AvatarRoot.GetComponentsInChildren<MRAgruparMateriales>(true);
                foreach (var agrupar in avatarAgrupars)
                {
                    if (!result.Contains(agrupar))
                        result.Add(agrupar);
                }
            }

            return result;
        }

        /// <summary>
        /// Escanea materiales alternativos que pertenecen a una ropa especifica
        /// y los agrega al grupo de esa ropa.
        /// Solo incluye texturas de materiales que estan referenciados en animaciones.
        /// </summary>
        private void ScanAlternativeMaterialsForClothing(
            GameObject clothingObject,
            TextureGroupEntry group,
            List<MRAgruparMateriales> allAgruparMateriales,
            HashSet<string> processedPaths,
            HashSet<string> referencedMaterialGuids)
        {
            foreach (var agrupar in allAgruparMateriales)
            {
                if (agrupar == null)
                    continue;

                // Verificar si este MRAgruparMateriales pertenece a esta ropa
                if (agrupar.SourceGameObject != clothingObject)
                    continue;

                // Escanear todos los materiales de los grupos
                foreach (var materialGroup in agrupar.Groups)
                {
                    if (materialGroup == null)
                        continue;

                    var validMaterials = materialGroup.GetValidMaterials();

                    // Filtrar solo materiales referenciados en animaciones
                    IEnumerable<Material> materialsToScan;
                    if (referencedMaterialGuids != null && referencedMaterialGuids.Count > 0)
                    {
                        materialsToScan = AnimationMaterialAnalyzer.FilterReferencedMaterials(validMaterials, referencedMaterialGuids);
                    }
                    else
                    {
                        materialsToScan = validMaterials;
                    }

                    var textures = TextureScanner.ScanMaterials(materialsToScan, processedPaths, isFromAlternativeMaterial: true);

                    foreach (var texture in textures)
                    {
                        group.AddTexture(texture);
                    }
                }
            }
        }

        private void ScanMaterialForGroup(Material material, TextureGroupEntry group, HashSet<string> processedPaths)
        {
            if (material == null)
                return;

            var shader = material.shader;
            if (shader == null)
                return;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;

                if (texture == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(texture);
                // Ignorar texturas que VRChat no cuenta:
                // - Resources/unity_builtin: texturas built-in de Unity
                // - Packages/: texturas de paquetes instalados localmente
                // - Library/: texturas de paquetes en cache (lilToon, VRChat SDK, etc.)
                if (string.IsNullOrEmpty(assetPath) ||
                    assetPath.StartsWith("Resources/") ||
                    assetPath.StartsWith("Packages/") ||
                    assetPath.StartsWith("Library/") ||
                    assetPath.Contains("unity_builtin"))
                    continue;

                if (processedPaths.Contains(assetPath))
                    continue;

                processedPaths.Add(assetPath);

                // Pasar el nombre de la propiedad para mejor deteccion de normal maps
                var entry = TextureScanner.CreateTextureEntry(texture, assetPath, false, propertyName);
                if (entry != null)
                {
                    group.AddTexture(entry);
                }
            }
        }

        #endregion

        #region Step Down Logic

        private void StepDownAllTextures()
        {
            int totalModified = 0;

            foreach (var group in _target.GetEnabledGroups())
            {
                totalModified += TextureScanner.ApplyStepDownToGroup(group);
            }

            _target.RecalculateTotal();
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MRPesoTexturas] Se redujeron {totalModified} texturas. Nuevo peso: {_target.TotalSizeLabel}");
        }

        private void StepDownGroup(TextureGroupEntry group)
        {
            int modified = TextureScanner.ApplyStepDownToGroup(group);

            _target.RecalculateTotal();
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MRPesoTexturas] Se redujeron {modified} texturas en '{group.SourceName}'. Nuevo peso del grupo: {group.TotalWeightLabel}");
        }

        #endregion

        #region Restore Logic

        private void RestoreAllTextures()
        {
            int restored = TextureScanner.RestoreAllFromHistory();

            // Re-escanear para actualizar los valores
            if (restored > 0)
            {
                ScanTextures();
            }

            Debug.Log($"[MRPesoTexturas] Se restauraron {restored} texturas a su resolucion original.");

            EditorUtility.DisplayDialog(
                MRLocalization.Get(L.PesoTexturas.RESTORE_COMPLETED),
                MRLocalization.Get(L.PesoTexturas.RESTORE_RESULT, restored),
                MRLocalization.Get(L.Common.OK));
        }

        private void RestoreGroup(TextureGroupEntry group)
        {
            int restored = TextureScanner.RestoreGroup(group);

            _target.RecalculateTotal();
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MRPesoTexturas] Se restauraron {restored} texturas en '{group.SourceName}'.");
        }

        #endregion
    }
}
