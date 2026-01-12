using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Services;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Services
{
    /// <summary>
    /// Servicio para escanear y detectar materiales compatibles
    /// OPTIMIZADO [2025-07-04]: Cache de resultados para evitar escaneos repetitivos
    /// </summary>
    [MRService(typeof(IIlluminationMaterialScanner))]
    public class IlluminationMaterialScanner : IIlluminationMaterialScanner
    {
        
        // Cache de resultados por GameObject para evitar escaneos repetitivos
        private readonly FrameBasedCache<int, CachedScanResult> _scanCache = new FrameBasedCache<int, CachedScanResult>(50);
        
        private class CachedScanResult
        {
            public Renderer[] Renderers;
            public Material[] AllMaterials;
            public Material[] CompatibleMaterials;
            public MaterialScanStats Stats;
        }
        
        /// <summary>
        /// Escanea materiales compatibles desde un objeto raíz
        /// OPTIMIZADO [2025-07-04]: Cache de resultados para evitar escaneos costosos repetitivos
        /// </summary>
        /// <param name="rootObject">Objeto raíz desde donde escanear</param>
        /// <returns>Lista de materiales compatibles encontrados</returns>
        public List<Material> ScanMaterials(GameObject rootObject)
        {
            if (rootObject == null) return new List<Material>();
            
            // Usar cache para evitar re-escaneos costosos
            var objectId = rootObject.GetInstanceID();
            var cached = _scanCache.GetOrCalculate(objectId, () => PerformFullScan(rootObject));
            
            // Retornar copia defensiva de materiales compatibles
            return new List<Material>(cached.CompatibleMaterials);
        }
        
        /// <summary>
        /// Obtiene estadísticas del escaneo de materiales
        /// OPTIMIZADO [2025-07-04]: Reutiliza cache del escaneo completo
        /// </summary>
        /// <param name="rootObject">Objeto raíz a analizar</param>
        /// <returns>Información estadística del escaneo</returns>
        public MaterialScanStats GetScanStats(GameObject rootObject)
        {
            if (rootObject == null) return new MaterialScanStats();
            
            // Reutilizar cache del escaneo completo
            var objectId = rootObject.GetInstanceID();
            var cached = _scanCache.GetOrCalculate(objectId, () => PerformFullScan(rootObject));
            
            return cached.Stats;
        }
        
        /// <summary>
        /// Verifica si un material específico es compatible
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        public bool IsCompatibleMaterial(Material material)
        {
            if (material == null) return false;
            
            var strategy = Shaders.ShaderStrategyFactory.Instance.GetStrategyForMaterial(material);
            return strategy != null;
        }
        
        /// <summary>
        /// Obtiene el nombre del material y su shader
        /// </summary>
        /// <param name="material">Material a analizar</param>
        /// <returns>Nombre y shader del material</returns>
        public string GetMaterialInfo(Material material)
        {
            if (material == null) return "null";
            return $"{material.name} ({material.shader?.name ?? "unknown shader"})";
        }
        
        
        
        /// <summary>
        /// Realiza un escaneo completo y crea resultado cacheado
        /// </summary>
        /// <param name="rootObject">Objeto raíz a escanear</param>
        /// <returns>Resultado completo del escaneo</returns>
        private CachedScanResult PerformFullScan(GameObject rootObject)
        {
            var result = new CachedScanResult();
            
            // Escaneo único de renderers (operación costosa)
            result.Renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            
            // Usar pools para listas temporales
            var allMaterialsList = ListPools.Materials.Get();
            var compatibleMaterialsList = ListPools.Materials.Get();
            
            try
            {
                // Procesar cada renderer una sola vez
                foreach (var renderer in result.Renderers)
                {
                    if (renderer == null) continue;
                    
                    ExtractValidMaterials(renderer, allMaterialsList, compatibleMaterialsList);
                }
                
                // Remover duplicados usando HashSet (más eficiente que Distinct())
                var uniqueAll = new HashSet<Material>(allMaterialsList);
                var uniqueCompatible = new HashSet<Material>(compatibleMaterialsList);
                
                result.AllMaterials = new Material[uniqueAll.Count];
                result.CompatibleMaterials = new Material[uniqueCompatible.Count];
                
                uniqueAll.CopyTo(result.AllMaterials, 0);
                uniqueCompatible.CopyTo(result.CompatibleMaterials, 0);
                
                // Crear estadísticas
                result.Stats = new MaterialScanStats
                {
                    TotalRenderers = result.Renderers.Length,
                    TotalMaterials = result.AllMaterials.Length,
                    CompatibleMaterials = result.CompatibleMaterials.Length,
                    IncompatibleMaterials = result.AllMaterials.Length - result.CompatibleMaterials.Length
                };
                
            }
            finally
            {
                ListPools.Materials.Return(allMaterialsList);
                ListPools.Materials.Return(compatibleMaterialsList);
            }
            
            return result;
        }
        
        /// <summary>
        /// Extrae materiales válidos de un renderer usando pooled lists
        /// </summary>
        private void ExtractValidMaterials(Renderer renderer, List<Material> allMaterials, List<Material> compatibleMaterials)
        {
            // Usar sharedMaterials para obtener los materiales originales
            var sharedMaterials = renderer.sharedMaterials;
            
            foreach (var material in sharedMaterials)
            {
                if (material != null)
                {
                    allMaterials.Add(material);
                    
                    if (IsCompatibleMaterial(material))
                    {
                        compatibleMaterials.Add(material);
                    }
                }
            }
        }
        
        /// <summary>
        /// Obtiene materiales de un renderer específico (método de compatibilidad)
        /// </summary>
        /// <param name="renderer">Renderer del cual obtener materiales</param>
        /// <returns>Lista de materiales del renderer</returns>
        private List<Material> GetRendererMaterials(Renderer renderer)
        {
            var materials = ListPools.Materials.Get();
            try
            {
                if (renderer != null)
                {
                    var sharedMaterials = renderer.sharedMaterials;
                    foreach (var material in sharedMaterials)
                    {
                        if (material != null)
                            materials.Add(material);
                    }
                }
                
                return new List<Material>(materials); // Copia defensiva
            }
            finally
            {
                ListPools.Materials.Return(materials);
            }
        }
        
    }
    
    /// <summary>
    /// Interfaz para el scanner de materiales
    /// </summary>
    public interface IIlluminationMaterialScanner
    {
        /// <summary>
        /// Escanea materiales compatibles desde un objeto raíz
        /// </summary>
        /// <param name="rootObject">Objeto raíz desde donde escanear</param>
        /// <returns>Lista de materiales compatibles encontrados</returns>
        List<Material> ScanMaterials(GameObject rootObject);
        
        /// <summary>
        /// Obtiene estadísticas del escaneo de materiales
        /// </summary>
        /// <param name="rootObject">Objeto raíz a analizar</param>
        /// <returns>Información estadística del escaneo</returns>
        MaterialScanStats GetScanStats(GameObject rootObject);
        
        /// <summary>
        /// Verifica si un material específico es compatible
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        bool IsCompatibleMaterial(Material material);
        
        /// <summary>
        /// Obtiene información detallada de un material
        /// </summary>
        /// <param name="material">Material a analizar</param>
        /// <returns>Información detallada del material</returns>
        string GetMaterialInfo(Material material);
    }
    
    /// <summary>
    /// Estadísticas de escaneo de materiales
    /// </summary>
    public class MaterialScanStats
    {
        /// <summary>
        /// Número total de renderers encontrados
        /// </summary>
        public int TotalRenderers { get; set; }
        
        /// <summary>
        /// Número total de materiales únicos encontrados
        /// </summary>
        public int TotalMaterials { get; set; }
        
        /// <summary>
        /// Número de materiales compatibles
        /// </summary>
        public int CompatibleMaterials { get; set; }
        
        /// <summary>
        /// Número de materiales incompatibles
        /// </summary>
        public int IncompatibleMaterials { get; set; }
        
        /// <summary>
        /// Porcentaje de compatibilidad
        /// </summary>
        public float CompatibilityPercentage => TotalMaterials > 0 ? (float)CompatibleMaterials / TotalMaterials * 100f : 0f;
        
        /// <summary>
        /// Representación en string 
        /// </summary>
        /// <returns>String con las estadísticas</returns>
        public override string ToString()
        {
            return $"MaterialScanStats(Renderers:{TotalRenderers}, Total:{TotalMaterials}, Compatible:{CompatibleMaterials}, Incompatible:{IncompatibleMaterials}, Compatibility:{CompatibilityPercentage:F1}%)";
        }
    }
}
