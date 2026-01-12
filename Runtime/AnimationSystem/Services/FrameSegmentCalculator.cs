using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.AnimationSystem.Services
{
    /// <summary>
    /// Calculadora especializada para dividir frames en segmentos según especificaciones del proyecto
    /// Implementa la lógica: división entera + último segmento más largo para compensar decimales
    /// Usa constantes centralizadas de MRAnimationConstants
    /// </summary>
    public static class FrameSegmentCalculator
    {
        // Constantes redirigidas a MRAnimationConstants para compatibilidad hacia atrás

        /// <summary>
        /// Número total de frames fijo para todas las animaciones
        /// </summary>
        public const int TOTAL_FRAMES = MRAnimationConstants.TOTAL_FRAMES;

        /// <summary>
        /// Frame rate fijo a 60 FPS
        /// </summary>
        public const float FRAME_RATE = MRAnimationConstants.FRAME_RATE;

        /// <summary>
        /// Duración de cada frame en segundos
        /// </summary>
        public const float FRAME_DURATION = MRAnimationConstants.FRAME_DURATION;

        /// <summary>
        /// Duración total de la animación en segundos
        /// </summary>
        public const float TOTAL_DURATION = MRAnimationConstants.TOTAL_DURATION;



        /// <summary>
        /// Calcula los segmentos de tiempo para división automática
        /// Implementa la lógica: división entera + último segmento más largo
        /// </summary>
        /// <param name="frameCount">Número de frames a dividir</param>
        /// <returns>Lista de segmentos calculados</returns>
        public static List<FrameSegment> CalculateSegments(int frameCount)
        {
            if (frameCount <= 0)
            {
                return new List<FrameSegment>();
            }

            var segments = new List<FrameSegment>();
            
            // Calcular tamaño base del segmento (solo enteros)
            int baseSegmentSize = TOTAL_FRAMES / frameCount;
            
            
            for (int i = 0; i < frameCount; i++)
            {
                int startFrame = i * baseSegmentSize;
                int endFrame;
                
                if (i == frameCount - 1)
                {
                    // Último segmento: hasta el frame 255 (más largo para compensar decimales)
                    endFrame = TOTAL_FRAMES;
                }
                else
                {
                    endFrame = startFrame + baseSegmentSize;
                }
                
                float startTime = startFrame * FRAME_DURATION;
                float endTime = endFrame * FRAME_DURATION;
                
                var segment = new FrameSegment
                {
                    FrameIndex = i,
                    StartFrame = startFrame,
                    EndFrame = endFrame,
                    StartTime = startTime,
                    EndTime = endTime,
                    SegmentDuration = endTime - startTime,
                    FrameCount = endFrame - startFrame
                };
                
                segments.Add(segment);
            }
            
            return segments;
        }

        /// <summary>
        /// Valida que la división de segmentos sea correcta
        /// </summary>
        /// <param name="segments">Segmentos a validar</param>
        /// <returns>True si la división es válida</returns>
        public static bool ValidateSegments(List<FrameSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return false;

            // Verificar continuidad de segmentos
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                // Verificar índice de frame
                if (segment.FrameIndex != i)
                {
                    return false;
                }
                
                // Verificar que el primer segmento comience en 0
                if (i == 0 && segment.StartFrame != 0)
                {
                    return false;
                }
                
                // Verificar continuidad entre segmentos
                if (i > 0)
                {
                    var previousSegment = segments[i - 1];
                    if (segment.StartFrame != previousSegment.EndFrame)
                    {
                        return false;
                    }
                }
                
                // Verificar que el último segmento termine en TOTAL_FRAMES
                if (i == segments.Count - 1 && segment.EndFrame != TOTAL_FRAMES)
                {
                    return false;
                }
            }
            
            return true;
        }

    }

    /// <summary>
    /// Representa un segmento de tiempo en la división de frames
    /// </summary>
    public class FrameSegment
    {
        /// <summary>
        /// Índice del frame (0-based)
        /// </summary>
        public int FrameIndex { get; set; }
        
        /// <summary>
        /// Frame de inicio del segmento
        /// </summary>
        public int StartFrame { get; set; }
        
        /// <summary>
        /// Frame de fin del segmento
        /// </summary>
        public int EndFrame { get; set; }
        
        /// <summary>
        /// Tiempo de inicio en segundos
        /// </summary>
        public float StartTime { get; set; }
        
        /// <summary>
        /// Tiempo de fin en segundos
        /// </summary>
        public float EndTime { get; set; }
        
        /// <summary>
        /// Duración del segmento en segundos
        /// </summary>
        public float SegmentDuration { get; set; }
        
        /// <summary>
        /// Número de frames en este segmento
        /// </summary>
        public int FrameCount { get; set; }

        /// <summary>
        /// Verifica si un tiempo dado está dentro de este segmento
        /// </summary>
        /// <param name="time">Tiempo a verificar</param>
        /// <returns>True si el tiempo está en el segmento</returns>
        public bool ContainsTime(float time)
        {
            return time >= StartTime && time <= EndTime;
        }

        /// <summary>
        /// Verifica si un frame dado está dentro de este segmento
        /// </summary>
        /// <param name="frame">Frame a verificar</param>
        /// <returns>True si el frame está en el segmento</returns>
        public bool ContainsFrame(int frame)
        {
            return frame >= StartFrame && frame < EndFrame;
        }

        public override string ToString()
        {
            return $"Segmento {FrameIndex}: Frames {StartFrame}-{EndFrame} ({StartTime:F3}s-{EndTime:F3}s)";
        }
    }
}
