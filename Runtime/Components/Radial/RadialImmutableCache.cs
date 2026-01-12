using System;
using System.Collections.Generic;
using System.Threading;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Cache inmutable con invalidación selectiva para MRUnificarObjetos
    /// REFACTOR: Elimina recálculos innecesarios y mejora rendimiento
    /// Thread-Safe: Usa ReaderWriterLockSlim para acceso concurrente
    /// </summary>
    public class RadialImmutableCache
    {
        // Thread-Safe Cache Storage
        
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly Dictionary<CacheKey, CacheEntry> _cache = new Dictionary<CacheKey, CacheEntry>();
        
        // Contadores de versión para invalidación selectiva
        private volatile int _frameListVersion = 0;
        private volatile int _activeFrameVersion = 0;
        private volatile int _propertyVersion = 0;
        
        
        // Cache Operations (Thread-Safe)
        
        /// <summary>
        /// Obtiene valor del cache o calcula y guarda si no existe (thread-safe)
        /// </summary>
        public T GetOrCalculate<T>(CacheKey key, Func<T> calculator, CacheInvalidationType invalidationType)
        {
            // Intento de lectura rápida
            _cacheLock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var entry) && IsValidEntry(entry, invalidationType))
                {
                    return (T)entry.Value;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
            
            // Necesita cálculo - obtener write lock
            _cacheLock.EnterWriteLock();
            try
            {
                // Double-check: otro hilo pudo haber calculado mientras esperábamos
                if (_cache.TryGetValue(key, out var entry) && IsValidEntry(entry, invalidationType))
                {
                    return (T)entry.Value;
                }
                
                // Calcular valor nuevo
                var newValue = calculator();
                var newEntry = new CacheEntry(newValue, GetCurrentVersion(invalidationType));
                _cache[key] = newEntry;
                
                return newValue;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Invalida selectivamente por tipo de operación (thread-safe)
        /// </summary>
        public void InvalidateByType(CacheInvalidationType invalidationType)
        {
            switch (invalidationType)
            {
                case CacheInvalidationType.FrameList:
                    Interlocked.Increment(ref _frameListVersion);
                    break;
                case CacheInvalidationType.ActiveFrame:
                    Interlocked.Increment(ref _activeFrameVersion);
                    break;
                case CacheInvalidationType.Properties:
                    Interlocked.Increment(ref _propertyVersion);
                    break;
                case CacheInvalidationType.All:
                    InvalidateAll();
                    break;
            }
        }
        
        /// <summary>
        /// Invalida todo el cache (thread-safe)
        /// </summary>
        public void InvalidateAll()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
                Interlocked.Increment(ref _frameListVersion);
                Interlocked.Increment(ref _activeFrameVersion);
                Interlocked.Increment(ref _propertyVersion);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        
        
        // Specialized Cache Methods
        
        /// <summary>
        /// Cache para conteo de frames válidos
        /// </summary>
        public int GetFrameCount(List<MRAgruparObjetos> frames)
        {
            return GetOrCalculate(
                CacheKey.FrameCount,
                () => CountValidFrames(frames),
                CacheInvalidationType.FrameList);
        }
        
        /// <summary>
        /// Cache para hash de lista de frames
        /// </summary>
        public int GetFrameListHash(List<MRAgruparObjetos> frames)
        {
            return GetOrCalculate(
                CacheKey.FrameListHash,
                () => CalculateFrameListHash(frames),
                CacheInvalidationType.FrameList);
        }
        
        /// <summary>
        /// Cache para frame activo
        /// </summary>
        public MRAgruparObjetos GetActiveFrame(List<MRAgruparObjetos> frames, int activeIndex)
        {
            return GetOrCalculate(
                CacheKey.ActiveFrame,
                () => GetActiveFrameInternal(frames, activeIndex),
                CacheInvalidationType.ActiveFrame);
        }
        
        /// <summary>
        /// Cache para rutas de animación completas
        /// </summary>
        public string GetFullAnimationPath(string animationPath, string animationName)
        {
            return GetOrCalculate(
                CacheKey.FullAnimationPath,
                () => System.IO.Path.Combine(animationPath, animationName + ".anim"),
                CacheInvalidationType.Properties);
        }
        
        
        // Private Implementation
        
        /// <summary>
        /// Verifica si una entrada del cache sigue siendo válida
        /// </summary>
        private bool IsValidEntry(CacheEntry entry, CacheInvalidationType invalidationType)
        {
            var currentVersion = GetCurrentVersion(invalidationType);
            return entry.Version >= currentVersion;
        }
        
        /// <summary>
        /// Obtiene la versión actual para un tipo de invalidación
        /// </summary>
        private int GetCurrentVersion(CacheInvalidationType invalidationType)
        {
            return invalidationType switch
            {
                CacheInvalidationType.FrameList => _frameListVersion,
                CacheInvalidationType.ActiveFrame => _activeFrameVersion,
                CacheInvalidationType.Properties => _propertyVersion,
                CacheInvalidationType.All => Math.Max(_frameListVersion, Math.Max(_activeFrameVersion, _propertyVersion)),
                _ => 0
            };
        }
        
        /// <summary>
        /// Cuenta frames válidos (no nulos)
        /// </summary>
        private int CountValidFrames(List<MRAgruparObjetos> frames)
        {
            if (frames == null) return 0;
            
            int count = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] != null) count++;
            }
            return count;
        }
        
        /// <summary>
        /// Calcula hash de lista de frames para detectar cambios
        /// </summary>
        private int CalculateFrameListHash(List<MRAgruparObjetos> frames)
        {
            if (frames == null || frames.Count == 0) return 0;
            
            int hash = frames.Count;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] != null)
                {
                    hash ^= frames[i].GetInstanceID() + i;
                }
            }
            return hash;
        }
        
        /// <summary>
        /// Obtiene frame activo considerando lógica específica
        /// </summary>
        private MRAgruparObjetos GetActiveFrameInternal(List<MRAgruparObjetos> frames, int activeIndex)
        {
            if (frames == null || frames.Count == 0) return null;
            
            // Lógica específica: Para animaciones On/Off, siempre devolver el primer frame
            if (CountValidFrames(frames) == 1)
            {
                return frames[0];
            }
            
            // Múltiples frames: lógica normal de índice
            if (activeIndex >= 0 && activeIndex < frames.Count)
            {
                return frames[activeIndex];
            }
            
            return null;
        }
        
        
        // Cleanup
        
        /// <summary>
        /// Limpia cache y libera recursos
        /// </summary>
        public void Dispose()
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
                _cacheLock?.Dispose();
            }
        }
        
        
        // Cache Statistics (Non-Debug)
        
        
    }
    
    /// <summary>
    /// Entrada inmutable del cache
    /// </summary>
    internal class CacheEntry
    {
        public readonly object Value;
        public readonly int Version;
        
        public CacheEntry(object value, int version)
        {
            Value = value;
            Version = version;
        }
    }
    
    /// <summary>
    /// Claves tipadas para cache
    /// </summary>
    public enum CacheKey
    {
        FrameCount,
        FrameListHash,
        ActiveFrame,
        FullAnimationPath,
        AnimationType,
        ValidationResult
    }
    
    /// <summary>
    /// Tipos de invalidación de cache
    /// </summary>
    public enum CacheInvalidationType
    {
        FrameList,    // Cuando se agregan/quitan frames
        ActiveFrame,  // Cuando cambia el índice activo
        Properties,   // Cuando cambian propiedades de animación
        All          // Invalida todo
    }
    
}
