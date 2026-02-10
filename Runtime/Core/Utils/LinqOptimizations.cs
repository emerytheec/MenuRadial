using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Cache inteligente para resultados de operaciones frecuentes
    /// </summary>
    /// <typeparam name="TKey">Tipo de clave</typeparam>
    /// <typeparam name="TValue">Tipo de valor</typeparam>
    public class FrameBasedCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, CachedValue<TValue>> _cache = new Dictionary<TKey, CachedValue<TValue>>();
        private readonly int _maxEntries;

        public FrameBasedCache(int maxEntries = 100)
        {
            _maxEntries = maxEntries;
        }

        /// <summary>
        /// Obtiene valor del cache o lo calcula si no existe/está obsoleto
        /// </summary>
        /// <param name="key">Clave del cache</param>
        /// <param name="valueFactory">Función para calcular el valor</param>
        /// <returns>Valor cacheado o recalculado</returns>
        public TValue GetOrCalculate(TKey key, Func<TValue> valueFactory)
        {
            if (_cache.TryGetValue(key, out var cached) && cached.Frame == Time.frameCount)
            {
                return cached.Value;
            }

            // Limpiar cache si está lleno
            if (_cache.Count >= _maxEntries)
            {
                CleanupOldEntries();
            }

            var newValue = valueFactory();
            _cache[key] = new CachedValue<TValue> { Value = newValue, Frame = Time.frameCount };

            return newValue;
        }

        /// <summary>
        /// Limpia entradas obsoletas del cache
        /// </summary>
        public void CleanupOldEntries()
        {
            var currentFrame = Time.frameCount;
            var keysToRemove = new List<TKey>();

            foreach (var kvp in _cache)
            {
                if (currentFrame - kvp.Value.Frame > 10) // Obsoleto si tiene más de 10 frames
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Limpia completamente el cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        private struct CachedValue<T>
        {
            public T Value;
            public int Frame;
        }
    }
}
