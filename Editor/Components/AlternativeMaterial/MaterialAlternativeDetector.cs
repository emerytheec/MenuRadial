using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;

namespace Bender_Dios.MenuRadial.Editor.Components.AlternativeMaterial
{
    /// <summary>
    /// Detects alternative materials for slots using folder analysis and name pattern matching.
    /// Implements Proposals 1 (folder analysis) and 2 (name pattern analysis).
    /// </summary>
    public class MaterialAlternativeDetector
    {
        #region Token Dictionaries

        /// <summary>
        /// Known color tokens in multiple languages.
        /// </summary>
        private static readonly HashSet<string> ColorTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // English
            "red", "blue", "green", "black", "white", "pink", "purple", "yellow", "orange",
            "brown", "gray", "grey", "cyan", "magenta", "gold", "silver", "beige", "navy",
            "teal", "violet", "indigo", "cream", "tan", "maroon", "olive", "coral", "salmon",
            "turquoise", "lavender", "mint", "peach", "rose", "ruby", "emerald", "sapphire",
            "burgundy", "khaki", "charcoal", "ivory", "bronze", "copper", "crimson", "scarlet",

            // Japanese (romaji)
            "aka", "ao", "midori", "kuro", "shiro", "pinku", "murasaki", "kiiro", "orenji",
            "chairo", "haiiro", "gin", "kin", "momoiro", "sora", "sakura",

            // Spanish
            "rojo", "azul", "verde", "negro", "blanco", "rosa", "morado", "amarillo", "naranja",
            "marron", "gris", "dorado", "plateado", "celeste",

            // Single letter abbreviations (very common in Japanese avatar assets)
            "r", "g", "b", "w", "p", "y", "o", "k", "c",

            // Two letter abbreviations (common in Japanese/Asian assets)
            "bl", "bk", "br", "pk", "pp", "yl", "or", "gr", "gy", "wh", "rd", "gn",
            "be", "pi", "pu", "ye", "og", "bw", "lg", "dg", "lb", "db",

            // Common English abbreviations
            "blk", "wht", "pnk", "prp", "ylw", "org", "brn", "gry", "grn", "blu",

            // Color variations/modifiers (used as prefix or standalone)
            "light", "dark", "pale", "deep", "bright", "pastel", "neon", "soft", "vivid",

            // Compound color modifiers (commonly used with other colors)
            "wine", "sky", "sea", "forest", "royal", "hot", "ice", "baby", "dusty", "dirty"
        };

        /// <summary>
        /// Known compound color patterns (multi-token colors).
        /// These are checked as consecutive token pairs.
        /// </summary>
        private static readonly HashSet<string> CompoundColorTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Wine variants
            "wine_red", "winered", "wine red",
            // Sky variants
            "sky_blue", "skyblue", "sky blue",
            // Sea variants
            "sea_green", "seagreen", "sea green",
            // Forest variants
            "forest_green", "forestgreen", "forest green",
            // Royal variants
            "royal_blue", "royalblue", "royal blue",
            // Hot variants
            "hot_pink", "hotpink", "hot pink",
            // Ice variants
            "ice_blue", "iceblue", "ice blue",
            // Baby variants
            "baby_blue", "babyblue", "baby blue", "baby_pink", "babypink", "baby pink",
            // Light/Dark variants
            "light_blue", "lightblue", "light blue", "light_green", "lightgreen", "light green",
            "light_pink", "lightpink", "light pink", "light_gray", "lightgray", "light gray",
            "dark_blue", "darkblue", "dark blue", "dark_green", "darkgreen", "dark green",
            "dark_red", "darkred", "dark red", "dark_gray", "darkgray", "dark gray",
            // Other compound colors
            "dusty_rose", "dustyrose", "dusty rose",
            "olive_green", "olivegreen", "olive green",
            "navy_blue", "navyblue", "navy blue",
            "burnt_orange", "burntorange", "burnt orange",
            "pale_pink", "palepink", "pale pink",
            "deep_purple", "deeppurple", "deep purple"
        };

        /// <summary>
        /// Patterns for version/variant suffixes.
        /// </summary>
        private static readonly Regex[] VersionPatterns = new Regex[]
        {
            new Regex(@"^v(\d+)$", RegexOptions.IgnoreCase),           // v1, v2, V01
            new Regex(@"^ver(\d+)$", RegexOptions.IgnoreCase),         // ver1, Ver2
            new Regex(@"^(\d+)$"),                                      // 1, 2, 01, 02
            new Regex(@"^[a-f]$", RegexOptions.IgnoreCase),            // a, b, c, d, e, f (extended for variant letters)
            new Regex(@"^alt(\d*)$", RegexOptions.IgnoreCase),         // alt, alt1, alt2
            new Regex(@"^var(\d*)$", RegexOptions.IgnoreCase),         // var, var1, var2
            new Regex(@"^type(\d*)$", RegexOptions.IgnoreCase),        // type, type1
            new Regex(@"^style(\d*)$", RegexOptions.IgnoreCase),       // style, style1
        };

        /// <summary>
        /// Pattern to detect color+number combinations like "black1", "brown2", "red01".
        /// </summary>
        private static readonly Regex ColorNumberPattern = new Regex(
            @"^(red|blue|green|black|white|pink|purple|yellow|orange|brown|gray|grey|cyan|magenta|gold|silver|beige|navy|teal|violet|indigo|cream|tan|maroon|olive|coral|salmon|turquoise|lavender|mint|peach|rose|ruby|emerald|sapphire|aka|ao|midori|kuro|shiro|pinku|murasaki|kiiro|orenji|chairo|haiiro|gin|kin|momoiro|sora|sakura|rojo|azul|verde|negro|blanco|rosa|morado|amarillo|naranja|marron|gris|dorado|plateado|celeste)(\d+)$",
            RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Known style tokens.
        /// </summary>
        private static readonly HashSet<string> StyleTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "casual", "formal", "sport", "gothic", "cute", "dark", "light",
            "summer", "winter", "spring", "autumn", "fall",
            "school", "swimsuit", "pajama", "uniform", "dress",
            "normal", "special", "rare", "basic", "premium", "deluxe",
            "day", "night", "evening", "morning",
            "clean", "dirty", "worn", "new", "old",
            "wet", "dry", "glossy", "matte", "shiny",
            "on", "off", "open", "closed", "half"
        };

        /// <summary>
        /// Sub-part identifiers that should be treated as variant suffixes.
        /// These represent different parts of the same clothing item (e.g., metal hardware, buttons).
        /// IMPORTANT: Only include tokens that are CLEARLY sub-parts and would NOT be used as main item names.
        /// Avoid: "belt", "outer", "top", "bottom" - these can be main items!
        /// </summary>
        private static readonly HashSet<string> SubPartTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Japanese common sub-parts
            "kanagu",       // 金具 = metal hardware/fittings (very common in JP assets)
            "botan",        // ボタン = button

            // Metal/hardware parts (clearly sub-parts, not main items)
            "buckle",
            "clasp",
            "hook",
            "zipper",
            "snap",
            "rivet",
            "grommet",
            "eyelet",

            // Decorative elements
            "charm",
            "pendant",
            "jewel",
            "gem",
            "crystal",
            "rhinestone",
            "stud",
            "spike",
            "emblem",
            "badge",
            "pin",

            // Fabric details
            "lace",
            "ribbon",
            "bow",
            "ruffle",
            "frill",
            "trim",
            "piping",
            "binding",
            "applique",

            // Structural sub-parts
            "liner",
            "lining",
            "padding",
            "insert",
            "panel",

            // Fasteners
            "button",
            "buttons",
            "velcro",
            "drawstring",
            "toggle"
        };

        /// <summary>
        /// Common separators in material names.
        /// </summary>
        private static readonly char[] Separators = new char[] { '_', '-', '.', ' ' };

        #endregion

        #region Configuration

        /// <summary>
        /// Configuration for the detector.
        /// </summary>
        public class DetectorConfig
        {
            /// <summary>
            /// Weight for same folder match (0-1).
            /// </summary>
            public float SameFolderWeight { get; set; } = 0.35f;

            /// <summary>
            /// Weight for same name prefix (0-1).
            /// </summary>
            public float SamePrefixWeight { get; set; } = 0.30f;

            /// <summary>
            /// Weight for known variant suffix (color/version/style).
            /// </summary>
            public float KnownSuffixWeight { get; set; } = 0.20f;

            /// <summary>
            /// Weight for same shader.
            /// </summary>
            public float SameShaderWeight { get; set; } = 0.10f;

            /// <summary>
            /// Bonus for being in immediate parent folder vs deeper.
            /// </summary>
            public float ImmediateFolderBonus { get; set; } = 0.05f;

            /// <summary>
            /// Minimum confidence to include in suggestions.
            /// </summary>
            public float MinimumConfidence { get; set; } = 0.3f;

            /// <summary>
            /// Maximum number of suggestions per slot.
            /// </summary>
            public int MaxSuggestionsPerSlot { get; set; } = 20;

            /// <summary>
            /// Whether to search parent folders.
            /// </summary>
            public bool SearchParentFolders { get; set; } = true;

            /// <summary>
            /// Maximum depth to search in parent folders.
            /// </summary>
            public int MaxParentFolderDepth { get; set; } = 2;
        }

        private readonly DetectorConfig _config;

        #endregion

        #region Constructor

        public MaterialAlternativeDetector() : this(new DetectorConfig()) { }

        public MaterialAlternativeDetector(DetectorConfig config)
        {
            _config = config ?? new DetectorConfig();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Detects alternative materials for all slots in a component.
        /// </summary>
        public MaterialSuggestionResult DetectAlternatives(MRAgruparMateriales component)
        {
            if (component == null)
                return new MaterialSuggestionResult(new List<SlotSuggestionResult>());

            var slotResults = new List<SlotSuggestionResult>();

            foreach (var slot in component.Slots)
            {
                var result = DetectAlternativesForSlot(slot);
                slotResults.Add(result);
            }

            return new MaterialSuggestionResult(slotResults);
        }

        /// <summary>
        /// Detects alternative materials for a single slot.
        /// </summary>
        public SlotSuggestionResult DetectAlternativesForSlot(MRMaterialSlot slot)
        {
            if (slot == null || !slot.IsValid)
                return SlotSuggestionResult.Error(slot, "Invalid slot");

            var currentMaterial = slot.CurrentMaterial;
            if (currentMaterial == null)
                return SlotSuggestionResult.Error(slot, "Slot has no current material");

            var currentPath = AssetDatabase.GetAssetPath(currentMaterial);
            if (string.IsNullOrEmpty(currentPath))
                return SlotSuggestionResult.Error(slot, "Material is not a project asset");

            var suggestions = new List<MaterialSuggestion>();

            // Get candidate materials from folder analysis (Proposal 1)
            var candidates = GetCandidateMaterials(currentPath);

            // Analyze each candidate
            foreach (var candidatePath in candidates)
            {
                // Skip the current material itself
                if (candidatePath == currentPath)
                    continue;

                var candidate = AssetDatabase.LoadAssetAtPath<Material>(candidatePath);
                if (candidate == null)
                    continue;

                // Calculate confidence score (Proposal 2 + combined scoring)
                var (confidence, reasons) = CalculateConfidence(currentMaterial, currentPath, candidate, candidatePath);

                if (confidence >= _config.MinimumConfidence)
                {
                    suggestions.Add(new MaterialSuggestion(candidate, confidence, reasons, candidatePath));
                }
            }

            // Limit suggestions
            if (suggestions.Count > _config.MaxSuggestionsPerSlot)
            {
                suggestions.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
                suggestions = suggestions.Take(_config.MaxSuggestionsPerSlot).ToList();
            }

            return new SlotSuggestionResult(slot, currentMaterial, suggestions);
        }

        #endregion

        #region Folder Analysis (Proposal 1)

        /// <summary>
        /// Gets candidate materials from the same folder and optionally parent folders.
        /// Also implements color folder pattern detection (Proposal 5).
        /// </summary>
        private List<string> GetCandidateMaterials(string currentMaterialPath)
        {
            var candidates = new HashSet<string>();
            var currentFolder = Path.GetDirectoryName(currentMaterialPath);

            if (string.IsNullOrEmpty(currentFolder))
                return candidates.ToList();

            // Normalize path separators
            currentFolder = currentFolder.Replace("\\", "/");

            // Search in current folder
            AddMaterialsFromFolder(currentFolder, candidates);

            // Search in parent and sibling folders if enabled
            if (_config.SearchParentFolders)
            {
                var parentFolder = Path.GetDirectoryName(currentFolder)?.Replace("\\", "/");

                if (!string.IsNullOrEmpty(parentFolder) && parentFolder != "Assets")
                {
                    // FIRST: Search all sibling folders (most likely to contain variants)
                    var siblingFolders = AssetDatabase.GetSubFolders(parentFolder);
                    foreach (var sibling in siblingFolders)
                    {
                        if (sibling != currentFolder)
                        {
                            AddMaterialsFromFolder(sibling, candidates);
                        }
                    }

                    // THEN: Standard parent folder search for additional depth
                    for (int depth = 0; depth < _config.MaxParentFolderDepth; depth++)
                    {
                        if (string.IsNullOrEmpty(parentFolder) || parentFolder == "Assets")
                            break;

                        // Search in parent folder itself
                        AddMaterialsFromFolder(parentFolder, candidates);

                        // Move up one level
                        parentFolder = Path.GetDirectoryName(parentFolder)?.Replace("\\", "/");

                        if (!string.IsNullOrEmpty(parentFolder) && parentFolder != "Assets")
                        {
                            // Search in uncle folders (siblings of parent)
                            var uncleFolders = AssetDatabase.GetSubFolders(parentFolder);
                            foreach (var uncle in uncleFolders)
                            {
                                AddMaterialsFromFolder(uncle, candidates);
                            }
                        }
                    }
                }
            }

            return candidates.ToList();
        }

        /// <summary>
        /// Checks if a folder name suggests it's a variant folder (color, number, style, etc.).
        /// </summary>
        private bool IsColorOrStyleFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return false;

            string lowerName = folderName.ToLowerInvariant();

            // Check against color tokens
            if (ColorTokens.Contains(lowerName))
                return true;

            // Check against style tokens
            if (StyleTokens.Contains(lowerName))
                return true;

            // Check if it's a number or version pattern
            foreach (var pattern in VersionPatterns)
            {
                if (pattern.IsMatch(folderName))
                    return true;
            }

            // Check if it's a pure number (01, 02, 1, 2, etc.)
            if (System.Text.RegularExpressions.Regex.IsMatch(folderName, @"^\d+$"))
                return true;

            // Check common variant prefixes/patterns
            string[] variantPatterns = {
                "ver", "version", "v", "type", "style", "color", "colour",
                "variant", "var", "opt", "option", "alt", "set"
            };

            foreach (var prefix in variantPatterns)
            {
                if (lowerName.StartsWith(prefix))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds all materials from a folder to the candidates set.
        /// </summary>
        private void AddMaterialsFromFolder(string folderPath, HashSet<string> candidates)
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Only add if in this exact folder (not subfolders)
                var materialFolder = Path.GetDirectoryName(path)?.Replace("\\", "/");
                if (materialFolder == folderPath)
                {
                    candidates.Add(path);
                }
            }
        }

        #endregion

        #region Name Pattern Analysis (Proposal 2)

        /// <summary>
        /// Calculates confidence score and reasons for a candidate material.
        /// </summary>
        private (float confidence, List<string> reasons) CalculateConfidence(
            Material currentMaterial, string currentPath,
            Material candidateMaterial, string candidatePath)
        {
            float score = 0f;
            var reasons = new List<string>();

            var currentFolder = Path.GetDirectoryName(currentPath)?.Replace("\\", "/");
            var candidateFolder = Path.GetDirectoryName(candidatePath)?.Replace("\\", "/");
            var currentName = Path.GetFileNameWithoutExtension(currentPath);
            var candidateName = Path.GetFileNameWithoutExtension(candidatePath);

            // 1. Same folder check (Proposal 1)
            bool isSameFolder = currentFolder == candidateFolder;
            if (isSameFolder)
            {
                score += _config.SameFolderWeight;
                reasons.Add("Misma carpeta");
            }
            else if (IsImmediateParentOrSibling(currentFolder, candidateFolder))
            {
                score += _config.SameFolderWeight * 0.7f;
                reasons.Add("Carpeta cercana");
            }

            // 1.1 Sibling folder pattern detection (Proposal 5 - generalized)
            // Check if materials are in sibling folders (same parent) with same base name
            string currentFolderName = Path.GetFileName(currentFolder);
            string candidateFolderName = Path.GetFileName(candidateFolder);

            // Check if they are siblings (same parent folder)
            string currentParent = Path.GetDirectoryName(currentFolder)?.Replace("\\", "/");
            string candidateParent = Path.GetDirectoryName(candidateFolder)?.Replace("\\", "/");
            bool areSiblingFolders = !string.IsNullOrEmpty(currentParent) &&
                                     !string.IsNullOrEmpty(candidateParent) &&
                                     currentParent == candidateParent &&
                                     currentFolder != candidateFolder;

            // Check if folder names suggest variants (colors, numbers, styles, etc.)
            bool currentIsVariantFolder = IsColorOrStyleFolderName(currentFolderName);
            bool candidateIsVariantFolder = IsColorOrStyleFolderName(candidateFolderName);

            if (areSiblingFolders)
            {
                // Sibling folders with same parent - this is a variant pattern
                score += 0.25f;
                reasons.Add($"Carpetas hermanas ({currentFolderName}/{candidateFolderName})");

                // Extra bonus if folder names are recognized variant names (colors, numbers, etc.)
                if (currentIsVariantFolder && candidateIsVariantFolder)
                {
                    score += 0.10f;
                    reasons.Add("Nombres de variante reconocidos");
                }
            }

            // 2. Name prefix analysis (Proposal 2)
            var currentTokens = TokenizeName(currentName);
            var candidateTokens = TokenizeName(candidateName);

            // 2.1 Check if they share the same base name (without color/version suffix)
            string currentBaseName = ExtractBaseName(currentName);
            string candidateBaseName = ExtractBaseName(candidateName);

            bool sameBaseName = !string.IsNullOrEmpty(currentBaseName) &&
                               !string.IsNullOrEmpty(candidateBaseName) &&
                               currentBaseName.Equals(candidateBaseName, StringComparison.OrdinalIgnoreCase);

            if (sameBaseName)
            {
                // Same base name = very likely variants
                score += 0.40f;  // Strong bonus
                reasons.Add("Mismo nombre base");

                // Extra bonus if in same folder or sibling folders
                if (isSameFolder)
                {
                    score += 0.15f;
                }
                else if (areSiblingFolders)
                {
                    // Sibling folders with same base name = very strong pattern
                    score += 0.20f;
                    reasons.Add("Patron de variantes en carpetas");
                }
            }
            else
            {
                // Different base names - this is likely NOT a variant
                // Materials with different base names are almost certainly different mesh materials
                if (isSameFolder)
                {
                    // Same folder with different base names = very unlikely to be variants
                    // These are typically different parts of the same outfit (body, cup, etc)
                    score -= 0.60f;  // Very strong penalty
                }
                else if (areSiblingFolders)
                {
                    score -= 0.50f;  // Strong penalty for sibling folders with different base names
                }
                else
                {
                    score -= 0.40f;  // Moderate penalty otherwise
                }
                // Don't add a reason for penalty, it will just result in low confidence
            }

            var (prefixMatch, prefixLength) = CalculatePrefixMatch(currentTokens, candidateTokens);

            if (prefixMatch > 0)
            {
                score += _config.SamePrefixWeight * prefixMatch;
                if (prefixLength > 0)
                {
                    reasons.Add($"Prefijo compartido ({prefixLength} tokens)");
                }
            }

            // 3. Variant suffix analysis (Proposal 2)
            var currentSuffixType = ClassifyLastToken(currentTokens);
            var candidateSuffixType = ClassifyLastToken(candidateTokens);

            if (candidateSuffixType != TokenType.Unknown)
            {
                score += _config.KnownSuffixWeight;
                string suffixReason = candidateSuffixType switch
                {
                    TokenType.Color => "Variante de color",
                    TokenType.Version => "Variante de version",
                    TokenType.Style => "Variante de estilo",
                    TokenType.SubPart => "Variante de sub-parte",
                    _ => "Variante conocida"
                };
                reasons.Add(suffixReason);
            }

            // Bonus if both have same type of suffix (e.g., both are colors)
            if (currentSuffixType != TokenType.Unknown &&
                currentSuffixType == candidateSuffixType)
            {
                score += 0.05f;
                reasons.Add("Mismo tipo de variante");
            }

            // 4. Same shader check
            if (currentMaterial.shader == candidateMaterial.shader)
            {
                score += _config.SameShaderWeight;
                reasons.Add("Mismo shader");
            }

            // 5. Immediate folder bonus
            if (currentFolder == candidateFolder)
            {
                score += _config.ImmediateFolderBonus;
            }

            return (Mathf.Clamp01(score), reasons);
        }

        /// <summary>
        /// Checks if candidateFolder is immediate parent or sibling of currentFolder.
        /// </summary>
        private bool IsImmediateParentOrSibling(string currentFolder, string candidateFolder)
        {
            if (string.IsNullOrEmpty(currentFolder) || string.IsNullOrEmpty(candidateFolder))
                return false;

            var currentParent = Path.GetDirectoryName(currentFolder)?.Replace("\\", "/");
            var candidateParent = Path.GetDirectoryName(candidateFolder)?.Replace("\\", "/");

            // Same parent = siblings
            if (currentParent == candidateParent)
                return true;

            // Candidate is parent of current
            if (candidateFolder == currentParent)
                return true;

            // Current is parent of candidate
            if (currentFolder == candidateParent)
                return true;

            return false;
        }

        /// <summary>
        /// Tokenizes a material name into parts.
        /// </summary>
        private List<string> TokenizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new List<string>();

            // Split by common separators
            var tokens = name.Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToList();

            // If no separators found, try to split by case changes (camelCase/PascalCase)
            if (tokens.Count <= 1 && !string.IsNullOrEmpty(name))
            {
                tokens = SplitByCaseChange(name);
            }

            return tokens;
        }

        /// <summary>
        /// Splits a string by case changes (camelCase, PascalCase).
        /// Also handles digit-to-uppercase transitions (e.g., "mt1A" -> ["mt1", "A"]).
        /// </summary>
        private List<string> SplitByCaseChange(string input)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(input))
                return result;

            var currentToken = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Start of uppercase sequence or single uppercase letter
                if (char.IsUpper(c) && currentToken.Length > 0)
                {
                    // Check if this is start of new word or part of acronym
                    bool isAcronymContinuation = i + 1 < input.Length && char.IsUpper(input[i + 1]);
                    bool previousWasLower = i > 0 && char.IsLower(input[i - 1]);
                    bool previousWasDigit = i > 0 && char.IsDigit(input[i - 1]);

                    if (previousWasLower || previousWasDigit || (!isAcronymContinuation && currentToken.Length > 0))
                    {
                        result.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }

                currentToken.Append(c);
            }

            if (currentToken.Length > 0)
            {
                result.Add(currentToken.ToString());
            }

            return result;
        }

        /// <summary>
        /// Calculates how well the prefixes match between two token lists.
        /// Returns (matchScore 0-1, matchingTokenCount).
        /// </summary>
        private (float matchScore, int matchingTokens) CalculatePrefixMatch(List<string> tokens1, List<string> tokens2)
        {
            if (tokens1.Count == 0 || tokens2.Count == 0)
                return (0f, 0);

            int matchingTokens = 0;
            int minLength = Math.Min(tokens1.Count, tokens2.Count);

            for (int i = 0; i < minLength; i++)
            {
                if (string.Equals(tokens1[i], tokens2[i], StringComparison.OrdinalIgnoreCase))
                {
                    matchingTokens++;
                }
                else
                {
                    break; // Stop at first non-matching token
                }
            }

            if (matchingTokens == 0)
            {
                // No prefix match, but check for partial match with different structure
                // e.g., "ShirtRed" vs "Shirt_Red"
                var joined1 = string.Join("", tokens1).ToLowerInvariant();
                var joined2 = string.Join("", tokens2).ToLowerInvariant();

                if (joined1.StartsWith(joined2) || joined2.StartsWith(joined1))
                {
                    return (0.5f, 1);
                }

                return (0f, 0);
            }

            // Calculate score based on proportion of matching tokens
            float maxTokens = Math.Max(tokens1.Count, tokens2.Count);
            float score = matchingTokens / maxTokens;

            // Bonus if all non-variant tokens match
            if (matchingTokens >= minLength - 1)
            {
                score = Math.Min(1f, score + 0.2f);
            }

            return (score, matchingTokens);
        }

        /// <summary>
        /// Classifies the last token(s) of a name.
        /// </summary>
        private TokenType ClassifyLastToken(List<string> tokens)
        {
            if (tokens.Count == 0)
                return TokenType.Unknown;

            var lastToken = tokens[tokens.Count - 1];

            // Skip common material suffixes that look like color codes
            // "M" commonly means "Material" or "Mat", not "Magenta"
            if (IsCommonMaterialSuffix(lastToken))
                return TokenType.Unknown;

            // Check for compound color (two consecutive tokens like "Wine_Red")
            if (tokens.Count >= 2)
            {
                var secondLastToken = tokens[tokens.Count - 2];
                var compoundCandidate = $"{secondLastToken}_{lastToken}";

                if (CompoundColorTokens.Contains(compoundCandidate) ||
                    CompoundColorTokens.Contains(compoundCandidate.Replace("_", "")))
                {
                    return TokenType.Color;
                }
            }

            // Check if it's a color
            if (ColorTokens.Contains(lastToken))
                return TokenType.Color;

            // Check if it's a color+number pattern (e.g., "black1", "brown2")
            if (ColorNumberPattern.IsMatch(lastToken))
                return TokenType.Color;

            // Check if it's a version pattern
            foreach (var pattern in VersionPatterns)
            {
                if (pattern.IsMatch(lastToken))
                    return TokenType.Version;
            }

            // Check if it's a style
            if (StyleTokens.Contains(lastToken))
                return TokenType.Style;

            // Check if it's a sub-part identifier
            if (SubPartTokens.Contains(lastToken))
                return TokenType.SubPart;

            return TokenType.Unknown;
        }


        private enum TokenType
        {
            Unknown,
            Color,
            Version,
            Style,
            SubPart
        }

        #endregion

        #region Static Utilities

        /// <summary>
        /// Quick check if a material name contains known variant tokens.
        /// </summary>
        public static bool ContainsKnownVariantToken(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return false;

            var tokens = materialName.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            // Check for compound colors (consecutive token pairs)
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                var compoundCandidate = $"{tokens[i]}_{tokens[i + 1]}";
                if (CompoundColorTokens.Contains(compoundCandidate) ||
                    CompoundColorTokens.Contains(compoundCandidate.Replace("_", "")))
                {
                    return true;
                }
            }

            foreach (var token in tokens)
            {
                // Skip common material suffixes
                if (IsCommonMaterialSuffix(token))
                    continue;

                if (ColorTokens.Contains(token))
                    return true;

                if (StyleTokens.Contains(token))
                    return true;

                if (SubPartTokens.Contains(token))
                    return true;

                // Check color+number patterns (e.g., "black1", "brown2")
                if (ColorNumberPattern.IsMatch(token))
                    return true;

                foreach (var pattern in VersionPatterns)
                {
                    if (pattern.IsMatch(token))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts the likely "base name" of a material (without variant suffix).
        /// </summary>
        public static string ExtractBaseName(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return materialName;

            var tokens = materialName.Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToList();

            // If no separators found, try to split by case changes (e.g., "mt1A" -> ["mt1", "A"])
            if (tokens.Count <= 1 && !string.IsNullOrEmpty(materialName))
            {
                tokens = SplitByCaseChangeStatic(materialName);
            }

            if (tokens.Count <= 1)
                return materialName;

            // Remove trailing variant tokens
            while (tokens.Count > 1)
            {
                var lastToken = tokens[tokens.Count - 1];

                // Skip common material suffixes - they're not variants
                if (IsCommonMaterialSuffix(lastToken))
                {
                    break;
                }

                // Check for compound color (two consecutive tokens like "Wine_Red")
                if (tokens.Count >= 2)
                {
                    var secondLastToken = tokens[tokens.Count - 2];
                    var compoundCandidate = $"{secondLastToken}_{lastToken}";

                    if (CompoundColorTokens.Contains(compoundCandidate) ||
                        CompoundColorTokens.Contains(compoundCandidate.Replace("_", "")))
                    {
                        // Remove both tokens of the compound color
                        tokens.RemoveAt(tokens.Count - 1);
                        tokens.RemoveAt(tokens.Count - 1);
                        continue;
                    }
                }

                bool isVariant = ColorTokens.Contains(lastToken) ||
                                StyleTokens.Contains(lastToken) ||
                                SubPartTokens.Contains(lastToken) ||  // Handle sub-parts like "kanagu", "metal", etc.
                                VersionPatterns.Any(p => p.IsMatch(lastToken)) ||
                                ColorNumberPattern.IsMatch(lastToken);  // Handle "black1", "brown2", etc.

                if (isVariant)
                {
                    tokens.RemoveAt(tokens.Count - 1);
                }
                else
                {
                    break;
                }
            }

            return string.Join("_", tokens);
        }

        /// <summary>
        /// Common suffixes for material files that should not be treated as variants.
        /// </summary>
        private static readonly HashSet<string> CommonMaterialSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "M", "Mat", "Material", "Mtl", "Tex", "Texture"
        };

        /// <summary>
        /// Checks if a token is a common material suffix (not a color/variant).
        /// Static version for ExtractBaseName.
        /// </summary>
        private static bool IsCommonMaterialSuffix(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            return CommonMaterialSuffixes.Contains(token);
        }

        /// <summary>
        /// Splits a string by case changes (camelCase, PascalCase).
        /// Static version for ExtractBaseName.
        /// Handles cases like "mt1A" -> ["mt1", "A"], "BodydsE" -> ["Bodyds", "E"]
        /// </summary>
        private static List<string> SplitByCaseChangeStatic(string input)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(input))
                return result;

            var currentToken = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Start of uppercase sequence or single uppercase letter
                if (char.IsUpper(c) && currentToken.Length > 0)
                {
                    // Check if this is start of new word or part of acronym
                    bool isAcronymContinuation = i + 1 < input.Length && char.IsUpper(input[i + 1]);
                    bool previousWasLower = i > 0 && char.IsLower(input[i - 1]);

                    // Also split when previous was a digit (e.g., "mt1A" -> ["mt1", "A"])
                    bool previousWasDigit = i > 0 && char.IsDigit(input[i - 1]);

                    if (previousWasLower || previousWasDigit || (!isAcronymContinuation && currentToken.Length > 0))
                    {
                        result.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }

                currentToken.Append(c);
            }

            if (currentToken.Length > 0)
            {
                result.Add(currentToken.ToString());
            }

            return result;
        }

        #endregion
    }
}
