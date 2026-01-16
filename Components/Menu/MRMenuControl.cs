using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Core.Preview;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.MenuRadial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Componente MR Menú Control (antes MRControlMenu)
    /// Orquesta todo el sistema MR y genera archivos VRChat
    /// </summary>
    [AddComponentMenu("MR/MR Menú Control")]
    public class MRMenuControl : MonoBehaviour, IAnimationProvider
    {
        
        [SerializeField] private List<MRAnimationSlot> animationSlots = new List<MRAnimationSlot>();
        [SerializeField] private bool _autoUpdatePaths = true;
        
        [Header("⚙️ Configuración")]
        [SerializeField] private MRVRChatConfig vrchatConfig = new MRVRChatConfig();

        private MRSlotManager slotManager;
        private MRNavigationManager navigationManager;
        private MRSubMenuManager subMenuManager;
        private MRMenuInteractionHandler interactionHandler;
#if UNITY_EDITOR
        private MRVRChatFileGenerator fileGenerator;
#endif
        

        
        // Control de inicialización para evitar recreación redundante
        private bool _managersInitialized = false;
        
        // Cache de validación para evitar operaciones repetidas
        private bool _validationCacheValid = false;
        private System.DateTime _lastValidationTime;
        
        // Detección de cambios en slots para validación condicional
        private int _lastSlotHashCode = 0;
        

        
        /// <summary>
        /// Lista de slots de animación (delegada al SlotManager)
        /// </summary>
        public List<MRAnimationSlot> AnimationSlots => animationSlots;
        
        /// <summary>
        /// Configuración VRChat
        /// </summary>
        public MRVRChatConfig VRChatConfig => vrchatConfig;
        
        /// <summary>
        /// Auto-actualizar rutas
        /// </summary>
        public bool AutoUpdatePaths { get => _autoUpdatePaths; set => _autoUpdatePaths = value; }

        /// <summary>
        /// Obtiene la ruta de salida desde MRMenuRadial padre.
        /// </summary>
        public string OutputPath
        {
            get
            {
                var menuRadial = GetComponentInParent<MRMenuRadial>();
                return menuRadial != null ? menuRadial.OutputPath : MRConstants.ANIMATION_OUTPUT_PATH;
            }
        }
        
        /// <summary>
        /// Número de slots
        /// </summary>
        public int SlotCount => slotManager?.SlotCount ?? 0;
        
        /// <summary>
        /// Si todos los slots son válidos
        /// </summary>
        public bool AllSlotsValid => slotManager?.AllSlotsValid ?? false;
        

        
        private void Awake()
        {
            InitializeManagers();
        }
        
        private void OnValidate()
        {
            // Inicializar gestores si no existen (puede pasar en el editor)
            if (slotManager == null)
            {
                InitializeManagers();
            }
            
            // OPTIMIZACIÓN 3: Validación condicional con detección de cambios
            if (slotManager != null)
            {
                slotManager.UpdateSlots(animationSlots);

                // Sincronizar nombres entre slots y componentes
                SyncSlotNamesFromAnimations();

                // Solo validar si los slots han cambiado
                int currentSlotHashCode = CalculateSlotHashCode();
                if (currentSlotHashCode != _lastSlotHashCode)
                {
                    ValidateWithCache();
                    _lastSlotHashCode = currentSlotHashCode;
                }
            }
            
            // Marcar como dirty para guardar cambios
            if (Application.isPlaying == false)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        
        /// <summary>
        /// OPTIMIZACIÓN 2: Cleanup explícito para prevenir memory leaks en Editor
        /// </summary>
        private void OnDestroy()
        {
            // Cleanup de gestores para prevenir referencias colgantes
            if (slotManager != null)
            {
                // Si los gestores implementan IDisposable en el futuro:
                slotManager = null;
            }
            
            if (navigationManager != null)
            {
                navigationManager = null;
            }
            
            if (subMenuManager != null)
            {
                subMenuManager = null;
            }
            
            if (interactionHandler != null)
            {
                interactionHandler = null;
            }

#if UNITY_EDITOR
            if (fileGenerator != null)
            {
                fileGenerator = null;
            }
#endif

            // Resetear flags de estado
            _managersInitialized = false;
            InvalidateValidationCache();
        }
        

        
        /// <summary>
        /// Inicializa todos los gestores (optimizado para evitar recreación redundante)
        /// </summary>
        private void InitializeManagers()
        {
            // OPTIMIZACIÓN 1: Evitar inicialización redundante
            bool managersReady = _managersInitialized && slotManager != null && navigationManager != null &&
                subMenuManager != null && interactionHandler != null;
#if UNITY_EDITOR
            managersReady = managersReady && fileGenerator != null;
#endif
            if (managersReady)
            {
                return; // Gestores ya inicializados correctamente
            }

            // Crear gestores siguiendo patrón de composición
            slotManager = new MRSlotManager(animationSlots);
            navigationManager = new MRNavigationManager(this);
            subMenuManager = new MRSubMenuManager(this, slotManager);
            interactionHandler = new MRMenuInteractionHandler(this, slotManager, navigationManager);
#if UNITY_EDITOR
            fileGenerator = new MRVRChatFileGenerator(this, slotManager, vrchatConfig, OutputPath);
#endif

            _managersInitialized = true;

            // Invalidar cache de validación después de reinicializar
            InvalidateValidationCache();
        }
        
        /// <summary>
        /// Asegura que los gestores estén inicializados (para uso en el editor)
        /// </summary>
        private void EnsureManagersInitialized()
        {
            bool needsInit = slotManager == null || navigationManager == null || subMenuManager == null ||
                interactionHandler == null;
#if UNITY_EDITOR
            needsInit = needsInit || fileGenerator == null;
#endif
            if (needsInit)
            {
                InitializeManagers();
            }
        }
        

        
        /// <summary>
        /// Añade un nuevo slot
        /// </summary>
        public void AddSlot()
        {
            EnsureManagersInitialized();
            slotManager?.AddSlot();
        }
        
        /// <summary>
        /// Remueve un slot por índice
        /// </summary>
        public void RemoveSlot(int index)
        {
            EnsureManagersInitialized();
            slotManager?.RemoveSlot(index);
        }
        
        /// <summary>
        /// Mueve un slot
        /// </summary>
        public void MoveSlot(int fromIndex, int toIndex)
        {
            EnsureManagersInitialized();
            slotManager?.MoveSlot(fromIndex, toIndex);
        }
        
        /// <summary>
        /// Crea un nuevo submenú
        /// </summary>
        public void CreateSubMenu()
        {
            EnsureManagersInitialized();
            subMenuManager?.CreateSubMenu();
        }

        /// <summary>
        /// Crea un nuevo MRUnificarObjetos como hijo y lo añade a un slot
        /// </summary>
        public void CreateRadialMenu()
        {
            EnsureManagersInitialized();
            subMenuManager?.CreateRadialMenu();
        }

        /// <summary>
        /// Crea un nuevo MRIluminacionRadial como hijo y lo añade a un slot
        /// </summary>
        public void CreateIllumination()
        {
            EnsureManagersInitialized();
            subMenuManager?.CreateIllumination();
        }

        /// <summary>
        /// Crea un nuevo MRUnificarMateriales como hijo y lo añade a un slot
        /// </summary>
        public void CreateUnifyMaterial()
        {
            EnsureManagersInitialized();
            subMenuManager?.CreateUnifyMaterial();
        }
        
        /// <summary>
        /// Maneja clic en botón del menú
        /// </summary>
        public void HandleMenuButtonClick(int buttonIndex)
        {
            EnsureManagersInitialized();
            interactionHandler?.HandleMenuButtonClick(buttonIndex);
        }
        
        /// <summary>
        /// Crea archivos VRChat
        /// </summary>
        public void CreateVRChatFiles()
        {
#if UNITY_EDITOR
            EnsureManagersInitialized();
            // Sincronizar configuración desde MRMenuRadial antes de generar
            vrchatConfig?.SyncFromMenuRadial(transform);
            // Recrear fileGenerator con la configuración actualizada
            fileGenerator = new MRVRChatFileGenerator(this, slotManager, vrchatConfig, OutputPath);
            fileGenerator?.CreateVRChatFiles();
#endif
        }
        

        
        /// <summary>
        /// Obtiene el gestor de slots
        /// </summary>
        public MRSlotManager GetSlotManager() => slotManager;
        
        /// <summary>
        /// Obtiene el gestor de navegación
        /// </summary>
        public MRNavigationManager GetNavigationManager() => navigationManager;
        
        /// <summary>
        /// Obtiene el gestor de submenús
        /// </summary>
        public MRSubMenuManager GetSubMenuManager() => subMenuManager;
        
        /// <summary>
        /// Obtiene el manejador de interacciones
        /// </summary>
        public MRMenuInteractionHandler GetInteractionHandler() => interactionHandler;
        

        
        /// <summary>
        /// Tipo de animación (SubMenu para navegación)
        /// </summary>
        public AnimationType AnimationType => AnimationType.SubMenu;
        
        /// <summary>
        /// Nombre de la animación
        /// </summary>
        public string AnimationName => name + "_ControlMenu";
        
        /// <summary>
        /// Puede generar animación si todos los slots son válidos
        /// </summary>
        public bool CanGenerateAnimation => AllSlotsValid;
        
        /// <summary>
        /// Descripción del tipo de animación
        /// </summary>
        public string GetAnimationTypeDescription()
        {
            return $"Menú de Control ({SlotCount} slots, {slotManager?.GetValidSlotCount() ?? 0} válidos)";
        }
        

        
        /// <summary>
        /// Recalcula rutas de componentes
        /// </summary>
        public void RecalculatePaths()
        {
            // Implementación delegada a gestores si es necesario
        }

        /// <summary>
        /// Resetea todos los previews de los slots a sus estados originales
        /// Útil para restaurar el estado del avatar después de previsualizar cambios
        /// CORREGIDO v4: Siempre resetea desde el menú raíz para cubrir todo el árbol
        /// </summary>
        public void ResetAllPreviews()
        {
            // Obtener el menú raíz para resetear todo el árbol
            var rootMenu = GetRootMenu();

            if (rootMenu != null)
            {
                ResetAllPreviewsRecursive(rootMenu);
            }
            else
            {
                // Fallback si no hay raíz (no debería pasar)
                ResetAllPreviewsRecursive(this);
            }
        }

        /// <summary>
        /// Resetea previews recursivamente para un menú y todos sus submenús
        /// </summary>
        private void ResetAllPreviewsRecursive(MRMenuControl menu)
        {
            if (menu == null || menu.animationSlots == null)
                return;

            // 1. Restaurar propiedades originales de iluminación y UnifyMaterial ANTES de limpiar cache
            foreach (var slot in menu.animationSlots)
            {
                if (slot == null || !slot.isValid || slot.targetObject == null) continue;

                // Restaurar iluminación
                var illumination = slot.CachedIllumination;
                if (illumination != null)
                {
                    string slotKey = $"{menu.GetInstanceID()}_{menu.animationSlots.IndexOf(slot)}_{slot.slotName}";
                    var illuminationRenderer = RadialSliderIntegration.GetOrCreateIlluminationRenderer(slotKey, illumination);
                    if (illuminationRenderer != null)
                    {
                        illuminationRenderer.RestoreOriginalMaterialProperties();
                    }
                }

                // Restaurar UnifyMaterial
                var unifyMaterial = slot.CachedUnifyMaterial;
                if (unifyMaterial != null)
                {
                    string slotKey = $"{menu.GetInstanceID()}_{menu.animationSlots.IndexOf(slot)}_{slot.slotName}";
                    var unifyRenderer = RadialSliderIntegration.GetOrCreateUnifyMaterialRenderer(slotKey, unifyMaterial);
                    if (unifyRenderer != null)
                    {
                        unifyRenderer.RestoreOriginalMaterials();
                    }
                }

                // Recursivamente resetear submenús
                var subMenu = slot.CachedControlMenu;
                if (subMenu != null && subMenu != menu)
                {
                    ResetAllPreviewsRecursive(subMenu);
                }
            }

            // 2. Cancelar previews y restaurar estados originales de cada slot
            foreach (var slot in menu.animationSlots)
            {
                if (slot == null || !slot.isValid || slot.targetObject == null) continue;

                // Usar cache de componentes del slot
                var previewable = slot.CachedPreviewable;
                if (previewable != null && previewable.IsPreviewActive)
                {
                    previewable.DeactivatePreview();
                }

                // Para MRUnificarObjetos, restaurar al estado neutral (todos los objetos apagados)
                var radialMenu = slot.CachedRadialMenu;
                if (radialMenu != null)
                {
                    radialMenu.RestoreToNeutralState();
                }
            }

            // 3. Limpiar cache de sliders (solo una vez al final si es el menú raíz)
            if (menu == this)
            {
                RadialSliderIntegration.ClearSliderCache();
            }
        }

        

        
        /// <summary>
        /// Constante para compatibilidad con inspector
        /// </summary>
        public const int MAX_SLOTS = MRSlotManager.MAX_SLOTS;
        
        /// <summary>
        /// Si tiene menú padre (delegado a NavigationManager)
        /// </summary>
        public bool HasParent => navigationManager?.HasParent ?? false;
        
        /// <summary>
        /// Menú padre (delegado a NavigationManager)
        /// </summary>
        public MRMenuControl ParentMenu => navigationManager?.ParentMenu;
        
        /// <summary>
        /// Ruta de navegación (delegado a NavigationManager)
        /// </summary>
        public string NavigationPath => navigationManager?.NavigationPath ?? name;
        
        /// <summary>
        /// Menú seleccionado actual (delegado a NavigationManager)
        /// </summary>
        public static MRMenuControl CurrentSelectedMenu => MRNavigationManager.CurrentSelectedMenu;
        
        /// <summary>
        /// Navega al menú padre (delegado a NavigationManager)
        /// </summary>
        public void NavigateToParent() => navigationManager?.NavigateToParent();
        
        /// <summary>
        /// Navega al menú raíz (delegado a NavigationManager)
        /// </summary>
        public void NavigateToRoot() => navigationManager?.NavigateToRoot();
        
        /// <summary>
        /// Obtiene el menú raíz (delegado a NavigationManager)
        /// </summary>
        public MRMenuControl GetRootMenu() => navigationManager?.GetRootMenu();
        
        /// <summary>
        /// Obtiene submenú de un slot (delegado a NavigationManager)
        /// </summary>
        public MRMenuControl GetSubMenuFromSlot(int index) => navigationManager?.GetSubMenuFromSlot(index);
        
        /// <summary>
        /// Valida todos los slots (delegado a SlotManager)
        /// </summary>
        public void ValidateAllSlots() => slotManager?.ValidateAllSlots();
        
        /// <summary>
        /// Puede crear submenú (delegado a SubMenuManager)
        /// </summary>
        public bool CanCreateSubMenu() => subMenuManager?.CanCreateSubMenu() ?? false;
        


        /// <summary>
        /// OPTIMIZACIÓN 4: Cache de validación temporal para evitar operaciones repetidas
        /// </summary>
        private void ValidateWithCache()
        {
            var now = System.DateTime.Now;
            
            // Cache de validación por 1 segundo para evitar validaciones múltiples seguidas
            if (_validationCacheValid && (now - _lastValidationTime).TotalSeconds < 1.0)
            {
                return; // Usar cache válido
            }
            
            // Ejecutar validación real
            if (slotManager != null)
            {
                slotManager.ValidateAllSlots();
            }
            
            // Actualizar cache
            _validationCacheValid = true;
            _lastValidationTime = now;
        }

        /// <summary>
        /// Sincroniza los nombres de los slots desde los nombres de animación de sus componentes.
        /// Permite que cambios en MRUnificarObjetos.AnimationName se reflejen en slotName.
        /// </summary>
        private void SyncSlotNamesFromAnimations()
        {
            if (animationSlots == null)
                return;

            foreach (var slot in animationSlots)
            {
                if (slot != null && slot.SyncNameWithAnimation)
                {
                    slot.SyncFromAnimationName();
                }
            }
        }
        
        /// <summary>
        /// Calcula hash de slots para detectar cambios estructurales
        /// </summary>
        private int CalculateSlotHashCode()
        {
            if (animationSlots == null)
                return 0;
                
            int hash = animationSlots.Count;
            
            for (int i = 0; i < animationSlots.Count; i++)
            {
                var slot = animationSlots[i];
                if (slot != null)
                {
                    // Hash basado en propiedades clave que afectan validación
                    hash ^= (slot.targetObject?.GetInstanceID() ?? 0) << i;
                    hash ^= (slot.slotName?.GetHashCode() ?? 0) << (i + 8);
                    hash ^= (slot.iconImage?.GetInstanceID() ?? 0) << (i + 16);
                }
            }
            
            return hash;
        }
        
        /// <summary>
        /// Invalida el cache de validación forzando revalidación en próxima llamada
        /// </summary>
        private void InvalidateValidationCache()
        {
            _validationCacheValid = false;
            _lastValidationTime = System.DateTime.MinValue;
        }
        
    }
}
