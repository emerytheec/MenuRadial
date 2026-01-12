using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Core.Common.ReferenceList
{
    /// <summary>
    /// Validador especializado para listas de referencias
    /// FASE 2: Extraído de ReferenceListManager (80 líneas)
    /// Responsabilidad única: Solo validaciones y verificaciones de integridad
    /// </summary>
    /// <typeparam name="TReference">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    /// <typeparam name="TTarget">Tipo del objeto objetivo (GameObject, Renderer, etc.)</typeparam>
    public class ReferenceListValidator<TReference, TTarget>
        where TReference : IReferenceBase<TTarget> 
        where TTarget : UnityEngine.Object
    {
        private readonly List<TReference> _references;
        
        /// <summary>
        /// Constructor con inyección de dependencia de la lista de referencias
        /// </summary>
        /// <param name="references">Lista de referencias a validar</param>
        public ReferenceListValidator(List<TReference> references)
        {
            _references = references ?? throw new ArgumentNullException(nameof(references));
        }
        
        
        /// <summary>
        /// Número de referencias válidas
        /// </summary>
        public int ValidCount => _references.Count(r => r.IsValid);
        
        /// <summary>
        /// Número de referencias inválidas
        /// </summary>
        public int InvalidCount => _references.Count(r => !r.IsValid);
        
        /// <summary>
        /// Indica si todas las referencias son válidas
        /// </summary>
        public bool AllReferencesValid => _references.Count > 0 && _references.All(r => r.IsValid);
        
        /// <summary>
        /// Indica si hay referencias inválidas
        /// </summary>
        public bool HasInvalidReferences => _references.Any(r => !r.IsValid);
        
        /// <summary>
        /// Indica si hay referencias duplicadas
        /// </summary>
        public bool HasDuplicateReferences => GetDuplicateReferences().Count > 0;
        
        
        
        /// <summary>
        /// Verifica si una referencia es duplicada según el tipo específico
        /// MEJORADO: Manejo inteligente de duplicados para diferentes tipos de referencias
        /// </summary>
        /// <param name="reference">Referencia a verificar</param>
        /// <returns>True si es duplicada</returns>
        public bool IsDuplicateReference(TReference reference)
        {
            if (reference == null || reference.Target == null)
                return false;
            
            // Para BlendshapeReference, verificar renderer + nombre del blendshape
            if (reference is BlendshapeReference blendRef)
            {
                return _references.Any(r => 
                    r is BlendshapeReference existingBlend &&
                    !ReferenceEquals(r, reference) && // Excluir la misma instancia
                    existingBlend.TargetRenderer == blendRef.TargetRenderer &&
                    existingBlend.BlendshapeName == blendRef.BlendshapeName);
            }
            
            // Para MaterialReference, verificar renderer + índice de material
            if (reference is MaterialReference matRef)
            {
                return _references.Any(r => 
                    r is MaterialReference existingMat &&
                    !ReferenceEquals(r, reference) &&
                    existingMat.TargetRenderer == matRef.TargetRenderer &&
                    existingMat.MaterialIndex == matRef.MaterialIndex);
            }
            
            // Para ObjectReference, verificar solo el GameObject
            if (reference is ObjectReference objRef)
            {
                return _references.Any(r => 
                    r is ObjectReference existingObj &&
                    !ReferenceEquals(r, reference) &&
                    existingObj.GameObject == objRef.GameObject);
            }
            
            // Por defecto, usar la verificación de target
            return _references.Any(r => 
                !ReferenceEquals(r, reference) &&
                r.Target == reference.Target);
        }
        
        /// <summary>
        /// Obtiene todas las referencias duplicadas
        /// NUEVA funcionalidad: Identificación de duplicados
        /// </summary>
        /// <returns>Lista de referencias duplicadas</returns>
        public List<TReference> GetDuplicateReferences()
        {
            var duplicates = new List<TReference>();
            
            for (int i = 0; i < _references.Count; i++)
            {
                var reference = _references[i];
                if (reference == null || reference.Target == null) continue;
                
                // Buscar duplicados desde la posición siguiente
                for (int j = i + 1; j < _references.Count; j++)
                {
                    var otherReference = _references[j];
                    if (AreReferencesEquivalent(reference, otherReference))
                    {
                        if (!duplicates.Contains(reference))
                            duplicates.Add(reference);
                        if (!duplicates.Contains(otherReference))
                            duplicates.Add(otherReference);
                    }
                }
            }
            
            return duplicates;
        }
        
        /// <summary>
        /// Verifica si dos referencias son equivalentes (representan lo mismo)
        /// NUEVA funcionalidad: Comparación inteligente de referencias
        /// </summary>
        /// <param name="ref1">Primera referencia</param>
        /// <param name="ref2">Segunda referencia</param>
        /// <returns>True si son equivalentes</returns>
        private bool AreReferencesEquivalent(TReference ref1, TReference ref2)
        {
            if (ref1 == null || ref2 == null) return false;
            if (ReferenceEquals(ref1, ref2)) return false; // Misma instancia no es duplicado
            
            // Comparación específica por tipo
            if (ref1 is BlendshapeReference blend1 && ref2 is BlendshapeReference blend2)
            {
                return blend1.TargetRenderer == blend2.TargetRenderer &&
                       blend1.BlendshapeName == blend2.BlendshapeName;
            }
            
            if (ref1 is MaterialReference mat1 && ref2 is MaterialReference mat2)
            {
                return mat1.TargetRenderer == mat2.TargetRenderer &&
                       mat1.MaterialIndex == mat2.MaterialIndex;
            }
            
            if (ref1 is ObjectReference obj1 && ref2 is ObjectReference obj2)
            {
                return obj1.GameObject == obj2.GameObject;
            }
            
            // Comparación por defecto usando Target
            return ref1.Target == ref2.Target;
        }
        
        
        
        /// <summary>
        /// Valida todas las referencias y retorna resultado detallado
        /// MEJORADO: Validación más completa con análisis específico por tipo
        /// </summary>
        /// <param name="typeName">Nombre del tipo para mensajes</param>
        /// <returns>Resultado de validación detallado</returns>
        public ValidationResult Validate(string typeName = "Referencias")
        {
            var result = new ValidationResult();
            result.Message = $"Validación de {typeName}";
            result.IsValid = true;
            
            // Validar estado general
            result.AddChild(ValidateGeneralState(typeName));
            
            // Validar referencias específicas
            result.AddChild(ValidateSpecificReferences(typeName));
            
            // Validar duplicados
            result.AddChild(ValidateDuplicates(typeName));
            
            // Validar integridad
            result.AddChild(ValidateReferenceIntegrity(typeName));
            
            return result;
        }
        
        /// <summary>
        /// Valida el estado general de la lista
        /// </summary>
        /// <param name="typeName">Nombre del tipo</param>
        /// <returns>Resultado de validación del estado general</returns>
        private ValidationResult ValidateGeneralState(string typeName)
        {
            var result = new ValidationResult();
            result.Message = $"Estado general de {typeName}";
            
            if (_references.Count == 0)
            {
                result.IsValid = true;
                result.Message = $"No hay {typeName.ToLower()} en la lista";
                result.Severity = ValidationSeverity.Info;
                return result;
            }
            
            var validCount = ValidCount;
            var invalidCount = InvalidCount;
            
            if (validCount > 0 && invalidCount == 0)
            {
                result.IsValid = true;
                result.Message = $"Todas las {validCount} {typeName.ToLower()} son válidas";
                result.Severity = ValidationSeverity.Info;
            }
            else if (validCount > 0 && invalidCount > 0)
            {
                result.IsValid = false;
                result.Message = $"{validCount} {typeName.ToLower()} válidas, {invalidCount} inválidas";
                result.Severity = ValidationSeverity.Warning;
            }
            else
            {
                result.IsValid = false;
                result.Message = $"Todas las {invalidCount} {typeName.ToLower()} son inválidas";
                result.Severity = ValidationSeverity.Error;
            }
            
            return result;
        }
        
        /// <summary>
        /// Valida referencias específicas por tipo
        /// </summary>
        /// <param name="typeName">Nombre del tipo</param>
        /// <returns>Resultado de validación específica</returns>
        private ValidationResult ValidateSpecificReferences(string typeName)
        {
            var result = new ValidationResult();
            result.Message = $"Validación específica de {typeName}";
            result.IsValid = true;
            
            var typeValidations = new List<ValidationResult>();
            
            // Validaciones específicas por tipo de referencia
            if (typeof(TReference) == typeof(ObjectReference))
            {
                typeValidations.Add(ValidateObjectReferences());
            }
            else if (typeof(TReference) == typeof(MaterialReference))
            {
                typeValidations.Add(ValidateMaterialReferences());
            }
            else if (typeof(TReference) == typeof(BlendshapeReference))
            {
                typeValidations.Add(ValidateBlendshapeReferences());
            }
            
            foreach (var validation in typeValidations)
            {
                result.AddChild(validation);
            }
            
            return result;
        }
        
        /// <summary>
        /// Validación específica para ObjectReference
        /// </summary>
        /// <returns>Resultado de validación de objetos</returns>
        private ValidationResult ValidateObjectReferences()
        {
            var result = new ValidationResult();
            result.Message = "Validación de referencias de objetos";
            result.IsValid = true;
            
            var objectRefs = _references.OfType<ObjectReference>().ToList();
            var inactiveObjects = objectRefs.Count(r => r.IsValid && !r.GameObject.activeInHierarchy);
            
            if (inactiveObjects > 0)
            {
                result.AddChild(ValidationResult.Info($"{inactiveObjects} objetos están inactivos en la jerarquía"));
            }
            
            return result;
        }
        
        /// <summary>
        /// Validación específica para MaterialReference
        /// </summary>
        /// <returns>Resultado de validación de materiales</returns>
        private ValidationResult ValidateMaterialReferences()
        {
            var result = new ValidationResult();
            result.Message = "Validación de referencias de materiales";
            result.IsValid = true;
            
            var materialRefs = _references.OfType<MaterialReference>().ToList();
            var missingAlternatives = materialRefs.Count(r => r.IsValid && r.AlternativeMaterial == null);
            
            if (missingAlternatives > 0)
            {
                result.AddChild(ValidationResult.Warning($"{missingAlternatives} materiales sin alternativo definido"));
            }
            
            return result;
        }
        
        /// <summary>
        /// Validación específica para BlendshapeReference
        /// </summary>
        /// <returns>Resultado de validación de blendshapes</returns>
        private ValidationResult ValidateBlendshapeReferences()
        {
            var result = new ValidationResult();
            result.Message = "Validación de referencias de blendshapes";
            result.IsValid = true;
            
            var blendRefs = _references.OfType<BlendshapeReference>().ToList();
            var extremeValues = blendRefs.Count(r => r.IsValid && (r.Value == 0f || r.Value == 100f));
            
            if (extremeValues > 0)
            {
                result.AddChild(ValidationResult.Info($"{extremeValues} blendshapes con valores extremos (0 o 100)"));
            }
            
            return result;
        }
        
        /// <summary>
        /// Valida la presencia de duplicados
        /// </summary>
        /// <param name="typeName">Nombre del tipo</param>
        /// <returns>Resultado de validación de duplicados</returns>
        private ValidationResult ValidateDuplicates(string typeName)
        {
            var result = new ValidationResult();
            result.Message = $"Duplicados en {typeName}";
            
            var duplicates = GetDuplicateReferences();
            
            if (duplicates.Count == 0)
            {
                result.IsValid = true;
                result.Message = $"No hay {typeName.ToLower()} duplicadas";
                result.Severity = ValidationSeverity.Info;
            }
            else
            {
                result.IsValid = false;
                result.Message = $"Se encontraron {duplicates.Count} {typeName.ToLower()} duplicadas";
                result.Severity = ValidationSeverity.Warning;
                
                // Agregar detalles de duplicados
                foreach (var duplicate in duplicates)
                {
                    var displayName = GetReferenceDisplayName(duplicate);
                    result.AddChild(ValidationResult.Warning($"Duplicado: {displayName}"));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Valida la integridad de las referencias
        /// </summary>
        /// <param name="typeName">Nombre del tipo</param>
        /// <returns>Resultado de validación de integridad</returns>
        private ValidationResult ValidateReferenceIntegrity(string typeName)
        {
            var result = new ValidationResult();
            result.Message = $"Integridad de {typeName}";
            result.IsValid = true;
            
            // Contar referencias con problemas específicos
            var nullTargets = _references.Count(r => r != null && r.Target == null);
            var nullReferences = _references.Count(r => r == null);
            
            if (nullReferences > 0)
            {
                result.AddChild(ValidationResult.Error($"{nullReferences} referencias null"));
                result.IsValid = false;
            }
            
            if (nullTargets > 0)
            {
                result.AddChild(ValidationResult.Error($"{nullTargets} referencias con target null"));
                result.IsValid = false;
            }
            
            if (result.IsValid)
            {
                result.Message = $"Integridad de {typeName} correcta";
                result.Severity = ValidationSeverity.Info;
            }
            
            return result;
        }
        
        
        
        /// <summary>
        /// Elimina automáticamente las referencias duplicadas
        /// NUEVA funcionalidad: Auto-corrección de duplicados
        /// </summary>
        /// <returns>Número de duplicados eliminados</returns>
        public int RemoveDuplicates()
        {
            var duplicates = GetDuplicateReferences();
            var removed = 0;
            
            // Eliminar duplicados manteniendo la primera ocurrencia
            var seen = new HashSet<string>();
            
            for (int i = _references.Count - 1; i >= 0; i--)
            {
                var reference = _references[i];
                if (reference == null || reference.Target == null) continue;
                
                var identifier = GetReferenceIdentifier(reference);
                
                if (seen.Contains(identifier))
                {
                    _references.RemoveAt(i);
                    removed++;
                }
                else
                {
                    seen.Add(identifier);
                }
            }
            
            if (removed > 0)
            {
            }
            
            return removed;
        }
        
        /// <summary>
        /// Elimina referencias null o con target null
        /// NUEVA funcionalidad: Limpieza automática
        /// </summary>
        /// <returns>Número de referencias null eliminadas</returns>
        public int RemoveNullReferences()
        {
            var removed = _references.RemoveAll(r => r == null || r.Target == null);
            
            if (removed > 0)
            {
            }
            
            return removed;
        }
        
        /// <summary>
        /// Repara automáticamente referencias inválidas cuando es posible
        /// NUEVA funcionalidad: Auto-reparación
        /// </summary>
        /// <returns>Número de referencias reparadas</returns>
        public int RepairInvalidReferences()
        {
            var repaired = 0;
            
            foreach (var reference in _references.Where(r => r != null && !r.IsValid).ToList())
            {
                // Intentar actualizar la ruta jerárquica
                reference.UpdateHierarchyPath();
                
                // Verificar si ahora es válida
                if (reference.IsValid)
                {
                    repaired++;
                }
            }
            
            if (repaired > 0)
            {
            }
            
            return repaired;
        }
        
        
        
        /// <summary>
        /// Obtiene un identificador único para una referencia
        /// NUEVA funcionalidad: Identificación única de referencias
        /// </summary>
        /// <param name="reference">Referencia a identificar</param>
        /// <returns>Identificador único</returns>
        private string GetReferenceIdentifier(TReference reference)
        {
            if (reference == null || reference.Target == null)
                return "null";
            
            // Identificador específico por tipo
            if (reference is BlendshapeReference blendRef)
                return $"blend_{blendRef.TargetRenderer?.GetInstanceID()}_{blendRef.BlendshapeName}";
            
            if (reference is MaterialReference matRef)
                return $"mat_{matRef.TargetRenderer?.GetInstanceID()}_{matRef.MaterialIndex}";
            
            if (reference is ObjectReference objRef)
                return $"obj_{objRef.GameObject?.GetInstanceID()}";
            
            // Por defecto, usar el target
            return $"ref_{reference.Target.GetInstanceID()}";
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
