using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz para componentes de iluminación radial
    /// </summary>
    public interface IIlluminationComponent
    {
        /// <summary>
        /// Objeto raíz desde donde buscar materiales
        /// </summary>
        GameObject RootObject { get; set; }
        
        /// <summary>
        /// Lista de materiales detectados y compatibles
        /// </summary>
        List<Material> DetectedMaterials { get; }
        
        /// <summary>
        /// Nombre de la animación a generar
        /// </summary>
        string AnimationName { get; set; }
        
        /// <summary>
        /// Ruta donde guardar la animación
        /// </summary>
        string AnimationPath { get; set; }
        
        /// <summary>
        /// Escanea materiales compatibles desde el objeto raíz
        /// </summary>
        void ScanMaterials();
        
        /// <summary>
        /// Aplica los valores de iluminación actuales a todos los materiales
        /// </summary>
        void ApplyValuesToAllMaterials();
        
        /// <summary>
        /// Genera la animación de iluminación
        /// </summary>
        /// <returns>AnimationClip generado</returns>
        AnimationClip GenerateIlluminationAnimation();
        
        /// <summary>
        /// Limpia la lista de materiales detectados
        /// </summary>
        void ClearDetectedMaterials();
    }
}