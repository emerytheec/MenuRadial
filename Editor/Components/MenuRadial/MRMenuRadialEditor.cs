using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.MenuRadial;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.AjustarBounds;

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
        private MRMenuRadial _target;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _statusLabelStyle;
        private bool _stylesInitialized;

        // Cache tipado para MRMenuControl
        private MRMenuControl _menuControlTyped;

        private void OnEnable()
        {
            _target = (MRMenuRadial)target;
            _avatarRootProperty = serializedObject.FindProperty("_avatarRoot");
            _autoDetectProperty = serializedObject.FindProperty("_autoDetectOnAvatarAssign");
            _autoGenerateMenuProperty = serializedObject.FindProperty("_autoGenerateMenuStructure");
            _outputPathProperty = serializedObject.FindProperty("_outputPath");
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

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawAvatarField();
            EditorGUILayout.Space(10);

            DrawOutputPathField();
            EditorGUILayout.Space(10);

            DrawStatusPanel();
            EditorGUILayout.Space(10);

            DrawActionButtons();
            EditorGUILayout.Space(10);

            DrawChildComponentsPanel();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("MR Menu Radial", _headerStyle);
            EditorGUILayout.LabelField("Contenedor principal del sistema Menu Radial", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawAvatarField()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Avatar", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_avatarRootProperty, new GUIContent("Avatar Root", "Arrastra aquí tu avatar con VRC_AvatarDescriptor"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _target.PropagateAvatarToChildren();
                RefreshMenuControlCache();
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.PropertyField(_autoDetectProperty, new GUIContent("Auto-detectar", "Detectar ropas y escanear automáticamente al asignar avatar"));

            EditorGUILayout.PropertyField(_autoGenerateMenuProperty, new GUIContent("Auto-generar Menú", "Generar automáticamente la estructura de menú basada en las ropas detectadas"));

            // Mostrar estado del avatar
            if (_avatarRootProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Arrastra tu avatar aquí para comenzar.", MessageType.Info);
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
                        EditorGUILayout.HelpBox($"Avatar: {avatarGO.name}", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("El GameObject no tiene VRC_AvatarDescriptor.", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputPathField()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Ruta de Salida", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_outputPathProperty, new GUIContent("Output Path", "Ruta donde se guardarán animaciones y archivos VRChat"));

            EditorGUILayout.HelpBox("Esta ruta se usa para generar animaciones y archivos VRChat.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Estado", EditorStyles.boldLabel);

            // Ropas
            DrawStatusLine(
                "Ropas detectadas",
                _target.DetectedClothingCount > 0,
                $"{_target.EnabledClothingCount}/{_target.DetectedClothingCount}",
                _target.DetectedClothingCount == 0 ? "Sin ropas" : null
            );

            // PhysBones
            string pbStatus = GetPhysBonesStatusText();
            bool pbOk = _target.IsPhysBonesOrganized || _target.DetectedPhysBonesCount == 0;
            DrawStatusLine(
                "PhysBones",
                pbOk,
                pbStatus,
                null
            );

            // Bounds
            DrawStatusLine(
                "Bounds",
                _target.IsBoundsApplied,
                _target.IsBoundsApplied ? $"Aplicados ({_target.DetectedMeshesCount} meshes)" : "Pendiente",
                _target.DetectedMeshesCount == 0 ? "Sin meshes" : null
            );

            // Menú
            DrawStatusLine(
                "Menú configurado",
                _target.MenuSlotCount > 0,
                $"{_target.MenuSlotCount} slots",
                _target.MenuSlotCount == 0 ? "Sin slots" : null
            );

            EditorGUILayout.EndVertical();
        }

        private string GetPhysBonesStatusText()
        {
            switch (_target.PhysBonesState)
            {
                case OrganizationState.NotScanned:
                    return "No escaneado";
                case OrganizationState.Scanned:
                    return $"Detectados ({_target.DetectedPhysBonesCount})";
                case OrganizationState.Organized:
                    return $"Organizados ({_target.DetectedPhysBonesCount})";
                default:
                    return "Desconocido";
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
            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);

            bool hasAvatar = _avatarRootProperty.objectReferenceValue != null;

            EditorGUI.BeginDisabledGroup(!hasAvatar);

            // Botón Preparar Todo
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Preparar Todo", "Detectar ropas, organizar PhysBones y aplicar bounds"), GUILayout.Height(30)))
            {
                Undo.RecordObject(_target, "Prepare All");
                _target.PrepareAll();
                EditorUtility.SetDirty(_target);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Botones de generación de estructura
            EditorGUILayout.BeginHorizontal();

            // Botón Generar Estructura (solo si no existe)
            bool hasExistingStructure = _target.MenuSlotCount > 0;

            EditorGUI.BeginDisabledGroup(hasExistingStructure || _target.DetectedClothingCount == 0);
            if (GUILayout.Button(new GUIContent("Generar Estructura", "Crea MRUnificarObjetos y MRAgruparObjetos para cada ropa"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Generate Menu Structure");
                var result = _target.GenerateMenuStructure();
                if (result.Success)
                {
                    EditorUtility.DisplayDialog("Estructura Generada",
                        $"Se crearon {result.ClothingFramesCreated} frames de ropa.\n" +
                        $"Avatar: {result.AvatarMeshesIncluded} meshes incluidos, {result.AvatarMeshesExcluded} excluidos.",
                        "OK");
                }
                RefreshMenuControlCache();
                EditorUtility.SetDirty(_target);
            }
            EditorGUI.EndDisabledGroup();

            // Botón Regenerar (solo si existe estructura)
            EditorGUI.BeginDisabledGroup(!hasExistingStructure || _target.DetectedClothingCount == 0);
            if (GUILayout.Button(new GUIContent("Regenerar", "Elimina y regenera la estructura"), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Regenerar Estructura",
                    "Esto eliminará la estructura existente y la recreará.\n¿Continuar?",
                    "Sí", "Cancelar"))
                {
                    Undo.RecordObject(_target, "Regenerate Menu Structure");
                    var result = _target.RegenerateMenuStructure();
                    if (result.Success)
                    {
                        EditorUtility.DisplayDialog("Estructura Regenerada",
                            $"Se crearon {result.ClothingFramesCreated} frames de ropa.\n" +
                            $"Avatar: {result.AvatarMeshesIncluded} meshes incluidos, {result.AvatarMeshesExcluded} excluidos.",
                            "OK");
                    }
                    RefreshMenuControlCache();
                    EditorUtility.SetDirty(_target);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_target.DetectedClothingCount == 0 && hasAvatar)
            {
                EditorGUILayout.HelpBox("Primero detecta ropas con 'Preparar Todo' o asigna un avatar.", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Botón Generar Archivos VRChat
            EditorGUILayout.BeginHorizontal();
            bool canGenerate = hasAvatar && _target.MenuSlotCount > 0;
            EditorGUI.BeginDisabledGroup(!canGenerate);
            if (GUILayout.Button(new GUIContent("Generar Archivos VRChat", "Genera FX Controller, Parameters y Menu"), GUILayout.Height(30)))
            {
                _target.GenerateVRChatFiles();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (!canGenerate && hasAvatar)
            {
                EditorGUILayout.HelpBox("Configura al menos un slot en el menú antes de generar.", MessageType.Info);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawChildComponentsPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Componentes Hijos", EditorStyles.boldLabel);

            DrawComponentStatus("MR Coser Ropa", _target.CoserRopa != null);
            DrawComponentStatus("MR Organiza PB", _target.OrganizaPB != null);
            DrawComponentStatus("MR Menu Control", _menuControlTyped != null);
            DrawComponentStatus("MR Ajustar Bounds", _target.AjustarBounds != null);

            // Verificar si faltan componentes
            bool missingComponents = _target.CoserRopa == null ||
                                     _target.OrganizaPB == null ||
                                     _menuControlTyped == null ||
                                     _target.AjustarBounds == null;

            if (missingComponents)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Recrear Componentes Faltantes"))
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

            if (_menuControlTyped == null)
            {
                var go = new GameObject("Menu Control");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MRMenuControl>();
                Undo.RegisterCreatedObjectUndo(go, "Create Menu Control");
            }

            if (_target.AjustarBounds == null)
            {
                var go = new GameObject("Ajustar Bounds");
                go.transform.SetParent(_target.transform);
                go.AddComponent<MRAjustarBounds>();
                Undo.RegisterCreatedObjectUndo(go, "Create Ajustar Bounds");
            }

            _target.InvalidateCache();
            RefreshMenuControlCache();

            // Propagar avatar a nuevos hijos
            _target.PropagateAvatarToChildren();
        }
    }
}
