using UnityEngine;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Integración con deslizadores radiales para el menú
    /// Responsabilidad única: Gestión de cache de RadialSliderRenderer y coordinación con slots lineales
    /// VERSIÓN 0.051: Soporte para click-to-show slider y MRIluminacionRadial
    /// </summary>
    public static class RadialSliderIntegration
    {
        // Cache de renderizadores de deslizadores radiales para slots lineales
        private static Dictionary<string, RadialSliderRenderer> _sliderRenderers = new Dictionary<string, RadialSliderRenderer>();

        // Cache de renderizadores para MRIluminacionRadial
        private static Dictionary<string, IlluminationSliderRenderer> _illuminationRenderers = new Dictionary<string, IlluminationSliderRenderer>();

        // Cache de renderizadores para MRUnificarMateriales
        private static Dictionary<string, UnifyMaterialSliderRenderer> _unifyMaterialRenderers = new Dictionary<string, UnifyMaterialSliderRenderer>();

        // Estado de qué slot tiene el slider activo/expandido (solo uno a la vez)
        private static string _activeSliderKey = null;
        
        /// <summary>
        /// Configuración para renderizado de slots con deslizadores
        /// </summary>
        public struct SliderSlotConfig
        {
            /// <summary>
            /// Radio disponible para el deslizador (optimizada para menú más grande)
            /// </summary>
            public float AvailableRadius;
            
            /// <summary>
            /// Configuración por defecto
            /// </summary>
            public static SliderSlotConfig Default => new SliderSlotConfig
            {
                AvailableRadius = 0.5f // Aumentado de 0.4f a 0.5f para menú más grande
            };
        }
        
        /// <summary>
        /// Verifica si un slot es de tipo Linear (puede tener slider)
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="linearSlots">Array de IAnimationProvider para slots lineales</param>
        /// <returns>True si es un slot lineal</returns>
        public static bool IsLinearSlot(int slotIndex, IAnimationProvider[] linearSlots)
        {
            if (linearSlots == null || slotIndex >= linearSlots.Length)
                return false;

            var animationProvider = linearSlots[slotIndex];
            if (animationProvider == null)
                return false;

            // Verificar que sea de tipo Linear
            if (animationProvider.AnimationType != AnimationType.Linear)
                return false;

            // Para MRUnificarObjetos, verificar que tenga suficientes frames
            if (animationProvider is MRUnificarObjetos radialMenu)
            {
                return radialMenu.FrameCount >= 3;
            }

            // Para MRIluminacionRadial u otros tipos lineales
            return true;
        }

        /// <summary>
        /// Verifica si un slot debe mostrar deslizador radial (debe estar expandido)
        /// NUEVO: Ahora requiere que el slider esté activo/expandido
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="linearSlots">Array de IAnimationProvider para slots lineales</param>
        /// <param name="slotKey">Clave del slot</param>
        /// <returns>True si debe mostrar deslizador radial</returns>
        public static bool ShouldShowRadialSlider(int slotIndex, IAnimationProvider[] linearSlots, string slotKey = null)
        {
            // El slot debe ser lineal Y tener el slider expandido
            return IsLinearSlot(slotIndex, linearSlots) && IsSliderExpanded(slotKey);
        }

        /// <summary>
        /// Versión de compatibilidad - siempre retorna false para forzar mostrar iconos
        /// </summary>
        public static bool ShouldShowRadialSlider(int slotIndex, IAnimationProvider[] linearSlots)
        {
            // Sin slotKey, no podemos saber si está expandido - retornar false
            return false;
        }

        /// <summary>
        /// Verifica si el slider de un slot está expandido
        /// </summary>
        /// <param name="slotKey">Clave del slot</param>
        /// <returns>True si el slider está expandido</returns>
        public static bool IsSliderExpanded(string slotKey)
        {
            return !string.IsNullOrEmpty(slotKey) && _activeSliderKey == slotKey;
        }

        /// <summary>
        /// Expande/colapsa el slider de un slot
        /// </summary>
        /// <param name="slotKey">Clave del slot</param>
        public static void ToggleSlider(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey))
                return;

            if (_activeSliderKey == slotKey)
            {
                // Colapsar el slider actual
                _activeSliderKey = null;
            }
            else
            {
                // Expandir este slider (y colapsar cualquier otro)
                _activeSliderKey = slotKey;
            }
        }

        /// <summary>
        /// Colapsa el slider activo
        /// </summary>
        public static void CollapseActiveSlider()
        {
            _activeSliderKey = null;
        }

        /// <summary>
        /// Obtiene la clave del slider actualmente expandido
        /// </summary>
        public static string GetActiveSliderKey()
        {
            return _activeSliderKey;
        }
        
        /// <summary>
        /// Dibuja un slot con deslizador radial integrado
        /// ACTUALIZADO: Ahora soporta MRUnificarObjetos y MRIluminacionRadial
        /// </summary>
        /// <param name="center">Centro del menú</param>
        /// <param name="outerRadius">Radio exterior del menú</param>
        /// <param name="innerRadius">Radio interior del menú</param>
        /// <param name="angle">Ángulo del slot</param>
        /// <param name="slotName">Nombre del slot</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="animationProvider">Componente IAnimationProvider linear</param>
        /// <param name="slotKey">Clave única para cache</param>
        /// <param name="onButtonClick">Callback de clic</param>
        /// <param name="config">Configuración de renderizado</param>
        /// <returns>True si hubo cambios en el deslizador</returns>
        public static bool DrawSlotWithRadialSlider(Vector2 center, float outerRadius, float innerRadius,
                                                   float angle, string slotName, int slotIndex,
                                                   IAnimationProvider animationProvider, string slotKey,
                                                   System.Action<int> onButtonClick, SliderSlotConfig? config = null)
        {
            SliderSlotConfig renderConfig = config ?? SliderSlotConfig.Default;

            // Calcular posición del slot
            Vector2 slotPosition = RadialGeometryCalculator.CalculateButtonPosition(
                center.x, center.y, angle,
                RadialGeometryCalculator.CalculateAverageRadius(outerRadius, innerRadius)
            );

            // Calcular área disponible para el deslizador
            float availableRadius = (outerRadius - innerRadius) * renderConfig.AvailableRadius;

            bool hasChanges = false;

            // Soporte para MRUnificarObjetos
            if (animationProvider is MRUnificarObjetos radialMenu)
            {
                // Obtener o crear renderizador de deslizador
                RadialSliderRenderer sliderRenderer = GetOrCreateSliderRenderer(
                    slotKey ?? $"slot_{slotIndex}", radialMenu);

                if (sliderRenderer != null)
                {
                    // Renderizar el deslizador radial
                    hasChanges = sliderRenderer.RenderSlider(slotPosition, availableRadius);
                }
            }
            // Soporte para MRIluminacionRadial
            else if (animationProvider is MRIluminacionRadial illumination)
            {
                // Usar renderizador de iluminación
                hasChanges = DrawIlluminationSlider(slotPosition, availableRadius, illumination, slotKey ?? $"slot_{slotIndex}");
            }
            // Soporte para MRUnificarMateriales
            else if (animationProvider is MRUnificarMateriales unifyMaterial)
            {
                // Usar renderizador de UnifyMaterial
                hasChanges = DrawUnifyMaterialSlider(slotPosition, availableRadius, unifyMaterial, slotKey ?? $"slot_{slotIndex}");
            }

            // Dibujar nombre del slot debajo del deslizador
            RadialIconManager.DrawSlotText(slotPosition, slotName);

            // Manejar clics del área externa (fuera del deslizador) para cerrar slider
            HandleSliderCloseClick(slotPosition, availableRadius * 1.5f, availableRadius, slotKey);

            return hasChanges;
        }

        /// <summary>
        /// Dibuja el slider para MRIluminacionRadial
        /// </summary>
        private static bool DrawIlluminationSlider(Vector2 center, float availableRadius,
                                                   MRIluminacionRadial illumination, string slotKey)
        {
#if UNITY_EDITOR
            // Obtener o crear renderizador para iluminación
            var renderer = GetOrCreateIlluminationRenderer(slotKey, illumination);
            if (renderer != null)
            {
                return renderer.RenderSlider(center, availableRadius);
            }
#endif
            return false;
        }

        /// <summary>
        /// Dibuja el slider para MRUnificarMateriales
        /// </summary>
        private static bool DrawUnifyMaterialSlider(Vector2 center, float availableRadius,
                                                    MRUnificarMateriales unifyMaterial, string slotKey)
        {
#if UNITY_EDITOR
            // Obtener o crear renderizador para UnifyMaterial
            var renderer = GetOrCreateUnifyMaterialRenderer(slotKey, unifyMaterial);
            if (renderer != null)
            {
                return renderer.RenderSlider(center, availableRadius);
            }
#endif
            return false;
        }

        /// <summary>
        /// Maneja el clic fuera del slider para cerrarlo
        /// NOTA: No consume el evento para permitir que otros botones lo procesen
        /// </summary>
        private static void HandleSliderCloseClick(Vector2 center, float outerCheckRadius, float innerCheckRadius, string slotKey)
        {
            // Ya no manejamos el cierre aquí - se hace en el callback de los otros botones
            // Esto evita que el evento sea consumido antes de que los botones lo procesen
        }
        
        /// <summary>
        /// Obtiene o crea un renderizador de deslizador para un slot específico
        /// </summary>
        /// <param name="slotKey">Clave única del slot</param>
        /// <param name="radialMenu">Componente MRUnificarObjetos</param>
        /// <returns>Renderizador de deslizador radial</returns>
        public static RadialSliderRenderer GetOrCreateSliderRenderer(string slotKey, MRUnificarObjetos radialMenu)
        {
            if (string.IsNullOrEmpty(slotKey))
            {
                slotKey = "default";
            }
                
            // Verificar si ya existe en cache
            if (_sliderRenderers.ContainsKey(slotKey))
            {
                var existing = _sliderRenderers[slotKey];
                
                // Verificar si el MRUnificarObjetos sigue siendo el mismo
                if (existing.TargetRadialMenu == radialMenu)
                {
                    // Actualizar el valor del deslizador basado en el estado actual del MRUnificarObjetos
                    existing.UpdateValueFromRadialMenu();
                    return existing;
                }
                else
                {
                    // El MRUnificarObjetos cambió, cancelar previews y remover del cache
                    existing.CancelAllPreviews();
                        
                    _sliderRenderers.Remove(slotKey);
                }
            }
            
            // Crear nuevo renderizador
            var newRenderer = new RadialSliderRenderer(radialMenu);
            _sliderRenderers[slotKey] = newRenderer;


            return newRenderer;
        }

        /// <summary>
        /// Obtiene o crea un renderizador de deslizador para MRIluminacionRadial
        /// </summary>
        /// <param name="slotKey">Clave única del slot</param>
        /// <param name="illumination">Componente MRIluminacionRadial</param>
        /// <returns>Renderizador de deslizador para iluminación</returns>
        public static IlluminationSliderRenderer GetOrCreateIlluminationRenderer(string slotKey, MRIluminacionRadial illumination)
        {
            if (string.IsNullOrEmpty(slotKey))
            {
                slotKey = "default_illum";
            }

            // Verificar si ya existe en cache
            if (_illuminationRenderers.ContainsKey(slotKey))
            {
                var existing = _illuminationRenderers[slotKey];

                // Verificar si el MRIluminacionRadial sigue siendo el mismo
                if (existing.TargetIllumination == illumination)
                {
                    // Actualizar el valor del deslizador basado en el estado actual
                    existing.UpdateValueFromIllumination();
                    return existing;
                }
                else
                {
                    // El componente cambió, remover del cache
                    _illuminationRenderers.Remove(slotKey);
                }
            }

            // Crear nuevo renderizador
            var newRenderer = new IlluminationSliderRenderer(illumination);
            _illuminationRenderers[slotKey] = newRenderer;

            return newRenderer;
        }

        /// <summary>
        /// Obtiene o crea un renderizador de deslizador para MRUnificarMateriales
        /// </summary>
        /// <param name="slotKey">Clave única del slot</param>
        /// <param name="unifyMaterial">Componente MRUnificarMateriales</param>
        /// <returns>Renderizador de deslizador para UnifyMaterial</returns>
        public static UnifyMaterialSliderRenderer GetOrCreateUnifyMaterialRenderer(string slotKey, MRUnificarMateriales unifyMaterial)
        {
            if (string.IsNullOrEmpty(slotKey))
            {
                slotKey = "default_unify";
            }

            // Verificar si ya existe en cache
            if (_unifyMaterialRenderers.ContainsKey(slotKey))
            {
                var existing = _unifyMaterialRenderers[slotKey];

                // Verificar si el MRUnificarMateriales sigue siendo el mismo
                if (existing.TargetUnifyMaterial == unifyMaterial)
                {
                    // Actualizar el valor del deslizador basado en el estado actual
                    existing.UpdateValueFromUnifyMaterial();
                    return existing;
                }
                else
                {
                    // El componente cambió, remover del cache
                    _unifyMaterialRenderers.Remove(slotKey);
                }
            }

            // Crear nuevo renderizador
            var newRenderer = new UnifyMaterialSliderRenderer(unifyMaterial);
            _unifyMaterialRenderers[slotKey] = newRenderer;

            return newRenderer;
        }

        /// <summary>
        /// Limpia el cache de renderizadores de deslizadores
        /// Útil para testing y para evitar memory leaks
        /// ACTUALIZADO: Ahora cancela previews antes de limpiar y limpia todos los renderers
        /// </summary>
        public static void ClearSliderCache()
        {
            // Limpiar renderizadores de MRUnificarObjetos
            foreach (var kvp in _sliderRenderers)
            {
                kvp.Value?.CancelAllPreviews();
            }
            _sliderRenderers.Clear();

            // Limpiar renderizadores de MRIluminacionRadial
            _illuminationRenderers.Clear();

            // Limpiar renderizadores de MRUnificarMateriales
            _unifyMaterialRenderers.Clear();

            // Resetear estado de slider activo
            _activeSliderKey = null;
        }
        
        
        /// <summary>
        /// Valida la integridad del cache de deslizadores
        /// </summary>
        /// <returns>True si el cache está íntegro</returns>
        public static bool ValidateSliderCache()
        {
            return _sliderRenderers != null;
        }
        
        /// <summary>
        /// Limpia renderizadores inválidos del cache
        /// </summary>
        /// <returns>Número de renderizadores eliminados</returns>
        public static int CleanupInvalidRenderers()
        {
            // Implementación directa de limpieza
            var keysToRemove = new System.Collections.Generic.List<string>();
            foreach (var kvp in _sliderRenderers)
            {
                if (kvp.Value?.TargetRadialMenu == null)
                    keysToRemove.Add(kvp.Key);
            }
            
            foreach (var key in keysToRemove)
            {
                _sliderRenderers.Remove(key);
            }
            
            return keysToRemove.Count;
        }
        
        /// <summary>
        /// Obtiene el número de renderizadores activos en cache
        /// </summary>
        /// <returns>Número de renderizadores en cache</returns>
        public static int GetActiveRenderersCount()
        {
            return _sliderRenderers?.Count ?? 0;
        }
        
        /// <summary>
        /// Verifica si existe un renderizador para una clave específica
        /// </summary>
        /// <param name="slotKey">Clave del slot</param>
        /// <returns>True si existe el renderizador</returns>
        public static bool HasRendererForKey(string slotKey)
        {
            return !string.IsNullOrEmpty(slotKey) && _sliderRenderers.ContainsKey(slotKey);
        }
        
        /// <summary>
        /// Obtiene un renderizador específico del cache (solo lectura)
        /// </summary>
        /// <param name="slotKey">Clave del slot</param>
        /// <returns>Renderizador si existe, null en caso contrario</returns>
        public static RadialSliderRenderer GetExistingRenderer(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey) || !_sliderRenderers.ContainsKey(slotKey))
                return null;
                
            return _sliderRenderers[slotKey];
        }
        
        /// <summary>
        /// Remueve un renderizador específico del cache
        /// </summary>
        /// <param name="slotKey">Clave del slot</param>
        /// <returns>True si se removió exitosamente</returns>
        public static bool RemoveRenderer(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey) || !_sliderRenderers.ContainsKey(slotKey))
                return false;
                
            var renderer = _sliderRenderers[slotKey];
            renderer?.CancelAllPreviews();
            _sliderRenderers.Remove(slotKey);
            
            return true;
        }
        
        /// <summary>
        /// Actualiza todos los renderizadores desde sus MRUnificarObjetos correspondientes
        /// </summary>
        public static void UpdateAllRenderersFromRadialMenus()
        {
            foreach (var kvp in _sliderRenderers)
            {
                kvp.Value?.UpdateValueFromRadialMenu();
            }
        }
        
        /// <summary>
        /// Cancela todos los previews activos en todos los renderizadores
        /// </summary>
        public static void CancelAllPreviews()
        {
            foreach (var kvp in _sliderRenderers)
            {
                kvp.Value?.CancelAllPreviews();
            }
            
        }
        
        
        /// <summary>
        /// Verifica si hay cambios pendientes en algún deslizador
        /// </summary>
        /// <returns>True si hay cambios pendientes</returns>
        public static bool HasPendingChanges()
        {
            // Esta función podría expandirse en el futuro para detectar cambios pendientes
            // Por ahora, simplemente verifica si hay renderizadores activos
            return _sliderRenderers.Count > 0;
        }
        
        /// <summary>
        /// Obtiene estadísticas de uso de los renderizadores
        /// </summary>
        /// <returns>String con estadísticas</returns>
        public static string GetUsageStatistics()
        {
            int validRenderers = 0;
            int invalidRenderers = 0;
            
            foreach (var kvp in _sliderRenderers)
            {
                if (kvp.Value?.TargetRadialMenu != null)
                    validRenderers++;
                else
                    invalidRenderers++;
            }
            
            return $"[RadialSliderIntegration] Estadísticas: {validRenderers} válidos, " +
                   $"{invalidRenderers} inválidos, {_sliderRenderers.Count} total";
        }
    }
}
