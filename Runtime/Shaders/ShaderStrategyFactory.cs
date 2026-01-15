using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Shaders.Strategies;

namespace Bender_Dios.MenuRadial.Shaders
{
    /// <summary>
    /// Factory para obtener estrategias de shaders
    /// </summary>
    public class ShaderStrategyFactory
    {
        private static ShaderStrategyFactory _instance;
        private readonly Dictionary<ShaderType, IShaderStrategy> _strategies;
        
        /// <summary>
        /// Instancia singleton del factory
        /// </summary>
        public static ShaderStrategyFactory Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ShaderStrategyFactory();
                return _instance;
            }
        }
        
        /// <summary>
        /// Constructor privado para singleton
        /// </summary>
        private ShaderStrategyFactory()
        {
            _strategies = new Dictionary<ShaderType, IShaderStrategy>();
            InitializeStrategies();
        }
        
        /// <summary>
        /// Inicializa las estrategias disponibles
        /// </summary>
        private void InitializeStrategies()
        {
            RegisterStrategy(new LilToonShaderStrategy());
        }
        
        /// <summary>
        /// Registra una estrategia de shader
        /// </summary>
        /// <param name="strategy">Estrategia a registrar</param>
        public void RegisterStrategy(IShaderStrategy strategy)
        {
            if (strategy == null) return;
            
            _strategies[strategy.ShaderType] = strategy;
        }
        
        /// <summary>
        /// Obtiene la estrategia apropiada para un material
        /// </summary>
        /// <param name="material">Material a analizar</param>
        /// <returns>Estrategia compatible o null si no hay ninguna</returns>
        public IShaderStrategy GetStrategyForMaterial(Material material)
        {
            if (material == null) return null;
            
            foreach (var strategy in _strategies.Values)
            {
                if (strategy.IsCompatible(material))
                    return strategy;
            }
            
            return null;
        }
        
        /// <summary>
        /// Obtiene una estrategia por tipo de shader
        /// </summary>
        /// <param name="shaderType">Tipo de shader</param>
        /// <returns>Estrategia del tipo especificado o null</returns>
        public IShaderStrategy GetStrategy(ShaderType shaderType)
        {
            return _strategies.TryGetValue(shaderType, out var strategy) ? strategy : null;
        }
        
        /// <summary>
        /// Verifica si un material es compatible con alguna estrategia
        /// </summary>
        /// <param name="material">Material a verificar</param>
        /// <returns>True si es compatible</returns>
        public bool IsCompatible(Material material)
        {
            return GetStrategyForMaterial(material) != null;
        }
        
        /// <summary>
        /// Obtiene todas las estrategias registradas
        /// </summary>
        /// <returns>Lista de estrategias disponibles</returns>
        public IShaderStrategy[] GetAllStrategies()
        {
            var strategies = new IShaderStrategy[_strategies.Count];
            _strategies.Values.CopyTo(strategies, 0);
            return strategies;
        }
        
    }
}