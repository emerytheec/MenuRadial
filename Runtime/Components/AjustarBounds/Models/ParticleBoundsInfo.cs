using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Models
{
    /// <summary>
    /// Informacion de bounds de un ParticleSystemRenderer individual.
    /// Almacena tanto los bounds originales como los calculados.
    /// Las particulas tienen bounds separados del sistema de meshes.
    /// </summary>
    [Serializable]
    public class ParticleBoundsInfo
    {
        [SerializeField]
        private ParticleSystemRenderer _renderer;

        [SerializeField]
        private ParticleSystem _particleSystem;

        [SerializeField]
        private Bounds _originalBounds;

        [SerializeField]
        private Bounds _calculatedBounds;

        [SerializeField]
        private bool _isValid;

        [SerializeField]
        private string _particleName;

        [SerializeField]
        private string _hierarchyPath;

        [SerializeField]
        private bool _wasAutomatic;

        /// <summary>
        /// Referencia al ParticleSystemRenderer
        /// </summary>
        public ParticleSystemRenderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }

        /// <summary>
        /// Referencia al ParticleSystem
        /// </summary>
        public ParticleSystem ParticleSystem
        {
            get => _particleSystem;
            set => _particleSystem = value;
        }

        /// <summary>
        /// Bounds originales antes de modificar
        /// </summary>
        public Bounds OriginalBounds
        {
            get => _originalBounds;
            set => _originalBounds = value;
        }

        /// <summary>
        /// Bounds calculados para esta particula
        /// </summary>
        public Bounds CalculatedBounds
        {
            get => _calculatedBounds;
            set => _calculatedBounds = value;
        }

        /// <summary>
        /// Indica si la referencia al renderer es valida
        /// </summary>
        public bool IsValid
        {
            get => _isValid && _renderer != null;
            set => _isValid = value;
        }

        /// <summary>
        /// Nombre del sistema de particulas para mostrar en UI
        /// </summary>
        public string ParticleName
        {
            get => _particleName;
            set => _particleName = value;
        }

        /// <summary>
        /// Ruta jerarquica del objeto en la escena
        /// </summary>
        public string HierarchyPath
        {
            get => _hierarchyPath;
            set => _hierarchyPath = value;
        }

        /// <summary>
        /// Indica si el bounds original era automatico
        /// </summary>
        public bool WasAutomatic
        {
            get => _wasAutomatic;
            set => _wasAutomatic = value;
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ParticleBoundsInfo() { }

        /// <summary>
        /// Constructor con ParticleSystem
        /// </summary>
        public ParticleBoundsInfo(ParticleSystem particleSystem)
        {
            _particleSystem = particleSystem;
            _renderer = particleSystem?.GetComponent<ParticleSystemRenderer>();

            if (_renderer != null)
            {
                _originalBounds = _renderer.bounds;
                _particleName = particleSystem.name;
                _hierarchyPath = GetHierarchyPath(particleSystem.transform);
                _isValid = true;

                // Detectar si usa bounds automaticos
                var main = particleSystem.main;
                // En versiones modernas de Unity no hay BoundsMode directo,
                // pero podemos inferirlo por otros medios
                _wasAutomatic = true; // Por defecto asumimos automatico
            }
        }

        /// <summary>
        /// Constructor con ParticleSystemRenderer
        /// </summary>
        public ParticleBoundsInfo(ParticleSystemRenderer renderer)
        {
            _renderer = renderer;
            _particleSystem = renderer?.GetComponent<ParticleSystem>();

            if (renderer != null)
            {
                _originalBounds = renderer.bounds;
                _particleName = renderer.name;
                _hierarchyPath = GetHierarchyPath(renderer.transform);
                _isValid = true;
                _wasAutomatic = true;
            }
        }

        /// <summary>
        /// Obtiene la ruta jerarquica de un transform
        /// </summary>
        private string GetHierarchyPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Actualiza la referencia y recaptura datos
        /// </summary>
        public void Refresh()
        {
            if (_renderer != null)
            {
                _originalBounds = _renderer.bounds;
                _particleName = _renderer.name;
                _hierarchyPath = GetHierarchyPath(_renderer.transform);
                _isValid = true;
            }
            else if (_particleSystem != null)
            {
                _renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
                if (_renderer != null)
                {
                    _originalBounds = _renderer.bounds;
                    _particleName = _particleSystem.name;
                    _hierarchyPath = GetHierarchyPath(_particleSystem.transform);
                    _isValid = true;
                }
                else
                {
                    _isValid = false;
                }
            }
            else
            {
                _isValid = false;
            }
        }

        /// <summary>
        /// Aplica los bounds calculados al renderer
        /// </summary>
        public void ApplyCalculatedBounds()
        {
            if (_renderer != null && _isValid)
            {
                // Para ParticleSystemRenderer, necesitamos usar el modo Custom
                // y establecer los bounds manualmente
                _renderer.bounds = _calculatedBounds;
            }
        }

        /// <summary>
        /// Aplica bounds especificos al renderer
        /// </summary>
        public void ApplyBounds(Bounds bounds)
        {
            if (_renderer != null && _isValid)
            {
                _calculatedBounds = bounds;
                _renderer.bounds = bounds;
            }
        }

        /// <summary>
        /// Restaura los bounds originales
        /// </summary>
        public void RestoreOriginalBounds()
        {
            if (_renderer != null)
            {
                _renderer.bounds = _originalBounds;
            }
        }

        /// <summary>
        /// Obtiene informacion del ParticleSystem para mostrar en UI
        /// </summary>
        public string GetParticleInfo()
        {
            if (_particleSystem == null) return "Sin informacion";

            var main = _particleSystem.main;
            return $"Max: {main.maxParticles}, Duration: {main.duration:F1}s";
        }
    }
}
