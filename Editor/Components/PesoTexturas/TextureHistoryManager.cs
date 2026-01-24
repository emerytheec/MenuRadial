using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Entrada del historial para una textura individual
    /// </summary>
    [Serializable]
    public class TextureHistoryEntry
    {
        public int originalMaxSize;
        public int stepDownCount;
        public string lastModified;

        public TextureHistoryEntry() { }

        public TextureHistoryEntry(int originalMaxSize)
        {
            this.originalMaxSize = originalMaxSize;
            this.stepDownCount = 0;
            this.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// Contenedor principal del historial
    /// </summary>
    [Serializable]
    public class TextureHistoryData
    {
        public int version = 1;
        public List<TextureHistoryItem> textures = new List<TextureHistoryItem>();

        /// <summary>
        /// Busca una entrada por ruta de asset
        /// </summary>
        public TextureHistoryEntry FindEntry(string assetPath)
        {
            var item = textures.Find(t => t.assetPath == assetPath);
            return item?.entry;
        }

        /// <summary>
        /// Agrega o actualiza una entrada
        /// </summary>
        public void SetEntry(string assetPath, TextureHistoryEntry entry)
        {
            var existing = textures.Find(t => t.assetPath == assetPath);
            if (existing != null)
            {
                existing.entry = entry;
            }
            else
            {
                textures.Add(new TextureHistoryItem { assetPath = assetPath, entry = entry });
            }
        }

        /// <summary>
        /// Elimina una entrada
        /// </summary>
        public bool RemoveEntry(string assetPath)
        {
            return textures.RemoveAll(t => t.assetPath == assetPath) > 0;
        }
    }

    /// <summary>
    /// Item individual en la lista (para serializacion JSON)
    /// </summary>
    [Serializable]
    public class TextureHistoryItem
    {
        public string assetPath;
        public TextureHistoryEntry entry;
    }

    /// <summary>
    /// Manager estatico para el historial de texturas.
    /// Guarda y carga el historial desde un archivo JSON.
    /// </summary>
    public static class TextureHistoryManager
    {
        private const string HISTORY_FOLDER = "Assets/Bender_Dios/Generated";
        private const string HISTORY_FILENAME = "TextureHistory.json";

        private static string HistoryPath => Path.Combine(HISTORY_FOLDER, HISTORY_FILENAME);

        private static TextureHistoryData _cachedData;
        private static bool _isDirty;

        #region Public Methods

        /// <summary>
        /// Registra el estado original de una textura antes de modificarla.
        /// Solo guarda si es la primera vez que se modifica esta textura.
        /// </summary>
        /// <param name="assetPath">Ruta del asset</param>
        /// <param name="currentMaxSize">Tamanio maximo actual (antes del step-down)</param>
        /// <returns>True si se registro (primera vez), False si ya existia</returns>
        public static bool RegisterOriginal(string assetPath, int currentMaxSize)
        {
            var data = LoadData();

            // Si ya existe, no sobrescribir el original
            if (data.FindEntry(assetPath) != null)
                return false;

            var entry = new TextureHistoryEntry(currentMaxSize);
            data.SetEntry(assetPath, entry);
            _isDirty = true;

            return true;
        }

        /// <summary>
        /// Incrementa el contador de step-downs para una textura
        /// </summary>
        public static void IncrementStepDown(string assetPath)
        {
            var data = LoadData();
            var entry = data.FindEntry(assetPath);

            if (entry != null)
            {
                entry.stepDownCount++;
                entry.lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _isDirty = true;
            }
        }

        /// <summary>
        /// Obtiene la resolucion original de una textura
        /// </summary>
        /// <param name="assetPath">Ruta del asset</param>
        /// <returns>Resolucion original, o -1 si no hay historial</returns>
        public static int GetOriginalMaxSize(string assetPath)
        {
            var data = LoadData();
            var entry = data.FindEntry(assetPath);
            return entry?.originalMaxSize ?? -1;
        }

        /// <summary>
        /// Obtiene la cantidad de step-downs aplicados
        /// </summary>
        public static int GetStepDownCount(string assetPath)
        {
            var data = LoadData();
            var entry = data.FindEntry(assetPath);
            return entry?.stepDownCount ?? 0;
        }

        /// <summary>
        /// Verifica si una textura tiene historial (fue modificada)
        /// </summary>
        public static bool HasHistory(string assetPath)
        {
            var data = LoadData();
            return data.FindEntry(assetPath) != null;
        }

        /// <summary>
        /// Elimina el historial de una textura (cuando se restaura)
        /// </summary>
        public static void ClearHistory(string assetPath)
        {
            var data = LoadData();
            if (data.RemoveEntry(assetPath))
            {
                _isDirty = true;
            }
        }

        /// <summary>
        /// Obtiene todas las texturas con historial
        /// </summary>
        public static List<TextureHistoryItem> GetAllEntries()
        {
            var data = LoadData();
            return new List<TextureHistoryItem>(data.textures);
        }

        /// <summary>
        /// Obtiene la cantidad de texturas con historial
        /// </summary>
        public static int GetHistoryCount()
        {
            var data = LoadData();
            return data.textures.Count;
        }

        /// <summary>
        /// Guarda los cambios pendientes al archivo
        /// </summary>
        public static void SaveIfDirty()
        {
            if (_isDirty)
            {
                SaveData();
                _isDirty = false;
            }
        }

        /// <summary>
        /// Fuerza guardar el historial
        /// </summary>
        public static void ForceSave()
        {
            SaveData();
            _isDirty = false;
        }

        /// <summary>
        /// Limpia todo el historial
        /// </summary>
        public static void ClearAllHistory()
        {
            _cachedData = new TextureHistoryData();
            _isDirty = true;
            SaveData();
            _isDirty = false;
        }

        /// <summary>
        /// Recarga el historial desde el archivo (descarta cache)
        /// </summary>
        public static void ReloadFromDisk()
        {
            _cachedData = null;
            _isDirty = false;
            LoadData();
        }

        #endregion

        #region Private Methods

        private static TextureHistoryData LoadData()
        {
            if (_cachedData != null)
                return _cachedData;

            string fullPath = HistoryPath;

            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    _cachedData = JsonUtility.FromJson<TextureHistoryData>(json);

                    if (_cachedData == null)
                        _cachedData = new TextureHistoryData();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TextureHistoryManager] Error al cargar historial: {e.Message}");
                    _cachedData = new TextureHistoryData();
                }
            }
            else
            {
                _cachedData = new TextureHistoryData();
            }

            return _cachedData;
        }

        private static void SaveData()
        {
            if (_cachedData == null)
                return;

            try
            {
                // Asegurar que existe la carpeta
                if (!Directory.Exists(HISTORY_FOLDER))
                {
                    Directory.CreateDirectory(HISTORY_FOLDER);
                }

                string json = JsonUtility.ToJson(_cachedData, true);
                File.WriteAllText(HistoryPath, json);

                // Refrescar AssetDatabase para que Unity vea el archivo
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TextureHistoryManager] Error al guardar historial: {e.Message}");
            }
        }

        #endregion
    }
}
