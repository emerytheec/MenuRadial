using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Models;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;

namespace Bender_Dios.MenuRadial.Components.AnalisisColision
{
    /// <summary>
    /// MR Analisis Colision - Detecta componentes de Modular Avatar que pueden interferir con Menu Radial.
    ///
    /// Categorias:
    /// - Problematic: Se desactivan automaticamente si estan en raiz de ropa
    /// - UserDecision: El usuario decide si desactivar
    /// - Compatible: Solo informativos, MR los respeta
    /// </summary>
    [AddComponentMenu("Bender Dios/MR Analisis Colision")]
    public class MRAnalisisColision : MRComponentBase
    {
        #region Serialized Fields

        [Header("Avatar")]
        [SerializeField]
        [Tooltip("GameObject raiz del avatar")]
        private GameObject _avatarRoot;

        [Header("Resultados del Escaneo")]
        [SerializeField]
        private ColisionScanResult _scanResult = new ColisionScanResult();

        [Header("Configuracion")]
        [SerializeField]
        [Tooltip("Desactivar automaticamente componentes problematicos en raiz de ropa")]
        private bool _autoDisableProblematicOnRoot = true;

        [SerializeField]
        [Tooltip("Lista de GameObjects raiz de ropa (poblada por MRCoserRopa)")]
        private List<GameObject> _clothingRoots = new List<GameObject>();

        #endregion

        #region Properties

        /// <summary>
        /// GameObject raiz del avatar.
        /// </summary>
        public GameObject AvatarRoot
        {
            get => _avatarRoot;
            set
            {
                if (_avatarRoot != value)
                {
                    _avatarRoot = value;
                    OnAvatarChanged();
                }
            }
        }

        /// <summary>
        /// Resultado del ultimo escaneo.
        /// </summary>
        public ColisionScanResult ScanResult => _scanResult;

        /// <summary>
        /// Si el escaneo se completo.
        /// </summary>
        public bool IsScanned => _scanResult?.ScanCompleted ?? false;

        /// <summary>
        /// Si Modular Avatar esta instalado.
        /// </summary>
        public bool IsMAAvailable => ModularAvatarDetector.Instance.IsModularAvatarAvailable;

        /// <summary>
        /// Cantidad de componentes problematicos detectados.
        /// </summary>
        public int ProblematicCount => _scanResult?.ProblematicCount ?? 0;

        /// <summary>
        /// Cantidad de componentes problematicos en raiz de ropa.
        /// </summary>
        public int ProblematicOnRootCount => _scanResult?.ProblematicOnClothingRootCount ?? 0;

        /// <summary>
        /// Cantidad de componentes que requieren decision del usuario.
        /// </summary>
        public int UserDecisionCount => _scanResult?.UserDecisionCount ?? 0;

        /// <summary>
        /// Cantidad de componentes compatibles.
        /// </summary>
        public int CompatibleCount => _scanResult?.CompatibleCount ?? 0;

        /// <summary>
        /// Total de componentes detectados.
        /// </summary>
        public int TotalCount => _scanResult?.TotalCount ?? 0;

        /// <summary>
        /// Si hay componentes problematicos.
        /// </summary>
        public bool HasProblematic => _scanResult?.HasProblematic ?? false;

        /// <summary>
        /// Si hay componentes problematicos en raiz de ropa.
        /// </summary>
        public bool HasProblematicOnRoot => _scanResult?.HasProblematicOnClothingRoot ?? false;

        /// <summary>
        /// Si hay componentes que requieren decision del usuario.
        /// </summary>
        public bool HasUserDecision => _scanResult?.HasUserDecision ?? false;

        /// <summary>
        /// Si hay cualquier colision detectada.
        /// </summary>
        public bool HasAnyColision => _scanResult?.HasAny ?? false;

        /// <summary>
        /// Si se deben desactivar automaticamente los componentes problematicos en raiz de ropa.
        /// </summary>
        public bool AutoDisableProblematicOnRoot
        {
            get => _autoDisableProblematicOnRoot;
            set => _autoDisableProblematicOnRoot = value;
        }

        /// <summary>
        /// Lista de GameObjects raiz de ropa.
        /// </summary>
        public List<GameObject> ClothingRoots => _clothingRoots;

        #endregion

        #region Lifecycle Methods

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            UpdateVersion("0.001");
            _scanResult ??= new ColisionScanResult();
            _clothingRoots ??= new List<GameObject>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Llamado cuando cambia el avatar asignado.
        /// </summary>
        public void OnAvatarChanged()
        {
            ClearScanResults();

            if (_avatarRoot == null)
                return;

            // Escanear automaticamente
            ScanAvatar();
        }

        /// <summary>
        /// Escanea el avatar buscando componentes de MA que puedan colisionar.
        /// </summary>
        public void ScanAvatar()
        {
            _scanResult.Clear();

            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MRAnalisisColision] No hay avatar asignado");
                return;
            }

            _scanResult.MAAvailable = IsMAAvailable;

            if (!IsMAAvailable)
            {
                Debug.Log("[MRAnalisisColision] Modular Avatar no esta instalado");
                _scanResult.MarkCompleted();
                return;
            }

            // Usar el detector para obtener todos los componentes de MA
            var maResult = ModularAvatarDetector.Instance.ScanForColisions(_avatarRoot);

            // Convertir a nuestro modelo y agregar informacion de ropa
            ProcessMAEntries(maResult.ProblematicEntries, ColisionCategory.Problematic);
            ProcessMAEntries(maResult.UserDecisionEntries, ColisionCategory.UserDecision);
            ProcessMAEntries(maResult.CompatibleEntries, ColisionCategory.Compatible);

            _scanResult.MarkCompleted();

            Debug.Log($"[MRAnalisisColision] Escaneo completado: {_scanResult.GetSummary()}");

            // Desactivar automaticamente problematicos en raiz de ropa si esta habilitado
            if (_autoDisableProblematicOnRoot && _scanResult.HasProblematicOnClothingRoot)
            {
                int disabled = DisableProblematicOnClothingRoots();
                if (disabled > 0)
                {
                    Debug.Log($"[MRAnalisisColision] Desactivados {disabled} componente(s) problematico(s) en raiz de ropa");
                }
            }
        }

        /// <summary>
        /// Actualiza la lista de raices de ropa desde MRCoserRopa.
        /// </summary>
        public void UpdateClothingRoots(List<GameObject> roots)
        {
            _clothingRoots.Clear();
            if (roots != null)
            {
                _clothingRoots.AddRange(roots);
            }

            // Re-marcar entradas existentes
            UpdateClothingRootFlags();
        }

        /// <summary>
        /// Desactiva todos los componentes problematicos que estan en raiz de ropa.
        /// </summary>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableProblematicOnClothingRoots()
        {
            if (_scanResult == null || !_scanResult.HasProblematicOnClothingRoot)
                return 0;

            int count = 0;
            foreach (var entry in _scanResult.GetProblematicOnClothingRoot())
            {
                if (entry.Disable())
                {
                    count++;
                    Debug.Log($"[MRAnalisisColision] Desactivado {entry.ShortTypeName} en '{entry.GameObjectName}'");
                }
            }

            return count;
        }

        /// <summary>
        /// Desactiva todos los componentes problematicos (sin importar ubicacion).
        /// </summary>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableAllProblematic()
        {
            if (_scanResult == null || !_scanResult.HasProblematic)
                return 0;

            int count = 0;
            foreach (var entry in _scanResult.ProblematicEntries)
            {
                if (entry.IsValid && entry.IsEnabled && entry.Disable())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Desactiva los componentes de decision de usuario que el usuario marco para desactivar.
        /// </summary>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableUserSelectedComponents()
        {
            if (_scanResult == null || !_scanResult.HasUserDecision)
                return 0;

            int count = 0;
            foreach (var entry in _scanResult.GetUserDecisionToDisable())
            {
                if (entry.Disable())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Restaura todos los componentes a su estado original.
        /// </summary>
        /// <returns>Cantidad de componentes restaurados.</returns>
        public int RestoreAllComponents()
        {
            if (_scanResult == null)
                return 0;

            int count = 0;

            foreach (var entry in _scanResult.AllEntries)
            {
                if (entry.Restore())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Limpia los resultados del escaneo.
        /// </summary>
        public void ClearScanResults()
        {
            _scanResult.Clear();
        }

        /// <summary>
        /// Refresca el escaneo.
        /// </summary>
        public void Refresh()
        {
            if (_avatarRoot != null)
            {
                ScanAvatar();
            }
        }

        #endregion

        #region Private Methods

        private void ProcessMAEntries(List<MAColisionEntry> entries, ColisionCategory category)
        {
            if (entries == null) return;

            foreach (var maEntry in entries)
            {
                if (!maEntry.IsValid) continue;

                var entry = new ColisionEntry(
                    maEntry.Component,
                    maEntry.TypeName,
                    category,
                    maEntry.HierarchyPath
                );

                // Verificar si esta en raiz de ropa
                UpdateEntryClothingInfo(entry);

                _scanResult.AddEntry(entry);
            }
        }

        private void UpdateEntryClothingInfo(ColisionEntry entry)
        {
            if (entry?.Component == null || _clothingRoots == null)
                return;

            foreach (var root in _clothingRoots)
            {
                if (root == null) continue;

                if (entry.Component.gameObject == root)
                {
                    entry.IsOnClothingRoot = true;
                    entry.ClothingName = root.name;
                    return;
                }
            }

            entry.IsOnClothingRoot = false;
            entry.ClothingName = "";
        }

        private void UpdateClothingRootFlags()
        {
            if (_scanResult == null) return;

            foreach (var entry in _scanResult.AllEntries)
            {
                UpdateEntryClothingInfo(entry);
            }
        }

        #endregion

        #region IValidatable Implementation

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (_avatarRoot == null)
            {
                result.AddChild(ValidationResult.Warning("Arrastra tu avatar aqui"));
                return result;
            }

            if (!IsMAAvailable)
            {
                result.AddChild(ValidationResult.Info("Modular Avatar no esta instalado"));
                return result;
            }

            if (!IsScanned)
            {
                result.AddChild(ValidationResult.Warning("Haz clic en 'Escanear' para detectar colisiones"));
                return result;
            }

            // Mostrar resultados
            if (!HasAnyColision)
            {
                result.AddChild(ValidationResult.Success("Sin colisiones detectadas"));
                return result;
            }

            // Problematicos
            if (HasProblematic)
            {
                int onRoot = ProblematicOnRootCount;
                if (onRoot > 0)
                {
                    result.AddChild(ValidationResult.Error(
                        $"{onRoot} componente(s) problematico(s) en raiz de ropa"));
                }
                else
                {
                    result.AddChild(ValidationResult.Warning(
                        $"{ProblematicCount} componente(s) problematico(s) detectado(s)"));
                }
            }

            // Decision de usuario
            if (HasUserDecision)
            {
                result.AddChild(ValidationResult.Warning(
                    $"{UserDecisionCount} componente(s) requieren tu decision"));
            }

            // Compatibles
            if (CompatibleCount > 0)
            {
                result.AddChild(ValidationResult.Info(
                    $"{CompatibleCount} componente(s) compatible(s)"));
            }

            return result;
        }

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();

            // Auto-escanear si el avatar cambio y no hay resultados
            if (_avatarRoot != null && !IsScanned)
            {
                ScanAvatar();
            }
        }
#endif

        #endregion
    }
}
