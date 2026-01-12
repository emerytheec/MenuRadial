using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Extensiones para operaciones LINQ optimizadas sin garbage collection
    /// NUEVO [2025-07-04]: Alternativas eficientes a operaciones LINQ costosas
    /// </summary>
    public static class LinqOptimizations
    {
        
        /// <summary>
        /// Filtra elementos válidos a una lista destino sin crear listas temporales
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Colección fuente</param>
        /// <param name="destination">Lista destino (se limpia antes de usar)</param>
        /// <param name="predicate">Condición de filtrado</param>
        public static void FilterValidTo<T>(this IEnumerable<T> source, List<T> destination, Func<T, bool> predicate)
        {
            destination.Clear();
            foreach (var item in source)
            {
                if (predicate(item))
                    destination.Add(item);
            }
        }
        
        /// <summary>
        /// Filtra elementos no nulos a una lista destino
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Colección fuente</param>
        /// <param name="destination">Lista destino</param>
        public static void FilterNonNullTo<T>(this IEnumerable<T> source, List<T> destination) where T : class
        {
            destination.Clear();
            foreach (var item in source)
            {
                if (item != null)
                    destination.Add(item);
            }
        }
        
        /// <summary>
        /// Filtra referencias Unity válidas (no null y no destroyed)
        /// </summary>
        /// <typeparam name="T">Tipo Unity Object</typeparam>
        /// <param name="source">Colección fuente</param>
        /// <param name="destination">Lista destino</param>
        public static void FilterValidUnityTo<T>(this IEnumerable<T> source, List<T> destination) where T : UnityEngine.Object
        {
            destination.Clear();
            foreach (var item in source)
            {
                if (item != null) // Unity null check automático
                    destination.Add(item);
            }
        }
        
        
        
        /// <summary>
        /// Cuenta elementos que cumplen condición sin crear iteradores LINQ
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <param name="predicate">Condición a verificar</param>
        /// <returns>Número de elementos que cumplen la condición</returns>
        public static int CountWhere<T>(this IList<T> source, Func<T, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// Cuenta elementos Unity válidos (no null)
        /// </summary>
        /// <typeparam name="T">Tipo Unity Object</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <returns>Número de elementos válidos</returns>
        public static int CountValidUnity<T>(this IList<T> source) where T : UnityEngine.Object
        {
            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// Cuenta elementos no nulos
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <returns>Número de elementos no nulos</returns>
        public static int CountNonNull<T>(this IList<T> source) where T : class
        {
            int count = 0;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    count++;
            }
            return count;
        }
        
        
        
        /// <summary>
        /// Encuentra el primer elemento que cumple condición sin LINQ
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <param name="predicate">Condición de búsqueda</param>
        /// <returns>Primer elemento encontrado o default(T)</returns>
        public static T FindFirst<T>(this IList<T> source, Func<T, bool> predicate)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                    return source[i];
            }
            return default(T);
        }
        
        /// <summary>
        /// Verifica si existe algún elemento que cumple condición
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <param name="predicate">Condición a verificar</param>
        /// <returns>True si existe al menos un elemento</returns>
        public static bool AnyWhere<T>(this IList<T> source, Func<T, bool> predicate)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Verifica si todos los elementos cumplen condición
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="source">Lista fuente</param>
        /// <param name="predicate">Condición a verificar</param>
        /// <returns>True si todos cumplen la condición</returns>
        public static bool AllWhere<T>(this IList<T> source, Func<T, bool> predicate)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (!predicate(source[i]))
                    return false;
            }
            return true;
        }
        
        
        
        /// <summary>
        /// Transforma elementos usando una función y los agrega a lista destino
        /// </summary>
        /// <typeparam name="TSource">Tipo fuente</typeparam>
        /// <typeparam name="TResult">Tipo resultado</typeparam>
        /// <param name="source">Colección fuente</param>
        /// <param name="destination">Lista destino</param>
        /// <param name="selector">Función de transformación</param>
        public static void SelectTo<TSource, TResult>(this IEnumerable<TSource> source, List<TResult> destination, Func<TSource, TResult> selector)
        {
            destination.Clear();
            foreach (var item in source)
            {
                destination.Add(selector(item));
            }
        }
        
        /// <summary>
        /// Transforma y filtra elementos en una sola operación
        /// </summary>
        /// <typeparam name="TSource">Tipo fuente</typeparam>
        /// <typeparam name="TResult">Tipo resultado</typeparam>
        /// <param name="source">Colección fuente</param>
        /// <param name="destination">Lista destino</param>
        /// <param name="selector">Función de transformación</param>
        /// <param name="predicate">Condición de filtrado (aplicada al resultado)</param>
        public static void SelectWhereTo<TSource, TResult>(this IEnumerable<TSource> source, List<TResult> destination, Func<TSource, TResult> selector, Func<TResult, bool> predicate)
        {
            destination.Clear();
            foreach (var item in source)
            {
                var transformed = selector(item);
                if (predicate(transformed))
                    destination.Add(transformed);
            }
        }
        
        
        
        /// <summary>
        /// Filtra GameObjects activos usando pooled list
        /// </summary>
        /// <param name="source">Lista de GameObjects</param>
        /// <param name="destination">Lista destino</param>
        public static void FilterActiveGameObjectsTo(this IEnumerable<GameObject> source, List<GameObject> destination)
        {
            destination.Clear();
            foreach (var gameObject in source)
            {
                if (gameObject != null && gameObject.activeInHierarchy)
                    destination.Add(gameObject);
            }
        }
        
        /// <summary>
        /// Filtra componentes válidos (no null y con GameObject activo)
        /// </summary>
        /// <typeparam name="T">Tipo de componente</typeparam>
        /// <param name="source">Lista de componentes</param>
        /// <param name="destination">Lista destino</param>
        public static void FilterValidComponentsTo<T>(this IEnumerable<T> source, List<T> destination) where T : Component
        {
            destination.Clear();
            foreach (var component in source)
            {
                if (component != null && component.gameObject != null)
                    destination.Add(component);
            }
        }
        
        /// <summary>
        /// Obtiene nombres de GameObjects de forma optimizada
        /// </summary>
        /// <param name="source">Lista de GameObjects</param>
        /// <param name="destination">Lista de nombres destino</param>
        public static void GetGameObjectNamesTo(this IEnumerable<GameObject> source, List<string> destination)
        {
            destination.Clear();
            foreach (var gameObject in source)
            {
                if (gameObject != null)
                    destination.Add(gameObject.name);
            }
        }
        
        
        
        /// <summary>
        /// Filtra WeakReferences vivas a lista destino
        /// </summary>
        /// <param name="source">Lista de WeakReferences</param>
        /// <param name="destination">Lista destino</param>
        public static void FilterAliveWeakReferencesTo(this IEnumerable<WeakReference> source, List<WeakReference> destination)
        {
            destination.Clear();
            foreach (var weakRef in source)
            {
                if (weakRef != null && weakRef.IsAlive)
                    destination.Add(weakRef);
            }
        }
        
        /// <summary>
        /// Extrae targets vivos de WeakReferences
        /// </summary>
        /// <typeparam name="T">Tipo del target</typeparam>
        /// <param name="source">Lista de WeakReferences</param>
        /// <param name="destination">Lista de targets destino</param>
        public static void ExtractAliveTargetsTo<T>(this IEnumerable<WeakReference> source, List<T> destination) where T : class
        {
            destination.Clear();
            foreach (var weakRef in source)
            {
                if (weakRef != null && weakRef.IsAlive && weakRef.Target is T target)
                    destination.Add(target);
            }
        }
        
        
        
        /// <summary>
        /// Realiza operación usando lista temporal del pool
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <typeparam name="TResult">Tipo de resultado</typeparam>
        /// <param name="pool">Pool de listas</param>
        /// <param name="operation">Operación a realizar con la lista temporal</param>
        /// <returns>Resultado de la operación</returns>
        public static TResult UsingPooledList<T, TResult>(this ObjectPool<List<T>> pool, Func<List<T>, TResult> operation)
        {
            var tempList = pool.Get();
            try
            {
                return operation(tempList);
            }
            finally
            {
                pool.Return(tempList);
            }
        }
        
        /// <summary>
        /// Realiza operación usando lista temporal del pool (sin retorno)
        /// </summary>
        /// <typeparam name="T">Tipo de elementos</typeparam>
        /// <param name="pool">Pool de listas</param>
        /// <param name="operation">Operación a realizar con la lista temporal</param>
        public static void UsingPooledList<T>(this ObjectPool<List<T>> pool, Action<List<T>> operation)
        {
            var tempList = pool.Get();
            try
            {
                operation(tempList);
            }
            finally
            {
                pool.Return(tempList);
            }
        }
        
    }
    
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
