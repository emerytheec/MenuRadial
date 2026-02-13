using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Bender_Dios.MenuRadial.Core.Preview
{
    /// <summary>
    /// Manager central para gestionar la previsualización en el sistema MR Control Menu
    /// Asegura que solo un componente tenga preview activo a la vez
    /// PATRÓN: Singleton para gestión centralizada de estado
    /// </summary>
    public static class PreviewManager
    {
        
        /// <summary>
        /// Componente que tiene preview activo actualmente (solo uno a la vez)
        /// </summary>
        private static IPreviewable _currentActivePreview;
        
        /// <summary>
        /// Lista de componentes registrados usando WeakReferences para evitar memory leaks
        /// REFACTORIZADO [2025-07-04]: Evita referencias a objetos destruidos
        /// OPTIMIZADO [2025-07-04]: Con cache para evitar reconteos frecuentes
        /// </summary>
        private static readonly List<System.WeakReference> _registeredComponents = new List<System.WeakReference>();
        


        /// <summary>
        /// Componente que tiene preview activo actualmente
        /// </summary>
        public static IPreviewable CurrentActivePreview => _currentActivePreview;
        
        /// <summary>
        /// Si hay algún preview activo
        /// </summary>
        public static bool HasActivePreview => _currentActivePreview != null;
        


        /// <summary>
        /// Activa un preview específico, desactivando cualquier preview anterior
        /// </summary>
        /// <param name="preview">Componente IPreviewable a activar</param>
        /// <param name="menuContext">Contexto del menú que solicita la activación (opcional)</param>
        public static void ActivatePreview(IPreviewable preview, object menuContext = null)
        {
            if (preview == null)
                return;

            // Si el mismo preview ya está activo, no hacer nada
            if (_currentActivePreview == preview)
                return;

            // Desactivar preview anterior si existe
            DeactivateCurrentPreview();

            // Activar nuevo preview
            preview.ActivatePreview();
            _currentActivePreview = preview;

            // Registrar componente si no está ya registrado
            RegisterComponent(preview);
        }
        
        /// <summary>
        /// Desactiva el preview activo actual
        /// </summary>
        public static void DeactivateCurrentPreview()
        {
            if (_currentActivePreview == null)
                return;

            _currentActivePreview.DeactivatePreview();
            _currentActivePreview = null;
        }
        
        /// <summary>
        /// Alterna el estado de un preview (activar si está inactivo, desactivar si está activo)
        /// </summary>
        /// <param name="preview">Componente a alternar</param>
        /// <param name="menuContext">Contexto del menú (opcional)</param>
        public static void TogglePreview(IPreviewable preview, object menuContext = null)
        {
            if (preview == null)
                return;
                
            if (_currentActivePreview == preview)
            {
                DeactivateCurrentPreview();
            }
            else
            {
                ActivatePreview(preview, menuContext);
            }
        }
        
        
        
        /// <summary>
        /// Registra un componente para tracking usando WeakReference (automático al activar)
        /// </summary>
        /// <param name="preview">Componente a registrar</param>
        public static void RegisterComponent(IPreviewable preview)
        {
            if (preview == null)
                return;
            
            // Limpiar referencias muertas primero
            CleanupDestroyedComponents();
            
            // Verificar si ya está registrado - OPTIMIZADO: sin LINQ
            if (_registeredComponents.Any(wr => wr.IsAlive && wr.Target == preview))
                return;

            _registeredComponents.Add(new System.WeakReference(preview));
        }
        
        /// <summary>
        /// Desregistra un componente
        /// </summary>
        /// <param name="preview">Componente a desregistrar</param>
        public static void UnregisterComponent(IPreviewable preview)
        {
            if (preview == null)
                return;

            // Si es el preview activo, desactivarlo primero
            if (_currentActivePreview == preview)
            {
                DeactivateCurrentPreview();
            }

            // Remover WeakReference específica
            for (int i = _registeredComponents.Count - 1; i >= 0; i--)
            {
                var weakRef = _registeredComponents[i];
                if (weakRef.IsAlive && weakRef.Target == preview)
                {
                    _registeredComponents.RemoveAt(i);
                }
            }

        }
        
        /// <summary>
        /// Limpia automáticamente referencias a componentes destruidos
        /// NUEVO [2025-07-04]: Previene memory leaks
        /// </summary>
        private static void CleanupDestroyedComponents()
        {
            int originalCount = _registeredComponents.Count;
            _registeredComponents.RemoveAll(wr => !wr.IsAlive);
            
            int removedCount = originalCount - _registeredComponents.Count;
        }
        
        /// <summary>
        /// Limpia completamente el estado del PreviewManager
        /// Útil para testing o reset del sistema
        /// </summary>
        public static void ClearAll()
        {
            DeactivateCurrentPreview();
            _registeredComponents.Clear();
        }
        
        
        
        /// <summary>
        /// Método para llamar cuando Unity se cierra o cambia de escena
        /// Asegura limpieza apropiada
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            // Limpiar estado estático al cargar
            ClearAll();
        }
        
    }
}
