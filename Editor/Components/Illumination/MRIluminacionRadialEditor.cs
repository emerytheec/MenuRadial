using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.Illumination;

namespace Bender_Dios.MenuRadial.Editor.Components.Illumination
{
    /// <summary>
    /// Editor personalizado para el componente MRIluminacionRadial
    /// Siguiendo el patrón modular establecido en el proyecto
    /// VERSIÓN 0.034: Auto-actualizar Rutas agregado para consistencia con otros componentes MR
    /// </summary>
    [CustomEditor(typeof(MRIluminacionRadial))]
    public class MRIluminacionRadialEditor : UnityEditor.Editor
    {
        private MRIluminacionRadial _target;
        private IlluminationUIRenderer _uiRenderer;
        private IlluminationPreviewManager _previewManager;
        
        /// <summary>
        /// Inicialización del editor
        /// </summary>
        private void OnEnable()
        {
            _target = (MRIluminacionRadial)target;
            
            // Inicializar módulos con inyección de dependencias
            _previewManager = new IlluminationPreviewManager(_target);
            _uiRenderer = new IlluminationUIRenderer(_target, serializedObject, _previewManager);
        }
        
        /// <summary>
        /// Renderizado del inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Actualizar objeto serializado
            serializedObject.Update();
            
            // Delegación completa al renderizador de UI
            _uiRenderer.RenderUI();
            
            // Manejar cambios de propiedades
            HandlePropertyChanges();
        }
        
        /// <summary>
        /// Maneja cambios en las propiedades del componente
        /// </summary>
        private void HandlePropertyChanges()
        {
            if (serializedObject.ApplyModifiedProperties())
            {
                // Notificar cambios al preview manager
                _previewManager?.OnPropertiesChanged();
                
                // Auto-actualizar rutas si está habilitado
                HandleAutoUpdatePaths();
                
                // Marcar objeto como modificado
                EditorUtility.SetDirty(_target);
            }
        }
        
        /// <summary>
        /// Maneja la auto-actualización de rutas (consistencia con otros componentes MR)
        /// NUEVO: Para funcionalidad Auto-actualizar Rutas
        /// </summary>
        private void HandleAutoUpdatePaths()
        {
            if (_target.AutoUpdatePaths)
            {
                // Usar el método RecalculatePaths() recién implementado
                _target.RecalculatePaths();
            }
        }
        
        /// <summary>
        /// Limpieza al deshabilitar el editor
        /// </summary>
        private void OnDisable()
        {
            _previewManager?.OnDisable();
        }
    }
}