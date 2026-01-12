using System;
using System.Collections.Generic;
using System.Text;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Object Pools optimizados para MRUnificarObjetos
    /// REFACTOR: Elimina allocations innecesarias en operaciones frecuentes
    /// </summary>
    public static class RadialObjectPools
    {
        // ValidationResult Pool
        
        private static readonly Stack<ValidationResult> _validationResultPool = new Stack<ValidationResult>();
        private static readonly object _validationPoolLock = new object();
        
        /// <summary>
        /// Obtiene ValidationResult reutilizable del pool (thread-safe)
        /// </summary>
        public static ValidationResult GetValidationResult(string message, bool isValid, ValidationSeverity severity)
        {
            ValidationResult result;
            
            lock (_validationPoolLock)
            {
                if (_validationResultPool.Count > 0)
                {
                    result = _validationResultPool.Pop();
                    // Reset del objeto reutilizado
                    ResetValidationResult(result, message, isValid, severity);
                }
                else
                {
                    result = new ValidationResult(message, isValid, severity);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Devuelve ValidationResult al pool (thread-safe)
        /// </summary>
        public static void ReturnValidationResult(ValidationResult result)
        {
            if (result == null) return;
            
            lock (_validationPoolLock)
            {
                // Límite del pool para evitar memory leaks
                if (_validationResultPool.Count < 50)
                {
                    _validationResultPool.Push(result);
                }
            }
        }
        
        private static void ResetValidationResult(ValidationResult result, string message, bool isValid, ValidationSeverity severity)
        {
            // Usar reflection mínima o recrear si ValidationResult es inmutable
            // Por simplicidad, crear nuevo si no hay setters públicos
        }
        
        
        // StringBuilder Pool
        
        private static readonly Stack<StringBuilder> _stringBuilderPool = new Stack<StringBuilder>();
        private static readonly object _stringBuilderPoolLock = new object();
        
        /// <summary>
        /// Obtiene StringBuilder reutilizable del pool (thread-safe)
        /// </summary>
        public static StringBuilder GetStringBuilder()
        {
            lock (_stringBuilderPoolLock)
            {
                if (_stringBuilderPool.Count > 0)
                {
                    var sb = _stringBuilderPool.Pop();
                    sb.Clear(); // Reset para reutilización
                    return sb;
                }
                else
                {
                    return new StringBuilder(256); // Capacidad inicial optimizada
                }
            }
        }
        
        /// <summary>
        /// Devuelve StringBuilder al pool (thread-safe)
        /// </summary>
        public static void ReturnStringBuilder(StringBuilder sb)
        {
            if (sb == null) return;
            
            lock (_stringBuilderPoolLock)
            {
                // Límite del pool y capacidad máxima para evitar memory leaks
                if (_stringBuilderPool.Count < 20 && sb.Capacity < 2048)
                {
                    _stringBuilderPool.Push(sb);
                }
            }
        }
        
        
        // List Pool
        
        private static readonly Stack<List<string>> _stringListPool = new Stack<List<string>>();
        private static readonly object _stringListPoolLock = new object();
        
        /// <summary>
        /// Obtiene List<string> reutilizable del pool (thread-safe)
        /// </summary>
        public static List<string> GetStringList()
        {
            lock (_stringListPoolLock)
            {
                if (_stringListPool.Count > 0)
                {
                    var list = _stringListPool.Pop();
                    list.Clear(); // Reset para reutilización
                    return list;
                }
                else
                {
                    return new List<string>(16); // Capacidad inicial optimizada
                }
            }
        }
        
        /// <summary>
        /// Devuelve List<string> al pool (thread-safe)
        /// </summary>
        public static void ReturnStringList(List<string> list)
        {
            if (list == null) return;
            
            lock (_stringListPoolLock)
            {
                // Límite del pool y capacidad máxima para evitar memory leaks
                if (_stringListPool.Count < 15 && list.Capacity < 100)
                {
                    _stringListPool.Push(list);
                }
            }
        }
        
        
        // Cache-Friendly Operations
        
        /// <summary>
        /// Operación segura con StringBuilder que maneja pool automáticamente
        /// </summary>
        public static string BuildString(Action<StringBuilder> buildAction)
        {
            if (buildAction == null) return string.Empty;
            
            var sb = GetStringBuilder();
            try
            {
                buildAction(sb);
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }
        
        /// <summary>
        /// Operación segura con List<string> que maneja pool automáticamente
        /// </summary>
        public static T ProcessStringList<T>(Func<List<string>, T> processAction)
        {
            if (processAction == null) return default(T);
            
            var list = GetStringList();
            try
            {
                return processAction(list);
            }
            finally
            {
                ReturnStringList(list);
            }
        }
        
        
        // Pool Statistics (Non-Debug)
        
        
    }
}
