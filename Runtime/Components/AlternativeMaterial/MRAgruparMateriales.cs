using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.AlternativeMaterial
{
    /// <summary>
    /// Componente MR Agrupar Materiales (antes MRAlternativeMaterial)
    /// Gestiona materiales alternativos para prendas y genera animaciones de cambio
    /// </summary>
    [AddComponentMenu("MR/MR Agrupar Materiales")]
    public class MRAgruparMateriales : MRComponentBase
    {
        [SerializeField] private string _componentName = "Nueva Prenda";
        [SerializeField] private List<MRMaterialSlot> _slots = new List<MRMaterialSlot>();
        [SerializeField] private List<MRMaterialGroup> _groups = new List<MRMaterialGroup>();

        #region Properties

        /// <summary>
        /// Nombre identificador del componente (ej: "Vestido", "Accesorios")
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set => _componentName = value;
        }

        /// <summary>
        /// Lista de slots de material capturados
        /// </summary>
        public List<MRMaterialSlot> Slots => _slots;

        /// <summary>
        /// Lista de grupos de materiales
        /// </summary>
        public List<MRMaterialGroup> Groups => _groups;

        /// <summary>
        /// Cantidad de slots
        /// </summary>
        public int SlotCount => _slots?.Count ?? 0;

        /// <summary>
        /// Cantidad de grupos
        /// </summary>
        public int GroupCount => _groups?.Count ?? 0;

        /// <summary>
        /// Cantidad de slots con grupo vinculado
        /// </summary>
        public int LinkedSlotsCount => _slots?.Count(s => s != null && s.HasLinkedGroup) ?? 0;

        /// <summary>
        /// Cantidad de slots sin grupo vinculado
        /// </summary>
        public int UnlinkedSlotsCount => SlotCount - LinkedSlotsCount;

        #endregion

        #region Slot Management

        /// <summary>
        /// Escanea un GameObject y extrae todos los slots de material de sus renderers
        /// </summary>
        /// <param name="targetObject">GameObject a escanear</param>
        /// <param name="includeChildren">Incluir hijos recursivamente</param>
        /// <returns>Cantidad de slots añadidos</returns>
        public int ScanGameObject(GameObject targetObject, bool includeChildren = true)
        {
            if (targetObject == null) return 0;

            int addedCount = 0;
            Renderer[] renderers;

            if (includeChildren)
            {
                renderers = targetObject.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                renderers = targetObject.GetComponents<Renderer>();
            }

            foreach (var renderer in renderers)
            {
                addedCount += AddRendererSlots(renderer);
            }

            return addedCount;
        }

        /// <summary>
        /// Añade todos los slots de material de un renderer
        /// </summary>
        /// <param name="renderer">Renderer a procesar</param>
        /// <returns>Cantidad de slots añadidos</returns>
        public int AddRendererSlots(Renderer renderer)
        {
            if (renderer == null) return 0;

            // Solo procesar MeshRenderer y SkinnedMeshRenderer
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                return 0;

            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return 0;

            int addedCount = 0;
            for (int i = 0; i < materials.Length; i++)
            {
                if (AddSlot(renderer, i))
                    addedCount++;
            }

            return addedCount;
        }

        /// <summary>
        /// Añade un slot de material específico
        /// </summary>
        /// <param name="renderer">Renderer objetivo</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <returns>True si se añadió, false si ya existía</returns>
        public bool AddSlot(Renderer renderer, int materialIndex)
        {
            if (renderer == null) return false;

            // Verificar si ya existe
            if (FindSlot(renderer, materialIndex) != null)
                return false;

            var newSlot = new MRMaterialSlot(renderer, materialIndex);
            _slots.Add(newSlot);
            return true;
        }

        /// <summary>
        /// Elimina un slot específico
        /// </summary>
        /// <param name="slot">Slot a eliminar</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveSlot(MRMaterialSlot slot)
        {
            if (slot == null) return false;
            return _slots.Remove(slot);
        }

        /// <summary>
        /// Elimina un slot por renderer e índice
        /// </summary>
        /// <param name="renderer">Renderer del slot</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveSlot(Renderer renderer, int materialIndex)
        {
            var slot = FindSlot(renderer, materialIndex);
            if (slot == null) return false;
            return _slots.Remove(slot);
        }

        /// <summary>
        /// Busca un slot por renderer e índice
        /// </summary>
        /// <param name="renderer">Renderer a buscar</param>
        /// <param name="materialIndex">Índice del material</param>
        /// <returns>Slot encontrado o null</returns>
        public MRMaterialSlot FindSlot(Renderer renderer, int materialIndex)
        {
            if (renderer == null) return null;
            return _slots.FirstOrDefault(s => s != null && s.Matches(renderer, materialIndex));
        }

        /// <summary>
        /// Elimina todos los slots
        /// </summary>
        public void ClearAllSlots()
        {
            _slots.Clear();
        }

        /// <summary>
        /// Elimina los slots inválidos (con renderer nulo)
        /// </summary>
        /// <returns>Cantidad de slots eliminados</returns>
        public int RemoveInvalidSlots()
        {
            int initialCount = _slots.Count;
            _slots.RemoveAll(s => s == null || !s.IsValid);
            return initialCount - _slots.Count;
        }

        /// <summary>
        /// Obtiene los slots con grupo vinculado
        /// </summary>
        /// <returns>Lista de slots vinculados</returns>
        public List<MRMaterialSlot> GetLinkedSlots()
        {
            return _slots.Where(s => s != null && s.HasLinkedGroup).ToList();
        }

        /// <summary>
        /// Obtiene los slots sin grupo vinculado
        /// </summary>
        /// <returns>Lista de slots sin vincular</returns>
        public List<MRMaterialSlot> GetUnlinkedSlots()
        {
            return _slots.Where(s => s != null && !s.HasLinkedGroup).ToList();
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Crea un nuevo grupo con los materiales proporcionados
        /// </summary>
        /// <param name="materials">Materiales iniciales del grupo</param>
        /// <param name="groupName">Nombre opcional del grupo</param>
        /// <returns>Grupo creado</returns>
        public MRMaterialGroup CreateGroup(IEnumerable<Material> materials, string groupName = null)
        {
            int newIndex = GetNextGroupIndex();
            var newGroup = new MRMaterialGroup(newIndex, groupName);
            newGroup.AddMaterials(materials);
            _groups.Add(newGroup);
            return newGroup;
        }

        /// <summary>
        /// Crea un nuevo grupo vacío
        /// </summary>
        /// <param name="groupName">Nombre opcional del grupo</param>
        /// <returns>Grupo creado</returns>
        public MRMaterialGroup CreateEmptyGroup(string groupName = null)
        {
            int newIndex = GetNextGroupIndex();
            var newGroup = new MRMaterialGroup(newIndex, groupName);
            _groups.Add(newGroup);
            return newGroup;
        }

        /// <summary>
        /// Elimina un grupo
        /// </summary>
        /// <param name="group">Grupo a eliminar</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveGroup(MRMaterialGroup group)
        {
            if (group == null) return false;

            // Desvincular todos los slots que usan este grupo
            foreach (var slot in _slots.Where(s => s != null && s.LinkedGroupIndex == group.GroupIndex))
            {
                slot.UnlinkGroup();
            }

            return _groups.Remove(group);
        }

        /// <summary>
        /// Elimina un grupo por índice
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveGroupByIndex(int groupIndex)
        {
            var group = FindGroupByIndex(groupIndex);
            return RemoveGroup(group);
        }

        /// <summary>
        /// Busca un grupo por su índice
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        /// <returns>Grupo encontrado o null</returns>
        public MRMaterialGroup FindGroupByIndex(int groupIndex)
        {
            return _groups.FirstOrDefault(g => g != null && g.GroupIndex == groupIndex);
        }

        /// <summary>
        /// Busca el grupo que contiene un material específico
        /// </summary>
        /// <param name="material">Material a buscar</param>
        /// <returns>Grupo que contiene el material o null</returns>
        public MRMaterialGroup FindGroupContainingMaterial(Material material)
        {
            if (material == null) return null;
            return _groups.FirstOrDefault(g => g != null && g.ContainsMaterial(material));
        }

        /// <summary>
        /// Elimina todos los grupos
        /// </summary>
        public void ClearAllGroups()
        {
            // Desvincular todos los slots primero
            foreach (var slot in _slots.Where(s => s != null))
            {
                slot.UnlinkGroup();
            }
            _groups.Clear();
        }

        /// <summary>
        /// Elimina los grupos vacíos
        /// </summary>
        /// <returns>Cantidad de grupos eliminados</returns>
        public int RemoveEmptyGroups()
        {
            var emptyGroups = _groups.Where(g => g == null || !g.HasMaterials).ToList();
            foreach (var group in emptyGroups)
            {
                RemoveGroup(group);
            }
            return emptyGroups.Count;
        }

        /// <summary>
        /// Obtiene el siguiente índice disponible para un grupo
        /// </summary>
        /// <returns>Siguiente índice</returns>
        private int GetNextGroupIndex()
        {
            if (_groups == null || _groups.Count == 0) return 0;
            return _groups.Max(g => g?.GroupIndex ?? -1) + 1;
        }

        #endregion

        #region Auto-Linking

        /// <summary>
        /// Detecta y vincula automáticamente los slots con sus grupos correspondientes.
        /// Un slot se vincula a un grupo si su material actual está en ese grupo.
        /// </summary>
        /// <returns>Cantidad de slots vinculados</returns>
        public int DetectAndLinkSlots()
        {
            int linkedCount = 0;

            foreach (var slot in _slots.Where(s => s != null && s.IsValid))
            {
                var currentMaterial = slot.CurrentMaterial;
                if (currentMaterial == null) continue;

                var matchingGroup = FindGroupContainingMaterial(currentMaterial);
                if (matchingGroup != null)
                {
                    slot.LinkToGroup(matchingGroup.GroupIndex);
                    linkedCount++;
                }
                else
                {
                    slot.UnlinkGroup();
                }
            }

            return linkedCount;
        }

        /// <summary>
        /// Desvincula todos los slots
        /// </summary>
        public void UnlinkAllSlots()
        {
            foreach (var slot in _slots.Where(s => s != null))
            {
                slot.UnlinkGroup();
            }
        }

        /// <summary>
        /// Obtiene el grupo vinculado a un slot
        /// </summary>
        /// <param name="slot">Slot a consultar</param>
        /// <returns>Grupo vinculado o null</returns>
        public MRMaterialGroup GetGroupForSlot(MRMaterialSlot slot)
        {
            if (slot == null || !slot.HasLinkedGroup) return null;
            return FindGroupByIndex(slot.LinkedGroupIndex);
        }

        /// <summary>
        /// Obtiene todos los slots vinculados a un grupo
        /// </summary>
        /// <param name="group">Grupo a consultar</param>
        /// <returns>Lista de slots vinculados</returns>
        public List<MRMaterialSlot> GetSlotsForGroup(MRMaterialGroup group)
        {
            if (group == null) return new List<MRMaterialSlot>();
            return _slots.Where(s => s != null && s.LinkedGroupIndex == group.GroupIndex).ToList();
        }

        #endregion

        #region Hierarchy Path Updates

        /// <summary>
        /// Actualiza las rutas jerárquicas de todos los slots
        /// </summary>
        /// <param name="root">Transform raíz (avatar root)</param>
        public void UpdateAllHierarchyPaths(Transform root = null)
        {
            foreach (var slot in _slots.Where(s => s != null))
            {
                slot.UpdateHierarchyPath(root);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valida el estado del componente
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public override ValidationResult Validate()
        {
            var result = new ValidationResult("MR Agrupar Materiales");

            // Validar que haya slots
            if (SlotCount == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay slots de material capturados"));
            }

            // Validar slots inválidos
            int invalidSlots = _slots.Count(s => s == null || !s.IsValid);
            if (invalidSlots > 0)
            {
                result.AddChild(ValidationResult.Warning($"{invalidSlots} slots tienen referencias inválidas"));
            }

            // Validar grupos
            if (GroupCount == 0)
            {
                result.AddChild(ValidationResult.Info("No hay grupos de materiales definidos"));
            }
            else
            {
                // Validar grupos con menos de 2 materiales
                int invalidGroups = _groups.Count(g => g == null || !g.IsValid);
                if (invalidGroups > 0)
                {
                    result.AddChild(ValidationResult.Warning($"{invalidGroups} grupos tienen menos de 2 materiales"));
                }
            }

            // Validar slots sin vincular
            if (UnlinkedSlotsCount > 0 && GroupCount > 0)
            {
                result.AddChild(ValidationResult.Info($"{UnlinkedSlotsCount} slots no tienen grupo vinculado (no se animarán)"));
            }

            // Si todo está bien
            if (result.Children.Count == 0)
            {
                result.AddChild(ValidationResult.Success("Configuración válida"));
            }

            return result;
        }

        #endregion

        #region Unity Lifecycle

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            if (_slots == null) _slots = new List<MRMaterialSlot>();
            if (_groups == null) _groups = new List<MRMaterialGroup>();
        }

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();
            // Limpieza automática de referencias nulas en el editor
        }
#endif

        #endregion
    }
}
