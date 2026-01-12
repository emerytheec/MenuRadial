using UnityEngine;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Shaders
{
    /// <summary>
    /// Estrategia para manejar diferentes tipos de shaders
    /// </summary>
    public interface IShaderStrategy
    {
        /// <summary>
        /// Tipo de shader que maneja esta estrategia
        /// </summary>
        ShaderType ShaderType { get; }
        
        /// <summary>
        /// Verifica si el material es compatible con esta estrategia
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        bool IsCompatible(Material material);
        
        /// <summary>
        /// Obtiene las propiedades de iluminación actuales del material
        /// </summary>
        /// <param name="material">Material del cual obtener propiedades</param>
        /// <returns>Propiedades de iluminación actuales</returns>
        IlluminationProperties GetProperties(Material material);
        
        /// <summary>
        /// Aplica las propiedades de iluminación al material
        /// </summary>
        /// <param name="material">Material al cual aplicar propiedades</param>
        /// <param name="properties">Propiedades a aplicar</param>
        void ApplyProperties(Material material, IlluminationProperties properties);
        
        /// <summary>
        /// Obtiene los nombres de las propiedades del shader
        /// </summary>
        /// <returns>Nombres de las propiedades del shader</returns>
        string[] GetPropertyNames();
        
        /// <summary>
        /// Verifica si el material tiene todas las propiedades requeridas
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si tiene todas las propiedades</returns>
        bool HasRequiredProperties(Material material);
    }
}