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

                    // Preview de ahorro
                    DrawSavingsPreview();

                    EditorGUILayout.Space(8);

                    // Botones de accion global
                    DrawGlobalActions();

                    EditorGUILayout.Space(8);

                    // Lista de grupos
                    DrawGroupList();
                }
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "Arrastra tu avatar aqui para escanear sus texturas y calcular el peso estimado en VRAM.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Header & Avatar

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MR Peso Texturas", EditorStyleManager.HeaderStyle);
            EditorGUILayout.LabelField(
                "Analiza el peso de texturas (VRAM) del avatar",
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
            EditorGUILayout.LabelField("Opciones de Escaneo", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Incluir avatar base
            EditorGUI.BeginChangeCheck();
            bool newIncludeBase = EditorGUILayout.Toggle(
                new GUIContent("Incluir Avatar Base", "Texturas del body, head, etc."),
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
                new GUIContent("Incluir Ropas", "Texturas de ropas detectadas por MRCoserRopa"),
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
                new GUIContent("Incluir Mat. Alternativos", "Texturas de MRUnificarMateriales y MRAgruparMateriales"),
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
            GUI.backgroundColor = HighlightColor;
            if (GUILayout.Button(
                new GUIContent("Escanear Texturas", "Analiza todas las texturas del avatar y ropas"),
                GUILayout.Height(30)))
            {
                ScanTextures();
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Summary Section

        private void DrawSummarySection()
        {
            EditorGUILayout.LabelField("Resumen", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Estado basado en el peso actual (lo que VRChat contara)
            bool isCurrentHigh = _target.CurrentEstimatedVRAM >= VRChatTextureWeightCalculator.VRCHAT_UNCOMPRESSED_SIZE_LIMIT;
            Color statusColor = isCurrentHigh ? ErrorColor : SuccessColor;
            GUI.contentColor = statusColor;
            EditorGUILayout.LabelField($"[{(isCurrentHigh ? "ALTO" : "OK")}]", EditorStyles.boldLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.Space(3);

            // Peso actual (lo que VRChat contara)
            EditorGUILayout.LabelField("Peso actual (VRChat):", EditorStyles.miniBoldLabel);
            GUI.contentColor = isCurrentHigh ? ErrorColor : SuccessColor;
            EditorGUILayout.LabelField($"  {_target.CurrentSizeLabel} ({_target.CurrentTextureCount} texturas)", EditorStyles.miniBoldLabel);
            GUI.contentColor = Color.white;

            // Si hay materiales alternativos, mostrar el total
            if (_target.AlternativeTextureCount > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Con materiales alternativos:", EditorStyles.miniBoldLabel);
                GUI.contentColor = WarningColor;
                EditorGUILayout.LabelField($"  {_target.TotalSizeLabel} (+{VRChatTextureWeightCalculator.FormatBytes(_target.AlternativeEstimatedVRAM)} en {_target.AlternativeTextureCount} texturas)", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Grupos: {_target.GroupCount}", EditorStyles.miniLabel);

            if (_target.MaxResolutionFound > 0)
            {
                EditorGUILayout.LabelField($"Resolucion maxima: {_target.MaxResolutionFound}px", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            // Mostrar limites de VRChat
            EditorGUILayout.LabelField("Limites de VRChat (PC):", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("  Download Size: 200 MB (archivo comprimido)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  Uncompressed Size: 500 MB (bundle descomprimido)", EditorStyles.miniLabel);

            // Advertencia si supera el limite
            if (isCurrentHigh)
            {
                EditorGUILayout.Space(3);
                GUI.contentColor = ErrorColor;
                EditorGUILayout.LabelField(
                    "El peso actual supera los 500 MB. El avatar podria no subirse.",
                    EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.EndVertical();
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
            EditorGUILayout.LabelField("Mip Streaming Desactivado", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.contentColor = Color.white;

            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField(
                $"{missingCount} textura(s) no tienen Mip Streaming habilitado.",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                "VRChat requiere Mip Streaming para subir el avatar.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Boton para habilitar Mip Streaming
            GUI.backgroundColor = ErrorColor;
            if (GUILayout.Button(
                new GUIContent($"Habilitar Mip Streaming en {missingCount} textura(s)",
                    "Activa Mip Streaming en todas las texturas que lo requieren"),
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
                    "Mip Streaming Habilitado",
                    $"Se habilito Mip Streaming en {fixedCount} textura(s).\n\n" +
                    "Las texturas han sido reimportadas.",
                    "OK");
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
            if (!_target.CanStepDown())
                return;

            EditorGUILayout.LabelField("Ahorro Potencial", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            long currentTotal = _target.TotalEstimatedVRAM;
            long afterStepDown = _target.GetTotalAfterStepDown();
            long savings = currentTotal - afterStepDown;
            float savingsPercent = _target.GetOverallSavingsPercentage();

            // Preview visual
            GUI.contentColor = SavingsColor;
            EditorGUILayout.LabelField(
                $"Si reduces un paso: {VRChatTextureWeightCalculator.FormatBytes(currentTotal)} -> {VRChatTextureWeightCalculator.FormatBytes(afterStepDown)}",
                EditorStyles.boldLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.LabelField(
                $"Ahorro: {VRChatTextureWeightCalculator.FormatBytes(savings)} (-{savingsPercent:F1}%)",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Global Actions

        private void DrawGlobalActions()
        {
            EditorGUILayout.BeginHorizontal();

            // Reducir todas un paso
            GUI.enabled = _target.CanStepDown();
            GUI.backgroundColor = _target.CanStepDown() ? SavingsColor : Color.white;
            if (GUILayout.Button(
                new GUIContent("Reducir Todas (1 paso)", "Reduce la resolucion de todas las texturas habilitadas un paso"),
                GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirmar Reduccion",
                    $"Esto reducira la resolucion de todas las texturas habilitadas un paso.\n\n" +
                    $"Ahorro estimado: {VRChatTextureWeightCalculator.FormatBytes(_target.GetPotentialSavings())}\n\n" +
                    "Este cambio modifica los archivos de textura. Puedes deshacer con Ctrl+Z.",
                    "Reducir", "Cancelar"))
                {
                    StepDownAllTextures();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            // Limpiar escaneo
            if (GUILayout.Button(
                new GUIContent("Limpiar", "Limpiar resultados del escaneo"),
                GUILayout.Height(25),
                GUILayout.Width(80)))
            {
                Undo.RecordObject(_target, "Limpiar Escaneo");
                _target.ClearScanResults();
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Group List

        private void DrawGroupList()
        {
            EditorGUILayout.LabelField("Grupos de Texturas", EditorStyles.boldLabel);

            if (_target.GroupCount == 0)
            {
                EditorGUILayout.HelpBox("No se encontraron texturas.", MessageType.Info);
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

            // Boton step-down individual
            GUI.enabled = group.IsEnabled && group.CanStepDown();
            if (GUILayout.Button(new GUIContent("↓", "Reducir resolucion un paso"), GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirmar Reduccion",
                    $"Reducir las texturas del grupo '{group.SourceName}'?\n\n" +
                    $"Ahorro estimado: {VRChatTextureWeightCalculator.FormatBytes(group.GetPotentialSavings())}",
                    "Reducir", "Cancelar"))
                {
                    StepDownGroup(group);
                }
            }
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
                TextureGroupType.AvatarBase => "Avatar Base",
                TextureGroupType.Clothing => "Ropa",
                TextureGroupType.AlternativeMaterials => "Materiales Alternativos",
                _ => "Desconocido"
            };
            EditorGUILayout.LabelField($"Tipo: {typeLabel}", EditorStyles.miniLabel);

            if (group.SourceObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fuente:", EditorStyles.miniLabel, GUILayout.Width(50));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(group.SourceObject, typeof(GameObject), true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(3);

            // Header de tabla
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent("T", "Tipo: N=Normal Map, A=Material Alternativo"),
                EditorStyles.miniBoldLabel, GUILayout.Width(20));
            EditorGUILayout.LabelField("Textura", EditorStyles.miniBoldLabel, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField("Res.", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Peso", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Alpha", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Mip", EditorStyles.miniBoldLabel, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            // Lista de texturas
            foreach (var texture in group.GetTexturesByWeight())
            {
                DrawTextureRow(texture);
            }
        }

        private void DrawTextureRow(TextureEntry texture)
        {
            EditorGUILayout.BeginHorizontal();

            // Indicador de tipo de textura
            string typeIndicator = "";
            string typeTooltip = "";
            Color typeColor = Color.white;

            if (texture.IsFromAlternativeMaterial)
            {
                typeIndicator = "A";
                typeTooltip = "Textura de material alternativo";
                typeColor = new Color(0.6f, 0.6f, 0.6f);
            }
            else if (texture.IsNormalMap)
            {
                typeIndicator = "N";
                typeTooltip = "Normal Map (BC5 - 1.0 byte/pixel)";
                typeColor = new Color(0.5f, 0.7f, 1f);
            }

            if (!string.IsNullOrEmpty(typeIndicator))
            {
                GUI.contentColor = typeColor;
                EditorGUILayout.LabelField(
                    new GUIContent(typeIndicator, typeTooltip),
                    EditorStyles.miniBoldLabel, GUILayout.Width(20));
                GUI.contentColor = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(20));
            }

            // Nombre (clickeable)
            bool isHeavy = VRChatTextureWeightCalculator.IsHighWeight(texture.EstimatedVRAMBytes);
            GUI.contentColor = texture.IsFromAlternativeMaterial
                ? new Color(0.6f, 0.6f, 0.6f)
                : (isHeavy ? WarningColor : Color.white);

            if (texture.Texture != null)
            {
                if (GUILayout.Button(texture.TextureName, EditorStyles.linkLabel, GUILayout.MinWidth(100)))
                {
                    Selection.activeObject = texture.Texture;
                    EditorGUIUtility.PingObject(texture.Texture);
                }
            }
            else
            {
                EditorGUILayout.LabelField(texture.TextureName, GUILayout.MinWidth(100));
            }

            // Resolucion
            EditorGUILayout.LabelField(texture.ResolutionLabel, EditorStyles.miniLabel, GUILayout.Width(70));

            // Peso
            EditorGUILayout.LabelField(
                VRChatTextureWeightCalculator.FormatBytesCompact(texture.EstimatedVRAMBytes),
                EditorStyles.miniLabel, GUILayout.Width(60));

            // Alpha
            EditorGUILayout.LabelField(texture.HasAlpha ? "Si" : "No", EditorStyles.miniLabel, GUILayout.Width(40));

            // Mip Streaming indicator
            bool needsMipStreaming = texture.HasMipmaps && !texture.HasMipStreaming;
            if (needsMipStreaming)
            {
                GUI.contentColor = ErrorColor;
                EditorGUILayout.LabelField(
                    new GUIContent("!", "Mip Streaming desactivado - requerido por VRChat"),
                    EditorStyles.miniBoldLabel, GUILayout.Width(30));
            }
            else
            {
                GUI.contentColor = SuccessColor;
                EditorGUILayout.LabelField("OK", EditorStyles.miniLabel, GUILayout.Width(30));
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

            _target.MarkAsScanned();
            EditorUtility.SetDirty(_target);

            Debug.Log($"[MRPesoTexturas] Escaneo completado: {_target.TotalTextureCount} texturas, {_target.TotalSizeLabel}");
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
    }
}
