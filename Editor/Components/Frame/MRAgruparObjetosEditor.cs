using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Editor.Components.Frame.Modules;
using Bender_Dios.MenuRadial.Localization;
using L = Bender_Dios.MenuRadial.Localization.MRLocalizationKeys;

namespace Bender_Dios.MenuRadial.Editor.Components.Frame
{
    /// <summary>
    /// Editor personalizado refactorizado para el componente MRAgruparObjetos
    /// Responsabilidad única: Coordinar los módulos especializados
    /// Versión: 0.037 - BOTÓN VERDE + TEXTO EXPLICATIVO
    /// Versión: 0.038 - Auto-actualizar Rutas agregado para consistencia con MR Radial Menu
    /// Versión: 0.039 - OPTIMIZACIÓN: Sistema de comparación de estado para evitar llamadas redundantes RecalculatePaths()
    /// Versión: 0.040 - FIX: Preview persiste al cambiar a componentes no-MR
    /// </summary>
    [CustomEditor(typeof(MRAgruparObjetos))]
    public class MRAgruparObjetosEditor : UnityEditor.Editor
    {
        private MRAgruparObjetos _target;

        // Módulos especializados - Patrón Strategy
        private ObjectListEditor _objectListEditor;
        private MaterialListEditor _materialListEditor;
        private BlendshapeListEditor _blendshapeListEditor;

        // Estados previos para comparación (evitar llamadas redundantes RecalculatePaths)
        private List<int> _lastObjectIds = new List<int>();
        private List<int> _lastMaterialIds = new List<int>();
        private List<int> _lastBlendshapeIds = new List<int>();

        // Para manejo correcto de selección
        private static bool _isSelectionChangeHandlerRegistered = false;
        private static MRAgruparObjetos _lastActiveFrameObject = null;

        /// <summary>
        /// Inicialización del editor y sus módulos
        /// </summary>
        private void OnEnable()
        {
            _target = (MRAgruparObjetos)target;

            // Inicializar módulos especializados - Inyección de dependencias manual
            _objectListEditor = new ObjectListEditor(_target);
            _materialListEditor = new MaterialListEditor(_target);
            _blendshapeListEditor = new BlendshapeListEditor(_target);

            // Registrar el handler de cambio de selección (una sola vez)
            if (!_isSelectionChangeHandlerRegistered)
            {
                Selection.selectionChanged += OnSelectionChanged;
                _isSelectionChangeHandlerRegistered = true;
            }

            // Guardar referencia al objeto activo actual
            _lastActiveFrameObject = _target;
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
            // Si no hay un FrameObject anterior con preview activo, no hay nada que hacer
            if (_lastActiveFrameObject == null || !_lastActiveFrameObject.IsPreviewActive)
            {
                // Actualizar referencia al nuevo objeto si hay uno seleccionado
                UpdateLastActiveFrameObject();
                return;
            }

            // Verificar si el nuevo objeto seleccionado tiene componentes MR que podrían solaparse
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                // Solo cancelar si se va a otro componente del sistema MR
                // Nota: MRMenuControl está en Assembly-CSharp, usamos búsqueda por nombre
                bool hasConflictingComponent =
                    selectedObject.GetComponent<MRAgruparObjetos>() != null ||
                    selectedObject.GetComponent("MRMenuControl") != null ||
                    selectedObject.GetComponent<MRUnificarObjetos>() != null;

                if (hasConflictingComponent)
                {
                    _lastActiveFrameObject.CancelPreview();
                }
                // Si no tiene componentes conflictivos, mantener el preview activo
            }
            else
            {
                // Si no hay nada seleccionado, cancelar el preview
                _lastActiveFrameObject.CancelPreview();
            }

            // Actualizar referencia al nuevo objeto
            UpdateLastActiveFrameObject();
        }

        /// <summary>
        /// Actualiza la referencia al último MRAgruparObjetos activo
        /// </summary>
        private static void UpdateLastActiveFrameObject()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                var frameObject = selectedObject.GetComponent<MRAgruparObjetos>();
                if (frameObject != null)
                {
                    _lastActiveFrameObject = frameObject;
                }
            }
        }

        /// <summary>
        /// Dibuja la interfaz del inspector usando los módulos
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;
            
            serializedObject.Update();
            
            // Header del componente
            DrawHeader();
            
            EditorGUILayout.Space(EditorStyleManager.SPACING);
            
            // Configuración general (incluyendo auto-actualizar rutas)
            DrawGeneralConfiguration();
            
            EditorGUILayout.Space(EditorStyleManager.SPACING);
            
            // Botón de previsualización
            DrawPreviewButton();
            
            EditorGUILayout.Space(EditorStyleManager.SPACING);
            
            // Delegar cada sección a su módulo especializado
            _objectListEditor?.DrawObjectSection();
            
            EditorGUILayout.Space(EditorStyleManager.SPACING);
            
            _materialListEditor?.DrawMaterialSection();
            
            EditorGUILayout.Space(EditorStyleManager.SPACING);
            
            _blendshapeListEditor?.DrawBlendshapeSection();
            
            // Aplicar cambios y manejar auto-actualización
            HandlePropertyChanges();
        }
        
        /// <summary>
        /// Dibuja el header del componente
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.Frame.HEADER), EditorStyleManager.HeaderStyle);
        }
        
        /// <summary>
        /// Dibuja la sección de configuración general
        /// NUEVO: Incluye Auto-actualizar Rutas para consistencia con MR Radial Menu
        /// </summary>
        private void DrawGeneralConfiguration()
        {
            EditorGUILayout.LabelField(MRLocalization.Get(L.Frame.GENERAL_CONFIG), EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Auto-actualizar Rutas (consistente con MR Radial Menu)
            var autoUpdatePathsProp = serializedObject.FindProperty("_autoUpdatePaths");
            var autoUpdateContent = MRLocalization.GetContent(L.Common.AUTO_UPDATE, L.Common.AUTO_UPDATE_TOOLTIP);

            if (autoUpdatePathsProp != null)
            {
                EditorGUILayout.PropertyField(autoUpdatePathsProp, autoUpdateContent);
            }
            else
            {
                // Fallback si no encuentra la propiedad
                _target.AutoUpdatePaths = EditorGUILayout.Toggle(autoUpdateContent, _target.AutoUpdatePaths);
            }

            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// Dibuja el botón de previsualización con estado visual
        /// </summary>
        private void DrawPreviewButton()
        {
            // Determinar texto y color del botón según el estado
            string buttonText = _target.IsPreviewActive
                ? MRLocalization.Get(L.Common.CANCEL_PREVIEW)
                : MRLocalization.Get(L.Frame.PREVIEW_BUTTON);
            Color buttonColor = _target.IsPreviewActive ? Color.green : Color.white;

            // Dibujar con color temporal
            EditorStyleManager.WithColor(buttonColor, () => {
                if (GUILayout.Button(buttonText, EditorStyleManager.ButtonStyle))
                {
                    _target.PreviewFrame();

                    // Forzar actualización de la interfaz
                    EditorUtility.SetDirty(_target);
                    Repaint();
                }
            });
        }
        
        /// <summary>
        /// Maneja los cambios en las propiedades y ejecuta acciones correspondientes
        /// NUEVO: Incluye auto-actualización de rutas como MR Radial Menu
        /// NUEVO: Refresca el preview automáticamente cuando hay cambios
        /// </summary>
        private void HandlePropertyChanges()
        {
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();

                // Auto-actualizar rutas si está habilitado
                HandleAutoUpdatePaths();

                // Refrescar preview si está activo para mostrar cambios en tiempo real
                if (_target.IsPreviewActive)
                {
                    _target.RefreshPreview();
                }
            }
        }
        
        /// <summary>
        /// Maneja la auto-actualización de rutas con comparación de estado
        /// Optimizado: Solo recalcula si hay cambios reales en las listas
        /// </summary>
        private void HandleAutoUpdatePaths()
        {
            if (!_target.AutoUpdatePaths) return;
            
            // Capturar estados actuales de las tres listas
            var currentObjectIds = GetCurrentObjectIds();
            var currentMaterialIds = GetCurrentMaterialIds();
            var currentBlendshapeIds = GetCurrentBlendshapeIds();
            
            bool hasChanges = false;
            
            // Comparar objetos
            if (!currentObjectIds.SequenceEqual(_lastObjectIds))
            {
                _lastObjectIds = currentObjectIds;
                hasChanges = true;
            }
            
            // Comparar materiales  
            if (!currentMaterialIds.SequenceEqual(_lastMaterialIds))
            {
                _lastMaterialIds = currentMaterialIds;
                hasChanges = true;
            }
            
            // Comparar blendshapes
            if (!currentBlendshapeIds.SequenceEqual(_lastBlendshapeIds))
            {
                _lastBlendshapeIds = currentBlendshapeIds;
                hasChanges = true;
            }
            
            // Solo recalcular si hay cambios reales
            if (hasChanges)
            {
                _target.RecalculatePaths();
            }
        }
        
        /// <summary>
        /// Obtiene los IDs actuales de objetos para comparación de estado
        /// </summary>
        private List<int> GetCurrentObjectIds()
        {
            if (_target?.ObjectReferences == null) return new List<int>();
            return _target.ObjectReferences.Select(obj => obj?.GameObject != null ? obj.GameObject.GetInstanceID() : 0).ToList();
        }
        
        /// <summary>
        /// Obtiene los IDs actuales de materiales para comparación de estado
        /// </summary>
        private List<int> GetCurrentMaterialIds()
        {
            if (_target?.MaterialReferences == null) return new List<int>();
            return _target.MaterialReferences.Select(mat => mat?.TargetRenderer != null ? mat.TargetRenderer.GetInstanceID() : 0).ToList();
        }
        
        /// <summary>
        /// Obtiene los IDs actuales de blendshapes para comparación de estado
        /// </summary>
        private List<int> GetCurrentBlendshapeIds()
        {
            if (_target?.BlendshapeReferences == null) return new List<int>();
            return _target.BlendshapeReferences.Select(blend => blend?.TargetRenderer != null ? blend.TargetRenderer.GetInstanceID() : 0).ToList();
        }
    }
}
