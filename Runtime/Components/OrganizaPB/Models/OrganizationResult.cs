using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.OrganizaPB.Models
{
    /// <summary>
    /// Resultado de la operación de reorganización de PhysBones.
    /// </summary>
    [Serializable]
    public class OrganizationResult
    {
        [SerializeField] private bool _success;
        [SerializeField] private int _physBonesRelocated;
        [SerializeField] private int _collidersRelocated;
        [SerializeField] private int _physBonesSkipped;
        [SerializeField] private int _collidersSkipped;
        [SerializeField] private List<string> _warnings = new List<string>();
        [SerializeField] private List<string> _errors = new List<string>();

        public bool Success
        {
            get => _success;
            set => _success = value;
        }

        public int PhysBonesRelocated
        {
            get => _physBonesRelocated;
            set => _physBonesRelocated = value;
        }

        public int CollidersRelocated
        {
            get => _collidersRelocated;
            set => _collidersRelocated = value;
        }

        public int PhysBonesSkipped
        {
            get => _physBonesSkipped;
            set => _physBonesSkipped = value;
        }

        public int CollidersSkipped
        {
            get => _collidersSkipped;
            set => _collidersSkipped = value;
        }

        public List<string> Warnings => _warnings;
        public List<string> Errors => _errors;

        public bool HasWarnings => _warnings.Count > 0;
        public bool HasErrors => _errors.Count > 0;

        public int TotalRelocated => _physBonesRelocated + _collidersRelocated;
        public int TotalSkipped => _physBonesSkipped + _collidersSkipped;

        public OrganizationResult()
        {
            _warnings = new List<string>();
            _errors = new List<string>();
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        public void AddError(string error)
        {
            _errors.Add(error);
            _success = false;
        }

        public static OrganizationResult CreateSuccess(int physBonesRelocated, int collidersRelocated)
        {
            return new OrganizationResult
            {
                _success = true,
                _physBonesRelocated = physBonesRelocated,
                _collidersRelocated = collidersRelocated
            };
        }

        public static OrganizationResult CreateFailure(string error)
        {
            var result = new OrganizationResult
            {
                _success = false
            };
            result.AddError(error);
            return result;
        }

        public static OrganizationResult CreateEmpty()
        {
            return new OrganizationResult
            {
                _success = true,
                _physBonesRelocated = 0,
                _collidersRelocated = 0
            };
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();

            if (_success)
            {
                sb.Append($"Reorganización exitosa: {_physBonesRelocated} PhysBones, {_collidersRelocated} Colliders reubicados");
            }
            else
            {
                sb.Append("Reorganización fallida");
            }

            if (_physBonesSkipped > 0 || _collidersSkipped > 0)
            {
                sb.Append($" ({_physBonesSkipped} PB, {_collidersSkipped} Col omitidos)");
            }

            if (_warnings.Count > 0)
            {
                sb.Append($" [{_warnings.Count} advertencia(s)]");
            }

            if (_errors.Count > 0)
            {
                sb.Append($" [{_errors.Count} error(es)]");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return GetSummary();
        }
    }
}
