using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.Menu.Editor;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

/// <summary>
/// Editor personalizado para MRMenuControl
/// Versión: 1.1 - FIX: Preview persiste al cambiar a componentes no-MR
/// </summary>
[CustomEditor(typeof(MRMenuControl))]
public class MRMenuControlInspector : Editor
{
    private MRMenuControl _target;

    // Para manejo correcto de selección
    private static bool _isSelectionChangeHandlerRegistered = false;
    private static MRMenuControl _lastActiveControlMenu = null;

    /// <summary>
    /// Inicialización del editor
    /// </summary>
    private void OnEnable()
    {
        _target = (MRMenuControl)target;

        // Registrar el handler de cambio de selección (una sola vez)
        if (!_isSelectionChangeHandlerRegistered)
        {
            Selection.selectionChanged += OnSelectionChanged;
            _isSelectionChangeHandlerRegistered = true;
        }

        // Guardar referencia al objeto activo actual
        _lastActiveControlMenu = _target;
    }

    /// <summary>
    /// Limpieza al desactivar el editor.
    /// La lógica de cancelación condicional se maneja en OnSelectionChanged.
    /// </summary>
    private void OnDisable()
    {
        // No hacemos nada aquí porque la lógica de cancelación
        // se maneja en OnSelectionChanged que tiene el timing correcto
    }

    /// <summary>
    /// Handler para cambios de selección en el Editor.
    /// Se ejecuta DESPUÉS de que la selección cambia, permitiendo verificar
    /// correctamente si el nuevo objeto tiene componentes MR conflictivos.
    /// </summary>
    private static void OnSelectionChanged()
    {
        // Si no hay un ControlMenu anterior, no hay nada que hacer
        if (_lastActiveControlMenu == null)
        {
            // Actualizar referencia al nuevo objeto si hay uno seleccionado
            UpdateLastActiveControlMenu();
            return;
        }

        // Verificar si el nuevo objeto seleccionado tiene componentes MR que podrían solaparse
        var selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            // Solo cancelar si se va a otro componente del sistema MR
            bool hasConflictingComponent =
                selectedObject.GetComponent<MRAgruparObjetos>() != null ||
                selectedObject.GetComponent<MRMenuControl>() != null ||
                selectedObject.GetComponent<MRUnificarObjetos>() != null;

            if (hasConflictingComponent)
            {
                _lastActiveControlMenu.ResetAllPreviews();
            }
            // Si no tiene componentes conflictivos, mantener los previews activos
        }
        else
        {
            // Si no hay nada seleccionado, cancelar los previews
            _lastActiveControlMenu.ResetAllPreviews();
        }

        // Actualizar referencia al nuevo objeto
        UpdateLastActiveControlMenu();
    }

    /// <summary>
    /// Actualiza la referencia al último MRMenuControl activo
    /// </summary>
    private static void UpdateLastActiveControlMenu()
    {
        var selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            var controlMenu = selectedObject.GetComponent<MRMenuControl>();
            if (controlMenu != null)
            {
                _lastActiveControlMenu = controlMenu;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        var target = (MRMenuControl)this.target;
        
        // Preview del menú radial - SIEMPRE VISIBLE
        GUILayout.Label(MRLocalization.Get(L.Menu.PREVIEW_TITLE), EditorStyles.boldLabel);
        
        // Crear arrays de nombres e iconos de slots (puede estar vacío para mostrar estado inicial)
        string[] slotNames = new string[Mathf.Max(target.SlotCount, 0)];
        Texture2D[] menuIcons = new Texture2D[Mathf.Max(target.SlotCount, 0)];  // Iconos del menú (BSX_GM_*)
        Texture2D[] logoImages = new Texture2D[Mathf.Max(target.SlotCount, 0)];  // Imágenes Logo personalizadas
        
        // Verificar que AnimationSlots no sea null y tenga elementos
        if (target.AnimationSlots != null)
        {
            int slotCount = Mathf.Min(target.AnimationSlots.Count, slotNames.Length);
            
            for (int i = 0; i < slotCount; i++)
            {
                var slot = target.AnimationSlots[i];
                if (slot != null)
                {
                    slotNames[i] = string.IsNullOrEmpty(slot.slotName) ? $"Slot {i+1}" : slot.slotName;
                    
                    // Separar iconos: menú (primer plano) y logo (fondo)
                    var (menuIcon, logoImage) = MRIconLoader.GetIconsForSlot(slot);
                    menuIcons[i] = menuIcon;   // Icono del menú basado en tipo de animación
                    logoImages[i] = logoImage; // Imagen Logo personalizada del usuario
                }
                else
                {
                    slotNames[i] = $"Slot {i+1}";
                }
            }
        }
        
        // Dibujar menú radial INTERACTIVO (ahora con iconos separados en capas y navegación + deslizadores radiales integrados)
        Rect menuRect = GUILayoutUtility.GetRect(300, 300); // REDUCIDO: de 400x400 a 300x300 (-25%)
        
        // Preparar datos para slots lineales (CORREGIDO: Ahora detecta CUALQUIER componente Linear)
        IAnimationProvider[] linearSlots = new IAnimationProvider[Mathf.Max(target.SlotCount, 0)];
        string[] slotKeys = new string[Mathf.Max(target.SlotCount, 0)];
        
        if (target.AnimationSlots != null)
        {
            int slotCount = Mathf.Min(target.AnimationSlots.Count, linearSlots.Length);
            
            for (int i = 0; i < slotCount; i++)
            {
                var slot = target.AnimationSlots[i];
                if (slot != null && slot.isValid && slot.targetObject != null)
                {
                    // CORREGIDO: Buscar cualquier IAnimationProvider de tipo Linear
                    var animationProvider = slot.GetAnimationProvider();
                    if (animationProvider != null && animationProvider.AnimationType == AnimationType.Linear)
                    {
                        // Verificar que sea un tipo que podemos manejar
                        if (animationProvider is MRUnificarObjetos radialMenu && radialMenu.FrameCount >= 3)
                        {
                            linearSlots[i] = animationProvider;
                            slotKeys[i] = $"{target.GetInstanceID()}_{i}_{slot.slotName}";
                        }
                        // Soporte para MRIluminacionRadial
                        else if (animationProvider is Bender_Dios.MenuRadial.Components.Illumination.MRIluminacionRadial)
                        {
                            linearSlots[i] = animationProvider;
                            slotKeys[i] = $"{target.GetInstanceID()}_{i}_{slot.slotName}";
                        }
                        // Soporte para MRUnificarMateriales
                        else if (animationProvider is MRUnificarMateriales unifyMaterial && unifyMaterial.CanGenerateAnimation)
                        {
                            linearSlots[i] = animationProvider;
                            slotKeys[i] = $"{target.GetInstanceID()}_{i}_{slot.slotName}";
                        }
                    }
                }
            }
        }
        
        // Usar el nuevo método con soporte para deslizadores radiales
        SimpleRadialMenuDrawer.DrawRadialMenuWithSliders(menuRect, slotNames, menuIcons, logoImages, (buttonIndex) => {
            target.HandleMenuButtonClick(buttonIndex);

            // Forzar repintado después de cualquier interacción
            Repaint();
        }, linearSlots, slotKeys);

        // Forzar repintado continuo para respuesta fluida en interacciones
        // Esto asegura que los clics y sliders respondan inmediatamente
        if (Event.current.type == EventType.MouseDown ||
            Event.current.type == EventType.MouseUp ||
            Event.current.type == EventType.MouseDrag ||
            RadialSliderIntegration.GetActiveSliderKey() != null)
        {
            Repaint();
        }

        // Botón de Reset debajo del menú
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(MRLocalization.Get(L.Menu.RESET_PREVIEWS), GUILayout.Width(120), GUILayout.Height(22)))
        {
            target.ResetAllPreviews();
            Repaint();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Mostrar información de navegación si tiene menú padre
        if (target.HasParent)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            // Botón para volver al menú padre
            if (GUILayout.Button(MRLocalization.Get(L.Menu.BACK_TO, target.ParentMenu.name), GUILayout.Height(25)))
            {
                target.NavigateToParent();
            }

            // Botón para ir al menú raíz
            var rootMenu = target.GetRootMenu();
            if (rootMenu != target && rootMenu != target.ParentMenu)
            {
                if (GUILayout.Button(MRLocalization.Get(L.Menu.ROOT_BUTTON), GUILayout.Width(60), GUILayout.Height(25)))
                {
                    target.NavigateToRoot();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Mostrar ruta de navegación
            EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.PATH_LABEL, target.NavigationPath), MessageType.Info);
        }
        
        GUILayout.Space(10);
        
        // Propiedades personalizadas (excluyendo las innecesarias)
        serializedObject.Update();
        
        // ÁREA DE DROP PARA AÑADIR SLOTS (antes de Animation Slots)
        DrawDropArea(target);
        GUILayout.Space(5);
        
        // Animation Slots (con su propio header)
        var animationSlotsProp = serializedObject.FindProperty("animationSlots");
        if (animationSlotsProp != null)
        {
            EditorGUILayout.PropertyField(animationSlotsProp, true);
        }
        
        // Botones para añadir componentes debajo de los slots
        DrawAddComponentButtons(target);
        GUILayout.Space(10);
        
        // Solo mostrar configuración VRChat en menús raíz (sin padre)
        if (!target.HasParent)
        {
            // Sección de Namespace del Avatar
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(MRLocalization.Get(L.Menu.NAMESPACE_SECTION), EditorStyles.boldLabel);

            var vrchatConfigProp = serializedObject.FindProperty("vrchatConfig");
            if (vrchatConfigProp != null)
            {
                // Campo de Output Prefix
                var outputPrefixProp = vrchatConfigProp.FindPropertyRelative("_outputPrefix");
                if (outputPrefixProp != null)
                {
                    EditorGUILayout.PropertyField(outputPrefixProp, MRLocalization.GetContent(L.Menu.OUTPUT_PREFIX, L.Menu.OUTPUT_PREFIX_TOOLTIP));

                    // Preview de la ruta de salida
                    string outputDir = target.VRChatConfig?.GetOutputDirectory() ?? "Assets/Bender_Dios/Generated/";
                    EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.OUTPUT_PATH, outputDir), MessageType.None);
                }

                EditorGUILayout.Space(5);

                // Write Default Values
                var writeDefaultValuesProp = vrchatConfigProp.FindPropertyRelative("writeDefaultValues");
                if (writeDefaultValuesProp != null)
                {
                    EditorGUILayout.PropertyField(writeDefaultValuesProp, new GUIContent(MRLocalization.Get(L.Menu.WRITE_DEFAULT_VALUES)));
                }
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);

            // Verificar y mostrar conflictos de nombres
            var slotManager = target.GetSlotManager();
            bool hasConflicts = slotManager != null && slotManager.HasConflicts();

            if (hasConflicts)
            {
                EditorGUILayout.HelpBox(
                    MRLocalization.Get(L.Menu.NAME_CONFLICTS),
                    MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(MRLocalization.Get(L.Menu.VIEW_CONFLICTS), GUILayout.Width(100)))
                {
                    string summary = slotManager.GetConflictsSummary();
                    EditorUtility.DisplayDialog(MRLocalization.Get(L.Menu.NAME_CONFLICTS), summary, MRLocalization.Get(L.Common.OK));
                }
                if (GUILayout.Button(MRLocalization.Get(L.Menu.AUTO_RESOLVE), GUILayout.Width(100)))
                {
                    if (slotManager.AutoResolveConflicts())
                    {
                        EditorUtility.SetDirty(target);
                        Repaint();
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
            }

            GUILayout.Space(10);

            // Botón de creación de archivos
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.fontStyle = FontStyle.Bold;

            bool canCreate = target.AllSlotsValid && !hasConflicts;
            GUI.enabled = canCreate;
            if (GUILayout.Button(MRLocalization.Get(L.Menu.CREATE_VRCHAT_FILES), buttonStyle, GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(MRLocalization.Get(L.Menu.CREATE_VRCHAT_FILES),
                    MRLocalization.Get(L.Menu.CREATE_FILES_CONFIRM), MRLocalization.Get(L.Common.CREATE), MRLocalization.Get(L.Common.CANCEL)))
                {
                    target.CreateVRChatFiles();
                }
            }
            GUI.enabled = true;

            // Mensajes de ayuda
            if (!target.AllSlotsValid)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.SLOTS_NOT_CONFIGURED), MessageType.Warning);
            }
            else if (hasConflicts)
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.NAME_CONFLICTS_RESOLVE), MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.READY_TO_CREATE), MessageType.Info);
            }
        }
        else
        {
            // Mensaje informativo para submenús
            serializedObject.ApplyModifiedProperties();
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                MRLocalization.Get(L.Menu.IS_SUBMENU, target.GetRootMenu()?.name ?? "Menu Principal"),
                MessageType.Info);
        }
        
        // Re-validar si hay cambios
        if (GUI.changed)
        {
            target.ValidateAllSlots();
        }
    }
    
    /// <summary>
    /// NUEVO: Maneja el clic en slots de tipo Linear para abrir la ventana circular
    /// </summary>
    private void HandleLinearSlotClick(MRMenuControl target, int buttonIndex)
    {
        // Ignorar botón Back (-1)
        if (buttonIndex < 0 || buttonIndex >= target.AnimationSlots.Count)
            return;
            
        var slot = target.AnimationSlots[buttonIndex];
        if (!slot.isValid || slot.targetObject == null)
            return;
            
        // Verificar si es un MRUnificarObjetos
        var radialMenu = slot.targetObject.GetComponent<MRUnificarObjetos>();
        if (radialMenu == null)
            return;
            
        // Verificar si es de tipo Linear
        if (radialMenu.AnimationType != AnimationType.Linear)
            return;
            
        // Abrir ventana circular
        CircularLinearMenuWindow.OpenCircularMenu(radialMenu, target, slot.slotName);
    }
    
    /// <summary>
    /// Dibuja el área de drop para añadir nuevos slots arrastrando GameObjects
    /// </summary>
    private void DrawDropArea(MRMenuControl target)
    {
        // Área de drop visual
        GUIStyle dropAreaStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            normal = { textColor = Color.gray }
        };
        
        Rect dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        
        // Cambiar color si hay objetos siendo arrastrados
        Color originalColor = GUI.color;
        Event evt = Event.current;
        
        if (dropRect.Contains(evt.mousePosition) && 
            (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
        {
            GUI.color = Color.cyan; // Resaltar cuando hay drag
        }
        
        GUI.Box(dropRect, MRLocalization.Get(L.Menu.DROP_COMPONENTS), dropAreaStyle);
        GUI.color = originalColor;
        
        // Manejar drag & drop
        HandleDragAndDrop(dropRect, target, evt);
    }
    
    /// <summary>
    /// Maneja el drag & drop de GameObjects para crear slots
    /// </summary>
    private void HandleDragAndDrop(Rect dropRect, MRMenuControl target, Event evt)
    {
        switch (evt.type)
        {
            case EventType.DragUpdated:
                if (dropRect.Contains(evt.mousePosition))
                {
                    // Verificar si los objetos arrastrados son GameObjects válidos
                    bool validObjects = true;
                    bool hasValidComponent = false;

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (!(obj is GameObject go))
                        {
                            validObjects = false;
                            break;
                        }

                        // Verificar que tenga un componente válido
                        if (HasValidSlotComponent(go))
                        {
                            hasValidComponent = true;
                        }
                    }

                    // Solo aceptar si es GameObject Y tiene componente válido
                    DragAndDrop.visualMode = (validObjects && hasValidComponent)
                        ? DragAndDropVisualMode.Copy
                        : DragAndDropVisualMode.Rejected;
                }
                break;

            case EventType.DragPerform:
                if (dropRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.AcceptDrag();

                    int addedCount = 0;
                    int skippedCount = 0;

                    // Añadir un slot por cada GameObject arrastrado
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject gameObject)
                        {
                            // Verificar que tenga un componente válido
                            if (!HasValidSlotComponent(gameObject))
                            {
                                skippedCount++;
                                continue;
                            }

                            // Verificar que no exceda el máximo
                            if (target.SlotCount >= MRMenuControl.MAX_SLOTS)
                            {
                                EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.WARNING),
                                    MRLocalization.Get(L.Menu.MAX_SLOTS_REACHED, MRMenuControl.MAX_SLOTS), MRLocalization.Get(L.Common.OK));
                                break;
                            }

                            // Añadir nuevo slot y asignar el GameObject
                            target.AddSlot();

                            // Asignar el GameObject al último slot creado
                            if (target.AnimationSlots.Count > 0)
                            {
                                var lastSlot = target.AnimationSlots[target.AnimationSlots.Count - 1];
                                lastSlot.targetObject = gameObject;

                                // Auto-asignar nombre si está vacío
                                if (string.IsNullOrEmpty(lastSlot.slotName))
                                {
                                    lastSlot.slotName = gameObject.name;
                                }

                                // Validar el slot
                                lastSlot.ValidateSlot();
                                addedCount++;
                            }
                        }
                    }

                    // Mostrar mensaje si se saltaron objetos
                    if (skippedCount > 0)
                    {
                        EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.WARNING),
                            MRLocalization.Get(L.Menu.INVALID_OBJECTS, skippedCount), MRLocalization.Get(L.Common.OK));
                    }

                    // Actualizar la UI
                    serializedObject.Update();
                    EditorUtility.SetDirty(target);
                    evt.Use();
                }
                break;
        }
    }

    /// <summary>
    /// Verifica si el GameObject tiene un componente válido para ser slot
    /// </summary>
    private bool HasValidSlotComponent(GameObject go)
    {
        if (go == null) return false;

        // MRUnificarObjetos
        if (go.GetComponent<MRUnificarObjetos>() != null)
            return true;

        // MRIluminacionRadial
        if (go.GetComponent<MRIluminacionRadial>() != null)
            return true;

        // MRUnificarMateriales
        if (go.GetComponent<MRUnificarMateriales>() != null)
            return true;

        // MRMenuControl (submenú)
        if (go.GetComponent<MRMenuControl>() != null)
            return true;

        return false;
    }
    
    /// <summary>
    /// Dibuja los 4 botones para añadir componentes en horizontal
    /// </summary>
    private void DrawAddComponentButtons(MRMenuControl target)
    {
        bool canAdd = target.CanCreateSubMenu();

        // Estilo del botón
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30,
            wordWrap = true
        };

        Color originalColor = GUI.backgroundColor;
        GUI.enabled = canAdd;

        EditorGUILayout.BeginHorizontal();

        // Botón 1: Unificar Objetos (Verde)
        GUI.backgroundColor = canAdd ? new Color(0.4f, 0.9f, 0.4f, 1f) : Color.gray;
        if (GUILayout.Button(MRLocalization.Get(L.Menu.CREATE_UNIFICAR_OBJETOS), buttonStyle))
        {
            CreateComponentWithConfirmation(target, MRLocalization.Get(L.Radial.HEADER),
                MRLocalization.Get(L.Menu.CREATE_UNIFICAR_OBJETOS_DESC),
                () => target.CreateRadialMenu());
        }

        // Botón 2: Iluminación (Amarillo)
        GUI.backgroundColor = canAdd ? new Color(1f, 0.9f, 0.4f, 1f) : Color.gray;
        if (GUILayout.Button(MRLocalization.Get(L.Menu.CREATE_ILUMINACION), buttonStyle))
        {
            CreateComponentWithConfirmation(target, MRLocalization.Get(L.Illumination.HEADER),
                MRLocalization.Get(L.Menu.CREATE_ILUMINACION_DESC),
                () => target.CreateIllumination());
        }

        // Botón 3: Unificar Materiales (Morado)
        GUI.backgroundColor = canAdd ? new Color(0.8f, 0.6f, 1f, 1f) : Color.gray;
        if (GUILayout.Button(MRLocalization.Get(L.Menu.CREATE_UNIFICAR_MATERIALES), buttonStyle))
        {
            CreateComponentWithConfirmation(target, MRLocalization.Get(L.UnifyMaterial.HEADER),
                MRLocalization.Get(L.Menu.CREATE_UNIFICAR_MATERIALES_DESC),
                () => target.CreateUnifyMaterial());
        }

        // Botón 4: Sub-Menú (Azul)
        GUI.backgroundColor = canAdd ? new Color(0.4f, 0.8f, 1f, 1f) : Color.gray;
        if (GUILayout.Button(MRLocalization.Get(L.Menu.CREATE_SUBMENU), buttonStyle))
        {
            CreateComponentWithConfirmation(target, MRLocalization.Get(L.Menu.SUBMENU_TITLE),
                MRLocalization.Get(L.Menu.CREATE_SUBMENU_DESC),
                () => target.CreateSubMenu());
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;
        GUI.backgroundColor = originalColor;

        // Mensaje de ayuda
        if (!canAdd)
        {
            EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.MAX_SLOTS_REACHED, MRMenuControl.MAX_SLOTS), MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(MRLocalization.Get(L.Menu.CREATE_CHILD_HELP), MessageType.Info);
        }
    }

    /// <summary>
    /// Muestra diálogo de confirmación y crea el componente
    /// </summary>
    private void CreateComponentWithConfirmation(MRMenuControl target, string componentName, string description, System.Action createAction)
    {
        if (EditorUtility.DisplayDialog(MRLocalization.Get(L.Common.CREATE) + " " + componentName,
            description + "\n\n" + MRLocalization.Get(L.Menu.CONTINUE_QUESTION),
            MRLocalization.Get(L.Common.CREATE), MRLocalization.Get(L.Common.CANCEL)))
        {
            createAction();

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }
}
