using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Shaders.Models
{
    /// <summary>
    /// Keyframe para animaciones de iluminación
    /// </summary>
    [Serializable]
    public class IlluminationKeyframe
    {
        [SerializeField] private float _time;
        [SerializeField] private IlluminationProperties _properties;
        
        /// <summary>
        /// Tiempo del keyframe en segundos
        /// </summary>
        public float Time 
        { 
            get => _time; 
            set => _time = Mathf.Max(0f, value); 
        }
        
        /// <summary>
        /// Propiedades de iluminación en este keyframe
        /// </summary>
        public IlluminationProperties Properties 
        { 
            get => _properties ?? (_properties = new IlluminationProperties()); 
            set => _properties = value ?? new IlluminationProperties(); 
        }
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public IlluminationKeyframe()
        {
            _time = 0f;
            _properties = new IlluminationProperties();
        }
        
        /// <summary>
        /// Constructor con tiempo y propiedades
        /// </summary>
        /// <param name="time">Tiempo del keyframe</param>
        /// <param name="properties">Propiedades de iluminación</param>
        public IlluminationKeyframe(float time, IlluminationProperties properties)
        {
            Time = time;
            Properties = properties;
        }
        
        /// <summary>
        /// Crea los keyframes predefinidos para la animación de iluminación radial
        /// </summary>
        /// <returns>Array con los 3 keyframes predefinidos</returns>
        public static IlluminationKeyframe[] CreateDefaultKeyframes()
        {
            return new[]
            {
                new IlluminationKeyframe(0f, IlluminationProperties.CreateFrame0()),      // Frame 0
                new IlluminationKeyframe(127f / 60f, IlluminationProperties.CreateFrame127()), // Frame 127
                new IlluminationKeyframe(255f / 60f, IlluminationProperties.CreateFrame255())  // Frame 255
            };
        }
        
        /// <summary>
        /// Crea keyframe para frame 0 (estado inicial)
        /// </summary>
        /// <returns>Keyframe del frame 0</returns>
        public static IlluminationKeyframe CreateFrame0()
        {
            return new IlluminationKeyframe(0f, IlluminationProperties.CreateFrame0());
        }
        
        /// <summary>
        /// Crea keyframe para frame 127 (estado intermedio)
        /// </summary>
        /// <returns>Keyframe del frame 127</returns>
        public static IlluminationKeyframe CreateFrame127()
        {
            return new IlluminationKeyframe(127f / 60f, IlluminationProperties.CreateFrame127());
        }
        
        /// <summary>
        /// Crea keyframe para frame 255 (estado final)
        /// </summary>
        /// <returns>Keyframe del frame 255</returns>
        public static IlluminationKeyframe CreateFrame255()
        {
            return new IlluminationKeyframe(255f / 60f, IlluminationProperties.CreateFrame255());
        }
        
        /// <summary>
        /// Copia los datos de otro keyframe
        /// </summary>
        /// <param name="other">Keyframe a copiar</param>
        public void CopyFrom(IlluminationKeyframe other)
        {
            if (other == null) return;
            
            Time = other.Time;
            Properties.CopyFrom(other.Properties);
        }
        
        /// <summary>
        /// Representación en string
        /// </summary>
        /// <returns>String con información del keyframe</returns>
        public override string ToString()
        {
            return $"IlluminationKeyframe(Time:{Time:F3}s, {Properties})";
        }
    }
}