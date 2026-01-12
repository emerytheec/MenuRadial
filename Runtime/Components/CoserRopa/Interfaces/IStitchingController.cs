using System.Collections.Generic;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Interfaces
{
    /// <summary>
    /// Interfaz para controladores de cosido
    /// </summary>
    public interface IStitchingController
    {
        /// <summary>
        /// Ejecuta el proceso de cosido
        /// </summary>
        /// <param name="mappings">Lista de mapeos de huesos a coser</param>
        /// <returns>Resultado de la operacion de cosido</returns>
        StitchingResult ExecuteStitching(List<BoneMapping> mappings);

        /// <summary>
        /// Valida si el cosido puede realizarse
        /// </summary>
        /// <param name="avatar">Referencia al avatar</param>
        /// <param name="clothing">Referencia a la ropa</param>
        /// <returns>Resultado de la validacion</returns>
        ValidationResult ValidateForStitching(ArmatureReference avatar, ArmatureReference clothing);

        /// <summary>
        /// Deshace la ultima operacion de cosido
        /// </summary>
        /// <returns>True si se pudo deshacer, false en caso contrario</returns>
        bool UndoStitching();
    }
}
