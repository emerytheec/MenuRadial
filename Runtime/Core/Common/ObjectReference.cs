using System;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Representa una referencia a un GameObject con su estado de activación
    /// Refactorizada para usar ReferenceBase - elimina código duplicado
    /// </summary>
    [Serializable]
    public class ObjectReference : ReferenceBase<GameObject>, IReferenceBase<UnityEngine.Object>
    {
        [SerializeField] private bool _isActive = true;
        
        /// <summary>
        /// GameObject referenciado (alias para mejor legibilidad)
        /// </summary>
        public GameObject GameObject 
        { 
            get => Target; 
            set => Target = value;
        }
        
        /// <summary>
        /// Estado de activación deseado para este objeto
        /// </summary>
        public bool IsActive 
        { 
            get => _isActive; 
            set => _isActive = value; 
        }
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ObjectReference() : base()
        {
        }
        
        /// <summary>
        /// Constructor con GameObject y estado
        /// </summary>
        /// <param name="gameObject">GameObject a referenciar</param>
        /// <param name="isActive">Estado de activación deseado</param>
        public ObjectReference(GameObject gameObject, bool isActive = true) : base(gameObject)
        {
            _isActive = isActive;
        }
        
        /// <summary>
        /// Aplica el estado de activación al GameObject
        /// Implementación del método abstracto Apply()
        /// </summary>
        public override void Apply()
        {
            if (IsValid)
            {
                Target.SetActive(_isActive);
            }
            else
            {
            }
        }
        
        /// <summary>
        /// Captura el estado actual del GameObject en la escena
        /// Implementación del método abstracto CaptureCurrentState()
        /// </summary>
        public override void CaptureCurrentState()
        {
            if (IsValid)
            {
                bool currentState = Target.activeSelf;
                _isActive = currentState;
            }
        }
        
        /// <summary>
        /// Aplica el estado de activación al GameObject (método para compatibilidad)
        /// </summary>
        [System.Obsolete("Use Apply() instead. This method is kept for backward compatibility.")]
        public void ApplyState()
        {
            Apply();
        }
        
        /// <summary>
        /// Implementación explícita de IReferenceBase<UnityEngine.Object> para compatibilidad con genéricos
        /// </summary>
        UnityEngine.Object IReferenceBase<UnityEngine.Object>.Target 
        { 
            get => Target; 
            set => Target = value as GameObject; 
        }
        
        /// <summary>
        /// Representación como string 
        /// </summary>
        /// <returns>String descriptivo del objeto</returns>
        public override string ToString()
        {
            var targetName = Target != null ? Target.name : "[Missing]";
            return $"ObjectRef: {targetName} ({(_isActive ? "Active" : "Inactive")}) - {(IsValid ? "Valid" : "Invalid")}";
        }
    }
}
