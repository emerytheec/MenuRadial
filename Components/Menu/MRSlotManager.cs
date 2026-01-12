using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu.Validators;

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Gestor de slots de animación para MRMenuControl.
    /// Maneja todas las operaciones CRUD sobre la lista de slots.
    /// </summary>
    public class MRSlotManager
    {
        public const int MIN_SLOTS = 1;
        /// <summary>
        /// Número máximo de slots - usa constante centralizada para compatibilidad hacia atrás
        /// </summary>
        public const int MAX_SLOTS = MRMenuConstants.MAX_SLOTS;
        

        
        private List<MRAnimationSlot> slots;
        

        
        /// <summary>
        /// Lista de slots configurados (solo lectura)
        /// </summary>
        public IReadOnlyList<MRAnimationSlot> Slots => slots.AsReadOnly();
        
        /// <summary>
        /// Número actual de slots
        /// </summary>
        public int SlotCount => slots.Count;
        
        /// <summary>
        /// Si todos los slots están validados correctamente
        /// </summary>
        public bool AllSlotsValid => GetValidSlotCount() == slots.Count && slots.Count > 0;
        

        
        /// <summary>
        /// Inicializa el gestor con una lista de slots existente
        /// Aplica límite duro de MAX_SLOTS
        /// </summary>
        /// <param name="existingSlots">Lista de slots existente (puede ser null)</param>
        public MRSlotManager(List<MRAnimationSlot> existingSlots = null)
        {
            slots = existingSlots ?? new List<MRAnimationSlot>();

            // Aplicar límite duro: eliminar slots extras si hay más de MAX_SLOTS
            while (slots.Count > MAX_SLOTS)
            {
                slots.RemoveAt(slots.Count - 1);
            }

            // Asegurar que tenemos al menos 1 slot
            if (slots.Count == 0)
            {
                AddSlot();
            }
        }
        

        
        /// <summary>
        /// Añade un nuevo slot de animación
        /// </summary>
        /// <returns>True si se añadió correctamente</returns>
        public bool AddSlot()
        {
            if (slots.Count >= MAX_SLOTS)
            {
                return false;
            }
            
            var newSlot = new MRAnimationSlot
            {
                slotName = GenerateUniqueSlotName()
            };
            
            slots.Add(newSlot);
            ValidateAllSlots();
            
            return true;
        }
        
        /// <summary>
        /// Remueve un slot de animación por índice
        /// </summary>
        /// <param name="index">Índice del slot a remover</param>
        /// <returns>True si se removió correctamente</returns>
        public bool RemoveSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                return false;
            }
            
            if (slots.Count <= MIN_SLOTS)
            {
                return false;
            }
            
            string removedSlotName = slots[index].slotName;
            slots.RemoveAt(index);
            ValidateAllSlots();
            
            return true;
        }
        
        /// <summary>
        /// Mueve un slot a una nueva posición (para drag & drop)
        /// </summary>
        /// <param name="fromIndex">Índice origen</param>
        /// <param name="toIndex">Índice destino</param>
        /// <returns>True si se movió correctamente</returns>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= slots.Count)
            {
                return false;
            }
            
            if (toIndex < 0 || toIndex >= slots.Count)
            {
                return false;
            }
            
            if (fromIndex == toIndex) return true;
            
            var slot = slots[fromIndex];
            slots.RemoveAt(fromIndex);
            slots.Insert(toIndex, slot);
            
            ValidateAllSlots();
            
            return true;
        }
        
        /// <summary>
        /// Obtiene un slot por índice
        /// </summary>
        /// <param name="index">Índice del slot</param>
        /// <returns>El slot o null si el índice es inválido</returns>
        public MRAnimationSlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
                return null;
                
            return slots[index];
        }
        
        /// <summary>
        /// Actualiza la lista interna de slots (para sincronización con componente principal)
        /// Aplica límite duro de MAX_SLOTS
        /// </summary>
        /// <param name="newSlots">Nueva lista de slots</param>
        public void UpdateSlots(List<MRAnimationSlot> newSlots)
        {
            slots = newSlots ?? new List<MRAnimationSlot>();

            // Aplicar límite duro: eliminar slots extras si hay más de MAX_SLOTS
            while (slots.Count > MAX_SLOTS)
            {
                slots.RemoveAt(slots.Count - 1);
            }

            ValidateAllSlots();
        }
        

        
        /// <summary>
        /// Valida todos los slots configurados
        /// </summary>
        public void ValidateAllSlots()
        {
            foreach (var slot in slots)
            {
                slot.ValidateSlot();
            }
            
            // Validar nombres únicos
            ValidateUniqueNames();
        }
        
        /// <summary>
        /// Valida que no haya nombres duplicados entre slots
        /// </summary>
        private void ValidateUniqueNames()
        {
            var usedNames = new HashSet<string>();
            
            foreach (var slot in slots)
            {
                if (string.IsNullOrEmpty(slot.slotName)) continue;
                
                if (usedNames.Contains(slot.slotName))
                {
                    slot.isValid = false;
                    slot.validationMessage = "Nombre duplicado";
                }
                else
                {
                    usedNames.Add(slot.slotName);
                }
            }
        }
        
        /// <summary>
        /// Obtiene el número de slots válidos
        /// </summary>
        /// <returns>Número de slots válidos</returns>
        public int GetValidSlotCount()
        {
            return slots.Count(slot => slot.isValid);
        }
        
        /// <summary>
        /// Obtiene información de validación para mostrar en el inspector
        /// </summary>
        /// <returns>String con resumen de validación</returns>
        public string GetValidationSummary()
        {
            int valid = GetValidSlotCount();
            int total = slots.Count;
            
            if (valid == total && total > 0)
                return $"✅ Todos los slots válidos ({valid}/{total})";
            else if (valid > 0)
                return $"⚠️ Algunos slots inválidos ({valid}/{total})";
            else
                return $"❌ No hay slots válidos ({valid}/{total})";
        }
        

        
        /// <summary>
        /// Genera un nombre único para un nuevo slot
        /// </summary>
        /// <returns>Nombre único para el slot</returns>
        private string GenerateUniqueSlotName()
        {
            var existingNames = new HashSet<string>();
            
            foreach (var slot in slots)
            {
                if (!string.IsNullOrEmpty(slot.slotName))
                    existingNames.Add(slot.slotName);
            }
            
            string baseName = "Slot";
            string uniqueName = baseName + "_1";
            int counter = 1;
            
            while (existingNames.Contains(uniqueName))
            {
                counter++;
                uniqueName = $"{baseName}_{counter}";
            }
            
            return uniqueName;
        }
        
        /// <summary>
        /// Busca un slot por nombre
        /// </summary>
        /// <param name="slotName">Nombre del slot a buscar</param>
        /// <returns>El slot encontrado o null</returns>
        public MRAnimationSlot FindSlotByName(string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
                return null;
                
            return slots.FirstOrDefault(slot => slot.slotName == slotName);
        }
        
        /// <summary>
        /// Obtiene el índice de un slot específico
        /// </summary>
        /// <param name="slot">El slot a buscar</param>
        /// <returns>Índice del slot o -1 si no se encuentra</returns>
        public int GetSlotIndex(MRAnimationSlot slot)
        {
            if (slot == null)
                return -1;
                
            return slots.IndexOf(slot);
        }
        
        /// <summary>
        /// Verifica si se puede añadir más slots
        /// </summary>
        /// <returns>True si se puede añadir un slot más</returns>
        public bool CanAddSlot()
        {
            return slots.Count < MAX_SLOTS;
        }
        
        /// <summary>
        /// Verifica si se puede remover slots
        /// </summary>
        /// <returns>True si se puede remover al menos un slot</returns>
        public bool CanRemoveSlot()
        {
            return slots.Count > MIN_SLOTS;
        }

        #region Conflict Detection

        private SlotNameConflictValidator _conflictValidator;

        /// <summary>
        /// Detecta conflictos de nombres en los slots
        /// </summary>
        /// <returns>Lista de conflictos encontrados</returns>
        public List<SlotNameConflictValidator.ConflictInfo> DetectConflicts()
        {
            _conflictValidator ??= new SlotNameConflictValidator();
            return _conflictValidator.DetectConflicts(slots);
        }

        /// <summary>
        /// Verifica si hay conflictos de nombres
        /// </summary>
        /// <returns>True si hay al menos un conflicto</returns>
        public bool HasConflicts()
        {
            return DetectConflicts().Count > 0;
        }

        /// <summary>
        /// Auto-resuelve conflictos de nombres de slots
        /// </summary>
        /// <returns>True si se resolvieron conflictos</returns>
        public bool AutoResolveConflicts()
        {
            var conflicts = DetectConflicts();
            if (conflicts.Count == 0)
                return false;

            _conflictValidator ??= new SlotNameConflictValidator();
            _conflictValidator.AutoResolveSlotNameConflicts(slots, conflicts);
            ValidateAllSlots();
            return true;
        }

        /// <summary>
        /// Obtiene un resumen de los conflictos detectados
        /// </summary>
        /// <returns>Descripción de los conflictos o null si no hay</returns>
        public string GetConflictsSummary()
        {
            var conflicts = DetectConflicts();
            if (conflicts.Count == 0)
                return null;

            var descriptions = conflicts.Select(c => c.GetDescription());
            return string.Join("\n", descriptions);
        }

        #endregion
    }
}
