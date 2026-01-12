using UnityEngine;
using UnityEditor;
using System;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame.Modules
{
    /// <summary>
    /// Gestor de estilos SIMPLIFICADO - Enfoque en lo esencial
    /// ANTES: 165 líneas | DESPUÉS: ~80 líneas | AHORRO: 85 líneas
    /// </summary>
    public static class EditorStyleManager
    {
        // Constantes esenciales
        public const float BUTTON_HEIGHT = 30f;
        public const float DROP_AREA_HEIGHT = 60f;
        public const float SPACING = 5f;
        public const float SMALL_BUTTON_HEIGHT = 25f;
        public const float ICON_BUTTON_WIDTH = 25f;
        public const float ICON_BUTTON_HEIGHT = 18f;
        
        // Estilos lazy-loaded (solo cuando se necesiten)
        private static GUIStyle _headerStyle;
        private static GUIStyle _foldoutStyle;
        private static GUIStyle _dropAreaStyle;
        private static GUIStyle _buttonStyle;
        
        public static GUIStyle HeaderStyle => _headerStyle ?? (_headerStyle = CreateHeaderStyle());
        public static GUIStyle FoldoutStyle => _foldoutStyle ?? (_foldoutStyle = CreateFoldoutStyle());
        public static GUIStyle DropAreaStyle => _dropAreaStyle ?? (_dropAreaStyle = CreateDropAreaStyle());
        public static GUIStyle ButtonStyle => _buttonStyle ?? (_buttonStyle = CreateButtonStyle());
        
        private static GUIStyle CreateHeaderStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
        
        private static GUIStyle CreateFoldoutStyle()
        {
            return new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
        
        private static GUIStyle CreateDropAreaStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray }
            };
        }
        
        private static GUIStyle CreateButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        /// <summary>
        /// Dibuja área de drag & drop SIMPLIFICADA
        /// </summary>
        public static Rect DrawDropArea(string iconName, string title, string subtitle)
        {
            var rect = GUILayoutUtility.GetRect(0, DROP_AREA_HEIGHT, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "", DropAreaStyle);
            
            // Layout simple sin cálculos complejos
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            
            if (!string.IsNullOrEmpty(iconName))
                GUILayout.Label(EditorGUIUtility.IconContent(iconName), GUILayout.Width(20), GUILayout.Height(20));
            
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(subtitle, DropAreaStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            return rect;
        }
        
        /// <summary>
        /// Dibuja botones de gestión SIMPLIFICADO
        /// </summary>
        public static void DrawManagementButtons(params (string text, Action callback)[] buttons)
        {
            if (buttons == null || buttons.Length == 0) return;
            
            EditorGUILayout.BeginHorizontal();
            foreach (var (text, callback) in buttons)
            {
                if (GUILayout.Button(text, GUILayout.Height(SMALL_BUTTON_HEIGHT)))
                    callback?.Invoke();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Dibuja header de tabla SIMPLIFICADO
        /// </summary>
        public static void DrawTableHeader(params (string text, float width)[] columns)
        {
            if (columns == null || columns.Length == 0) return;
            
            EditorGUILayout.BeginHorizontal();
            foreach (var (text, width) in columns)
            {
                if (width > 0)
                    EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Width(width));
                else
                    EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            // Separador simple
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        
        /// <summary>
        /// Botón de icono SIMPLIFICADO
        /// </summary>
        public static bool DrawIconButton(string iconName, string tooltip = "")
        {
            var content = new GUIContent(EditorGUIUtility.IconContent(iconName)) { tooltip = tooltip };
            return GUILayout.Button(content, GUILayout.Width(ICON_BUTTON_WIDTH), GUILayout.Height(ICON_BUTTON_HEIGHT));
        }
        
        /// <summary>
        /// Aplicar color temporal SIMPLIFICADO
        /// </summary>
        public static void WithColor(Color color, Action drawAction)
        {
            if (drawAction == null) return;
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            try
            {
                drawAction();
            }
            finally
            {
                GUI.backgroundColor = originalColor;
            }
        }
    }
}