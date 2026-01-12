using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame.Preview
{
    /// <summary>
    /// Gestor especializado para el estado y persistencia de previsualización
    /// FASE 2: Extraído de FramePreviewService (200 líneas)
    /// Responsabilidad única: Solo gestión de estado y datos de previsualización
    /// </summary>
    public class PreviewStateManager
    {
        // Estados originales para restauración
        private List<ObjectReference> _originalObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        private bool _isPreviewActive = false;
        
        
        /// <summary>
        /// Indica si la previsualización está activa (basado en estados reales)
        /// </summary>
        public bool IsPreviewActive 
        { 
            get 
            {
                // La previsualización está activa si tenemos estados guardados
                bool hasObjectStates = _originalObjectStates != null && _originalObjectStates.Count > 0;
                bool hasMaterialStates = _originalMaterialStates != null && _originalMaterialStates.Count > 0;
                bool hasBlendshapeStates = _originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0;
                
                return _isPreviewActive && (hasObjectStates || hasMaterialStates || hasBlendshapeStates);
            }
        }
        
        /// <summary>
        /// Obtiene el número total de estados guardados
        /// </summary>
        public int SavedStatesCount => 
            (_originalObjectStates?.Count ?? 0) + 
            (_originalMaterialStates?.Count ?? 0) + 
            (_originalBlendshapeStates?.Count ?? 0);
        
        /// <summary>
        /// Indica si hay estados guardados realmente
        /// </summary>
        public bool HasSavedStates => SavedStatesCount > 0;
        
        /// <summary>
        /// Estadísticas detalladas de estados guardados
        /// </summary>
        public (int objects, int materials, int blendshapes) SavedStatesBreakdown => 
            (_originalObjectStates?.Count ?? 0, 
             _originalMaterialStates?.Count ?? 0, 
             _originalBlendshapeStates?.Count ?? 0);
        
        
        
        /// <summary>
        /// Guarda los estados originales de objetos
        /// </summary>
        /// <param name="objectStates">Estados de objetos a guardar</param>
        public void SaveObjectStates(List<ObjectReference> objectStates)
        {
            _originalObjectStates = objectStates ?? new List<ObjectReference>();
        }
        
        /// <summary>
        /// Guarda los estados originales de materiales
        /// </summary>
        /// <param name="materialStates">Estados de materiales a guardar</param>
        public void SaveMaterialStates(List<MaterialReference> materialStates)
        {
            _originalMaterialStates = materialStates ?? new List<MaterialReference>();
        }
        
        /// <summary>
        /// Guarda los estados originales de blendshapes
        /// </summary>
        /// <param name="blendshapeStates">Estados de blendshapes a guardar</param>
        public void SaveBlendshapeStates(List<BlendshapeReference> blendshapeStates)
        {
            _originalBlendshapeStates = blendshapeStates ?? new List<BlendshapeReference>();
        }
        
        /// <summary>
        /// Guarda todos los estados de una vez
        /// </summary>
        /// <param name="objectStates">Estados de objetos</param>
        /// <param name="materialStates">Estados de materiales</param>
        /// <param name="blendshapeStates">Estados de blendshapes</param>
        public void SaveAllStates(List<ObjectReference> objectStates, 
                                  List<MaterialReference> materialStates, 
                                  List<BlendshapeReference> blendshapeStates)
        {
            SaveObjectStates(objectStates);
            SaveMaterialStates(materialStates);
            SaveBlendshapeStates(blendshapeStates);
            
        }
        
        
        
        /// <summary>
        /// Obtiene los estados originales de objetos
        /// </summary>
        /// <returns>Lista de estados de objetos guardados</returns>
        public List<ObjectReference> GetObjectStates()
        {
            return _originalObjectStates ?? new List<ObjectReference>();
        }
        
        /// <summary>
        /// Obtiene los estados originales de materiales
        /// </summary>
        /// <returns>Lista de estados de materiales guardados</returns>
        public List<MaterialReference> GetMaterialStates()
        {
            return _originalMaterialStates ?? new List<MaterialReference>();
        }
        
        /// <summary>
        /// Obtiene los estados originales de blendshapes
        /// </summary>
        /// <returns>Lista de estados de blendshapes guardados</returns>
        public List<BlendshapeReference> GetBlendshapeStates()
        {
            return _originalBlendshapeStates ?? new List<BlendshapeReference>();
        }
        
        /// <summary>
        /// Obtiene una copia de todos los estados guardados
        /// </summary>
        /// <returns>Tupla con todos los estados</returns>
        public (List<ObjectReference> objects, List<MaterialReference> materials, List<BlendshapeReference> blendshapes) GetAllStates()
        {
            return (GetObjectStates(), GetMaterialStates(), GetBlendshapeStates());
        }
        
        
        
        /// <summary>
        /// Activa el estado de previsualización
        /// </summary>
        public void ActivatePreview()
        {
            _isPreviewActive = true;
        }
        
        /// <summary>
        /// Desactiva el estado de previsualización
        /// </summary>
        public void DeactivatePreview()
        {
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia todos los estados guardados
        /// </summary>
        public void ClearAllStates()
        {
            var totalCleared = SavedStatesCount;
            
            if (_originalObjectStates == null) _originalObjectStates = new List<ObjectReference>();
            if (_originalMaterialStates == null) _originalMaterialStates = new List<MaterialReference>();
            if (_originalBlendshapeStates == null) _originalBlendshapeStates = new List<BlendshapeReference>();
            
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
            
        }
        
        /// <summary>
        /// Limpia solo los estados de objetos
        /// </summary>
        public void ClearObjectStates()
        {
            if (_originalObjectStates == null) _originalObjectStates = new List<ObjectReference>();
            _originalObjectStates.Clear();
        }
        
        /// <summary>
        /// Limpia solo los estados de materiales
        /// </summary>
        public void ClearMaterialStates()
        {
            if (_originalMaterialStates == null) _originalMaterialStates = new List<MaterialReference>();
            _originalMaterialStates.Clear();
        }
        
        /// <summary>
        /// Limpia solo los estados de blendshapes
        /// </summary>
        public void ClearBlendshapeStates()
        {
            if (_originalBlendshapeStates == null) _originalBlendshapeStates = new List<BlendshapeReference>();
            _originalBlendshapeStates.Clear();
        }
        
        /// <summary>
        /// Reinicia completamente el gestor de estado
        /// </summary>
        public void Reset()
        {
            _isPreviewActive = false;
            ClearAllStates();
        }
        
        
        
        
    }
}
