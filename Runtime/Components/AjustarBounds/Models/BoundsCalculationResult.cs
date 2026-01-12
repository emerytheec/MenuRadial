using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AjustarBounds.Models
{
    /// <summary>
    /// Resultado del calculo de bounds unificados.
    /// Contiene el bounding box final y estadisticas del proceso.
    /// </summary>
    [Serializable]
    public class BoundsCalculationResult
    {
        [SerializeField]
        private bool _success;

        [SerializeField]
        private Bounds _unifiedBounds;

        [SerializeField]
        private Bounds _unifiedBoundsWithMargin;

        [SerializeField]
        private int _meshCount;

        [SerializeField]
        private int _validMeshCount;

        [SerializeField]
        private float _marginPercentage;

        [SerializeField]
        private List<string> _errors = new List<string>();

        [SerializeField]
        private List<string> _warnings = new List<string>();

        /// <summary>
        /// Indica si el calculo fue exitoso
        /// </summary>
        public bool Success
        {
            get => _success;
            set => _success = value;
        }

        /// <summary>
        /// Bounds unificados sin margen (calculo exacto)
        /// </summary>
        public Bounds UnifiedBounds
        {
            get => _unifiedBounds;
            set => _unifiedBounds = value;
        }

        /// <summary>
        /// Bounds unificados con margen de seguridad aplicado
        /// </summary>
        public Bounds UnifiedBoundsWithMargin
        {
            get => _unifiedBoundsWithMargin;
            set => _unifiedBoundsWithMargin = value;
        }

        /// <summary>
        /// Total de meshes encontrados
        /// </summary>
        public int MeshCount
        {
            get => _meshCount;
            set => _meshCount = value;
        }

        /// <summary>
        /// Meshes validos procesados
        /// </summary>
        public int ValidMeshCount
        {
            get => _validMeshCount;
            set => _validMeshCount = value;
        }

        /// <summary>
        /// Porcentaje de margen aplicado
        /// </summary>
        public float MarginPercentage
        {
            get => _marginPercentage;
            set => _marginPercentage = value;
        }

        /// <summary>
        /// Lista de errores durante el calculo
        /// </summary>
        public List<string> Errors => _errors;

        /// <summary>
        /// Lista de advertencias durante el calculo
        /// </summary>
        public List<string> Warnings => _warnings;

        /// <summary>
        /// Tamanio del bounding box final (con margen)
        /// </summary>
        public Vector3 FinalSize => _unifiedBoundsWithMargin.size;

        /// <summary>
        /// Centro del bounding box final
        /// </summary>
        public Vector3 FinalCenter => _unifiedBoundsWithMargin.center;

        /// <summary>
        /// Altura total del avatar (eje Y)
        /// </summary>
        public float TotalHeight => _unifiedBoundsWithMargin.size.y;

        /// <summary>
        /// Punto mas bajo del bounding box
        /// </summary>
        public float MinY => _unifiedBoundsWithMargin.min.y;

        /// <summary>
        /// Punto mas alto del bounding box
        /// </summary>
        public float MaxY => _unifiedBoundsWithMargin.max.y;

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public BoundsCalculationResult()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        /// <summary>
        /// Agrega un error al resultado
        /// </summary>
        public void AddError(string error)
        {
            _errors ??= new List<string>();
            _errors.Add(error);
            _success = false;
        }

        /// <summary>
        /// Agrega una advertencia al resultado
        /// </summary>
        public void AddWarning(string warning)
        {
            _warnings ??= new List<string>();
            _warnings.Add(warning);
        }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static BoundsCalculationResult CreateSuccess(Bounds unified, Bounds withMargin, int meshCount, int validCount, float margin)
        {
            return new BoundsCalculationResult
            {
                _success = true,
                _unifiedBounds = unified,
                _unifiedBoundsWithMargin = withMargin,
                _meshCount = meshCount,
                _validMeshCount = validCount,
                _marginPercentage = margin
            };
        }

        /// <summary>
        /// Crea un resultado de fallo
        /// </summary>
        public static BoundsCalculationResult CreateFailure(string errorMessage)
        {
            var result = new BoundsCalculationResult
            {
                _success = false
            };
            result.AddError(errorMessage);
            return result;
        }

        /// <summary>
        /// Obtiene un resumen del resultado
        /// </summary>
        public string GetSummary()
        {
            if (!_success)
            {
                return $"Fallo: {string.Join(", ", _errors)}";
            }

            return $"Exito: {_validMeshCount}/{_meshCount} meshes, " +
                   $"Tamanio: {FinalSize.x:F2}x{FinalSize.y:F2}x{FinalSize.z:F2}m, " +
                   $"Centro Y: {FinalCenter.y:F2}m";
        }
    }
}
