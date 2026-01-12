using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Interfaz base para componentes que manejan frames de animación
    /// </summary>
    public interface IFrameComponent
    {
        /// <summary>
        /// Lista de frames contenidos en el componente
        /// </summary>
        List<IFrameData> Frames { get; }
        
        /// <summary>
        /// Índice del frame actualmente activo
        /// </summary>
        int ActiveFrameIndex { get; set; }
        
        /// <summary>
        /// Selecciona el siguiente frame en la secuencia
        /// </summary>
        void SelectNextFrame();
        
        /// <summary>
        /// Selecciona el frame anterior en la secuencia
        /// </summary>
        void SelectPreviousFrame();
        
        /// <summary>
        /// Selecciona un frame específico por su índice
        /// </summary>
        /// <param name="index">Índice del frame a seleccionar</param>
        void SelectFrameByIndex(int index);
        
        /// <summary>
        /// Aplica el estado del frame activo
        /// </summary>
        void ApplyCurrentFrame();
    }
}