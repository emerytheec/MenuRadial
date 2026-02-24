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

        /// <summary>
        /// Busca un GameObject con VRCAvatarDescriptor en la jerarquía padre.
        /// Útil para auto-detectar el avatar cuando el componente es hijo de él.
        /// </summary>
        /// <returns>El GameObject del avatar, o null si no se encontró</returns>
        protected GameObject FindAvatarInParents()
        {
            var current = transform;
            while (current != null)
            {
                foreach (var comp in current.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    var typeName = comp.GetType().Name;
                    if (typeName == "VRCAvatarDescriptor" || typeName == "VRC_AvatarDescriptor")
                        return current.gameObject;
                }
                current = current.parent;
            }
            return null;
        }
    }
}