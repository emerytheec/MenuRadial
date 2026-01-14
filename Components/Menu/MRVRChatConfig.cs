using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Configuración específica para la generación de archivos VRChat.
    /// Contiene todos los parámetros necesarios para crear archivos FX, Parameters y Menu.
    /// </summary>
    [System.Serializable]
    public class MRVRChatConfig
    {
        #region Namespace Configuration

        [Header("Namespace del Avatar")]
        [Tooltip("Prefijo único para este avatar. Crea subcarpeta y prefija nombres de archivo. Dejar vacío para comportamiento legacy.")]
        [SerializeField] private string _outputPrefix = "";

        /// <summary>
        /// Prefijo de salida para archivos. Si está vacío, usa comportamiento legacy.
        /// </summary>
        public string OutputPrefix => _outputPrefix;

        /// <summary>
        /// Obtiene el directorio de salida usando la ruta base proporcionada, incluyendo subcarpeta si hay prefijo.
        /// </summary>
        /// <param name="basePath">Ruta base desde MRMenuRadial</param>
        public string GetOutputDirectory(string basePath)
        {
            // Usar la ruta base proporcionada o fallback a constante
            string effectiveBasePath = string.IsNullOrEmpty(basePath)
                ? MRConstants.VRCHAT_OUTPUT_PATH
                : basePath.TrimEnd('/') + "/";

            if (string.IsNullOrEmpty(_outputPrefix))
                return effectiveBasePath;
            return $"{effectiveBasePath}{_outputPrefix}/";
        }

        /// <summary>
        /// Obtiene el directorio de salida usando la ruta por defecto (legacy).
        /// </summary>
        public string GetOutputDirectory()
        {
            return GetOutputDirectory(null);
        }

        /// <summary>
        /// Obtiene un nombre de archivo con prefijo si está configurado.
        /// </summary>
        /// <param name="baseFileName">Nombre base del archivo</param>
        /// <returns>Nombre con prefijo o el nombre base si no hay prefijo</returns>
        public string GetPrefixedFileName(string baseFileName)
        {
            if (string.IsNullOrEmpty(_outputPrefix))
                return baseFileName;
            return $"{_outputPrefix}_{baseFileName}";
        }

        /// <summary>
        /// Verifica si hay un prefijo configurado.
        /// </summary>
        public bool HasPrefix => !string.IsNullOrEmpty(_outputPrefix);

        #endregion

        #region General Configuration

        [Header("Configuración General")]
        [Tooltip("writeDefaultValues para las capas del controlador FX")]
        public bool writeDefaultValues = true;

        #endregion

        #region File Names

        [Header("Nombres de Archivos Base")]
        [SerializeField] private string fxFileName = "FX_Menu_Radial.controller";
        [SerializeField] private string parametersFileName = "Parametro_Menu_Radial.asset";
        [SerializeField] private string menuFileName = "Menu_Menu_Radial.asset";
        

        
        /// <summary>
        /// Nombre del archivo del controlador FX
        /// </summary>
        public string FXFileName => fxFileName;
        
        /// <summary>
        /// Nombre del archivo de parámetros VRChat
        /// </summary>
        public string ParametersFileName => parametersFileName;
        
        /// <summary>
        /// Nombre del archivo de menú VRChat
        /// </summary>
        public string MenuFileName => menuFileName;

        #endregion
    }
}
