using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Configuración de animación para menús radiales
    /// Maneja la especificación de 255 frames con división automática
    /// </summary>
    [Serializable]
    public class RadialAnimationSettings
    {
        [Header("Configuración de Animación")]
        [SerializeField] private string _animationName = "RadialToggle";
        [SerializeField] private string _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        
        [Header("Información de Duración (Solo Lectura)")]
        [SerializeField, HideInInspector] private int _frameCount = 0;
        
        // Constantes del sistema de animación según especificación
        private const int TOTAL_FRAMES = 255;
        private const float FRAME_DURATION = 0.0166667f; // 60 FPS
        
        // Public Properties
        
        /// <summary>
        /// Nombre de la animación generada
        /// </summary>
        public string AnimationName 
        { 
            get => _animationName; 
            set => _animationName = !string.IsNullOrEmpty(value) ? value : "RadialToggle"; 
        }
        
        /// <summary>
        /// Ruta donde se guardará la animación generada
        /// </summary>
        public string AnimationPath 
        { 
            get => _animationPath; 
            set => _animationPath = !string.IsNullOrEmpty(value) ? value : MRConstants.ANIMATION_OUTPUT_PATH; 
        }
        
        /// <summary>
        /// Número total de frames de la animación (constante: 255)
        /// </summary>
        public int TotalFrames => TOTAL_FRAMES;
        
        /// <summary>
        /// Duración de cada frame en segundos (constante: 1/60)
        /// </summary>
        public float FrameDuration => FRAME_DURATION;
        
        /// <summary>
        /// Duración total de la animación en segundos
        /// </summary>
        public float TotalDuration => TotalFrames * FrameDuration;
        
        /// <summary>
        /// Número de frames/segmentos en el menú radial
        /// </summary>
        public int FrameCount => _frameCount;
        
        
        // Frame Division Logic
        
        /// <summary>
        /// Actualiza el número de frames y recalcula la división
        /// </summary>
        /// <param name="frameCount">Número de frames en el menú radial</param>
        public void UpdateFrameCount(int frameCount)
        {
            _frameCount = Mathf.Max(1, frameCount);
        }
        
        /// <summary>
        /// Calcula los puntos de división de la línea de tiempo según especificación
        /// Al dividir 255 frames en partes iguales, solo se usan valores enteros descartando decimales
        /// El último segmento será más largo para compensar los decimales descartados
        /// </summary>
        /// <returns>Array con los puntos de división (incluye 0 y el frame final)</returns>
        public int[] GetFrameDivisionPoints()
        {
            if (_frameCount <= 1)
            {
                return new int[] { 0, TOTAL_FRAMES };
            }
            
            // Calcular división básica (descartando decimales)
            int basicSegmentSize = TOTAL_FRAMES / _frameCount;
            
            var divisionPoints = new int[_frameCount + 1];
            
            // Llenar los puntos de división regulares
            for (int i = 0; i < _frameCount; i++)
            {
                divisionPoints[i] = i * basicSegmentSize;
            }
            
            // El último punto siempre es el frame final
            divisionPoints[_frameCount] = TOTAL_FRAMES;
            
            return divisionPoints;
        }
        
        /// <summary>
        /// Obtiene la información de segmentos con sus rangos
        /// CORREGIDO: Evitar solapamiento entre segmentos contiguos
        /// </summary>
        /// <returns>Array de rangos (inicio, fin) para cada segmento</returns>
        public (int start, int end)[] GetSegmentRanges()
        {
            var divisionPoints = GetFrameDivisionPoints();
            var segments = new (int start, int end)[_frameCount];
            
            for (int i = 0; i < _frameCount; i++)
            {
                // CORRECCIÓN: Hacer que cada segmento termine un frame antes del siguiente
                // para evitar solapamiento
                int startFrame = divisionPoints[i];
                int endFrame = (i == _frameCount - 1) ? divisionPoints[i + 1] : divisionPoints[i + 1] - 1;
                
                segments[i] = (startFrame, endFrame);
            }
            
            return segments;
        }
        
        /// <summary>
        /// Obtiene el tamaño de cada segmento
        /// </summary>
        /// <returns>Array con el tamaño de cada segmento</returns>
        public int[] GetSegmentSizes()
        {
            var segments = GetSegmentRanges();
            var sizes = new int[segments.Length];
            
            for (int i = 0; i < segments.Length; i++)
            {
                sizes[i] = segments[i].end - segments[i].start;
            }
            
            return sizes;
        }
        
        /// <summary>
        /// Convierte un índice de frame a tiempo en la animación
        /// </summary>
        /// <param name="frameIndex">Índice del frame</param>
        /// <returns>Tiempo en segundos</returns>
        public float FrameIndexToTime(int frameIndex)
        {
            return frameIndex * FrameDuration;
        }
        
        /// <summary>
        /// Obtiene el tiempo de inicio de un segmento específico
        /// </summary>
        /// <param name="segmentIndex">Índice del segmento</param>
        /// <returns>Tiempo de inicio en segundos</returns>
        public float GetSegmentStartTime(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _frameCount)
            {
                return 0f;
            }
            
            var divisionPoints = GetFrameDivisionPoints();
            return FrameIndexToTime(divisionPoints[segmentIndex]);
        }
        
        /// <summary>
        /// Obtiene el tiempo de fin de un segmento específico
        /// </summary>
        /// <param name="segmentIndex">Índice del segmento</param>
        /// <returns>Tiempo de fin en segundos</returns>
        public float GetSegmentEndTime(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _frameCount)
            {
                return TotalDuration;
            }
            
            var divisionPoints = GetFrameDivisionPoints();
            return FrameIndexToTime(divisionPoints[segmentIndex + 1]);
        }
        
        
        // Validation and Utilities
        
        /// <summary>
        /// Valida la configuración y corrige valores incorrectos
        /// </summary>
        /// <param name="frameCount">Número de frames para validar</param>
        public void ValidateSettings(int frameCount)
        {
            // Validar nombre de animación
            if (string.IsNullOrEmpty(_animationName))
            {
                _animationName = "RadialToggle";
            }
            
            // Validar ruta de animación
            if (string.IsNullOrEmpty(_animationPath))
            {
                _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
            }
            
            // Asegurar que la ruta termine con /
            if (!_animationPath.EndsWith("/"))
            {
                _animationPath += "/";
            }
            
            // Actualizar número de frames
            UpdateFrameCount(frameCount);
        }
        
        
        /// <summary>
        /// Ruta completa del archivo de animación
        /// </summary>
        public string GetFullAnimationPath()
        {
            return _animationPath + _animationName + ".anim";
        }
        
        
        // Constructor
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public RadialAnimationSettings()
        {
            _animationName = "RadialToggle";
            _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
            _frameCount = 1;
        }
        
        /// <summary>
        /// Constructor con configuración personalizada
        /// </summary>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="animationPath">Ruta de la animación</param>
        public RadialAnimationSettings(string animationName, string animationPath = null)
        {
            _animationName = !string.IsNullOrEmpty(animationName) ? animationName : "RadialToggle";
            _animationPath = !string.IsNullOrEmpty(animationPath) ? animationPath : MRConstants.ANIMATION_OUTPUT_PATH;
            _frameCount = 1;
            
            // Asegurar que la ruta termine con /
            if (!_animationPath.EndsWith("/"))
            {
                _animationPath += "/";
            }
        }
        
    }
}
