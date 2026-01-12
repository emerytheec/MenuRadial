using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.AlternativeMaterial
{
    /// <summary>
    /// Representa un grupo de materiales alternativos.
    /// Los materiales en un grupo son variaciones intercambiables (ej: diferentes colores).
    /// </summary>
    [Serializable]
    public class MRMaterialGroup
    {
        [SerializeField] private int _groupIndex;
        [SerializeField] private string _groupName;
        [SerializeField] private List<Material> _materials = new List<Material>();

        /// <summary>
        /// Índice único del grupo
        /// </summary>
        public int GroupIndex
        {
            get => _groupIndex;
            set => _groupIndex = value;
        }

        /// <summary>
        /// Nombre personalizado del grupo (opcional)
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set => _groupName = value;
        }

        /// <summary>
        /// Lista de materiales en el grupo
        /// </summary>
        public List<Material> Materials => _materials;

        /// <summary>
        /// Cantidad de materiales en el grupo
        /// </summary>
        public int MaterialCount => _materials?.Count ?? 0;

        /// <summary>
        /// Indica si el grupo tiene materiales válidos (al menos 2)
        /// </summary>
        public bool IsValid => MaterialCount >= 2;

        /// <summary>
        /// Indica si el grupo tiene al menos un material
        /// </summary>
        public bool HasMaterials => MaterialCount > 0;

        /// <summary>
        /// Nombre para mostrar en el inspector
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_groupName))
                    return $"Grupo {_groupIndex}: {_groupName}";
                return $"Grupo {_groupIndex}";
            }
        }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public MRMaterialGroup()
        {
            _materials = new List<Material>();
        }

        /// <summary>
        /// Constructor con índice
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        public MRMaterialGroup(int groupIndex)
        {
            _groupIndex = groupIndex;
            _materials = new List<Material>();
        }

        /// <summary>
        /// Constructor con índice y nombre
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        /// <param name="groupName">Nombre del grupo</param>
        public MRMaterialGroup(int groupIndex, string groupName)
        {
            _groupIndex = groupIndex;
            _groupName = groupName;
            _materials = new List<Material>();
        }

        /// <summary>
        /// Constructor con materiales iniciales
        /// </summary>
        /// <param name="groupIndex">Índice del grupo</param>
        /// <param name="initialMaterials">Materiales iniciales</param>
        public MRMaterialGroup(int groupIndex, IEnumerable<Material> initialMaterials)
        {
            _groupIndex = groupIndex;
            _materials = initialMaterials?.Where(m => m != null).ToList() ?? new List<Material>();
        }

        /// <summary>
        /// Añade un material al grupo
        /// </summary>
        /// <param name="material">Material a añadir</param>
        /// <returns>True si se añadió, false si ya existía o es null</returns>
        public bool AddMaterial(Material material)
        {
            if (material == null) return false;
            if (_materials.Contains(material)) return false;

            _materials.Add(material);
            return true;
        }

        /// <summary>
        /// Añade múltiples materiales al grupo
        /// </summary>
        /// <param name="materials">Materiales a añadir</param>
        /// <returns>Cantidad de materiales añadidos</returns>
        public int AddMaterials(IEnumerable<Material> materials)
        {
            if (materials == null) return 0;

            int added = 0;
            foreach (var material in materials)
            {
                if (AddMaterial(material))
                    added++;
            }
            return added;
        }

        /// <summary>
        /// Elimina un material del grupo
        /// </summary>
        /// <param name="material">Material a eliminar</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveMaterial(Material material)
        {
            if (material == null) return false;
            return _materials.Remove(material);
        }

        /// <summary>
        /// Elimina un material por índice
        /// </summary>
        /// <param name="index">Índice del material</param>
        /// <returns>True si se eliminó</returns>
        public bool RemoveMaterialAt(int index)
        {
            if (index < 0 || index >= _materials.Count) return false;
            _materials.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Verifica si el grupo contiene un material específico.
        /// Compara por referencia y por asset path (editor).
        /// NO usa comparación por nombre ya que diferentes materiales pueden tener el mismo nombre.
        /// </summary>
        /// <param name="material">Material a buscar</param>
        /// <returns>True si lo contiene</returns>
        public bool ContainsMaterial(Material material)
        {
            if (material == null) return false;

            // Comparación rápida por referencia primero
            if (_materials.Contains(material)) return true;

            // Comparación por asset path (precisa en editor)
            #if UNITY_EDITOR
            string searchPath = UnityEditor.AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(searchPath))
            {
                foreach (var m in _materials)
                {
                    if (m == null) continue;
                    string groupMaterialPath = UnityEditor.AssetDatabase.GetAssetPath(m);
                    if (searchPath == groupMaterialPath)
                        return true;
                }
            }
            #endif

            // NO usar fallback por nombre - puede causar falsos positivos
            // cuando diferentes materiales tienen el mismo nombre
            return false;
        }

        /// <summary>
        /// Obtiene el índice de un material dentro del grupo.
        /// Compara por referencia y por asset path (editor).
        /// NO usa comparación por nombre ya que diferentes materiales pueden tener el mismo nombre.
        /// </summary>
        /// <param name="material">Material a buscar</param>
        /// <returns>Índice del material o -1 si no existe</returns>
        public int GetMaterialIndex(Material material)
        {
            if (material == null) return -1;

            // Comparación rápida por referencia primero
            int index = _materials.IndexOf(material);
            if (index >= 0) return index;

            // Comparación por asset path (precisa en editor)
            #if UNITY_EDITOR
            string searchPath = UnityEditor.AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(searchPath))
            {
                for (int i = 0; i < _materials.Count; i++)
                {
                    var m = _materials[i];
                    if (m == null) continue;
                    string groupMaterialPath = UnityEditor.AssetDatabase.GetAssetPath(m);
                    if (searchPath == groupMaterialPath)
                        return i;
                }
            }
            #endif

            // NO usar fallback por nombre - puede causar falsos positivos
            return -1;
        }

        /// <summary>
        /// Obtiene un material por su índice
        /// </summary>
        /// <param name="index">Índice del material</param>
        /// <returns>Material o null si el índice es inválido</returns>
        public Material GetMaterialAt(int index)
        {
            if (index < 0 || index >= _materials.Count) return null;
            return _materials[index];
        }

        /// <summary>
        /// Limpia todos los materiales del grupo
        /// </summary>
        public void ClearMaterials()
        {
            _materials.Clear();
        }

        /// <summary>
        /// Elimina materiales nulos de la lista
        /// </summary>
        /// <returns>Cantidad de materiales nulos eliminados</returns>
        public int RemoveNullMaterials()
        {
            int initialCount = _materials.Count;
            _materials.RemoveAll(m => m == null);
            return initialCount - _materials.Count;
        }

        /// <summary>
        /// Mueve un material a una nueva posición
        /// </summary>
        /// <param name="fromIndex">Índice actual</param>
        /// <param name="toIndex">Nuevo índice</param>
        /// <returns>True si se movió correctamente</returns>
        public bool MoveMaterial(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _materials.Count) return false;
            if (toIndex < 0 || toIndex >= _materials.Count) return false;
            if (fromIndex == toIndex) return true;

            var material = _materials[fromIndex];
            _materials.RemoveAt(fromIndex);
            _materials.Insert(toIndex, material);
            return true;
        }

        /// <summary>
        /// Obtiene todos los materiales válidos (no nulos)
        /// </summary>
        /// <returns>Lista de materiales válidos</returns>
        public List<Material> GetValidMaterials()
        {
            return _materials.Where(m => m != null).ToList();
        }

        public override string ToString()
        {
            return $"{DisplayName} ({MaterialCount} materiales)";
        }
    }
}
