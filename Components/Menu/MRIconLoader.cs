#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Sistema de carga de iconos para el menú radial
    /// Carga automáticamente los iconos desde la carpeta iconos/
    /// </summary>
    public static class MRIconLoader
    {
        private static Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
        private static readonly string IconsPath = "Assets/Bender_Dios/MenuRadial/Components/Menu/Resources/";
        
        /// <summary>
        /// Carga un icono por nombre desde la carpeta de iconos
        /// </summary>
        public static Texture2D LoadIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;
                
            // Verificar cache primero
            if (_iconCache.ContainsKey(iconName))
                return _iconCache[iconName];
            
            // Construir ruta completa
            string iconPath = Path.Combine(IconsPath, iconName + ".png");
            
            // Cargar desde AssetDatabase
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            
            // Guardar en cache (incluso si es null para evitar búsquedas repetidas)
            _iconCache[iconName] = icon;
            
            if (icon == null)
            {
            }
            
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
            
            // Imagen logo (fondo) - asignada por el usuario
            Texture2D logoImage = slot?.iconImage;
            
            return (menuIcon, logoImage);
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
