using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AnalisisColision.Models
{
    /// <summary>
    /// Resultado del escaneo de componentes de Modular Avatar que pueden colisionar con Menu Radial.
    /// </summary>
    [Serializable]
    public class ColisionScanResult
    {
        #region Serialized Fields

        [SerializeField]
        [Tooltip("Lista de componentes problematicos detectados")]
        private List<ColisionEntry> _problematicEntries = new List<ColisionEntry>();

        [SerializeField]
        [Tooltip("Lista de componentes que requieren decision del usuario")]
        private List<ColisionEntry> _userDecisionEntries = new List<ColisionEntry>();

        [SerializeField]
        [Tooltip("Lista de componentes compatibles (informativos)")]
        private List<ColisionEntry> _compatibleEntries = new List<ColisionEntry>();

        [SerializeField]
        [Tooltip("Lista de meshes detectados en raíces de ropa (error del autor)")]
        private List<MeshOnRootEntry> _meshOnRootEntries = new List<MeshOnRootEntry>();

        [SerializeField]
        [Tooltip("Si el escaneo se completo exitosamente")]
        private bool _scanCompleted;

        [SerializeField]
        [Tooltip("Fecha y hora del ultimo escaneo")]
        private string _lastScanTime;

        [SerializeField]
        [Tooltip("Si Modular Avatar esta instalado")]
        private bool _maAvailable;

        #endregion

        #region Properties

        /// <summary>
        /// Lista de componentes problematicos (desactivar automaticamente si estan en raiz de ropa).
        /// </summary>
        public List<ColisionEntry> ProblematicEntries => _problematicEntries;

        /// <summary>
        /// Lista de componentes que requieren decision del usuario.
        /// </summary>
        public List<ColisionEntry> UserDecisionEntries => _userDecisionEntries;

        /// <summary>
        /// Lista de componentes compatibles (solo informativos).
        /// </summary>
        public List<ColisionEntry> CompatibleEntries => _compatibleEntries;

        /// <summary>
        /// Lista de meshes detectados directamente en raíces de ropa.
        /// Esto indica un error del autor de la ropa que puede causar
        /// que el sistema de menú confunda estos meshes con meshes del avatar base.
        /// </summary>
        public List<MeshOnRootEntry> MeshOnRootEntries => _meshOnRootEntries;

        /// <summary>
        /// Si el escaneo se completo exitosamente.
        /// </summary>
        public bool ScanCompleted
        {
            get => _scanCompleted;
            set => _scanCompleted = value;
        }

        /// <summary>
        /// Fecha y hora del ultimo escaneo.
        /// </summary>
        public string LastScanTime
        {
            get => _lastScanTime;
            set => _lastScanTime = value;
        }

        /// <summary>
        /// Si Modular Avatar esta instalado en el proyecto.
        /// </summary>
        public bool MAAvailable
        {
            get => _maAvailable;
            set => _maAvailable = value;
        }

        /// <summary>
        /// Total de componentes detectados en todas las categorias.
        /// </summary>
        public int TotalCount => ProblematicCount + UserDecisionCount + CompatibleCount;

        /// <summary>
        /// Cantidad de componentes problematicos.
        /// </summary>
        public int ProblematicCount => _problematicEntries?.Count(e => e.IsValid) ?? 0;

        /// <summary>
        /// Cantidad de componentes problematicos en raiz de ropa.
        /// </summary>
        public int ProblematicOnClothingRootCount => _problematicEntries?.Count(e => e.IsValid && e.IsOnClothingRoot) ?? 0;

        /// <summary>
        /// Cantidad de componentes que requieren decision del usuario.
        /// </summary>
        public int UserDecisionCount => _userDecisionEntries?.Count(e => e.IsValid) ?? 0;

        /// <summary>
        /// Cantidad de componentes compatibles.
        /// </summary>
        public int CompatibleCount => _compatibleEntries?.Count(e => e.IsValid) ?? 0;

        /// <summary>
        /// Cantidad de meshes detectados en raíces de ropa.
        /// </summary>
        public int MeshOnRootCount => _meshOnRootEntries?.Count(e => e.IsValid) ?? 0;

        /// <summary>
        /// Si hay algun componente problematico.
        /// </summary>
        public bool HasProblematic => ProblematicCount > 0;

        /// <summary>
        /// Si hay meshes en raíces de ropa (error del autor).
        /// </summary>
        public bool HasMeshOnRoot => MeshOnRootCount > 0;

        /// <summary>
        /// Si hay componentes problematicos en raiz de ropa (que se desactivaran automaticamente).
        /// </summary>
        public bool HasProblematicOnClothingRoot => ProblematicOnClothingRootCount > 0;

        /// <summary>
        /// Si hay componentes que requieren decision del usuario.
        /// </summary>
        public bool HasUserDecision => UserDecisionCount > 0;

        /// <summary>
        /// Si hay componentes compatibles.
        /// </summary>
        public bool HasCompatible => CompatibleCount > 0;

        /// <summary>
        /// Si hay cualquier tipo de componente detectado.
        /// </summary>
        public bool HasAny => TotalCount > 0;

        /// <summary>
        /// Todas las entradas combinadas.
        /// </summary>
        public IEnumerable<ColisionEntry> AllEntries =>
            _problematicEntries.Concat(_userDecisionEntries).Concat(_compatibleEntries);

        #endregion

        #region Methods

        /// <summary>
        /// Limpia todos los resultados del escaneo.
        /// </summary>
        public void Clear()
        {
            _problematicEntries.Clear();
            _userDecisionEntries.Clear();
            _compatibleEntries.Clear();
            _meshOnRootEntries.Clear();
            _scanCompleted = false;
            _lastScanTime = "";
        }

        /// <summary>
        /// Agrega una entrada al resultado segun su categoria.
        /// </summary>
        public void AddEntry(ColisionEntry entry)
        {
            if (entry == null) return;

            switch (entry.Category)
            {
                case ColisionCategory.Problematic:
                    _problematicEntries.Add(entry);
                    break;
                case ColisionCategory.UserDecision:
                    _userDecisionEntries.Add(entry);
                    break;
                case ColisionCategory.Compatible:
                    _compatibleEntries.Add(entry);
                    break;
            }
        }

        /// <summary>
        /// Agrega una entrada de mesh en raíz de ropa.
        /// </summary>
        public void AddMeshOnRoot(MeshOnRootEntry entry)
        {
            if (entry == null) return;
            _meshOnRootEntries.Add(entry);
        }

        /// <summary>
        /// Elimina entradas invalidas (componentes destruidos).
        /// </summary>
        /// <returns>Cantidad de entradas eliminadas.</returns>
        public int RemoveInvalidEntries()
        {
            int removed = 0;
            removed += _problematicEntries.RemoveAll(e => !e.IsValid);
            removed += _userDecisionEntries.RemoveAll(e => !e.IsValid);
            removed += _compatibleEntries.RemoveAll(e => !e.IsValid);
            removed += _meshOnRootEntries.RemoveAll(e => !e.IsValid);
            return removed;
        }

        /// <summary>
        /// Obtiene entradas problematicas que estan en raiz de ropa y que el usuario quiere desactivar.
        /// NOTA: No usa IsValid porque en NDMF las referencias pueden estar rotas.
        /// En su lugar, verifica que tenga datos suficientes para buscar el componente.
        /// </summary>
        public IEnumerable<ColisionEntry> GetProblematicOnClothingRoot()
        {
            return _problematicEntries.Where(e => e.IsOnClothingRoot && e.UserWantsDisabled && e.HasSearchableData);
        }

        /// <summary>
        /// Obtiene entradas de decision de usuario que el usuario quiere desactivar.
        /// NOTA: No usa IsValid porque en NDMF las referencias pueden estar rotas.
        /// </summary>
        public IEnumerable<ColisionEntry> GetUserDecisionToDisable()
        {
            return _userDecisionEntries.Where(e => e.UserWantsDisabled && e.HasSearchableData);
        }

        /// <summary>
        /// Obtiene entradas problematicas que el usuario quiere desactivar,
        /// excluyendo las que estan en raiz de ropa (esas se manejan por GetProblematicOnClothingRoot).
        /// NOTA: No usa IsValid porque en NDMF las referencias pueden estar rotas.
        /// </summary>
        public IEnumerable<ColisionEntry> GetProblematicToDisable()
        {
            return _problematicEntries.Where(e => e.UserWantsDisabled && !e.IsOnClothingRoot && e.HasSearchableData);
        }

        /// <summary>
        /// Marca el escaneo como completado.
        /// </summary>
        public void MarkCompleted()
        {
            _scanCompleted = true;
            _lastScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Obtiene un resumen del resultado del escaneo.
        /// </summary>
        public string GetSummary()
        {
            if (!_scanCompleted)
                return "Sin escanear";

            var parts = new List<string>();

            // Meshes en raíz de ropa (siempre mostrar, no depende de MA)
            if (MeshOnRootCount > 0)
                parts.Add($"{MeshOnRootCount} mesh(es) en raíz de ropa");

            if (!_maAvailable)
            {
                if (parts.Count == 0)
                    return "Modular Avatar no instalado";
                return string.Join(", ", parts) + " (MA no instalado)";
            }

            if (TotalCount == 0 && MeshOnRootCount == 0)
                return "Sin colisiones detectadas";

            if (ProblematicCount > 0)
                parts.Add($"{ProblematicCount} problematico(s)");

            if (UserDecisionCount > 0)
                parts.Add($"{UserDecisionCount} requiere decision");

            if (CompatibleCount > 0)
                parts.Add($"{CompatibleCount} compatible(s)");

            return parts.Count > 0 ? string.Join(", ", parts) : "Sin colisiones detectadas";
        }

        public override string ToString() => GetSummary();

        #endregion
    }
}
