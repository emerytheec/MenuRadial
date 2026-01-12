using System;
using System.IO;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Procesador especializado para transformaciones y cálculos de rutas de animación
    /// REFACTORIZACIÓN [2025-07-04]: Extraído de RadialPropertyManager para cumplir SRP
    /// 
    /// Responsabilidad única: Procesamiento, normalización y cálculo de rutas
    /// </summary>
    public class RadialPathProcessor
    {
        // Public Methods - Path Normalization
        
        /// <summary>
        /// Normaliza una ruta asegurando que termine con '/'
        /// </summary>
        /// <param name="path">Ruta a normalizar</param>
        /// <returns>Ruta normalizada</returns>
        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return MRConstants.ANIMATION_OUTPUT_PATH;
                
            // Reemplazar separadores de Windows con Unix
            path = path.Replace('\\', '/');
            
            // Asegurar que termine con '/'
            if (!path.EndsWith("/"))
                path += "/";
                
            return path;
        }
        
        /// <summary>
        /// Normaliza el directorio de animación
        /// </summary>
        /// <param name="animationPath">Ruta de animación a normalizar</param>
        /// <returns>Directorio normalizado</returns>
        public string GetNormalizedAnimationDirectory(string animationPath)
        {
            return NormalizePath(animationPath);
        }
        
        
        // Public Methods - Path Calculations
        
        /// <summary>
        /// Calcula la ruta completa del archivo de animación
        /// </summary>
        /// <param name="animationPath">Directorio de la animación</param>
        /// <param name="animationName">Nombre de la animación</param>
        /// <returns>Ruta completa con nombre y extensión</returns>
        public string CalculateFullAnimationPath(string animationPath, string animationName)
        {
            var directory = GetNormalizedAnimationDirectory(animationPath);
            var fileName = GetAnimationFileName(animationName);
            
            if (string.IsNullOrEmpty(fileName))
                fileName = "RadialToggle";
                
            return Path.Combine(directory, fileName + ".anim").Replace('\\', '/');
        }
        
        /// <summary>
        /// Obtiene el nombre del archivo de animación sin extensión
        /// </summary>
        /// <param name="animationName">Nombre completo de la animación</param>
        /// <returns>Nombre de archivo sin extensión</returns>
        public string GetAnimationFileName(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
                return "RadialToggle";
                
            return Path.GetFileNameWithoutExtension(animationName);
        }
        
        
        // Public Methods - Path Generation
        
        /// <summary>
        /// Genera un nombre único basado en el nombre actual
        /// </summary>
        /// <param name="baseName">Nombre base</param>
        /// <param name="suffix">Sufijo opcional (por defecto usa timestamp)</param>
        /// <returns>Nuevo nombre único</returns>
        public string GenerateUniqueName(string baseName, string suffix = null)
        {
            if (string.IsNullOrEmpty(suffix))
                suffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
            var fileName = GetAnimationFileName(baseName);
            if (string.IsNullOrEmpty(fileName))
                fileName = "RadialToggle";
                
            return $"{fileName}_{suffix}";
        }
        
        /// <summary>
        /// Crea una ruta sugerida basada en la jerarquía del GameObject
        /// </summary>
        /// <param name="gameObjectPath">Ruta del GameObject en la jerarquía</param>
        /// <returns>Ruta sugerida</returns>
        public string SuggestPathFromHierarchy(string gameObjectPath)
        {
            if (string.IsNullOrEmpty(gameObjectPath))
                return MRConstants.ANIMATION_OUTPUT_PATH;
                
            // Limpiar caracteres especiales y crear ruta válida
            var cleanPath = gameObjectPath
                .Replace('/', '_')
                .Replace(' ', '_')
                .Replace('(', '_')
                .Replace(')', '_');
                
            return $"{MRConstants.ANIMATION_OUTPUT_PATH}{cleanPath}/";
        }
        
        /// <summary>
        /// Auto-genera ruta basada en la jerarquía del GameObject
        /// </summary>
        /// <param name="gameObjectPath">Ruta del GameObject en la jerarquía</param>
        /// <param name="currentPath">Ruta actual (para comparación)</param>
        /// <returns>Nueva ruta si es diferente, null si no hay cambios</returns>
        public string AutoGeneratePathFromHierarchy(string gameObjectPath, string currentPath)
        {
            if (string.IsNullOrEmpty(gameObjectPath))
                return null;
                
            // Validación defensiva antes del procesamiento
            if (string.IsNullOrWhiteSpace(gameObjectPath))
                return null;
                
            // Construir ruta basada en la jerarquía
            var hierarchyPath = $"{MRConstants.ANIMATION_OUTPUT_PATH}{gameObjectPath.Replace('/', '_')}/";
            var normalizedHierarchyPath = NormalizePath(hierarchyPath);
            var normalizedCurrentPath = NormalizePath(currentPath);
            
            if (normalizedHierarchyPath != normalizedCurrentPath)
            {
                return normalizedHierarchyPath;
            }
            
            return null; // No hay cambios
        }
        
        
        // Public Methods - Path Parsing
        
        /// <summary>
        /// Extrae el directorio de una ruta completa
        /// </summary>
        /// <param name="fullPath">Ruta completa</param>
        /// <returns>Directorio extraído</returns>
        public string ExtractDirectoryFromFullPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return MRConstants.ANIMATION_OUTPUT_PATH;

            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
                return MRConstants.ANIMATION_OUTPUT_PATH;

            return NormalizePath(directory);
        }
        
        /// <summary>
        /// Extrae el nombre del archivo de una ruta completa
        /// </summary>
        /// <param name="fullPath">Ruta completa</param>
        /// <returns>Nombre de archivo extraído</returns>
        public string ExtractFileNameFromFullPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return "RadialToggle";
                
            var fileName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrEmpty(fileName))
                return "RadialToggle";
                
            return fileName;
        }
        
        
        // Public Methods - Path Utilities
        
        /// <summary>
        /// Combina directorio y nombre de archivo de forma segura
        /// </summary>
        /// <param name="directory">Directorio base</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <param name="extension">Extensión del archivo (opcional, por defecto .anim)</param>
        /// <returns>Ruta completa combinada</returns>
        public string CombinePathSafely(string directory, string fileName, string extension = ".anim")
        {
            var normalizedDir = NormalizePath(directory);
            var cleanFileName = GetAnimationFileName(fileName);
            
            if (string.IsNullOrEmpty(cleanFileName))
                cleanFileName = "RadialToggle";
                
            if (!extension.StartsWith("."))
                extension = "." + extension;
                
            return Path.Combine(normalizedDir, cleanFileName + extension).Replace('\\', '/');
        }
        
        /// <summary>
        /// Verifica si una ruta tiene el formato correcto para Unity
        /// </summary>
        /// <param name="path">Ruta a verificar</param>
        /// <returns>True si tiene formato correcto</returns>
        public bool IsValidUnityPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            // Debe empezar con Assets/
            if (!path.StartsWith("Assets/"))
                return false;
                
            // Validación defensiva de caracteres inválidos
            var invalidChars = Path.GetInvalidPathChars();
            if (invalidChars != null)
            {
                foreach (char c in path)
                {
                    if (Array.IndexOf(invalidChars, c) >= 0)
                        return false;
                }
            }
            
            return true;
        }
        
    }
}
