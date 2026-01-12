using System;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Editor.Validation
{
    /// <summary>
    /// Resultado de validación específico para assets de Unity
    /// Extiende ValidationResult con información específica de assets
    /// </summary>
    public class AssetValidationResult : ValidationResult
    {
        
        /// <summary>
        /// Ruta del asset validado
        /// </summary>
        public string AssetPath { get; }
        
        /// <summary>
        /// Tipo esperado del asset
        /// </summary>
        public Type ExpectedType { get; }
        
        /// <summary>
        /// Indica si el asset existe en el sistema de archivos
        /// </summary>
        public bool AssetExists { get; }
        
        /// <summary>
        /// Indica si el asset se puede cargar como el tipo esperado
        /// </summary>
        public bool CanLoadAsType { get; }
        
        
        
        /// <summary>
        /// Constructor para validación exitosa
        /// </summary>
        /// <param name="isValid">Si la validación fue exitosa</param>
        /// <param name="assetPath">Ruta del asset</param>
        /// <param name="expectedType">Tipo esperado</param>
        public AssetValidationResult(bool isValid, string assetPath, Type expectedType) 
            : base($"Asset '{assetPath}' validado como {expectedType?.Name}", isValid)
        {
            AssetPath = assetPath;
            ExpectedType = expectedType;
            AssetExists = isValid;
            CanLoadAsType = isValid;
        }
        
        /// <summary>
        /// Constructor para validación con error
        /// </summary>
        /// <param name="isValid">Si la validación fue exitosa</param>
        /// <param name="assetPath">Ruta del asset</param>
        /// <param name="expectedType">Tipo esperado</param>
        /// <param name="errorMessage">Mensaje de error</param>
        public AssetValidationResult(bool isValid, string assetPath, Type expectedType, string errorMessage) 
            : base(errorMessage, isValid, ValidationSeverity.Error)
        {
            AssetPath = assetPath;
            ExpectedType = expectedType;
            AssetExists = false;
            CanLoadAsType = false;
        }
        
        /// <summary>
        /// Constructor detallado para casos complejos
        /// </summary>
        /// <param name="isValid">Si la validación fue exitosa</param>
        /// <param name="assetPath">Ruta del asset</param>
        /// <param name="expectedType">Tipo esperado</param>
        /// <param name="assetExists">Si el asset existe</param>
        /// <param name="canLoadAsType">Si se puede cargar como el tipo esperado</param>
        /// <param name="message">Mensaje personalizado</param>
        public AssetValidationResult(bool isValid, string assetPath, Type expectedType, 
                                   bool assetExists, bool canLoadAsType, string message = null) 
            : base(message ?? $"Asset '{assetPath}': exists={assetExists}, loadable={canLoadAsType}", 
                  isValid, isValid ? ValidationSeverity.Info : ValidationSeverity.Error)
        {
            AssetPath = assetPath;
            ExpectedType = expectedType;
            AssetExists = assetExists;
            CanLoadAsType = canLoadAsType;
        }
        
        
        
        /// <summary>
        /// Obtiene un resumen detallado de la validación
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
            {
                return $"✅ Asset válido: '{AssetPath}' como {ExpectedType?.Name}";
            }
            else
            {
                return $"❌ Asset inválido: '{AssetPath}' - {Message}";
            }
        }
        
        /// <summary>
        /// Crea un resultado de validación exitosa
        /// </summary>
        public static AssetValidationResult Success(string assetPath, Type expectedType)
        {
            return new AssetValidationResult(true, assetPath, expectedType);
        }
        
        /// <summary>
        /// Crea un resultado de validación fallida
        /// </summary>
        public static AssetValidationResult Failure(string assetPath, Type expectedType, string errorMessage)
        {
            return new AssetValidationResult(false, assetPath, expectedType, errorMessage);
        }
        
    }
}
