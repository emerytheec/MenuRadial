#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.AnalisisColision;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Controllers;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.AnalisisColision.MRAnalisisColisionPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.AnalisisColision
{
    /// <summary>
    /// Plugin NDMF para MRAnalisisColision.
    /// Se ejecuta en la fase Resolving, ANTES de que Modular Avatar procese el avatar.
    ///
    /// Acciones:
    /// - Desactiva componentes problematicos de MA en raiz de ropa
    /// - Desactiva componentes marcados por el usuario
    /// - Destruye el componente MRAnalisisColision
    /// </summary>
    public class MRAnalisisColisionPlugin : Plugin<MRAnalisisColisionPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.analisis_colision";
        public override string DisplayName => "MR Analisis Colision";

        // Color tema: Naranja (advertencia)
        public override Color? ThemeColor => new Color(0.9f, 0.6f, 0.2f, 1);

        protected override void Configure()
        {
            // Ejecutar en fase Resolving, ANTES de Modular Avatar
            // Esto es critico para que los componentes problematicos esten desactivados
            // cuando MA intente procesarlos
            InPhase(BuildPhase.Resolving)
                .BeforePlugin("nadena.dev.modular-avatar") // Antes de MA
                .Run(MRAnalisisColisionPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRAnalisisColision] Error durante el procesamiento NDMF: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que procesa los componentes de MA detectados.
    /// </summary>
    internal class MRAnalisisColisionPass : Pass<MRAnalisisColisionPass>
    {
        public override string DisplayName => "Analisis Colision (Desactivar MA)";

        protected override void Execute(BuildContext context)
        {
            // Buscar todos los componentes MRAnalisisColision en el avatar
            var analisisComponents = context.AvatarRootObject.GetComponentsInChildren<MRAnalisisColision>(true);

            if (analisisComponents.Length == 0)
            {
                return; // No hay nada que procesar
            }

            Debug.Log($"[MRAnalisisColision NDMF] Procesando {analisisComponents.Length} componente(s) MRAnalisisColision...");

            int totalDisabled = 0;

            foreach (var analisis in analisisComponents)
            {
                if (!analisis.enabled)
                {
                    Debug.Log($"[MRAnalisisColision NDMF] Saltando '{analisis.gameObject.name}' (deshabilitado)");
                    continue;
                }

                try
                {
                    // Obtener la lista de raices de ropa desde MRCoserRopa si esta disponible
                    UpdateClothingRootsFromCoserRopa(analisis, context.AvatarRootObject);

                    // Re-escanear si es necesario
                    if (!analisis.IsScanned)
                    {
                        analisis.ScanAvatar();
                    }

                    // Desactivar componentes problematicos en raiz de ropa
                    if (analisis.AutoDisableProblematicOnRoot && analisis.HasProblematicOnRoot)
                    {
                        int disabled = DisableProblematicOnRoot(analisis);
                        totalDisabled += disabled;

                        if (disabled > 0)
                        {
                            Debug.Log($"[MRAnalisisColision NDMF] Desactivados {disabled} componente(s) problematico(s) en raiz de ropa");
                        }
                    }

                    // Desactivar componentes marcados por el usuario
                    int userDisabled = DisableUserSelectedComponents(analisis);
                    totalDisabled += userDisabled;

                    if (userDisabled > 0)
                    {
                        Debug.Log($"[MRAnalisisColision NDMF] Desactivados {userDisabled} componente(s) marcados por el usuario");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MRAnalisisColision NDMF] Error procesando '{analisis.gameObject.name}': {e.Message}");
                    Debug.LogException(e);
                }

                // Destruir el componente MRAnalisisColision despues de procesar
                UnityEngine.Object.DestroyImmediate(analisis);
            }

            Debug.Log($"[MRAnalisisColision NDMF] Procesamiento completado: {totalDisabled} componente(s) desactivado(s)");
        }

        /// <summary>
        /// Actualiza la lista de raices de ropa desde MRCoserRopa.
        /// </summary>
        private void UpdateClothingRootsFromCoserRopa(MRAnalisisColision analisis, GameObject avatarRoot)
        {
            try
            {
                var coserRopa = avatarRoot.GetComponentInChildren<MRCoserRopa>(true);
                if (coserRopa != null && coserRopa.DetectedClothingCount > 0)
                {
                    var roots = new List<GameObject>();

                    // Usar reflexion para obtener la lista de ropas
                    var clothingList = coserRopa.DetectedClothings;
                    if (clothingList != null)
                    {
                        foreach (var clothing in clothingList)
                        {
                            if (clothing != null && clothing.GameObject != null)
                            {
                                roots.Add(clothing.GameObject);
                            }
                        }
                    }

                    if (roots.Count > 0)
                    {
                        analisis.UpdateClothingRoots(roots);
                        Debug.Log($"[MRAnalisisColision NDMF] Actualizadas {roots.Count} raices de ropa desde MRCoserRopa");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MRAnalisisColision NDMF] Error obteniendo raices de ropa: {e.Message}");
            }
        }

        /// <summary>
        /// Desactiva componentes problematicos en raiz de ropa.
        /// </summary>
        private int DisableProblematicOnRoot(MRAnalisisColision analisis)
        {
            if (analisis?.ScanResult == null) return 0;

            int count = 0;

            foreach (var entry in analisis.ScanResult.GetProblematicOnClothingRoot())
            {
                if (entry.IsValid && entry.IsEnabled)
                {
                    if (entry.Component is MonoBehaviour mb)
                    {
                        mb.enabled = false;
                        count++;
                        Debug.Log($"[MRAnalisisColision NDMF] Desactivado {entry.ShortTypeName} en '{entry.GameObjectName}'");
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Desactiva componentes marcados por el usuario (UserDecision y Problematic).
        /// </summary>
        private int DisableUserSelectedComponents(MRAnalisisColision analisis)
        {
            if (analisis?.ScanResult == null) return 0;

            int count = 0;

            // Desactivar UserDecision marcados
            foreach (var entry in analisis.ScanResult.GetUserDecisionToDisable())
            {
                if (entry.IsValid && entry.IsEnabled)
                {
                    if (entry.Component is MonoBehaviour mb)
                    {
                        mb.enabled = false;
                        count++;
                        Debug.Log($"[MRAnalisisColision NDMF] Desactivado {entry.ShortTypeName} en '{entry.GameObjectName}' (UserDecision marcado)");
                    }
                }
            }

            // Desactivar Problematicos marcados por el usuario
            foreach (var entry in analisis.ScanResult.GetProblematicToDisable())
            {
                if (entry.IsValid && entry.IsEnabled)
                {
                    if (entry.Component is MonoBehaviour mb)
                    {
                        mb.enabled = false;
                        count++;
                        Debug.Log($"[MRAnalisisColision NDMF] Desactivado {entry.ShortTypeName} en '{entry.GameObjectName}' (Problematic marcado)");
                    }
                }
            }

            return count;
        }
    }
}
#endif
