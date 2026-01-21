using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AlternativeMaterial
{
    /// <summary>
    /// Represents a suggested material alternative with confidence score and reasoning.
    /// </summary>
    [Serializable]
    public class MaterialSuggestion
    {
        [SerializeField] private Material _material;
        [SerializeField] private float _confidence;
        [SerializeField] private List<string> _reasons;
        [SerializeField] private string _assetPath;

        /// <summary>
        /// The suggested material.
        /// </summary>
        public Material Material => _material;

        /// <summary>
        /// Confidence score from 0.0 to 1.0.
        /// </summary>
        public float Confidence => _confidence;

        /// <summary>
        /// List of reasons why this material was suggested.
        /// </summary>
        public IReadOnlyList<string> Reasons => _reasons;

        /// <summary>
        /// Asset path of the material.
        /// </summary>
        public string AssetPath => _assetPath;

        /// <summary>
        /// Confidence level category.
        /// </summary>
        public ConfidenceLevel ConfidenceLevel
        {
            get
            {
                if (_confidence >= 0.8f) return ConfidenceLevel.High;
                if (_confidence >= 0.5f) return ConfidenceLevel.Medium;
                return ConfidenceLevel.Low;
            }
        }

        /// <summary>
        /// Whether this is a high confidence suggestion (>= 80%).
        /// </summary>
        public bool IsHighConfidence => _confidence >= 0.8f;

        /// <summary>
        /// Whether this is a medium confidence suggestion (50-79%).
        /// </summary>
        public bool IsMediumConfidence => _confidence >= 0.5f && _confidence < 0.8f;

        public MaterialSuggestion(Material material, float confidence, List<string> reasons, string assetPath)
        {
            _material = material;
            _confidence = Mathf.Clamp01(confidence);
            _reasons = reasons ?? new List<string>();
            _assetPath = assetPath ?? string.Empty;
        }

        /// <summary>
        /// Gets a formatted confidence percentage string.
        /// </summary>
        public string GetConfidencePercentage()
        {
            return $"{Mathf.RoundToInt(_confidence * 100)}%";
        }

        /// <summary>
        /// Gets all reasons as a single formatted string.
        /// </summary>
        public string GetReasonsText()
        {
            return string.Join(", ", _reasons);
        }
    }

    /// <summary>
    /// Confidence level categories for suggestions.
    /// </summary>
    public enum ConfidenceLevel
    {
        Low,    // < 50%
        Medium, // 50-79%
        High    // >= 80%
    }

    /// <summary>
    /// Result of material suggestion detection for a single slot.
    /// </summary>
    [Serializable]
    public class SlotSuggestionResult
    {
        [SerializeField] private MRMaterialSlot _slot;
        [SerializeField] private Material _currentMaterial;
        [SerializeField] private List<MaterialSuggestion> _suggestions;
        [SerializeField] private bool _hasError;
        [SerializeField] private string _errorMessage;

        /// <summary>
        /// The slot this result is for.
        /// </summary>
        public MRMaterialSlot Slot => _slot;

        /// <summary>
        /// The current material of the slot.
        /// </summary>
        public Material CurrentMaterial => _currentMaterial;

        /// <summary>
        /// List of suggested alternative materials, ordered by confidence (highest first).
        /// </summary>
        public IReadOnlyList<MaterialSuggestion> Suggestions => _suggestions;

        /// <summary>
        /// Whether detection encountered an error.
        /// </summary>
        public bool HasError => _hasError;

        /// <summary>
        /// Error message if HasError is true.
        /// </summary>
        public string ErrorMessage => _errorMessage;

        /// <summary>
        /// Whether any suggestions were found.
        /// </summary>
        public bool HasSuggestions => _suggestions != null && _suggestions.Count > 0;

        /// <summary>
        /// Number of suggestions found.
        /// </summary>
        public int SuggestionCount => _suggestions?.Count ?? 0;

        /// <summary>
        /// Number of high confidence suggestions.
        /// </summary>
        public int HighConfidenceCount
        {
            get
            {
                int count = 0;
                if (_suggestions != null)
                {
                    foreach (var s in _suggestions)
                    {
                        if (s.IsHighConfidence) count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Creates a successful result with suggestions.
        /// </summary>
        public SlotSuggestionResult(MRMaterialSlot slot, Material currentMaterial, List<MaterialSuggestion> suggestions)
        {
            _slot = slot;
            _currentMaterial = currentMaterial;
            _suggestions = suggestions ?? new List<MaterialSuggestion>();
            _hasError = false;
            _errorMessage = string.Empty;

            // Sort by confidence descending
            _suggestions.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
        }

        /// <summary>
        /// Creates an error result.
        /// </summary>
        public static SlotSuggestionResult Error(MRMaterialSlot slot, string errorMessage)
        {
            return new SlotSuggestionResult(slot, null, null)
            {
                _hasError = true,
                _errorMessage = errorMessage
            };
        }

        /// <summary>
        /// Gets suggestions filtered by minimum confidence.
        /// </summary>
        public List<MaterialSuggestion> GetSuggestionsWithMinConfidence(float minConfidence)
        {
            var result = new List<MaterialSuggestion>();
            if (_suggestions != null)
            {
                foreach (var s in _suggestions)
                {
                    if (s.Confidence >= minConfidence)
                        result.Add(s);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets only high confidence suggestions (>= 80%).
        /// </summary>
        public List<MaterialSuggestion> GetHighConfidenceSuggestions()
        {
            return GetSuggestionsWithMinConfidence(0.8f);
        }
    }

    /// <summary>
    /// Complete result of material suggestion detection for all slots.
    /// </summary>
    [Serializable]
    public class MaterialSuggestionResult
    {
        [SerializeField] private List<SlotSuggestionResult> _slotResults;
        [SerializeField] private int _totalSuggestionsFound;
        [SerializeField] private int _slotsWithSuggestions;
        [SerializeField] private int _slotsWithoutSuggestions;

        /// <summary>
        /// Results for each slot.
        /// </summary>
        public IReadOnlyList<SlotSuggestionResult> SlotResults => _slotResults;

        /// <summary>
        /// Total number of suggestions found across all slots.
        /// </summary>
        public int TotalSuggestionsFound => _totalSuggestionsFound;

        /// <summary>
        /// Number of slots that have at least one suggestion.
        /// </summary>
        public int SlotsWithSuggestions => _slotsWithSuggestions;

        /// <summary>
        /// Number of slots without any suggestions.
        /// </summary>
        public int SlotsWithoutSuggestions => _slotsWithoutSuggestions;

        /// <summary>
        /// Whether any suggestions were found for any slot.
        /// </summary>
        public bool HasAnySuggestions => _totalSuggestionsFound > 0;

        public MaterialSuggestionResult(List<SlotSuggestionResult> slotResults)
        {
            _slotResults = slotResults ?? new List<SlotSuggestionResult>();

            _totalSuggestionsFound = 0;
            _slotsWithSuggestions = 0;
            _slotsWithoutSuggestions = 0;

            foreach (var result in _slotResults)
            {
                if (result.HasSuggestions)
                {
                    _slotsWithSuggestions++;
                    _totalSuggestionsFound += result.SuggestionCount;
                }
                else
                {
                    _slotsWithoutSuggestions++;
                }
            }
        }

        /// <summary>
        /// Gets only slot results that have suggestions.
        /// </summary>
        public List<SlotSuggestionResult> GetSlotsWithSuggestions()
        {
            var result = new List<SlotSuggestionResult>();
            foreach (var slotResult in _slotResults)
            {
                if (slotResult.HasSuggestions)
                    result.Add(slotResult);
            }
            return result;
        }
    }
}
