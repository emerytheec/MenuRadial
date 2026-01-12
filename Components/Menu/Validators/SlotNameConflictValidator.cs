using System.Collections.Generic;
using System.Linq;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.Menu.Validators
{
    /// <summary>
    /// Validador de conflictos de nombres entre slots.
    /// Detecta nombres duplicados de slots y de animaciones.
    /// </summary>
    public class SlotNameConflictValidator
    {
        /// <summary>
        /// Tipo de conflicto detectado
        /// </summary>
        public enum ConflictType
        {
            /// <summary>
            /// Nombres de slot duplicados
            /// </summary>
            DuplicateSlotName,

            /// <summary>
            /// Nombres de animación duplicados
            /// </summary>
            DuplicateAnimationName
        }

        /// <summary>
        /// Información sobre un conflicto detectado
        /// </summary>
        public class ConflictInfo
        {
            /// <summary>
            /// Nombre que está en conflicto
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Tipo de conflicto
            /// </summary>
            public ConflictType Type { get; set; }

            /// <summary>
            /// Índices de los slots que tienen el conflicto
            /// </summary>
            public List<int> SlotIndices { get; set; } = new List<int>();

            /// <summary>
            /// Descripción legible del conflicto
            /// </summary>
            public string GetDescription()
            {
                string typeDesc = Type == ConflictType.DuplicateSlotName
                    ? "nombre de slot"
                    : "nombre de animación";
                return $"Conflicto de {typeDesc}: '{Name}' (slots: {string.Join(", ", SlotIndices)})";
            }
        }

        /// <summary>
        /// Detecta conflictos de nombres en una lista de slots
        /// </summary>
        /// <param name="slots">Lista de slots a validar</param>
        /// <returns>Lista de conflictos encontrados</returns>
        public List<ConflictInfo> DetectConflicts(IReadOnlyList<MRAnimationSlot> slots)
        {
            var conflicts = new List<ConflictInfo>();

            if (slots == null || slots.Count == 0)
                return conflicts;

            // Detectar nombres de slot duplicados
            DetectDuplicateSlotNames(slots, conflicts);

            // Detectar nombres de animación duplicados
            DetectDuplicateAnimationNames(slots, conflicts);

            return conflicts;
        }

        /// <summary>
        /// Detecta nombres de slot duplicados
        /// </summary>
        private void DetectDuplicateSlotNames(IReadOnlyList<MRAnimationSlot> slots, List<ConflictInfo> conflicts)
        {
            var nameToIndices = new Dictionary<string, List<int>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.slotName))
                    continue;

                if (!nameToIndices.ContainsKey(slot.slotName))
                {
                    nameToIndices[slot.slotName] = new List<int>();
                }
                nameToIndices[slot.slotName].Add(i);
            }

            foreach (var kvp in nameToIndices)
            {
                if (kvp.Value.Count > 1)
                {
                    conflicts.Add(new ConflictInfo
                    {
                        Name = kvp.Key,
                        Type = ConflictType.DuplicateSlotName,
                        SlotIndices = kvp.Value
                    });
                }
            }
        }

        /// <summary>
        /// Detecta nombres de animación duplicados
        /// </summary>
        private void DetectDuplicateAnimationNames(IReadOnlyList<MRAnimationSlot> slots, List<ConflictInfo> conflicts)
        {
            var nameToIndices = new Dictionary<string, List<int>>();

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                var provider = slot.GetAnimationProvider();
                if (provider == null)
                    continue;

                string animName = provider.AnimationName;
                if (string.IsNullOrEmpty(animName))
                    continue;

                // Ignorar submenús (no generan animaciones propias)
                if (provider.AnimationType == AnimationType.SubMenu)
                    continue;

                if (!nameToIndices.ContainsKey(animName))
                {
                    nameToIndices[animName] = new List<int>();
                }
                nameToIndices[animName].Add(i);
            }

            foreach (var kvp in nameToIndices)
            {
                if (kvp.Value.Count > 1)
                {
                    conflicts.Add(new ConflictInfo
                    {
                        Name = kvp.Key,
                        Type = ConflictType.DuplicateAnimationName,
                        SlotIndices = kvp.Value
                    });
                }
            }
        }

        /// <summary>
        /// Auto-resuelve conflictos de nombres de slot agregando sufijos numéricos
        /// </summary>
        /// <param name="slots">Lista de slots a modificar</param>
        /// <param name="conflicts">Conflictos a resolver (solo DuplicateSlotName)</param>
        public void AutoResolveSlotNameConflicts(IList<MRAnimationSlot> slots, List<ConflictInfo> conflicts)
        {
            foreach (var conflict in conflicts.Where(c => c.Type == ConflictType.DuplicateSlotName))
            {
                // Mantener el primer slot con el nombre original, renombrar los demás
                int counter = 1;
                foreach (var index in conflict.SlotIndices.Skip(1))
                {
                    if (index >= 0 && index < slots.Count)
                    {
                        string newName = $"{conflict.Name}_{counter}";

                        // Asegurar que el nuevo nombre no existe
                        while (slots.Any(s => s != null && s.slotName == newName))
                        {
                            counter++;
                            newName = $"{conflict.Name}_{counter}";
                        }

                        slots[index].slotName = newName;
                        counter++;
                    }
                }
            }
        }

        /// <summary>
        /// Valida y retorna un resultado de validación
        /// </summary>
        /// <param name="slots">Lista de slots a validar</param>
        /// <returns>Resultado de validación con detalles de conflictos</returns>
        public ValidationResult Validate(IReadOnlyList<MRAnimationSlot> slots)
        {
            var conflicts = DetectConflicts(slots);

            if (conflicts.Count == 0)
            {
                return ValidationResult.Success("No se detectaron conflictos de nombres");
            }

            var result = ValidationResult.Warning($"Se detectaron {conflicts.Count} conflicto(s) de nombres");

            foreach (var conflict in conflicts)
            {
                result.AddChild(ValidationResult.Warning(conflict.GetDescription()));
            }

            return result;
        }
    }
}
