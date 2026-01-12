using UnityEngine;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Shaders.Strategies
{
    /// <summary>
    /// Estrategia para manejar materiales con shader lilToon
    /// </summary>
    public class LilToonShaderStrategy : IShaderStrategy
    {
        /// <summary>
        /// Nombres de las propiedades del shader lilToon
        /// </summary>
        private static class LilToonProperties
        {
            public const string AsUnlit = "_AsUnlit";
            public const string LightMaxLimit = "_LightMaxLimit";
            public const string ShadowBorder = "_ShadowBorder";
            public const string ShadowStrength = "_ShadowStrength";
        }
        
        /// <summary>
        /// Nombres de shaders lilToon conocidos
        /// </summary>
        private static readonly string[] LilToonShaderNames = 
        {
            "lilToon",
            "Hidden/lilToonOutline",
            "Hidden/lilToonCutout",
            "Hidden/lilToonTransparent",
            "Hidden/lilToonTessellation",
            "_lil/lilToon",
            "_lil/[Optional] lilToonOutline",
            "_lil/[Optional] lilToonMulti"
        };
        
        /// <summary>
        /// Tipo de shader que maneja esta estrategia
        /// </summary>
        public ShaderType ShaderType => ShaderType.LilToon;
        
        /// <summary>
        /// Verifica si el material es compatible con lilToon
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        public bool IsCompatible(Material material)
        {
            if (material == null || material.shader == null) return false;
            
            string shaderName = material.shader.name;
            
            // Verificar nombres exactos conocidos
            foreach (string knownName in LilToonShaderNames)
            {
                if (shaderName.Equals(knownName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            // Verificar si contiene "liltoon" en el nombre
            if (shaderName.ToLower().Contains("liltoon"))
                return true;
            
            // Verificar si tiene las propiedades requeridas
            return HasRequiredProperties(material);
        }
        
        /// <summary>
        /// Obtiene las propiedades de iluminación actuales del material
        /// </summary>
        /// <param name="material">Material del cual obtener propiedades</param>
        /// <returns>Propiedades de iluminación actuales</returns>
        public IlluminationProperties GetProperties(Material material)
        {
            if (material == null) return new IlluminationProperties();
            
            var properties = new IlluminationProperties();
            
            if (material.HasProperty(LilToonProperties.AsUnlit))
                properties.AsUnlit = material.GetFloat(LilToonProperties.AsUnlit);
                
            if (material.HasProperty(LilToonProperties.LightMaxLimit))
                properties.LightMaxLimit = material.GetFloat(LilToonProperties.LightMaxLimit);
                
            if (material.HasProperty(LilToonProperties.ShadowBorder))
                properties.ShadowBorder = material.GetFloat(LilToonProperties.ShadowBorder);
                
            if (material.HasProperty(LilToonProperties.ShadowStrength))
                properties.ShadowStrength = material.GetFloat(LilToonProperties.ShadowStrength);
            
            return properties;
        }
        
        /// <summary>
        /// Aplica las propiedades de iluminación al material
        /// </summary>
        /// <param name="material">Material al cual aplicar propiedades</param>
        /// <param name="properties">Propiedades a aplicar</param>
        public void ApplyProperties(Material material, IlluminationProperties properties)
        {
            if (material == null || properties == null) return;
            
            if (material.HasProperty(LilToonProperties.AsUnlit))
                material.SetFloat(LilToonProperties.AsUnlit, properties.AsUnlit);
                
            if (material.HasProperty(LilToonProperties.LightMaxLimit))
                material.SetFloat(LilToonProperties.LightMaxLimit, properties.LightMaxLimit);
                
            if (material.HasProperty(LilToonProperties.ShadowBorder))
                material.SetFloat(LilToonProperties.ShadowBorder, properties.ShadowBorder);
                
            if (material.HasProperty(LilToonProperties.ShadowStrength))
                material.SetFloat(LilToonProperties.ShadowStrength, properties.ShadowStrength);
        }
        
        /// <summary>
        /// Obtiene los nombres de las propiedades del shader
        /// </summary>
        /// <returns>Nombres de las propiedades del shader</returns>
        public string[] GetPropertyNames()
        {
            return new[]
            {
                LilToonProperties.AsUnlit,
                LilToonProperties.LightMaxLimit,
                LilToonProperties.ShadowBorder,
                LilToonProperties.ShadowStrength
            };
        }
        
        /// <summary>
        /// Verifica si el material tiene todas las propiedades requeridas
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si tiene todas las propiedades</returns>
        public bool HasRequiredProperties(Material material)
        {
            if (material == null) return false;
            
            return material.HasProperty(LilToonProperties.AsUnlit) &&
                   material.HasProperty(LilToonProperties.LightMaxLimit) &&
                   material.HasProperty(LilToonProperties.ShadowBorder) &&
                   material.HasProperty(LilToonProperties.ShadowStrength);
        }
        
    }
}