using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Preview;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Slot de animación para configuración de menús radiales.
    /// Incluye cache de componentes para evitar llamadas repetidas a GetComponent.
    /// </summary>
    [System.Serializable]
    public class MRAnimationSlot
    {
        [SerializeField] public string slotName = "";
        [SerializeField] public GameObject targetObject = null;
        [SerializeField] public Texture2D iconImage = null;

        #region Name Synchronization

        [SerializeField]
        [Tooltip("Si está activado, sincroniza el nombre del slot con el nombre de animación del componente")]
        private bool _syncNameWithAnimation = true;

        [NonSerialized] private string _lastKnownAnimationName;
        [NonSerialized] private bool _isSyncing;

        /// <summary>
        /// Si true, sincroniza slotName con AnimationName del componente automáticamente
        /// </summary>
        public bool SyncNameWithAnimation
        {
            get => _syncNameWithAnimation;
            set => _syncNameWithAnimation = value;
        }

        /// <summary>
        /// Propaga el nombre del slot al componente de animación
        /// </summary>
        private void PropagateNameToAnimation(string newName)
        {
            if (_isSyncing || string.IsNullOrEmpty(newName))
                return;

            _isSyncing = true;
            try
            {
                var radialMenu = CachedRadialMenu;
                if (radialMenu != null)
                {
                    radialMenu.AnimationName = newName;
                    _lastKnownAnimationName = newName;
                    return;
                }

                var illumination = CachedIllumination;
                if (illumination != null)
                {
                    illumination.AnimationName = newName;
                    _lastKnownAnimationName = newName;
                    return;
                }

                var unifyMaterial = CachedUnifyMaterial;
                if (unifyMaterial != null)
                {
                    unifyMaterial.AnimationName = newName;
                    _lastKnownAnimationName = newName;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Sincroniza slotName desde el AnimationName del componente.
        /// Llamar desde OnValidate del componente padre.
        /// </summary>
        public void SyncFromAnimationName()
        {
            if (!_syncNameWithAnimation || _isSyncing)
                return;

            var provider = CachedAnimationProvider;
            if (provider == null)
                return;

            string animName = provider.AnimationName;
            if (string.IsNullOrEmpty(animName))
                return;

            // Solo sincronizar si el nombre de animación cambió externamente
            if (animName != _lastKnownAnimationName && animName != slotName)
            {
                _isSyncing = true;
                try
                {
                    slotName = animName;
                    _lastKnownAnimationName = animName;
                }
                finally
                {
                    _isSyncing = false;
                }
            }
        }

        /// <summary>
        /// Inicializa el estado de sincronización con el componente actual
        /// </summary>
        public void InitializeSyncState()
        {
            var provider = CachedAnimationProvider;
            if (provider != null)
            {
                _lastKnownAnimationName = provider.AnimationName;
            }
        }

        #endregion

        #region Component Cache

        // Cache de componentes (no serializado)
        [NonSerialized] private GameObject _cachedTargetObject;
        [NonSerialized] private IAnimationProvider _cachedAnimationProvider;
        [NonSerialized] private IPreviewable _cachedPreviewable;
        [NonSerialized] private MRUnificarObjetos _cachedRadialMenu;
        [NonSerialized] private MRIluminacionRadial _cachedIllumination;
        [NonSerialized] private MRUnificarMateriales _cachedUnifyMaterial;
        [NonSerialized] private MRMenuControl _cachedControlMenu;
        [NonSerialized] private bool _cacheInitialized;

        /// <summary>
        /// Invalida el cache si el targetObject ha cambiado
        /// </summary>
        private void EnsureCacheValid()
        {
            if (_cachedTargetObject != targetObject || !_cacheInitialized)
            {
                InvalidateCache();
                _cachedTargetObject = targetObject;
                _cacheInitialized = true;
            }
        }

        /// <summary>
        /// Invalida todo el cache de componentes
        /// </summary>
        public void InvalidateCache()
        {
            _cachedAnimationProvider = null;
            _cachedPreviewable = null;
            _cachedRadialMenu = null;
            _cachedIllumination = null;
            _cachedUnifyMaterial = null;
            _cachedControlMenu = null;
            _cacheInitialized = false;
        }

        /// <summary>
        /// Obtiene el IAnimationProvider cacheado del targetObject
        /// </summary>
        public IAnimationProvider CachedAnimationProvider
        {
            get
            {
                EnsureCacheValid();
                if (_cachedAnimationProvider == null && targetObject != null)
                    _cachedAnimationProvider = targetObject.GetComponent<IAnimationProvider>();
                return _cachedAnimationProvider;
            }
        }

        /// <summary>
        /// Obtiene el IPreviewable cacheado del targetObject
        /// </summary>
        public IPreviewable CachedPreviewable
        {
            get
            {
                EnsureCacheValid();
                if (_cachedPreviewable == null && targetObject != null)
                    _cachedPreviewable = targetObject.GetComponent<IPreviewable>();
                return _cachedPreviewable;
            }
        }

        /// <summary>
        /// Obtiene el MRUnificarObjetos cacheado del targetObject
        /// </summary>
        public MRUnificarObjetos CachedRadialMenu
        {
            get
            {
                EnsureCacheValid();
                if (_cachedRadialMenu == null && targetObject != null)
                    _cachedRadialMenu = targetObject.GetComponent<MRUnificarObjetos>();
                return _cachedRadialMenu;
            }
        }

        /// <summary>
        /// Obtiene el MRIluminacionRadial cacheado del targetObject
        /// </summary>
        public MRIluminacionRadial CachedIllumination
        {
            get
            {
                EnsureCacheValid();
                if (_cachedIllumination == null && targetObject != null)
                    _cachedIllumination = targetObject.GetComponent<MRIluminacionRadial>();
                return _cachedIllumination;
            }
        }

        /// <summary>
        /// Obtiene el MRUnificarMateriales cacheado del targetObject
        /// </summary>
        public MRUnificarMateriales CachedUnifyMaterial
        {
            get
            {
                EnsureCacheValid();
                if (_cachedUnifyMaterial == null && targetObject != null)
                    _cachedUnifyMaterial = targetObject.GetComponent<MRUnificarMateriales>();
                return _cachedUnifyMaterial;
            }
        }

        /// <summary>
        /// Obtiene el MRMenuControl cacheado del targetObject (para submenús)
        /// </summary>
        public MRMenuControl CachedControlMenu
        {
            get
            {
                EnsureCacheValid();
                if (_cachedControlMenu == null && targetObject != null)
                    _cachedControlMenu = targetObject.GetComponent<MRMenuControl>();
                return _cachedControlMenu;
            }
        }

        #endregion

        #region Slot Name Property

        /// <summary>
        /// Nombre del slot con sincronización automática al AnimationName del componente
        /// </summary>
        public string SlotName
        {
            get => slotName;
            set
            {
                if (slotName == value)
                    return;

                slotName = value;

                // Propagar al componente si la sincronización está activa
                if (_syncNameWithAnimation)
                {
                    PropagateNameToAnimation(value);
                }
            }
        }

        #endregion

        [SerializeField] public string validationMessage = "";
        
        /// <summary>
        /// Validez del slot
        /// </summary>
        public bool isValid 
        { 
            get => !string.IsNullOrEmpty(slotName);
            set { }
        }
        
        /// <summary>
        /// Tipo de animación basado en el componente IAnimationProvider del targetObject
        /// </summary>
        /// <returns>Tipo de animación del slot</returns>
        public AnimationType GetAnimationType()
        {
            var provider = CachedAnimationProvider;
            return provider?.AnimationType ?? AnimationType.None;
        }
        
        /// <summary>
        /// Valida el slot
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public Bender_Dios.MenuRadial.Validation.Models.ValidationResult ValidateSlot()
        {
            return Bender_Dios.MenuRadial.Validation.Models.ValidationResult.Success("Slot válido");
        }
        
        /// <summary>
        /// Verifica si puede abrir interfaz circular
        /// </summary>
        /// <returns>True si puede abrir interfaz circular</returns>
        public bool CanOpenCircularInterface()
        {
            return isValid && targetObject != null;
        }
        
        /// <summary>
        /// Verifica si puede ejecutar toggle
        /// </summary>
        /// <returns>True si puede ejecutar toggle</returns>
        public bool CanExecuteToggle()
        {
            return isValid && targetObject != null;
        }
        
        
        /// <summary>
        /// Obtiene el proveedor de animación (usa cache interno)
        /// </summary>
        /// <returns>Proveedor de animación del slot</returns>
        public IAnimationProvider GetAnimationProvider()
        {
            return CachedAnimationProvider;
        }
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MRAnimationSlot()
        {
            slotName = "Nuevo Slot";
        }

        /// <summary>
        /// Constructor con nombre
        /// </summary>
        public MRAnimationSlot(string name)
        {
            slotName = name ?? "Nuevo Slot";
        }

        /// <summary>
        /// Reinicia el slot a sus valores por defecto
        /// </summary>
        public void Reset()
        {
            slotName = "Nuevo Slot";
            targetObject = null;
            iconImage = null;
        }
        
        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            return $"MRAnimationSlot: {slotName} - {(targetObject != null ? targetObject.name : "Sin asignar")}";
        }
    }
}