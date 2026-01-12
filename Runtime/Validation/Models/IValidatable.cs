namespace Bender_Dios.MenuRadial.Validation.Models
{
    /// <summary>
    /// Interfaz para componentes que pueden ser validados
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// Valida el estado actual del objeto
        /// </summary>
        /// <returns>Resultado de la validación</returns>
        ValidationResult Validate();
    }
}