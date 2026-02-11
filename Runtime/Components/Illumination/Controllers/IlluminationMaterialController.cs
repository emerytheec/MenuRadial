using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Shaders;
using Bender_Dios.MenuRadial.Shaders.Models;
using Bender_Dios.MenuRadial.Core.Services;

namespace Bender_Dios.MenuRadial.Components.Illumination.Controllers
{
    /// <summary>
    /// Controlador para gestión de materiales en componentes de iluminación
    /// </summary>
    public class IlluminationMaterialController
    {
        private List<Material> _detectedMaterials;
        private IlluminationProperties _currentProperties;
        private IIlluminationMaterialScanner _materialScanner;
        
        /// <summary>
        /// Lista de materiales detectados y compatibles
        /// </summary>
        public List<Material> DetectedMaterials => _detectedMaterials ?? (_detectedMaterials = new List<Material>());
        
        /// <summary>
        /// Propiedades de iluminación actuales
        /// </summary>
        public IlluminationProperties CurrentProperties
        {
            get => _currentProperties ?? (_currentProperties = IlluminationProperties.CreateFrame255());
            set => _currentProperties = value ?? new IlluminationProperties();
        }
        
        /// <summary>
        /// Scanner de materiales (lazy loading)
        /// </summary>
        private IIlluminationMaterialScanner MaterialScanner
        {
            get
            {
                if (_materialScanner == null)
                    _materialScanner = MenuRadialServiceBootstrap.GetService<IIlluminationMaterialScanner>();
                return _materialScanner;
            }
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        public IlluminationMaterialController()
        {
            _detectedMaterials = new List<Material>();
            _currentProperties = IlluminationProperties.CreateFrame255();
        }
        
        /// <summary>
        /// Escanea materiales compatibles desde un objeto raíz
        /// </summary>
        /// <param name="rootObject">Objeto raíz desde donde escanear</param>
        /// <returns>Número de materiales encontrados</returns>
        public int ScanMaterials(GameObject rootObject)
        {
            DetectedMaterials.Clear();
            
            if (rootObject == null)
            {
                return 0;
            }
            
            if (MaterialScanner == null)
            {
                return 0;
            }
            
            var scannedMaterials = MaterialScanner.ScanMaterials(rootObject);
            DetectedMaterials.AddRange(scannedMaterials);
            
            
            return DetectedMaterials.Count;
        }
        
        /// <summary>
        /// Aplica las propiedades actuales a todos los materiales detectados
        /// </summary>
        /// <returns>Número de materiales modificados</returns>
        public int ApplyPropertiesToAllMaterials()
        {
            if (DetectedMaterials.Count == 0)
            {
                return 0;
            }
            
            int appliedCount = 0;
            var factory = ShaderStrategyFactory.Instance;
            
            foreach (var material in DetectedMaterials)
            {
                if (material == null) continue;
                
                var strategy = factory.GetStrategyForMaterial(material);
                if (strategy != null)
                {
                    strategy.ApplyProperties(material, CurrentProperties);
                    appliedCount++;
                }
            }
            
            return appliedCount;
        }
        
        /// <summary>
        /// Aplica propiedades específicas a todos los materiales
        /// </summary>
        /// <param name="properties">Propiedades a aplicar</param>
        /// <returns>Número de materiales modificados</returns>
        public int ApplyPropertiesToAllMaterials(IlluminationProperties properties)
        {
            if (properties == null) return 0;
            
            CurrentProperties = properties;
            return ApplyPropertiesToAllMaterials();
        }
        
        /// <summary>
        /// Obtiene estadísticas de los materiales detectados
        /// </summary>
        /// <param name="rootObject">Objeto raíz para estadísticas adicionales</param>
        /// <returns>Estadísticas de materiales</returns>
        public MaterialScanStats GetMaterialStats(GameObject rootObject)
        {
            if (MaterialScanner == null || rootObject == null)
                return new MaterialScanStats();
                
            return MaterialScanner.GetScanStats(rootObject);
        }
        
        /// <summary>
        /// Limpia la lista de materiales detectados
        /// </summary>
        public void ClearDetectedMaterials()
        {
            DetectedMaterials.Clear();
        }
        
    }
}