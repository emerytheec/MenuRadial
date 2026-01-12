using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Shaders.Models
{
    /// <summary>
    /// Propiedades de iluminación para materiales
    /// </summary>
    [Serializable]
    public class IlluminationProperties
    {
        [SerializeField] private float _asUnlit = 0f;
        [SerializeField] private float _lightMaxLimit = 1f;
        [SerializeField] private float _shadowBorder = 0.05f;
        [SerializeField] private float _shadowStrength = 0f;
        
        /// <summary>
        /// Intensidad unlit (_AsUnlit para lilToon)
        /// Rango: 0.0 - 1.0
        /// </summary>
        public float AsUnlit 
        { 
            get => _asUnlit; 
            set => _asUnlit = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// Límite máximo de luz (_LightMaxLimit para lilToon)
        /// Rango: 0.0 - 1.0
        /// </summary>
        public float LightMaxLimit 
        { 
            get => _lightMaxLimit; 
            set => _lightMaxLimit = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// Borde de sombra (_ShadowBorder para lilToon)
        /// Rango: 0.0 - 1.0
        /// </summary>
        public float ShadowBorder 
        { 
            get => _shadowBorder; 
            set => _shadowBorder = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// Fuerza de sombra (_ShadowStrength para lilToon)
        /// Rango: 0.0 - 1.0
        /// </summary>
        public float ShadowStrength 
        { 
            get => _shadowStrength; 
            set => _shadowStrength = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// Constructor por defecto con valores iniciales (Frame 0)
        /// </summary>
        public IlluminationProperties()
        {
            // Valores predeterminados para frame 0 usando constantes centralizadas
            _asUnlit = MRIlluminationConstants.FRAME0_AS_UNLIT;
            _lightMaxLimit = MRIlluminationConstants.FRAME0_LIGHT_MAX_LIMIT;
            _shadowBorder = MRIlluminationConstants.FRAME0_SHADOW_BORDER;
            _shadowStrength = MRIlluminationConstants.FRAME0_SHADOW_STRENGTH;
        }
        
        /// <summary>
        /// Constructor con valores específicos
        /// </summary>
        /// <param name="asUnlit">Intensidad unlit</param>
        /// <param name="lightMaxLimit">Límite máximo de luz</param>
        /// <param name="shadowBorder">Borde de sombra</param>
        /// <param name="shadowStrength">Fuerza de sombra</param>
        public IlluminationProperties(float asUnlit, float lightMaxLimit, float shadowBorder, float shadowStrength)
        {
            AsUnlit = asUnlit;
            LightMaxLimit = lightMaxLimit;
            ShadowBorder = shadowBorder;
            ShadowStrength = shadowStrength;
        }
        
        /// <summary>
        /// Crea propiedades para frame 0 (estado inicial/Normal)
        /// </summary>
        /// <returns>Propiedades del frame 0</returns>
        public static IlluminationProperties CreateFrame0()
        {
            return new IlluminationProperties(
                MRIlluminationConstants.FRAME0_AS_UNLIT,
                MRIlluminationConstants.FRAME0_LIGHT_MAX_LIMIT,
                MRIlluminationConstants.FRAME0_SHADOW_BORDER,
                MRIlluminationConstants.FRAME0_SHADOW_STRENGTH
            );
        }

        /// <summary>
        /// Crea propiedades para frame 127 (estado intermedio)
        /// </summary>
        /// <returns>Propiedades del frame 127</returns>
        public static IlluminationProperties CreateFrame127()
        {
            return new IlluminationProperties(
                MRIlluminationConstants.FRAME127_AS_UNLIT,
                MRIlluminationConstants.FRAME127_LIGHT_MAX_LIMIT,
                MRIlluminationConstants.FRAME127_SHADOW_BORDER,
                MRIlluminationConstants.FRAME127_SHADOW_STRENGTH
            );
        }

        /// <summary>
        /// Crea propiedades para frame 255 (estado final/Unlit)
        /// </summary>
        /// <returns>Propiedades del frame 255</returns>
        public static IlluminationProperties CreateFrame255()
        {
            return new IlluminationProperties(
                MRIlluminationConstants.FRAME255_AS_UNLIT,
                MRIlluminationConstants.FRAME255_LIGHT_MAX_LIMIT,
                MRIlluminationConstants.FRAME255_SHADOW_BORDER,
                MRIlluminationConstants.FRAME255_SHADOW_STRENGTH
            );
        }
        
        /// <summary>
        /// Copia las propiedades de otra instancia
        /// </summary>
        /// <param name="other">Propiedades a copiar</param>
        public void CopyFrom(IlluminationProperties other)
        {
            if (other == null) return;
            
            AsUnlit = other.AsUnlit;
            LightMaxLimit = other.LightMaxLimit;
            ShadowBorder = other.ShadowBorder;
            ShadowStrength = other.ShadowStrength;
        }
        
        /// <summary>
        /// Compara si las propiedades son iguales
        /// </summary>
        /// <param name="other">Propiedades a comparar</param>
        /// <returns>True si son iguales</returns>
        public bool Equals(IlluminationProperties other)
        {
            if (other == null) return false;
            
            return Mathf.Approximately(AsUnlit, other.AsUnlit) &&
                   Mathf.Approximately(LightMaxLimit, other.LightMaxLimit) &&
                   Mathf.Approximately(ShadowBorder, other.ShadowBorder) &&
                   Mathf.Approximately(ShadowStrength, other.ShadowStrength);
        }
        
        /// <summary>
        /// Interpolación lineal entre dos propiedades de iluminación
        /// </summary>
        /// <param name="from">Propiedades de origen</param>
        /// <param name="to">Propiedades de destino</param>
        /// <param name="t">Factor de interpolación (0-1)</param>
        /// <returns>Propiedades interpoladas</returns>
        public static IlluminationProperties Lerp(IlluminationProperties from, IlluminationProperties to, float t)
        {
            if (from == null || to == null)
                throw new ArgumentNullException("Las propiedades de origen y destino no pueden ser null");
            
            t = Mathf.Clamp01(t);
            
            return new IlluminationProperties(
                Mathf.Lerp(from.AsUnlit, to.AsUnlit, t),
                Mathf.Lerp(from.LightMaxLimit, to.LightMaxLimit, t),
                Mathf.Lerp(from.ShadowBorder, to.ShadowBorder, t),
                Mathf.Lerp(from.ShadowStrength, to.ShadowStrength, t)
            );
        }
        
        /// <summary>
        /// Representación en string
        /// </summary>
        /// <returns>String con los valores de las propiedades</returns>
        public override string ToString()
        {
            return $"IlluminationProperties(AsUnlit:{AsUnlit:F2}, LightMaxLimit:{LightMaxLimit:F2}, " +
                   $"ShadowBorder:{ShadowBorder:F2}, ShadowStrength:{ShadowStrength:F2})";
        }
    }
}