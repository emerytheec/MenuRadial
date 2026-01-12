using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Validador especializado para menús radiales
    /// REFACTORIZADO: Extraído de MRUnificarObjetos.cs para responsabilidad única
    /// Versión: 0.031 - Validador independiente
    /// </summary>
    public class RadialMenuValidator
    {
        // Private Fields
        private readonly System.Collections.Generic.List<MRAgruparObjetos> _frames;
        private readonly string _animationName;
        private readonly string _animationPath;
        
        // Constructor
        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="frames">Lista de frames para validar</param>
        /// <param name="animationName">Nombre de la animación</param>
        /// <param name="animationPath">Ruta de animaciones</param>
        public RadialMenuValidator(System.Collections.Generic.List<MRAgruparObjetos> frames, string animationName, string animationPath)
        {
            _frames = frames ?? throw new System.ArgumentNullException(nameof(frames));
            _animationName = animationName;
            _animationPath = animationPath;
        }
        
        // Public Properties
        public int FrameCount => _frames?.Count(f => f != null) ?? 0;
        
        // Public Methods - Validation
        
        /// <summary>
        /// Validación principal del menú radial
        /// EXTRAÍDO: De MRUnificarObjetos.Validate()
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación MRUnificarObjetos" };
            
            // Validar nombre de animación
            if (string.IsNullOrEmpty(_animationName))
            {
                result.AddChild(ValidationResult.Error("Nombre de animación requerido"));
            }
            else if (_animationName.Trim() != _animationName)
            {
                result.AddChild(ValidationResult.Warning("El nombre tiene espacios al inicio o final"));
            }
            else if (_animationName.Contains(" "))
            {
                result.AddChild(ValidationResult.Info("El nombre contiene espacios - se recomienda usar guiones bajos"));
            }
            
            // Validar ruta de animación
            if (string.IsNullOrEmpty(_animationPath))
            {
                result.AddChild(ValidationResult.Warning("Ruta de animación vacía - se usará ruta por defecto"));
            }
            else if (!_animationPath.StartsWith("Assets/"))
            {
                result.AddChild(ValidationResult.Error("La ruta debe comenzar con 'Assets/'"));
            }
            
            // Validar frames
            if (FrameCount == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay frames configurados"));
            }
            else
            {
                result.AddChild(ValidateFrames());
            }
            
            return result;
        }
        
        /// <summary>
        /// Validación específica de frames
        /// NUEVO: Validación detallada de todos los frames
        /// </summary>
        /// <returns>Resultado de validación de frames</returns>
        /// <summary>
        /// Valida la colección de frames usando Strategy Pattern
        /// REFACTORIZADO: Reducida complejidad de 12+ a 3 caminos de ejecución
        /// </summary>
        public ValidationResult ValidateFrames()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación de Frames" };
            
            var frameStats = AnalyzeFrameCollection();
            var validationResults = ValidateIndividualFrames();
            var duplicateResults = ValidateDuplicateNames();
            
            CombineValidationResults(result, frameStats, validationResults, duplicateResults);
            
            return result;
        }
        
        /// <summary>
        /// Analiza la colección de frames y genera estadísticas
        /// </summary>
        private FrameCollectionStats AnalyzeFrameCollection()
        {
            var stats = new FrameCollectionStats();
            
            foreach (var frame in _frames)
            {
                if (frame == null)
                    stats.InvalidFrames++;
                else if (IsFrameEmpty(frame))
                    stats.EmptyFrames++;
                else
                    stats.ValidFrames++;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Valida cada frame individualmente y recopila resultados
        /// </summary>
        private List<ValidationResult> ValidateIndividualFrames()
        {
            var results = new List<ValidationResult>();
            
            for (int i = 0; i < _frames.Count; i++)
            {
                var frame = _frames[i];
                if (frame == null) continue;
                
                var frameValidation = frame.Validate();
                if (!frameValidation.IsValid)
                {
                    results.Add(ValidationResult.Warning($"Frame {i} '{frame.FrameName}' tiene problemas: {frameValidation.Message}"));
                }
                
                if (IsFrameEmpty(frame))
                {
                    results.Add(ValidationResult.Info($"Frame {i} '{frame.FrameName}' está vacío"));
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Valida nombres duplicados en frames
        /// </summary>
        private FrameDuplicateValidation ValidateDuplicateNames()
        {
            var duplicateResults = FrameDuplicateValidation.Create();
            var frameNames = new HashSet<string>();
            
            for (int i = 0; i < _frames.Count; i++)
            {
                var frame = _frames[i];
                if (frame == null || string.IsNullOrEmpty(frame.FrameName)) continue;
                
                if (!frameNames.Add(frame.FrameName))
                {
                    duplicateResults.Count++;
                    duplicateResults.ValidationResults.Add(
                        ValidationResult.Warning($"Frame {i}: Nombre duplicado '{frame.FrameName}'"));
                }
            }
            
            return duplicateResults;
        }
        
        /// <summary>
        /// Combina todos los resultados de validación en el resultado final
        /// </summary>
        private void CombineValidationResults(ValidationResult mainResult, 
            FrameCollectionStats stats, 
            List<ValidationResult> individualResults, 
            FrameDuplicateValidation duplicateResults)
        {
            // Agregar estadísticas
            mainResult.AddChild(ValidationResult.Success($"Frames válidos: {stats.ValidFrames}"));
            
            if (stats.InvalidFrames > 0)
                mainResult.AddChild(ValidationResult.Error($"Frames nulos: {stats.InvalidFrames}"));
                
            if (stats.EmptyFrames > 0)
                mainResult.AddChild(ValidationResult.Info($"Frames vacíos: {stats.EmptyFrames}"));
                
            if (duplicateResults.Count > 0)
                mainResult.AddChild(ValidationResult.Warning($"Nombres duplicados: {duplicateResults.Count}"));
            
            // Agregar resultados individuales
            foreach (var result in individualResults)
            {
                mainResult.AddChild(result);
            }
            
            // Agregar resultados de duplicados
            foreach (var result in duplicateResults.ValidationResults)
            {
                mainResult.AddChild(result);
            }
        }
        
        /// <summary>
        /// Validación específica para generación de animaciones
        /// NUEVO: Validación enfocada en requisitos de animación
        /// </summary>
        /// <returns>Resultado de validación para animación</returns>
        public ValidationResult ValidateForAnimation()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación para Generación de Animación" };
            
            // Verificar requisitos básicos
            if (FrameCount == 0)
            {
                result.AddChild(ValidationResult.Error("No se puede generar animación sin frames"));
                return result;
            }
            
            if (string.IsNullOrEmpty(_animationName))
            {
                result.AddChild(ValidationResult.Error("Nombre de animación requerido para generar archivos"));
                return result;
            }
            
            // Validar según tipo de animación
            switch (FrameCount)
            {
                case 1:
                    result.AddChild(ValidateOnOffAnimation());
                    break;
                case 2:
                    result.AddChild(ValidateABAnimation());
                    break;
                default:
                    result.AddChild(ValidateLinearAnimation());
                    break;
            }
            
            return result;
        }
        
        
        // Private Methods - Specific Validations
        
        /// <summary>
        /// Validación para animación ON/OFF (1 frame)
        /// </summary>
        private ValidationResult ValidateOnOffAnimation()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación ON/OFF" };
            
            var frame = _frames.FirstOrDefault(f => f != null);
            if (frame == null)
            {
                result.AddChild(ValidationResult.Error("Frame requerido para animación ON/OFF"));
                return result;
            }
            
            if (IsFrameEmpty(frame))
            {
                result.AddChild(ValidationResult.Warning("Frame vacío - la animación ON/OFF no tendrá contenido"));
            }
            else
            {
                result.AddChild(ValidationResult.Success($"Generará 2 archivos: {_animationName}_on.anim y {_animationName}_off.anim"));
            }
            
            return result;
        }
        
        /// <summary>
        /// Validación para animación A/B (2 frames)
        /// </summary>
        private ValidationResult ValidateABAnimation()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación A/B" };
            
            var frameA = _frames.ElementAtOrDefault(0);
            var frameB = _frames.ElementAtOrDefault(1);
            
            if (frameA == null || frameB == null)
            {
                result.AddChild(ValidationResult.Error("Se requieren 2 frames válidos para animación A/B"));
                return result;
            }
            
            if (IsFrameEmpty(frameA))
            {
                result.AddChild(ValidationResult.Warning("Frame A está vacío"));
            }
            
            if (IsFrameEmpty(frameB))
            {
                result.AddChild(ValidationResult.Warning("Frame B está vacío"));
            }
            
            result.AddChild(ValidationResult.Success($"Generará 2 archivos: {_animationName}_A.anim y {_animationName}_B.anim"));
            
            return result;
        }
        
        /// <summary>
        /// Validación para animación lineal (3+ frames)
        /// </summary>
        private ValidationResult ValidateLinearAnimation()
        {
            var result = new ValidationResult { IsValid = true, Message = "Validación Lineal" };
            
            var validFrames = _frames.Count(f => f != null);
            var emptyFrames = _frames.Count(f => f != null && IsFrameEmpty(f));
            
            if (validFrames < 3)
            {
                result.AddChild(ValidationResult.Error($"Se requieren al menos 3 frames para animación lineal (encontrados: {validFrames})"));
                return result;
            }
            
            if (emptyFrames > 0)
            {
                result.AddChild(ValidationResult.Warning($"{emptyFrames} frames están vacíos"));
            }
            
            // Calcular información de segmentos
            int framesPerSegment = 255 / FrameCount;
            int remainingFrames = 255 % FrameCount;
            
            result.AddChild(ValidationResult.Success($"Generará 1 archivo lineal: {_animationName}.anim"));
            result.AddChild(ValidationResult.Info($"Segmentos: {FrameCount} ({framesPerSegment} frames c/u, +{remainingFrames} en el último)"));
            
            return result;
        }
        
        /// <summary>
        /// Verifica si un frame está completamente vacío
        /// </summary>
        private bool IsFrameEmpty(MRAgruparObjetos frame)
        {
            if (frame == null) return true;
            
            var hasObjects = frame.ObjectReferences != null && frame.ObjectReferences.Any(o => o != null && o.IsValid);
            var hasMaterials = frame.MaterialReferences != null && frame.MaterialReferences.Any(m => m != null && m.IsValid);
            var hasBlendshapes = frame.BlendshapeReferences != null && frame.BlendshapeReferences.Any(b => b != null && b.IsValid);
            
            return !hasObjects && !hasMaterials && !hasBlendshapes;
        }
        
        
        // Public Methods - Utilities
        
        
        /// <summary>
        /// Verifica si el menú está listo para generar animaciones
        /// NUEVO: Método de verificación rápida
        /// </summary>
        /// <returns>True si está listo para generar animaciones</returns>
        public bool IsReadyForAnimation()
        {
            if (FrameCount == 0 || string.IsNullOrEmpty(_animationName))
                return false;
            
            var animationValidation = ValidateForAnimation();
            return animationValidation.IsValid;
        }
        
        
        /// <summary>
        /// Estadísticas de la colección de frames
        /// </summary>
        private struct FrameCollectionStats
        {
            public int ValidFrames;
            public int InvalidFrames;
            public int EmptyFrames;
        }
        
        /// <summary>
        /// Resultado de validación de duplicados
        /// </summary>
        private struct FrameDuplicateValidation
        {
            public int Count;
            public List<ValidationResult> ValidationResults;
            
            public static FrameDuplicateValidation Create()
            {
                return new FrameDuplicateValidation
                {
                    Count = 0,
                    ValidationResults = new List<ValidationResult>()
                };
            }
        }
        
    }
}
