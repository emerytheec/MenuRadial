using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Interface para componentes que pueden mostrar previsualización en el MR Control Menu
    /// Permite activación/desactivación de estados de preview y gestión unificada
    /// </summary>
    public interface IPreviewable
    {
        /// <summary>
        /// Activa el sistema de previsualización para este componente
        /// Cada implementación define qué significa "activar preview"
        /// </summary>
        void ActivatePreview();
        
        /// <summary>
        /// Desactiva el sistema de previsualización y restaura estado base
        /// Debe limpiar cualquier estado temporal aplicado por ActivatePreview()
        /// </summary>
        void DeactivatePreview();
        
        /// <summary>
        /// Indica si el sistema de previsualización está actualmente activo
        /// </summary>
        bool IsPreviewActive { get; }
        
        /// <summary>
        /// Obtiene el tipo de previsualización que maneja este componente
        /// </summary>
        /// <returns>Tipo de preview correspondiente</returns>
        PreviewType GetPreviewType();
        
    }
}
