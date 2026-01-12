using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz base genérica para todas las referencias del sistema Frame
    /// Unifica comportamiento común y elimina duplicación de código
    /// </summary>
    /// <typeparam name="T">Tipo del objeto referenciado (GameObject, Renderer, etc.)</typeparam>
    public interface IReferenceBase<T> where T : UnityEngine.Object
    {
        /// <summary>
        /// Objeto objetivo de la referencia
        /// </summary>
        T Target { get; set; }
        
        /// <summary>
        /// Indica si la referencia es válida (objeto existe y es utilizable)
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        /// Ruta jerárquica del objeto  y recuperación
        /// </summary>
        string HierarchyPath { get; }
        
        /// <summary>
        /// Actualiza la ruta jerárquica del objeto
        /// </summary>
        void UpdateHierarchyPath();
        
        /// <summary>
        /// Aplica el estado/configuración de esta referencia
        /// </summary>
        void Apply();
        
        /// <summary>
        /// Captura el estado actual del objeto en la escena
        /// </summary>
        void CaptureCurrentState();
    }
}
