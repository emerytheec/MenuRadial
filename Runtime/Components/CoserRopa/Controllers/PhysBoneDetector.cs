using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Controllers
{
    /// <summary>
    /// Detecta y preserva cadenas de PhysBones (VRCPhysBone) durante la fusión de huesos.
    ///
    /// VRCPhysBone es el componente de física de VRChat que controla:
    /// - Pelo
    /// - Faldas
    /// - Orejas
    /// - Colas
    /// - Cualquier hueso con física
    ///
    /// Durante la fusión, estos huesos NO deben ser eliminados, solo movidos.
    /// </summary>
    public class PhysBoneDetector
    {
        #region Constants

        /// <summary>
        /// Nombre del tipo VRCPhysBone (buscado por reflexión para evitar dependencia directa)
        /// </summary>
        private const string PHYSBONE_TYPE_NAME = "VRCPhysBone";
        private const string PHYSBONE_FULL_TYPE = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";

        /// <summary>
        /// Nombre del tipo VRCPhysBoneCollider
        /// </summary>
        private const string PHYSBONE_COLLIDER_TYPE_NAME = "VRCPhysBoneCollider";

        #endregion

        #region Private Fields

        private System.Type _physBoneType;
        private System.Type _physBoneColliderType;
        private bool _typesResolved = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Intenta resolver los tipos de VRChat SDK por reflexión.
        /// Esto evita dependencia directa del SDK.
        /// </summary>
        private void EnsureTypesResolved()
        {
            if (_typesResolved) return;

            // Buscar en todos los assemblies cargados
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_physBoneType == null)
                {
                    _physBoneType = assembly.GetType(PHYSBONE_FULL_TYPE);
                }

                if (_physBoneColliderType == null)
                {
                    _physBoneColliderType = assembly.GetType("VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider");
                }

                if (_physBoneType != null && _physBoneColliderType != null)
                    break;
            }

            _typesResolved = true;

            if (_physBoneType != null)
            {
                Debug.Log($"[PhysBoneDetector] VRCPhysBone encontrado: {_physBoneType.FullName}");
            }
            else
            {
                Debug.Log("[PhysBoneDetector] VRCPhysBone no encontrado (VRChat SDK no instalado o versión diferente)");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Detecta todos los huesos que son parte de una cadena PhysBone.
        /// Estos huesos deben preservarse durante la fusión.
        /// </summary>
        /// <param name="root">Transform raíz donde buscar</param>
        /// <returns>HashSet de transforms que son huesos de física</returns>
        public HashSet<Transform> DetectPhysBoneChains(Transform root)
        {
            var physBones = new HashSet<Transform>();

            if (root == null) return physBones;

            EnsureTypesResolved();

            if (_physBoneType == null)
            {
                // VRChat SDK no disponible, usar detección heurística
                return DetectPhysBonesHeuristic(root);
            }

            // Buscar todos los VRCPhysBone en la jerarquía
            var components = root.GetComponentsInChildren(_physBoneType, true);

            foreach (var component in components)
            {
                var physBoneComponent = component as Component;
                if (physBoneComponent == null) continue;

                // Obtener el rootTransform del PhysBone (puede ser null, usa el transform del componente)
                Transform physBoneRoot = GetPhysBoneRoot(physBoneComponent);

                if (physBoneRoot != null)
                {
                    // Agregar toda la cadena de huesos bajo este root
                    CollectBoneChain(physBoneRoot, physBones);
                    Debug.Log($"[PhysBoneDetector] Cadena PhysBone detectada en '{physBoneRoot.name}' ({CountChildren(physBoneRoot)} huesos)");
                }
            }

            Debug.Log($"[PhysBoneDetector] Total huesos de física detectados: {physBones.Count}");
            return physBones;
        }

        /// <summary>
        /// Verifica si un transform específico es parte de una cadena PhysBone.
        /// </summary>
        public bool IsPhysBone(Transform bone)
        {
            if (bone == null) return false;

            EnsureTypesResolved();

            if (_physBoneType == null)
            {
                // Usar detección heurística
                return IsPhysBoneHeuristic(bone);
            }

            // Verificar si tiene VRCPhysBone directamente
            if (bone.GetComponent(_physBoneType) != null)
                return true;

            // Verificar si algún ancestro tiene VRCPhysBone apuntando a este hueso o sus ancestros
            return HasPhysBoneAncestor(bone);
        }

        /// <summary>
        /// Obtiene información sobre PhysBones en una jerarquía.
        /// </summary>
        public PhysBoneInfo GetPhysBoneInfo(Transform root)
        {
            var info = new PhysBoneInfo();

            if (root == null) return info;

            EnsureTypesResolved();

            if (_physBoneType == null)
            {
                info.SDKAvailable = false;
                return info;
            }

            info.SDKAvailable = true;

            var components = root.GetComponentsInChildren(_physBoneType, true);
            info.PhysBoneCount = components.Length;

            foreach (var component in components)
            {
                var physBoneRoot = GetPhysBoneRoot(component as Component);
                if (physBoneRoot != null)
                {
                    info.AffectedBones += CountChildren(physBoneRoot) + 1;
                }
            }

            return info;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Obtiene el rootTransform de un VRCPhysBone usando reflexión.
        /// </summary>
        private Transform GetPhysBoneRoot(Component physBoneComponent)
        {
            if (physBoneComponent == null) return null;

            try
            {
                // Intentar obtener la propiedad rootTransform
                var rootTransformField = _physBoneType.GetField("rootTransform");
                if (rootTransformField != null)
                {
                    var rootTransform = rootTransformField.GetValue(physBoneComponent) as Transform;
                    return rootTransform ?? physBoneComponent.transform;
                }

                // Fallback: usar el transform del componente
                return physBoneComponent.transform;
            }
            catch
            {
                return physBoneComponent.transform;
            }
        }

        /// <summary>
        /// Recopila todos los huesos en una cadena (incluyendo hijos recursivamente).
        /// </summary>
        private void CollectBoneChain(Transform root, HashSet<Transform> result)
        {
            if (root == null) return;

            result.Add(root);

            foreach (Transform child in root)
            {
                CollectBoneChain(child, result);
            }
        }

        /// <summary>
        /// Verifica si un hueso tiene un ancestro con VRCPhysBone.
        /// </summary>
        private bool HasPhysBoneAncestor(Transform bone)
        {
            if (_physBoneType == null) return false;

            Transform current = bone.parent;
            while (current != null)
            {
                if (current.GetComponent(_physBoneType) != null)
                    return true;
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Cuenta hijos recursivamente.
        /// </summary>
        private int CountChildren(Transform parent)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                count++;
                count += CountChildren(child);
            }
            return count;
        }

        #endregion

        #region Heuristic Detection

        /// <summary>
        /// Patrones de nombres comunes para huesos de física.
        /// Usado cuando VRChat SDK no está disponible.
        /// </summary>
        private static readonly string[] PhysBoneNamePatterns = new[]
        {
            // Pelo
            "hair", "bangs", "fringe", "ponytail", "pigtail", "braid", "strand",
            // Orejas
            "ear", "kemonomimi",
            // Colas
            "tail",
            // Faldas/Vestidos
            "skirt", "dress", "cloth", "ribbon", "bow",
            // Accesorios
            "accessory", "pendant", "earring", "necklace", "chain",
            // Pechos (physics)
            "breast", "bust", "boob",
            // Genéricos
            "phys", "dynamic", "jiggle", "swing", "dangle"
        };

        /// <summary>
        /// Detección heurística de huesos de física por nombre.
        /// Usado cuando VRChat SDK no está disponible.
        /// </summary>
        private HashSet<Transform> DetectPhysBonesHeuristic(Transform root)
        {
            var physBones = new HashSet<Transform>();

            if (root == null) return physBones;

            var allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (var t in allTransforms)
            {
                if (IsPhysBoneHeuristic(t))
                {
                    // Agregar toda la cadena desde este punto
                    CollectBoneChain(t, physBones);
                }
            }

            return physBones;
        }

        /// <summary>
        /// Verifica si un hueso parece ser de física por su nombre.
        /// </summary>
        private bool IsPhysBoneHeuristic(Transform bone)
        {
            if (bone == null) return false;

            string nameLower = bone.name.ToLowerInvariant();

            foreach (var pattern in PhysBoneNamePatterns)
            {
                if (nameLower.Contains(pattern))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Información sobre PhysBones en una jerarquía.
    /// </summary>
    public struct PhysBoneInfo
    {
        /// <summary>
        /// Si el VRChat SDK está disponible
        /// </summary>
        public bool SDKAvailable;

        /// <summary>
        /// Número de componentes VRCPhysBone encontrados
        /// </summary>
        public int PhysBoneCount;

        /// <summary>
        /// Número total de huesos afectados por física
        /// </summary>
        public int AffectedBones;

        public override string ToString()
        {
            if (!SDKAvailable)
                return "VRChat SDK no disponible (detección heurística)";

            return $"PhysBones: {PhysBoneCount} componentes, {AffectedBones} huesos afectados";
        }
    }
}
