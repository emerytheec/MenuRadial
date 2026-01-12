using System;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Validation
{
    /// <summary>
    /// Helper para gestión de cache de validación.
    /// Proporciona un patrón reutilizable para cache con invalidación basada en hash.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto que se valida</typeparam>
    public class ValidationCacheHelper<T> where T : class
    {
        private ValidationResult _cachedResult;
        private int _lastHash;
        private bool _isDirty = true;

        /// <summary>
        /// Función para calcular el hash del estado actual
        /// </summary>
        private readonly Func<T, int> _hashCalculator;

        /// <summary>
        /// Función para realizar la validación real
        /// </summary>
        private readonly Func<T, ValidationResult> _validator;

        /// <summary>
        /// Constructor con funciones de hash y validación
        /// </summary>
        /// <param name="hashCalculator">Función que calcula el hash del estado actual</param>
        /// <param name="validator">Función que realiza la validación real</param>
        public ValidationCacheHelper(Func<T, int> hashCalculator, Func<T, ValidationResult> validator)
        {
            _hashCalculator = hashCalculator ?? throw new ArgumentNullException(nameof(hashCalculator));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Indica si el cache está sucio y necesita revalidación
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Último resultado de validación cacheado
        /// </summary>
        public ValidationResult CachedResult => _cachedResult;

        /// <summary>
        /// Obtiene el resultado de validación, usando cache si es válido
        /// </summary>
        /// <param name="target">Objeto a validar</param>
        /// <returns>Resultado de validación</returns>
        public ValidationResult Validate(T target)
        {
            if (target == null)
            {
                return ValidationResult.Error("El objeto a validar es null");
            }

            int currentHash = _hashCalculator(target);

            // Si el cache es válido y el hash no ha cambiado, retornar cache
            if (!_isDirty && _lastHash == currentHash && _cachedResult != null)
            {
                return _cachedResult;
            }

            // Ejecutar validación real
            _cachedResult = _validator(target);
            _lastHash = currentHash;
            _isDirty = false;

            return _cachedResult;
        }

        /// <summary>
        /// Invalida el cache forzando una nueva validación en la próxima llamada
        /// </summary>
        public void Invalidate()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Limpia completamente el cache
        /// </summary>
        public void Clear()
        {
            _cachedResult = null;
            _lastHash = 0;
            _isDirty = true;
        }

        /// <summary>
        /// Verifica si el cache es válido para el estado actual
        /// </summary>
        /// <param name="target">Objeto a verificar</param>
        /// <returns>True si el cache es válido</returns>
        public bool IsCacheValid(T target)
        {
            if (target == null || _isDirty || _cachedResult == null)
            {
                return false;
            }

            int currentHash = _hashCalculator(target);
            return _lastHash == currentHash;
        }
    }

    /// <summary>
    /// Versión simplificada del helper de cache sin tipo genérico.
    /// Para casos donde se necesita cache simple sin hash.
    /// </summary>
    public class SimpleValidationCache
    {
        private ValidationResult _cachedResult;
        private bool _isValid;

        /// <summary>
        /// Indica si hay un resultado cacheado válido
        /// </summary>
        public bool HasValidCache => _isValid && _cachedResult != null;

        /// <summary>
        /// Resultado cacheado actual
        /// </summary>
        public ValidationResult CachedResult => _cachedResult;

        /// <summary>
        /// Obtiene el resultado cacheado o ejecuta la validación
        /// </summary>
        /// <param name="validator">Función de validación a ejecutar si no hay cache</param>
        /// <returns>Resultado de validación</returns>
        public ValidationResult GetOrValidate(Func<ValidationResult> validator)
        {
            if (_isValid && _cachedResult != null)
            {
                return _cachedResult;
            }

            _cachedResult = validator();
            _isValid = true;

            return _cachedResult;
        }

        /// <summary>
        /// Establece el resultado cacheado directamente
        /// </summary>
        /// <param name="result">Resultado a cachear</param>
        public void SetCache(ValidationResult result)
        {
            _cachedResult = result;
            _isValid = true;
        }

        /// <summary>
        /// Invalida el cache
        /// </summary>
        public void Invalidate()
        {
            _isValid = false;
        }

        /// <summary>
        /// Limpia el cache completamente
        /// </summary>
        public void Clear()
        {
            _cachedResult = null;
            _isValid = false;
        }
    }
}
