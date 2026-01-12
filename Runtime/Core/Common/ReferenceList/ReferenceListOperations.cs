using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Core.Common.ReferenceList
{
    /// <summary>
    /// Operaciones avanzadas especializadas para listas de referencias
    /// FASE 2: Extraído de ReferenceListManager (100 líneas)
    /// Responsabilidad única: Solo operaciones avanzadas de búsqueda, filtrado y manipulación
    /// </summary>
    /// <typeparam name="TReference">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    /// <typeparam name="TTarget">Tipo del objeto objetivo (GameObject, Renderer, etc.)</typeparam>
    public class ReferenceListOperations<TReference, TTarget>
        where TReference : IReferenceBase<TTarget> 
        where TTarget : UnityEngine.Object
    {
        private readonly List<TReference> _references;
        
        /// <summary>
        /// Constructor con inyección de dependencia de la lista de referencias
        /// </summary>
        /// <param name="references">Lista de referencias a gestionar</param>
        public ReferenceListOperations(List<TReference> references)
        {
            _references = references ?? throw new ArgumentNullException(nameof(references));
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
        /// Verifica si contiene una referencia a un target específico
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>True si existe</returns>
        public bool ContainsTarget(TTarget target)
        {
            if (target == null) return false;
            return _references.Any(r => r.Target == target);
        }
        
        /// <summary>
        /// Busca referencia por target
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>Primera referencia encontrada o default</returns>
        public TReference FindByTarget(TTarget target)
        {
            if (target == null) return default(TReference);
            return _references.FirstOrDefault(r => r.Target == target);
        }
        
        /// <summary>
        /// Busca todas las referencias a un target específico
        /// </summary>
        /// <param name="target">Target a buscar</param>
        /// <returns>Lista de referencias</returns>
        public List<TReference> FindAllByTarget(TTarget target)
        {
            if (target == null) return new List<TReference>();
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
        /// Obtiene referencias filtradas por un predicado personalizado
        /// NUEVA funcionalidad: Filtrado genérico avanzado
        /// </summary>
        /// <param name="predicate">Función de filtrado</param>
        /// <returns>Lista de referencias filtradas</returns>
        public List<TReference> GetReferencesWhere(Func<TReference, bool> predicate)
        {
            if (predicate == null) return new List<TReference>();
            return _references.Where(predicate).ToList();
        }
        
        /// <summary>
        /// Cuenta referencias que cumplen un predicado
        /// NUEVA funcionalidad: Conteo condicional
        /// </summary>
        /// <param name="predicate">Función de filtrado</param>
        /// <returns>Número de referencias que cumplen la condición</returns>
        public int CountWhere(Func<TReference, bool> predicate)
        {
            if (predicate == null) return 0;
            return _references.Count(predicate);
        }
        
        
        
        /// <summary>
        /// Actualiza las rutas jerárquicas de todas las referencias
        /// </summary>
        public void UpdateAllHierarchyPaths()
        {
            if (_references == null) return;
            
            foreach (var reference in _references)
            {
                if (reference != null && reference.IsValid)
                {
                    reference.UpdateHierarchyPath();
                }
            }
        }
        
        /// <summary>
        /// Aplica todas las referencias válidas
        /// </summary>
        public void ApplyAll()
        {
            var validRefs = GetValidReferences();
            if (validRefs == null) return;
            
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
            if (validRefs == null) return;
            
            foreach (var reference in validRefs)
            {
                if (reference != null && reference.IsValid)
                {
                    reference.CaptureCurrentState();
                }
            }
        }
        
        /// <summary>
        /// Ejecuta una acción en todas las referencias válidas
        /// NUEVA funcionalidad: Operación personalizada en lote
        /// </summary>
        /// <param name="action">Acción a ejecutar</param>
        /// <param name="actionName">Nombre de la acción para logging</param>
        /// <returns>Número de referencias procesadas exitosamente</returns>
        public int ExecuteOnValidReferences(Action<TReference> action, string actionName = "Operación")
        {
            if (action == null) return 0;
            
            var validRefs = GetValidReferences();
            if (validRefs == null) return 0;
            
            var processed = 0;
            foreach (var reference in validRefs)
            {
                if (reference != null && reference.IsValid)
                {
                    action(reference);
                    processed++;
                }
            }
            
            return processed;
        }
        
        
        
        /// <summary>
        /// Agrupa las referencias por tipo de target
        /// NUEVA funcionalidad: Agrupación avanzada
        /// </summary>
        /// <returns>Diccionario con referencias agrupadas por tipo</returns>
        public Dictionary<System.Type, List<TReference>> GroupByTargetType()
        {
            return _references
                .Where(r => r.Target != null)
                .GroupBy(r => r.Target.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>
        /// Agrupa las referencias por validez
        /// NUEVA funcionalidad: Agrupación por estado
        /// </summary>
        /// <returns>Diccionario con referencias agrupadas por validez</returns>
        public Dictionary<bool, List<TReference>> GroupByValidity()
        {
            return _references
                .GroupBy(r => r.IsValid)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        
        /// <summary>
        /// Ordena las referencias por nombre del target
        /// NUEVA funcionalidad: Ordenamiento
        /// </summary>
        /// <returns>Lista ordenada de referencias</returns>
        public List<TReference> GetReferencesSortedByTargetName()
        {
            return _references
                .Where(r => r.Target != null)
                .OrderBy(r => r.Target.name)
                .ToList();
        }
        
        
        
        
        
        
        /// <summary>
        /// Obtiene el nombre de display de una referencia para logging
        /// </summary>
        /// <param name="reference">Referencia a procesar</param>
        /// <returns>Nombre descriptivo</returns>
        private string GetReferenceDisplayName(TReference reference)
        {
            if (reference == null) return "[Null Reference]";
            if (reference.Target == null) return "[Missing Target]";
            
            // Personalizar según tipo de referencia
            if (reference is BlendshapeReference blendRef)
                return $"{blendRef.TargetRenderer?.name ?? "[Missing]"}.{blendRef.BlendshapeName}";
            
            if (reference is MaterialReference matRef)
                return $"{matRef.TargetRenderer?.name ?? "[Missing]"}[{matRef.MaterialIndex}]";
            
            return reference.Target.name;
        }
        
        
        
    }
    
}
