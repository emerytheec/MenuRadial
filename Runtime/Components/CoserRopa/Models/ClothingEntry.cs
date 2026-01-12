using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Models
{
    /// <summary>
    /// Representa una prenda de ropa detectada dentro del avatar.
    /// Contiene la referencia al GameObject, su armature y estado de seleccion.
    /// </summary>
    [Serializable]
    public class ClothingEntry
    {
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private string _name;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private ArmatureReference _armatureReference;
        [SerializeField] private List<BoneMapping> _boneMappings;
        [SerializeField] private StitchingResult _lastResult;

        [SerializeField] private string _bonePrefix = "";
        [SerializeField] private string _boneSuffix = "";

        /// <summary>
        /// GameObject raiz de la ropa
        /// </summary>
        public GameObject GameObject
        {
            get => _gameObject;
            set
            {
                _gameObject = value;
                _name = value != null ? value.name : "";
            }
        }

        /// <summary>
        /// Nombre de la ropa (para mostrar en UI)
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Indica si esta ropa esta habilitada para coser
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Referencia al armature de la ropa
        /// </summary>
        public ArmatureReference ArmatureReference
        {
            get => _armatureReference;
            set => _armatureReference = value;
        }

        /// <summary>
        /// Mapeos de huesos detectados para esta ropa
        /// </summary>
        public List<BoneMapping> BoneMappings
        {
            get => _boneMappings;
            set => _boneMappings = value;
        }

        /// <summary>
        /// Resultado del ultimo cosido de esta ropa
        /// </summary>
        public StitchingResult LastResult
        {
            get => _lastResult;
            set => _lastResult = value;
        }

        /// <summary>
        /// Indica si la ropa es valida (tiene GameObject y armature)
        /// </summary>
        public bool IsValid => _gameObject != null &&
                               _armatureReference != null &&
                               _armatureReference.IsValid;

        /// <summary>
        /// Cantidad de huesos mapeados correctamente
        /// </summary>
        public int MappedBoneCount => _boneMappings?.FindAll(m => m.IsValid).Count ?? 0;

        /// <summary>
        /// Cantidad total de mapeos
        /// </summary>
        public int TotalBoneCount => _boneMappings?.Count ?? 0;

        /// <summary>
        /// Indica si tiene mapeos validos
        /// </summary>
        public bool HasValidMappings => MappedBoneCount > 0;

        /// <summary>
        /// Indica si fue cosida exitosamente
        /// </summary>
        public bool WasStitched => _lastResult != null && _lastResult.Success;

        /// <summary>
        /// Prefijo en los nombres de huesos de esta ropa (ej: "Outfit_")
        /// Se elimina durante el matching para mejor detección
        /// </summary>
        public string BonePrefix
        {
            get => _bonePrefix ?? "";
            set => _bonePrefix = value ?? "";
        }

        /// <summary>
        /// Sufijo en los nombres de huesos de esta ropa (ej: ".001")
        /// Se elimina durante el matching para mejor detección
        /// </summary>
        public string BoneSuffix
        {
            get => _boneSuffix ?? "";
            set => _boneSuffix = value ?? "";
        }

        /// <summary>
        /// Indica si tiene prefijo o sufijo configurado
        /// </summary>
        public bool HasCustomNaming => !string.IsNullOrEmpty(_bonePrefix) || !string.IsNullOrEmpty(_boneSuffix);

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ClothingEntry()
        {
            _boneMappings = new List<BoneMapping>();
        }

        /// <summary>
        /// Constructor con GameObject
        /// </summary>
        public ClothingEntry(GameObject gameObject) : this()
        {
            GameObject = gameObject;
            _armatureReference = new ArmatureReference(gameObject);
        }

        /// <summary>
        /// Limpia los mapeos de huesos
        /// </summary>
        public void ClearMappings()
        {
            _boneMappings?.Clear();
            _lastResult = null;
        }
    }
}
