using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.CoserRopa;
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
        private SerializedProperty _detectedClothingsProp;
        private SerializedProperty _selectedIndexProp;
        private ReorderableList _clothingList;
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

        private void OnEnable()
        {
            _target = (MRCoserRopa)target;
            _detectedClothingsProp = serializedObject.FindProperty("_detectedClothings");
            _selectedIndexProp = serializedObject.FindProperty("_selectedClothingIndex");

            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            _clothingList = new ReorderableList(serializedObject, _detectedClothingsProp, false, true, true, true)
            {
                drawHeaderCallback = DrawListHeader,
                drawElementCallback = DrawListElement,
                elementHeight = ITEM_HEIGHT + 4f,
                onAddCallback = OnAddClothing,
                onRemoveCallback = OnRemoveClothing,
                onSelectCallback = OnSelectClothing,
                drawElementBackgroundCallback = DrawElementBackground
            };
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

                // Lista de ropas (ReorderableList)
                DrawClothingList();

                // Detalles de la ropa seleccionada
                DrawSelectedClothingDetails();

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
                var label = _target.IsAvatarHumanoid ? "Humanoid" : "Generic (fallback por nombre)";

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

        private void DrawClothingList()
        {
            // Header con boton refrescar
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    MRLocalization.Get(L.CoserRopa.DETECTED_CLOTHINGS, _target.DetectedClothingCount),
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
            _clothingList.index = _target.SelectedClothingIndex;

            // Dibujar lista
            _clothingList.DoLayoutList();

            // Botones de seleccion rapida
            if (_target.DetectedClothingCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(MRLocalization.Get(L.CoserRopa.SELECT_ALL), EditorStyles.miniButtonLeft))
                    {
                        Undo.RecordObject(_target, "Seleccionar todas");
                        _target.EnableAllClothings();
                        EditorUtility.SetDirty(_target);
                    }
                    if (GUILayout.Button(MRLocalization.Get(L.CoserRopa.DESELECT_ALL), EditorStyles.miniButtonRight))
                    {
                        Undo.RecordObject(_target, "Deseleccionar todas");
                        _target.DisableAllClothings();
                        EditorUtility.SetDirty(_target);
                    }
                }
            }
        }

        private void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, $"Ropas ({_target.EnabledClothingCount}/{_target.DetectedClothingCount} habilitadas)");
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _target.DetectedClothingCount) return;

            if (isActive || isFocused)
            {
                EditorGUI.DrawRect(rect, SelectedBgColor);
            }
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index >= _target.DetectedClothingCount) return;

            var clothing = _target.DetectedClothings[index];
            if (clothing == null) return;

            rect.y += 2f;
            rect.height = ITEM_HEIGHT;

            // Layout: [Toggle] "Ropa N" [ObjectField] [Huesos] [X]
            const float LABEL_WIDTH = 55f;
            const float OBJECT_FIELD_MIN = 80f;

            float x = rect.x;

            // Toggle habilitado
            var toggleRect = new Rect(x, rect.y, TOGGLE_WIDTH, rect.height);
            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUI.Toggle(toggleRect, clothing.Enabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Toggle ropa");
                _target.SetClothingEnabled(index, enabled);
                EditorUtility.SetDirty(_target);
            }
            x += TOGGLE_WIDTH + 2f;

            // Color segun estado
            Color originalColor = GUI.contentColor;
            bool isSelected = (index == _target.SelectedClothingIndex);

            if (isSelected)
            {
                GUI.contentColor = EnabledColor;
            }
            else
            {
                GUI.contentColor = clothing.Enabled ? Color.white : DisabledColor;
            }

            // Label "Ropa N"
            var labelRect = new Rect(x, rect.y, LABEL_WIDTH, rect.height);
            EditorGUI.LabelField(labelRect, $"Ropa {index + 1}");
            x += LABEL_WIDTH;

            GUI.contentColor = originalColor;

            // ObjectField (permite click para seleccionar en jerarquia y drag para cambiar)
            float objectFieldWidth = rect.width - TOGGLE_WIDTH - LABEL_WIDTH - BONES_INFO_WIDTH - DELETE_BUTTON_WIDTH - 20f;
            objectFieldWidth = Mathf.Max(objectFieldWidth, OBJECT_FIELD_MIN);
            var objectFieldRect = new Rect(x, rect.y, objectFieldWidth, rect.height);

            EditorGUI.BeginChangeCheck();
            var newGameObject = (GameObject)EditorGUI.ObjectField(
                objectFieldRect, clothing.GameObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newGameObject != clothing.GameObject)
            {
                Undo.RecordObject(_target, "Cambiar ropa");
                // Actualizar el GameObject de la ropa
                if (newGameObject != null)
                {
                    clothing.GameObject = newGameObject;
                    clothing.ArmatureReference = new ArmatureReference(newGameObject);
                    _target.DetectBoneMappingsForClothing(clothing);
                }
                EditorUtility.SetDirty(_target);
            }
            x += objectFieldWidth + 5f;

            // Info de huesos
            var bonesRect = new Rect(x, rect.y, BONES_INFO_WIDTH, rect.height);
            GUI.contentColor = clothing.Enabled
                ? (clothing.HasValidMappings ? EnabledColor : WarningColor)
                : DisabledColor;
            EditorGUI.LabelField(bonesRect, $"{clothing.MappedBoneCount} huesos", EditorStyles.miniLabel);
            GUI.contentColor = originalColor;

            // Boton eliminar (rojo)
            var deleteRect = new Rect(rect.x + rect.width - DELETE_BUTTON_WIDTH - 2f, rect.y + 1f, DELETE_BUTTON_WIDTH, rect.height - 2f);
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUI.Button(deleteRect, "X", EditorStyles.miniButton))
            {
                Undo.RecordObject(_target, "Quitar ropa");
                _target.RemoveClothing(index);
                EditorUtility.SetDirty(_target);
            }
            GUI.color = Color.white;
        }

        private void OnAddClothing(ReorderableList list)
        {
            // Agregar entrada vacia (el usuario arrastrara el objeto al ObjectField)
            Undo.RecordObject(_target, "Agregar ropa");

            var newEntry = new ClothingEntry();
            _target.DetectedClothings.Add(newEntry);

            // Seleccionar la nueva entrada
            _target.SelectedClothingIndex = _target.DetectedClothingCount - 1;

            EditorUtility.SetDirty(_target);
        }

        private void OnRemoveClothing(ReorderableList list)
        {
            if (list.index >= 0 && list.index < _target.DetectedClothingCount)
            {
                Undo.RecordObject(_target, "Quitar ropa");
                _target.RemoveClothing(list.index);
                EditorUtility.SetDirty(_target);
            }
        }

        private void OnSelectClothing(ReorderableList list)
        {
            _selectedIndexProp.intValue = list.index;
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Selected Clothing Details

        private Vector2 _mappingsScrollPos;

        private void DrawSelectedClothingDetails()
        {
            var selected = _target.SelectedClothing;
            if (selected == null) return;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Titulo
            EditorGUILayout.LabelField($"Detalles: {selected.Name}", EditorStyles.boldLabel);

            // Info
            EditorGUILayout.LabelField($"Huesos: {selected.MappedBoneCount} mapeados de {selected.TotalBoneCount}", EditorStyles.miniLabel);

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
        private void DrawBoneNamingInfo(ClothingEntry clothing)
        {
            EditorGUILayout.Space(3);

            // Detectar automáticamente para mostrar como placeholder/sugerencia
            var detectedPrefix = DetectCommonPrefix(clothing);
            var detectedSuffix = DetectCommonSuffix(clothing);

            // Foldout para la sección
            bool showSection = clothing.HasCustomNaming ||
                               !string.IsNullOrEmpty(detectedPrefix) ||
                               !string.IsNullOrEmpty(detectedSuffix);

            string foldoutLabel = clothing.HasCustomNaming
                ? "Prefijo/Sufijo de huesos (configurado)"
                : (showSection ? "Prefijo/Sufijo de huesos (detectado)" : "Prefijo/Sufijo de huesos");

            _showNamingFoldout = EditorGUILayout.Foldout(_showNamingFoldout, foldoutLabel, true);

            if (!_showNamingFoldout)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Campo Prefijo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefijo:", GUILayout.Width(50));

            EditorGUI.BeginChangeCheck();
            string newPrefix = EditorGUILayout.TextField(clothing.BonePrefix);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar prefijo");
                clothing.BonePrefix = newPrefix;
                EditorUtility.SetDirty(_target);
            }

            // Botón para usar el detectado
            if (!string.IsNullOrEmpty(detectedPrefix) && detectedPrefix != clothing.BonePrefix)
            {
                if (GUILayout.Button($"Usar: {detectedPrefix}", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Undo.RecordObject(_target, "Usar prefijo detectado");
                    clothing.BonePrefix = detectedPrefix;
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Campo Sufijo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sufijo:", GUILayout.Width(50));

            EditorGUI.BeginChangeCheck();
            string newSuffix = EditorGUILayout.TextField(clothing.BoneSuffix);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Cambiar sufijo");
                clothing.BoneSuffix = newSuffix;
                EditorUtility.SetDirty(_target);
            }

            // Botón para usar el detectado
            if (!string.IsNullOrEmpty(detectedSuffix) && detectedSuffix != clothing.BoneSuffix)
            {
                if (GUILayout.Button($"Usar: {detectedSuffix}", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Undo.RecordObject(_target, "Usar sufijo detectado");
                    clothing.BoneSuffix = detectedSuffix;
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Botón para re-detectar con los nuevos valores
            EditorGUILayout.Space(3);
            if (GUILayout.Button("Re-detectar huesos con estos valores", EditorStyles.miniButton))
            {
                Undo.RecordObject(_target, "Re-detectar huesos");
                _target.DetectBoneMappingsForClothing(clothing);
                EditorUtility.SetDirty(_target);
            }

            // Mensaje informativo
            if (clothing.HasCustomNaming)
            {
                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField("Se eliminará el prefijo/sufijo para mejor matching.", EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = Color.white;
            }
            else
            {
                GUI.contentColor = new Color(0.6f, 0.6f, 0.6f);
                EditorGUILayout.LabelField("Opcional: Si los huesos tienen prefijo/sufijo, escríbelo aquí.", EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Detecta prefijo común en los nombres de huesos de la ropa
        /// </summary>
        private string DetectCommonPrefix(ClothingEntry clothing)
        {
            var clothingBoneNames = clothing.BoneMappings
                .Where(m => m.ClothingBone != null)
                .Select(m => m.ClothingBone.name)
                .ToList();

            if (clothingBoneNames.Count < 3)
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
                int matchCount = clothingBoneNames.Count(name => name.StartsWith(prefix));
                // Si más del 50% de los huesos tienen este prefijo, lo consideramos común
                if (matchCount > clothingBoneNames.Count / 2)
                {
                    return prefix;
                }
            }

            // Intentar detectar prefijo personalizado
            // Buscar el prefijo más largo común entre los primeros huesos
            if (clothingBoneNames.Count >= 2)
            {
                string first = clothingBoneNames[0];
                string commonPrefix = "";

                for (int i = 1; i <= first.Length && i <= 20; i++)
                {
                    string candidate = first.Substring(0, i);
                    // Verificar si termina en separador común
                    if (!candidate.EndsWith("_") && !candidate.EndsWith("-") && !candidate.EndsWith(".") && !candidate.EndsWith(":"))
                        continue;

                    int matchCount = clothingBoneNames.Count(name => name.StartsWith(candidate));
                    if (matchCount > clothingBoneNames.Count / 2)
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
        private string DetectCommonSuffix(ClothingEntry clothing)
        {
            var clothingBoneNames = clothing.BoneMappings
                .Where(m => m.ClothingBone != null)
                .Select(m => m.ClothingBone.name)
                .ToList();

            if (clothingBoneNames.Count < 3)
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
                int matchCount = clothingBoneNames.Count(name => name.EndsWith(suffix));
                if (matchCount > clothingBoneNames.Count / 2)
                {
                    return suffix;
                }
            }

            return null;
        }

        private void DrawBoneMappingsEditor(ClothingEntry clothing)
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hueso Avatar (destino)", EditorStyles.miniLabel, GUILayout.Width(140));
            EditorGUILayout.LabelField("←", GUILayout.Width(20));
            EditorGUILayout.LabelField("Hueso Ropa (clic para ver, arrastra para cambiar)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("", GUILayout.Width(50)); // Espacio para indicador
            EditorGUILayout.EndHorizontal();

            // Lista con scroll
            _mappingsScrollPos = EditorGUILayout.BeginScrollView(_mappingsScrollPos,
                GUILayout.MaxHeight(200));

            for (int i = 0; i < clothing.BoneMappings.Count; i++)
            {
                var mapping = clothing.BoneMappings[i];
                DrawSingleMappingEditor(mapping);
            }

            EditorGUILayout.EndScrollView();

            // Boton para agregar mapeo manual (selecciona hueso del avatar)
            EditorGUILayout.Space(3);
            if (GUILayout.Button("+ Agregar mapeo desde hueso del avatar", EditorStyles.miniButton))
            {
                AddManualMappingFromAvatar(clothing);
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
                mapping.ClothingBone,
                typeof(Transform),
                true);

            if (EditorGUI.EndChangeCheck() && newBone != mapping.ClothingBone)
            {
                Undo.RecordObject(_target, "Cambiar mapeo de hueso");
                mapping.ClothingBone = newBone;
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

        private void AddManualMappingFromAvatar(ClothingEntry clothing)
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
                bool alreadyMapped = clothing.BoneMappings.Any(m => m.AvatarBone == avatarBone);
                if (!alreadyMapped)
                {
                    var capturedBone = avatarBone;
                    menu.AddItem(new GUIContent(avatarBone.name), false, () =>
                    {
                        Undo.RecordObject(_target, "Agregar mapeo manual");
                        var mapping = new BoneMapping
                        {
                            AvatarBone = capturedBone,
                            ClothingBone = null, // Usuario arrastrara el hueso de la ropa
                            MappingMethod = BoneMappingMethod.ManualAssignment
                        };
                        clothing.BoneMappings.Add(mapping);
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

        #endregion

        #region NDMF Info

        private void DrawNDMFInfo()
        {
            // Resumen de estado
            int enabledCount = _target.EnabledClothingCount;
            int totalBones = _target.TotalMappedBones;

            if (enabledCount > 0 && totalBones > 0)
            {
                // Estado listo
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUI.contentColor = EnabledColor;
                EditorGUILayout.LabelField($"[OK] {enabledCount} ropa(s) lista(s) - {totalBones} huesos", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(
                    "El merge se ejecutará automáticamente al:",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(
                    "  • Entrar en Play Mode\n  • Subir el avatar a VRChat",
                    EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
            }
            else if (enabledCount > 0)
            {
                // Ropas habilitadas pero sin mapeos
                EditorGUILayout.HelpBox(
                    "Las ropas habilitadas no tienen mapeos de huesos válidos.\n" +
                    "Verifica que las ropas tengan armature con huesos humanoid.",
                    MessageType.Warning);
            }
            else
            {
                // Sin ropas habilitadas
                EditorGUILayout.HelpBox(
                    "Habilita al menos una ropa para que se procese automáticamente.",
                    MessageType.Info);
            }
        }

        #endregion
    }
}
