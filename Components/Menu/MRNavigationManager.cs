using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Gestor de navegación entre menús y submenús.
    /// Maneja el stack de navegación y las relaciones padre-hijo entre menús.
    /// </summary>
    public class MRNavigationManager
    {
        
        /// <summary>
        /// Stack de navegación para mantener la jerarquía
        /// </summary>
        private static readonly List<MRMenuControl> navigationStack = new List<MRMenuControl>();
        
        /// <summary>
        /// Menú actualmente seleccionado en el inspector
        /// </summary>
        private static MRMenuControl currentSelectedMenu;
        

        
        /// <summary>
        /// Evento que se dispara cuando cambia la navegación
        /// </summary>
        public static event System.Action<MRMenuControl> OnNavigationChanged;
        

        
        /// <summary>
        /// Menú actualmente seleccionado para navegación
        /// </summary>
        public static MRMenuControl CurrentSelectedMenu => currentSelectedMenu;
        

        
        private MRMenuControl ownerMenu;
        private MRMenuControl parentMenu;
        

        
        /// <summary>
        /// Inicializa el gestor de navegación para un menú específico
        /// </summary>
        /// <param name="owner">El menú propietario de este gestor</param>
        public MRNavigationManager(MRMenuControl owner)
        {
            ownerMenu = owner;
        }
        

        
        /// <summary>
        /// Menú padre de este submenú (null si es el menú raíz)
        /// </summary>
        public MRMenuControl ParentMenu 
        { 
            get => parentMenu; 
            set => parentMenu = value; 
        }
        
        /// <summary>
        /// Si este menú tiene un menú padre
        /// </summary>
        public bool HasParent => parentMenu != null;
        
        /// <summary>
        /// Ruta de navegación desde el menú raíz hasta este menú
        /// </summary>
        public string NavigationPath
        {
            get
            {
                if (parentMenu == null)
                    return ownerMenu.name;

                // CORREGIDO: Usar GetNavigationManager() en lugar de GetComponent
                // MRNavigationManager no es un MonoBehaviour, es una clase C# normal
                var parentNavigation = parentMenu.GetNavigationManager();
                if (parentNavigation != null)
                    return $"{parentNavigation.NavigationPath} → {ownerMenu.name}";

                return $"{parentMenu.name} → {ownerMenu.name}";
            }
        }
        

        
        /// <summary>
        /// Navega a un submenú específico
        /// </summary>
        /// <param name="subMenu">El submenú al que navegar</param>
        public void NavigateToSubMenu(MRMenuControl subMenu)
        {
            if (subMenu == null)
            {
                return;
            }
            
            // Establecer relación padre-hijo
            var subMenuNavigation = GetOrCreateNavigationManager(subMenu);
            subMenuNavigation.parentMenu = ownerMenu;
            
            // Añadir al stack de navegación
            if (!navigationStack.Contains(ownerMenu))
                navigationStack.Add(ownerMenu);
            
            // Cambiar selección en Unity Editor
            ChangeEditorSelection(subMenu);
            
        }
        
        /// <summary>
        /// Navega de vuelta al menú padre
        /// </summary>
        public void NavigateToParent()
        {
            if (parentMenu == null)
            {
                return;
            }
            
            // Remover del stack si es necesario
            if (navigationStack.Contains(ownerMenu))
                navigationStack.Remove(ownerMenu);
            
            // Cambiar selección al menú padre
            ChangeEditorSelection(parentMenu);
            
        }
        
        /// <summary>
        /// Navega al menú raíz de la jerarquía
        /// </summary>
        public void NavigateToRoot()
        {
            var rootMenu = GetRootMenu();
            if (rootMenu != null && rootMenu != ownerMenu)
            {
                // Limpiar stack de navegación
                navigationStack.Clear();
                
                ChangeEditorSelection(rootMenu);
            }
        }
        
        /// <summary>
        /// Obtiene el menú raíz de la jerarquía
        /// </summary>
        /// <returns>El menú raíz</returns>
        public MRMenuControl GetRootMenu()
        {
            var current = ownerMenu;
            var currentNavigation = this;
            
            while (currentNavigation.parentMenu != null)
            {
                current = currentNavigation.parentMenu;
                currentNavigation = GetOrCreateNavigationManager(current);
            }
            
            return current;
        }
        

        
        /// <summary>
        /// Obtiene el slot correspondiente a un submenú específico
        /// </summary>
        /// <param name="subMenu">El submenú a buscar</param>
        /// <returns>El índice del slot, o -1 si no se encuentra</returns>
        public int GetSlotIndexForSubMenu(MRMenuControl subMenu)
        {
            var slotManager = ownerMenu.GetSlotManager();
            if (slotManager == null) return -1;
            
            for (int i = 0; i < slotManager.SlotCount; i++)
            {
                var slot = slotManager.GetSlot(i);
                if (slot?.targetObject != null && slot.CachedControlMenu == subMenu)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Obtiene el submenú de un slot específico
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <returns>El componente MRMenuControl del slot, o null</returns>
        public MRMenuControl GetSubMenuFromSlot(int slotIndex)
        {
            var slotManager = ownerMenu.GetSlotManager();
            if (slotManager == null) return null;

            var slot = slotManager.GetSlot(slotIndex);
            if (slot?.targetObject == null)
                return null;

            return slot.CachedControlMenu;
        }
        

        
        /// <summary>
        /// Cambia la selección del objeto en Unity Editor
        /// </summary>
        /// <param name="targetMenu">El menú objetivo</param>
        private void ChangeEditorSelection(MRMenuControl targetMenu)
        {
#if UNITY_EDITOR
            // Cambiar selección en Unity
            UnityEditor.Selection.activeGameObject = targetMenu.gameObject;
            
            // Actualizar el menú seleccionado actual
            currentSelectedMenu = targetMenu;
            
            // Disparar evento de navegación
            OnNavigationChanged?.Invoke(targetMenu);
            
            // Forzar repintado del inspector
            UnityEditor.EditorUtility.SetDirty(targetMenu);
#endif
        }
        
        /// <summary>
        /// Obtiene o crea un NavigationManager para un menú específico
        /// </summary>
        /// <param name="menu">El menú para el que obtener el NavigationManager</param>
        /// <returns>El NavigationManager del menú</returns>
        private MRNavigationManager GetOrCreateNavigationManager(MRMenuControl menu)
        {
            // En este caso, como MRNavigationManager es un componente separado,
            // cada MRMenuControl debería tener su propio NavigationManager interno
            // Esto se implementaría en el MRMenuControl refactorizado
            return menu.GetNavigationManager();
        }
        

        
        
        
        /// <summary>
        /// Verifica si hay ciclos en la jerarquía de navegación
        /// </summary>
        /// <param name="startMenu">Menú desde donde comenzar la verificación</param>
        /// <returns>True si se detecta un ciclo</returns>
        public static bool DetectNavigationCycle(MRMenuControl startMenu)
        {
            if (startMenu == null) return false;
            
            var visited = new HashSet<MRMenuControl>();
            var current = startMenu;
            
            while (current != null)
            {
                if (visited.Contains(current))
                {
                    return true;
                }
                
                visited.Add(current);
                var navigationManager = current.GetNavigationManager();
                current = navigationManager?.parentMenu;
            }
            
            return false;
        }
        

    }
}
