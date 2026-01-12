using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Utils;
using Bender_Dios.MenuRadial.Core.Preview;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Gestor especializado para la administración de frames en menús radiales
    /// REFACTORIZADO: Extraído de MRUnificarObjetos.cs para responsabilidad única
    /// 
    /// Responsabilidades:
    /// - CRUD de frames (Add, Remove, Cleanup)
    /// - Validación de lista de frames
    /// - Cálculo de ActiveFrame según tipo de animación
    /// - Gestión de índices para diferentes tipos (ON/OFF vs Linear)
    /// </summary>
    public class RadialFrameManager
    {
        // Campos privados
        private readonly List<MRAgruparObjetos> _frames;
        private int _activeFrameIndex;
        
        // Cache para optimizaciones - NUEVO [2025-07-04]
        private int _cachedValidFrameCount = -1;
        private int _lastFrameListHash = 0;
        
        // Constructor
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frames">Lista de frames a gestionar</param>
        /// <param name="initialActiveIndex">Índice activo inicial</param>
        public RadialFrameManager(List<MRAgruparObjetos> frames, int initialActiveIndex = 0)
        {
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            _activeFrameIndex = initialActiveIndex;
            
        }
        
        // Propiedades públicas
        /// <summary>
        /// Número de frames válidos (no nulos)
        /// OPTIMIZADO [2025-07-04]: Cache para evitar recálculos en cada acceso
        /// </summary>
        public int FrameCount 
        { 
            get 
            {
                var currentHash = GetFrameListHash();
                if (_lastFrameListHash != currentHash)
                {
                    _cachedValidFrameCount = _frames?.CountNonNull() ?? 0;
                    _lastFrameListHash = currentHash;
                }
                return _cachedValidFrameCount;
            }
        }
        
        /// <summary>
        /// Lista de frames para acceso de solo lectura
        /// </summary>
        public IReadOnlyList<MRAgruparObjetos> Frames => _frames.AsReadOnly();
        
        /// <summary>
        /// Índice del frame activo actual
        /// </summary>
        public int ActiveFrameIndex 
        { 
            get => _activeFrameIndex;
            set => SetActiveFrameIndex(value);
        }
        
        /// <summary>
        /// Frame activo actual considerando el tipo de animación
        /// </summary>
        public MRAgruparObjetos ActiveFrame 
        {
            get
            {
                // LÓGICA ESPECÍFICA: Para animaciones On/Off, siempre devolver el primer frame si existe
                if (FrameCount == 1)
                {
                    // On/Off: siempre usar el frame 0, independientemente del ActiveFrameIndex conceptual
                    return _frames[0];
                }
                
                // Múltiples frames: lógica normal de índice
                return HasValidActiveFrame() ? _frames[_activeFrameIndex] : null;
            }
        }
        
        /// <summary>
        /// Tipo de animación basado en el número de frames
        /// </summary>
        public AnimationType AnimationType
        {
            get
            {
                return FrameCount switch
                {
                    0 => AnimationType.None,
                    1 => AnimationType.OnOff,
                    2 => AnimationType.AB,
                    _ => AnimationType.Linear
                };
            }
        }
        
        // Métodos públicos - Operaciones CRUD
        
        /// <summary>
        /// Añade un frame a la lista si no existe
        /// </summary>
        /// <param name="frameObject">Frame a añadir</param>
        /// <returns>True si se añadió, False si ya existía o era nulo</returns>
        public bool AddFrame(MRAgruparObjetos frameObject)
        {
            if (frameObject == null)
            {
                return false;
            }
            
            if (_frames.Contains(frameObject))
            {
                return false;
            }
            
            _frames.Add(frameObject);
            InvalidateCache(); // Cache invalidation
            
            // Validar índice activo después de añadir
            ValidateActiveFrameIndex();
            
            return true;
        }
        
        /// <summary>
        /// Remueve un frame de la lista
        /// </summary>
        /// <param name="frameObject">Frame a remover</param>
        /// <returns>True si se removió, False si no existía</returns>
        public bool RemoveFrame(MRAgruparObjetos frameObject)
        {
            if (frameObject == null)
            {
                return false;
            }
            
            bool removed = _frames.Remove(frameObject);
            
            if (removed)
            {
                // Limpiar PreviewManager registration
                PreviewManager.UnregisterComponent(frameObject);
                
                // Limpiar el frame si implementa IDisposable
                if (frameObject is System.IDisposable disposableFrame)
                {
                    disposableFrame.Dispose();
                }
                
                InvalidateCache(); // Cache invalidation
                
                // Ajustar índice activo si es necesario
                if (_activeFrameIndex >= FrameCount)
                {
                    _activeFrameIndex = Math.Max(0, FrameCount - 1);
                }
            }
            else
            {
            }
            
            return removed;
        }
        
        /// <summary>
        /// Remueve un frame por índice
        /// </summary>
        /// <param name="index">Índice del frame a remover</param>
        /// <returns>True si se removió, False si el índice era inválido</returns>
        public bool RemoveFrameAt(int index)
        {
            if (index < 0 || index >= _frames.Count)
            {
                return false;
            }
            
            var frameObject = _frames[index];
            
            // Limpiar PreviewManager registration
            if (frameObject != null)
            {
                PreviewManager.UnregisterComponent(frameObject);
                
                // Limpiar el frame si implementa IDisposable
                if (frameObject is System.IDisposable disposableFrame)
                {
                    disposableFrame.Dispose();
                }
            }
            
            _frames.RemoveAt(index);
            
            
            // Ajustar índice activo si es necesario
            if (_activeFrameIndex >= FrameCount)
            {
                _activeFrameIndex = Math.Max(0, FrameCount - 1);
            }
            
            return true;
        }
        
        /// <summary>
        /// Limpia todos los frames nulos de la lista
        /// </summary>
        /// <returns>Número de frames nulos removidos</returns>
        public int CleanupInvalidFrames()
        {
            int initialCount = _frames.Count;
            int removedCount = _frames.RemoveAll(f => f == null);
            
            if (removedCount > 0)
            {
                
                // Validar índice activo después de la limpieza
                ValidateActiveFrameIndex();
            }
            
            return removedCount;
        }
        
        /// <summary>
        /// Limpia todos los frames de la lista
        /// </summary>
        public void ClearAllFrames()
        {
            // Limpiar PreviewManager registrations y dispose de todos los frames
            foreach (var frame in _frames.Where(f => f != null))
            {
                PreviewManager.UnregisterComponent(frame);
                
                if (frame is System.IDisposable disposableFrame)
                {
                    disposableFrame.Dispose();
                }
            }
            
            int count = _frames.Count;
            _frames.Clear();
            _activeFrameIndex = 0;
            
        }
        
        // Métodos públicos - Navegación
        
        /// <summary>
        /// Selecciona el siguiente frame en la secuencia
        /// </summary>
        public void SelectNextFrame()
        {
            if (FrameCount == 0)
            {
                return;
            }
            
            int newIndex = (_activeFrameIndex + 1) % FrameCount;
            SetActiveFrameIndex(newIndex);
        }
        
        /// <summary>
        /// Selecciona el frame anterior en la secuencia
        /// </summary>
        public void SelectPreviousFrame()
        {
            if (FrameCount == 0)
            {
                return;
            }
            
            int newIndex = (_activeFrameIndex - 1 + FrameCount) % FrameCount;
            SetActiveFrameIndex(newIndex);
        }
        
        /// <summary>
        /// Selecciona un frame específico por índice
        /// </summary>
        /// <param name="index">Índice del frame a seleccionar</param>
        public void SelectFrameByIndex(int index)
        {
            SetActiveFrameIndex(index);
        }
        
        /// <summary>
        /// Aplica el frame activo actual
        /// </summary>
        public void ApplyCurrentFrame()
        {
            // LÓGICA ESPECÍFICA: Manejar correctamente animaciones ON/OFF (1 frame)
            if (FrameCount == 1)
            {
                // ON/OFF: siempre aplicar el frame 0, independientemente del ActiveFrameIndex conceptual
                if (_frames[0] != null)
                {
                    _frames[0].ApplyCurrentFrame();
                }
                else
                {
                }
            }
            else if (HasValidActiveFrame())
            {
                // Múltiples frames: lógica normal
                _frames[_activeFrameIndex].ApplyCurrentFrame();
            }
            else
            {
            }
        }
        
        // Métodos públicos - Validación e información
        
        /// <summary>
        /// Verifica si hay frames válidos para trabajar
        /// </summary>
        /// <returns>True si hay al menos un frame válido</returns>
        public bool HasValidFrames()
        {
            return FrameCount > 0;
        }
        
        /// <summary>
        /// Verifica si el índice activo actual es válido
        /// </summary>
        /// <returns>True si el índice activo es válido</returns>
        public bool HasValidActiveFrame()
        {
            // LÓGICA ESPECÍFICA: Para animaciones On/Off (1 frame), ActiveFrameIndex puede ser 0 o 1
            if (FrameCount == 1)
            {
                // On/Off: index 0 = OFF, index 1 = ON, pero solo hay 1 frame real (index 0)
                return _activeFrameIndex >= 0 && _activeFrameIndex <= 1 && _frames[0] != null;
            }
            
            // Múltiples frames: validación normal
            return _activeFrameIndex >= 0 && _activeFrameIndex < FrameCount && 
                   _frames[_activeFrameIndex] != null;
        }
        
        
        // Métodos privados
        
        /// <summary>
        /// Establece el índice activo con validación específica por tipo de animación
        /// </summary>
        /// <param name="index">Nuevo índice activo</param>
        private void SetActiveFrameIndex(int index)
        {
            // LÓGICA ESPECÍFICA: Para animaciones On/Off (1 frame), permitir valores 0 y 1
            if (FrameCount == 1)
            {
                // On/Off: permitir 0 (OFF) y 1 (ON)
                if (index >= 0 && index <= 1)
                {
                    _activeFrameIndex = index;
                }
                else
                {
                }
            }
            else
            {
                // Múltiples frames: validación normal
                if (index >= 0 && index < FrameCount)
                {
                    _activeFrameIndex = index;
                }
                else
                {
                }
            }
        }
        
        /// <summary>
        /// Valida y corrige el índice activo después de cambios en la lista
        /// </summary>
        private void ValidateActiveFrameIndex()
        {
            if (FrameCount == 0)
            {
                _activeFrameIndex = 0;
                return;
            }
            
            // Para ON/OFF, mantener el índice conceptual si es válido (0 o 1)
            if (FrameCount == 1)
            {
                if (_activeFrameIndex < 0 || _activeFrameIndex > 1)
                {
                    _activeFrameIndex = 0;
                }
                return;
            }
            
            // Para múltiples frames, ajustar al rango válido
            if (_activeFrameIndex >= FrameCount)
            {
                _activeFrameIndex = FrameCount - 1;
            }
            else if (_activeFrameIndex < 0)
            {
                _activeFrameIndex = 0;
            }
        }
        
        // Métodos privados de optimización
        
        /// <summary>
        /// Calcula hash simple de la lista de frames para detectar cambios
        /// </summary>
        private int GetFrameListHash()
        {
            if (_frames == null) return 0;
            
            unchecked
            {
                int hash = _frames.Count.GetHashCode();
                
                // Hash basado en referencias de objetos (detecta add/remove)
                for (int i = 0; i < _frames.Count; i++)
                {
                    hash = hash * 31 + (_frames[i]?.GetHashCode() ?? 0);
                }
                
                return hash;
            }
        }
        
        /// <summary>
        /// Invalida cache cuando se modifican frames
        /// </summary>
        private void InvalidateCache()
        {
            _lastFrameListHash = 0;
            _cachedValidFrameCount = -1;
        }
        
        // Métodos públicos - Compatibilidad
        
        /// <summary>
        /// Obtiene la lista de IFrameData para compatibilidad con interfaces
        /// OPTIMIZADO [2025-07-04]: Sin listas temporales LINQ
        /// </summary>
        /// <returns>Lista de IFrameData de todos los frames válidos</returns>
        public List<IFrameData> GetFrameDataList()
        {
            var frameDataList = ListPools.FrameObjects.Get();
            try
            {
                if (_frames != null)
                {
                    _frames.FilterNonNullTo(frameDataList);
                }
                
                // Convertir a IFrameData usando lista temporal
                var result = new List<IFrameData>();
                foreach (var frame in frameDataList)
                {
                    if (frame.Frames != null)
                    {
                        result.AddRange(frame.Frames);
                    }
                }
                
                return result;
            }
            finally
            {
                ListPools.FrameObjects.Return(frameDataList);
            }
        }
        
        /// <summary>
        /// Obtiene descripción del tipo de animación 
        /// </summary>
        /// <returns>Descripción legible del tipo de animación</returns>
        public string GetAnimationTypeDescription()
        {
            return AnimationType switch
            {
                AnimationType.None => $"Sin frames válidos ({FrameCount})",
                AnimationType.OnOff => $"Animación ON/OFF ({FrameCount} frame) - Parámetro Bool",
                AnimationType.AB => $"Animación A/B ({FrameCount} frames) - Parámetro Bool",
                AnimationType.Linear => $"Animación Lineal ({FrameCount} frames) - Parámetro Float",
                _ => $"Tipo desconocido ({FrameCount} frames)"
            };
        }
    }
}
