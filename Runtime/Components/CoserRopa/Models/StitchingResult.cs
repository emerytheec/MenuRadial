using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Resultado de una operacion de cosido
    /// </summary>
    public class StitchingResult
    {
        /// <summary>
        /// Indica si la operacion fue exitosa
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Cantidad de huesos cosidos exitosamente
        /// </summary>
        public int BonesStitched { get; set; }

        /// <summary>
        /// Cantidad de huesos omitidos
        /// </summary>
        public int BonesSkipped { get; set; }

        /// <summary>
        /// Cantidad de huesos no-humanoid preservados
        /// </summary>
        public int NonHumanoidBonesPreserved { get; set; }

        /// <summary>
        /// Cantidad de huesos fusionados (modo Merge)
        /// </summary>
        public int BonesMerged { get; set; }

        /// <summary>
        /// Cantidad de huesos PhysBone preservados
        /// </summary>
        public int PhysBonesPreserved { get; set; }

        /// <summary>
        /// Lista de advertencias durante el proceso
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Lista de errores durante el proceso
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public StitchingResult()
        {
            Success = true;
        }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static StitchingResult CreateSuccess(int stitched, int skipped = 0, int preserved = 0)
        {
            return new StitchingResult
            {
                Success = true,
                BonesStitched = stitched,
                BonesSkipped = skipped,
                NonHumanoidBonesPreserved = preserved
            };
        }

        /// <summary>
        /// Crea un resultado de fallo
        /// </summary>
        public static StitchingResult CreateFailure(string error)
        {
            var result = new StitchingResult
            {
                Success = false
            };
            result.Errors.Add(error);
            return result;
        }

        /// <summary>
        /// Agrega una advertencia
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// Agrega un error
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            Success = false;
        }

        /// <summary>
        /// Obtiene un resumen del resultado
        /// </summary>
        public string GetSummary()
        {
            if (!Success)
            {
                return $"Fallo: {string.Join(", ", Errors)}";
            }

            var parts = new List<string>();

            if (BonesMerged > 0)
            {
                // Modo Merge
                parts.Add($"{BonesMerged} huesos fusionados");
            }
            else
            {
                // Modo Stitch
                parts.Add($"{BonesStitched} huesos cosidos");
            }

            if (BonesSkipped > 0)
                parts.Add($"{BonesSkipped} omitidos");

            if (NonHumanoidBonesPreserved > 0)
                parts.Add($"{NonHumanoidBonesPreserved} no-humanoid preservados");

            if (PhysBonesPreserved > 0)
                parts.Add($"{PhysBonesPreserved} PhysBones preservados");

            if (Warnings.Count > 0)
                parts.Add($"{Warnings.Count} advertencias");

            return string.Join(", ", parts);
        }
    }
}
