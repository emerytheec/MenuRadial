using System;
using System.IO;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Validador especializado para propiedades de animación radial
    /// REFACTORIZACIÓN [2025-07-04]: Extraído de RadialPropertyManager para cumplir SRP
    /// 
    /// Responsabilidad única: Validación de nombres y rutas de animación
    /// </summary>
    public class RadialPropertyValidator
    {
        // Private Fields
        
        private readonly string _componentName;
        
        
        // Constructor
        
        /// <summary>
        /// Inicializa el validador con el nombre del componente para logging
        /// </summary>
        /// <param name="componentName">Nombre del componente para logging</param>
        public RadialPropertyValidator(string componentName)
        {
            _componentName = componentName ?? "Unknown";
        }
        
        
        // Public Methods - Animation Name Validation
        
        /// <summary>
        /// Valida el nombre de la animación
        /// </summary>
        /// <param name="name">Nombre a validar</param>
        /// <returns>True si es válido</returns>
        public bool ValidateAnimationName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            
            // Verificar caracteres inválidos para nombres de archivo
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    return false;
                }
            }
            
            // Verificar longitud razonable
            if (name.Length > 100)
            {
                return false;
            }
            
            return true;
        }
        
        
        // Public Methods - Animation Path Validation
        
        /// <summary>
        /// Valida la ruta de la animación
        /// </summary>
        /// <param name="path">Ruta a validar</param>
        /// <returns>True si es válida</returns>
        public bool ValidateAnimationPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            
            // Debe empezar con Assets/ para Unity
            if (!path.StartsWith("Assets/"))
            {
                return false;
            }
            
            // Verificar caracteres inválidos para rutas
            var invalidChars = Path.GetInvalidPathChars();
            foreach (char c in path)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    return false;
                }
            }
            
            // Verificar longitud razonable
            if (path.Length > 200)
            {
                return false;
            }
            
            return true;
        }
        
        
        // Public Methods - Comprehensive Validation
        
        /// <summary>
        /// Valida tanto el nombre como la ruta de animación
        /// </summary>
        /// <param name="animationName">Nombre a validar</param>
        /// <param name="animationPath">Ruta a validar</param>
        /// <returns>True si ambos son válidos</returns>
        public bool ValidateAllProperties(string animationName, string animationPath)
        {
            var nameValid = ValidateAnimationName(animationName);
            var pathValid = ValidateAnimationPath(animationPath);
            
            if (nameValid && pathValid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        
    }
}
