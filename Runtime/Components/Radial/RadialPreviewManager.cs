using System;
using System.Text;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Preview;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Gestor especializado para el sistema de previsualización de menús radiales
    /// REFACTORIZADO: Extraído de MRUnificarObjetos.cs para responsabilidad única
    /// 
    /// Responsabilidades:
    /// - Implementación completa de IPreviewable
    /// - Gestión de estados de preview (activar/desactivar)
    /// - Manejo de valores para diferentes tipos de animación
    /// - Registro en PreviewManager global
    /// - Restauración de estados originales
    /// </summary>
    public class RadialPreviewManager : IPreviewable
    {
        // Campos privados
        private readonly RadialFrameManager _frameManager;
        private readonly string _componentName;
        
        // Preview state
        private bool _isPreviewActive = false;
        private int _originalFrameIndex = 0;
        private PreviewStrategyBase _previewStrategy;
        
        // Eventos
        /// <summary>
        /// Evento disparado cuando cambia el estado de preview
        /// </summary>
        public event System.Action<bool> OnPreviewStateChanged;
        
        /// <summary>
        /// Evento disparado cuando cambia el valor durante preview
        /// </summary>
        public event System.Action<float> OnPreviewValueChanged;
        
        // Constructor
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameManager">Gestor de frames para manipular</param>
        /// <param name="componentName">Nombre del componente para logging</param>
        public RadialPreviewManager(RadialFrameManager frameManager, string componentName)
        {
            _frameManager = frameManager ?? throw new ArgumentNullException(nameof(frameManager));
            _componentName = componentName ?? "Unknown";
            
        }
        
        // Implementación de IPreviewable
        
        /// <summary>
        /// Indica si el sistema de previsualización está activo
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        /// <summary>
        /// Obtiene el tipo de previsualización basado en el tipo de animación
        /// </summary>
        /// <returns>Tipo de preview apropiado</returns>
        public PreviewType GetPreviewType()
        {
            return _frameManager.AnimationType switch
            {
                AnimationType.Linear => PreviewType.Linear,
                AnimationType.OnOff => PreviewType.Toggle,
                AnimationType.AB => PreviewType.Toggle,
                _ => PreviewType.None
            };
        }
        
        /// <summary>
        /// Activa el sistema de previsualización
        /// </summary>
        public void ActivatePreview()
        {
            if (_isPreviewActive)
            {
                return;
            }
            
            if (!_frameManager.HasValidFrames())
            {
                return;
            }
            
            // Guardar estado original
            _originalFrameIndex = _frameManager.ActiveFrameIndex;
            
            // Aplicar lógica específica según tipo de animación
            ApplyInitialPreviewState();
            
            // Aplicar el frame resultante
            _frameManager.ApplyCurrentFrame();
            
            // Marcar como activo
            _isPreviewActive = true;
            
            // Registrar en el PreviewManager global
            PreviewManager.RegisterComponent(this);
            
            // Disparar evento
            OnPreviewStateChanged?.Invoke(true);
        }
        
        /// <summary>
        /// Desactiva el sistema de previsualización y restaura el estado original
        /// </summary>
        public void DeactivatePreview()
        {
            if (!_isPreviewActive)
            {
                return;
            }
            
            // Validar y restaurar estado original de forma segura
            RestoreOriginalState();
            
            // Aplicar frame restaurado si es necesario
            if (_frameManager.HasValidFrames() && _frameManager.HasValidActiveFrame())
            {
                _frameManager.ApplyCurrentFrame();
            }
            
            // Marcar como inactivo
            _isPreviewActive = false;
            
            // Disparar evento
            OnPreviewStateChanged?.Invoke(false);
        }
        
        // Métodos públicos - Control de preview
        
        /// <summary>
        /// Alterna el estado de preview (activar/desactivar)
        /// </summary>
        public void TogglePreview()
        {
            if (_isPreviewActive)
                DeactivatePreview();
            else
                ActivatePreview();
        }
        
        /// <summary>
        /// Establece un valor específico para previews lineales
        /// </summary>
        /// <param name="normalizedValue">Valor entre 0 y 1</param>
        public void SetPreviewValue(float normalizedValue)
        {
            if (!_isPreviewActive)
            {
                return;
            }
            
            if (_frameManager.AnimationType != AnimationType.Linear)
            {
                return;
            }
            
            if (_frameManager.FrameCount <= 1)
            {
                return;
            }
            
            // Normalizar valor de entrada
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Convertir valor normalizado a índice de frame
            int targetFrame = Mathf.RoundToInt(normalizedValue * (_frameManager.FrameCount - 1));
            targetFrame = Mathf.Clamp(targetFrame, 0, _frameManager.FrameCount - 1);
            
            // Solo cambiar si es diferente
            if (targetFrame != _frameManager.ActiveFrameIndex)
            {
                _frameManager.SelectFrameByIndex(targetFrame);
                _frameManager.ApplyCurrentFrame();
                
                // Disparar evento
                OnPreviewValueChanged?.Invoke(normalizedValue);
                
            }
        }
        
        /// <summary>
        /// Para animaciones toggle (ON/OFF, A/B), alterna al estado opuesto
        /// </summary>
        public void TogglePreviewState()
        {
            if (!_isPreviewActive)
            {
                return;
            }
            
            var animType = _frameManager.AnimationType;
            if (animType != AnimationType.OnOff && animType != AnimationType.AB)
            {
                return;
            }
            
            // Alternar estado
            int newIndex = _frameManager.ActiveFrameIndex == 0 ? 1 : 0;
            _frameManager.SelectFrameByIndex(newIndex);
            _frameManager.ApplyCurrentFrame();
            
            string stateName = animType == AnimationType.OnOff ? 
                (newIndex == 1 ? "ON" : "OFF") : 
                (newIndex == 1 ? "B" : "A");
            
        }
        
        // Métodos públicos - Información
        
        
        
        // Métodos privados - Lógica de preview
        
        /// <summary>
        /// Aplica el estado inicial apropiado según el tipo de animación
        /// </summary>
        private void ApplyInitialPreviewState()
        {
            switch (_frameManager.AnimationType)
            {
                case AnimationType.Linear:
                    // Para animaciones lineales, empezar en frame 0
                    _frameManager.SelectFrameByIndex(0);
                    break;
                    
                case AnimationType.OnOff:
                    // Para ON/OFF, alternar el estado actual
                    int newOnOffIndex = _frameManager.ActiveFrameIndex == 0 ? 1 : 0;
                    _frameManager.SelectFrameByIndex(newOnOffIndex);
                    break;
                    
                case AnimationType.AB:
                    // Para A/B, alternar entre 0 y 1
                    int newABIndex = _frameManager.ActiveFrameIndex == 0 ? 1 : 0;
                    _frameManager.SelectFrameByIndex(newABIndex);
                    break;
                    
                default:
                    break;
            }
        }
        
        /// <summary>
        /// Restaura el estado original de forma segura
        /// </summary>
        private void RestoreOriginalState()
        {
            // Validar originalFrameIndex antes de restaurar
            int safeOriginalIndex = _originalFrameIndex;
            
            // Para animaciones ON/OFF (1 frame), asegurar que el índice sea válido
            if (_frameManager.FrameCount == 1)
            {
                // ON/OFF: índice conceptual puede ser 0 o 1, validar rango
                safeOriginalIndex = _originalFrameIndex >= 0 && _originalFrameIndex <= 1 ? _originalFrameIndex : 0;
            }
            else if (_frameManager.FrameCount > 1)
            {
                // Múltiples frames: validación normal con clamp
                safeOriginalIndex = Mathf.Clamp(_originalFrameIndex, 0, _frameManager.FrameCount - 1);
            }
            else
            {
                // Sin frames válidos
                safeOriginalIndex = 0;
            }
            
            // Restaurar estado original con índice seguro
            _frameManager.SelectFrameByIndex(safeOriginalIndex);
            
        }
        
        /// <summary>
        /// Fuerza la limpieza a un estado seguro en caso de error
        /// </summary>
        private void ForceCleanupToSafeState()
        {
            _isPreviewActive = false;
            
            // Restaurar a estado seguro por defecto
            if (_frameManager?.HasValidFrames() == true)
            {
                _frameManager.SelectFrameByIndex(0);
                _frameManager.ApplyCurrentFrame();
            }
            
            // Disparar evento de desactivación
            OnPreviewStateChanged?.Invoke(false);
        }
        
        // Métodos públicos - Limpieza
        
        /// <summary>
        /// Limpia el gestor de preview (llamar desde OnDestroy del componente)
        /// </summary>
        public void Cleanup()
        {
            // Limpiar eventos locales para evitar memory leaks
            OnPreviewStateChanged = null;
            OnPreviewValueChanged = null;
            
            if (_isPreviewActive)
            {
                // Desregistrar del PreviewManager global
                PreviewManager.UnregisterComponent(this);
                
                // Marcar como inactivo sin restaurar estado (el objeto se está destruyendo)
                _isPreviewActive = false;
            }
        }
        
        // Soporte de integración con Unity
        
        /// <summary>
        /// Verifica si el preview puede ser activado (para validación en UI)
        /// </summary>
        /// <returns>True si puede activarse, False si no</returns>
        public bool CanActivatePreview()
        {
            if (_isPreviewActive)
                return false;
                
            if (!_frameManager.HasValidFrames())
                return false;
                
            if (_frameManager.AnimationType == AnimationType.None)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Obtiene el valor normalizado actual para sliders (solo para Linear)
        /// </summary>
        /// <returns>Valor entre 0 y 1, o -1 si no aplicable</returns>
        public float GetCurrentNormalizedValue()
        {
            if (!_isPreviewActive || _frameManager.AnimationType != AnimationType.Linear)
                return -1f;
                
            if (_frameManager.FrameCount <= 1)
                return 0f;
                
            return (float)_frameManager.ActiveFrameIndex / (_frameManager.FrameCount - 1);
        }
        
        /// <summary>
        /// Obtiene el estado booleano actual para toggles (ON/OFF, A/B)
        /// </summary>
        /// <returns>True/False según el estado, null si no aplicable</returns>
        public bool? GetCurrentToggleState()
        {
            if (!_isPreviewActive)
                return null;
                
            var animType = _frameManager.AnimationType;
            if (animType != AnimationType.OnOff && animType != AnimationType.AB)
                return null;
                
            return _frameManager.ActiveFrameIndex == 1;
        }
    }
}
