using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Components.MenuRadial;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Gestor de creación y administración de submenús.
    /// Maneja la creación de GameObjects hijos con componente MRMenuControl.
    /// </summary>
    public class MRSubMenuManager
    {
        
        private MRMenuControl ownerMenu;
        private MRSlotManager slotManager;
        

        
        /// <summary>
        /// Inicializa el gestor de submenús
        /// </summary>
        /// <param name="owner">El menú propietario</param>
        /// <param name="slotManager">El gestor de slots asociado</param>
        public MRSubMenuManager(MRMenuControl owner, MRSlotManager slotManager)
        {
            ownerMenu = owner;
            this.slotManager = slotManager;
        }
        

        
        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRMenuControl y lo asigna al siguiente slot disponible
        /// CORREGIDO: Ahora registra Undo correctamente y asegura serialización
        /// </summary>
        /// <returns>El MRMenuControl creado o null si no se pudo crear</returns>
        public MRMenuControl CreateSubMenu()
        {
            if (!CanCreateSubMenu())
            {
                return null;
            }

#if UNITY_EDITOR
            // IMPORTANTE: Registrar el estado del objeto padre ANTES de modificarlo
            UnityEditor.Undo.RecordObject(ownerMenu, "Crear Sub-Menú");
#endif

            // Generar nombre único para el submenú
            string subMenuName = GenerateUniqueSubMenuName();

            // Crear nuevo GameObject como hijo
            GameObject subMenuObject = CreateSubMenuGameObject(subMenuName);

#if UNITY_EDITOR
            // Registrar el nuevo GameObject para Undo
            UnityEditor.Undo.RegisterCreatedObjectUndo(subMenuObject, "Crear Sub-Menú");
#endif

            // Añadir componente MRMenuControl
            var subMenuComponent = subMenuObject.AddComponent<MRMenuControl>();

            // Forzar inicialización de managers del nuevo submenú
            // Esto asegura que GetNavigationManager() retorne un valor válido
            subMenuComponent.gameObject.SetActive(true);

            // Establecer relación padre-hijo
            var navigationManager = subMenuComponent.GetNavigationManager();
            if (navigationManager != null)
            {
                navigationManager.ParentMenu = ownerMenu;
            }

            // Añadir al slot del menú padre
            // Primero buscar slot vacío, si no hay crear uno nuevo
            var animationSlots = ownerMenu.AnimationSlots;
            bool slotAssigned = false;

            // Buscar primer slot vacío (targetObject == null)
            for (int i = 0; i < animationSlots.Count; i++)
            {
                if (animationSlots[i].targetObject == null)
                {
                    // Usar el slot vacío existente
                    animationSlots[i].slotName = subMenuName;
                    animationSlots[i].targetObject = subMenuObject;
                    animationSlots[i].ValidateSlot();
                    slotManager.UpdateSlots(animationSlots);
                    slotAssigned = true;
                    break;
                }
            }

            // Si no se asignó a slot vacío, crear uno nuevo si hay espacio
            if (!slotAssigned && animationSlots.Count < MRSlotManager.MAX_SLOTS)
            {
                var newSlot = new MRAnimationSlot
                {
                    slotName = subMenuName,
                    targetObject = subMenuObject
                };

                animationSlots.Add(newSlot);
                newSlot.ValidateSlot();
                slotManager.UpdateSlots(animationSlots);
            }

            // Marcar como dirty para guardar cambios
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ownerMenu);
            UnityEditor.EditorUtility.SetDirty(subMenuComponent);

            // Seleccionar el nuevo submenú en el inspector para feedback visual
            UnityEditor.Selection.activeGameObject = subMenuObject;
#endif

            return subMenuComponent;
        }

        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRUnificarObjetos y lo asigna al siguiente slot disponible
        /// </summary>
        /// <returns>El MRUnificarObjetos creado o null si no se pudo crear</returns>
        public MRUnificarObjetos CreateRadialMenu()
        {
            if (!CanCreateSubMenu())
            {
                return null;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(ownerMenu, "Crear Unificar Objetos");
#endif

            string componentName = GenerateUniqueComponentName("UnificarObjetos");
            GameObject componentObject = CreateComponentGameObject(componentName);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Crear Unificar Objetos");
#endif

            var radialMenu = componentObject.AddComponent<MRUnificarObjetos>();
            AddToSlot(componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ownerMenu);
            UnityEditor.EditorUtility.SetDirty(radialMenu);
            UnityEditor.Selection.activeGameObject = componentObject;
#endif

            return radialMenu;
        }

        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRIluminacionRadial y lo asigna al siguiente slot disponible.
        /// Automáticamente asigna el avatar desde MRMenuRadial si está disponible.
        /// </summary>
        /// <returns>El MRIluminacionRadial creado o null si no se pudo crear</returns>
        public MRIluminacionRadial CreateIllumination()
        {
            if (!CanCreateSubMenu())
            {
                return null;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(ownerMenu, "Crear Iluminación Radial");
#endif

            string componentName = GenerateUniqueComponentName("IluminacionRadial");
            GameObject componentObject = CreateComponentGameObject(componentName);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Crear Iluminación Radial");
#endif

            var illumination = componentObject.AddComponent<MRIluminacionRadial>();

            // Asignar automáticamente el avatar desde MRMenuRadial
            var avatarRoot = GetAvatarFromMenuRadial();
            if (avatarRoot != null)
            {
                illumination.RootObject = avatarRoot;
            }

            AddToSlot(componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ownerMenu);
            UnityEditor.EditorUtility.SetDirty(illumination);
            UnityEditor.Selection.activeGameObject = componentObject;
#endif

            return illumination;
        }

        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRUnificarMateriales y lo asigna al siguiente slot disponible
        /// </summary>
        /// <returns>El MRUnificarMateriales creado o null si no se pudo crear</returns>
        public MRUnificarMateriales CreateUnifyMaterial()
        {
            if (!CanCreateSubMenu())
            {
                return null;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(ownerMenu, "Crear Unificar Materiales");
#endif

            string componentName = GenerateUniqueComponentName("UnificarMateriales");
            GameObject componentObject = CreateComponentGameObject(componentName);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Crear Unificar Materiales");
#endif

            var unifyMaterial = componentObject.AddComponent<MRUnificarMateriales>();
            AddToSlot(componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ownerMenu);
            UnityEditor.EditorUtility.SetDirty(unifyMaterial);
            UnityEditor.Selection.activeGameObject = componentObject;
#endif

            return unifyMaterial;
        }

        /// <summary>
        /// Busca MRMenuRadial en los ancestros y obtiene el avatar asignado.
        /// </summary>
        /// <returns>El GameObject del avatar o null si no se encuentra</returns>
        private GameObject GetAvatarFromMenuRadial()
        {
            if (ownerMenu == null)
                return null;

            // Buscar MRMenuRadial en los ancestros
            Transform current = ownerMenu.transform;
            while (current != null)
            {
                var menuRadial = current.GetComponent<MRMenuRadial>();
                if (menuRadial != null && menuRadial.AvatarRoot != null)
                {
                    return menuRadial.AvatarRoot;
                }
                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Crea un GameObject hijo genérico para cualquier componente
        /// </summary>
        private GameObject CreateComponentGameObject(string componentName)
        {
            GameObject componentObject = new GameObject(componentName);
            componentObject.transform.SetParent(ownerMenu.transform);
            componentObject.transform.localPosition = Vector3.zero;
            componentObject.transform.localRotation = Quaternion.identity;
            componentObject.transform.localScale = Vector3.one;
            return componentObject;
        }

        /// <summary>
        /// Añade un GameObject a un slot del menú.
        /// Primero busca un slot vacío existente, si no hay crea uno nuevo.
        /// </summary>
        private void AddToSlot(GameObject targetObject, string slotName)
        {
            var animationSlots = ownerMenu.AnimationSlots;

            // Buscar primer slot vacío (targetObject == null)
            for (int i = 0; i < animationSlots.Count; i++)
            {
                if (animationSlots[i].targetObject == null)
                {
                    // Usar el slot vacío existente
                    animationSlots[i].slotName = slotName;
                    animationSlots[i].targetObject = targetObject;
                    animationSlots[i].ValidateSlot();
                    slotManager.UpdateSlots(animationSlots);
                    return;
                }
            }

            // Si no hay slot vacío, crear uno nuevo si hay espacio
            if (animationSlots.Count < MRSlotManager.MAX_SLOTS)
            {
                var newSlot = new MRAnimationSlot
                {
                    slotName = slotName,
                    targetObject = targetObject
                };

                animationSlots.Add(newSlot);
                newSlot.ValidateSlot();
                slotManager.UpdateSlots(animationSlots);
            }
        }

        /// <summary>
        /// Genera un nombre único para un nuevo componente
        /// </summary>
        public string GenerateUniqueComponentName(string baseName)
        {
            var existingNames = new HashSet<string>();

            foreach (var slot in slotManager.Slots)
            {
                if (!string.IsNullOrEmpty(slot.slotName))
                    existingNames.Add(slot.slotName);
            }

            for (int i = 0; i < ownerMenu.transform.childCount; i++)
            {
                existingNames.Add(ownerMenu.transform.GetChild(i).name);
            }

            string uniqueName = baseName;
            int counter = 1;

            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}_{counter:00}";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// Crea el GameObject base para el submenú
        /// </summary>
        /// <param name="subMenuName">Nombre del submenú</param>
        /// <returns>GameObject creado</returns>
        private GameObject CreateSubMenuGameObject(string subMenuName)
        {
            GameObject subMenuObject = new GameObject(subMenuName);
            subMenuObject.transform.SetParent(ownerMenu.transform);
            subMenuObject.transform.localPosition = Vector3.zero;
            subMenuObject.transform.localRotation = Quaternion.identity;
            subMenuObject.transform.localScale = Vector3.one;
            
            return subMenuObject;
        }
        
        /// <summary>
        /// Genera un nombre único para el nuevo submenú
        /// </summary>
        /// <returns>Nombre único</returns>
        public string GenerateUniqueSubMenuName()
        {
            // Obtener todos los nombres existentes
            var existingNames = new HashSet<string>();
            
            // Nombres de slots existentes
            foreach (var slot in slotManager.Slots)
            {
                if (!string.IsNullOrEmpty(slot.slotName))
                    existingNames.Add(slot.slotName);
            }
            
            // Nombres de GameObjects hijos
            for (int i = 0; i < ownerMenu.transform.childCount; i++)
            {
                existingNames.Add(ownerMenu.transform.GetChild(i).name);
            }
            
            // Generar nombre único
            string baseName = "SubMenu";
            string uniqueName = baseName;
            int counter = 1;
            
            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}_{counter:00}";
                counter++;
            }
            
            return uniqueName;
        }
        

        
        /// <summary>
        /// Verifica si se puede crear un nuevo submenú
        /// </summary>
        /// <returns>True si se puede crear</returns>
        public bool CanCreateSubMenu()
        {
            return slotManager.CanAddSlot();
        }
        
        /// <summary>
        /// Obtiene todos los submenús hijos de este menú
        /// </summary>
        /// <returns>Lista de componentes MRMenuControl hijos</returns>
        public List<MRMenuControl> GetAllSubMenus()
        {
            var subMenus = new List<MRMenuControl>();
            
            for (int i = 0; i < ownerMenu.transform.childCount; i++)
            {
                var child = ownerMenu.transform.GetChild(i);
                var subMenu = child.GetComponent<MRMenuControl>();
                if (subMenu != null)
                {
                    subMenus.Add(subMenu);
                }
            }
            
            return subMenus;
        }
        
        /// <summary>
        /// Obtiene submenús que están configurados en slots
        /// </summary>
        /// <returns>Lista de submenús configurados</returns>
        public List<MRMenuControl> GetConfiguredSubMenus()
        {
            var configuredSubMenus = new List<MRMenuControl>();
            
            foreach (var slot in slotManager.Slots)
            {
                if (slot.isValid && slot.targetObject != null)
                {
                    var subMenu = slot.CachedControlMenu;
                    if (subMenu != null)
                    {
                        configuredSubMenus.Add(subMenu);
                    }
                }
            }
            
            return configuredSubMenus;
        }
        
        /// <summary>
        /// Obtiene submenús que NO están configurados en ningún slot
        /// </summary>
        /// <returns>Lista de submenús huérfanos</returns>
        public List<MRMenuControl> GetOrphanedSubMenus()
        {
            var allSubMenus = GetAllSubMenus();
            var configuredSubMenus = GetConfiguredSubMenus();
            
            var orphanedSubMenus = new List<MRMenuControl>();
            
            foreach (var subMenu in allSubMenus)
            {
                if (!configuredSubMenus.Contains(subMenu))
                {
                    orphanedSubMenus.Add(subMenu);
                }
            }
            
            return orphanedSubMenus;
        }
        
        /// <summary>
        /// Valida la integridad de la estructura de submenús
        /// </summary>
        /// <returns>Lista de problemas encontrados</returns>
        public List<string> ValidateSubMenuIntegrity()
        {
            var issues = new List<string>();
            
            // Verificar submenús huérfanos
            var orphanedSubMenus = GetOrphanedSubMenus();
            if (orphanedSubMenus.Count > 0)
            {
                issues.Add($"{orphanedSubMenus.Count} submenús no están configurados en slots");
            }
            
            // Verificar slots con referencias inválidas
            for (int i = 0; i < slotManager.SlotCount; i++)
            {
                var slot = slotManager.GetSlot(i);
                if (slot?.targetObject != null)
                {
                    var subMenu = slot.CachedControlMenu;
                    if (subMenu != null)
                    {
                        // Verificar si es realmente hijo de este menú
                        if (subMenu.transform.parent != ownerMenu.transform)
                        {
                            issues.Add($"Slot {i} referencia un submenú que no es hijo directo: {subMenu.name}");
                        }
                    }
                }
            }
            
            // Verificar nombres duplicados
            var subMenuNames = new HashSet<string>();
            foreach (var subMenu in GetAllSubMenus())
            {
                if (subMenuNames.Contains(subMenu.name))
                {
                    issues.Add($"Nombre de submenú duplicado: {subMenu.name}");
                }
                else
                {
                    subMenuNames.Add(subMenu.name);
                }
            }
            
            return issues;
        }
        
    }
}
