using System;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Utils;

namespace Bender_Dios.MenuRadial.Core.Common
{
    /// <summary>
    /// Clase base abstracta que implementa funcionalidad común para todas las referencias
    /// Elimina código duplicado y centraliza la lógica de rutas jerárquicas
    /// </summary>
    /// <typeparam name="T">Tipo del objeto referenciado</typeparam>
    [Serializable]
    public abstract class ReferenceBase<T> : IReferenceBase<T> where T : UnityEngine.Object
    {
        [SerializeField] protected T _target;
        [SerializeField] protected string _hierarchyPath = "";
        
        /// <summary>
        /// Objeto objetivo de la referencia
        /// </summary>
        public virtual T Target 
        { 
            get => _target; 
            set 
            { 
                _target = value;
                UpdateHierarchyPath();
            } 
        }
        
        /// <summary>
        /// Ruta jerárquica del objeto
        /// </summary>
        public string HierarchyPath => _hierarchyPath;
        
        /// <summary>
        /// Indica si la referencia es válida (implementación base)
        /// Las clases derivadas pueden sobreescribir para validaciones específicas
        /// </summary>
        public virtual bool IsValid => _target != null;
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        protected ReferenceBase()
        {
        }
        
        /// <summary>
        /// Constructor con target
        /// </summary>
        /// <param name="target">Objeto objetivo</param>
        protected ReferenceBase(T target)
        {
            _target = target;
            UpdateHierarchyPath();
        }
        
        /// <summary>
        /// Actualiza la ruta jerárquica del objeto usando HierarchyPathHelper
        /// Centraliza esta funcionalidad que antes estaba duplicada
        /// </summary>
        public virtual void UpdateHierarchyPath()
        {
            if (_target != null)
            {
                // Usar HierarchyPathHelper existente para consistencia
                if (_target is GameObject go)
                {
                    _hierarchyPath = HierarchyPathHelper.GetHierarchyPath(go);
                }
                else if (_target is Component comp)
                {
                    _hierarchyPath = HierarchyPathHelper.GetHierarchyPath(comp);
                }
                else
                {
                    _hierarchyPath = _target.name;
                }
            }
            else
            {
                _hierarchyPath = "[Missing Reference]";
            }
        }
        
        /// <summary>
        /// Aplica el estado/configuración de esta referencia
        /// Método abstracto que debe ser implementado por clases derivadas
        /// </summary>
        public abstract void Apply();
        
        /// <summary>
        /// Captura el estado actual del objeto en la escena
        /// Método abstracto que debe ser implementado por clases derivadas
        /// </summary>
        public abstract void CaptureCurrentState();
        
        /// <summary>
        /// Validación base que verifica que el target existe
        /// Las clases derivadas pueden agregar validaciones específicas
        /// </summary>
        /// <returns>True si la referencia es válida</returns>
        protected virtual bool ValidateTarget()
        {
            return _target != null;
        }
        
        /// <summary>
        /// Obtiene el Transform del objeto para operaciones de jerarquía
        /// Maneja tanto GameObjects como Components
        /// </summary>
        /// <returns>Transform del objeto o null</returns>
        protected Transform GetTransform()
        {
            if (_target is GameObject go)
                return go.transform;
            else if (_target is Component comp)
                return comp.transform;
            
            return null;
        }
        
        /// <summary>
        /// Representación como string
        /// </summary>
        /// <returns>String descriptivo de la referencia</returns>
        public override string ToString()
        {
            var targetName = _target != null ? _target.name : "[Missing]";
            var type = GetType().Name;
            return $"{type}: {targetName} ({(_target != null ? "Valid" : "Invalid")})";
        }
        
        /// <summary>
        /// Compara dos referencias por su target
        /// </summary>
        /// <param name="obj">Objeto a comparar</param>
        /// <returns>True si referencian el mismo target</returns>
        public override bool Equals(object obj)
        {
            if (obj is ReferenceBase<T> other)
            {
                return _target == other._target;
            }
            return false;
        }
        
        /// <summary>
        /// Hash code basado en el target
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return _target != null ? _target.GetHashCode() : 0;
        }
    }
}
