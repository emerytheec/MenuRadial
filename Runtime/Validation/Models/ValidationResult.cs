using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Validation.Models
{
    /// <summary>
    /// Severidad de los resultados de validación
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    /// <summary>
    /// Resultado de una operación de validación
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indica si la validación fue exitosa
        /// </summary>
        public bool IsValid { get; set; } = true;
        
        /// <summary>
        /// Mensaje descriptivo del resultado
        /// </summary>
        public string Message { get; set; } = "";
        
        /// <summary>
        /// Severidad del resultado
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Info;
        
        /// <summary>
        /// Resultados de validación anidados
        /// </summary>
        public List<ValidationResult> Children { get; } = new List<ValidationResult>();
        
        /// <summary>
        /// Constructor por defecto (validación exitosa)
        /// </summary>
        public ValidationResult()
        {
        }
        
        /// <summary>
        /// Constructor con mensaje
        /// </summary>
        /// <param name="message">Mensaje del resultado</param>
        /// <param name="isValid">Estado de validez</param>
        /// <param name="severity">Severidad del resultado</param>
        public ValidationResult(string message, bool isValid = true, ValidationSeverity severity = ValidationSeverity.Info)
        {
            Message = message;
            IsValid = isValid;
            Severity = severity;
        }
        
        /// <summary>
        /// Añade un resultado hijo
        /// </summary>
        /// <param name="childResult">Resultado hijo a añadir</param>
        public void AddChild(ValidationResult childResult)
        {
            Children.Add(childResult);
            
            // Si algún hijo es inválido, este resultado también lo es
            if (!childResult.IsValid)
            {
                IsValid = false;
            }
        }
        
        /// <summary>
        /// Crea un resultado de error
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <returns>ValidationResult de error</returns>
        public static ValidationResult Error(string message)
        {
            return new ValidationResult(message, false, ValidationSeverity.Error);
        }
        
        /// <summary>
        /// Crea un resultado de advertencia
        /// </summary>
        /// <param name="message">Mensaje de advertencia</param>
        /// <returns>ValidationResult de advertencia</returns>
        public static ValidationResult Warning(string message)
        {
            return new ValidationResult(message, true, ValidationSeverity.Warning);
        }
        
        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        /// <param name="message">Mensaje de éxito</param>
        /// <returns>ValidationResult exitoso</returns>
        public static ValidationResult Success(string message = "Validación exitosa")
        {
            return new ValidationResult(message, true, ValidationSeverity.Info);
        }
        
        /// <summary>
        /// Crea un resultado informativo
        /// </summary>
        /// <param name="message">Mensaje informativo</param>
        /// <returns>ValidationResult informativo</returns>
        public static ValidationResult Info(string message)
        {
            return new ValidationResult(message, true, ValidationSeverity.Info);
        }
        
        /// <summary>
        /// Combina este resultado con otro resultado de validación
        /// </summary>
        /// <param name="other">Resultado a combinar con éste</param>
        public void MergeWith(ValidationResult other)
        {
            if (other == null) return;
            
            // Añadir todos los hijos del otro resultado
            foreach (var child in other.Children)
            {
                AddChild(child);
            }
            
            // Si el otro resultado es inválido, este también debe serlo
            if (!other.IsValid)
            {
                IsValid = false;
            }
            
            // Si el otro resultado tiene mayor severidad, actualizarla
            if (other.Severity > Severity)
            {
                Severity = other.Severity;
            }
        }
        
        /// <summary>
        /// Combina múltiples resultados de validación en uno solo
        /// </summary>
        /// <param name="results">Lista de resultados a combinar</param>
        /// <returns>Resultado combinado</returns>
        public static ValidationResult Combine(List<ValidationResult> results)
        {
            if (results == null || results.Count == 0)
                return Success("Sin validaciones");
            
            var combined = new ValidationResult("Validación combinada");
            
            foreach (var result in results)
            {
                if (result != null)
                {
                    combined.MergeWith(result);
                }
            }
            
            return combined;
        }
        
        /// <summary>
        /// Obtiene mensaje completo incluyendo todos los hijos
        /// </summary>
        /// <returns>Mensaje completo con jerarquía</returns>
        public string GetCompleteMessage()
        {
            var messages = new List<string>();
            
            if (!string.IsNullOrEmpty(Message))
                messages.Add(Message);
            
            foreach (var child in Children)
            {
                var childMessage = child.GetCompleteMessage();
                if (!string.IsNullOrEmpty(childMessage))
                    messages.Add($"  - {childMessage}");
            }
            
            return string.Join("\n", messages);
        }
    }
}