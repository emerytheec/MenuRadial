using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Editor.Components.Illumination
{
    /// <summary>
    /// Gestor de preview para el componente MRIluminacionRadial
    /// Responsabilidad única: Gestión de preview, estados y control de frames de iluminación
    /// VERSIÓN 0.033: Soporte para interfaz normalizada (0-1), lógica interna conserva 0-255
    /// </summary>
    public class IlluminationPreviewManager
    {
        private readonly MRIluminacionRadial _target;
        private bool _isPreviewActive = false;
        
        // Sistema de frames de iluminación (similar al RadialMenu)
        private int _currentFrame = 127; // Frame por defecto (medio)
        private Dictionary<Material, IlluminationProperties> _originalProperties;
        private bool _hasStoredOriginalProperties = false;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">Componente objetivo</param>
        public IlluminationPreviewManager(MRIluminacionRadial target)
        {
            _target = target ?? throw new System.ArgumentNullException(nameof(target));
            _originalProperties = new Dictionary<Material, IlluminationProperties>();
        }
        
        
        /// <summary>
        /// Frame actual de iluminación (0-255)
        /// </summary>
        public int CurrentFrame
        {
            get => _currentFrame;
            set
            {
                if (value >= 0 && value <= 255)
                {
                    _currentFrame = value;
                    ApplyFramePreview(_currentFrame);
                }
            }
        }
        
        /// <summary>
        /// Indica si el preview está activo
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;
        
        
        
        /// <summary>
        /// Maneja cambios en las propiedades del componente
        /// </summary>
        public void OnPropertiesChanged()
        {
            // Si hay preview activo, aplicar el frame actual
            if (_isPreviewActive && _target.DetectedMaterials.Count > 0)
            {
                ApplyFramePreview(_currentFrame);
            }
        }
        
        /// <summary>
        /// Activa el modo preview y almacena propiedades originales
        /// </summary>
        public void StartPreview()
        {
            if (_isPreviewActive) return;
            
            // Escanear materiales si no se ha hecho
            if (_target.DetectedMaterials.Count == 0)
            {
                _target.ScanMaterials();
            }
            
            // Almacenar propiedades originales antes de aplicar preview
            StoreOriginalProperties();
            
            _isPreviewActive = true;
            ApplyFramePreview(_currentFrame);
            
        }
        
        /// <summary>
        /// Desactiva el modo preview y restaura propiedades originales
        /// </summary>
        public void StopPreview()
        {
            if (!_isPreviewActive) return;
            
            _isPreviewActive = false;
            RestoreOriginalProperties();
            
        }
        
        /// <summary>
        /// Aplica el frame especificado a todos los materiales
        /// </summary>
        /// <param name="frame">Frame de 0 a 255</param>
        public void ApplyFramePreview(int frame)
        {
            if (!_isPreviewActive || _target.DetectedMaterials.Count == 0) return;
            
            // Obtener propiedades para el frame especificado
            var frameProperties = GetPropertiesForFrame(frame);
            
            // Aplicar a todos los materiales
            ApplyPropertiesToMaterials(frameProperties);
            
        }
        
        /// <summary>
        /// Navegación: Ir al siguiente frame
        /// Incremento ajustado para interfaz normalizada (~0.004 por paso)
        /// </summary>
        public void NextFrame()
        {
            CurrentFrame = Mathf.Min(255, _currentFrame + 1);
        }
        
        /// <summary>
        /// Navegación: Ir al frame anterior
        /// Decremento ajustado para interfaz normalizada (~0.004 por paso)
        /// </summary>
        public void PreviousFrame()
        {
            CurrentFrame = Mathf.Max(0, _currentFrame - 1);
        }
        
        /// <summary>
        /// Salta a un frame específico preconfigurado
        /// </summary>
        /// <param name="frameType">0, 127, o 255</param>
        public void JumpToPresetFrame(int frameType)
        {
            CurrentFrame = frameType switch
            {
                0 => 0,
                127 => 127,
                255 => 255,
                _ => _currentFrame
            };
        }
        
        
        
        /// <summary>
        /// Obtiene las propiedades de iluminación para un frame específico
        /// </summary>
        /// <param name="frame">Frame de 0 a 255</param>
        /// <returns>Propiedades de iluminación para el frame</returns>
        private IlluminationProperties GetPropertiesForFrame(int frame)
        {
            // Interpolación lineal entre los tres puntos predefinidos
            if (frame <= 127)
            {
                // Interpolación entre frame 0 y frame 127
                float t = frame / 127f;
                return IlluminationProperties.Lerp(IlluminationProperties.CreateFrame0(), IlluminationProperties.CreateFrame127(), t);
            }
            else
            {
                // Interpolación entre frame 127 y frame 255
                float t = (frame - 127) / 128f;
                return IlluminationProperties.Lerp(IlluminationProperties.CreateFrame127(), IlluminationProperties.CreateFrame255(), t);
            }
        }
        
        /// <summary>
        /// Aplica propiedades específicas a todos los materiales detectados
        /// </summary>
        /// <param name="properties">Propiedades a aplicar</param>
        private void ApplyPropertiesToMaterials(IlluminationProperties properties)
        {
            foreach (var material in _target.DetectedMaterials)
            {
                if (material == null) continue;
                
                // Aplicar propiedades del shader
                material.SetFloat("_AsUnlit", properties.AsUnlit);
                material.SetFloat("_LightMaxLimit", properties.LightMaxLimit);
                material.SetFloat("_ShadowBorder", properties.ShadowBorder);
                material.SetFloat("_ShadowStrength", properties.ShadowStrength);
            }
        }
        
        /// <summary>
        /// Almacena las propiedades originales de todos los materiales
        /// </summary>
        private void StoreOriginalProperties()
        {
            if (_hasStoredOriginalProperties) return;
            
            _originalProperties.Clear();
            
            foreach (var material in _target.DetectedMaterials)
            {
                if (material == null) continue;
                
                var originalProps = new IlluminationProperties(
                    material.GetFloat("_AsUnlit"),
                    material.GetFloat("_LightMaxLimit"),
                    material.GetFloat("_ShadowBorder"),
                    material.GetFloat("_ShadowStrength")
                );
                
                _originalProperties[material] = originalProps;
            }
            
            _hasStoredOriginalProperties = true;
        }
        
        /// <summary>
        /// Restaura las propiedades originales de todos los materiales
        /// </summary>
        private void RestoreOriginalProperties()
        {
            if (!_hasStoredOriginalProperties) return;
            
            int restoredCount = 0;
            foreach (var kvp in _originalProperties)
            {
                var material = kvp.Key;
                var originalProps = kvp.Value;
                
                if (material == null) continue;
                
                material.SetFloat("_AsUnlit", originalProps.AsUnlit);
                material.SetFloat("_LightMaxLimit", originalProps.LightMaxLimit);
                material.SetFloat("_ShadowBorder", originalProps.ShadowBorder);
                material.SetFloat("_ShadowStrength", originalProps.ShadowStrength);
                
                restoredCount++;
            }
            
        }
        
        
        
        /// <summary>
        /// Limpieza al deshabilitar el editor
        /// </summary>
        public void OnDisable()
        {
            StopPreview();
            _originalProperties.Clear();
            _hasStoredOriginalProperties = false;
        }
        
        /// <summary>
        /// Obtiene información del estado del preview
        /// </summary>
        /// <returns>String con información del preview</returns>
        
    }
}
