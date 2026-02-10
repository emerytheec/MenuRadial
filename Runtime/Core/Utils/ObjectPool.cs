using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Pool de objetos genérico para reducir garbage collection
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _objects = new Stack<T>();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;

        public ObjectPool(
            Func<T> objectGenerator = null,
            Action<T> resetAction = null,
            int maxSize = 100,
            string poolName = null)
        {
            _objectGenerator = objectGenerator ?? (() => new T());
            _resetAction = resetAction;
            _maxSize = maxSize;
        }

        public T Get()
        {
            if (_objects.Count > 0)
                return _objects.Pop();

            return _objectGenerator();
        }

        public void Return(T item)
        {
            if (item == null || _objects.Count >= _maxSize)
                return;

            _resetAction?.Invoke(item);
            _objects.Push(item);
        }

        public void Clear()
        {
            _objects.Clear();
        }
    }

    /// <summary>
    /// Pools estáticos para tipos usados en el sistema MenuRadial
    /// </summary>
    public static class ListPools
    {
        public static readonly ObjectPool<List<Material>> Materials =
            new ObjectPool<List<Material>>(resetAction: list => list.Clear(), maxSize: 50);

        public static readonly ObjectPool<List<MRAgruparObjetos>> FrameObjects =
            new ObjectPool<List<MRAgruparObjetos>>(resetAction: list => list.Clear(), maxSize: 20);

        public static void ClearAllPools()
        {
            Materials.Clear();
            FrameObjects.Clear();
        }
    }
}
