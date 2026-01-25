using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Models;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;

namespace Bender_Dios.MenuRadial.Editor.Components.AnalisisColision
{
    /// <summary>
    /// Editor personalizado para MRAnalisisColision.
    /// Muestra componentes de MA detectados agrupados por categoria.
    /// </summary>
    [CustomEditor(typeof(MRAnalisisColision))]
    public class MRAnalisisColisionEditor : UnityEditor.Editor
    {
        private MRAnalisisColision _target;
        private bool _showProblematic = true;
        private bool _showUserDecision = true;
        private bool _showCompatible = false;
        private Vector2 _scrollPos;

        // Colores
        private static readonly Color ProblematicColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color UserDecisionColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color CompatibleColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color MABlueColor = new Color(0.4f, 0.7f, 1f);

        private void OnEnable()
        {
            _target = (MRAnalisisColision)target;
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

                // Resumen
                DrawSummarySection();

                EditorGUILayout.Space(8);

                // Secciones de componentes
                if (_target.IsScanned && _target.HasAnyColision)
                {
                    DrawProblematicSection();
                    EditorGUILayout.Space(5);

                    DrawUserDecisionSection();
                    EditorGUILayout.Space(5);

                    DrawCompatibleSection();
                    EditorGUILayout.Space(8);
                }

                // Botones de accion
                DrawActionButtons();

                EditorGUILayout.Space(8);

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
                EditorGUILayout.LabelField("Haz clic en 'Escanear' para detectar colisiones.", EditorStyles.miniLabel);
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
                // Resumen con iconos
                EditorGUILayout.BeginHorizontal();

                // Problematicos
                if (_target.HasProblematic)
                {
                    GUI.contentColor = ProblematicColor;
                    GUILayout.Label(EditorGUIUtility.IconContent("d_redLight"), GUILayout.Width(20), GUILayout.Height(18));
                    EditorGUILayout.LabelField($"{_target.ProblematicCount}", GUILayout.Width(30));
                    GUI.contentColor = Color.white;
                }

                // Decision de usuario
                if (_target.HasUserDecision)
                {
                    GUI.contentColor = UserDecisionColor;
                    GUILayout.Label(EditorGUIUtility.IconContent("d_orangeLight"), GUILayout.Width(20), GUILayout.Height(18));
                    EditorGUILayout.LabelField($"{_target.UserDecisionCount}", GUILayout.Width(30));
                    GUI.contentColor = Color.white;
                }

                // Compatibles
                if (_target.CompatibleCount > 0)
                {
                    GUI.contentColor = CompatibleColor;
                    GUILayout.Label(EditorGUIUtility.IconContent("d_greenLight"), GUILayout.Width(20), GUILayout.Height(18));
                    EditorGUILayout.LabelField($"{_target.CompatibleCount}", GUILayout.Width(30));
                    GUI.contentColor = Color.white;
                }

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
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Problematic Section

        private void DrawProblematicSection()
        {
            if (!_target.HasProblematic) return;

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f, 0.3f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header con foldout
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = ProblematicColor;
            _showProblematic = EditorGUILayout.Foldout(_showProblematic,
                $"Problematicos ({_target.ProblematicCount})", true, EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            // Boton desactivar todos
            if (GUILayout.Button("Desactivar Todos", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                Undo.RecordObject(_target, "Desactivar Problematicos");
                int disabled = _target.DisableAllProblematic();
                if (disabled > 0)
                {
                    EditorUtility.DisplayDialog("Componentes Desactivados",
                        $"Se desactivaron {disabled} componente(s) problematico(s).", "OK");
                }
                EditorUtility.SetDirty(_target);
            }
            EditorGUILayout.EndHorizontal();

            if (_showProblematic)
            {
                DrawEntryList(_target.ScanResult.ProblematicEntries, false);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region User Decision Section

        private void DrawUserDecisionSection()
        {
            if (!_target.HasUserDecision) return;

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.5f, 0.3f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header con foldout
            GUI.contentColor = UserDecisionColor;
            _showUserDecision = EditorGUILayout.Foldout(_showUserDecision,
                $"Requieren Decision ({_target.UserDecisionCount})", true, EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            if (_showUserDecision)
            {
                EditorGUILayout.LabelField(
                    "Marca los componentes que deseas desactivar durante el build:",
                    EditorStyles.miniLabel);
                EditorGUILayout.Space(3);

                DrawEntryList(_target.ScanResult.UserDecisionEntries, true);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Compatible Section

        private void DrawCompatibleSection()
        {
            if (_target.CompatibleCount == 0) return;

            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f, 0.3f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Header con foldout
            GUI.contentColor = CompatibleColor;
            _showCompatible = EditorGUILayout.Foldout(_showCompatible,
                $"Compatibles ({_target.CompatibleCount})", true, EditorStyles.foldoutHeader);
            GUI.contentColor = Color.white;

            if (_showCompatible)
            {
                EditorGUILayout.LabelField(
                    "Estos componentes son compatibles con MR (solo informativos):",
                    EditorStyles.miniLabel);
                EditorGUILayout.Space(3);

                DrawEntryList(_target.ScanResult.CompatibleEntries, false);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Entry List

        private void DrawEntryList(System.Collections.Generic.List<ColisionEntry> entries, bool showCheckbox)
        {
            if (entries == null || entries.Count == 0) return;

            foreach (var entry in entries)
            {
                if (!entry.IsValid) continue;

                EditorGUILayout.BeginHorizontal();

                // Checkbox para UserDecision
                if (showCheckbox)
                {
                    EditorGUI.BeginChangeCheck();
                    bool newValue = EditorGUILayout.Toggle(entry.UserWantsDisabled, GUILayout.Width(20));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_target, "Toggle Component");
                        entry.UserWantsDisabled = newValue;
                        EditorUtility.SetDirty(_target);
                    }
                }
                else
                {
                    // Icono de estado
                    var icon = entry.IsEnabled
                        ? EditorGUIUtility.IconContent("d_greenLight")
                        : EditorGUIUtility.IconContent("d_redLight");
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
                }

                // Nombre del tipo
                EditorGUILayout.LabelField(entry.ShortTypeName, EditorStyles.miniLabel, GUILayout.Width(120));

                // GameObject (clickeable)
                if (GUILayout.Button(entry.GameObjectName, EditorStyles.linkLabel))
                {
                    if (entry.Component != null)
                    {
                        Selection.activeGameObject = entry.Component.gameObject;
                        EditorGUIUtility.PingObject(entry.Component.gameObject);
                    }
                }

                // Indicador de raiz de ropa
                if (entry.IsOnClothingRoot)
                {
                    GUI.contentColor = new Color(1f, 0.6f, 0.2f);
                    EditorGUILayout.LabelField("[Ropa]", EditorStyles.miniBoldLabel, GUILayout.Width(45));
                    GUI.contentColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Action Buttons

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Boton Escanear
            if (GUILayout.Button(new GUIContent("Escanear", "Re-escanear componentes de MA"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Escanear Avatar");
                _target.ScanAvatar();
                EditorUtility.SetDirty(_target);
            }

            // Boton Desactivar Problematicos en Raiz
            GUI.enabled = _target.HasProblematicOnRoot;
            if (GUILayout.Button(new GUIContent("Desactivar en Raiz", "Desactivar problematicos en raiz de ropa"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Desactivar en Raiz");
                int disabled = _target.DisableProblematicOnClothingRoots();
                if (disabled > 0)
                {
                    EditorUtility.DisplayDialog("Componentes Desactivados",
                        $"Se desactivaron {disabled} componente(s) en raiz de ropa.", "OK");
                }
                EditorUtility.SetDirty(_target);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Boton Restaurar
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _target.TotalCount > 0;
            if (GUILayout.Button(new GUIContent("Restaurar Todos", "Restaurar componentes a su estado original"), GUILayout.Height(25)))
            {
                Undo.RecordObject(_target, "Restaurar Componentes");
                int restored = _target.RestoreAllComponents();
                if (restored > 0)
                {
                    EditorUtility.DisplayDialog("Componentes Restaurados",
                        $"Se restauraron {restored} componente(s) a su estado original.", "OK");
                }
                EditorUtility.SetDirty(_target);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
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

            var infoText = "• Componentes PROBLEMATICOS en raiz de ropa: Desactivados automaticamente\n" +
                          "• Componentes DECISION USUARIO marcados: Desactivados si el checkbox esta activo\n" +
                          "• Componentes COMPATIBLES: No modificados (MR los respeta)";

            EditorGUILayout.LabelField(infoText, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(3);

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
