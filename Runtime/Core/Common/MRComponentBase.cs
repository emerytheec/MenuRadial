using UnityEngine;
using VRC.SDKBase;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Clase base para todos los componentes del sistema Menu Radial.
    /// Implementa IEditorOnly para que VRChat SDK elimine automáticamente
    /// estos componentes al subir el avatar.
    /// </summary>
    public abstract class MRComponentBase : MonoBehaviour, IValidatable, IEditorOnly
    {
        [SerializeField, HideInInspector] 
        private string _componentVersion = "0.001";
        
        /// <summary>
        /// Versión del componente para control de actualizaciones
        /// </summary>
        public string ComponentVersion => _componentVersion;
        
        /// <summary>
        /// Valida el estado actual del componente
        /// </summary>
        /// <returns>Resultado de la validación</returns>
        public abstract ValidationResult Validate();
        
        /// <summary>
        /// Inicializa el componente con valores por defecto
        /// </summary>
        protected virtual void Awake()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Inicialización específica del componente
        /// </summary>
        protected virtual void InitializeComponent()
        {
            // Implementación base vacía, sobreescribir en clases derivadas
        }
        
        /// <summary>
        /// Limpia recursos del componente
        /// </summary>
        protected virtual void OnDestroy()
        {
            CleanupComponent();
        }
        
        /// <summary>
        /// Limpieza específica del componente
        /// </summary>
        protected virtual void CleanupComponent()
        {
            // Implementación base vacía, sobreescribir en clases derivadas
        }
        
        /// <summary>
        /// Actualiza la versión del componente
        /// </summary>
        /// <param name="newVersion">Nueva versión</param>
        protected void UpdateVersion(string newVersion)
        {
            _componentVersion = newVersion;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Ejecuta validaciones en el editor
        /// </summary>
        protected virtual void OnValidate()
        {
            // Solo ejecutar validaciones en tiempo de edición
            if (Application.isPlaying) return;
            
            ValidateInEditor();
        }
        
        /// <summary>
        /// Validaciones específicas para el editor
        /// </summary>
        protected virtual void ValidateInEditor()
        {
            // Implementación base vacía, sobreescribir en clases derivadas
        }
#endif
    }
}