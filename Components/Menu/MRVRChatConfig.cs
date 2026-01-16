using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Configuración específica para la generación de archivos VRChat.
    /// Los valores principales (OutputPrefix, WriteDefaultValues) se obtienen desde MRMenuRadial.
    /// </summary>
    [System.Serializable]
    public class MRVRChatConfig
    {
        #region Cached Values from MRMenuRadial

        // Valores cacheados desde MRMenuRadial (no serializados, se obtienen dinámicamente)
        [System.NonSerialized] private string _cachedOutputPrefix = "";
        [System.NonSerialized] private bool _cachedWriteDefaultValues = true;
        [System.NonSerialized] private string _cachedOutputPath = "";
        [System.NonSerialized] private bool _valuesFromMenuRadial = false;

        /// <summary>
        /// Sincroniza los valores desde MRMenuRadial.
        /// Llamar antes de usar la configuración.
        /// </summary>
        /// <param name="menuControlTransform">Transform del MRMenuControl para buscar MRMenuRadial en ancestros</param>
        public void SyncFromMenuRadial(Transform menuControlTransform)
        {
            if (menuControlTransform == null)
            {
                _valuesFromMenuRadial = false;
                return;
            }

            // Buscar MRMenuRadial en ancestros usando reflexión (cross-assembly)
            Transform current = menuControlTransform;
            while (current != null)
            {
                var components = current.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name == "MRMenuRadial")
                    {
                        // Obtener OutputPrefix
                        var outputPrefixProp = comp.GetType().GetProperty("OutputPrefix");
                        if (outputPrefixProp != null)
                        {
                            _cachedOutputPrefix = (string)outputPrefixProp.GetValue(comp) ?? "";
                        }

                        // Obtener WriteDefaultValues
                        var writeDefaultProp = comp.GetType().GetProperty("WriteDefaultValues");
                        if (writeDefaultProp != null)
                        {
                            _cachedWriteDefaultValues = (bool)writeDefaultProp.GetValue(comp);
                        }

                        // Obtener OutputPath
                        var outputPathProp = comp.GetType().GetProperty("OutputPath");
                        if (outputPathProp != null)
                        {
                            _cachedOutputPath = (string)outputPathProp.GetValue(comp) ?? "";
                        }

                        _valuesFromMenuRadial = true;
                        return;
                    }
                }
                current = current.parent;
            }

            _valuesFromMenuRadial = false;
        }

        #endregion

        #region Namespace Configuration

        /// <summary>
        /// Prefijo de salida para archivos. Obtenido desde MRMenuRadial.
        /// </summary>
        public string OutputPrefix => _valuesFromMenuRadial ? _cachedOutputPrefix : "";

        /// <summary>
        /// Obtiene el directorio de salida usando la ruta desde MRMenuRadial, incluyendo subcarpeta si hay prefijo.
        /// </summary>
        public string GetOutputDirectory()
        {
            // Usar la ruta desde MRMenuRadial o fallback a constante
            string effectiveBasePath = string.IsNullOrEmpty(_cachedOutputPath)
                ? MRConstants.VRCHAT_OUTPUT_PATH
                : _cachedOutputPath.TrimEnd('/') + "/";

            string prefix = OutputPrefix;
            if (string.IsNullOrEmpty(prefix))
                return effectiveBasePath;
            return $"{effectiveBasePath}{prefix}/";
        }

        /// <summary>
        /// Obtiene el directorio de salida usando una ruta base específica.
        /// Siempre añade el prefijo si está configurado.
        /// </summary>
        /// <param name="basePath">Ruta base override (si es null, usa la ruta de MRMenuRadial)</param>
        public string GetOutputDirectory(string basePath)
        {
            // Determinar la ruta base efectiva
            string effectiveBasePath;
            if (string.IsNullOrEmpty(basePath))
            {
                // Sin basePath, usar la ruta cacheada de MRMenuRadial o fallback
                effectiveBasePath = string.IsNullOrEmpty(_cachedOutputPath)
                    ? MRConstants.VRCHAT_OUTPUT_PATH
                    : _cachedOutputPath.TrimEnd('/') + "/";
            }
            else
            {
                effectiveBasePath = basePath.TrimEnd('/') + "/";
            }

            // Siempre añadir el prefijo si existe
            string prefix = OutputPrefix;
            if (string.IsNullOrEmpty(prefix))
                return effectiveBasePath;
            return $"{effectiveBasePath}{prefix}/";
        }

        /// <summary>
        /// Obtiene un nombre de archivo con prefijo si está configurado.
        /// </summary>
        /// <param name="baseFileName">Nombre base del archivo</param>
        /// <returns>Nombre con prefijo o el nombre base si no hay prefijo</returns>
        public string GetPrefixedFileName(string baseFileName)
        {
            string prefix = OutputPrefix;
            if (string.IsNullOrEmpty(prefix))
                return baseFileName;
            return $"{prefix}_{baseFileName}";
        }

        /// <summary>
        /// Verifica si hay un prefijo configurado.
        /// </summary>
        public bool HasPrefix => !string.IsNullOrEmpty(OutputPrefix);

        #endregion

        #region General Configuration

        /// <summary>
        /// WriteDefaultValues para las capas del controlador FX. Obtenido desde MRMenuRadial.
        /// </summary>
        public bool writeDefaultValues => _valuesFromMenuRadial ? _cachedWriteDefaultValues : true;

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
