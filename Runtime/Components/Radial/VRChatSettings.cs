using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Configuración específica para la integración con VRChat
    /// Maneja parámetros flotantes y configuración de Expression Menu
    /// </summary>
    [Serializable]
    public class VRChatSettings
    {
        [Header("Configuración de Parámetros")]
        [SerializeField] private string _parameterName = "RadialToggle";
        [SerializeField] private bool _syncParameter = true;
        [SerializeField] private bool _localParameter = false;
        
        [Header("Integración con Avatar")]
        [SerializeField] private GameObject _avatarDescriptor;
        [SerializeField] private bool _addToExpressionMenu = true;
        [SerializeField] private string _menuDisplayName = "Radial Menu";
        
        [Header("Configuración de Menú")]
        [SerializeField] private Texture2D _menuIcon;
        [SerializeField] private bool _createSubMenu = false;
        [SerializeField] private string _subMenuName = "Radial Options";
        
        // Public Properties
        
        /// <summary>
        /// Nombre del parámetro flotante en VRChat
        /// </summary>
        public string ParameterName 
        { 
            get => _parameterName; 
            set => _parameterName = ValidateParameterName(value); 
        }
        
        /// <summary>
        /// Indica si el parámetro debe sincronizarse entre usuarios
        /// </summary>
        public bool SyncParameter 
        { 
            get => _syncParameter; 
            set => _syncParameter = value; 
        }
        
        /// <summary>
        /// Indica si el parámetro es local (no sincronizado)
        /// </summary>
        public bool LocalParameter 
        { 
            get => _localParameter; 
            set 
            {
                _localParameter = value;
                // Si es local, no puede ser sincronizado
                if (value) _syncParameter = false;
            } 
        }
        
        /// <summary>
        /// GameObject que contiene el VRCAvatarDescriptor
        /// </summary>
        public GameObject AvatarDescriptor 
        { 
            get => _avatarDescriptor; 
            set => _avatarDescriptor = value; 
        }
        
        /// <summary>
        /// Indica si debe añadirse automáticamente al Expression Menu
        /// </summary>
        public bool AddToExpressionMenu 
        { 
            get => _addToExpressionMenu; 
            set => _addToExpressionMenu = value; 
        }
        
        /// <summary>
        /// Nombre que se mostrará en el Expression Menu
        /// </summary>
        public string MenuDisplayName 
        { 
            get => _menuDisplayName; 
            set => _menuDisplayName = !string.IsNullOrEmpty(value) ? value : "Radial Menu"; 
        }
        
        /// <summary>
        /// Icono que se mostrará en el Expression Menu
        /// </summary>
        public Texture2D MenuIcon 
        { 
            get => _menuIcon; 
            set => _menuIcon = value; 
        }
        
        /// <summary>
        /// Indica si debe crearse un submenú para las opciones
        /// </summary>
        public bool CreateSubMenu 
        { 
            get => _createSubMenu; 
            set => _createSubMenu = value; 
        }
        
        /// <summary>
        /// Nombre del submenú si se crea uno
        /// </summary>
        public string SubMenuName 
        { 
            get => _subMenuName; 
            set => _subMenuName = !string.IsNullOrEmpty(value) ? value : "Radial Options"; 
        }
        
        
        // Validation and Utilities
        
        /// <summary>
        /// Valida y limpia el nombre del parámetro según reglas de VRChat
        /// </summary>
        /// <param name="parameterName">Nombre a validar</param>
        /// <returns>Nombre válido para VRChat</returns>
        private string ValidateParameterName(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return "RadialToggle";
            }
            
            // Remover caracteres no válidos para parámetros de VRChat
            string cleaned = System.Text.RegularExpressions.Regex.Replace(parameterName, @"[^a-zA-Z0-9_]", "");
            
            // Asegurar que no empiece con número
            if (char.IsDigit(cleaned[0]))
            {
                cleaned = "_" + cleaned;
            }
            
            // Limitar longitud (VRChat tiene límites en nombres de parámetros)
            if (cleaned.Length > 32)
            {
                cleaned = cleaned.Substring(0, 32);
            }
            
            return !string.IsNullOrEmpty(cleaned) ? cleaned : "RadialToggle";
        }
        
        /// <summary>
        /// Valida toda la configuración de VRChat
        /// </summary>
        /// <returns>True si la configuración es válida</returns>
        public bool ValidateConfiguration()
        {
            bool isValid = true;
            
            // Validar nombre del parámetro
            if (string.IsNullOrEmpty(_parameterName))
            {
                _parameterName = "RadialToggle";
                isValid = false;
            }
            
            // Validar nombre del menú
            if (string.IsNullOrEmpty(_menuDisplayName))
            {
                _menuDisplayName = "Radial Menu";
                isValid = false;
            }
            
            // Validar nombre del submenú si está habilitado
            if (_createSubMenu && string.IsNullOrEmpty(_subMenuName))
            {
                _subMenuName = "Radial Options";
                isValid = false;
            }
            
            // Validar avatar descriptor si se va a añadir al menú
            if (_addToExpressionMenu && _avatarDescriptor == null)
            {
                isValid = false;
            }
            
            return isValid;
        }
        
        
        /// <summary>
        /// Verifica si el avatar descriptor es válido para VRChat
        /// </summary>
        /// <returns>True si el avatar descriptor es válido</returns>
        public bool HasValidAvatarDescriptor()
        {
            if (_avatarDescriptor == null) return false;
            
            // Verificar si tiene el componente VRCAvatarDescriptor
            // Nota: En un entorno real, aquí verificaríamos el componente VRCAvatarDescriptor
            
            // Por ahora, solo verificamos que no sea null
            return true;
        }
        
        /// <summary>
        /// Obtiene el tipo de parámetro según la configuración
        /// </summary>
        /// <returns>Tipo de parámetro VRChat</returns>
        public string GetParameterType()
        {
            if (_localParameter) return "Local";
            if (_syncParameter) return "Synced";
            return "Standard";
        }
        
        /// <summary>
        /// Calcula el valor mínimo y máximo del parámetro flotante según el número de frames
        /// </summary>
        /// <param name="frameCount">Número de frames en el menú radial</param>
        /// <returns>Tupla con valores mín y máx</returns>
        public (float min, float max) GetParameterRange(int frameCount)
        {
            // Para menús radiales, típicamente el rango va de 0 a frameCount-1
            return (0f, Mathf.Max(0f, frameCount - 1));
        }
        
        
        // Constructor
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public VRChatSettings()
        {
            _parameterName = "RadialToggle";
            _syncParameter = true;
            _localParameter = false;
            _addToExpressionMenu = true;
            _menuDisplayName = "Radial Menu";
            _createSubMenu = false;
            _subMenuName = "Radial Options";
        }
        
        /// <summary>
        /// Constructor con configuración personalizada
        /// </summary>
        /// <param name="parameterName">Nombre del parámetro</param>
        /// <param name="menuDisplayName">Nombre en el menú</param>
        /// <param name="syncParameter">Si debe sincronizarse</param>
        public VRChatSettings(string parameterName, string menuDisplayName = null, bool syncParameter = true)
        {
            _parameterName = ValidateParameterName(parameterName);
            _syncParameter = syncParameter;
            _localParameter = false;
            _addToExpressionMenu = true;
            _menuDisplayName = !string.IsNullOrEmpty(menuDisplayName) ? menuDisplayName : "Radial Menu";
            _createSubMenu = false;
            _subMenuName = "Radial Options";
        }
        
    }
}
