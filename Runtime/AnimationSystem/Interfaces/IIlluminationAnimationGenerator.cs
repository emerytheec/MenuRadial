using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.AnimationSystem.Interfaces
{
    /// <summary>
    /// Interfaz para generadores de animaciones de iluminación
    /// </summary>
    public interface IIlluminationAnimationGenerator
    {
        /// <summary>
        /// Genera una animación de iluminación con keyframes predefinidos
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="keyframes">Keyframes de iluminación</param>
        /// <param name="savePath">Ruta donde guardar (opcional)</param>
        /// <param name="rootObject">Objeto raíz para limitar la búsqueda (opcional)</param>
        /// <returns>AnimationClip generado</returns>
        AnimationClip GenerateIlluminationAnimation(
            string animationName,
            List<Material> materials,
            IlluminationKeyframe[] keyframes,
            string savePath = null,
            GameObject rootObject = null
        );
        
        /// <summary>
        /// Genera una animación de iluminación con keyframes por defecto
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="materials">Lista de materiales a animar</param>
        /// <param name="savePath">Ruta donde guardar (opcional)</param>
        /// <param name="rootObject">Objeto raíz para limitar la búsqueda (opcional)</param>
        /// <returns>AnimationClip generado</returns>
        AnimationClip GenerateDefaultIlluminationAnimation(
            string animationName,
            List<Material> materials,
            string savePath = null,
            GameObject rootObject = null
        );
        
        /// <summary>
        /// Valida que los materiales sean compatibles para animación
        /// </summary>
        /// <param name="materials">Materiales a validar</param>
        /// <returns>True si todos son compatibles</returns>
        bool ValidateMaterials(List<Material> materials);
    }
}