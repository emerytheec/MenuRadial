using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.MenuRadial;
using Bender_Dios.MenuRadial.Components.MenuRadial.Models;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.OrganizaPB.Models;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using VRC.SDK3.Avatars.ScriptableObjects;

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

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawAvatarField();
            EditorGUILayout.Space(10);

            DrawOutputPathField();
            EditorGUILayout.Space(10);

            DrawMenuIntegrationSection();
            EditorGUILayout.Space(10);

            DrawNDMFControlPanel();
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
                // Refrescar serializedObject para mostrar el OutputPrefix auto-asignado
                serializedObject.Update();
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

            EditorGUILayout.Space(10);

            // Sección de Namespace del Avatar (Configuración VRChat)
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Namespace del Avatar", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_outputPrefixProperty, new GUIContent("Output Prefix", "Prefijo único para este avatar. Crea subcarpeta y prefija nombres de archivo. Dejar vacío para comportamiento legacy."));

            // Preview de la ruta de salida completa
            string outputDir = _target.GetVRChatOutputDirectory();
            EditorGUILayout.HelpBox($"Ruta de salida: {outputDir}", MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_writeDefaultValuesProperty, new GUIContent("Write Default Values", "writeDefaultValues para las capas del controlador FX"));

            EditorGUILayout.EndVertical();
        }

        private void DrawMenuIntegrationSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Integración del Menú VRChat", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Configura cómo y dónde se integrará el menú MR en el avatar.", MessageType.Info);

            EditorGUILayout.Space(5);

            // Nombre del menú
            EditorGUILayout.PropertyField(_menuNameProperty, new GUIContent(
                "Nombre",
                "Nombre que aparecerá en el menú VRChat. Si está vacío, usa el prefijo o 'Menu Radial'."));

            // Preview del nombre efectivo
            string effectiveName = _target.EffectiveMenuName;
            EditorGUILayout.LabelField("Nombre efectivo:", effectiveName, EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Icono del menú
            EditorGUILayout.PropertyField(_menuIconProperty, new GUIContent(
                "Icono",
                "Icono que aparecerá junto al nombre en el menú VRChat."));

            EditorGUILayout.Space(10);

            // Modo de integración
            EditorGUILayout.PropertyField(_menuIntegrationModeProperty, new GUIContent(
                "Ubicación",
                "Define dónde se ubicará el menú MR dentro del menú del avatar."));

            EditorGUILayout.Space(5);

            // Mostrar opciones según el modo seleccionado
            var integrationMode = (MenuIntegrationMode)_menuIntegrationModeProperty.enumValueIndex;

            switch (integrationMode)
            {
                case MenuIntegrationMode.RootMenu:
                    EditorGUILayout.HelpBox("El menú se añadirá directamente al menú raíz del avatar.", MessageType.None);
                    break;

                case MenuIntegrationMode.ExistingSubMenu:
                    DrawExistingSubMenuSelector();
                    break;

                case MenuIntegrationMode.CustomPath:
                    EditorGUILayout.PropertyField(_customMenuPathProperty, new GUIContent(
                        "Ruta",
                        "Ruta de carpetas separadas por '/' (ej: 'Outfits/Casual'). Si no existen, se crearán."));

                    if (!string.IsNullOrEmpty(_customMenuPathProperty.stringValue))
                    {
                        EditorGUILayout.HelpBox($"Se creará la ruta: {_customMenuPathProperty.stringValue}/{effectiveName}", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Ingresa una ruta para crear menús anidados.", MessageType.Warning);
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
                EditorGUILayout.HelpBox("Primero asigna un avatar para ver los submenús disponibles.", MessageType.Warning);
                return;
            }

            // Buscar VRCAvatarDescriptor
            var descriptor = avatarGO.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (descriptor == null)
            {
                EditorGUILayout.HelpBox("El avatar no tiene VRCAvatarDescriptor.", MessageType.Warning);
                return;
            }

            var expressionsMenu = descriptor.expressionsMenu;
            if (expressionsMenu == null || expressionsMenu.controls == null || expressionsMenu.controls.Count == 0)
            {
                EditorGUILayout.HelpBox("El avatar no tiene un menú de expresiones configurado o está vacío.", MessageType.Warning);
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
                EditorGUILayout.HelpBox("El menú del avatar no tiene submenús. Usa 'Menú Raíz' o 'Ruta personalizada'.", MessageType.Warning);
                return;
            }

            // Encontrar el índice actual en la lista
            int currentIndex = subMenuIndices.IndexOf(_targetSubMenuIndexProperty.intValue);
            if (currentIndex < 0) currentIndex = 0;

            // Dropdown para seleccionar
            int newIndex = EditorGUILayout.Popup(
                new GUIContent("Submenú destino", "Selecciona un submenú existente donde añadir el menú MR."),
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
                EditorGUILayout.HelpBox($"Submenú '{selectedControl.name}' tiene {controlCount}/8 controles.", MessageType.None);
            }
        }

        private void DrawNDMFControlPanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("NDMF - Control de Procesos", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Controla qué procesos se ejecutan automáticamente durante Play Mode o al subir el avatar.", MessageType.Info);

            EditorGUILayout.Space(5);

            // Checkbox para desactivar cosido de huesos
            EditorGUILayout.PropertyField(_disableBoneStitchingProperty, new GUIContent(
                "Desactivar Cosido de Huesos",
                "Si está activado, NDMF NO cosera automáticamente los armatures de ropa al avatar durante el build."));

            // Checkbox para desactivar merge de VRChat
            EditorGUILayout.PropertyField(_disableVRChatMergeProperty, new GUIContent(
                "Desactivar Merge VRChat",
                "Si está activado, NDMF NO mezclará automáticamente los archivos VRChat (FX, Parameters, Menu) durante el build."));

            EditorGUILayout.Space(10);

            // Separador visual
            EditorGUILayout.LabelField("Integración con Otros Plugins", EditorStyles.boldLabel);

            // Checkbox para desactivar Modular Avatar
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_disableModularAvatarProperty, new GUIContent(
                "Desactivar Modular Avatar",
                "Si está activado, TODOS los componentes de Modular Avatar serán desactivados durante el build. " +
                "Útil cuando hay conflictos entre MR Menu Radial y Modular Avatar. " +
                "Solo afecta al build (clon del avatar), NO modifica la escena original."));

            if (EditorGUI.EndChangeCheck() && _disableModularAvatarProperty.boolValue)
            {
                // Mostrar advertencia al activar
                EditorUtility.DisplayDialog("Advertencia",
                    "Al activar esta opción, Modular Avatar NO procesará este avatar durante el build.\n\n" +
                    "Esto significa que:\n" +
                    "• Los componentes de MA (MergeArmature, MenuInstaller, etc.) serán ignorados\n" +
                    "• Las ropas con MA deberán ser procesadas por MR Coser Ropa\n\n" +
                    "Esta opción es segura y solo afecta al build, no modifica tu escena.",
                    "Entendido");
            }

            // Mostrar advertencia si alguno está activado
            bool anyDisabled = _disableBoneStitchingProperty.boolValue ||
                              _disableVRChatMergeProperty.boolValue ||
                              _disableModularAvatarProperty.boolValue;

            if (anyDisabled)
            {
                EditorGUILayout.Space(5);
                string warning = "Procesos desactivados durante el build:\n";
                if (_disableBoneStitchingProperty.boolValue)
                    warning += "• Cosido de huesos (MR)\n";
                if (_disableVRChatMergeProperty.boolValue)
                    warning += "• Merge de archivos VRChat (MR)\n";
                if (_disableModularAvatarProperty.boolValue)
                    warning += "• Modular Avatar (TODOS sus componentes)\n";
                warning += "\nEl avatar en Play Mode/Upload NO tendrá estos cambios aplicados.";

                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

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
