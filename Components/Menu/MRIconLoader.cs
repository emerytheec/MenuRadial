#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Sistema de carga de iconos para el menú radial.
    /// Carga iconos desde la carpeta Resources.
    /// </summary>
    public static class MRIconLoader
    {
        private static Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Carga un icono por nombre desde la carpeta Resources.
        /// Usa Resources.Load para compatibilidad con instalación via VPM.
        /// </summary>
        public static Texture2D LoadIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;

            // Verificar cache primero
            if (_iconCache.TryGetValue(iconName, out var cachedIcon))
                return cachedIcon;

            // Cargar usando Resources.Load (funciona sin importar dónde esté el paquete)
            Texture2D icon = Resources.Load<Texture2D>(iconName);

            // Si no se encuentra, intentar con AssetDatabase como fallback (solo editor)
            if (icon == null)
            {
                // Buscar en todas las carpetas Resources del proyecto
                string[] guids = AssetDatabase.FindAssets($"{iconName} t:Texture2D");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Resources") && path.EndsWith($"{iconName}.png"))
                    {
                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (icon != null) break;
                    }
                }
            }

            // Guardar en cache (incluso si es null para evitar búsquedas repetidas)
            _iconCache[iconName] = icon;

            return icon;
        }
        
        /// <summary>
        /// Carga el icono del botón Back
        /// </summary>
        public static Texture2D GetBackIcon()
        {
            return LoadIcon("BSX_GM_Back");
        }
        
        /// <summary>
        /// Obtiene el icono correspondiente según el tipo de animación
        /// </summary>
        public static Texture2D GetIconForAnimationType(AnimationType animationType)
        {
            return animationType switch
            {
                AnimationType.OnOff => LoadIcon("BSX_GM_Toggle"),
                AnimationType.AB => LoadIcon("BSX_GM_Toggle"),
                AnimationType.Linear => LoadIcon("BSX_GM_Radial"),
                AnimationType.SubMenu => LoadIcon("BSX_GM_Option"),
                _ => LoadIcon("BSX_GM_Default")
            };
        }

        /// <summary>
        /// Obtiene el icono para un slot específico basado en su componente
        /// SOLO devuelve el icono del menú (BSX_GM_*), NO la imagen logo personalizada
        /// </summary>
        public static Texture2D GetIconForSlot(MRAnimationSlot slot)
        {
            if (slot == null)
                return LoadIcon("BSX_GM_Default");

            // Obtener tipo de animación incluso si el nombre del slot está vacío
            // (el slot puede tener un targetObject válido con IAnimationProvider)
            if (slot.targetObject != null)
            {
                AnimationType animationType = slot.GetAnimationType();
                if (animationType != AnimationType.None)
                {
                    return GetIconForAnimationType(animationType);
                }
            }

            // Fallback para slots sin targetObject o sin IAnimationProvider
            return LoadIcon("BSX_GM_Default");
        }
        
        /// <summary>
        /// Obtiene ambos iconos para un slot: el del menú (primer plano) y la imagen logo (fondo)
        /// </summary>
        public static (Texture2D menuIcon, Texture2D logoImage) GetIconsForSlot(MRAnimationSlot slot)
        {
            // Icono del menú (primer plano) - basado en tipo de animación
            Texture2D menuIcon = GetIconForSlot(slot);

            // Imagen logo (fondo) - asignada por el usuario, con fallback a categoría
            Texture2D logoImage = slot?.iconImage;
            if (logoImage == null && slot != null)
                logoImage = GetIconForSlotName(slot.slotName);

            return (menuIcon, logoImage);
        }
        
        /// <summary>
        /// Obtiene el icono de categoría para un slot basado en su nombre.
        /// Mapea nombres de slot a iconos semánticos: ropa, peluca, color, luz.
        /// </summary>
        public static Texture2D GetIconForSlotName(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
                return null;

            string lower = slotName.ToLowerInvariant();

            if (lower == "outfits" || lower == "ropa")
                return LoadIcon("ropa");

            if (lower == "pelucas" || lower == "wigs")
                return LoadIcon("peluca");

            if (lower.Contains("color") || lower.Contains("material"))
                return LoadIcon("color");

            if (lower.Contains("iluminacion") || lower.Contains("illumination") || lower.Contains("luz"))
                return LoadIcon("luz");

            return null;
        }

        /// <summary>
        /// Limpia el cache de iconos (útil para testing)
        /// </summary>
        public static void ClearCache()
        {
            _iconCache.Clear();
        }

    }
}
#endif
