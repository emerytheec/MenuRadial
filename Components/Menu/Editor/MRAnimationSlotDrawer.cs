using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;

namespace Bender_Dios.MenuRadial.Components.Menu.Editor
{
    /// <summary>
    /// PropertyDrawer personalizado para MRAnimationSlot.
    /// Valida que el targetObject tenga uno de los componentes permitidos:
    /// - MRUnificarObjetos
    /// - MRIluminacionRadial
    /// - MRUnificarMateriales
    /// - MRMenuControl (submenú)
    /// </summary>
    [CustomPropertyDrawer(typeof(MRAnimationSlot))]
    public class MRAnimationSlotDrawer : PropertyDrawer
    {
        private const float PADDING = 2f;
        private const float LINE_HEIGHT = 18f;
        private const float WARNING_HEIGHT = 36f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = LINE_HEIGHT + PADDING; // Foldout

            if (property.isExpanded)
            {
                height += (LINE_HEIGHT + PADDING) * 3; // slotName, targetObject, iconImage

                // Espacio adicional para mensaje de validación si hay error
                var targetObjectProp = property.FindPropertyRelative("targetObject");
                if (targetObjectProp.objectReferenceValue != null)
                {
                    var go = targetObjectProp.objectReferenceValue as GameObject;
                    if (go != null && !HasValidComponent(go))
                    {
                        height += WARNING_HEIGHT + PADDING;
                    }
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Obtener propiedades
            var slotNameProp = property.FindPropertyRelative("slotName");
            var targetObjectProp = property.FindPropertyRelative("targetObject");
            var iconImageProp = property.FindPropertyRelative("iconImage");

            // Calcular rects
            Rect foldoutRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);

            // Crear label con nombre del slot o índice
            string displayName = !string.IsNullOrEmpty(slotNameProp.stringValue)
                ? slotNameProp.stringValue
                : label.text;

            // Obtener tipo de animación para mostrar en el foldout
            string typeIndicator = GetTypeIndicator(targetObjectProp.objectReferenceValue as GameObject);
            if (!string.IsNullOrEmpty(typeIndicator))
            {
                displayName = $"{displayName} [{typeIndicator}]";
            }

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayName, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = position.y + LINE_HEIGHT + PADDING;
                Rect lineRect = new Rect(position.x, y, position.width, LINE_HEIGHT);

                // Slot Name
                EditorGUI.PropertyField(lineRect, slotNameProp, new GUIContent("Nombre"));
                y += LINE_HEIGHT + PADDING;
                lineRect.y = y;

                // Target Object con validación
                EditorGUI.BeginChangeCheck();

                // Guardar color original
                Color originalColor = GUI.backgroundColor;

                // Verificar si el objeto actual es válido
                var currentTarget = targetObjectProp.objectReferenceValue as GameObject;
                bool isValid = currentTarget == null || HasValidComponent(currentTarget);

                if (!isValid)
                {
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Rojo claro
                }

                EditorGUI.PropertyField(lineRect, targetObjectProp, new GUIContent("Target Object"));

                GUI.backgroundColor = originalColor;

                if (EditorGUI.EndChangeCheck())
                {
                    // Validar el nuevo objeto asignado
                    var newTarget = targetObjectProp.objectReferenceValue as GameObject;
                    if (newTarget != null && !HasValidComponent(newTarget))
                    {
                        // Mostrar diálogo de advertencia pero permitir la asignación
                        // para que el usuario vea el error y pueda corregirlo
                        Debug.LogWarning($"[MRAnimationSlot] El GameObject '{newTarget.name}' no tiene ningún componente válido (MRUnificarObjetos, MRIluminacionRadial, MRUnificarMateriales o MRMenuControl).");
                    }
                    else if (newTarget != null)
                    {
                        // Auto-asignar nombre si está vacío
                        if (string.IsNullOrEmpty(slotNameProp.stringValue))
                        {
                            slotNameProp.stringValue = newTarget.name;
                        }
                    }
                }

                y += LINE_HEIGHT + PADDING;
                lineRect.y = y;

                // Mostrar mensaje de error si no es válido
                if (!isValid && currentTarget != null)
                {
                    Rect warningRect = new Rect(position.x, y, position.width, WARNING_HEIGHT);
                    EditorGUI.HelpBox(warningRect,
                        $"'{currentTarget.name}' no tiene componente válido.\nSe requiere: MRUnificarObjetos, MRIluminacionRadial, MRUnificarMateriales o MRMenuControl",
                        MessageType.Error);
                    y += WARNING_HEIGHT + PADDING;
                    lineRect.y = y;
                }

                // Icon Image
                EditorGUI.PropertyField(lineRect, iconImageProp, new GUIContent("Icono"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Verifica si el GameObject tiene uno de los componentes permitidos
        /// </summary>
        private bool HasValidComponent(GameObject go)
        {
            if (go == null) return false;

            // Verificar MRUnificarObjetos
            if (go.GetComponent<MRUnificarObjetos>() != null)
                return true;

            // Verificar MRIluminacionRadial
            if (go.GetComponent<MRIluminacionRadial>() != null)
                return true;

            // Verificar MRUnificarMateriales
            if (go.GetComponent<MRUnificarMateriales>() != null)
                return true;

            // Verificar MRMenuControl (submenú)
            if (go.GetComponent<MRMenuControl>() != null)
                return true;

            return false;
        }

        /// <summary>
        /// Obtiene un indicador del tipo de componente para mostrar en el foldout
        /// </summary>
        private string GetTypeIndicator(GameObject go)
        {
            if (go == null) return "";

            // Verificar MRMenuControl primero (submenú)
            if (go.GetComponent<MRMenuControl>() != null)
                return "SubMenu";

            // Verificar MRIluminacionRadial
            if (go.GetComponent<MRIluminacionRadial>() != null)
                return "Illumination";

            // Verificar MRUnificarMateriales
            var unifyMaterial = go.GetComponent<MRUnificarMateriales>();
            if (unifyMaterial != null)
            {
                int linkedSlots = unifyMaterial.GetTotalLinkedSlots();
                return linkedSlots > 0 ? $"UnifyMat ({linkedSlots})" : "UnifyMat";
            }

            // Verificar MRUnificarObjetos
            var radialMenu = go.GetComponent<MRUnificarObjetos>();
            if (radialMenu != null)
            {
                int frameCount = radialMenu.FrameCount;
                if (frameCount == 0) return "Empty";
                if (frameCount == 1) return "OnOff";
                if (frameCount == 2) return "AB";
                return $"Linear ({frameCount})";
            }

            return "Invalid";
        }
    }
}
