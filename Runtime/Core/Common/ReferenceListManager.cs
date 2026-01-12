using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Gestor genérico para listas de referencias
    /// Centraliza toda la lógica de gestión que antes estaba duplicada 3 veces
    /// Elimina ~200 líneas de código duplicado
    /// </summary>
    /// <typeparam name="TReference">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    /// <typeparam name="TTarget">Tipo del objeto objetivo (GameObject, Renderer, etc.)</typeparam>
    [Serializable]
    public class ReferenceListManager<TReference, TTarget> 
        where TReference : IReferenceBase<TTarget> 
        where TTarget : UnityEngine.Object
    {
        [SerializeField] private List<TReference> _references = new List<TReference>();
        
        /// <summary>
        /// Lista de referencias (acceso directo para serialización)
        /// </summary>
        public List<TReference> References => _references;
        
        /// <summary>
        /// Número total de referencias
        /// </summary>
        public int Count => _references.Count;
        
        /// <summary>
        /// Número de referencias válidas
        /// </summary>
        public int ValidCount => _references.Count(r => r.IsValid);
        
        /// <summary>
        /// Número de referencias inválidas
        /// </summary>
        public int InvalidCount => _references.Count(r => !r.IsValid);
        
        /// <summary>
        /// Indica si hay referencias válidas
        /// </summary>
        public bool HasValidReferences => ValidCount > 0;
        
        /// <summary>
        /// Indica si hay referencias inválidas
        /// </summary>
        public bool HasInvalidReferences => InvalidCount > 0;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ReferenceListManager()
        {
            _references = new List<TReference>();
        }
        
        /// <summary>
        /// Añade una referencia si no existe ya
        /// </summary>
        /// <param name="reference">Referencia a añadir</param>
        /// <returns>True si fue añadida, false si ya existía</returns>
        public bool Add(TReference reference)
        {
            if (reference == null || reference.Target == null)
            {
                return false;
            }
            
            // CORREGIDO: Para blendshapes, verificar duplicados de manera más específica
            if (IsDuplicateReference(reference))
            {
                return false;
            }
            
            _references.Add(reference);
            return true;
        }
        
        /// <summary>
        /// Verifica si una referencia es duplicada según el tipo específico
        /// NUEVO: Manejo inteligente de duplicados para diferentes tipos de referencias
        /// </summary>
        /// <param name="reference">Referencia a verificar</param>
        /// <returns>True si es duplicada</returns>
        private bool IsDuplicateReference(TReference reference)
        {
            // Para BlendshapeReference, verificar renderer + nombre del blendshape
            if (reference is BlendshapeReference blendRef)
            {
                return _references.Any(r => 
                    r is BlendshapeReference existingBlend &&
                    existingBlend.TargetRenderer == blendRef.TargetRenderer &&
                    existingBlend.BlendshapeName == blendRef.BlendshapeName);
            }
            
            // Para MaterialReference, verificar renderer + índice de material
            if (reference is MaterialReference matRef)
            {
                return _references.Any(r => 
                    r is MaterialReference existingMat &&
                    existingMat.TargetRenderer == matRef.TargetRenderer &&
                    existingMat.MaterialIndex == matRef.MaterialIndex);
            }
            
            // Para ObjectReference, verificar solo el GameObject (no se permiten duplicados)
            if (reference is ObjectReference objRef)
            {
                return _references.Any(r => 
                    r is ObjectReference existingObj &&
                    existingObj.GameObject == objRef.GameObject);
            }
            
            // Por defecto, usar la verificación original (solo target)
            return _references.Any(r => r.Target == reference.Target);
        }
        
        /// <summary>
        /// Elimina una referencia específica
        /// </summary>
        /// <param name="reference">Referencia a eliminar</param>
        /// <returns>True si fue eliminada</returns>
        public bool Remove(TReference reference)
        {
            if (reference == null) return false;
            
            bool removed = _references.Remove(reference);
            if (removed)
            {
                var targetName = reference.Target != null ? reference.Target.name : "[Unknown]";
            }
            
            return removed;
        }
        
        /// <summary>
        /// Elimina referencia por target
        /// </summary>
        /// <param name="target">Target a eliminar</param>
        /// <returns>Número de referencias eliminadas</returns>
        public int RemoveByTarget(TTarget target)
        {
            if (target == null) return 0;
            
            int removed = _references.RemoveAll(r => r.Target == target);
            if (removed > 0)
            {
            }
            
            return removed;
        }
        
        /// <summary>
        /// Elimina todas las referencias inválidas
        /// </summary>
        /// <returns>Número de referencias eliminadas</returns>
        public int RemoveInvalid()
        {
            int removed = _references.RemoveAll(r => !r.IsValid);
            if (removed > 0)
            {
            }
            
            return removed;
        }
        
        /// <summary>
        /// Limpia todas las referencias
        /// </summary>
        public void Clear()
        {
            int count = _references.Count;
            _references.Clear();
        }
        
        /// <summary>
        /// Verifica si contiene una referencia específica
        /// </summary>
        /// <param name="reference">Referencia a buscar</param>
        /// <returns>True si existe</returns>
        public bool Contains(TReference reference)
        {
            return _references.Contains(reference);
        }
        
        /// <summary>
        /// Verifica si contiene una referencia a un target específico
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>True si existe</returns>
        public bool ContainsTarget(TTarget target)
        {
            return _references.Any(r => r.Target == target);
        }
        
        /// <summary>
        /// Busca referencia por target
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>Primera referencia encontrada o default</returns>
        public TReference FindByTarget(TTarget target)
        {
            return _references.FirstOrDefault(r => r.Target == target);
        }
        
        /// <summary>
        /// Busca todas las referencias a un target específico
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>Lista de referencias</returns>
        public List<TReference> FindAllByTarget(TTarget target)
        {
            return _references.Where(r => r.Target == target).ToList();
        }
        
        /// <summary>
        /// Obtiene solo las referencias válidas
        /// </summary>
        /// <returns>Lista de referencias válidas</returns>
        public List<TReference> GetValidReferences()
        {
            return _references.Where(r => r.IsValid).ToList();
        }
        
        /// <summary>
        /// Obtiene solo las referencias inválidas
        /// </summary>
        /// <returns>Lista de referencias inválidas</returns>
        public List<TReference> GetInvalidReferences()
        {
            return _references.Where(r => !r.IsValid).ToList();
        }
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todas las referencias
        /// </summary>
        public void UpdateAllHierarchyPaths()
        {
            foreach (var reference in _references)
            {
                reference.UpdateHierarchyPath();
            }
            
        }
        
        /// <summary>
        /// Aplica todas las referencias válidas
        /// </summary>
        public void ApplyAll()
        {
            var validRefs = GetValidReferences();
            foreach (var reference in validRefs)
            {
                if (reference != null && reference.IsValid)
                {
                    reference.Apply();
                }
            }
        }
        
        /// <summary>
        /// Captura el estado actual de todas las referencias válidas
        /// </summary>
        public void CaptureAllCurrentStates()
        {
            var validRefs = GetValidReferences();
            foreach (var reference in validRefs)
            {
                if (reference != null && reference.IsValid)
                {
                    reference.CaptureCurrentState();
                }
            }
        }
        
        /// <summary>
        /// Valida todas las referencias y retorna resultado detallado
        /// </summary>
        /// <param name="typeName">Nombre del tipo para mensajes</param>
        /// <returns>Resultado de validación</returns>
        public ValidationResult Validate(string typeName = "Referencias")
        {
            var result = new ValidationResult();
            
            if (_references.Count == 0)
            {
                result.AddChild(ValidationResult.Info("No hay " + typeName.ToLower() + " en la lista"));
                return result;
            }
            
            // Estadísticas
            int validCount = ValidCount;
            int invalidCount = InvalidCount;
            
            if (validCount > 0)
            {
                result.AddChild(ValidationResult.Success(typeName + ": " + validCount + " válidas"));
            }
            
            if (invalidCount > 0)
            {
                result.AddChild(ValidationResult.Warning(typeName + ": " + invalidCount + " inválidas"));
            }
            
            // Detalles de referencias inválidas
            var invalidRefs = GetInvalidReferences();
            foreach (var invalidRef in invalidRefs)
            {
                var targetName = invalidRef.Target != null ? invalidRef.Target.name : "[Missing]";
                result.AddChild(ValidationResult.Error("Referencia inválida: " + targetName));
            }
            
            return result;
        }
        
        
        /// <summary>
        /// Representación como string 
        /// </summary>
        /// <returns>String descriptivo</returns>
        public override string ToString()
        {
            return "ReferenceListManager<" + typeof(TReference).Name + "> - Total: " + Count + ", Válidas: " + ValidCount + ", Inválidas: " + InvalidCount;
        }
    }
}
