using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    
    /// <summary>
    /// Interface base para reglas de validación
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Valida un objeto
        /// </summary>
        /// <param name="target">Objeto a validar</param>
        /// <returns>Resultado de validación</returns>
        ValidationResult Validate(object target);
        
        /// <summary>
        /// Nombre descriptivo de la regla
        /// </summary>
        string RuleName { get; }
        
        /// <summary>
        /// Puede auto-corregir el problema detectado
        /// </summary>
        bool CanAutoFix { get; }
        
        /// <summary>
        /// Intenta auto-corregir el problema
        /// </summary>
        /// <param name="target">Objeto a corregir</param>
        /// <returns>True si se pudo corregir</returns>
        bool TryAutoFix(object target);
    }
    
    /// <summary>
    /// Interface genérica para reglas de validación con tipo específico
    /// </summary>
    /// <typeparam name="T">Tipo de objeto que valida</typeparam>
    public interface IValidationRule<T> : IValidationRule
    {
        /// <summary>
        /// Valida un objeto del tipo específico
        /// </summary>
        /// <param name="target">Objeto a validar</param>
        /// <returns>Resultado de validación</returns>
        ValidationResult Validate(T target);
    }
    
    
    
    /// <summary>
    /// Clase base para reglas de validación
    /// </summary>
    /// <typeparam name="T">Tipo de objeto que valida</typeparam>
    public abstract class ValidationRuleBase<T> : IValidationRule<T>
    {
        public abstract string RuleName { get; }
        public virtual bool CanAutoFix => false;
        
        public ValidationResult Validate(object target)
        {
            if (target == null)
                return ValidationResult.Error($"Target es null para regla {RuleName}");
            
            if (!(target is T typedTarget))
                return ValidationResult.Error($"Target no es del tipo esperado {typeof(T).Name} para regla {RuleName}");
            
            return Validate(typedTarget);
        }
        
        public abstract ValidationResult Validate(T target);
        
        public virtual bool TryAutoFix(object target)
        {
            return false; // Por defecto no hay auto-fix
        }
    }
    
    
    
    /// <summary>
    /// Regla que verifica que objetos Unity no sean null
    /// </summary>
    public class UnityObjectNullRule : ValidationRuleBase<UnityEngine.Object>
    {
        public override string RuleName => "UnityObjectNull";
        
        public override ValidationResult Validate(UnityEngine.Object target)
        {
            if (target == null)
                return ValidationResult.Error("Objeto Unity es null");
            
            return ValidationResult.Success($"Objeto Unity '{target.name}' válido");
        }
    }
    
    /// <summary>
    /// Regla que verifica que objetos Unity no estén destruidos
    /// </summary>
    public class UnityObjectDestroyedRule : ValidationRuleBase<UnityEngine.Object>
    {
        public override string RuleName => "UnityObjectDestroyed";
        
        public override ValidationResult Validate(UnityEngine.Object target)
        {
            if (target == null) // Unity's null check
                return ValidationResult.Error("Objeto Unity está destruido");
            
            return ValidationResult.Success($"Objeto Unity '{target.name}' activo");
        }
    }
    
    
    
    /// <summary>
    /// Regla que verifica el estado activo de GameObjects
    /// </summary>
    public class GameObjectActiveRule : ValidationRuleBase<GameObject>
    {
        public override string RuleName => "GameObjectActive";
        public override bool CanAutoFix => true;
        
        public override ValidationResult Validate(GameObject target)
        {
            if (target == null)
                return ValidationResult.Error("GameObject es null");
            
            if (!target.activeInHierarchy)
                return ValidationResult.Warning($"GameObject '{target.name}' está inactivo en jerarquía");
            
            if (!target.activeSelf)
                return ValidationResult.Info($"GameObject '{target.name}' está inactivo pero activo en jerarquía");
            
            return ValidationResult.Success($"GameObject '{target.name}' activo");
        }
        
        public override bool TryAutoFix(object target)
        {
            if (target is GameObject go && go != null)
            {
                go.SetActive(true);
                return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// Regla que verifica nombres válidos de GameObjects
    /// </summary>
    public class GameObjectNameRule : ValidationRuleBase<GameObject>
    {
        public override string RuleName => "GameObjectName";
        public override bool CanAutoFix => true;
        
        public override ValidationResult Validate(GameObject target)
        {
            if (target == null)
                return ValidationResult.Error("GameObject es null");
            
            if (string.IsNullOrEmpty(target.name))
                return ValidationResult.Error($"GameObject tiene nombre vacío");
            
            if (target.name.Trim() != target.name)
                return ValidationResult.Warning($"GameObject '{target.name}' tiene espacios al inicio/final");
            
            if (target.name.Contains("(Clone)"))
                return ValidationResult.Info($"GameObject '{target.name}' es un clon");
            
            return ValidationResult.Success($"GameObject '{target.name}' tiene nombre válido");
        }
        
        public override bool TryAutoFix(object target)
        {
            if (target is GameObject go && go != null)
            {
                if (string.IsNullOrEmpty(go.name))
                {
                    go.name = "GameObject";
                    return true;
                }
                
                if (go.name.Trim() != go.name)
                {
                    go.name = go.name.Trim();
                    return true;
                }
            }
            return false;
        }
    }
    
    
    
    /// <summary>
    /// Regla que verifica que componentes tengan GameObject válido
    /// </summary>
    public class ComponentValidRule : ValidationRuleBase<Component>
    {
        public override string RuleName => "ComponentValid";
        
        public override ValidationResult Validate(Component target)
        {
            if (target == null)
                return ValidationResult.Error("Component es null");
            
            if (target.gameObject == null)
                return ValidationResult.Error($"Component '{target.GetType().Name}' tiene GameObject null");
            
            if (!target.gameObject.activeInHierarchy)
                return ValidationResult.Warning($"Component '{target.GetType().Name}' en GameObject inactivo");
            
            return ValidationResult.Success($"Component '{target.GetType().Name}' válido");
        }
    }
    
    
    
    /// <summary>
    /// Regla que verifica que strings no estén vacíos
    /// </summary>
    public class StringNotEmptyRule : ValidationRuleBase<string>
    {
        public override string RuleName => "StringNotEmpty";
        
        public override ValidationResult Validate(string target)
        {
            if (target == null)
                return ValidationResult.Error("String es null");
            
            if (string.IsNullOrEmpty(target))
                return ValidationResult.Error("String está vacío");
            
            if (string.IsNullOrWhiteSpace(target))
                return ValidationResult.Warning("String solo contiene espacios");
            
            return ValidationResult.Success("String válido");
        }
    }
    
    /// <summary>
    /// Regla que verifica que strings representen rutas válidas
    /// </summary>
    public class StringValidPathRule : ValidationRuleBase<string>
    {
        public override string RuleName => "StringValidPath";
        
        public override ValidationResult Validate(string target)
        {
            if (target == null)
                return ValidationResult.Error("Path string es null");
            
            if (string.IsNullOrEmpty(target))
                return ValidationResult.Error("Path está vacío");
            
            // Verificar caracteres inválidos para rutas
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (target.Any(c => invalidChars.Contains(c)))
                return ValidationResult.Error($"Path contiene caracteres inválidos: '{target}'");
            
            // Verificar formato de path Unity
            if (target.StartsWith("Assets/"))
                return ValidationResult.Success($"Path Unity válido: '{target}'");
            
            if (System.IO.Path.IsPathRooted(target))
                return ValidationResult.Info($"Path absoluto: '{target}'");
            
            return ValidationResult.Warning($"Path relativo: '{target}'");
        }
    }
    
    
    
    /// <summary>
    /// Regla que verifica que colecciones no estén vacías
    /// </summary>
    public class CollectionNotEmptyRule : ValidationRuleBase<ICollection>
    {
        public override string RuleName => "CollectionNotEmpty";
        
        public override ValidationResult Validate(ICollection target)
        {
            if (target == null)
                return ValidationResult.Error("Colección es null");
            
            if (target.Count == 0)
                return ValidationResult.Warning("Colección está vacía");
            
            return ValidationResult.Success($"Colección con {target.Count} elementos");
        }
    }
    
    /// <summary>
    /// Regla que verifica que elementos de colecciones sean válidos
    /// </summary>
    public class CollectionValidElementsRule : ValidationRuleBase<ICollection>
    {
        public override string RuleName => "CollectionValidElements";
        
        public override ValidationResult Validate(ICollection target)
        {
            if (target == null)
                return ValidationResult.Error("Colección es null");
            
            var results = new List<ValidationResult>();
            var nullCount = 0;
            var validCount = 0;
            
            foreach (var item in target)
            {
                if (item == null)
                {
                    nullCount++;
                }
                else
                {
                    validCount++;
                    
                    // Si es Unity Object, verificar que no esté destruido
                    if (item is UnityEngine.Object unityObj && unityObj == null)
                    {
                        results.Add(ValidationResult.Warning($"Elemento Unity destruido en colección"));
                    }
                }
            }
            
            if (nullCount > 0)
            {
                results.Add(ValidationResult.Warning($"Colección tiene {nullCount} elementos null"));
            }
            
            if (validCount == 0 && target.Count > 0)
            {
                results.Add(ValidationResult.Error("Colección no tiene elementos válidos"));
            }
            else if (validCount > 0)
            {
                results.Add(ValidationResult.Success($"Colección con {validCount} elementos válidos"));
            }
            
            return ValidationResult.Combine(results);
        }
    }
    
    
    
    /// <summary>
    /// Regla específica para validar objetos IFrameComponent
    /// </summary>
    public class FrameComponentRule : ValidationRuleBase<IFrameComponent>
    {
        public override string RuleName => "FrameComponent";
        
        public override ValidationResult Validate(IFrameComponent target)
        {
            if (target == null)
                return ValidationResult.Error("IFrameComponent es null");
            
            var results = new List<ValidationResult>();
            
            // Validar frames
            if (target.Frames == null)
            {
                results.Add(ValidationResult.Error("Frames collection es null"));
            }
            else if (target.Frames.Count == 0)
            {
                results.Add(ValidationResult.Warning("No hay frames configurados"));
            }
            else
            {
                results.Add(ValidationResult.Success($"{target.Frames.Count} frames configurados"));
                
                // Validar ActiveFrameIndex
                if (target.ActiveFrameIndex < 0 || target.ActiveFrameIndex >= target.Frames.Count)
                {
                    results.Add(ValidationResult.Error($"ActiveFrameIndex {target.ActiveFrameIndex} fuera de rango [0-{target.Frames.Count - 1}]"));
                }
            }
            
            return ValidationResult.Combine(results);
        }
    }
    
    /// <summary>
    /// Regla específica para validar objetos IAnimationProvider
    /// </summary>
    public class AnimationProviderRule : ValidationRuleBase<IAnimationProvider>
    {
        public override string RuleName => "AnimationProvider";
        
        public override ValidationResult Validate(IAnimationProvider target)
        {
            if (target == null)
                return ValidationResult.Error("IAnimationProvider es null");
            
            var results = new List<ValidationResult>();
            
            // Validar AnimationName
            if (string.IsNullOrEmpty(target.AnimationName))
            {
                results.Add(ValidationResult.Error("AnimationName está vacío"));
            }
            else if (target.AnimationName.Contains(" "))
            {
                results.Add(ValidationResult.Warning($"AnimationName contiene espacios: '{target.AnimationName}'"));
            }
            else
            {
                results.Add(ValidationResult.Success($"AnimationName válido: '{target.AnimationName}'"));
            }
            
            // Validar CanGenerateAnimation
            if (!target.CanGenerateAnimation)
            {
                results.Add(ValidationResult.Warning("Componente no está listo para generar animaciones"));
            }
            else
            {
                results.Add(ValidationResult.Success("Componente listo para generar animaciones"));
            }
            
            // Validar AnimationType con validación defensiva
            if (target.AnimationType != null)
            {
                var animationType = target.AnimationType;
                var description = target.GetAnimationTypeDescription();
                
                if (!string.IsNullOrEmpty(description))
                {
                    results.Add(ValidationResult.Success($"Tipo de animación: {animationType} - {description}"));
                }
                else
                {
                    results.Add(ValidationResult.Warning($"Tipo de animación: {animationType} - sin descripción"));
                }
            }
            else
            {
                results.Add(ValidationResult.Warning("AnimationType no está definido"));
            }
            
            return ValidationResult.Combine(results);
        }
    }
    
}
