using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Preview;
using Bender_Dios.MenuRadial.Components.Radial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Manejador de interacciones del menú radial.
    /// Gestiona clics de botones, activación de previews y acciones específicas por tipo de animación.
    /// </summary>
    public class MRMenuInteractionHandler
    {
        
        private MRMenuControl ownerMenu;
        private MRSlotManager slotManager;
        private MRNavigationManager navigationManager;
        

        
        /// <summary>
        /// Inicializa el manejador de interacciones
        /// </summary>
        /// <param name="owner">Menú propietario</param>
        /// <param name="slotManager">Gestor de slots</param>
        /// <param name="navigationManager">Gestor de navegación</param>
        public MRMenuInteractionHandler(MRMenuControl owner, MRSlotManager slotManager, MRNavigationManager navigationManager)
        {
            ownerMenu = owner;
            this.slotManager = slotManager;
            this.navigationManager = navigationManager;
        }
        

        
        /// <summary>
        /// Maneja el clic en un botón del menú radial.
        /// Integración con PreviewManager para activación automática de previews.
        /// </summary>
        /// <param name="buttonIndex">Índice del botón clickeado (-1 = Back, 0+ = slots)</param>
        public void HandleMenuButtonClick(int buttonIndex)
        {
            if (buttonIndex == -1)
            {
                // Clic en botón Back
                navigationManager.NavigateToParent();
                return;
            }

            if (buttonIndex < 0 || buttonIndex >= slotManager.SlotCount)
                return;

            var slot = slotManager.GetSlot(buttonIndex);
            if (!slot.isValid)
                return;

            // Obtener el tipo de animación del slot
            var animationType = slot.GetAnimationType();

            // Usar cache de componentes del slot
            var previewable = slot.CachedPreviewable;

            switch (animationType)
            {
                case AnimationType.SubMenu:
                    HandleSubMenuClick(buttonIndex, slot);
                    break;

                case AnimationType.Linear:
                    HandleLinearClick(buttonIndex, slot, previewable);
                    break;

                case AnimationType.OnOff:
                case AnimationType.AB:
                    HandleToggleClick(buttonIndex, slot, previewable);
                    break;
            }
        }
        

        
        /// <summary>
        /// Maneja el clic en un slot de tipo SubMenu
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        private void HandleSubMenuClick(int slotIndex, MRAnimationSlot slot)
        {
            var subMenu = navigationManager.GetSubMenuFromSlot(slotIndex);
            if (subMenu != null)
            {
                navigationManager.NavigateToSubMenu(subMenu);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Maneja el clic en un slot de tipo Linear
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        /// <param name="previewable">Componente IPreviewable si existe</param>
        private void HandleLinearClick(int slotIndex, MRAnimationSlot slot, IPreviewable previewable)
        {
            // Activar preview si está disponible
            if (previewable != null)
            {
                PreviewManager.ActivatePreview(previewable, ownerMenu);
                
                // Caso especial para MRIluminacionRadial: automático
                if (previewable.GetPreviewType() == PreviewType.Illumination)
                {
                }
                else
                {
                    // Para otros tipos lineales, abrir interfaz circular
                    OpenCircularLinearInterface(slotIndex, slot);
                }
            }
            else
            {
                // Fallback: comportamiento original
                OpenCircularLinearInterface(slotIndex, slot);
            }
        }
        
        /// <summary>
        /// Maneja el clic en un slot de tipo Toggle (OnOff o AB)
        /// CORREGIDO: Ahora siempre ejecuta el toggle real, no solo activa preview
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        /// <param name="previewable">Componente IPreviewable si existe</param>
        private void HandleToggleClick(int slotIndex, MRAnimationSlot slot, IPreviewable previewable)
        {
            // Siempre ejecutar el toggle real (alternar entre frames)
            ExecuteToggleAnimation(slotIndex, slot);
        }
        

        
        /// <summary>
        /// Abre la interfaz circular para un slot con animación linear
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        private void OpenCircularLinearInterface(int slotIndex, MRAnimationSlot slot)
        {
            if (!slot.isValid || slot.targetObject == null)
            {
                return;
            }

            // Usar cache de componentes del slot
            var radialMenu = slot.CachedRadialMenu;
            if (radialMenu == null)
            {
                return;
            }
            
            // Verificar que sea de tipo Linear
            if (radialMenu.AnimationType != Core.Common.AnimationType.Linear)
            {
                return;
            }
            
            // Verificar que tenga suficientes frames
            if (radialMenu.FrameCount < 3)
            {
                return;
            }
            
#if UNITY_EDITOR
            // Usar reflection para evitar dependencias directas del namespace Editor
            var windowType = System.Type.GetType("Bender_Dios.MenuRadial.Components.Menu.Editor.CircularLinearMenuWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                // Intentar sin especificar el assembly
                windowType = System.Type.GetType("Bender_Dios.MenuRadial.Components.Menu.Editor.CircularLinearMenuWindow");
            }
            
            if (windowType != null)
            {
                var openMethod = windowType.GetMethod("OpenCircularMenu", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (openMethod != null)
                {
                    openMethod.Invoke(null, new object[] { radialMenu, ownerMenu, slot.slotName });
                    return;
                }
            }
            
            // Si no se pudo abrir la ventana, log informativo
#else
#endif
        }
        

        
        /// <summary>
        /// Ejecuta la animación toggle para slots ON/OFF o A/B
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        private void ExecuteToggleAnimation(int slotIndex, MRAnimationSlot slot)
        {
            if (!slot.isValid || slot.targetObject == null)
                return;

            // Usar cache de componentes del slot
            var radialMenu = slot.CachedRadialMenu;
            if (radialMenu == null)
                return;

            var animationType = radialMenu.AnimationType;

            if (animationType == Core.Common.AnimationType.OnOff)
            {
                // Toggle ON/OFF: alternar entre 0 y 1
                int newIndex = radialMenu.ActiveFrameIndex == 0 ? 1 : 0;
                radialMenu.ActiveFrameIndex = newIndex;
                radialMenu.ApplyCurrentFrame();
            }
            else if (animationType == Core.Common.AnimationType.AB)
            {
                // Toggle A/B: alternar entre frame 0 y 1
                int newIndex = radialMenu.ActiveFrameIndex == 0 ? 1 : 0;
                radialMenu.ActiveFrameIndex = newIndex;
                radialMenu.ApplyCurrentFrame();
            }

            // Actualizar icono después del toggle
            UpdateSlotIconForToggle(slotIndex, slot);
        }
        

        
        /// <summary>
        /// Actualiza el icono dinámico para slots de tipo toggle.
        /// Integración con DynamicIconManager.
        /// </summary>
        /// <param name="slotIndex">Índice del slot</param>
        /// <param name="slot">Datos del slot</param>
        private void UpdateSlotIconForToggle(int slotIndex, MRAnimationSlot slot)
        {
            // Usar cache de componentes del slot
            var radialMenu = slot.CachedRadialMenu;
            if (radialMenu == null)
                return;
            
            // Determinar el nuevo estado del toggle
            bool newToggleState = radialMenu.ActiveFrameIndex == 1;
            
            // Configurar el slot como dinámico si no lo está ya
            DynamicIconManager.SetupDynamicSlot(ownerMenu.GetInstanceID(), slotIndex, slot.iconImage);
            
            // Actualizar el estado del toggle
            DynamicIconManager.UpdateSlotToggleState(ownerMenu.GetInstanceID(), slotIndex, newToggleState);
        }
        

        
        /// <summary>
        /// Verifica si un slot puede abrir interfaz circular
        /// </summary>
        /// <param name="slotIndex">Índice del slot a verificar</param>
        /// <returns>True si puede abrir interfaz circular</returns>
        public bool CanOpenCircularInterface(int slotIndex)
        {
            var slot = slotManager.GetSlot(slotIndex);
            return slot?.CanOpenCircularInterface() ?? false;
        }
        
        /// <summary>
        /// Verifica si un slot puede ejecutar toggle
        /// </summary>
        /// <param name="slotIndex">Índice del slot a verificar</param>
        /// <returns>True si puede ejecutar toggle</returns>
        public bool CanExecuteToggle(int slotIndex)
        {
            var slot = slotManager.GetSlot(slotIndex);
            return slot?.CanExecuteToggle() ?? false;
        }
        
        
    }
}
