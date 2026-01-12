using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Managers
{
    /// <summary>
    /// Clase base genérica para gestión de referencias
    /// FASE 1: Unifica la lógica duplicada entre FrameObjectManager, FrameMaterialManager y FrameBlendshapeManager
    /// Elimina ~60-70% de código duplicado entre managers (de 1,102 líneas a ~400 líneas)
    /// </summary>
    /// <typeparam name="TReference">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    /// <typeparam name="TTarget">Tipo del objeto objetivo (GameObject, Renderer, etc.)</typeparam>
    public abstract class BaseReferenceManager<TReference, TTarget>
        where TReference : IReferenceBase<TTarget>
        where TTarget : UnityEngine.Object
    {
        protected readonly FrameData _frameData;
        protected readonly ReferenceListManager<TReference, TTarget> _listManager;
        
        /// <summary>
        /// Constructor base con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        protected BaseReferenceManager(FrameData frameData)
        {
            _frameData = frameData ?? throw new ArgumentNullException(nameof(frameData));
            _listManager = GetListManager();
        }
        
        /// <summary>
        /// Obtiene el manager de lista específico del FrameData
        /// Método abstracto que debe ser implementado por cada manager especializado
        /// </summary>
        /// <returns>ReferenceListManager específico</returns>
        protected abstract ReferenceListManager<TReference, TTarget> GetListManager();
        
        
        /// <summary>
        /// Lista de referencias (delegación directa al list manager)
        /// </summary>
        public List<TReference> References => _listManager.References;
        
        /// <summary>
        /// Número total de referencias
        /// </summary>
        public int Count => _listManager.Count;
        
        /// <summary>
        /// Número de referencias válidas
        /// </summary>
        public int ValidCount => _listManager.ValidCount;
        
        /// <summary>
        /// Número de referencias inválidas
        /// </summary>
        public int InvalidCount => _listManager.InvalidCount;
        
        
        
        /// <summary>
        /// Añade una referencia (método genérico común)
        /// Las clases derivadas pueden sobrescribir para validaciones específicas
        /// </summary>
        /// <param name="reference">Referencia a añadir</param>
        /// <returns>True si fue añadida correctamente</returns>
        public virtual bool Add(TReference reference)
        {
            if (reference == null || reference.Target == null)
            {
                return false;
            }
            
            if (ValidateBeforeAdd(reference))
            {
                bool added = _listManager.Add(reference);
                if (added)
                {
                    OnReferenceAdded(reference);
                }
                return added;
            }
            
            return false;
        }
        
        /// <summary>
        /// Elimina una referencia específica
        /// </summary>
        /// <param name="reference">Referencia a eliminar</param>
        /// <returns>True si fue eliminada</returns>
        public virtual bool Remove(TReference reference)
        {
            if (reference == null) return false;
            
            bool removed = _listManager.Remove(reference);
            if (removed)
            {
                OnReferenceRemoved(reference);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Elimina referencia por target
        /// </summary>
        /// <param name="target">Target a eliminar</param>
        /// <returns>Número de referencias eliminadas</returns>
        public virtual int RemoveByTarget(TTarget target)
        {
            if (target == null) return 0;
            
            int removed = _listManager.RemoveByTarget(target);
            if (removed > 0)
            {
                OnReferencesRemovedByTarget(target, removed);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Limpia todas las referencias
        /// </summary>
        public virtual void ClearAll()
        {
            var count = _listManager.Count;
            _listManager.Clear();
            OnAllReferencesCleared(count);
        }
        
        /// <summary>
        /// Elimina todas las referencias inválidas
        /// </summary>
        /// <returns>Número de referencias eliminadas</returns>
        public virtual int RemoveInvalid()
        {
            int removed = _listManager.RemoveInvalid();
            if (removed > 0)
            {
                OnInvalidReferencesRemoved(removed);
            }
            
            return removed;
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todas las referencias
        /// </summary>
        public virtual void UpdateAllHierarchyPaths()
        {
            _listManager.UpdateAllHierarchyPaths();
            OnHierarchyPathsUpdated();
        }
        
        
        
        /// <summary>
        /// Aplica los estados de todas las referencias válidas
        /// </summary>
        public virtual void ApplyAllStates()
        {
            var validRefs = GetValidReferences();
            var applied = 0;
            
            foreach (var reference in validRefs)
            {
                reference.Apply();
                applied++;
            }
            
            OnStatesApplied(applied, 0);
        }
        
        /// <summary>
        /// Captura los estados actuales para previsualización
        /// </summary>
        /// <returns>Lista de estados capturados</returns>
        public virtual List<TReference> CaptureCurrentStates()
        {
            var capturedStates = new List<TReference>();
            
            foreach (var reference in GetValidReferences())
            {
                var capturedState = CreateStateCopy(reference);
                if (capturedState != null)
                {
                    capturedStates.Add(capturedState);
                }
            }
            
            return capturedStates;
        }
        
        /// <summary>
        /// Restaura estados desde una lista capturada
        /// </summary>
        /// <param name="capturedStates">Estados previamente capturados</param>
        public virtual void RestoreStates(List<TReference> capturedStates)
        {
            if (capturedStates == null)
            {
                return;
            }
            
            var restored = 0;
            
            foreach (var capturedState in capturedStates)
            {
                if (capturedState.IsValid)
                {
                    capturedState.Apply();
                    restored++;
                }
            }
            
            OnStatesRestored(restored, 0);
        }
        
        
        
        /// <summary>
        /// Obtiene solo las referencias válidas
        /// </summary>
        public List<TReference> GetValidReferences()
        {
            return _listManager.GetValidReferences();
        }
        
        /// <summary>
        /// Obtiene solo las referencias inválidas
        /// </summary>
        public List<TReference> GetInvalidReferences()
        {
            return _listManager.GetInvalidReferences();
        }
        
        /// <summary>
        /// Busca referencia por target
        /// </summary>
        public TReference FindByTarget(TTarget target)
        {
            return _listManager.FindByTarget(target);
        }
        
        /// <summary>
        /// Busca todas las referencias a un target específico
        /// </summary>
        public List<TReference> FindAllByTarget(TTarget target)
        {
            return _listManager.FindAllByTarget(target);
        }
        
        /// <summary>
        /// Verifica si contiene una referencia a un target específico
        /// </summary>
        public bool ContainsTarget(TTarget target)
        {
            return _listManager.ContainsTarget(target);
        }
        
        
        
        /// <summary>
        /// Valida todas las referencias usando el list manager
        /// </summary>
        public virtual ValidationResult ValidateAll()
        {
            var result = _listManager.Validate(GetManagerTypeName());
            
            var specificValidation = ValidateSpecific();
            if (specificValidation != null)
            {
                result.AddChild(specificValidation);
            }
            
            return result;
        }
        
        /// <summary>
        /// Validaciones específicas del tipo de manager
        /// </summary>
        protected virtual ValidationResult ValidateSpecific()
        {
            return null; // Implementación base vacía
        }
        
        
        
        /// <summary>
        /// Validación antes de añadir una referencia
        /// </summary>
        protected virtual bool ValidateBeforeAdd(TReference reference)
        {
            return reference.IsValid;
        }
        
        /// <summary>
        /// Crea una copia del estado actual de una referencia
        /// Debe ser implementado por cada manager especializado
        /// </summary>
        protected abstract TReference CreateStateCopy(TReference reference);
        
        /// <summary>
        /// Obtiene el nombre de display de una referencia para logging
        /// </summary>
        protected virtual string GetReferenceDisplayName(TReference reference)
        {
            return reference.Target != null ? reference.Target.name : "[Missing]";
        }
        
        /// <summary>
        /// Obtiene el nombre del tipo de manager para mensajes
        /// </summary>
        protected virtual string GetManagerTypeName()
        {
            return GetType().Name.Replace("Manager", "");
        }
        
        
        
        protected virtual void OnReferenceAdded(TReference reference) { }
        protected virtual void OnReferenceRemoved(TReference reference) { }
        protected virtual void OnReferencesRemovedByTarget(TTarget target, int count) { }
        protected virtual void OnAllReferencesCleared(int count) { }
        protected virtual void OnInvalidReferencesRemoved(int count) { }
        protected virtual void OnHierarchyPathsUpdated() { }
        protected virtual void OnCurrentStatesCaptured() { }
        protected virtual void OnStatesApplied(int applied, int failed) { }
        protected virtual void OnStatesRestored(int restored, int failed) { }
        
        
        
        
    }
    
}
