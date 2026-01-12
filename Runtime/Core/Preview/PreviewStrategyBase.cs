using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Datos del estado original de un componente antes de activar preview
    /// Permite restaurar el estado exacto al desactivar preview
    /// </summary>
    [System.Serializable]
    public class PreviewState
    {
        /// <summary>
        /// Nombre del componente que originó este estado
        /// </summary>
        public string ComponentName { get; set; }
        
        /// <summary>
        /// Tipo de preview que estaba activo
        /// </summary>
        public PreviewType PreviewType { get; set; }
        
        /// <summary>
        /// Timestamp de cuando se guardó este estado
        /// </summary>
        public System.DateTime SavedAt { get; set; }
        
        /// <summary>
        /// Datos específicos del estado (formato libre)
        /// Cada implementación define qué guardar aquí
        /// </summary>
        public object StateData { get; set; }
        
        /// <summary>
        /// Constructor básico
        /// </summary>
        public PreviewState()
        {
            SavedAt = System.DateTime.Now;
        }
        
        /// <summary>
        /// Constructor con datos
        /// </summary>
        /// <param name="componentName">Nombre del componente</param>
        /// <param name="previewType">Tipo de preview</param>
        /// <param name="stateData">Datos del estado a guardar</param>
        public PreviewState(string componentName, PreviewType previewType, object stateData = null)
        {
            ComponentName = componentName;
            PreviewType = previewType;
            StateData = stateData;
            SavedAt = System.DateTime.Now;
        }
        
        /// <summary>
        /// Verifica si el estado es válido
        /// </summary>
        /// <returns>True si el estado puede ser usado para restaurar</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ComponentName) && 
                   PreviewType != PreviewType.None;
        }
        
        /// <summary>
        /// Obtiene información legible del estado
        /// </summary>
        /// <returns>String descriptivo</returns>
        public override string ToString()
        {
            return $"PreviewState({ComponentName}, {PreviewType}, {SavedAt:HH:mm:ss})";
        }
    }
    
    /// <summary>
    /// Clase base abstracta para estrategias de preview específicas
    /// Implementa el patrón Strategy para diferentes tipos de preview
    /// </summary>
    public abstract class PreviewStrategyBase
    {
        /// <summary>
        /// Estado guardado antes de activar preview
        /// </summary>
        protected PreviewState _savedState;
        
        /// <summary>
        /// Si el preview está actualmente activo
        /// </summary>
        protected bool _isActive;
        
        /// <summary>
        /// Nombre del componente que usa esta estrategia
        /// </summary>
        protected string _componentName;
        
        /// <summary>
        /// Constructor básico
        /// </summary>
        /// <param name="componentName">Nombre del componente</param>
        protected PreviewStrategyBase(string componentName)
        {
            _componentName = componentName;
            _isActive = false;
        }
        
        
        /// <summary>
        /// Si el preview está activo
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Tipo de preview que maneja esta estrategia
        /// </summary>
        public abstract PreviewType PreviewType { get; }
        
        /// <summary>
        /// Nombre del componente
        /// </summary>
        public string ComponentName => _componentName;
        
        
        
        /// <summary>
        /// Guarda el estado actual antes de activar preview
        /// Cada estrategia define qué datos guardar
        /// </summary>
        /// <returns>Estado guardado</returns>
        protected abstract PreviewState SaveCurrentState();
        
        /// <summary>
        /// Aplica el estado de preview específico
        /// Cada estrategia define cómo activar su preview
        /// </summary>
        protected abstract void ApplyPreviewState();
        
        /// <summary>
        /// Restaura el estado original desde el estado guardado
        /// Cada estrategia define cómo restaurar
        /// </summary>
        /// <param name="state">Estado a restaurar</param>
        protected abstract void RestoreOriginalState(PreviewState state);
        
        
        
        /// <summary>
        /// Activa el preview usando esta estrategia
        /// </summary>
        public virtual void ActivatePreview()
        {
            if (_isActive)
            {
                return;
            }
            
            // Guardar estado actual
            _savedState = SaveCurrentState();
            
            if (_savedState != null && _savedState.IsValid())
            {
                // Aplicar estado de preview
                ApplyPreviewState();
                
                // Marcar como activo
                _isActive = true;
            }
        }
        
        /// <summary>
        /// Desactiva el preview y restaura estado original
        /// </summary>
        public virtual void DeactivatePreview()
        {
            if (!_isActive)
            {
                return;
            }
            
            // Restaurar estado original si existe
            if (_savedState != null && _savedState.IsValid())
            {
                RestoreOriginalState(_savedState);
            }
            
            // Limpiar estado
            _savedState = null;
            _isActive = false;
        }
        
        
    }
}
