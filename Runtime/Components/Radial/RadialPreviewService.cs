using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Servicio de previsualización para menús radiales
    /// Permite previsualizar frames individuales del menú radial
    /// </summary>
    public class RadialPreviewService
    {
        // Private Fields
        
        private readonly MRUnificarObjetos _radialMenu;
        private int _previewingFrameIndex = -1;
        private bool _isPreviewActive = false;
        
        
        // Constructor
        
        /// <summary>
        /// Constructor que requiere referencia al MRUnificarObjetos
        /// </summary>
        /// <param name="radialMenu">Referencia al menú radial</param>
        public RadialPreviewService(MRUnificarObjetos radialMenu)
        {
            _radialMenu = radialMenu ?? throw new System.ArgumentNullException(nameof(radialMenu));
        }
        
        
        // Public Properties
        
        /// <summary>
        /// Indica si hay una previsualización activa
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        /// <summary>
        /// Índice del frame que se está previsualizando (-1 si no hay previsualización)
        /// </summary>
        public int PreviewingFrameIndex => _previewingFrameIndex;
        
        /// <summary>
        /// Frame que se está previsualizando actualmente (null si no hay previsualización)
        /// </summary>
        public MRAgruparObjetos PreviewingFrame
        {
            get
            {
                if (_isPreviewActive && _previewingFrameIndex >= 0 && _previewingFrameIndex < _radialMenu.FrameCount)
                {
                    return _radialMenu.FrameObjects[_previewingFrameIndex];
                }
                return null;
            }
        }
        
        
        // Preview Methods
        
        /// <summary>
        /// Inicia la previsualización de un frame específico
        /// </summary>
        /// <param name="frameIndex">Índice del frame a previsualizar</param>
        /// <returns>True si la previsualización se inició correctamente</returns>
        public bool StartPreview(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= _radialMenu.FrameCount)
            {
                return false;
            }
            
            var frame = _radialMenu.FrameObjects[frameIndex];
            if (frame == null)
            {
                return false;
            }
            
            // Cancelar previsualización anterior si existe
            if (_isPreviewActive)
            {
                StopPreview();
            }
            
            // Iniciar nueva previsualización
            frame.PreviewFrame();
            
            _isPreviewActive = true;
            _previewingFrameIndex = frameIndex;
            
            return true;
        }
        
        /// <summary>
        /// Detiene la previsualización actual
        /// </summary>
        /// <returns>True si se detuvo una previsualización activa</returns>
        public bool StopPreview()
        {
            if (!_isPreviewActive)
            {
                return false;
            }
            
            var frame = PreviewingFrame;
            if (frame != null && frame.IsPreviewActive)
            {
                frame.CancelPreview();
            }
            
            _isPreviewActive = false;
            _previewingFrameIndex = -1;
            
            return true;
        }
        
        /// <summary>
        /// Cambia la previsualización al siguiente frame
        /// </summary>
        /// <returns>True si se cambió al siguiente frame</returns>
        public bool PreviewNextFrame()
        {
            if (_radialMenu.FrameCount == 0)
            {
                return false;
            }
            
            int nextIndex;
            
            if (_isPreviewActive)
            {
                nextIndex = (_previewingFrameIndex + 1) % _radialMenu.FrameCount;
            }
            else
            {
                nextIndex = _radialMenu.ActiveFrameIndex;
            }
            
            return StartPreview(nextIndex);
        }
        
        /// <summary>
        /// Cambia la previsualización al frame anterior
        /// </summary>
        /// <returns>True si se cambió al frame anterior</returns>
        public bool PreviewPreviousFrame()
        {
            if (_radialMenu.FrameCount == 0)
            {
                return false;
            }
            
            int previousIndex;
            
            if (_isPreviewActive)
            {
                previousIndex = (_previewingFrameIndex - 1 + _radialMenu.FrameCount) % _radialMenu.FrameCount;
            }
            else
            {
                previousIndex = _radialMenu.ActiveFrameIndex;
            }
            
            return StartPreview(previousIndex);
        }
        
        /// <summary>
        /// Alterna la previsualización del frame activo del menú radial
        /// </summary>
        /// <returns>True si se inició previsualización, False si se detuvo</returns>
        public bool TogglePreviewActiveFrame()
        {
            if (_isPreviewActive && _previewingFrameIndex == _radialMenu.ActiveFrameIndex)
            {
                // Si ya se está previsualizando el frame activo, detener
                StopPreview();
                return false;
            }
            else
            {
                // Iniciar previsualización del frame activo
                return StartPreview(_radialMenu.ActiveFrameIndex);
            }
        }
        
        
        // Validation and Information
        
        /// <summary>
        /// Valida el estado del sistema de previsualización
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public ValidationResult ValidatePreviewSystem()
        {
            var result = new ValidationResult();
            result.IsValid = true;
            result.Message = "Validación del sistema de previsualización";
            
            if (_radialMenu.FrameCount == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay frames disponibles para previsualización"));
                return result;
            }
            
            // Verificar consistencia del estado
            if (_isPreviewActive)
            {
                if (_previewingFrameIndex < 0 || _previewingFrameIndex >= _radialMenu.FrameCount)
                {
                    result.AddChild(ValidationResult.Error($"Índice de previsualización inválido: {_previewingFrameIndex}"));
                }
                
                var frame = PreviewingFrame;
                if (frame == null)
                {
                    result.AddChild(ValidationResult.Error("Frame en previsualización es null"));
                }
                else if (!frame.IsPreviewActive)
                {
                    result.AddChild(ValidationResult.Warning($"Estado inconsistente: servicio indica previsualización activa pero frame '{frame.FrameName}' no"));
                }
                else
                {
                    result.AddChild(ValidationResult.Success($"Previsualizando frame {_previewingFrameIndex} ('{frame.FrameName}')"));
                }
            }
            else
            {
                if (_previewingFrameIndex != -1)
                {
                    result.AddChild(ValidationResult.Warning($"Estado inconsistente: no hay previsualización activa pero índice es {_previewingFrameIndex}"));
                }
                
                result.AddChild(ValidationResult.Success("No hay previsualización activa"));
            }
            
            // Verificar frames con previsualización activa inconsistente
            int framesWithActivePreview = 0;
            for (int i = 0; i < _radialMenu.FrameCount; i++)
            {
                var frame = _radialMenu.FrameObjects[i];
                if (frame != null && frame.IsPreviewActive)
                {
                    framesWithActivePreview++;
                    
                    if (!_isPreviewActive || i != _previewingFrameIndex)
                    {
                        result.AddChild(ValidationResult.Warning($"Frame {i} ('{frame.FrameName}') tiene previsualización activa pero no está siendo gestionado por el servicio"));
                    }
                }
            }
            
            if (framesWithActivePreview > 1)
            {
                result.AddChild(ValidationResult.Error($"Múltiples frames ({framesWithActivePreview}) tienen previsualización activa simultáneamente"));
            }
            
            return result;
        }
        
        
        /// <summary>
        /// Repara inconsistencias en el estado de previsualización
        /// </summary>
        /// <returns>Número de inconsistencias reparadas</returns>
        public int RepairInconsistentStates()
        {
            int repairedCount = 0;
            
            // Cancelar previsualizaciones en frames que no deberían tenerlas
            for (int i = 0; i < _radialMenu.FrameCount; i++)
            {
                var frame = _radialMenu.FrameObjects[i];
                if (frame != null && frame.IsPreviewActive)
                {
                    if (!_isPreviewActive || i != _previewingFrameIndex)
                    {
                        frame.CancelPreview();
                        repairedCount++;
                    }
                }
            }
            
            // Verificar y corregir el estado interno
            if (_isPreviewActive)
            {
                var frame = PreviewingFrame;
                if (frame == null || !frame.IsPreviewActive)
                {
                    // El frame no existe o no tiene previsualización activa
                    _isPreviewActive = false;
                    _previewingFrameIndex = -1;
                    repairedCount++;
                }
            }
            
            return repairedCount;
        }
        
        
        // Cleanup
        
        /// <summary>
        /// Limpia el estado del servicio de previsualización
        /// </summary>
        public void Cleanup()
        {
            if (_isPreviewActive)
            {
                StopPreview();
            }
            
            // Asegurar que todos los frames estén sin previsualización
            RepairInconsistentStates();
        }
        
    }
}
