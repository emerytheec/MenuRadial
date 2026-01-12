using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Estrategia unificada y flexible para preview que reemplaza múltiples clases
    /// Cumple con el principio de simplificación sin clases innecesarias
    /// </summary>
    public class UnifiedPreviewStrategy
    {
        // Tipos de preview soportados
        public enum PreviewType
        {
            None,
            Toggle,
            Slider,
            Custom
        }
        
        // Campos privados
        
        private readonly string _componentName;
        private PreviewType _currentType;
        private bool _isActive;
        
        // Estados guardados para restauración
        private Dictionary<GameObject, bool> _originalObjectStates = new Dictionary<GameObject, bool>();
        private Dictionary<SkinnedMeshRenderer, float[]> _originalBlendshapeValues = new Dictionary<SkinnedMeshRenderer, float[]>();
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        
        // Funciones de callback personalizables
        private System.Action _onActivateCallback;
        private System.Action _onDeactivateCallback;
        private System.Action _onApplyCallback;
        
        // Constructor
        
        /// <summary>
        /// Inicializa la estrategia unificada
        /// </summary>
        /// <param name="componentName">Nombre del componente que usa la estrategia</param>
        public UnifiedPreviewStrategy(string componentName)
        {
            _componentName = componentName ?? "Unknown";
            _currentType = PreviewType.Toggle;
        }
        
        // Propiedades públicas
        
        /// <summary>
        /// Indica si el preview está activo
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Tipo de preview actual
        /// </summary>
        public PreviewType CurrentType => _currentType;
        
        /// <summary>
        /// Número de estados guardados
        /// </summary>
        public int SavedStatesCount => _originalObjectStates.Count + _originalBlendshapeValues.Count + _originalMaterials.Count;
        
        // Métodos de configuración
        
        /// <summary>
        /// Configura la estrategia para modo Toggle
        /// </summary>
        /// <param name="onActivate">Callback al activar</param>
        /// <param name="onDeactivate">Callback al desactivar</param>
        /// <returns>Esta instancia para fluent API</returns>
        public UnifiedPreviewStrategy AsToggle(System.Action onActivate = null, System.Action onDeactivate = null)
        {
            _currentType = PreviewType.Toggle;
            _onActivateCallback = onActivate;
            _onDeactivateCallback = onDeactivate;
            return this;
        }
        
        /// <summary>
        /// Configura la estrategia para modo Slider
        /// </summary>
        /// <param name="onApply">Callback al aplicar valores</param>
        /// <returns>Esta instancia para fluent API</returns>
        public UnifiedPreviewStrategy AsSlider(System.Action onApply = null)
        {
            _currentType = PreviewType.Slider;
            _onApplyCallback = onApply;
            return this;
        }
        
        /// <summary>
        /// Configura la estrategia para modo Custom
        /// </summary>
        /// <param name="onActivate">Callback al activar</param>
        /// <param name="onDeactivate">Callback al desactivar</param>
        /// <param name="onApply">Callback al aplicar</param>
        /// <returns>Esta instancia para fluent API</returns>
        public UnifiedPreviewStrategy AsCustom(System.Action onActivate = null, System.Action onDeactivate = null, System.Action onApply = null)
        {
            _currentType = PreviewType.Custom;
            _onActivateCallback = onActivate;
            _onDeactivateCallback = onDeactivate;
            _onApplyCallback = onApply;
            return this;
        }
        
        // Métodos públicos
        
        /// <summary>
        /// Activa el preview guardando estados actuales
        /// </summary>
        /// <param name="targetObjects">GameObjects a incluir en el preview</param>
        public void Activate(IEnumerable<GameObject> targetObjects = null)
        {
            if (_isActive) return;
            
            SaveCurrentStates(targetObjects);
            _isActive = true;
            _onActivateCallback?.Invoke();
        }
        
        /// <summary>
        /// Desactiva el preview restaurando estados originales
        /// </summary>
        public void Deactivate()
        {
            if (!_isActive) return;
            
            RestoreOriginalStates();
            _isActive = false;
            _onDeactivateCallback?.Invoke();
        }
        
        /// <summary>
        /// Alterna el estado de preview
        /// </summary>
        /// <param name="targetObjects">GameObjects a incluir si se activa</param>
        public void Toggle(IEnumerable<GameObject> targetObjects = null)
        {
            if (_isActive)
                Deactivate();
            else
                Activate(targetObjects);
        }
        
        /// <summary>
        /// Aplica cambios inmediatos (útil para sliders)
        /// </summary>
        public void Apply()
        {
            if (_isActive)
            {
                _onApplyCallback?.Invoke();
            }
        }
        
        /// <summary>
        /// Aplica un valor normalizado (0-1) para previews tipo slider
        /// </summary>
        /// <param name="normalizedValue">Valor entre 0 y 1</param>
        /// <param name="applyAction">Acción que aplica el valor</param>
        public void ApplyNormalizedValue(float normalizedValue, System.Action<float> applyAction)
        {
            if (!_isActive || _currentType != PreviewType.Slider) return;
            
            normalizedValue = Mathf.Clamp01(normalizedValue);
            applyAction?.Invoke(normalizedValue);
            _onApplyCallback?.Invoke();
        }
        
        /// <summary>
        /// Limpia todos los recursos
        /// </summary>
        public void Cleanup()
        {
            if (_isActive)
            {
                Deactivate();
            }
            
            _originalObjectStates.Clear();
            _originalBlendshapeValues.Clear();
            _originalMaterials.Clear();
            _onActivateCallback = null;
            _onDeactivateCallback = null;
            _onApplyCallback = null;
        }
        
        
        // Métodos privados
        
        /// <summary>
        /// Guarda los estados actuales de los objetos
        /// </summary>
        private void SaveCurrentStates(IEnumerable<GameObject> targetObjects)
        {
            ClearSavedStates();
            
            if (targetObjects == null) return;
            
            foreach (var obj in targetObjects)
            {
                if (obj == null) continue;
                
                // Guardar estado del GameObject
                _originalObjectStates[obj] = obj.activeInHierarchy;
                
                // Guardar estados de SkinnedMeshRenderer
                var renderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.sharedMesh != null)
                    {
                        float[] blendshapeValues = new float[renderer.sharedMesh.blendShapeCount];
                        for (int i = 0; i < blendshapeValues.Length; i++)
                        {
                            blendshapeValues[i] = renderer.GetBlendShapeWeight(i);
                        }
                        _originalBlendshapeValues[renderer] = blendshapeValues;
                    }
                }
                
                // Guardar estados de materiales
                var allRenderers = obj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in allRenderers)
                {
                    if (renderer != null && renderer.materials != null)
                    {
                        _originalMaterials[renderer] = renderer.materials;
                    }
                }
            }
        }
        
        /// <summary>
        /// Restaura los estados originales guardados
        /// </summary>
        private void RestoreOriginalStates()
        {
            // Restaurar GameObjects
            foreach (var kvp in _originalObjectStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.SetActive(kvp.Value);
                }
            }
            
            // Restaurar blendshapes
            foreach (var kvp in _originalBlendshapeValues)
            {
                if (kvp.Key != null && kvp.Key.sharedMesh != null)
                {
                    var values = kvp.Value;
                    for (int i = 0; i < values.Length && i < kvp.Key.sharedMesh.blendShapeCount; i++)
                    {
                        kvp.Key.SetBlendShapeWeight(i, values[i]);
                    }
                }
            }
            
            // Restaurar materiales
            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.materials = kvp.Value;
                }
            }
            
            ClearSavedStates();
        }
        
        /// <summary>
        /// Limpia todos los estados guardados
        /// </summary>
        private void ClearSavedStates()
        {
            _originalObjectStates.Clear();
            _originalBlendshapeValues.Clear();
            _originalMaterials.Clear();
        }
    }
}
