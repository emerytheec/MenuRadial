using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Validador centralizado para referencias comunes
    /// Responsabilidad única: Validaciones reutilizables
    /// Elimina duplicación de lógica de validación
    /// VERSIÓN SIMPLIFICADA para evitar errores de compilación
    /// </summary>
    public static class ReferenceValidator
    {
        /// <summary>
        /// Valida que un GameObject no sea null y exista en la escena
        /// </summary>
        /// <param name="gameObject">GameObject a validar</param>
        /// <param name="fieldName">Nombre del campo para mensajes</param>
        /// <returns>Resultado de validación</returns>
        public static ValidationResult ValidateGameObject(GameObject gameObject, string fieldName = "GameObject")
        {
            if (gameObject == null)
            {
                return ValidationResult.Error($"{fieldName} no puede ser null");
            }
            
            // Verificar que el objeto existe en la escena (no es un prefab desconectado)
            if (gameObject.scene.name == null)
            {
                return ValidationResult.Warning($"{fieldName} '{gameObject.name}' puede ser un prefab desconectado");
            }
            
            return ValidationResult.Success($"{fieldName} '{gameObject.name}' es válido");
        }
        
        /// <summary>
        /// Valida que un Component existe y está activo
        /// </summary>
        /// <param name="component">Component a validar</param>
        /// <param name="componentTypeName">Nombre del tipo de componente para mensajes</param>
        /// <returns>Resultado de validación</returns>
        public static ValidationResult ValidateComponent(Component component, string componentTypeName = "Component")
        {
            if (component == null)
            {
                return ValidationResult.Error($"{componentTypeName} no puede ser null");
            }
            
            if (component.gameObject == null)
            {
                return ValidationResult.Error($"{componentTypeName} tiene GameObject null");
            }
            
            return ValidationResult.Success($"{componentTypeName} en '{component.gameObject.name}' es válido");
        }
    }
}
