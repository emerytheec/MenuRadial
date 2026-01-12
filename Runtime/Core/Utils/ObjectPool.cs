using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Pool de objetos genérico para reducir garbage collection
    /// Simplificado para ejecución single-thread en Editor Unity
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a poolear</typeparam>
    public class ObjectPool<T> where T : class, new()
    {
        
        private readonly Stack<T> _objects = new Stack<T>();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;
        private readonly string _poolName;
        
        // Estadísticas del pool
        private int _totalCreated = 0;
        private int _totalRequested = 0;
        private int _totalReturned = 0;
        private int _poolHits = 0;
        
        
        
        /// <summary>
        /// Constructor del ObjectPool con configuración personalizable
        /// </summary>
        /// <param name="objectGenerator">Función para crear nuevos objetos</param>
        /// <param name="resetAction">Acción para resetear objetos antes de reutilizar</param>
        /// <param name="maxSize">Tamaño máximo del pool</param>
        /// <param name="poolName">Nombre del pool</param>
        public ObjectPool(
            Func<T> objectGenerator = null, 
            Action<T> resetAction = null, 
            int maxSize = 100,
            string poolName = null)
        {
            _objectGenerator = objectGenerator ?? (() => new T());
            _resetAction = resetAction;
            _maxSize = maxSize;
            _poolName = poolName ?? typeof(T).Name;
            
        }
        
        
        
        /// <summary>
        /// Obtiene un objeto del pool o crea uno nuevo
        /// </summary>
        /// <returns>Objeto reutilizable del pool</returns>
        public T Get()
        {
            _totalRequested++;
            
            if (_objects.Count > 0)
            {
                _poolHits++;
                return _objects.Pop();
            }
            
            // Crear nuevo objeto si el pool está vacío
            var item = _objectGenerator();
            _totalCreated++;
            
            return item;
        }
        
        /// <summary>
        /// Devuelve un objeto al pool para reutilización
        /// </summary>
        /// <param name="item">Objeto a devolver al pool</param>
        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }
            
            if (_objects.Count >= _maxSize)
            {
                return;
            }
            
            // Resetear objeto antes de devolverlo al pool
            _resetAction?.Invoke(item);
            _objects.Push(item);
            _totalReturned++;
        }
        
        /// <summary>
        /// Limpia completamente el pool
        /// </summary>
        public void Clear()
        {
            var previousCount = _objects.Count;
            _objects.Clear();
        }
        
        
    }
    
    /// <summary>
    /// Pools estáticos para tipos comunes del sistema MenuRadial
    /// </summary>
    public static class ListPools
    {
        
        // Pools para tipos Unity comunes
        public static readonly ObjectPool<List<Material>> Materials = 
            new ObjectPool<List<Material>>(
                resetAction: list => list.Clear(),
                maxSize: 50,
                poolName: "Materials"
            );
            
        public static readonly ObjectPool<List<Renderer>> Renderers = 
            new ObjectPool<List<Renderer>>(
                resetAction: list => list.Clear(),
                maxSize: 30,
                poolName: "Renderers"
            );
            
        public static readonly ObjectPool<List<GameObject>> GameObjects = 
            new ObjectPool<List<GameObject>>(
                resetAction: list => list.Clear(),
                maxSize: 40,
                poolName: "GameObjects"
            );
        
        // Pools para tipos MenuRadial específicos
        public static readonly ObjectPool<List<MRAgruparObjetos>> FrameObjects = 
            new ObjectPool<List<MRAgruparObjetos>>(
                resetAction: list => list.Clear(),
                maxSize: 20,
                poolName: "FrameObjects"
            );
            
        // Pools genéricos para referencias - usando tipos Unity estándar
        public static readonly ObjectPool<List<Component>> ComponentReferences = 
            new ObjectPool<List<Component>>(
                resetAction: list => list.Clear(),
                maxSize: 30,
                poolName: "ComponentReferences"
            );
            
        public static readonly ObjectPool<List<Transform>> TransformReferences = 
            new ObjectPool<List<Transform>>(
                resetAction: list => list.Clear(),
                maxSize: 25,
                poolName: "TransformReferences"
            );
            
        public static readonly ObjectPool<List<SkinnedMeshRenderer>> SkinnedMeshRendererReferences = 
            new ObjectPool<List<SkinnedMeshRenderer>>(
                resetAction: list => list.Clear(),
                maxSize: 20,
                poolName: "SkinnedMeshRendererReferences"
            );
        
        // Pools para tipos de sistema
        public static readonly ObjectPool<List<WeakReference>> WeakReferences = 
            new ObjectPool<List<WeakReference>>(
                resetAction: list => list.Clear(),
                maxSize: 25,
                poolName: "WeakReferences"
            );
            
        public static readonly ObjectPool<List<string>> Strings = 
            new ObjectPool<List<string>>(
                resetAction: list => list.Clear(),
                maxSize: 30,
                poolName: "Strings"
            );
        
        // Pool para tipos genéricos comunes
        public static readonly ObjectPool<List<int>> Integers = 
            new ObjectPool<List<int>>(
                resetAction: list => list.Clear(),
                maxSize: 20,
                poolName: "Integers"
            );
            
        
        
        
        /// <summary>
        /// Limpia todos los pools estáticos
        /// </summary>
        public static void ClearAllPools()
        {
            
            Materials.Clear();
            Renderers.Clear();
            GameObjects.Clear();
            FrameObjects.Clear();
            ComponentReferences.Clear();
            TransformReferences.Clear();
            SkinnedMeshRendererReferences.Clear();
            WeakReferences.Clear();
            Strings.Clear();
            Integers.Clear();
            
        }
        
        
    }
    
    /// <summary>
    /// Extensiones para facilitar el uso de ObjectPools
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        /// Wrapper IDisposable para auto-return al pool
        /// </summary>
        public static PooledObject<T> GetPooled<T>(this ObjectPool<T> pool) where T : class, new()
        {
            return new PooledObject<T>(pool, pool.Get());
        }
    }
    
    /// <summary>
    /// Wrapper que implementa IDisposable para auto-return al pool
    /// </summary>
    public class PooledObject<T> : IDisposable where T : class, new()
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _item;
        private bool _disposed = false;
        
        public T Item => _item;
        
        internal PooledObject(ObjectPool<T> pool, T item)
        {
            _pool = pool;
            _item = item;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _pool.Return(_item);
                _disposed = true;
            }
        }
        
        // Operator implícito para uso transparente
        public static implicit operator T(PooledObject<T> pooledObject) => pooledObject.Item;
    }
}
