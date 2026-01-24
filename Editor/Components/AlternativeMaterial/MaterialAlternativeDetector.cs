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
    /// Detector de materiales alternativos.
    /// Analiza la estructura de carpetas y sugiere grupos de materiales basándose en:
    /// - La relación MeshRenderer → carpeta (eje primario)
    /// - Diferenciadores extraídos de nombres de materiales
    /// - Emparejamiento con Levenshtein para tolerancia en concordancia
    ///
    /// Casos soportados:
    /// - Caso 1: MeshRenderers apuntan a carpetas hermanas diferentes → alternativas son materiales en la misma carpeta
    /// - Caso 2: MeshRenderers apuntan a misma carpeta con hermanas → emparejar diferenciadores entre carpetas
    /// - Caso 3: Sin carpetas hermanas → agrupar por similitud de nombres
    /// </summary>
    public class MaterialAlternativeDetector
    {
        private readonly FolderStructureAnalyzer _structureAnalyzer;
        private FolderStructureAnalysis _cachedAnalysis;

        public MaterialAlternativeDetector()
        {
            _structureAnalyzer = new FolderStructureAnalyzer();
        }

        #region Public API

        /// <summary>
        /// Detecta materiales alternativos para todos los slots de un componente.
        /// Respeta el modo forzado y las carpetas seleccionadas configuradas en el componente.
        /// </summary>
        public MaterialSuggestionResult DetectAlternatives(MRAgruparMateriales component)
        {
            if (component == null)
            {
                return new MaterialSuggestionResult(new List<SlotSuggestionResult>());
            }

            // IMPORTANTE: Pasar el modo forzado y las carpetas seleccionadas del componente
            return DetectAlternatives(component.Slots, component.FolderStructureMode, component.SelectedSiblingFolders);
        }

        /// <summary>
        /// Detecta materiales alternativos para una lista de slots.
        /// </summary>
        public MaterialSuggestionResult DetectAlternatives(IEnumerable<MRMaterialSlot> slots)
        {
            return DetectAlternatives(slots, FolderStructureMode.Auto, null);
        }

        /// <summary>
        /// Detecta materiales alternativos para una lista de slots con parámetros de configuración.
        /// </summary>
        /// <param name="slots">Lista de slots a analizar</param>
        /// <param name="forcedMode">Modo forzado (Auto = detectar automáticamente)</param>
        /// <param name="selectedFolders">Lista de carpetas seleccionadas manualmente</param>
        public MaterialSuggestionResult DetectAlternatives(
            IEnumerable<MRMaterialSlot> slots,
            FolderStructureMode forcedMode,
            List<string> selectedFolders)
        {
            var slotResults = new List<SlotSuggestionResult>();

            if (slots == null)
            {
                return new MaterialSuggestionResult(slotResults);
            }

            var slotList = slots.Where(s => s != null && s.IsValid).ToList();
            if (slotList.Count == 0)
            {
                return new MaterialSuggestionResult(slotResults);
            }

#if UNITY_EDITOR
            // Analizar estructura de carpetas con los parámetros de configuración
            _cachedAnalysis = _structureAnalyzer.Analyze(slotList, forcedMode, selectedFolders);

            if (!_cachedAnalysis.IsValid)
            {
                // Sin estructura válida, retornar sin sugerencias
                foreach (var slot in slotList)
                {
                    slotResults.Add(new SlotSuggestionResult(slot, slot.CurrentMaterial, new List<MaterialSuggestion>()));
                }
                return new MaterialSuggestionResult(slotResults);
            }

            // Generar sugerencias según el tipo de estructura
            switch (_cachedAnalysis.StructureType)
            {
                case FolderStructureType.MaterialsGroupedByFolder:
                    slotResults = DetectCase1_GroupedByFolder(slotList, _cachedAnalysis);
                    break;

                case FolderStructureType.MaterialsDistributedInSiblingFolders:
                    slotResults = DetectCase2_DistributedInSiblings(slotList, _cachedAnalysis);
                    break;

                case FolderStructureType.MaterialsMixedInSingleFolder:
                    slotResults = DetectCase3_MixedInSingleFolder(slotList, _cachedAnalysis);
                    break;

                default:
                    foreach (var slot in slotList)
                    {
                        slotResults.Add(new SlotSuggestionResult(slot, slot.CurrentMaterial, new List<MaterialSuggestion>()));
                    }
                    break;
            }
#endif

            return new MaterialSuggestionResult(slotResults);
        }

        /// <summary>
        /// Obtiene el análisis de estructura actual (para debugging o UI).
        /// </summary>
        public FolderStructureAnalysis GetFolderStructureAnalysis(MRAgruparMateriales component)
        {
            if (component == null)
            {
                return new FolderStructureAnalysis { Description = "Componente nulo" };
            }

            return _structureAnalyzer.Analyze(component);
        }

        /// <summary>
        /// Limpia la caché de análisis.
        /// </summary>
        public void ClearCache()
        {
            _cachedAnalysis = null;
        }

        /// <summary>
        /// Extrae el nombre base de un material.
        /// Remueve sufijos numéricos y de variante (_01, _A, etc.)
        /// </summary>
        public static string ExtractBaseName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return "";

            var name = materialName;

            // Remover extensión si existe
            if (name.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            // Remover sufijos numéricos comunes: _00, _01, _1, _2, etc.
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[_\-\s]?\d+$", "");

            // Remover sufijos de letra: _A, _B, _a, _b, etc.
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[_\-\s]?[A-Za-z]$", "");

            return name.Trim();
        }

        #endregion

#if UNITY_EDITOR
        #region Case 1: Materials Grouped By Folder

        /// <summary>
        /// CASO 1: Cada carpeta hermana con slots contiene un grupo de materiales.
        /// TODOS los materiales de la carpeta forman el grupo (incluyendo el actual).
        /// No importa el nombre ni la cantidad de materiales.
        /// </summary>
        private List<SlotSuggestionResult> DetectCase1_GroupedByFolder(
            List<MRMaterialSlot> slots,
            FolderStructureAnalysis analysis)
        {
            var results = new List<SlotSuggestionResult>();

            foreach (var slot in slots)
            {
                var suggestions = new List<MaterialSuggestion>();
                var currentMaterial = slot.CurrentMaterial;

                if (currentMaterial == null)
                {
                    results.Add(new SlotSuggestionResult(slot, null, suggestions));
                    continue;
                }

                string currentPath = AssetDatabase.GetAssetPath(currentMaterial);
                string currentFolder = Path.GetDirectoryName(currentPath)?.Replace("\\", "/");

                // Encontrar la carpeta de este slot
                var folderInfo = analysis.FolderInfos.FirstOrDefault(f => f.FolderPath == currentFolder);

                if (folderInfo != null && folderInfo.MaterialCount >= 2)
                {
                    // TODOS los materiales de la carpeta forman el grupo (incluyendo el actual)
                    // Solo si hay 2+ materiales (un grupo de 1 no tiene alternativas)
                    foreach (var matPath in folderInfo.MaterialPaths)
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (material == null) continue;

                        bool isCurrentMaterial = (matPath == currentPath);

                        var reasons = new List<string>
                        {
                            $"Misma carpeta: {folderInfo.FolderName}",
                            "Grupos en carpeta"
                        };

                        if (isCurrentMaterial)
                        {
                            reasons.Add("(Material actual)");
                        }

                        // Alta confianza porque están en la misma carpeta
                        suggestions.Add(new MaterialSuggestion(material, 0.95f, reasons, matPath));
                    }
                }

                results.Add(new SlotSuggestionResult(slot, currentMaterial, suggestions));
            }

            return results;
        }

        #endregion

        #region Case 2: Materials Distributed In Sibling Folders

        /// <summary>
        /// CASO 2: Emparejar materiales entre carpetas hermanas.
        /// Las carpetas hermanas suelen tener el mismo número de materiales.
        /// Estrategia:
        /// 1. Primero emparejar por diferenciador exacto (incluyendo vacíos)
        /// 2. Luego emparejar por Levenshtein con buena confianza
        /// 3. Finalmente emparejar por eliminación (los que quedan, mismo índice)
        /// Todos los grupos resultantes tienen el mismo tamaño (una entrada por carpeta).
        /// </summary>
        private List<SlotSuggestionResult> DetectCase2_DistributedInSiblings(
            List<MRMaterialSlot> slots,
            FolderStructureAnalysis analysis)
        {
            var results = new List<SlotSuggestionResult>();

            // Construir mapa global de emparejamiento entre todas las carpetas hermanas
            var globalMatching = BuildGlobalFolderMatching(analysis);

            foreach (var slot in slots)
            {
                var suggestions = new List<MaterialSuggestion>();
                var currentMaterial = slot.CurrentMaterial;

                if (currentMaterial == null)
                {
                    results.Add(new SlotSuggestionResult(slot, null, suggestions));
                    continue;
                }

                string currentPath = AssetDatabase.GetAssetPath(currentMaterial);

                // Buscar el grupo de este material en el matching global
                var materialGroup = globalMatching.FirstOrDefault(g =>
                    g.Materials.Any(m => m.Path == currentPath));

                if (materialGroup != null)
                {
                    // Agregar todos los materiales del grupo como sugerencias
                    foreach (var matchedMat in materialGroup.Materials)
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(matchedMat.Path);
                        if (material == null) continue;

                        bool isCurrentMaterial = (matchedMat.Path == currentPath);

                        var reasons = new List<string>
                        {
                            $"Carpeta: {matchedMat.FolderName}"
                        };

                        if (!string.IsNullOrEmpty(materialGroup.MatchReason))
                        {
                            reasons.Add(materialGroup.MatchReason);
                        }

                        if (isCurrentMaterial)
                        {
                            reasons.Add("(Material actual)");
                        }

                        suggestions.Add(new MaterialSuggestion(material, materialGroup.Confidence, reasons, matchedMat.Path));
                    }
                }

                results.Add(new SlotSuggestionResult(slot, currentMaterial, suggestions));
            }

            return results;
        }

        /// <summary>
        /// Información de un material para el matching global.
        /// </summary>
        private class MaterialMatchInfo
        {
            public string Path;
            public string Name;
            public string FolderPath;
            public string FolderName;
            public string Differentiator;
            public int IndexInFolder;
        }

        /// <summary>
        /// Grupo de materiales emparejados entre carpetas.
        /// </summary>
        private class MaterialMatchGroup
        {
            public List<MaterialMatchInfo> Materials = new List<MaterialMatchInfo>();
            public float Confidence;
            public string MatchReason;
        }

        /// <summary>
        /// Construye el emparejamiento global entre todas las carpetas hermanas.
        /// Usa estrategia de 3 pasos: exacto → Levenshtein → eliminación.
        /// </summary>
        private List<MaterialMatchGroup> BuildGlobalFolderMatching(FolderStructureAnalysis analysis)
        {
            var groups = new List<MaterialMatchGroup>();

            if (analysis.DifferentiatorInfos.Count == 0)
                return groups;

            // Crear lista de todos los materiales con su info
            var allMaterials = new List<MaterialMatchInfo>();
            foreach (var diffInfo in analysis.DifferentiatorInfos)
            {
                for (int i = 0; i < diffInfo.Folder.MaterialNames.Count && i < diffInfo.Folder.MaterialPaths.Count; i++)
                {
                    string name = diffInfo.Folder.MaterialNames[i];
                    string diff = diffInfo.MaterialToDifferentiator.ContainsKey(name)
                        ? diffInfo.MaterialToDifferentiator[name]
                        : name;

                    allMaterials.Add(new MaterialMatchInfo
                    {
                        Path = diffInfo.Folder.MaterialPaths[i],
                        Name = name,
                        FolderPath = diffInfo.Folder.FolderPath,
                        FolderName = diffInfo.Folder.FolderName,
                        Differentiator = diff,
                        IndexInFolder = i
                    });
                }
            }

            // Set de materiales ya emparejados
            var matchedPaths = new HashSet<string>();

            // PASO 1: Emparejar por diferenciador exacto (incluyendo vacíos)
            var byDifferentiator = allMaterials
                .GroupBy(m => NormalizeDifferentiator(m.Differentiator))
                .Where(g => g.Select(m => m.FolderPath).Distinct().Count() > 1)
                .ToList();

            foreach (var diffGroup in byDifferentiator)
            {
                var foldersInGroup = diffGroup.Select(m => m.FolderPath).Distinct().ToList();
                if (foldersInGroup.Count <= 1) continue;

                var group = new MaterialMatchGroup
                {
                    Confidence = 0.95f,
                    MatchReason = string.IsNullOrEmpty(diffGroup.Key)
                        ? "Diferenciador vacío (match exacto)"
                        : $"Diferenciador exacto: '{diffGroup.First().Differentiator}'"
                };

                foreach (var mat in diffGroup)
                {
                    if (!matchedPaths.Contains(mat.Path))
                    {
                        group.Materials.Add(mat);
                        matchedPaths.Add(mat.Path);
                    }
                }

                if (group.Materials.Count > 1)
                {
                    groups.Add(group);
                }
            }

            // PASO 2: Emparejar por Levenshtein con buena confianza
            var unmatchedByFolder = allMaterials
                .Where(m => !matchedPaths.Contains(m.Path))
                .GroupBy(m => m.FolderPath)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (unmatchedByFolder.Count > 1)
            {
                var folders = unmatchedByFolder.Keys.ToList();
                var referenceFolder = folders[0];
                var referenceMaterials = unmatchedByFolder[referenceFolder].ToList();

                foreach (var refMat in referenceMaterials.ToList())
                {
                    if (matchedPaths.Contains(refMat.Path)) continue;

                    var group = new MaterialMatchGroup
                    {
                        Confidence = 0.85f
                    };
                    group.Materials.Add(refMat);

                    string refDiffNorm = NormalizeDifferentiator(refMat.Differentiator);

                    foreach (var otherFolder in folders.Skip(1))
                    {
                        if (!unmatchedByFolder.ContainsKey(otherFolder)) continue;

                        var candidates = unmatchedByFolder[otherFolder]
                            .Where(m => !matchedPaths.Contains(m.Path))
                            .ToList();

                        if (candidates.Count == 0) continue;

                        var bestMatch = FindBestLevenshteinMatch(refDiffNorm, candidates);
                        if (bestMatch != null && bestMatch.Distance <= GetMaxAllowedDistance(refDiffNorm))
                        {
                            group.Materials.Add(bestMatch.Material);
                            group.MatchReason = $"Diferenciador similar: '{refMat.Differentiator}' ≈ '{bestMatch.Material.Differentiator}'";
                            group.Confidence = CalculateLevenshteinConfidence(bestMatch.Distance, refDiffNorm.Length);
                        }
                    }

                    if (group.Materials.Count > 1)
                    {
                        foreach (var mat in group.Materials)
                        {
                            matchedPaths.Add(mat.Path);
                        }
                        groups.Add(group);
                    }
                }
            }

            // PASO 3: Emparejar por eliminación (mismo índice en carpeta)
            // Las carpetas hermanas suelen tener el mismo número de materiales
            var stillUnmatchedByFolder = allMaterials
                .Where(m => !matchedPaths.Contains(m.Path))
                .GroupBy(m => m.FolderPath)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.IndexInFolder).ToList());

            if (stillUnmatchedByFolder.Count > 1)
            {
                var foldersList = stillUnmatchedByFolder.Keys.ToList();
                int maxMaterials = stillUnmatchedByFolder.Values.Max(v => v.Count);

                for (int i = 0; i < maxMaterials; i++)
                {
                    var group = new MaterialMatchGroup
                    {
                        Confidence = 0.7f,
                        MatchReason = "Emparejado por eliminación (mismo índice)"
                    };

                    foreach (var folder in foldersList)
                    {
                        var materialsInFolder = stillUnmatchedByFolder[folder];
                        if (i < materialsInFolder.Count)
                        {
                            var mat = materialsInFolder[i];
                            if (!matchedPaths.Contains(mat.Path))
                            {
                                group.Materials.Add(mat);
                                matchedPaths.Add(mat.Path);
                            }
                        }
                    }

                    if (group.Materials.Count > 1)
                    {
                        groups.Add(group);
                    }
                }
            }

            return groups;
        }

        /// <summary>
        /// Resultado de búsqueda Levenshtein.
        /// </summary>
        private class LevenshteinMatchResult
        {
            public MaterialMatchInfo Material;
            public int Distance;
        }

        /// <summary>
        /// Encuentra el mejor match por Levenshtein en una lista de candidatos.
        /// </summary>
        private LevenshteinMatchResult FindBestLevenshteinMatch(string sourceDiffNorm, List<MaterialMatchInfo> candidates)
        {
            LevenshteinMatchResult best = null;
            int bestDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                string candDiffNorm = NormalizeDifferentiator(candidate.Differentiator);
                int distance = LevenshteinDistance(sourceDiffNorm, candDiffNorm);

                // Bonus si uno contiene al otro
                if (!string.IsNullOrEmpty(sourceDiffNorm) && !string.IsNullOrEmpty(candDiffNorm))
                {
                    if (sourceDiffNorm.Contains(candDiffNorm) || candDiffNorm.Contains(sourceDiffNorm))
                    {
                        distance = Math.Min(distance, Math.Abs(sourceDiffNorm.Length - candDiffNorm.Length));
                    }
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new LevenshteinMatchResult
                    {
                        Material = candidate,
                        Distance = distance
                    };
                }
            }

            return best;
        }

        /// <summary>
        /// Calcula la distancia máxima permitida para considerar un match válido.
        /// </summary>
        private int GetMaxAllowedDistance(string normalizedDiff)
        {
            if (string.IsNullOrEmpty(normalizedDiff)) return 0; // Vacío solo matchea con vacío
            return Math.Max(2, normalizedDiff.Length / 3);
        }

        /// <summary>
        /// Calcula la confianza basada en la distancia de Levenshtein.
        /// </summary>
        private float CalculateLevenshteinConfidence(int distance, int sourceLength)
        {
            if (distance == 0) return 0.95f;
            float distanceRatio = (float)distance / Math.Max(sourceLength, 1);
            return Mathf.Clamp(0.9f - (distanceRatio * 0.4f), 0.5f, 0.9f);
        }

        /// <summary>
        /// Normaliza un diferenciador para comparación.
        /// Remueve caracteres no alfanuméricos y convierte a minúsculas.
        /// </summary>
        private string NormalizeDifferentiator(string diff)
        {
            if (string.IsNullOrEmpty(diff)) return "";

            var normalized = new System.Text.StringBuilder();
            foreach (char c in diff.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    normalized.Append(c);
                }
            }
            return normalized.ToString();
        }

        /// <summary>
        /// Calcula la distancia de Levenshtein entre dos strings.
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        #endregion

        #region Case 3: Materials Mixed In Single Folder

        /// <summary>
        /// CASO 3: Sin carpetas hermanas, agrupar por similitud de nombres usando Levenshtein.
        /// TODOS los materiales con nombre base similar forman el grupo (incluyendo el actual).
        /// </summary>
        private List<SlotSuggestionResult> DetectCase3_MixedInSingleFolder(
            List<MRMaterialSlot> slots,
            FolderStructureAnalysis analysis)
        {
            var results = new List<SlotSuggestionResult>();

            if (analysis.FolderInfos.Count == 0)
            {
                foreach (var slot in slots)
                {
                    results.Add(new SlotSuggestionResult(slot, slot.CurrentMaterial, new List<MaterialSuggestion>()));
                }
                return results;
            }

            var folderInfo = analysis.FolderInfos[0];

            // Agrupar materiales por nombre base
            var baseNameGroups = GroupMaterialsByBaseName(folderInfo);

            foreach (var slot in slots)
            {
                var suggestions = new List<MaterialSuggestion>();
                var currentMaterial = slot.CurrentMaterial;

                if (currentMaterial == null)
                {
                    results.Add(new SlotSuggestionResult(slot, null, suggestions));
                    continue;
                }

                string currentPath = AssetDatabase.GetAssetPath(currentMaterial);
                string currentName = Path.GetFileNameWithoutExtension(currentPath);
                string currentBaseName = ExtractBaseName(currentName);

                // Buscar grupo del material actual
                // Solo si hay 2+ materiales (un grupo de 1 no tiene alternativas)
                if (baseNameGroups.TryGetValue(currentBaseName, out var group) && group.Count >= 2)
                {
                    foreach (var matPath in group)
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (material == null) continue;

                        bool isCurrentMaterial = (matPath == currentPath);
                        string matName = Path.GetFileNameWithoutExtension(matPath);

                        var reasons = new List<string>
                        {
                            $"Mismo nombre base: {currentBaseName}",
                            "Todo en carpeta"
                        };

                        if (isCurrentMaterial)
                        {
                            reasons.Add("(Material actual)");
                        }

                        // Calcular confianza basada en similitud de nombre
                        int distance = LevenshteinDistance(currentName.ToLower(), matName.ToLower());
                        float maxLen = Math.Max(currentName.Length, matName.Length);
                        float similarity = 1f - (distance / maxLen);
                        float confidence = Mathf.Clamp(similarity, 0.6f, 0.9f);

                        suggestions.Add(new MaterialSuggestion(material, confidence, reasons, matPath));
                    }
                }

                // Si no encontró grupo exacto, buscar con Levenshtein en todos los materiales
                if (suggestions.Count == 0)
                {
                    suggestions = FindSimilarMaterialsWithLevenshtein(currentPath, currentName, folderInfo);
                }

                results.Add(new SlotSuggestionResult(slot, currentMaterial, suggestions));
            }

            return results;
        }

        /// <summary>
        /// Agrupa materiales por nombre base en una carpeta.
        /// </summary>
        private Dictionary<string, List<string>> GroupMaterialsByBaseName(FolderMaterialInfo folderInfo)
        {
            var groups = new Dictionary<string, List<string>>();

            for (int i = 0; i < folderInfo.MaterialNames.Count && i < folderInfo.MaterialPaths.Count; i++)
            {
                string name = folderInfo.MaterialNames[i];
                string path = folderInfo.MaterialPaths[i];
                string baseName = ExtractBaseName(name);

                if (!groups.ContainsKey(baseName))
                {
                    groups[baseName] = new List<string>();
                }
                groups[baseName].Add(path);
            }

            return groups;
        }

        /// <summary>
        /// Busca materiales similares usando Levenshtein cuando no hay grupo exacto.
        /// Incluye el material actual como parte del grupo.
        /// </summary>
        private List<MaterialSuggestion> FindSimilarMaterialsWithLevenshtein(
            string currentPath,
            string currentName,
            FolderMaterialInfo folderInfo)
        {
            var suggestions = new List<MaterialSuggestion>();
            string normalizedCurrent = NormalizeDifferentiator(currentName);

            var candidates = new List<(string path, string name, int distance, bool isCurrent)>();

            for (int i = 0; i < folderInfo.MaterialNames.Count && i < folderInfo.MaterialPaths.Count; i++)
            {
                string matPath = folderInfo.MaterialPaths[i];
                string matName = folderInfo.MaterialNames[i];
                string normalizedName = NormalizeDifferentiator(matName);
                bool isCurrent = (matPath == currentPath);

                int distance = LevenshteinDistance(normalizedCurrent, normalizedName);

                // Umbral: nombres muy diferentes se ignoran (pero el actual siempre se incluye)
                int maxDistance = Math.Max(3, normalizedCurrent.Length / 2);
                if (distance <= maxDistance || isCurrent)
                {
                    candidates.Add((matPath, matName, distance, isCurrent));
                }
            }

            // Ordenar por distancia y tomar los mejores
            foreach (var candidate in candidates.OrderBy(c => c.distance).Take(6))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(candidate.path);
                if (material == null) continue;

                var reasons = new List<string>
                {
                    $"Nombre similar a: {currentName}",
                    $"Distancia Levenshtein: {candidate.distance}"
                };

                if (candidate.isCurrent)
                {
                    reasons.Add("(Material actual)");
                }

                float maxLen = Math.Max(normalizedCurrent.Length, 1);
                float confidence = Mathf.Clamp(0.7f - (candidate.distance / maxLen * 0.3f), 0.4f, 0.7f);

                suggestions.Add(new MaterialSuggestion(material, confidence, reasons, candidate.path));
            }

            return suggestions;
        }

        #endregion
#endif
    }
}
