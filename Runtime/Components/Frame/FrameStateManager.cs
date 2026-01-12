using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Gestor especializado para el estado y previsualización de frames
    /// FASE 1: Extraído de MRAgruparObjetos.cs para separar responsabilidades
    /// ANTES: Mezclado en 642 líneas | DESPUÉS: ~200 líneas especializadas
    /// Responsabilidad única: Solo gestión de estado y previsualización
    /// </summary>
    public class FrameStateManager
    {
        private readonly FrameData _frameData;
        
        // Sistema de previsualización - Extraído de MRAgruparObjetos
        private bool _isPreviewActive = false;
        private List<ObjectReference> _originalObjectStates = new List<ObjectReference>();
        private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        /// <summary>
        /// Constructor con inyección de dependencia del FrameData
        /// </summary>
        /// <param name="frameData">Datos del frame a gestionar</param>
        public FrameStateManager(FrameData frameData)
        {
            _frameData = frameData ?? throw new System.ArgumentNullException(nameof(frameData));
        }
        
        
        /// <summary>
        /// Indica si la previsualización está activa (basado en estados reales)
        /// EXTRAÍDO de MRAgruparObjetos.IsPreviewActive
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
        /// NUEVO: Estadística útil
        /// </summary>
        public int SavedStatesCount => 
            (_originalObjectStates?.Count ?? 0) + 
            (_originalMaterialStates?.Count ?? 0) + 
            (_originalBlendshapeStates?.Count ?? 0);
        
        /// <summary>
        /// Estados de objetos guardados para serialización
        /// EXTRAÍDO de MRAgruparObjetos para acceso externo
        /// </summary>
        public List<ObjectReference> OriginalObjectStates => _originalObjectStates;
        
        /// <summary>
        /// Estados de materiales guardados para serialización
        /// EXTRAÍDO de MRAgruparObjetos para acceso externo
        /// </summary>
        public List<MaterialReference> OriginalMaterialStates => _originalMaterialStates;
        
        /// <summary>
        /// Estados de blendshapes guardados para serialización
        /// EXTRAÍDO de MRAgruparObjetos para acceso externo
        /// </summary>
        public List<BlendshapeReference> OriginalBlendshapeStates => _originalBlendshapeStates;
        
        
        
        /// <summary>
        /// Aplica el estado completo del frame
        /// EXTRAÍDO de MRAgruparObjetos.ApplyCurrentFrame()
        /// </summary>
        public void ApplyFrameState()
        {
            if (_frameData == null)
            {
                return;
            }
            
            
            // Aplicar estado usando el método unificado de FrameData
            _frameData.ApplyState();
            
        }
        
        
        
        /// <summary>
        /// Previsualiza el frame aplicando su estado (toggle)
        /// EXTRAÍDO de MRAgruparObjetos.PreviewFrame()
        /// </summary>
        public void PreviewFrame()
        {
            // Verificar si realmente tenemos una previsualización activa
            bool hasOriginalStates = (_originalObjectStates != null && _originalObjectStates.Count > 0) ||
                                     (_originalMaterialStates != null && _originalMaterialStates.Count > 0) ||
                                     (_originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0);
            
            if (_isPreviewActive && hasOriginalStates)
            {
                // Cancelar previsualización - restaurar estados originales
                RestoreOriginalStates();
            }
            else
            {
                // Activar previsualización - guardar estados y aplicar frame
                StartPreview();
            }
        }
        
        /// <summary>
        /// Cancela la previsualización sin aplicar cambios
        /// EXTRAÍDO de MRAgruparObjetos.CancelPreview()
        /// </summary>
        public void CancelPreview()
        {
            if (_isPreviewActive)
            {
                RestoreOriginalStates();
            }
        }
        
        /// <summary>
        /// Inicia la previsualización guardando estados y aplicando frame
        /// NUEVO: Lógica de inicio extraída y optimizada
        /// </summary>
        private void StartPreview()
        {
            // Limpiar cualquier estado anterior inconsistente
            ClearSavedStates();
            
            // Guardar estados actuales
            SaveOriginalStates();
            
            // Aplicar frame
            ApplyFrameState();
            
            _isPreviewActive = true;
            
        }
        
        
        
        /// <summary>
        /// Guarda los estados originales de todos los objetos, materiales y blendshapes
        /// EXTRAÍDO de MRAgruparObjetos.SaveOriginalStates()
        /// </summary>
        private void SaveOriginalStates()
        {
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
            
            // Guardar estados de objetos
            foreach (var objRef in _frameData.ObjectReferences)
            {
                if (objRef.IsValid)
                {
                    // Capturar el estado actual del GameObject en la escena
                    bool currentState = objRef.GameObject.activeSelf;
                    var originalState = new ObjectReference(objRef.GameObject, currentState);
                    _originalObjectStates.Add(originalState);
                    
                }
            }
            
            // Guardar estados de materiales
            foreach (var matRef in _frameData.MaterialReferencesData)
            {
                if (matRef.IsValid)
                {
                    // Crear una copia del material reference con el material original actual
                    var originalMatRef = new MaterialReference(matRef.TargetRenderer, matRef.MaterialIndex);
                    _originalMaterialStates.Add(originalMatRef);
                    
                }
            }
            
            // Guardar estados de blendshapes
            foreach (var blendRef in _frameData.BlendshapeReferences)
            {
                if (blendRef.IsValid)
                {
                    // Capturar el valor actual del blendshape
                    float currentValue = blendRef.GetCurrentValue();
                    var originalBlendRef = new BlendshapeReference(blendRef.TargetRenderer, blendRef.BlendshapeName, currentValue);
                    _originalBlendshapeStates.Add(originalBlendRef);
                    
                }
            }
            
        }
        
        /// <summary>
        /// Restaura los estados originales de todos los objetos, materiales y blendshapes
        /// EXTRAÍDO de MRAgruparObjetos.RestoreOriginalStates()
        /// </summary>
        private void RestoreOriginalStates()
        {
            var restored = 0;
            
            // Restaurar objetos
            foreach (var originalState in _originalObjectStates)
            {
                if (originalState.IsValid)
                {
                    originalState.GameObject.SetActive(originalState.IsActive);
                    restored++;
                }
            }
            
            // Restaurar materiales
            foreach (var originalMatState in _originalMaterialStates)
            {
                if (originalMatState.IsValid)
                {
                    originalMatState.RestoreOriginalMaterial();
                    restored++;
                }
            }
            
            // Restaurar blendshapes
            foreach (var originalBlendState in _originalBlendshapeStates)
            {
                if (originalBlendState.IsValid)
                {
                    originalBlendState.Apply();
                    restored++;
                }
            }
            
            
            // Limpiar estados guardados
            ClearSavedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia todos los estados guardados
        /// NUEVO: Operación de limpieza centralizada
        /// </summary>
        private void ClearSavedStates()
        {
            if (_originalObjectStates == null) _originalObjectStates = new List<ObjectReference>();
            if (_originalMaterialStates == null) _originalMaterialStates = new List<MaterialReference>();
            if (_originalBlendshapeStates == null) _originalBlendshapeStates = new List<BlendshapeReference>();
            
            _originalObjectStates.Clear();
            _originalMaterialStates.Clear();
            _originalBlendshapeStates.Clear();
        }
        
        
        
        /// <summary>
        /// Sincroniza el estado interno con datos serializados externos
        /// NUEVO: Para mantener compatibilidad con MRAgruparObjetos serializado
        /// </summary>
        /// <param name="isPreviewActive">Estado de preview externo</param>
        /// <param name="originalObjectStates">Estados de objetos externos</param>
        /// <param name="originalMaterialStates">Estados de materiales externos</param>
        /// <param name="originalBlendshapeStates">Estados de blendshapes externos</param>
        public void SyncWithSerializedStates(bool isPreviewActive, 
                                           List<ObjectReference> originalObjectStates,
                                           List<MaterialReference> originalMaterialStates,
                                           List<BlendshapeReference> originalBlendshapeStates)
        {
            _isPreviewActive = isPreviewActive;
            _originalObjectStates = originalObjectStates ?? new List<ObjectReference>();
            _originalMaterialStates = originalMaterialStates ?? new List<MaterialReference>();
            _originalBlendshapeStates = originalBlendshapeStates ?? new List<BlendshapeReference>();
            
            // Verificar consistencia después de sincronizar
            if (HasInconsistentStates())
            {
                FixInconsistentStates();
            }
            
        }
        
        /// <summary>
        /// Actualiza los estados serializados con los valores internos
        /// NUEVO: Para sincronizar cambios de vuelta al MRAgruparObjetos
        /// </summary>
        /// <param name="isPreviewActive">Variable externa de preview</param>
        /// <param name="originalObjectStates">Lista externa de estados de objetos</param>
        /// <param name="originalMaterialStates">Lista externa de estados de materiales</param>
        /// <param name="originalBlendshapeStates">Lista externa de estados de blendshapes</param>
        public void UpdateSerializedStates(ref bool isPreviewActive, 
                                         ref List<ObjectReference> originalObjectStates,
                                         ref List<MaterialReference> originalMaterialStates,
                                         ref List<BlendshapeReference> originalBlendshapeStates)
        {
            isPreviewActive = _isPreviewActive;
            originalObjectStates = _originalObjectStates;
            originalMaterialStates = _originalMaterialStates;
            originalBlendshapeStates = _originalBlendshapeStates;
        }
        
        
        
        /// <summary>
        /// Verifica si hay estados guardados inconsistentes
        /// NUEVO: Validación de integridad
        /// </summary>
        public bool HasInconsistentStates()
        {
            if (_isPreviewActive)
            {
                bool hasObjectStates = _originalObjectStates != null && _originalObjectStates.Count > 0;
                bool hasMaterialStates = _originalMaterialStates != null && _originalMaterialStates.Count > 0;
                bool hasBlendshapeStates = _originalBlendshapeStates != null && _originalBlendshapeStates.Count > 0;
                
                bool hasObjectsInFrame = _frameData.ObjectReferences.Count > 0;
                bool hasMaterialsInFrame = _frameData.MaterialReferencesData.Count > 0;
                bool hasBlendshapesInFrame = _frameData.BlendshapeReferences.Count > 0;
                
                // Inconsistencia: tenemos elementos en el frame pero no estados guardados
                if ((hasObjectsInFrame && !hasObjectStates) ||
                    (hasMaterialsInFrame && !hasMaterialStates) ||
                    (hasBlendshapesInFrame && !hasBlendshapeStates))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Corrige estados inconsistentes
        /// NUEVO: Auto-corrección de estados
        /// </summary>
        public void FixInconsistentStates()
        {
            if (HasInconsistentStates())
            {
                
                if (_isPreviewActive)
                {
                    // Cancelar preview y limpiar estados
                    _isPreviewActive = false;
                    ClearSavedStates();
                }
                
            }
        }
        
        /// <summary>
        /// Fuerza la aplicación del frame sin guardar estados (para uso externo)
        /// NUEVO: Operación de aplicación directa
        /// </summary>
        public void ForceApplyFrame()
        {
            ApplyFrameState();
        }
        
        
        
        /// <summary>
        /// Inicializa el gestor con estados limpios
        /// NUEVO: Método de inicialización
        /// </summary>
        public void Initialize()
        {
            ClearSavedStates();
            _isPreviewActive = false;
        }
        
        /// <summary>
        /// Limpia el gestor y cancela cualquier preview activo
        /// NUEVO: Método de limpieza
        /// </summary>
        public void Cleanup()
        {
            if (_isPreviewActive)
            {
                CancelPreview();
            }
            ClearSavedStates();
        }
        
        
        
        
        
    }
}
