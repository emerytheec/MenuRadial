using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de GameObjects en frames.
    /// </summary>
    public class FrameObjectController
    {
        private readonly FrameData _frameData;

        public FrameObjectController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region Reference Operations

        public int Count => _frameData.ObjectReferences?.Count ?? 0;
        public int ValidCount => _frameData.ObjectReferences?.Count(o => o != null && o.IsValid) ?? 0;
        public int InvalidCount => Count - ValidCount;
        public List<ObjectReference> References => _frameData.ObjectReferences;

        // Alias para compatibilidad
        public int ObjectCount => Count;
        public int ValidObjectCount => ValidCount;
        public int InvalidObjectCount => InvalidCount;
        public List<ObjectReference> ObjectReferences => References;

        public void ClearAll() => ClearAllObjects();
        public void ApplyStates() => ApplyObjectStates();

        #endregion

        public bool AddObject(GameObject gameObject, bool isActive = true)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir objeto: GameObject es null");
                return false;
            }

            var existing = _frameData.ObjectReferences.FirstOrDefault(r => r?.GameObject == gameObject);
            if (existing != null)
            {
                existing.IsActive = isActive;
                return true;
            }

            var newReference = new ObjectReference(gameObject, isActive);
            _frameData.ObjectReferences.Add(newReference);

            return true;
        }

        public void RemoveObject(GameObject gameObject)
        {
            if (gameObject == null) return;

            var toRemove = _frameData.ObjectReferences.Where(r => r?.GameObject == gameObject).ToList();

            foreach (var reference in toRemove)
            {
                _frameData.ObjectReferences.Remove(reference);
            }
        }

        public void ClearAllObjects()
        {
            _frameData.ObjectReferences.Clear();
        }

        public void SelectAllObjects()
        {
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                objRef.IsActive = true;
            }
        }

        public void DeselectAllObjects()
        {
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                objRef.IsActive = false;
            }
        }

        public void RecalculateAllPaths()
        {
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.GameObject != null))
            {
                objRef.UpdateHierarchyPath();
            }
        }

        public void RemoveInvalidReferences()
        {
            _frameData.ObjectReferences.RemoveAll(o => o == null || !o.IsValid);
        }

        public void ApplyObjectStates()
        {
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                if (objRef.GameObject != null)
                {
                    objRef.GameObject.SetActive(objRef.IsActive);
                }
            }
        }

        public List<ObjectReference> CaptureCurrentStates()
        {
            var currentStates = new List<ObjectReference>();

            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                if (objRef.GameObject != null)
                {
                    currentStates.Add(new ObjectReference(objRef.GameObject, objRef.GameObject.activeSelf));
                }
            }

            return currentStates;
        }

        public void RestoreStates(List<ObjectReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
                return;

            foreach (var savedState in savedStates.Where(s => s != null && s.IsValid))
            {
                savedState.GameObject.SetActive(savedState.IsActive);
            }
        }
    }
}
