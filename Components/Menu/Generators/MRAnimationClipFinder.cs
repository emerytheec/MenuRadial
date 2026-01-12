using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Buscador de AnimationClips por nombre y sufijo.
    /// Responsabilidad: Localizar archivos de animación generados.
    /// </summary>
    public class MRAnimationClipFinder
    {
        private readonly string[] _searchPaths;

        /// <summary>
        /// Constructor con ruta prioritaria de búsqueda.
        /// La ruta proporcionada se agrega al inicio de las rutas por defecto.
        /// </summary>
        /// <param name="priorityPath">Ruta prioritaria donde buscar primero</param>
        public MRAnimationClipFinder(string priorityPath)
        {
            var paths = new List<string>();

            // Agregar ruta prioritaria al inicio si es válida
            if (!string.IsNullOrEmpty(priorityPath))
            {
                paths.Add(priorityPath.TrimEnd('/'));
            }

            // Agregar rutas por defecto (evitando duplicados)
            foreach (var defaultPath in GetDefaultSearchPaths())
            {
                if (!paths.Contains(defaultPath))
                {
                    paths.Add(defaultPath);
                }
            }

            _searchPaths = paths.ToArray();
        }

        /// <summary>
        /// Constructor con rutas de búsqueda personalizadas
        /// </summary>
        /// <param name="searchPaths">Rutas donde buscar animaciones</param>
        public MRAnimationClipFinder(string[] searchPaths = null)
        {
            _searchPaths = searchPaths ?? GetDefaultSearchPaths();
        }

        /// <summary>
        /// Obtiene las rutas de búsqueda por defecto
        /// </summary>
        private static string[] GetDefaultSearchPaths()
        {
            return new string[]
            {
                MRConstants.VRCHAT_OUTPUT_PATH.TrimEnd('/'),
                "Assets/Bender_Dios/salida",
                "Assets/Bender_Dios/Generated",
                "Assets/Animations",
                "Assets"
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// Busca clips de animación para un slot según su tipo
        /// </summary>
        /// <param name="slot">Slot de animación</param>
        /// <param name="provider">Proveedor de animación</param>
        /// <param name="animationType">Tipo de animación</param>
        /// <returns>Lista de clips encontrados</returns>
        public List<AnimationClip> FindClipsForSlot(MRAnimationSlot slot, IAnimationProvider provider, AnimationType animationType)
        {
            var clips = new List<AnimationClip>();

            if (provider == null)
            {
                Debug.Log($"[MRAnimationClipFinder] FindClipsForSlot: provider es null para slot '{slot.slotName}'");
                return clips;
            }

            string animName = provider.AnimationName;
            if (string.IsNullOrEmpty(animName))
                animName = slot.slotName;

            Debug.Log($"[MRAnimationClipFinder] Buscando animaciones para '{slot.slotName}' (animName='{animName}', tipo={animationType})");

            switch (animationType)
            {
                case AnimationType.OnOff:
                    var onClip = FindClipWithVariants(animName, MRAnimationSuffixes.ON_VARIANTS);
                    var offClip = FindClipWithVariants(animName, MRAnimationSuffixes.OFF_VARIANTS);
                    if (onClip != null) clips.Add(onClip);
                    if (offClip != null) clips.Add(offClip);
                    break;

                case AnimationType.AB:
                    var aClip = FindClipWithVariants(animName, MRAnimationSuffixes.A_VARIANTS);
                    var bClip = FindClipWithVariants(animName, MRAnimationSuffixes.B_VARIANTS);
                    if (aClip != null) clips.Add(aClip);
                    if (bClip != null) clips.Add(bClip);
                    break;

                case AnimationType.Linear:
                    var linClip = FindClipWithVariants(animName, MRAnimationSuffixes.LINEAR_VARIANTS);
                    if (linClip != null) clips.Add(linClip);
                    break;
            }

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[MRAnimationClipFinder] No se encontraron animaciones para slot '{slot.slotName}' (buscando: '{animName}', tipo: {animationType})");
            }
            else
            {
                Debug.Log($"[MRAnimationClipFinder] Encontrados {clips.Count} clip(s) para '{slot.slotName}': {string.Join(", ", clips.ConvertAll(c => c.name))}");
            }

            return clips;
        }

        /// <summary>
        /// Busca un AnimationClip probando múltiples variantes de sufijo
        /// </summary>
        /// <param name="baseName">Nombre base de la animación</param>
        /// <param name="suffixes">Variantes de sufijo a probar</param>
        /// <returns>Clip encontrado o null</returns>
        public AnimationClip FindClipWithVariants(string baseName, string[] suffixes)
        {
            foreach (var suffix in suffixes)
            {
                var clip = FindClipByName($"{baseName}{suffix}");
                if (clip != null)
                    return clip;
            }
            return null;
        }

        /// <summary>
        /// Busca un AnimationClip por nombre exacto en las rutas de búsqueda
        /// </summary>
        /// <param name="clipName">Nombre del clip a buscar</param>
        /// <returns>Clip encontrado o null</returns>
        public AnimationClip FindClipByName(string clipName)
        {
            foreach (var searchPath in _searchPaths)
            {
                if (!AssetDatabase.IsValidFolder(searchPath))
                    continue;

                string[] allGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { searchPath });

                foreach (var guid in allGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                    if (clip != null)
                    {
                        // Comparar exacto o case-insensitive
                        if (clip.name == clipName || string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                            return clip;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Verifica si existen los clips necesarios para un tipo de animación
        /// </summary>
        /// <param name="animName">Nombre base de la animación</param>
        /// <param name="animationType">Tipo de animación</param>
        /// <returns>True si todos los clips necesarios existen</returns>
        public bool HasRequiredClips(string animName, AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.OnOff:
                    return FindClipWithVariants(animName, MRAnimationSuffixes.ON_VARIANTS) != null &&
                           FindClipWithVariants(animName, MRAnimationSuffixes.OFF_VARIANTS) != null;

                case AnimationType.AB:
                    return FindClipWithVariants(animName, MRAnimationSuffixes.A_VARIANTS) != null &&
                           FindClipWithVariants(animName, MRAnimationSuffixes.B_VARIANTS) != null;

                case AnimationType.Linear:
                    return FindClipWithVariants(animName, MRAnimationSuffixes.LINEAR_VARIANTS) != null;

                default:
                    return false;
            }
        }
#endif
    }
}
