using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Controlador especializado para la gestión de GameObjects en frames.
    /// Implementa IObjectReferenceController para abstracción.
    /// REFACTORIZADO: Extraído de MRAgruparObjetos.cs para responsabilidad única.
    /// </summary>
    public class FrameObjectController : IObjectReferenceController
    {
        private readonly FrameData _frameData;

        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frameData">Datos del frame para gestionar</param>
        public FrameObjectController(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }

        #region IReferenceController Implementation

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

        // RemoveInvalidReferences() ya existe como método público más abajo

        #endregion
        
        
        /// <summary>
        /// Añade un GameObject al frame
        /// EXTRAÍDO: De MRAgruparObjetos.AddGameObject()
        /// </summary>
        /// <param name="gameObject">GameObject a añadir</param>
        /// <param name="isActive">Estado de activación deseado</param>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        public bool AddObject(GameObject gameObject, bool isActive = true)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[MRAgruparObjetos] No se puede añadir objeto: GameObject es null");
                return false;
            }

            // Verificar si ya existe
            var existing = _frameData.ObjectReferences.FirstOrDefault(r => r?.GameObject == gameObject);
            if (existing != null)
            {
                existing.IsActive = isActive; // Actualizar estado existente
                return true; // Se considera éxito porque se actualizó
            }

            // Crear nueva referencia
            var newReference = new ObjectReference(gameObject, isActive);
            _frameData.ObjectReferences.Add(newReference);

            return true;
        }
        
        /// <summary>
        /// Elimina un GameObject del frame
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveGameObject()
        /// </summary>
        /// <param name="gameObject">GameObject a eliminar</param>
        public void RemoveObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            
            var toRemove = _frameData.ObjectReferences.Where(r => r?.GameObject == gameObject).ToList();
            
            foreach (var reference in toRemove)
            {
                _frameData.ObjectReferences.Remove(reference);
            }
        }
        
        /// <summary>
        /// Limpia todos los objetos del frame
        /// EXTRAÍDO: De MRAgruparObjetos.ClearAllObjects()
        /// </summary>
        public void ClearAllObjects()
        {
            int count = ObjectCount;
            _frameData.ObjectReferences.Clear();
        }
        
        /// <summary>
        /// Selecciona todos los objetos (los marca como activos)
        /// EXTRAÍDO: De MRAgruparObjetos.SelectAllObjects()
        /// </summary>
        public void SelectAllObjects()
        {
            int activatedCount = 0;
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                if (!objRef.IsActive)
                {
                    objRef.IsActive = true;
                    activatedCount++;
                }
            }
            
        }
        
        /// <summary>
        /// Deselecciona todos los objetos (los marca como inactivos)
        /// EXTRAÍDO: De MRAgruparObjetos.DeselectAllObjects()
        /// </summary>
        public void DeselectAllObjects()
        {
            int deactivatedCount = 0;
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.IsValid))
            {
                if (objRef.IsActive)
                {
                    objRef.IsActive = false;
                    deactivatedCount++;
                }
            }
            
        }
        
        /// <summary>
        /// Recalcula las rutas jerárquicas de todos los objetos
        /// EXTRAÍDO: De MRAgruparObjetos.RecalculatePaths()
        /// </summary>
        public void RecalculateAllPaths()
        {
            int updatedCount = 0;
            
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null && o.GameObject != null))
            {
                objRef.UpdateHierarchyPath();
                updatedCount++;
            }
            
        }
        
        /// <summary>
        /// Elimina las referencias inválidas
        /// EXTRAÍDO: De MRAgruparObjetos.RemoveInvalidReferences()
        /// </summary>
        public void RemoveInvalidReferences()
        {
            var invalidReferences = _frameData.ObjectReferences.Where(o => o == null || !o.IsValid).ToList();
            
            foreach (var invalidRef in invalidReferences)
            {
                _frameData.ObjectReferences.Remove(invalidRef);
            }
            
            if (invalidReferences.Count > 0)
            {
            }
        }
        
        
        
        /// <summary>
        /// Aplica los estados de los objetos en la escena con validación robusta
        /// EXTRAÍDO: Parte de MRAgruparObjetos.ApplyCurrentFrame()
        /// </summary>
        public void ApplyObjectStates()
        {
            int appliedCount = 0;
            int skippedCount = 0;
            
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null))
            {
                if (objRef.GameObject == null)
                {
                    skippedCount++;
                    continue;
                }
                
                if (!objRef.IsValid)
                {
                    skippedCount++;
                    continue;
                }
                
                // Verificación adicional antes del acceso
                if (objRef.GameObject != null)
                {
                    objRef.GameObject.SetActive(objRef.IsActive);
                    appliedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            
        }
        
        /// <summary>
        /// Captura los estados actuales de los objetos en la escena con validación robusta
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <returns>Lista de estados actuales</returns>
        public List<ObjectReference> CaptureCurrentStates()
        {
            var currentStates = new List<ObjectReference>();
            int skippedCount = 0;
            
            foreach (var objRef in _frameData.ObjectReferences.Where(o => o != null))
            {
                if (objRef.GameObject == null)
                {
                    skippedCount++;
                    continue;
                }
                
                if (!objRef.IsValid)
                {
                    skippedCount++;
                    continue;
                }
                
                // Verificación adicional antes del acceso
                if (objRef.GameObject != null)
                {
                    bool currentState = objRef.GameObject.activeSelf;
                    var stateCapture = new ObjectReference(objRef.GameObject, currentState);
                    currentStates.Add(stateCapture);
                }
                else
                {
                    skippedCount++;
                }
            }
            
            return currentStates;
        }
        
        /// <summary>
        /// Restaura estados previamente capturados
        /// EXTRAÍDO: Parte del sistema de preview de MRAgruparObjetos
        /// </summary>
        /// <param name="savedStates">Estados a restaurar</param>
        public void RestoreStates(List<ObjectReference> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
            {
                return;
            }
            
            int restoredCount = 0;
            
            foreach (var savedState in savedStates.Where(s => s != null && s.IsValid))
            {
                savedState.GameObject.SetActive(savedState.IsActive);
                restoredCount++;
            }
            
        }
        
        
        
        /// <summary>
        /// Busca un objeto específico en las referencias
        /// NUEVO: Método utilitario para búsquedas
        /// </summary>
        /// <param name="gameObject">GameObject a buscar</param>
        /// <returns>Referencia encontrada o null</returns>
        public ObjectReference FindObjectReference(GameObject gameObject)
        {
            if (gameObject == null) return null;
            
            return _frameData.ObjectReferences.FirstOrDefault(r => r?.GameObject == gameObject);
        }
        
        /// <summary>
        /// Verifica si un objeto está en el frame
        /// NUEVO: Método utilitario para verificaciones
        /// </summary>
        /// <param name="gameObject">GameObject a verificar</param>
        /// <returns>True si está en el frame</returns>
        public bool ContainsObject(GameObject gameObject)
        {
            return FindObjectReference(gameObject) != null;
        }
        
        /// <summary>
        /// Obtiene todos los objetos activos en el frame
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de GameObjects activos</returns>
        public List<GameObject> GetActiveObjects()
        {
            return _frameData.ObjectReferences
                .Where(o => o != null && o.IsValid && o.IsActive)
                .Select(o => o.GameObject)
                .ToList();
        }
        
        /// <summary>
        /// Obtiene todos los objetos inactivos en el frame
        /// NUEVO: Método utilitario para filtros
        /// </summary>
        /// <returns>Lista de GameObjects inactivos</returns>
        public List<GameObject> GetInactiveObjects()
        {
            return _frameData.ObjectReferences
                .Where(o => o != null && o.IsValid && !o.IsActive)
                .Select(o => o.GameObject)
                .ToList();
        }
        
        
    }
}
