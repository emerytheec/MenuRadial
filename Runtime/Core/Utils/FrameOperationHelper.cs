using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Helper para operaciones comunes de Frame - CORREGIDO para compatibilidad de tipos
    /// ELIMINA: Duplicación real en operaciones cotidianas
    /// </summary>
    public static class FrameOperationHelper
    {
        /// <summary>
        /// Cuenta referencias válidas en cualquier lista - GENÉRICO FLEXIBLE
        /// USO: En lugar de escribir 3 veces el mismo foreach
        /// </summary>
        public static (int valid, int invalid) CountValidReferences<T>(IEnumerable<T> references) 
            where T : Common.IReferenceBase<UnityEngine.Object>
        {
            if (references == null) return (0, 0);
            
            var valid = 0;
            var invalid = 0;
            
            foreach (var reference in references)
            {
                if (reference.IsValid)
                    valid++;
                else
                    invalid++;
            }
            
            return (valid, invalid);
        }
        
        /// <summary>
        /// Cuenta referencias válidas - SOBRECARGA para ObjectReference específicamente
        /// </summary>
        public static (int valid, int invalid) CountValidObjectReferences(IEnumerable<Common.ObjectReference> references)
        {
            if (references == null) return (0, 0);
            
            var valid = 0;
            var invalid = 0;
            
            foreach (var reference in references)
            {
                if (reference.IsValid)
                    valid++;
                else
                    invalid++;
            }
            
            return (valid, invalid);
        }
        
        /// <summary>
        /// Genera mensaje de estadísticas estándar
        /// USO: Consistencia en todos los managers
        /// </summary>
        public static string GenerateStatsMessage(string itemType, int valid, int invalid)
        {
            if (valid == 0 && invalid == 0)
                return $"No hay {itemType.ToLower()}";
            
            if (invalid == 0)
                return $"{itemType}: {valid} válidos";
            
            if (valid == 0)
                return $"{itemType}: {invalid} inválidos";
                
            return $"{itemType}: {valid} válidos, {invalid} inválidos";
        }
        
        /// <summary>
        /// Valida que un objeto tenga componente específico
        /// USO: En drag & drop validation
        /// </summary>
        public static bool HasComponent<T>(GameObject obj) where T : Component
        {
            return obj != null && obj.GetComponent<T>() != null;
        }
        
        /// <summary>
        /// Limpia lista de referencias inválidas - GENÉRICO
        /// USO: Operación común en todos los managers
        /// </summary>
        public static int RemoveInvalidReferences<T>(List<T> references) 
            where T : Common.IReferenceBase<UnityEngine.Object>
        {
            if (references == null) return 0;
            
            var removed = references.RemoveAll(r => !r.IsValid);
            
            return removed;
        }
        
        /// <summary>
        /// Limpia lista de ObjectReference inválidas - ESPECÍFICO
        /// </summary>
        public static int RemoveInvalidObjectReferences(List<Common.ObjectReference> references)
        {
            if (references == null) return 0;
            
            var removed = references.RemoveAll(r => !r.IsValid);
            
            return removed;
        }
        
        /// <summary>
        /// Actualiza rutas jerárquicas de una lista - GENÉRICO
        /// USO: Operación común cuando objetos se mueven en hierarchy
        /// </summary>
        public static void UpdateHierarchyPaths<T>(IEnumerable<T> references) 
            where T : Common.IReferenceBase<UnityEngine.Object>
        {
            if (references == null) return;
            
            var count = 0;
            foreach (var reference in references)
            {
                reference.UpdateHierarchyPath();
                count++;
            }
            
        }
        
        /// <summary>
        /// Actualiza rutas jerárquicas para ObjectReference - ESPECÍFICO
        /// </summary>
        public static void UpdateObjectHierarchyPaths(IEnumerable<Common.ObjectReference> references)
        {
            if (references == null) return;
            
            var count = 0;
            foreach (var reference in references)
            {
                reference.UpdateHierarchyPath();
                count++;
            }
            
        }
    }
}