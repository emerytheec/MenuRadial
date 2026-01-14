using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Shaders.Models;

namespace Bender_Dios.MenuRadial.Shaders.Strategies
{
    /// <summary>
    /// Estrategia para manejar materiales con shader Poiyomi
    /// IMPORTANTE: Las propiedades _MinBrightness y _Grayscale_Lighting requieren
    /// estar marcadas como "Animated (when locked)" en el inspector de Poiyomi
    /// </summary>
    public class PoiyomiShaderStrategy : IShaderStrategy
    {
        /// <summary>
        /// Nombres de las propiedades del shader Poiyomi
        /// </summary>
        private static class PoiyomiProperties
        {
            public const string PPLightingMultiplier = "_PPLightingMultiplier";
            public const string MinBrightness = "_MinBrightness";
            public const string GrayscaleLighting = "_Grayscale_Lighting";
        }

        /// <summary>
        /// Nombres de shaders Poiyomi conocidos
        /// </summary>
        private static readonly string[] PoiyomiShaderNames =
        {
            ".poiyomi/Poiyomi Toon",
            ".poiyomi/Poiyomi Pro",
            "Poiyomi/Toon",
            "Poiyomi/Pro",
            "Poiyomi/Poiyomi Toon",
            "Poiyomi/Poiyomi Pro",
            "Hidden/Poiyomi",
            "_poiyomi/Poiyomi Toon",
            "_poiyomi/Poiyomi Pro"
        };

        /// <summary>
        /// Tipo de shader que maneja esta estrategia
        /// </summary>
        public ShaderType ShaderType => ShaderType.Poiyomi;

        /// <summary>
        /// Verifica si el material es compatible con Poiyomi
        /// Soporta tanto materiales desbloqueados como bloqueados (Hidden/Locked/...)
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        public bool IsCompatible(Material material)
        {
            if (material == null || material.shader == null) return false;

            string shaderName = material.shader.name.ToLower();

            // Verificar si contiene "poiyomi" en el nombre (incluye Hidden/Locked/.poiyomi/...)
            if (shaderName.Contains("poiyomi"))
                return true;

            // Verificar nombres exactos conocidos
            foreach (string knownName in PoiyomiShaderNames)
            {
                if (material.shader.name.Equals(knownName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Verificar si tiene las propiedades requeridas (fallback)
            return HasRequiredProperties(material);
        }

        /// <summary>
        /// Obtiene las propiedades de iluminación actuales del material
        /// </summary>
        /// <param name="material">Material del cual obtener propiedades</param>
        /// <returns>Propiedades de iluminación actuales</returns>
        public IlluminationProperties GetProperties(Material material)
        {
            if (material == null) return new IlluminationProperties();

            var properties = new IlluminationProperties();

            if (material.HasProperty(PoiyomiProperties.PPLightingMultiplier))
                properties.PPLightingMultiplier = material.GetFloat(PoiyomiProperties.PPLightingMultiplier);

            if (material.HasProperty(PoiyomiProperties.MinBrightness))
                properties.MinBrightness = material.GetFloat(PoiyomiProperties.MinBrightness);

            if (material.HasProperty(PoiyomiProperties.GrayscaleLighting))
                properties.GrayscaleLighting = material.GetFloat(PoiyomiProperties.GrayscaleLighting);

            return properties;
        }

        /// <summary>
        /// Aplica las propiedades de iluminación al material
        /// </summary>
        /// <param name="material">Material al cual aplicar propiedades</param>
        /// <param name="properties">Propiedades a aplicar</param>
        public void ApplyProperties(Material material, IlluminationProperties properties)
        {
            if (material == null || properties == null) return;

            if (material.HasProperty(PoiyomiProperties.PPLightingMultiplier))
                material.SetFloat(PoiyomiProperties.PPLightingMultiplier, properties.PPLightingMultiplier);

            if (material.HasProperty(PoiyomiProperties.MinBrightness))
                material.SetFloat(PoiyomiProperties.MinBrightness, properties.MinBrightness);

            if (material.HasProperty(PoiyomiProperties.GrayscaleLighting))
                material.SetFloat(PoiyomiProperties.GrayscaleLighting, properties.GrayscaleLighting);
        }

        /// <summary>
        /// Obtiene los nombres de las propiedades del shader
        /// </summary>
        /// <returns>Nombres de las propiedades del shader</returns>
        public string[] GetPropertyNames()
        {
            return new[]
            {
                PoiyomiProperties.PPLightingMultiplier,
                PoiyomiProperties.MinBrightness,
                PoiyomiProperties.GrayscaleLighting
            };
        }

        /// <summary>
        /// Verifica si el material tiene todas las propiedades requeridas
        /// Para materiales bloqueados, verifica si al menos una propiedad animable existe
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si tiene al menos una propiedad requerida</returns>
        public bool HasRequiredProperties(Material material)
        {
            if (material == null) return false;

            // Para Poiyomi, verificamos si tiene al menos una de las propiedades
            // En materiales bloqueados, solo existiran las propiedades marcadas como "Animated"
            return material.HasProperty(PoiyomiProperties.PPLightingMultiplier) ||
                   material.HasProperty(PoiyomiProperties.MinBrightness) ||
                   material.HasProperty(PoiyomiProperties.GrayscaleLighting);
        }

        /// <summary>
        /// Obtiene los nombres de las propiedades que realmente existen en el material
        /// Util para materiales bloqueados donde algunas propiedades pueden no existir
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>Lista de propiedades existentes</returns>
        public string[] GetExistingPropertyNames(Material material)
        {
            if (material == null) return System.Array.Empty<string>();

            var existingProps = new System.Collections.Generic.List<string>();

            if (material.HasProperty(PoiyomiProperties.PPLightingMultiplier))
                existingProps.Add(PoiyomiProperties.PPLightingMultiplier);

            if (material.HasProperty(PoiyomiProperties.MinBrightness))
                existingProps.Add(PoiyomiProperties.MinBrightness);

            if (material.HasProperty(PoiyomiProperties.GrayscaleLighting))
                existingProps.Add(PoiyomiProperties.GrayscaleLighting);

            return existingProps.ToArray();
        }

        /// <summary>
        /// Verifica si el material tiene las propiedades que requieren "Animated (when locked)"
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si tiene las propiedades animables</returns>
        public bool HasAnimatedProperties(Material material)
        {
            if (material == null) return false;

            return material.HasProperty(PoiyomiProperties.MinBrightness) &&
                   material.HasProperty(PoiyomiProperties.GrayscaleLighting);
        }

        /// <summary>
        /// Verifica si el material Poiyomi está bloqueado (locked)
        /// Los materiales bloqueados requieren que las propiedades animables
        /// estén marcadas como "Animated (when locked)"
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si está bloqueado</returns>
        public bool IsMaterialLocked(Material material)
        {
            if (material == null) return false;

            // Metodo 1: Verificar nombre del shader (Hidden/Locked/...)
            string shaderName = material.shader.name;
            if (shaderName.Contains("Hidden/Locked/"))
                return true;

            // Metodo 2: Verificar propiedad _ShaderOptimizer
            if (material.HasProperty("_ShaderOptimizer"))
                return material.GetFloat("_ShaderOptimizer") > 0;

            if (material.HasProperty("_ShaderOptimizerEnabled"))
                return material.GetFloat("_ShaderOptimizerEnabled") > 0;

            return false;
        }

        /// <summary>
        /// Marca las propiedades de iluminacion como animables en un material Poiyomi desbloqueado
        /// Usa SetOverrideTag con sufijo "Animated" como lo hace ThryEditor
        /// IMPORTANTE: Solo funciona en materiales DESBLOQUEADOS
        /// </summary>
        /// <param name="material">Material a configurar</param>
        /// <returns>True si se marcaron las propiedades correctamente</returns>
        public bool MarkPropertiesAsAnimated(Material material)
        {
            if (material == null) return false;

            // No podemos marcar propiedades en materiales bloqueados
            if (IsMaterialLocked(material))
                return false;

            // Marcar cada propiedad como animada usando el sistema de tags de Poiyomi/ThryEditor
            // El formato es: material.SetOverrideTag("_PropertyNameAnimated", "1")
            material.SetOverrideTag(PoiyomiProperties.PPLightingMultiplier + "Animated", "1");
            material.SetOverrideTag(PoiyomiProperties.MinBrightness + "Animated", "1");
            material.SetOverrideTag(PoiyomiProperties.GrayscaleLighting + "Animated", "1");

            return true;
        }

        /// <summary>
        /// Verifica si una propiedad especifica esta marcada como animada
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <param name="propertyName">Nombre de la propiedad</param>
        /// <returns>True si esta marcada como animada</returns>
        public bool IsPropertyMarkedAsAnimated(Material material, string propertyName)
        {
            if (material == null) return false;

            string tag = material.GetTag(propertyName + "Animated", false, "0");
            return tag == "1";
        }

        /// <summary>
        /// Verifica si todas las propiedades de iluminacion estan marcadas como animadas
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si todas estan marcadas</returns>
        public bool AreAllPropertiesMarkedAsAnimated(Material material)
        {
            if (material == null) return false;

            return IsPropertyMarkedAsAnimated(material, PoiyomiProperties.PPLightingMultiplier) &&
                   IsPropertyMarkedAsAnimated(material, PoiyomiProperties.MinBrightness) &&
                   IsPropertyMarkedAsAnimated(material, PoiyomiProperties.GrayscaleLighting);
        }

        #region Poiyomi ShaderOptimizer API (via Reflection)

        // Cache para el tipo ShaderOptimizer
        private static Type _shaderOptimizerType;
        private static bool _shaderOptimizerSearched = false;

        /// <summary>
        /// Obtiene el tipo ShaderOptimizer de Poiyomi/ThryEditor via reflexion
        /// </summary>
        private static Type GetShaderOptimizerType()
        {
            if (_shaderOptimizerSearched) return _shaderOptimizerType;

            _shaderOptimizerSearched = true;

            // Buscar en todos los assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Buscar ShaderOptimizer en diferentes namespaces posibles
                    _shaderOptimizerType = assembly.GetType("Thry.ShaderOptimizer");
                    if (_shaderOptimizerType != null) return _shaderOptimizerType;

                    _shaderOptimizerType = assembly.GetType("ThryEditor.ShaderOptimizer");
                    if (_shaderOptimizerType != null) return _shaderOptimizerType;

                    // Buscar por nombre parcial
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "ShaderOptimizer" && type.Namespace != null &&
                            (type.Namespace.Contains("Thry") || type.Namespace.Contains("Poiyomi")))
                        {
                            _shaderOptimizerType = type;
                            return _shaderOptimizerType;
                        }
                    }
                }
                catch
                {
                    // Ignorar assemblies que no se pueden inspeccionar
                }
            }

            return null;
        }

        /// <summary>
        /// Verifica si la API de ShaderOptimizer esta disponible
        /// </summary>
        public static bool IsShaderOptimizerAvailable()
        {
            return GetShaderOptimizerType() != null;
        }

        /// <summary>
        /// Desbloquea una lista de materiales Poiyomi usando la API de ShaderOptimizer
        /// </summary>
        /// <param name="materials">Lista de materiales a desbloquear</param>
        /// <returns>True si se desbloquearon correctamente</returns>
        public bool UnlockMaterials(List<Material> materials)
        {
            var optimizerType = GetShaderOptimizerType();
            if (optimizerType == null)
            {
                Debug.LogWarning("[MR Iluminacion] ShaderOptimizer de Poiyomi no encontrado");
                return false;
            }

            try
            {
                // Buscar el metodo UnlockMaterials
                var unlockMethod = optimizerType.GetMethod("UnlockMaterials",
                    BindingFlags.Public | BindingFlags.Static);

                if (unlockMethod == null)
                {
                    // Intentar con SetLockedForAllMaterials
                    var setLockedMethod = optimizerType.GetMethod("SetLockedForAllMaterials",
                        BindingFlags.Public | BindingFlags.Static);

                    if (setLockedMethod != null)
                    {
                        // SetLockedForAllMaterials(materials, locked, showProgressBar, showDialog, allowCancel)
                        setLockedMethod.Invoke(null, new object[] { materials, 0, true, false, false });
                        return true;
                    }

                    Debug.LogWarning("[MR Iluminacion] No se encontro metodo para desbloquear materiales");
                    return false;
                }

                // Llamar UnlockMaterials con progress bar
                // UnlockMaterials(IEnumerable<Material> materials, ProgressBar progressBar)
                unlockMethod.Invoke(null, new object[] { materials, 0 }); // 0 = None
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MR Iluminacion] Error al desbloquear materiales: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bloquea una lista de materiales Poiyomi usando la API de ShaderOptimizer
        /// </summary>
        /// <param name="materials">Lista de materiales a bloquear</param>
        /// <returns>True si se bloquearon correctamente</returns>
        public bool LockMaterials(List<Material> materials)
        {
            var optimizerType = GetShaderOptimizerType();
            if (optimizerType == null)
            {
                Debug.LogWarning("[MR Iluminacion] ShaderOptimizer de Poiyomi no encontrado");
                return false;
            }

            try
            {
                // Buscar el metodo LockMaterials
                var lockMethod = optimizerType.GetMethod("LockMaterials",
                    BindingFlags.Public | BindingFlags.Static);

                if (lockMethod == null)
                {
                    // Intentar con SetLockedForAllMaterials
                    var setLockedMethod = optimizerType.GetMethod("SetLockedForAllMaterials",
                        BindingFlags.Public | BindingFlags.Static);

                    if (setLockedMethod != null)
                    {
                        // SetLockedForAllMaterials(materials, locked, showProgressBar, showDialog, allowCancel)
                        setLockedMethod.Invoke(null, new object[] { materials, 1, true, false, false });
                        return true;
                    }

                    Debug.LogWarning("[MR Iluminacion] No se encontro metodo para bloquear materiales");
                    return false;
                }

                // Llamar LockMaterials con progress bar
                lockMethod.Invoke(null, new object[] { materials, 0 }); // 0 = None
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MR Iluminacion] Error al bloquear materiales: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Proceso completo: Desbloquea, marca propiedades, y vuelve a bloquear
        /// </summary>
        /// <param name="materials">Lista de materiales a procesar</param>
        /// <returns>True si el proceso fue exitoso</returns>
        public bool PrepareAndLockMaterials(List<Material> materials)
        {
            if (materials == null || materials.Count == 0) return false;

            // Filtrar solo materiales Poiyomi bloqueados
            var lockedMaterials = new List<Material>();
            foreach (var mat in materials)
            {
                if (mat != null && IsCompatible(mat) && IsMaterialLocked(mat))
                {
                    lockedMaterials.Add(mat);
                }
            }

            if (lockedMaterials.Count == 0)
            {
                // No hay materiales bloqueados, solo marcar los desbloqueados
                foreach (var mat in materials)
                {
                    if (mat != null && IsCompatible(mat) && !IsMaterialLocked(mat))
                    {
                        MarkPropertiesAsAnimated(mat);
                    }
                }
                return true;
            }

            // 1. Desbloquear materiales
            Debug.Log($"[MR Iluminacion] Desbloqueando {lockedMaterials.Count} material(es) Poiyomi...");
            if (!UnlockMaterials(lockedMaterials))
            {
                return false;
            }

            // 2. Marcar propiedades como animadas
            Debug.Log("[MR Iluminacion] Marcando propiedades como Animated...");
            foreach (var mat in lockedMaterials)
            {
                MarkPropertiesAsAnimated(mat);
            }

            // 3. Volver a bloquear
            Debug.Log("[MR Iluminacion] Volviendo a bloquear materiales...");
            if (!LockMaterials(lockedMaterials))
            {
                Debug.LogWarning("[MR Iluminacion] No se pudieron volver a bloquear los materiales. Hazlo manualmente.");
                return false;
            }

            Debug.Log("[MR Iluminacion] Proceso completado exitosamente.");
            return true;
        }

        #endregion
    }
}
