using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Datos del icono para un slot en el menú radial
    /// Separación entre icono de menú (funcional) e imagen logo (personalizada)
    /// </summary>
    [System.Serializable]
    public class SlotIconData
    {
        /// <summary>
        /// Icono funcional del menú (BSX_GM_*) basado en tipo y estado
        /// </summary>
        public Texture2D MenuIcon { get; set; }
        
        /// <summary>
        /// Imagen logo personalizada del usuario (fondo)
        /// </summary>
        public Texture2D LogoImage { get; set; }
        
        /// <summary>
        /// Estado actual del toggle (para iconos dinámicos)
        /// </summary>
        public bool ToggleState { get; set; }
        
        /// <summary>
        /// Si el icono puede cambiar dinámicamente
        /// </summary>
        public bool IsDynamic { get; set; }
        
        /// <summary>
        /// Constructor básico
        /// </summary>
        public SlotIconData()
        {
            ToggleState = false;
            IsDynamic = false;
        }
        
        /// <summary>
        /// Constructor con datos
        /// </summary>
        /// <param name="menuIcon">Icono funcional del menú</param>
        /// <param name="logoImage">Imagen logo personalizada</param>
        /// <param name="isDynamic">Si puede cambiar dinámicamente</param>
        public SlotIconData(Texture2D menuIcon, Texture2D logoImage, bool isDynamic = false)
        {
            MenuIcon = menuIcon;
            LogoImage = logoImage;
            IsDynamic = isDynamic;
            ToggleState = false;
        }
    }
    
    /// <summary>
    /// Manager para iconos dinámicos en el sistema de menú radial
    /// Gestiona el cambio automático de iconos basado en estado de preview
    /// </summary>
    public static class DynamicIconManager
    {
        
        /// <summary>
        /// Cache de iconos BSX_GM cargados
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _iconCache = 
            new System.Collections.Generic.Dictionary<string, Texture2D>();
        
        /// <summary>
        /// Cache de estados de iconos por slot
        /// Clave: instanceId_slotIndex
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, SlotIconData> _slotIconCache = 
            new System.Collections.Generic.Dictionary<string, SlotIconData>();
        
        
        
        /// <summary>
        /// Carga un icono BSX_GM desde Resources
        /// </summary>
        /// <param name="iconName">Nombre del icono (sin extensión)</param>
        /// <returns>Texture2D o null si no se encuentra</returns>
        public static Texture2D LoadBSXIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return null;
                
            // Verificar cache primero
            if (_iconCache.TryGetValue(iconName, out Texture2D cachedIcon))
                return cachedIcon;
            
            // Intentar cargar desde diferentes rutas posibles
            string[] possiblePaths = {
                $"iconos/{iconName}",
                $"Icons/{iconName}",
                $"BSX_Icons/{iconName}",
                iconName
            };
            
            foreach (string path in possiblePaths)
            {
                var icon = Resources.Load<Texture2D>(path);
                if (icon != null)
                {
                    // Guardar en cache
                    _iconCache[iconName] = icon;
                    
                    return icon;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Carga los iconos de toggle (normal y activado)
        /// </summary>
        /// <returns>Tupla con (iconoNormal, iconoActivado)</returns>
        public static (Texture2D normal, Texture2D active) LoadToggleIcons()
        {
            var normal = LoadBSXIcon("BSX_GM_Toggle");
            var active = LoadBSXIcon("BSX_GM_Toggle_on");
            
            if (normal == null)
            {
            }
            
            if (active == null)
            {
            }
            
            return (normal, active);
        }
        
        
        
        /// <summary>
        /// Genera clave única para un slot
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>Clave única</returns>
        private static string GetSlotKey(int menuInstanceId, int slotIndex)
        {
            return $"{menuInstanceId}_{slotIndex}";
        }
        
        /// <summary>
        /// Obtiene o crea datos de icono para un slot
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>Datos de icono del slot</returns>
        public static SlotIconData GetOrCreateSlotIconData(int menuInstanceId, int slotIndex)
        {
            string key = GetSlotKey(menuInstanceId, slotIndex);
            
            if (!_slotIconCache.TryGetValue(key, out SlotIconData iconData))
            {
                iconData = new SlotIconData();
                _slotIconCache[key] = iconData;
            }
            
            return iconData;
        }
        
        /// <summary>
        /// Actualiza el estado de toggle de un slot
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="newToggleState">Nuevo estado del toggle</param>
        public static void UpdateSlotToggleState(int menuInstanceId, int slotIndex, bool newToggleState)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            
            if (iconData.IsDynamic && iconData.ToggleState != newToggleState)
            {
                iconData.ToggleState = newToggleState;
                
                // Actualizar el icono del menú automáticamente
                var (normalIcon, activeIcon) = LoadToggleIcons();
                iconData.MenuIcon = newToggleState ? activeIcon : normalIcon;
                
            }
        }
        
        /// <summary>
        /// Configura un slot como dinámico (toggle)
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="logoImage">Imagen logo personalizada</param>
        public static void SetupDynamicSlot(int menuInstanceId, int slotIndex, Texture2D logoImage = null)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            
            iconData.IsDynamic = true;
            iconData.LogoImage = logoImage;
            iconData.ToggleState = false; // Empezar en estado OFF
            
            // Cargar icono inicial (OFF)
            var (normalIcon, _) = LoadToggleIcons();
            iconData.MenuIcon = normalIcon;
            
        }
        
        /// <summary>
        /// Configura un slot como estático
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="menuIcon">Icono funcional fijo</param>
        /// <param name="logoImage">Imagen logo personalizada</param>
        public static void SetupStaticSlot(int menuInstanceId, int slotIndex, Texture2D menuIcon, Texture2D logoImage = null)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            
            iconData.IsDynamic = false;
            iconData.MenuIcon = menuIcon;
            iconData.LogoImage = logoImage;
            iconData.ToggleState = false;
        }
        
        
        
        /// <summary>
        /// Obtiene el icono actual de menú para un slot
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>Texture2D del icono actual</returns>
        public static Texture2D GetCurrentMenuIcon(int menuInstanceId, int slotIndex)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            return iconData.MenuIcon;
        }
        
        /// <summary>
        /// Obtiene la imagen logo para un slot
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>Texture2D de la imagen logo</returns>
        public static Texture2D GetSlotLogoImage(int menuInstanceId, int slotIndex)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            return iconData.LogoImage;
        }
        
        /// <summary>
        /// Obtiene ambos iconos para un slot (menú y logo)
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>Tupla con (iconoMenú, imagenLogo)</returns>
        public static (Texture2D menuIcon, Texture2D logoImage) GetSlotIcons(int menuInstanceId, int slotIndex)
        {
            var iconData = GetOrCreateSlotIconData(menuInstanceId, slotIndex);
            return (iconData.MenuIcon, iconData.LogoImage);
        }
        
        
        
        /// <summary>
        /// Limpia el cache de un menú específico
        /// </summary>
        /// <param name="menuInstanceId">ID de instancia del MRMenuControl</param>
        public static void ClearMenuCache(int menuInstanceId)
        {
            var keysToRemove = new System.Collections.Generic.List<string>();
            
            foreach (var key in _slotIconCache.Keys)
            {
                if (key.StartsWith($"{menuInstanceId}_"))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _slotIconCache.Remove(key);
            }
            
        }
        
        /// <summary>
        /// Limpia completamente el cache
        /// </summary>
        public static void ClearAllCache()
        {
            _iconCache.Clear();
            _slotIconCache.Clear();
        }
        
        
        
        
        /// <summary>
        /// Limpieza automática al cambiar de escena
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            ClearAllCache();
        }
        
    }
}
