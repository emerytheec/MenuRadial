using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bender_Dios.MenuRadial.Editor.Components.AlternativeMaterial
{
    /// <summary>
    /// Tipos de estructura de carpetas detectados.
    /// </summary>
    public enum FolderStructureType
    {
        /// <summary>
        /// No determinado o sin slots.
        /// </summary>
        Unknown,

        /// <summary>
        /// CASO 1: MeshRenderers apuntan a carpetas hermanas diferentes.
        /// TODOS los materiales de cada carpeta forman el grupo (nombres no importan).
        /// </summary>
        MaterialsGroupedByFolder,

        /// <summary>
        /// CASO 2: MeshRenderers apuntan a la misma carpeta.
        /// Existen carpetas hermanas con variantes identificables por diferenciadores.
        /// </summary>
        MaterialsDistributedInSiblingFolders,

        /// <summary>
        /// CASO 3: Todos los materiales en una única carpeta sin hermanas útiles.
        /// Solo se puede agrupar por similitud de nombres.
        /// </summary>
        MaterialsMixedInSingleFolder
    }

    /// <summary>
    /// Información sobre una carpeta de materiales.
    /// </summary>
    public class FolderMaterialInfo
    {
        public string FolderPath { get; set; }
        public string FolderName { get; set; }
        public string ParentFolderPath { get; set; }
        public List<string> MaterialPaths { get; set; } = new List<string>();
        public List<string> MaterialNames { get; set; } = new List<string>();
        public List<MRMaterialSlot> SlotsPointingHere { get; set; } = new List<MRMaterialSlot>();

        public int MaterialCount => MaterialPaths?.Count ?? 0;
        public int SlotCount => SlotsPointingHere?.Count ?? 0;
        public bool HasSlots => SlotCount > 0;
    }

    /// <summary>
    /// Información sobre el diferenciador extraído de una carpeta.
    /// </summary>
    public class FolderDifferentiatorInfo
    {
        public FolderMaterialInfo Folder { get; set; }
        public string CommonPrefix { get; set; } = "";
        public string CommonSuffix { get; set; } = "";
        public List<string> Differentiators { get; set; } = new List<string>();

        /// <summary>
        /// Mapa: nombre de material (sin extensión) -> diferenciador
        /// </summary>
        public Dictionary<string, string> MaterialToDifferentiator { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Resultado del análisis de estructura de carpetas.
    /// </summary>
    public class FolderStructureAnalysis
    {
        public FolderStructureType StructureType { get; set; } = FolderStructureType.Unknown;
        public float Confidence { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Todas las carpetas donde hay materiales de los slots.
        /// </summary>
        public List<FolderMaterialInfo> FolderInfos { get; set; } = new List<FolderMaterialInfo>();

        /// <summary>
        /// Carpetas hermanas (mismo padre) que tienen slots apuntando a ellas.
        /// </summary>
        public List<FolderMaterialInfo> SiblingFoldersWithSlots { get; set; } = new List<FolderMaterialInfo>();

        /// <summary>
        /// Todas las carpetas hermanas (incluyendo las que no tienen slots).
        /// </summary>
        public List<FolderMaterialInfo> AllSiblingFolders { get; set; } = new List<FolderMaterialInfo>();

        /// <summary>
        /// Alias para AllSiblingFolders (compatibilidad con editor).
        /// </summary>
        public List<FolderMaterialInfo> SiblingFolders => AllSiblingFolders;

        /// <summary>
        /// Información de diferenciadores por carpeta.
        /// </summary>
        public List<FolderDifferentiatorInfo> DifferentiatorInfos { get; set; } = new List<FolderDifferentiatorInfo>();

        public int TotalSlots { get; set; }

        /// <summary>
        /// Carpeta principal (dominante) para Caso 2.
        /// Cuando hay múltiples carpetas con slots pero una tiene la mayoría,
        /// esta es la carpeta principal y las otras se consideran referencias puntuales.
        /// </summary>
        public FolderMaterialInfo PrimaryFolder { get; set; }

        /// <summary>
        /// Carpetas con referencias puntuales (minoritarias) que se ignoran en Caso 2.
        /// </summary>
        public List<FolderMaterialInfo> SecondaryFoldersWithSlots { get; set; } = new List<FolderMaterialInfo>();

        public int UniqueFolderCount => FolderInfos?.Count ?? 0;
        public bool IsValid => StructureType != FolderStructureType.Unknown && TotalSlots > 0;
    }

    /// <summary>
    /// Analizador de estructura de carpetas para materiales.
    /// Determina el tipo de organización basándose en la relación MeshRenderer → carpeta.
    ///
    /// La lógica de detección es:
    /// 1. Obtener las carpetas donde apuntan los materiales de los slots
    /// 2. Identificar carpetas hermanas (mismo directorio padre)
    /// 3. Determinar el caso según la distribución de slots en carpetas
    /// </summary>
    public class FolderStructureAnalyzer
    {
        #region Public API

        /// <summary>
        /// Analiza la estructura de carpetas de los slots.
        /// </summary>
        /// <param name="slots">Lista de slots a analizar</param>
        /// <param name="forcedMode">Modo forzado (Auto = detectar automáticamente)</param>
        /// <param name="selectedFolders">Lista de carpetas seleccionadas manualmente (null = usar todas)</param>
        public FolderStructureAnalysis Analyze(
            IEnumerable<MRMaterialSlot> slots,
            FolderStructureMode forcedMode = FolderStructureMode.Auto,
            List<string> selectedFolders = null)
        {
            var result = new FolderStructureAnalysis();

            if (slots == null)
            {
                result.Description = "Sin slots para analizar";
                return result;
            }

            var slotList = slots.Where(s => s != null && s.IsValid).ToList();
            result.TotalSlots = slotList.Count;

            if (slotList.Count == 0)
            {
                result.Description = "No hay slots válidos";
                return result;
            }

#if UNITY_EDITOR
            // Paso 1: Obtener información de carpetas donde están los materiales
            var folderInfos = CollectFolderInfos(slotList);
            result.FolderInfos = folderInfos;

            if (folderInfos.Count == 0)
            {
                result.Description = "No se encontraron carpetas de materiales";
                return result;
            }

            // Paso 2: Identificar carpetas hermanas
            var (siblingFoldersWithSlots, allSiblingFolders) = FindSiblingFolders(folderInfos);

            // Si hay carpetas seleccionadas manualmente, filtrar TODAS las listas
            if (selectedFolders != null && selectedFolders.Count > 0 && forcedMode != FolderStructureMode.Auto)
            {
                var selectedSet = new HashSet<string>(selectedFolders);

                // Filtrar FolderInfos también para que el Caso 1 respete la selección
                folderInfos = folderInfos
                    .Where(f => selectedSet.Contains(f.FolderPath))
                    .ToList();
                result.FolderInfos = folderInfos;

                allSiblingFolders = allSiblingFolders
                    .Where(f => selectedSet.Contains(f.FolderPath))
                    .ToList();
                siblingFoldersWithSlots = siblingFoldersWithSlots
                    .Where(f => selectedSet.Contains(f.FolderPath))
                    .ToList();
            }

            result.SiblingFoldersWithSlots = siblingFoldersWithSlots;
            result.AllSiblingFolders = allSiblingFolders;

            // Paso 3: Determinar el tipo de estructura
            if (forcedMode != FolderStructureMode.Auto)
            {
                // Modo forzado por el usuario
                ApplyForcedStructureType(result, forcedMode, allSiblingFolders);
            }
            else
            {
                // Detección automática
                DetermineStructureType(result, folderInfos, siblingFoldersWithSlots, allSiblingFolders);
            }

            // Paso 4: Extraer diferenciadores si aplica
            if (result.StructureType == FolderStructureType.MaterialsGroupedByFolder ||
                result.StructureType == FolderStructureType.MaterialsDistributedInSiblingFolders)
            {
                ExtractDifferentiators(result);
            }
#else
            result.Description = "Solo disponible en editor";
#endif

            return result;
        }

        /// <summary>
        /// Analiza la estructura de carpetas de un componente.
        /// Respeta el modo forzado y las carpetas seleccionadas configuradas en el componente.
        /// </summary>
        public FolderStructureAnalysis Analyze(MRAgruparMateriales component)
        {
            if (component == null)
                return new FolderStructureAnalysis { Description = "Componente nulo" };

            return Analyze(component.Slots, component.FolderStructureMode, component.SelectedSiblingFolders);
        }

        /// <summary>
        /// Obtiene todas las carpetas hermanas disponibles para un componente.
        /// Útil para mostrar opciones en el editor.
        /// </summary>
        public List<FolderMaterialInfo> GetAvailableSiblingFolders(MRAgruparMateriales component)
        {
            if (component == null || component.Slots == null)
                return new List<FolderMaterialInfo>();

            var slotList = component.Slots.Where(s => s != null && s.IsValid).ToList();
            if (slotList.Count == 0)
                return new List<FolderMaterialInfo>();

#if UNITY_EDITOR
            var folderInfos = CollectFolderInfos(slotList);
            if (folderInfos.Count == 0)
                return new List<FolderMaterialInfo>();

            var (_, allSiblingFolders) = FindSiblingFolders(folderInfos);
            return allSiblingFolders;
#else
            return new List<FolderMaterialInfo>();
#endif
        }

        #endregion

#if UNITY_EDITOR
        #region Private Methods

        /// <summary>
        /// Recolecta información sobre las carpetas donde están los materiales de los slots.
        /// </summary>
        private List<FolderMaterialInfo> CollectFolderInfos(List<MRMaterialSlot> slots)
        {
            var folderDict = new Dictionary<string, FolderMaterialInfo>();

            foreach (var slot in slots)
            {
                var material = slot.CurrentMaterial;
                if (material == null) continue;

                string materialPath = AssetDatabase.GetAssetPath(material);
                if (string.IsNullOrEmpty(materialPath)) continue;

                string folderPath = Path.GetDirectoryName(materialPath)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(folderPath)) continue;

                if (!folderDict.TryGetValue(folderPath, out var folderInfo))
                {
                    folderInfo = new FolderMaterialInfo
                    {
                        FolderPath = folderPath,
                        FolderName = Path.GetFileName(folderPath),
                        ParentFolderPath = Path.GetDirectoryName(folderPath)?.Replace("\\", "/")
                    };

                    // Cargar todos los materiales de esta carpeta
                    LoadMaterialsInFolder(folderInfo);
                    folderDict[folderPath] = folderInfo;
                }

                // Registrar que este slot apunta a esta carpeta
                if (!folderInfo.SlotsPointingHere.Contains(slot))
                {
                    folderInfo.SlotsPointingHere.Add(slot);
                }
            }

            return folderDict.Values.ToList();
        }

        /// <summary>
        /// Carga todos los materiales .mat de una carpeta.
        /// </summary>
        private void LoadMaterialsInFolder(FolderMaterialInfo folderInfo)
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderInfo.FolderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Solo incluir materiales directamente en esta carpeta (no subcarpetas)
                string materialFolder = Path.GetDirectoryName(path)?.Replace("\\", "/");
                if (materialFolder != folderInfo.FolderPath) continue;

                string materialName = Path.GetFileNameWithoutExtension(path);

                folderInfo.MaterialPaths.Add(path);
                folderInfo.MaterialNames.Add(materialName);
            }
        }

        /// <summary>
        /// Encuentra carpetas hermanas (mismo directorio padre).
        /// Retorna: (carpetas hermanas con slots, todas las carpetas hermanas)
        /// </summary>
        private (List<FolderMaterialInfo>, List<FolderMaterialInfo>) FindSiblingFolders(List<FolderMaterialInfo> folderInfos)
        {
            var siblingFoldersWithSlots = new List<FolderMaterialInfo>();
            var allSiblingFolders = new List<FolderMaterialInfo>();

            if (folderInfos.Count == 0) return (siblingFoldersWithSlots, allSiblingFolders);

            // Agrupar por directorio padre
            var byParent = folderInfos.GroupBy(f => f.ParentFolderPath).ToList();

            foreach (var group in byParent)
            {
                string parentPath = group.Key;
                if (string.IsNullOrEmpty(parentPath)) continue;

                var foldersWithSlotsInGroup = group.ToList();

                // Buscar TODAS las carpetas hermanas en este padre (incluyendo las sin slots)
                var allSiblingsInParent = FindAllSiblingFoldersInParent(parentPath, foldersWithSlotsInGroup);

                // Si hay múltiples carpetas con slots en el mismo padre, son hermanas (Caso 1)
                if (foldersWithSlotsInGroup.Count > 1)
                {
                    siblingFoldersWithSlots.AddRange(foldersWithSlotsInGroup);
                    allSiblingFolders.AddRange(allSiblingsInParent);
                }
                // Si hay 1 carpeta con slots pero tiene hermanas con materiales (Caso 2)
                else if (foldersWithSlotsInGroup.Count == 1 && allSiblingsInParent.Count > 1)
                {
                    siblingFoldersWithSlots.Add(foldersWithSlotsInGroup[0]);
                    allSiblingFolders.AddRange(allSiblingsInParent);
                }
                // Si hay 1 carpeta sin hermanas útiles (Caso 3)
                else if (foldersWithSlotsInGroup.Count == 1)
                {
                    // No agregar a siblingFoldersWithSlots (no tiene hermanas con slots)
                    // allSiblingFolders queda vacío o solo con esta carpeta
                    if (allSiblingsInParent.Count > 0)
                    {
                        allSiblingFolders.AddRange(allSiblingsInParent);
                    }
                }
            }

            return (siblingFoldersWithSlots, allSiblingFolders);
        }

        /// <summary>
        /// Encuentra todas las carpetas que contienen materiales en el directorio padre.
        /// </summary>
        private List<FolderMaterialInfo> FindAllSiblingFoldersInParent(string parentPath, List<FolderMaterialInfo> existingInfos)
        {
            var result = new List<FolderMaterialInfo>();
            if (string.IsNullOrEmpty(parentPath)) return result;

            // Diccionario de carpetas ya conocidas
            var existingPaths = new HashSet<string>(existingInfos.Select(f => f.FolderPath));

            // Buscar subcarpetas del padre
            string[] subFolderGuids = AssetDatabase.FindAssets("", new[] { parentPath });
            var subFolderPaths = new HashSet<string>();

            foreach (string guid in subFolderGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;

                string folderPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(folderPath)) continue;

                // Solo subcarpetas directas del padre
                string folderParent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
                if (folderParent != parentPath) continue;

                subFolderPaths.Add(folderPath);
            }

            foreach (string folderPath in subFolderPaths)
            {
                // Si ya tenemos info de esta carpeta, usarla
                var existing = existingInfos.FirstOrDefault(f => f.FolderPath == folderPath);
                if (existing != null)
                {
                    result.Add(existing);
                }
                else
                {
                    // Crear info para carpeta hermana sin slots
                    var newInfo = new FolderMaterialInfo
                    {
                        FolderPath = folderPath,
                        FolderName = Path.GetFileName(folderPath),
                        ParentFolderPath = parentPath
                    };
                    LoadMaterialsInFolder(newInfo);

                    // Solo agregar si tiene materiales
                    if (newInfo.MaterialCount > 0)
                    {
                        result.Add(newInfo);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determina el tipo de estructura basándose en la distribución de slots.
        /// </summary>
        private void DetermineStructureType(
            FolderStructureAnalysis result,
            List<FolderMaterialInfo> folderInfos,
            List<FolderMaterialInfo> siblingFoldersWithSlots,
            List<FolderMaterialInfo> allSiblingFolders)
        {
            int foldersWithSlots = folderInfos.Count;
            int siblingCount = allSiblingFolders.Count;
            int totalSlots = result.TotalSlots;

            // Verificar si hay una carpeta dominante (tiene la mayoría de los slots)
            // Esto permite detectar Caso 2 cuando hay referencias puntuales en otras carpetas
            if (siblingFoldersWithSlots.Count > 1 && totalSlots > 0)
            {
                var dominantFolder = FindDominantFolder(siblingFoldersWithSlots, totalSlots);

                if (dominantFolder != null)
                {
                    // Hay una carpeta dominante → Caso 2 con referencias puntuales
                    result.StructureType = FolderStructureType.MaterialsDistributedInSiblingFolders;
                    result.Confidence = 0.85f;
                    result.PrimaryFolder = dominantFolder;

                    // Las demás carpetas con slots son referencias puntuales (secundarias)
                    result.SecondaryFoldersWithSlots = siblingFoldersWithSlots
                        .Where(f => f.FolderPath != dominantFolder.FolderPath)
                        .ToList();

                    int secondaryCount = result.SecondaryFoldersWithSlots.Sum(f => f.SlotCount);
                    result.Description = $"Caso 2: Carpeta principal '{dominantFolder.FolderName}' con {dominantFolder.SlotCount} slots. " +
                                        $"{result.SecondaryFoldersWithSlots.Count} carpetas con {secondaryCount} referencias puntuales. " +
                                        $"{siblingCount} carpetas hermanas totales.";
                    return;
                }
            }

            // CASO 1: Múltiples carpetas con slots que son hermanas (distribución equilibrada)
            // Los MeshRenderers apuntan a carpetas DIFERENTES que son hermanas entre sí
            // TODOS los materiales de cada carpeta forman el grupo (nombres no importan)
            if (siblingFoldersWithSlots.Count > 1)
            {
                result.StructureType = FolderStructureType.MaterialsGroupedByFolder;
                result.Confidence = 0.9f;
                result.Description = $"Caso 1: {siblingFoldersWithSlots.Count} carpetas hermanas con slots (distribución equilibrada). " +
                                    "Todos los materiales de cada carpeta forman el grupo.";
                return;
            }

            // CASO 2: Una carpeta con slots pero tiene carpetas hermanas con materiales
            // Los MeshRenderers apuntan a la MISMA carpeta, hay hermanas con variantes
            if (foldersWithSlots == 1 && siblingCount > 1)
            {
                result.StructureType = FolderStructureType.MaterialsDistributedInSiblingFolders;
                result.Confidence = 0.85f;
                result.PrimaryFolder = folderInfos[0];
                result.Description = $"Caso 2: 1 carpeta con slots, {siblingCount} carpetas hermanas. " +
                                    "Buscar concordancia de diferenciadores entre carpetas.";
                return;
            }

            // CASO 3: Sin carpetas hermanas útiles
            // Todo en una carpeta sin variantes en hermanas
            if (foldersWithSlots == 1 && siblingCount <= 1)
            {
                result.StructureType = FolderStructureType.MaterialsMixedInSingleFolder;
                result.Confidence = 0.7f;
                result.Description = "Caso 3: Una única carpeta sin hermanas útiles. " +
                                    "Agrupar por similitud de nombres.";
                return;
            }

            // Caso no determinado (múltiples carpetas sin relación de hermanas)
            result.StructureType = FolderStructureType.Unknown;
            result.Confidence = 0.3f;
            result.Description = $"Estructura no determinada: {foldersWithSlots} carpetas sin relación clara.";
        }

        /// <summary>
        /// Busca una carpeta dominante que tenga la mayoría de los slots.
        /// Una carpeta es dominante si tiene >= 70% de los slots totales.
        /// </summary>
        /// <returns>La carpeta dominante o null si la distribución es equilibrada.</returns>
        private FolderMaterialInfo FindDominantFolder(List<FolderMaterialInfo> foldersWithSlots, int totalSlots)
        {
            if (foldersWithSlots == null || foldersWithSlots.Count < 2 || totalSlots <= 0)
                return null;

            // Umbral: una carpeta debe tener >= 70% de los slots para ser dominante
            const float dominanceThreshold = 0.70f;
            int minSlotsForDominance = (int)Math.Ceiling(totalSlots * dominanceThreshold);

            // Ordenar por cantidad de slots descendente
            var ordered = foldersWithSlots.OrderByDescending(f => f.SlotCount).ToList();
            var topFolder = ordered[0];

            // Verificar si la carpeta top es dominante
            if (topFolder.SlotCount >= minSlotsForDominance)
            {
                return topFolder;
            }

            return null;
        }

        /// <summary>
        /// Aplica un tipo de estructura forzado por el usuario.
        /// </summary>
        private void ApplyForcedStructureType(
            FolderStructureAnalysis result,
            FolderStructureMode forcedMode,
            List<FolderMaterialInfo> allSiblingFolders)
        {
            switch (forcedMode)
            {
                case FolderStructureMode.Case1_GroupedByFolder:
                    result.StructureType = FolderStructureType.MaterialsGroupedByFolder;
                    result.Confidence = 1.0f;
                    result.Description = "Caso 1 (FORZADO): Materiales agrupados por carpeta.";
                    break;

                case FolderStructureMode.Case2_DistributedInSiblings:
                    result.StructureType = FolderStructureType.MaterialsDistributedInSiblingFolders;
                    result.Confidence = 1.0f;

                    // Identificar carpeta principal (la que tiene más slots)
                    var foldersWithSlots = allSiblingFolders.Where(f => f.HasSlots).OrderByDescending(f => f.SlotCount).ToList();
                    if (foldersWithSlots.Count > 0)
                    {
                        result.PrimaryFolder = foldersWithSlots[0];
                        result.SecondaryFoldersWithSlots = foldersWithSlots.Skip(1).ToList();
                    }
                    else if (result.FolderInfos.Count > 0)
                    {
                        // Si no hay carpetas hermanas con slots en la selección, usar la primera del análisis
                        result.PrimaryFolder = result.FolderInfos.OrderByDescending(f => f.SlotCount).First();
                    }

                    result.Description = $"Caso 2 (FORZADO): Materiales en {allSiblingFolders.Count} carpetas hermanas.";
                    break;

                case FolderStructureMode.Case3_MixedInSingleFolder:
                    result.StructureType = FolderStructureType.MaterialsMixedInSingleFolder;
                    result.Confidence = 1.0f;
                    result.Description = "Caso 3 (FORZADO): Materiales mezclados en carpeta única.";
                    break;

                default:
                    result.StructureType = FolderStructureType.Unknown;
                    result.Confidence = 0.5f;
                    result.Description = "Modo no reconocido.";
                    break;
            }
        }

        /// <summary>
        /// Extrae diferenciadores de los nombres de materiales en cada carpeta.
        /// El diferenciador es lo que queda después de remover las partes comunes a TODOS los materiales.
        /// </summary>
        private void ExtractDifferentiators(FolderStructureAnalysis result)
        {
            var foldersToAnalyze = result.AllSiblingFolders.Count > 0
                ? result.AllSiblingFolders
                : result.FolderInfos;

            foreach (var folder in foldersToAnalyze)
            {
                if (folder.MaterialCount == 0) continue;

                var diffInfo = new FolderDifferentiatorInfo
                {
                    Folder = folder
                };

                var names = folder.MaterialNames;

                // Encontrar prefijo común más largo
                diffInfo.CommonPrefix = FindLongestCommonPrefix(names);

                // Encontrar sufijo común más largo
                diffInfo.CommonSuffix = FindLongestCommonSuffix(names);

                // Extraer diferenciadores removiendo prefijo y sufijo comunes
                foreach (var name in names)
                {
                    string diff = ExtractDifferentiator(name, diffInfo.CommonPrefix, diffInfo.CommonSuffix);
                    diffInfo.Differentiators.Add(diff);
                    diffInfo.MaterialToDifferentiator[name] = diff;
                }

                result.DifferentiatorInfos.Add(diffInfo);
            }
        }

        /// <summary>
        /// Encuentra el prefijo común más largo de una lista de strings.
        /// </summary>
        private string FindLongestCommonPrefix(List<string> strings)
        {
            if (strings == null || strings.Count == 0) return "";
            if (strings.Count == 1) return "";  // Sin otros strings para comparar, no hay "común"

            string first = strings[0];
            int prefixLength = 0;

            for (int i = 0; i < first.Length; i++)
            {
                char c = first[i];
                bool allMatch = true;

                for (int j = 1; j < strings.Count; j++)
                {
                    if (i >= strings[j].Length || strings[j][i] != c)
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                    prefixLength = i + 1;
                else
                    break;
            }

            return first.Substring(0, prefixLength);
        }

        /// <summary>
        /// Encuentra el sufijo común más largo de una lista de strings.
        /// </summary>
        private string FindLongestCommonSuffix(List<string> strings)
        {
            if (strings == null || strings.Count == 0) return "";
            if (strings.Count == 1) return "";

            // Invertir strings y buscar prefijo común
            var reversed = strings.Select(s => new string(s.Reverse().ToArray())).ToList();
            string reversedPrefix = FindLongestCommonPrefix(reversed);

            return new string(reversedPrefix.Reverse().ToArray());
        }

        /// <summary>
        /// Extrae el diferenciador removiendo prefijo y sufijo comunes.
        /// </summary>
        private string ExtractDifferentiator(string name, string prefix, string suffix)
        {
            string result = name;

            // Remover prefijo
            if (!string.IsNullOrEmpty(prefix) && result.StartsWith(prefix))
            {
                result = result.Substring(prefix.Length);
            }

            // Remover sufijo
            if (!string.IsNullOrEmpty(suffix) && result.EndsWith(suffix))
            {
                result = result.Substring(0, result.Length - suffix.Length);
            }

            return result;
        }

        #endregion
#endif
    }
}
