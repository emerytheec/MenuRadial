using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Models;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Controllers;
using Bender_Dios.MenuRadial.Editor.Components.AnalisisColision.Controllers;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;

namespace Bender_Dios.MenuRadial.Editor.Components.AnalisisColision
{
    /// <summary>
    /// Editor personalizado para MRAnalisisColision.
    /// Muestra componentes de MA detectados agrupados por prenda de ropa.
    /// </summary>
    [CustomEditor(typeof(MRAnalisisColision))]
    public class MRAnalisisColisionEditor : UnityEditor.Editor
    {
        private MRAnalisisColision _target;
        private MRCoserRopa _coserRopa;

        // Foldouts por GameObject (usando path como key)
        private Dictionary<string, bool> _gameObjectFoldouts = new Dictionary<string, bool>();

        // Colores
        private static readonly Color ProblematicColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color UserDecisionColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color CompatibleColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color MABlueColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color CriticalRedColor = new Color(1f, 0.2f, 0.2f);
        private static readonly Color MeshOnRootColor = new Color(1f, 0.5f, 0f); // Naranja para meshes en raíz

        // Componentes criticos que siempre se muestran en rojo y se desactivan automaticamente
        private static readonly string[] CRITICAL_COMPONENTS = new[]
        {
            "ModularAvatarMeshCutter",
            "ModularAvatarVertexFilter",
            "ModularAvatarShapeChanger",
            "VertexFilterByAxisComponent",  // Nombre real del filtro de vertices
            "ModularAvatarBlendshapeSync"   // Sincroniza blendshapes en Edit Mode
        };

        // Color para BlendshapeSync (naranja especial)
        private static readonly Color BlendshapeSyncColor = new Color(1f, 0.6f, 0.2f);

        // Color para botón de Menú
        private static readonly Color MenuActiveColor = new Color(0.3f, 0.7f, 0.9f);
        private static readonly Color MenuDisabledColor = new Color(0.6f, 0.6f, 0.6f);

        // Componentes de menú (para contar)
        private static readonly string[] MENU_COMPONENTS = new[]
        {
            "Animator",
            "ModularAvatarMergeAnimator",
            "ModularAvatarParameters",
            "ModularAvatarMenuInstaller",
            "ModularAvatarMenuGroup",
            "ModularAvatarMenuItem"
        };

        // SerializedProperties para las listas
        private SerializedProperty _scanResultProp;
        private SerializedProperty _problematicEntriesProp;
        private SerializedProperty _userDecisionEntriesProp;
        private SerializedProperty _compatibleEntriesProp;

        private void OnEnable()
        {
            _target = (MRAnalisisColision)target;
            FindCoserRopa();
            CacheSerializedProperties();
        }

        private void CacheSerializedProperties()
        {
            _scanResultProp = serializedObject.FindProperty("_scanResult");
            if (_scanResultProp != null)
            {
                _problematicEntriesProp = _scanResultProp.FindPropertyRelative("_problematicEntries");
                _userDecisionEntriesProp = _scanResultProp.FindPropertyRelative("_userDecisionEntries");
                _compatibleEntriesProp = _scanResultProp.FindPropertyRelative("_compatibleEntries");
            }
        }

        /// <summary>
        /// Busca MRCoserRopa en la jerarquía para obtener información de ropas.
        /// </summary>
        private void FindCoserRopa()
        {
            _coserRopa = null;

            if (_target == null) return;

            // Buscar en el mismo GameObject o padre
            _coserRopa = _target.GetComponentInParent<MRCoserRopa>();

            // Si no encontró, buscar en hermanos (mismo padre)
            if (_coserRopa == null && _target.transform.parent != null)
            {
                _coserRopa = _target.transform.parent.GetComponentInChildren<MRCoserRopa>();
            }

            // Si aún no encontró, buscar en el avatar
            if (_coserRopa == null && _target.AvatarRoot != null)
            {
                _coserRopa = _target.AvatarRoot.GetComponentInChildren<MRCoserRopa>();
            }
        }

        /// <summary>
        /// Obtiene la lista de ropas detectadas desde MRCoserRopa.
        /// </summary>
        private List<ClothingEntry> GetDetectedClothings()
        {
            if (_coserRopa == null)
                FindCoserRopa();

            return _coserRopa?.DetectedClothings ?? new List<ClothingEntry>();
        }

        /// <summary>
        /// Sincroniza la lista de raíces de ropa en MRAnalisisColision desde MRCoserRopa.
        /// Esto es necesario para que la lógica de clasificación funcione correctamente.
        /// Si hay cambios en la lista de ropas, re-escanea automáticamente.
        /// </summary>
        private void SyncClothingRoots()
        {
            var detectedClothings = GetDetectedClothings();
            var currentRoots = _target.ClothingRoots;

            // Crear lista de GameObjects desde las ropas detectadas
            var newRoots = detectedClothings
                .Where(c => c?.GameObject != null)
                .Select(c => c.GameObject)
                .ToList();

            // Solo actualizar si hay cambios
            bool needsUpdate = currentRoots.Count != newRoots.Count ||
                               !currentRoots.SequenceEqual(newRoots);

            if (needsUpdate)
            {
                Undo.RecordObject(_target, "Sync Clothing Roots");
                _target.UpdateClothingRoots(newRoots);

                // Re-escanear componentes de MA cuando cambia la lista de ropas
                if (_target.AvatarRoot != null && _target.IsMAAvailable)
                {
                    _target.ScanAvatar();
                }

                EditorUtility.SetDirty(_target);
            }
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            // Sincronizar lista de ropas desde MRCoserRopa
            SyncClothingRoots();

            // Header
            DrawHeader();
            EditorGUILayout.Space(5);

            // Avatar
            DrawAvatarSection();

            // Solo mostrar el resto si hay avatar
            if (_target.AvatarRoot != null)
            {
                EditorGUILayout.Space(8);

                // Resumen
                DrawSummarySection();

                EditorGUILayout.Space(8);

                // Meshes en raíces de ropa (mostrar siempre si hay)
                if (_target.IsScanned && _target.HasMeshOnRoot)
                {
                    DrawMeshOnRootSection();
                    EditorGUILayout.Space(8);
                }

                // Secciones de componentes agrupados por GameObject
                if (_target.IsScanned && _target.HasAnyColision)
                {
                    DrawComponentsByGameObject();
                    EditorGUILayout.Space(8);
                }

                // Seccion especial para BlendshapeSync (si hay alguno)
                if (_target.BlendshapeSyncCount > 0)
                {
                    DrawBlendshapeSyncSection();
                    EditorGUILayout.Space(8);
                }

                // Info NDMF
                DrawNDMFInfo();
            }
            else
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "Arrastra tu avatar aqui para escanear componentes de Modular Avatar.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Header & Avatar

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MR Analisis Colision", EditorStyleManager.HeaderStyle);
            EditorGUILayout.LabelField(
                "Detecta componentes de MA que pueden interferir con MR",
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

            // Estado de MA
            if (_target.AvatarRoot != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth + 2);

                if (_target.IsMAAvailable)
                {
                    GUI.contentColor = MABlueColor;
                    EditorGUILayout.LabelField("Modular Avatar detectado", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.contentColor = Color.gray;
                    EditorGUILayout.LabelField("Modular Avatar no instalado", EditorStyles.miniLabel);
                }
                GUI.contentColor = Color.white;

                GUILayout.EndHorizontal();

                // Checkbox para desactivar previews de MA (ShapeChanger, MeshDeleter)
                if (NDMFPreviewController.IsAvailable)
                {
                    bool previewsEnabled = NDMFPreviewController.IsShapeChangerEnabled || NDMFPreviewController.IsMeshDeleterEnabled;

                    EditorGUI.BeginChangeCheck();
                    bool newValue = EditorGUILayout.Toggle(
                        new GUIContent("MA Preview Edit Mode",
                            "Activa/desactiva la previsualizacion de ShapeChanger y MeshDeleter de MA en Edit Mode"),
                        previewsEnabled);

                    if (EditorGUI.EndChangeCheck() && newValue != previewsEnabled)
                    {
                        if (newValue)
                            NDMFPreviewController.EnableAllBodyAffectingPreviews();
                        else
                            NDMFPreviewController.DisableAllBodyAffectingPreviews();
                    }
                }
            }
        }

        #endregion

        #region Summary Section

        private void DrawSummarySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!_target.IsMAAvailable)
            {
                EditorGUILayout.LabelField("Modular Avatar no esta instalado", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("No hay componentes de MA que analizar.", EditorStyles.miniLabel);
            }
            else if (!_target.IsScanned)
            {
                EditorGUILayout.LabelField("Sin escanear", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("El escaneo se realiza automaticamente al detectar ropas.", EditorStyles.miniLabel);
            }
            else if (!_target.HasAnyColision)
            {
                GUI.contentColor = CompatibleColor;
                EditorGUILayout.LabelField("Sin colisiones detectadas", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.LabelField("No hay componentes de MA que puedan interferir.", EditorStyles.miniLabel);
            }
            else
            {
                // Resumen con iconos (siempre mostrar las 3 categorias)
                EditorGUILayout.BeginHorizontal();

                // Problematicos
                GUI.contentColor = _target.HasProblematic ? ProblematicColor : Color.gray;
                GUILayout.Label(EditorGUIUtility.IconContent("d_redLight"), GUILayout.Width(20), GUILayout.Height(18));
                EditorGUILayout.LabelField($"{_target.ProblematicCount}", GUILayout.Width(30));
                GUI.contentColor = Color.white;

                // Decision de usuario
                GUI.contentColor = _target.HasUserDecision ? UserDecisionColor : Color.gray;
                GUILayout.Label(EditorGUIUtility.IconContent("d_orangeLight"), GUILayout.Width(20), GUILayout.Height(18));
                EditorGUILayout.LabelField($"{_target.UserDecisionCount}", GUILayout.Width(30));
                GUI.contentColor = Color.white;

                // Compatibles con checkbox para mostrar/ocultar
                GUI.contentColor = _target.CompatibleCount > 0 ? CompatibleColor : Color.gray;
                GUILayout.Label(EditorGUIUtility.IconContent("d_greenLight"), GUILayout.Width(20), GUILayout.Height(18));

                // Solo mostrar checkbox si hay compatibles
                if (_target.CompatibleCount > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    bool showCompatible = EditorGUILayout.Toggle(_target.ShowCompatibleComponents, GUILayout.Width(20));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_target, "Toggle Show Compatible");
                        _target.ShowCompatibleComponents = showCompatible;
                        EditorUtility.SetDirty(_target);
                    }
                }

                EditorGUILayout.LabelField($"{_target.CompatibleCount}", GUILayout.Width(30));
                GUI.contentColor = Color.white;

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Info adicional
                if (_target.ProblematicOnRootCount > 0)
                {
                    EditorGUILayout.Space(3);
                    GUI.contentColor = ProblematicColor;
                    EditorGUILayout.LabelField(
                        $"{_target.ProblematicOnRootCount} en raiz de ropa (se desactivaran)",
                        EditorStyles.miniLabel);
                    GUI.contentColor = Color.white;

                    // Nota sobre ubicación correcta de componentes
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox(
                        "Estos componentes (Vertex Filter, Mesh Cutter, Shape Changer) estan en la raiz de la prenda, " +
                        "pero deberian estar en el GameObject del mesh que modifican.\n\n" +
                        "Muevelos al mesh correcto para que funcionen sin conflictos con MR.",
                        MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Mesh On Root Section

        /// <summary>
        /// Dibuja la sección de meshes detectados en raíces de ropa.
        /// Estos son errores del autor de la ropa que pueden causar problemas.
        /// </summary>
        private void DrawMeshOnRootSection()
        {
            var meshEntries = _target.ScanResult.MeshOnRootEntries;
            if (meshEntries == null || meshEntries.Count == 0) return;

            string foldoutKey = "mesh_on_root_section";
            if (!_gameObjectFoldouts.ContainsKey(foldoutKey))
                _gameObjectFoldouts[foldoutKey] = true; // Abierto por defecto

            // Fondo con color naranja
            GUI.backgroundColor = new Color(MeshOnRootColor.r, MeshOnRootColor.g, MeshOnRootColor.b, 0.15f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header
            EditorGUILayout.BeginHorizontal();

            GUI.contentColor = MeshOnRootColor;
            _gameObjectFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                _gameObjectFoldouts[foldoutKey],
                $"⚠ Meshes en Raíz de Ropa ({meshEntries.Count})",
                true,
                EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_gameObjectFoldouts[foldoutKey])
            {
                EditorGUILayout.Space(3);

                // Explicación del problema
                var infoStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    richText = true
                };

                EditorGUILayout.LabelField(
                    "<b>Problema:</b> Se detectaron meshes (Renderers) directamente en el GameObject raíz de una ropa. " +
                    "Esto es un error del autor de la ropa - los meshes deberían estar en GameObjects hijos.",
                    infoStyle);

                EditorGUILayout.Space(2);

                EditorGUILayout.LabelField(
                    "<b>Consecuencia:</b> El sistema de auto-generación de menú puede confundir estos meshes " +
                    "con meshes del avatar base, causando que se apaguen incorrectamente.",
                    infoStyle);

                EditorGUILayout.Space(2);

                EditorGUILayout.LabelField(
                    "<b>Solución:</b> Mueve el componente Renderer a un GameObject hijo, o elimina el mesh si no es necesario.",
                    infoStyle);

                EditorGUILayout.Space(5);

                // Lista de meshes problemáticos
                foreach (var entry in meshEntries)
                {
                    if (!entry.IsValid) continue;

                    EditorGUILayout.BeginHorizontal();

                    // Icono de warning
                    GUILayout.Label(EditorGUIUtility.IconContent("d_console.warnicon.sml"), GUILayout.Width(20), GUILayout.Height(18));

                    // Información del mesh
                    GUI.contentColor = MeshOnRootColor;
                    EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.boldLabel);
                    GUI.contentColor = Color.white;

                    GUILayout.FlexibleSpace();

                    // Nombre de la ropa
                    EditorGUILayout.LabelField($"en '{entry.ClothingRootName}'", EditorStyles.miniLabel, GUILayout.Width(150));

                    // Botón para seleccionar en jerarquía
                    if (GUILayout.Button(
                        new GUIContent(EditorGUIUtility.IconContent("d_SceneViewFx").image, "Seleccionar en jerarquía"),
                        GUILayout.Width(25), GUILayout.Height(18)))
                    {
                        entry.SelectInHierarchy();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Components By Clothing

        /// <summary>
        /// Agrupa todas las entradas por prenda de ropa y las muestra.
        /// </summary>
        private void DrawComponentsByGameObject()
        {
            // Filtrar compatibles si el usuario no quiere verlos
            var allEntries = _target.ScanResult.AllEntries
                .Where(e => e.IsValid)
                .Where(e => e.Category != ColisionCategory.Compatible || _target.ShowCompatibleComponents)
                .ToList();

            var detectedClothings = GetDetectedClothings();

            // Agrupar entradas por prenda
            var entriesByClothing = new Dictionary<ClothingEntry, List<ColisionEntry>>();
            var entriesWithoutClothing = new List<ColisionEntry>();

            foreach (var entry in allEntries)
            {
                var clothing = FindClothingForGameObject(entry.Component.gameObject, detectedClothings);
                if (clothing != null)
                {
                    if (!entriesByClothing.ContainsKey(clothing))
                        entriesByClothing[clothing] = new List<ColisionEntry>();
                    entriesByClothing[clothing].Add(entry);
                }
                else
                {
                    entriesWithoutClothing.Add(entry);
                }
            }

            EditorGUILayout.LabelField(
                "Checkbox marcado = se mantiene activo. Desmarca para desactivar durante build.",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            // Dibujar cada prenda detectada
            foreach (var clothing in detectedClothings)
            {
                if (clothing == null || clothing.GameObject == null) continue;
                if (!entriesByClothing.ContainsKey(clothing)) continue;

                var entries = entriesByClothing[clothing];
                DrawClothingSection(clothing, entries);
            }

            // Dibujar componentes sin prenda asignada
            if (entriesWithoutClothing.Count > 0)
            {
                DrawOtherSection(entriesWithoutClothing);
            }
        }

        /// <summary>
        /// Encuentra la prenda de ropa a la que pertenece un GameObject.
        /// </summary>
        private ClothingEntry FindClothingForGameObject(GameObject obj, List<ClothingEntry> clothings)
        {
            if (obj == null || clothings == null) return null;

            foreach (var clothing in clothings)
            {
                if (clothing?.GameObject == null) continue;

                // Verificar si obj es el root o esta bajo el root
                if (obj == clothing.GameObject || obj.transform.IsChildOf(clothing.GameObject.transform))
                    return clothing;
            }
            return null;
        }

        /// <summary>
        /// Dibuja una seccion para una prenda de ropa.
        /// </summary>
        private void DrawClothingSection(ClothingEntry clothing, List<ColisionEntry> entries)
        {
            var clothingRoot = clothing.GameObject;
            string foldoutKey = $"clothing_{clothingRoot.name}";
            if (!_gameObjectFoldouts.ContainsKey(foldoutKey))
                _gameObjectFoldouts[foldoutKey] = false;

            // Determinar color basado en categoria mas severa
            var mostSevere = GetMostSevereCategory(entries);
            var sectionColor = GetCategoryColor(mostSevere);

            // Seccion principal de la prenda
            GUI.backgroundColor = new Color(sectionColor.r, sectionColor.g, sectionColor.b, 0.15f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header de la prenda
            EditorGUILayout.BeginHorizontal();

            GUI.contentColor = sectionColor;
            _gameObjectFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                _gameObjectFoldouts[foldoutKey],
                $"{clothing.Name}",
                true,
                EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            // Empujar elementos a la derecha
            GUILayout.FlexibleSpace();

            // Badge de cantidad (alineado a la derecha)
            GUILayout.Label($"({entries.Count})", EditorStyles.miniLabel);

            // Indicador si tiene Modular Avatar
            if (clothing.HasModularAvatar)
            {
                GUI.contentColor = MABlueColor;
                GUILayout.Label("[MA]", EditorStyles.miniBoldLabel);
                GUI.contentColor = Color.white;
            }

            // Botón "Menú" para desactivar/activar componentes de menú de esta prenda
            DrawMenuToggleButton(clothing.Name, entries);

            // Boton para mostrar la prenda en jerarquia
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_SceneViewFx"), GUILayout.Width(25), GUILayout.Height(18)))
            {
                EditorGUIUtility.PingObject(clothingRoot);
            }

            EditorGUILayout.EndHorizontal();

            // Contenido
            if (_gameObjectFoldouts[foldoutKey])
            {
                // Agrupar por GameObject dentro de la prenda
                var groupedByGameObject = entries
                    .GroupBy(e => e.Component.gameObject)
                    .OrderBy(g => g.Key == clothingRoot ? 0 : 1) // Raiz primero
                    .ThenBy(g => GetRelativePath(g.Key, clothingRoot))
                    .ToList();

                foreach (var group in groupedByGameObject)
                {
                    DrawGameObjectInClothing(group.Key, group.ToList(), clothingRoot);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        /// <summary>
        /// Dibuja un GameObject dentro de una seccion de prenda.
        /// </summary>
        private void DrawGameObjectInClothing(GameObject gameObject, List<ColisionEntry> entries, GameObject clothingRoot)
        {
            bool isRoot = gameObject == clothingRoot;

            // Si NO es raíz, mostrar el nombre del GameObject
            if (!isRoot)
            {
                string displayName = GetRelativePath(gameObject, clothingRoot);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);

                GUILayout.Label(EditorGUIUtility.IconContent("d_GameObject Icon"), GUILayout.Width(18), GUILayout.Height(16));

                // Nombre clickeable - solo ping, no seleccionar
                if (GUILayout.Button(displayName, EditorStyles.miniLabel))
                {
                    EditorGUIUtility.PingObject(gameObject);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Componentes
            EditorGUI.indentLevel += isRoot ? 1 : 2;
            foreach (var entry in entries)
            {
                DrawComponentEntry(entry);
            }
            EditorGUI.indentLevel -= isRoot ? 1 : 2;

            if (!isRoot)
            {
                EditorGUILayout.Space(2);
            }
        }

        /// <summary>
        /// Dibuja la seccion de componentes sin prenda asignada.
        /// </summary>
        private void DrawOtherSection(List<ColisionEntry> entries)
        {
            string foldoutKey = "others_no_clothing";
            if (!_gameObjectFoldouts.ContainsKey(foldoutKey))
                _gameObjectFoldouts[foldoutKey] = false;

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header
            EditorGUILayout.BeginHorizontal();

            GUI.contentColor = Color.gray;
            _gameObjectFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                _gameObjectFoldouts[foldoutKey],
                "Otros (sin prenda)",
                true,
                EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            // Empujar a la derecha
            GUILayout.FlexibleSpace();

            GUILayout.Label($"({entries.Count})", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // Contenido
            if (_gameObjectFoldouts[foldoutKey])
            {
                var groupedByGameObject = entries
                    .GroupBy(e => e.Component.gameObject)
                    .OrderBy(g => g.First().HierarchyPath)
                    .ToList();

                foreach (var group in groupedByGameObject)
                {
                    DrawGameObjectInOther(group.Key, group.ToList());
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        /// <summary>
        /// Dibuja un GameObject en la seccion "Otros".
        /// </summary>
        private void DrawGameObjectInOther(GameObject gameObject, List<ColisionEntry> entries)
        {
            var firstEntry = entries.First();
            string displayName = string.IsNullOrEmpty(firstEntry.HierarchyPath)
                ? gameObject.name
                : firstEntry.HierarchyPath;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUILayout.Label(EditorGUIUtility.IconContent("d_GameObject Icon"), GUILayout.Width(18), GUILayout.Height(16));

            if (GUILayout.Button(displayName, EditorStyles.miniLabel))
            {
                EditorGUIUtility.PingObject(gameObject);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Componentes
            EditorGUI.indentLevel += 2;
            foreach (var entry in entries)
            {
                DrawComponentEntry(entry);
            }
            EditorGUI.indentLevel -= 2;

            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// Obtiene la ruta relativa de un GameObject respecto a una raiz.
        /// </summary>
        private string GetRelativePath(GameObject obj, GameObject root)
        {
            if (obj == null || root == null || obj == root)
                return obj?.name ?? "";

            var path = new List<string>();
            var current = obj.transform;

            while (current != null && current.gameObject != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        /// <summary>
        /// Dibuja una entrada de componente individual.
        /// </summary>
        private void DrawComponentEntry(ColisionEntry entry)
        {
            // Guardar y resetear indentLevel para que el Toggle funcione correctamente
            int savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Verificar si es BlendshapeSync
            bool isBlendshapeSync = IsBlendshapeSyncComponent(entry.ComponentTypeName);
            bool isSyncStopped = isBlendshapeSync && _target.IsBlendshapeSyncStopped(entry);

            EditorGUILayout.BeginHorizontal();

            // Espacio para simular indentación visual
            GUILayout.Space(savedIndent * 15 + 20);

            // Checkbox para desactivar (Problematic y UserDecision)
            bool canToggle = entry.Category == ColisionCategory.UserDecision ||
                             entry.Category == ColisionCategory.Problematic;

            if (canToggle)
            {
                // Checkbox marcado = mantener activo, desmarcado = desactivar
                bool currentValue = !entry.UserWantsDisabled;
                bool newValue = GUILayout.Toggle(currentValue, GUIContent.none, GUILayout.Width(18));
                if (newValue != currentValue)
                {
                    SetEntryUserWantsDisabled(entry, !newValue);
                }
            }
            else
            {
                // Icono de estado (solo para Compatible)
                var icon = entry.IsEnabled
                    ? EditorGUIUtility.IconContent("d_greenLight")
                    : EditorGUIUtility.IconContent("d_redLight");
                GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
            }

            // Nombre del tipo de componente
            bool isCriticalOnRoot = IsCriticalComponent(entry.ComponentTypeName) && entry.IsOnClothingRoot;

            if (isBlendshapeSync)
            {
                // BlendshapeSync tiene color especial
                GUI.contentColor = isSyncStopped ? Color.gray : BlendshapeSyncColor;
                string syncStatus = isSyncStopped ? " [DETENIDO]" : "";
                GUILayout.Label(entry.ShortTypeName + syncStatus, EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
            }
            else if (isCriticalOnRoot)
            {
                GUI.contentColor = CriticalRedColor;
                GUILayout.Label(entry.ShortTypeName, EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
            }
            else
            {
                GUILayout.Label(entry.ShortTypeName);
            }

            // Empujar categoría a la derecha
            GUILayout.FlexibleSpace();

            // Botones individuales para BlendshapeSync
            if (isBlendshapeSync && _target.IsBlendshapeSyncControlAvailable)
            {
                if (isSyncStopped)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    if (GUILayout.Button(
                        new GUIContent("▶", "Restaurar sincronización"),
                        GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        Undo.RecordObject(_target, "Restaurar BlendshapeSync");
                        _target.RestoreBlendshapeSync(entry);
                        EditorUtility.SetDirty(_target);
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
                    if (GUILayout.Button(
                        new GUIContent("■", "Detener sincronización"),
                        GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        Undo.RecordObject(_target, "Detener BlendshapeSync");
                        _target.StopBlendshapeSync(entry);
                        EditorUtility.SetDirty(_target);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            // Indicador de categoria con color (alineado a la derecha)
            var categoryColor = GetCategoryColor(entry.Category);
            var categoryLabel = GetCategoryShortLabel(entry.Category);

            GUI.contentColor = categoryColor;
            GUILayout.Label(categoryLabel, EditorStyles.miniBoldLabel, GUILayout.Width(75));
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Restaurar indentLevel
            EditorGUI.indentLevel = savedIndent;
        }

        /// <summary>
        /// Modifica UserWantsDisabled de una entrada usando SerializedProperty.
        /// </summary>
        private void SetEntryUserWantsDisabled(ColisionEntry entry, bool value)
        {
            // Refrescar las propiedades serializadas
            serializedObject.Update();
            CacheSerializedProperties();

            if (_scanResultProp == null)
            {
                Debug.LogWarning("[MRAnalisisColision] No se pudo encontrar ScanResult para modificar");
                return;
            }

            // Determinar en qué lista buscar
            SerializedProperty listProp = entry.Category switch
            {
                ColisionCategory.Problematic => _problematicEntriesProp,
                ColisionCategory.UserDecision => _userDecisionEntriesProp,
                _ => null
            };

            if (listProp == null || !listProp.isArray)
            {
                Debug.LogWarning($"[MRAnalisisColision] Lista no encontrada para categoria {entry.Category}");
                return;
            }

            bool found = false;

            // Estrategia 1: Buscar por referencia al componente
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elementProp = listProp.GetArrayElementAtIndex(i);
                var componentProp = elementProp.FindPropertyRelative("_component");

                if (componentProp != null && componentProp.objectReferenceValue == entry.Component)
                {
                    found = ApplyUserWantsDisabled(elementProp, value);
                    if (found) break;
                }
            }

            // Estrategia 2: Si no se encontró por referencia, buscar por tipo y nombre
            if (!found && !string.IsNullOrEmpty(entry.ComponentTypeName))
            {
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var elementProp = listProp.GetArrayElementAtIndex(i);
                    var typeNameProp = elementProp.FindPropertyRelative("_componentTypeName");
                    var gameObjectNameProp = elementProp.FindPropertyRelative("_gameObjectName");
                    var hierarchyPathProp = elementProp.FindPropertyRelative("_hierarchyPath");

                    // Coincidir por tipo y algún identificador (nombre o path)
                    if (typeNameProp != null && typeNameProp.stringValue == entry.ComponentTypeName)
                    {
                        bool pathMatch = hierarchyPathProp != null && hierarchyPathProp.stringValue == entry.HierarchyPath;
                        bool nameMatch = gameObjectNameProp != null && gameObjectNameProp.stringValue == entry.GameObjectName;

                        if (pathMatch || nameMatch)
                        {
                            found = ApplyUserWantsDisabled(elementProp, value);
                            if (found) break;
                        }
                    }
                }
            }

            if (!found)
            {
                Debug.LogWarning($"[MRAnalisisColision] No se pudo encontrar la entrada para modificar: {entry.ShortTypeName} en {entry.GameObjectName}");
            }
        }

        /// <summary>
        /// Aplica el valor de UserWantsDisabled a una SerializedProperty.
        /// </summary>
        private bool ApplyUserWantsDisabled(SerializedProperty elementProp, bool value)
        {
            var userWantsDisabledProp = elementProp.FindPropertyRelative("_userWantsDisabled");
            if (userWantsDisabledProp != null)
            {
                userWantsDisabledProp.boolValue = value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtiene la categoria mas severa de una lista de entradas.
        /// </summary>
        private ColisionCategory GetMostSevereCategory(List<ColisionEntry> entries)
        {
            if (entries.Any(e => e.Category == ColisionCategory.Problematic))
                return ColisionCategory.Problematic;
            if (entries.Any(e => e.Category == ColisionCategory.UserDecision))
                return ColisionCategory.UserDecision;
            return ColisionCategory.Compatible;
        }

        /// <summary>
        /// Obtiene el color para una categoria.
        /// </summary>
        private Color GetCategoryColor(ColisionCategory category)
        {
            return category switch
            {
                ColisionCategory.Problematic => ProblematicColor,
                ColisionCategory.UserDecision => UserDecisionColor,
                ColisionCategory.Compatible => CompatibleColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Obtiene la etiqueta corta para una categoria.
        /// </summary>
        private string GetCategoryShortLabel(ColisionCategory category)
        {
            return category switch
            {
                ColisionCategory.Problematic => "Problematic",
                ColisionCategory.UserDecision => "Decision",
                ColisionCategory.Compatible => "Compatible",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Verifica si un componente es critico (siempre en rojo y auto-desactivado).
        /// </summary>
        private bool IsCriticalComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;

            foreach (var critical in CRITICAL_COMPONENTS)
            {
                if (typeName.Contains(critical) || critical.Contains(typeName))
                    return true;
            }

            // Tambien verificar por patrones de nombre
            return typeName.Contains("MeshCutter") ||
                   typeName.Contains("VertexFilter") ||
                   typeName.Contains("ShapeChanger");
        }

        #endregion

        #region Menu Toggle Button

        /// <summary>
        /// Dibuja el botón "Menú" para una prenda específica.
        /// </summary>
        /// <param name="clothingName">Nombre de la prenda.</param>
        /// <param name="entries">Entradas de componentes de la prenda.</param>
        private void DrawMenuToggleButton(string clothingName, List<ColisionEntry> entries)
        {
            // Contar componentes de menú en esta prenda
            int menuComponentCount = CountMenuComponents(entries);

            // Si no hay componentes de menú, no mostrar el botón
            if (menuComponentCount == 0)
                return;

            bool isMenuDisabled = _target.IsMenuDisabledForClothing(clothingName);

            // Estilo del botón según estado
            var buttonColor = isMenuDisabled ? MenuDisabledColor : MenuActiveColor;
            var buttonText = isMenuDisabled ? "Menú ✗" : "Menú ✓";
            var tooltip = isMenuDisabled
                ? $"Menú DESACTIVADO ({menuComponentCount} componentes). Clic para activar."
                : $"Menú ACTIVO ({menuComponentCount} componentes). Clic para desactivar.";

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(new GUIContent(buttonText, tooltip), EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(18)))
            {
                Undo.RecordObject(_target, isMenuDisabled ? "Activar Menú Prenda" : "Desactivar Menú Prenda");

                if (isMenuDisabled)
                {
                    int restored = _target.EnableMenuComponentsForClothing(clothingName);
                    if (restored > 0)
                    {
                        Debug.Log($"[MRAnalisisColision] Menú activado para '{clothingName}': {restored} componente(s) restaurado(s)");
                    }
                }
                else
                {
                    int disabled = _target.DisableMenuComponentsForClothing(clothingName);
                    if (disabled > 0)
                    {
                        Debug.Log($"[MRAnalisisColision] Menú desactivado para '{clothingName}': {disabled} componente(s) desactivado(s)");
                    }
                }

                EditorUtility.SetDirty(_target);
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// Cuenta los componentes de menú en una lista de entradas.
        /// </summary>
        private int CountMenuComponents(List<ColisionEntry> entries)
        {
            if (entries == null) return 0;

            return entries.Count(e => e.IsValid && IsMenuComponentType(e.ComponentTypeName));
        }

        /// <summary>
        /// Verifica si un tipo de componente es un componente de menú.
        /// </summary>
        private bool IsMenuComponentType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;

            foreach (var menuComp in MENU_COMPONENTS)
            {
                if (typeName.Contains(menuComp))
                    return true;
            }

            return false;
        }

        #endregion

        #region BlendshapeSync Section

        /// <summary>
        /// Dibuja la seccion especial para BlendshapeSync con informacion y controles.
        /// </summary>
        private void DrawBlendshapeSyncSection()
        {
            // Fondo con color especial
            GUI.backgroundColor = new Color(BlendshapeSyncColor.r, BlendshapeSyncColor.g, BlendshapeSyncColor.b, 0.15f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = BlendshapeSyncColor;
            EditorGUILayout.LabelField("BlendshapeSync", EditorStyles.boldLabel);
            GUI.contentColor = Color.white;

            // Badge de cantidad
            GUILayout.FlexibleSpace();
            int total = _target.BlendshapeSyncCount;
            int stopped = _target.BlendshapeSyncStoppedCount;
            string statusText = stopped > 0 ? $"{stopped}/{total} detenidos" : $"{total} activos";
            EditorGUILayout.LabelField(statusText, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Informacion del problema
            var infoStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                richText = true
            };

            EditorGUILayout.LabelField(
                "<b>Problema:</b> BlendshapeSync sincroniza blendshapes del cuerpo del avatar " +
                "en tiempo real, incluso fuera de Play Mode. Si está en la raíz de la ropa, " +
                "afecta al body constantemente.",
                infoStyle);

            EditorGUILayout.Space(2);

            EditorGUILayout.LabelField(
                "<b>Solución correcta:</b> Mover el componente al mesh que realmente necesita " +
                "sincronización (no la raíz de la prenda).",
                infoStyle);

            EditorGUILayout.Space(2);

            EditorGUILayout.LabelField(
                "<b>Solución temporal:</b> Usa los botones para detener/restaurar la " +
                "sincronización en Edit Mode sin mover el componente.",
                infoStyle);

            EditorGUILayout.Space(5);

            // Advertencia sobre reflexión
            GUI.contentColor = new Color(1f, 0.8f, 0.4f);
            EditorGUILayout.LabelField(
                "⚠ Esta funcionalidad usa reflexión y podría dejar de funcionar con " +
                "actualizaciones de Modular Avatar.",
                EditorStyles.wordWrappedMiniLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.Space(5);

            // Estado del controlador
            if (!_target.IsBlendshapeSyncControlAvailable)
            {
                EditorGUILayout.HelpBox(
                    "El controlador de BlendshapeSync no está disponible. " +
                    "La reflexión a los tipos internos de MA falló.",
                    MessageType.Warning);

                // Botón de diagnóstico
                if (GUILayout.Button("Mostrar diagnóstico", EditorStyles.miniButton))
                {
                    Debug.Log(_target.GetBlendshapeSyncControllerStatus());
                }
            }
            else
            {
                // Botones
                EditorGUILayout.BeginHorizontal();

                // Boton Detener
                GUI.enabled = _target.HasActiveBlendshapeSyncs;
                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
                if (GUILayout.Button(
                    new GUIContent("Detener Sincronización",
                        "Desregistra del loop de actualización y limpia bindings. " +
                        "El componente permanece pero no sincroniza."),
                    GUILayout.Height(25)))
                {
                    Undo.RecordObject(_target, "Detener BlendshapeSyncs");
                    int count = _target.StopAllBlendshapeSyncs();
                    if (count > 0)
                    {
                        EditorUtility.DisplayDialog("BlendshapeSync",
                            $"Se detuvo la sincronización de {count} componente(s).\n\n" +
                            "Los blendshapes del body ya no se verán afectados en Edit Mode.\n\n" +
                            "Nota: El componente sigue existiendo, solo se desactivó su efecto.",
                            "Entendido");
                    }
                    EditorUtility.SetDirty(_target);
                }
                GUI.backgroundColor = Color.white;

                // Boton Restaurar
                GUI.enabled = _target.HasStoppedBlendshapeSyncs;
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                if (GUILayout.Button(
                    new GUIContent("Restaurar Sincronización",
                        "Re-registra en el loop y fuerza rebind. " +
                        "Los blendshapes volverán a sincronizarse."),
                    GUILayout.Height(25)))
                {
                    Undo.RecordObject(_target, "Restaurar BlendshapeSyncs");
                    int count = _target.RestoreAllBlendshapeSyncs();
                    if (count > 0)
                    {
                        EditorUtility.DisplayDialog("BlendshapeSync",
                            $"Se restauró la sincronización de {count} componente(s).\n\n" +
                            "Los blendshapes volverán a sincronizarse en Edit Mode.",
                            "Entendido");
                    }
                    EditorUtility.SetDirty(_target);
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                // Botón de diagnóstico (pequeño)
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Diagnóstico", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    string status = _target.GetBlendshapeSyncControllerStatus();
                    Debug.Log(status);
                    EditorUtility.DisplayDialog("Estado BlendshapeSyncController", status, "OK");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Verifica si un nombre de tipo corresponde a BlendshapeSync.
        /// </summary>
        private bool IsBlendshapeSyncComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            return typeName.Contains("BlendshapeSync");
        }

        #endregion

        #region NDMF Info

        private void DrawNDMFInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Comportamiento NDMF", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Durante Play Mode o Upload:",
                EditorStyles.wordWrappedMiniLabel);

            var infoText = "• Componentes PROBLEMATICOS: Desmarcados por defecto (se desactivaran). Marca para mantener.\n" +
                          "• Componentes DECISION USUARIO: Marcados por defecto (se mantienen). Desmarca para desactivar.\n" +
                          "• Componentes COMPATIBLES: No modificados (MR los respeta)\n" +
                          "• Botón MENÚ: Desactiva/activa Animator, Parameters, MenuInstaller, etc. por prenda.";

            EditorGUILayout.LabelField(infoText, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(3);

            // Info especial sobre BlendshapeSync
            if (_target.BlendshapeSyncCount > 0)
            {
                GUI.contentColor = BlendshapeSyncColor;
                EditorGUILayout.LabelField(
                    "• BlendshapeSync: Se desactiva en NDMF pero sigue activo en Edit Mode. " +
                    "Usa 'Detener Sincronización' para evitar efectos en el body mientras editas.",
                    EditorStyles.wordWrappedMiniLabel);
                GUI.contentColor = Color.white;
                EditorGUILayout.Space(3);
            }

            GUI.contentColor = Color.gray;
            EditorGUILayout.LabelField(
                "El componente MRAnalisisColision se destruye despues de procesar.",
                EditorStyles.miniLabel);
            GUI.contentColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
