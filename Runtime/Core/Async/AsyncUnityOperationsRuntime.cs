using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Async
{
    /// <summary>
    /// Operaciones asíncronas de Unity que no requieren APIs del Editor
    /// Parte Runtime de AsyncUnityOperations
    /// </summary>
    public static partial class AsyncUnityOperations
    {
        
        /// <summary>
        /// Ejecuta operaciones asíncronas con límite de concurrencia
        /// Runtime-safe: no usa APIs del Editor
        /// </summary>
        /// <typeparam name="T">Tipo de resultado</typeparam>
        /// <param name="operations">Lista de operaciones a ejecutar</param>
        /// <param name="maxConcurrency">Máximo número de operaciones concurrentes</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista de resultados</returns>
        public static async Task<List<T>> ExecuteWithConcurrencyLimitAsync<T>(
            IEnumerable<Func<CancellationToken, Task<T>>> operations,
            int maxConcurrency = 4,
            CancellationToken cancellationToken = default)
        {
            if (operations == null)
            {
                return new List<T>();
            }
            
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task<T>>();
            
            foreach (var operation in operations)
            {
                tasks.Add(ExecuteWithSemaphoreAsync(operation, semaphore, cancellationToken));
            }
            
            try
            {
                var results = await Task.WhenAll(tasks);
                return new List<T>(results);
            }
            finally
            {
                semaphore?.Dispose();
            }
        }
        
        /// <summary>
        /// Ejecuta operación con semáforo para control de concurrencia
        /// </summary>
        private static async Task<T> ExecuteWithSemaphoreAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        
        
        /// <summary>
        /// Carga textura de forma runtime-safe (Editor usa AssetDatabase, Runtime usa Resources)
        /// </summary>
        /// <param name="texturePath">Ruta de la textura</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Textura cargada</returns>
        public static async Task<Texture2D> LoadTextureAsync(string texturePath, CancellationToken cancellationToken = default)
        {
#if UNITY_EDITOR
            // En Editor, usamos Resources.LoadAsync ya que LoadAssetAsync está en el archivo Editor
            var request = Resources.LoadAsync<Texture2D>(texturePath);
            
            while (!request.isDone && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
            }
            
            cancellationToken.ThrowIfCancellationRequested();
            return request.asset as Texture2D;
#else
            // En Runtime, intentar cargar desde Resources
            if (string.IsNullOrEmpty(texturePath))
            {
                return null;
            }
            
            // Convertir ruta de asset a ruta de Resources
            var resourcePath = texturePath;
            if (resourcePath.StartsWith("Assets/"))
            {
                resourcePath = resourcePath.Substring(7); // Remove "Assets/"
            }
            if (resourcePath.Contains("/Resources/"))
            {
                var resourcesIndex = resourcePath.IndexOf("/Resources/") + 11;
                resourcePath = resourcePath.Substring(resourcesIndex);
            }
            
            // Remover extensión para Resources.Load
            var lastDot = resourcePath.LastIndexOf('.');
            if (lastDot > 0)
            {
                resourcePath = resourcePath.Substring(0, lastDot);
            }
            
            return await LoadTextureFromResourcesAsync(resourcePath, cancellationToken);
#endif
        }
        
        /// <summary>
        /// Carga textura desde Resources (Runtime-safe)
        /// </summary>
        /// <param name="resourcePath">Ruta en Resources</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Textura cargada</returns>
        public static async Task<Texture2D> LoadTextureFromResourcesAsync(string resourcePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }
            
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var texture = Resources.Load<Texture2D>(resourcePath);
                return texture;
            }, cancellationToken);
        }
        
        
        
        /// <summary>
        /// Resultado de validación de asset
        /// </summary>
        public struct AssetValidationResult
        {
            public bool IsValid;
            public string ErrorMessage;
            public string AssetPath;
            public Type AssetType;
            
            public AssetValidationResult(bool isValid, string assetPath, Type assetType, string errorMessage = "")
            {
                IsValid = isValid;
                AssetPath = assetPath;
                AssetType = assetType;
                ErrorMessage = errorMessage ?? "";
            }
        }
        
    }
}
