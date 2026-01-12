using UnityEngine;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Validador especializado para MRAgruparObjetos
    /// EXTRAE: ~150 líneas de lógica de validación de MRAgruparObjetos
    /// Responsabilidad única: Solo validaciones
    /// </summary>
    public static class MRAgruparObjetosValidator
    {
        /// <summary>
        /// Valida completamente un MRAgruparObjetos
        /// </summary>
        public static ValidationResult ValidateFrameObject(MRAgruparObjetos frameObject)
        {
            var result = new ValidationResult();
            
            if (frameObject == null)
            {
                result.AddChild(ValidationResult.Error("MRAgruparObjetos no puede ser null"));
                return result;
            }
            
            // Validar FrameData
            result.AddChild(ValidateFrameData(frameObject.FrameData));
            
            // Validar referencias específicas
            result.AddChild(ValidateObjectReferences(frameObject.ObjectReferences));
            result.AddChild(ValidateMaterialReferences(frameObject.MaterialReferences));
            result.AddChild(ValidateBlendshapeReferences(frameObject.BlendshapeReferences));
            
            // Validar estado de preview
            result.AddChild(ValidatePreviewState(frameObject));
            
            return result;
        }
        
        private static ValidationResult ValidateFrameData(FrameData frameData)
        {
            if (frameData == null)
                return ValidationResult.Error("FrameData no puede ser null");
                
            if (string.IsNullOrEmpty(frameData.Name))
                return ValidationResult.Warning("Frame sin nombre asignado");
                
            return ValidationResult.Success($"FrameData '{frameData.Name}' válido");
        }
        
        private static ValidationResult ValidateObjectReferences(System.Collections.Generic.List<ObjectReference> references)
        {
            var result = new ValidationResult();
            var validCount = 0;
            var invalidCount = 0;
            
            foreach (var objRef in references)
            {
                if (objRef.IsValid)
                    validCount++;
                else
                    invalidCount++;
            }
            
            if (validCount > 0)
                result.AddChild(ValidationResult.Success($"Objetos válidos: {validCount}"));
            if (invalidCount > 0)
                result.AddChild(ValidationResult.Warning($"Objetos inválidos: {invalidCount}"));
                
            return result;
        }
        
        private static ValidationResult ValidateMaterialReferences(System.Collections.Generic.List<MaterialReference> references)
        {
            var result = new ValidationResult();
            var validCount = 0;
            var invalidCount = 0;
            var withoutAlternative = 0;
            
            foreach (var matRef in references)
            {
                if (matRef.IsValid)
                {
                    validCount++;
                    if (!matRef.HasAlternativeMaterial)
                        withoutAlternative++;
                }
                else
                {
                    invalidCount++;
                }
            }
            
            if (validCount > 0)
                result.AddChild(ValidationResult.Success($"Materiales válidos: {validCount}"));
            if (invalidCount > 0)
                result.AddChild(ValidationResult.Warning($"Materiales inválidos: {invalidCount}"));
            if (withoutAlternative > 0)
                result.AddChild(ValidationResult.Info($"Materiales sin alternativo: {withoutAlternative}"));
                
            return result;
        }
        
        private static ValidationResult ValidateBlendshapeReferences(System.Collections.Generic.List<BlendshapeReference> references)
        {
            var result = new ValidationResult();
            var validCount = 0;
            var invalidCount = 0;
            var outOfRange = 0;
            
            foreach (var blendRef in references)
            {
                if (blendRef.IsValid)
                {
                    validCount++;
                    if (blendRef.Value < 0f || blendRef.Value > 100f)
                        outOfRange++;
                }
                else
                {
                    invalidCount++;
                }
            }
            
            if (validCount > 0)
                result.AddChild(ValidationResult.Success($"Blendshapes válidos: {validCount}"));
            if (invalidCount > 0)
                result.AddChild(ValidationResult.Warning($"Blendshapes inválidos: {invalidCount}"));
            if (outOfRange > 0)
                result.AddChild(ValidationResult.Warning($"Blendshapes fuera de rango 0-100: {outOfRange}"));
                
            return result;
        }
        
        private static ValidationResult ValidatePreviewState(MRAgruparObjetos frameObject)
        {
            if (frameObject.IsPreviewActive)
            {
                return ValidationResult.Info("Preview activo - estados guardados");
            }
            return ValidationResult.Success("Preview inactivo - listo para usar");
        }
    }
}