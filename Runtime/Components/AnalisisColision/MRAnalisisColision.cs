using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Models;
using Bender_Dios.MenuRadial.Components.AnalisisColision.Controllers;
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
    [Icon("Packages/com.bender-dios.menu-radial/Components/Menu/Resources/logo_MR.png")]
    public class MRAnalisisColision : MRComponentBase
    {
        #region Constants

        /// <summary>
        /// Componentes criticos que SIEMPRE se desactivan automaticamente.
        /// Estos modifican vertices/meshes y causan conflictos graves con MR.
        /// NOTA: ShapeChanger fue removido — no causa conflictos reales con MR,
        /// MA lo procesa correctamente y destruirlo rompe funcionalidad del avatar.
        /// </summary>
        private static readonly string[] CRITICAL_COMPONENTS = new[]
        {
            "MeshCutter",
            "VertexFilter"
        };

        /// <summary>
        /// Nombre del componente BlendshapeSync que requiere tratamiento especial.
        /// Este componente sincroniza blendshapes en Edit Mode y necesita
        /// desregistrarse del loop de actualización para detenerse completamente.
        /// </summary>
        private const string BLENDSHAPE_SYNC_TYPE = "ModularAvatarBlendshapeSync";

        /// <summary>
        /// Componentes de Modular Avatar relacionados con el sistema de menú.
        /// Estos pueden ser desactivados por prenda usando el botón "Menú".
        /// </summary>
        private static readonly string[] MENU_COMPONENTS = new[]
        {
            "Animator",                      // Unity Animator
            "ModularAvatarMergeAnimator",    // MA Merge Animator
            "ModularAvatarParameters",       // MA Parameters
            "ModularAvatarMenuInstaller",    // MA Menu Installer
            "ModularAvatarMenuGroup",        // MA Menu Group
            "ModularAvatarMenuItem"          // MA Menu Item
        };

        #endregion

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

        [FormerlySerializedAs("_clothingRoots")]
        [SerializeField]
        [Tooltip("Lista de GameObjects raiz de piezas (poblada por MRCoserRopa)")]
        private List<GameObject> _pieceRoots = new List<GameObject>();

        [SerializeField]
        [Tooltip("Mostrar componentes compatibles en el inspector")]
        private bool _showCompatibleComponents = false;

        [FormerlySerializedAs("_clothingWithMenuDisabled")]
        [SerializeField]
        [Tooltip("Lista de nombres de piezas con menú desactivado")]
        private List<string> _piecesWithMenuDisabled = new List<string>();

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
        /// Cantidad de componentes problematicos en raiz de pieza.
        /// </summary>
        public int ProblematicOnRootCount => _scanResult?.ProblematicOnPieceRootCount ?? 0;

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
        /// Si hay componentes problematicos en raiz de pieza.
        /// </summary>
        public bool HasProblematicOnRoot => _scanResult?.HasProblematicOnPieceRoot ?? false;

        /// <summary>
        /// Si hay componentes que requieren decision del usuario.
        /// </summary>
        public bool HasUserDecision => _scanResult?.HasUserDecision ?? false;

        /// <summary>
        /// Si hay cualquier colision detectada.
        /// </summary>
        public bool HasAnyColision => _scanResult?.HasAny ?? false;

        /// <summary>
        /// Cantidad de meshes detectados en raíces de ropa.
        /// </summary>
        public int MeshOnRootCount => _scanResult?.MeshOnRootCount ?? 0;

        /// <summary>
        /// Si hay meshes en raíces de ropa (error del autor).
        /// </summary>
        public bool HasMeshOnRoot => _scanResult?.HasMeshOnRoot ?? false;

        /// <summary>
        /// Si se deben desactivar automaticamente los componentes problematicos en raiz de ropa.
        /// </summary>
        public bool AutoDisableProblematicOnRoot
        {
            get => _autoDisableProblematicOnRoot;
            set => _autoDisableProblematicOnRoot = value;
        }

        /// <summary>
        /// Lista de GameObjects raiz de piezas.
        /// </summary>
        public List<GameObject> PieceRoots => _pieceRoots;

        /// <summary>
        /// Si se deben mostrar los componentes compatibles en el inspector.
        /// </summary>
        public bool ShowCompatibleComponents
        {
            get => _showCompatibleComponents;
            set => _showCompatibleComponents = value;
        }

        /// <summary>
        /// Si el controlador de BlendshapeSync está disponible (reflexión funciona).
        /// </summary>
        public bool IsBlendshapeSyncControlAvailable => BlendshapeSyncController.IsAvailable;

        /// <summary>
        /// Cantidad de componentes BlendshapeSync detectados.
        /// </summary>
        public int BlendshapeSyncCount
        {
            get
            {
                if (_scanResult == null) return 0;
                return _scanResult.AllEntries
                    .Count(e => e.IsValid && IsBlendshapeSyncComponent(e.ComponentTypeName));
            }
        }

        /// <summary>
        /// Cantidad de BlendshapeSync actualmente detenidos (sincronización desactivada).
        /// </summary>
        public int BlendshapeSyncStoppedCount
        {
            get
            {
                if (_scanResult == null) return 0;
                return _scanResult.AllEntries
                    .Count(e => e.IsValid &&
                                IsBlendshapeSyncComponent(e.ComponentTypeName) &&
                                BlendshapeSyncController.IsStopped(e.Component));
            }
        }

        /// <summary>
        /// Si hay BlendshapeSync que pueden ser detenidos.
        /// </summary>
        public bool HasActiveBlendshapeSyncs => BlendshapeSyncCount > BlendshapeSyncStoppedCount;

        /// <summary>
        /// Si hay BlendshapeSync detenidos que pueden ser restaurados.
        /// </summary>
        public bool HasStoppedBlendshapeSyncs => BlendshapeSyncStoppedCount > 0;

        #endregion

        #region Lifecycle Methods

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            _scanResult ??= new ColisionScanResult();
            _pieceRoots ??= new List<GameObject>();
            _piecesWithMenuDisabled ??= new List<string>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Llamado cuando cambia el avatar asignado.
        /// Limpia resultados previos pero NO escanea automáticamente.
        /// El escaneo se controla desde MRMenuRadial.AutoDetectAll() para
        /// asegurar que se haga DESPUÉS de detectar las ropas.
        /// </summary>
        public void OnAvatarChanged()
        {
            ClearScanResults();
        }

        /// <summary>
        /// Escanea el avatar buscando componentes de MA que puedan colisionar.
        /// También detecta meshes problemáticos en raíces de ropa.
        /// </summary>
        public void ScanAvatar()
        {
            _scanResult.Clear();

            if (_avatarRoot == null)
            {
                Debug.LogWarning("[MRAnalisisColision] No hay avatar asignado");
                return;
            }

            // SIEMPRE escanear meshes en raíces de piezas (no depende de MA)
            ScanMeshesOnPieceRoots();

            _scanResult.MAAvailable = IsMAAvailable;

            if (!IsMAAvailable)
            {
                Debug.Log("[MRAnalisisColision] Modular Avatar no esta instalado");
                _scanResult.MarkCompleted();

                // Log de meshes en raíz aunque MA no esté instalado
                if (_scanResult.HasMeshOnRoot)
                {
                    Debug.LogWarning($"[MRAnalisisColision] Detectados {_scanResult.MeshOnRootCount} mesh(es) en raiz de ropa (error del autor)");
                }
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

            // SIEMPRE desactivar componentes criticos (MeshCutter, VertexFilter)
            int criticalDisabled = DisableCriticalComponents();
            if (criticalDisabled > 0)
            {
                Debug.Log($"[MRAnalisisColision] Desactivados {criticalDisabled} componente(s) critico(s) automaticamente");
            }

            // Desactivar automaticamente problematicos en raiz de pieza si esta habilitado
            if (_autoDisableProblematicOnRoot && _scanResult.HasProblematicOnPieceRoot)
            {
                int disabled = DisableProblematicOnPieceRoots();
                if (disabled > 0)
                {
                    Debug.Log($"[MRAnalisisColision] Desactivados {disabled} componente(s) problematico(s) en raiz de ropa");
                }
            }
        }

        /// <summary>
        /// Escanea las raíces de piezas buscando meshes directamente en el GameObject raíz.
        /// Esto es un error del autor - los meshes deberían estar en GameObjects hijos.
        /// </summary>
        private void ScanMeshesOnPieceRoots()
        {
            if (_pieceRoots == null || _pieceRoots.Count == 0)
                return;

            foreach (var pieceRoot in _pieceRoots)
            {
                if (pieceRoot == null) continue;

                // Buscar Renderer directamente en el GameObject raíz de la pieza
                var renderers = pieceRoot.GetComponents<Renderer>();

                foreach (var renderer in renderers)
                {
                    if (renderer == null) continue;

                    // Calcular ruta de jerarquía
                    string hierarchyPath = GetHierarchyPath(renderer.transform);

                    // Crear entrada para este mesh problemático
                    var entry = new MeshOnRootEntry(renderer, pieceRoot, hierarchyPath);
                    _scanResult.AddMeshOnRoot(entry);

                    Debug.LogWarning($"[MRAnalisisColision] Mesh en raíz de pieza: '{renderer.gameObject.name}' ({renderer.GetType().Name}) en '{pieceRoot.name}'. " +
                                     "Esto es un error del autor - el mesh debería estar en un GameObject hijo.");
                }
            }
        }

        /// <summary>
        /// Obtiene la ruta de jerarquía de un transform relativa al avatar.
        /// </summary>
        private string GetHierarchyPath(Transform target)
        {
            if (target == null || _avatarRoot == null) return "";

            var path = new System.Text.StringBuilder();
            var current = target;

            while (current != null && current.gameObject != _avatarRoot)
            {
                if (path.Length > 0)
                    path.Insert(0, "/");
                path.Insert(0, current.name);
                current = current.parent;
            }

            return path.ToString();
        }

        /// <summary>
        /// Actualiza la lista de raices de piezas desde MRCoserRopa.
        /// </summary>
        public void UpdatePieceRoots(List<GameObject> roots)
        {
            _pieceRoots.Clear();
            if (roots != null)
            {
                _pieceRoots.AddRange(roots);
            }

            // Re-marcar entradas existentes
            UpdatePieceRootFlags();

            // Limpiar entradas de menú desactivado que ya no existen
            CleanupOrphanedMenuDisabledEntries();
        }

        /// <summary>
        /// Desactiva todos los componentes problematicos que estan en raiz de pieza.
        /// </summary>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableProblematicOnPieceRoots()
        {
            if (_scanResult == null || !_scanResult.HasProblematicOnPieceRoot)
                return 0;

            int count = 0;
            foreach (var entry in _scanResult.GetProblematicOnPieceRoot())
            {
                if (entry.Destroy())
                {
                    count++;
                    Debug.Log($"[MRAnalisisColision] Eliminado {entry.ShortTypeName} en '{entry.GameObjectName}'");
                }
            }

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

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
                if (entry.IsValid && entry.IsEnabled && entry.Destroy())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Desactiva componentes criticos (MeshCutter, VertexFilter) en raiz de ropa.
        /// Solo se desactivan automaticamente si estan en la raiz de una prenda detectada.
        /// </summary>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableCriticalComponents()
        {
            if (_scanResult == null)
                return 0;

            int count = 0;

            // Buscar en todas las categorias
            foreach (var entry in _scanResult.AllEntries)
            {
                if (!entry.IsValid || !entry.IsEnabled)
                    continue;

                // Verificar si es un componente critico Y esta en raiz de pieza
                if (IsCriticalComponent(entry.ComponentTypeName) && entry.IsOnPieceRoot)
                {
                    if (entry.Destroy())
                    {
                        count++;
                        Debug.Log($"[MRAnalisisColision] Eliminado (critico) {entry.ShortTypeName} en '{entry.GameObjectName}'");
                    }
                }
            }

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Verifica si un tipo de componente es critico.
        /// </summary>
        private bool IsCriticalComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            foreach (var critical in CRITICAL_COMPONENTS)
            {
                if (typeName.Contains(critical))
                    return true;
            }

            return false;
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
                if (entry.Destroy())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Destruye los componentes problematicos que el usuario marco para desactivar.
        /// </summary>
        /// <returns>Cantidad de componentes destruidos.</returns>
        public int DisableUserSelectedProblematic()
        {
            if (_scanResult == null || !_scanResult.HasProblematic)
                return 0;

            int count = 0;
            foreach (var entry in _scanResult.GetProblematicToDisable())
            {
                if (entry.Destroy())
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

            // Limpiar lista de menús desactivados ya que todo fue restaurado
            _piecesWithMenuDisabled.Clear();

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Limpia los resultados del escaneo.
        /// </summary>
        public void ClearScanResults()
        {
            _scanResult.Clear();
            // También limpiar lista de menús desactivados ya que el escaneo se reinicia
            _piecesWithMenuDisabled.Clear();
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

        #region BlendshapeSync Methods

        /// <summary>
        /// Detiene la sincronización de todos los BlendshapeSync detectados.
        /// Usa reflexión para desregistrar del loop de actualización y limpiar bindings.
        /// </summary>
        /// <returns>Cantidad de componentes detenidos.</returns>
        public int StopAllBlendshapeSyncs()
        {
            if (_scanResult == null || !BlendshapeSyncController.IsAvailable)
                return 0;

            int count = 0;

            foreach (var entry in _scanResult.AllEntries)
            {
                if (!entry.IsValid || !IsBlendshapeSyncComponent(entry.ComponentTypeName))
                    continue;

                if (BlendshapeSyncController.IsStopped(entry.Component))
                    continue;

                if (BlendshapeSyncController.StopSync(entry.Component))
                {
                    count++;
                    Debug.Log($"[MRAnalisisColision] BlendshapeSync detenido en '{entry.GameObjectName}'");
                }
            }

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Restaura la sincronización de todos los BlendshapeSync detenidos.
        /// </summary>
        /// <returns>Cantidad de componentes restaurados.</returns>
        public int RestoreAllBlendshapeSyncs()
        {
            if (_scanResult == null || !BlendshapeSyncController.IsAvailable)
                return 0;

            int count = 0;

            foreach (var entry in _scanResult.AllEntries)
            {
                if (!entry.IsValid || !IsBlendshapeSyncComponent(entry.ComponentTypeName))
                    continue;

                if (!BlendshapeSyncController.IsStopped(entry.Component))
                    continue;

                if (BlendshapeSyncController.RestoreSync(entry.Component))
                {
                    count++;
                    Debug.Log($"[MRAnalisisColision] BlendshapeSync restaurado en '{entry.GameObjectName}'");
                }
            }

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Detiene la sincronización de un BlendshapeSync específico.
        /// </summary>
        public bool StopBlendshapeSync(ColisionEntry entry)
        {
            if (entry == null || !entry.IsValid || !IsBlendshapeSyncComponent(entry.ComponentTypeName))
                return false;

            if (!BlendshapeSyncController.IsAvailable)
                return false;

            bool result = BlendshapeSyncController.StopSync(entry.Component);

#if UNITY_EDITOR
            if (result && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return result;
        }

        /// <summary>
        /// Restaura la sincronización de un BlendshapeSync específico.
        /// </summary>
        public bool RestoreBlendshapeSync(ColisionEntry entry)
        {
            if (entry == null || !entry.IsValid || !IsBlendshapeSyncComponent(entry.ComponentTypeName))
                return false;

            if (!BlendshapeSyncController.IsAvailable)
                return false;

            bool result = BlendshapeSyncController.RestoreSync(entry.Component);

#if UNITY_EDITOR
            if (result && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return result;
        }

        /// <summary>
        /// Verifica si un BlendshapeSync específico está detenido.
        /// </summary>
        public bool IsBlendshapeSyncStopped(ColisionEntry entry)
        {
            if (entry == null || !entry.IsValid || !IsBlendshapeSyncComponent(entry.ComponentTypeName))
                return false;

            return BlendshapeSyncController.IsStopped(entry.Component);
        }

        /// <summary>
        /// Verifica si un nombre de tipo corresponde a BlendshapeSync.
        /// </summary>
        private bool IsBlendshapeSyncComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            return typeName.Contains("BlendshapeSync");
        }

        /// <summary>
        /// Obtiene información de diagnóstico del controlador de BlendshapeSync.
        /// </summary>
        public string GetBlendshapeSyncControllerStatus()
        {
            return BlendshapeSyncController.GetStatusInfo();
        }

        #endregion

        #region Menu Toggle Methods

        /// <summary>
        /// Verifica si el menú está desactivado para una pieza específica.
        /// </summary>
        /// <param name="pieceName">Nombre de la pieza.</param>
        /// <returns>True si el menú está desactivado para esta pieza.</returns>
        public bool IsMenuDisabledForPiece(string pieceName)
        {
            if (string.IsNullOrEmpty(pieceName)) return false;
            return _piecesWithMenuDisabled.Contains(pieceName);
        }

        /// <summary>
        /// Alterna el estado del menú (activo/desactivado) para una pieza específica.
        /// </summary>
        /// <param name="pieceName">Nombre de la pieza.</param>
        /// <returns>True si ahora el menú está desactivado, False si está activo.</returns>
        public bool ToggleMenuForPiece(string pieceName)
        {
            if (string.IsNullOrEmpty(pieceName)) return false;

            if (IsMenuDisabledForPiece(pieceName))
            {
                EnableMenuComponentsForPiece(pieceName);
                return false;
            }
            else
            {
                DisableMenuComponentsForPiece(pieceName);
                return true;
            }
        }

        /// <summary>
        /// Desactiva todos los componentes de menú de MA para una pieza específica.
        /// Esto desactiva el componente en Edit Mode Y lo marca para destrucción en NDMF.
        /// </summary>
        /// <param name="pieceName">Nombre de la pieza.</param>
        /// <returns>Cantidad de componentes desactivados.</returns>
        public int DisableMenuComponentsForPiece(string pieceName)
        {
            if (string.IsNullOrEmpty(pieceName) || _scanResult == null)
                return 0;

            int count = 0;

            foreach (var entry in _scanResult.AllEntries)
            {
                if (!entry.IsValid)
                    continue;

                // Verificar si pertenece a esta pieza
                if (!BelongsToPiece(entry, pieceName))
                    continue;

                // Verificar si es un componente de menú
                if (!IsMenuComponent(entry.ComponentTypeName))
                    continue;

                // Destruir en Edit Mode
                if (entry.IsEnabled)
                {
                    entry.Destroy();
                }

                // Marcar para destrucción en NDMF (UserWantsDisabled = true significa "desactivar/destruir")
                entry.UserWantsDisabled = true;

                count++;
                Debug.Log($"[MRAnalisisColision] Menú: Desactivado {entry.ShortTypeName} en '{entry.GameObjectName}' (pieza: {pieceName})");
            }

            // Registrar que esta pieza tiene menú desactivado
            if (!_piecesWithMenuDisabled.Contains(pieceName))
            {
                _piecesWithMenuDisabled.Add(pieceName);
            }

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Restaura (activa) todos los componentes de menú de MA para una pieza específica.
        /// Esto restaura el componente en Edit Mode Y lo desmarca para destrucción en NDMF.
        /// </summary>
        /// <param name="pieceName">Nombre de la pieza.</param>
        /// <returns>Cantidad de componentes restaurados.</returns>
        public int EnableMenuComponentsForPiece(string pieceName)
        {
            if (string.IsNullOrEmpty(pieceName) || _scanResult == null)
                return 0;

            int count = 0;

            foreach (var entry in _scanResult.AllEntries)
            {
                if (!entry.IsValid)
                    continue;

                // Verificar si pertenece a esta pieza
                if (!BelongsToPiece(entry, pieceName))
                    continue;

                // Verificar si es un componente de menú
                if (!IsMenuComponent(entry.ComponentTypeName))
                    continue;

                // Restaurar en Edit Mode
                entry.Restore();

                // Desmarcar para destrucción en NDMF (UserWantsDisabled = false significa "mantener activo")
                entry.UserWantsDisabled = false;

                count++;
                Debug.Log($"[MRAnalisisColision] Menú: Restaurado {entry.ShortTypeName} en '{entry.GameObjectName}' (pieza: {pieceName})");
            }

            // Quitar de la lista de menús desactivados
            _piecesWithMenuDisabled.Remove(pieceName);

#if UNITY_EDITOR
            if (count > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif

            return count;
        }

        /// <summary>
        /// Obtiene la cantidad de componentes de menú de MA en una pieza específica.
        /// </summary>
        /// <param name="pieceName">Nombre de la pieza.</param>
        /// <returns>Cantidad de componentes de menú encontrados.</returns>
        public int GetMenuComponentCountForPiece(string pieceName)
        {
            if (string.IsNullOrEmpty(pieceName) || _scanResult == null)
                return 0;

            return _scanResult.AllEntries
                .Count(e => e.IsValid &&
                            BelongsToPiece(e, pieceName) &&
                            IsMenuComponent(e.ComponentTypeName));
        }

        /// <summary>
        /// Verifica si una entrada pertenece a una pieza específica.
        /// Busca si el GameObject de la entrada es hijo (o es) del root de la pieza.
        /// </summary>
        private bool BelongsToPiece(ColisionEntry entry, string pieceName)
        {
            if (entry?.Component == null || string.IsNullOrEmpty(pieceName))
                return false;

            // Buscar el root de la pieza
            var pieceRoot = _pieceRoots.FirstOrDefault(r => r != null && r.name == pieceName);
            if (pieceRoot == null)
                return false;

            // Verificar si el componente está en el root o es hijo del root
            return entry.Component.gameObject == pieceRoot ||
                   entry.Component.transform.IsChildOf(pieceRoot.transform);
        }

        /// <summary>
        /// Verifica si un tipo de componente es un componente de menú.
        /// </summary>
        private bool IsMenuComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            foreach (var menuComp in MENU_COMPONENTS)
            {
                if (typeName.Contains(menuComp))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Limpia las prendas de la lista de menú desactivado que ya no existen.
        /// </summary>
        public void CleanupOrphanedMenuDisabledEntries()
        {
            if (_pieceRoots == null || _piecesWithMenuDisabled == null)
                return;

            var validPieceNames = _pieceRoots
                .Where(r => r != null)
                .Select(r => r.name)
                .ToHashSet();

            _piecesWithMenuDisabled.RemoveAll(name => !validPieceNames.Contains(name));
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

                // Verificar si esta en raiz de pieza
                UpdateEntryPieceInfo(entry);

                // Re-clasificar: componentes problematicos que NO estan en raiz de pieza
                // deben ser UserDecision (el usuario decide si desactivarlos)
                if (category == ColisionCategory.Problematic && !entry.IsOnPieceRoot)
                {
                    entry.Category = ColisionCategory.UserDecision;
                    // UserDecision: checkbox marcado (se mantiene activo)
                    entry.UserWantsDisabled = false;
                }

                _scanResult.AddEntry(entry);
            }
        }

        private void UpdateEntryPieceInfo(ColisionEntry entry)
        {
            if (entry?.Component == null || _pieceRoots == null)
                return;

            foreach (var root in _pieceRoots)
            {
                if (root == null) continue;

                if (entry.Component.gameObject == root)
                {
                    entry.IsOnPieceRoot = true;
                    entry.PieceRootName = root.name;
                    return;
                }
            }

            entry.IsOnPieceRoot = false;
            entry.PieceRootName = "";
        }

        private void UpdatePieceRootFlags()
        {
            if (_scanResult == null) return;

            foreach (var entry in _scanResult.AllEntries)
            {
                UpdateEntryPieceInfo(entry);
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

            if (!IsScanned)
            {
                result.AddChild(ValidationResult.Warning("Detecta las ropas para escanear colisiones automaticamente"));
                return result;
            }

            // Meshes en raíz de ropa (siempre mostrar, independiente de MA)
            if (HasMeshOnRoot)
            {
                result.AddChild(ValidationResult.Warning(
                    $"{MeshOnRootCount} mesh(es) en raiz de ropa detectado(s). " +
                    "Esto puede causar que el sistema confunda estos meshes con meshes del avatar base."));
            }

            if (!IsMAAvailable)
            {
                if (!HasMeshOnRoot)
                {
                    result.AddChild(ValidationResult.Info("Modular Avatar no esta instalado"));
                }
                return result;
            }

            // Mostrar resultados
            if (!HasAnyColision && !HasMeshOnRoot)
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
            // El escaneo se controla desde MRMenuRadial.AutoDetectAll()
            // No escanear automáticamente aquí para evitar escaneos sin las ropas detectadas
        }
#endif

        #endregion
    }
}
