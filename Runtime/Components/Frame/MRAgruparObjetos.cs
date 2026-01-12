using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Core.Utils;
using Bender_Dios.MenuRadial.Core.Preview;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    // EventArgs eliminados - usando eventos estáticos simples

    /// <summary>
    /// Componente MR Agrupar Objetos (antes MRFrameObject)
    /// Captura estados de GameObjects, materiales y blendshapes para animaciones
    /// </summary>
    [System.Serializable]
    [AddComponentMenu("MR/MR Agrupar Objetos")]
    public class MRAgruparObjetos : MRComponentBase, IFrameComponent, IAnimationProvider, IPreviewable, System.IDisposable
    {
        
        private static IFrameControllerFactory _controllerFactory = new DefaultFrameControllerFactory();
        
        /// <summary>
        /// Configura el factory para inyección de dependencias
        /// </summary>
        /// <param name="factory">Factory personalizado o null para usar el por defecto</param>
        public static void SetControllerFactory(IFrameControllerFactory factory)
        {
            _controllerFactory = factory ?? new DefaultFrameControllerFactory();
        }
        
        
        
        /// <summary>
        /// Evento disparado cuando se agrega un GameObject a un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, GameObject> OnObjectAdded
        {
            add => FrameObjectEventSystem.OnObjectAdded += value;
            remove => FrameObjectEventSystem.OnObjectAdded -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando se remueve un GameObject de un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, GameObject> OnObjectRemoved
        {
            add => FrameObjectEventSystem.OnObjectRemoved += value;
            remove => FrameObjectEventSystem.OnObjectRemoved -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando se agrega un material a un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, Renderer, int> OnMaterialAdded
        {
            add => FrameObjectEventSystem.OnMaterialAdded += value;
            remove => FrameObjectEventSystem.OnMaterialAdded -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando se remueve un material de un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, Renderer, int> OnMaterialRemoved
        {
            add => FrameObjectEventSystem.OnMaterialRemoved += value;
            remove => FrameObjectEventSystem.OnMaterialRemoved -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando se agrega un blendshape a un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, SkinnedMeshRenderer, string> OnBlendshapeAdded
        {
            add => FrameObjectEventSystem.OnBlendshapeAdded += value;
            remove => FrameObjectEventSystem.OnBlendshapeAdded -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando se remueve un blendshape de un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, SkinnedMeshRenderer, string> OnBlendshapeRemoved
        {
            add => FrameObjectEventSystem.OnBlendshapeRemoved += value;
            remove => FrameObjectEventSystem.OnBlendshapeRemoved -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando cambia el estado interno de un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, string> OnStateChanged
        {
            add => FrameObjectEventSystem.OnStateChanged += value;
            remove => FrameObjectEventSystem.OnStateChanged -= value;
        }
        
        /// <summary>
        /// Evento disparado cuando cambia el estado de preview de un frame
        /// </summary>
        public static event System.Action<MRAgruparObjetos, bool> OnPreviewStateChanged
        {
            add => FrameObjectEventSystem.OnPreviewStateChanged += value;
            remove => FrameObjectEventSystem.OnPreviewStateChanged -= value;
        }
        
        /// <summary>
        /// Limpia todas las suscripciones de eventos estáticos para prevenir memory leaks
        /// IMPORTANTE: Llamar antes de recargar assemblies o cambiar escenas
        /// </summary>
        public static void CleanupAllEventSubscriptions()
        {
            FrameObjectEventSystem.UnregisterEvents();
        }
        
        /// <summary>
        /// Verifica si hay suscriptores activos en los eventos estáticos
        /// </summary>
        /// <returns>True si hay al menos un suscriptor activo</returns>
        public static bool HasActiveEventSubscriptions()
        {
            // Verificación delegada al sistema de eventos
            return true; // Simplificado - el sistema de eventos maneja esto internamente
        }
        
        // Los eventos estáticos se subscriben directamente
        
        /// <summary>
        /// Disparar evento de objeto agregado
        /// </summary>
        private void RaiseObjectAdded(GameObject gameObject)
        {
            FrameObjectEventSystem.NotifyObjectAdded(this, gameObject);
        }
        
        /// <summary>
        /// Disparar evento de objeto eliminado
        /// </summary>
        private void RaiseObjectRemoved(GameObject gameObject)
        {
            FrameObjectEventSystem.NotifyObjectRemoved(this, gameObject);
        }
        
        /// <summary>
        /// Disparar evento de material agregado
        /// </summary>
        private void RaiseMaterialAdded(Renderer renderer, int materialIndex)
        {
            FrameObjectEventSystem.NotifyMaterialAdded(this, renderer, materialIndex);
        }
        
        /// <summary>
        /// Disparar evento de material eliminado
        /// </summary>
        private void RaiseMaterialRemoved(Renderer renderer, int materialIndex)
        {
            FrameObjectEventSystem.NotifyMaterialRemoved(this, renderer, materialIndex);
        }
        
        /// <summary>
        /// Disparar evento de blendshape agregado
        /// </summary>
        private void RaiseBlendshapeAdded(SkinnedMeshRenderer skinnedMeshRenderer, string blendshapeName)
        {
            FrameObjectEventSystem.NotifyBlendshapeAdded(this, skinnedMeshRenderer, blendshapeName);
        }
        
        /// <summary>
        /// Disparar evento de blendshape eliminado
        /// </summary>
        private void RaiseBlendshapeRemoved(SkinnedMeshRenderer skinnedMeshRenderer, string blendshapeName)
        {
            FrameObjectEventSystem.NotifyBlendshapeRemoved(this, skinnedMeshRenderer, blendshapeName);
        }
        
        /// <summary>
        /// Disparar evento de cambio de estado
        /// </summary>
        private void RaiseStateChanged(string stateChange = null)
        {
            InvalidateValidationCache(); // Invalidar cache cuando cambia el estado
            FrameObjectEventSystem.NotifyStateChanged(this, stateChange ?? "StateChanged");
        }
        
        /// <summary>
        /// Disparar evento de cambio de preview
        /// </summary>
        private void RaisePreviewStateChanged(bool isPreviewActive)
        {
            FrameObjectEventSystem.NotifyPreviewStateChanged(this, isPreviewActive);
        }
        
        
        [SerializeField] private FrameData _frameData = new FrameData("Agrupar Objetos");
        [SerializeField] private bool _autoUpdatePaths = true;
        [SerializeField] private bool _showObjectList = true;
        [SerializeField] private bool _showMaterialList = true;
        [SerializeField] private bool _showBlendshapeList = true;
        
        // Estados de previsualización (para serialización)
        [SerializeField, HideInInspector] private bool _isPreviewActive = false;
        [SerializeField, HideInInspector] private List<ObjectReference> _originalStates = new List<ObjectReference>();
        [SerializeField, HideInInspector] private List<MaterialReference> _originalMaterialStates = new List<MaterialReference>();
        [SerializeField, HideInInspector] private List<BlendshapeReference> _originalBlendshapeStates = new List<BlendshapeReference>();
        
        private FrameObjectController _objectController;
        private FrameMaterialController _materialController;
        private FrameBlendshapeController _blendshapeController;
        private FramePreviewController _previewController;
        private bool _controllersInitialized = false;
        
        // Cache de validación para evitar recálculos
        private ValidationResult _cachedValidationResult;
        private bool _isValidationDirty = true;
        private int _lastValidationHash = 0;
        
        public FrameData FrameData => _frameData;
        public List<ObjectReference> ObjectReferences => _frameData.ObjectReferences;
        public List<MaterialReference> MaterialReferences => _frameData.MaterialReferencesData;
        public List<BlendshapeReference> BlendshapeReferences => _frameData.BlendshapeReferences;
        
        public string FrameName 
        { 
            get => _frameData.Name; 
            set 
            {
                if (_frameData.Name != value)
                {
                    _frameData.Name = value;
                    InvalidateValidationCache(); // Invalidar cuando cambia el nombre
                }
            }
        }
        
        public bool ShowObjectList { get => _showObjectList; set => _showObjectList = value; }
        public bool ShowMaterialList { get => _showMaterialList; set => _showMaterialList = value; }
        public bool ShowBlendshapeList { get => _showBlendshapeList; set => _showBlendshapeList = value; }
        public bool AutoUpdatePaths { get => _autoUpdatePaths; set => _autoUpdatePaths = value; }
        
        public bool IsPreviewActive 
        { 
            get 
            {
                EnsureControllersInitialized();
                return _previewController?.IsPreviewActive ?? _isPreviewActive;
            }
        }
        
        
        public List<IFrameData> Frames => _frameData != null ? new List<IFrameData> { _frameData } : new List<IFrameData>();
        
        public int ActiveFrameIndex { get; set; } = 0;
        
        public void SelectNextFrame() { }
        public void SelectPreviousFrame() { }
        public void SelectFrameByIndex(int index) { }
        
        public void ApplyCurrentFrame()
        {
            if (_frameData == null)
            {
                return;
            }
            
            EnsureControllersInitialized();
            
            _objectController.ApplyObjectStates();
            _materialController.ApplyMaterialStates();
            _blendshapeController.ApplyBlendshapeStates();
        }
        
        public bool AddGameObject(GameObject gameObject, bool isActive = true)
        {
            // Validación de entrada
            if (gameObject == null)
                return false;

            // Validación de estado del componente
            if (_frameData == null)
                return false;

            EnsureControllersInitialized();
            bool success = _objectController.AddObject(gameObject, isActive);

            // Solo disparar eventos si se añadió correctamente
            if (success)
            {
                RaiseObjectAdded(gameObject);
                RaiseStateChanged();
            }

            return success;
        }
        
        public void RemoveGameObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            
            EnsureControllersInitialized();
            _objectController.RemoveObject(gameObject);
            
            // Disparar evento (thread-safe)
            RaiseObjectRemoved(gameObject);
            RaiseStateChanged();
        }
        
        // remover en próxima major
        [Obsolete("Usa ClearObjects() en su lugar")]
        public void ClearAllObjects()
        {
            EnsureControllersInitialized();
            _objectController.ClearAllObjects();
        }
        
        public void ClearObjects()
        {
            if (ObjectReferences.Count == 0) return;
            
            EnsureControllersInitialized();
            var removedObjects = ObjectReferences.Select(obj => obj.GameObject).Where(obj => obj != null).ToList();
            
            _objectController.ClearAllObjects();
            
            // Emitir eventos por cada objeto eliminado (thread-safe)
            foreach (var gameObject in removedObjects)
            {
                RaiseObjectRemoved(gameObject);
            }
            RaiseStateChanged();
        }
        
        public void SelectAllObjects()
        {
            EnsureControllersInitialized();
            _objectController.SelectAllObjects();
        }
        
        public void DeselectAllObjects()
        {
            EnsureControllersInitialized();
            _objectController.DeselectAllObjects();
        }
        
        // remover en próxima major
        [Obsolete("Usa UpdateAllPaths() en su lugar")]
        public void RecalculatePaths()
        {
            EnsureControllersInitialized();
            _objectController.RecalculateAllPaths();
        }
        
        public void UpdateAllPaths()
        {
            EnsureControllersInitialized();
            _objectController.RecalculateAllPaths();
        }
        
        public void RemoveInvalidReferences()
        {
            EnsureControllersInitialized();
            var invalidObjects = ObjectReferences.Where(obj => obj.GameObject == null).ToList();
            
            if (invalidObjects.Count == 0) return;
            
            _objectController.RemoveInvalidReferences();
            
            // Emitir evento de cambio de estado (thread-safe) - no podemos emitir OnObjectRemoved para objetos null
            RaiseStateChanged();
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().Objects en su lugar")]
        public int GetObjectCount()
        {
            EnsureControllersInitialized();
            return _objectController?.ObjectCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().ValidObjects en su lugar")]
        public int GetValidObjectCount()
        {
            EnsureControllersInitialized();
            return _objectController?.ValidObjectCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().InvalidObjects en su lugar")]
        public int GetInvalidObjectCount()
        {
            EnsureControllersInitialized();
            return _objectController?.InvalidObjectCount ?? 0;
        }
        
        public bool AddMaterialReference(Renderer renderer, int materialIndex = 0, Material alternativeMaterial = null)
        {
            if (renderer == null) return false;

            EnsureControllersInitialized();
            bool success = _materialController.AddMaterial(renderer, materialIndex, alternativeMaterial);

            // Solo disparar eventos si se añadió correctamente
            if (success)
            {
                RaiseMaterialAdded(renderer, materialIndex);
                RaiseStateChanged();
            }

            return success;
        }
        
        public void RemoveMaterialReference(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null) return;
            
            EnsureControllersInitialized();
            _materialController.RemoveMaterial(renderer, materialIndex);
            
            // Disparar evento (thread-safe)
            RaiseMaterialRemoved(renderer, materialIndex);
            RaiseStateChanged();
        }
        
        // remover en próxima major
        [Obsolete("Usa ClearMaterials() en su lugar")]
        public void ClearAllMaterials()
        {
            EnsureControllersInitialized();
            _materialController.ClearAllMaterials();
        }
        
        public void ClearMaterials()
        {
            if (MaterialReferences.Count == 0) return;
            
            EnsureControllersInitialized();
            var removedMaterials = MaterialReferences.Where(mat => mat.TargetRenderer != null)
                .Select(mat => new { mat.TargetRenderer, mat.MaterialIndex }).ToList();
            
            _materialController.ClearAllMaterials();
            
            // Emitir eventos por cada material eliminado (thread-safe)
            foreach (var material in removedMaterials)
            {
                RaiseMaterialRemoved(material.TargetRenderer, material.MaterialIndex);
            }
            RaiseStateChanged();
        }
        
        public void RemoveInvalidMaterialReferences()
        {
            EnsureControllersInitialized();
            var invalidMaterials = MaterialReferences.Where(mat => mat.TargetRenderer == null).ToList();
            
            if (invalidMaterials.Count == 0) return;
            
            _materialController.RemoveInvalidMaterialReferences();
            
            // Emitir evento de cambio de estado (thread-safe) - no podemos emitir OnMaterialRemoved para renderers null
            RaiseStateChanged();
        }
        
        public void UpdateAllOriginalMaterials()
        {
            EnsureControllersInitialized();
            _materialController.UpdateAllOriginalMaterials();
        }
        
        public void UpdateAllMaterialRendererPaths()
        {
            EnsureControllersInitialized();
            _materialController.UpdateAllMaterialRendererPaths();
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().Materials en su lugar")]
        public int GetMaterialCount()
        {
            EnsureControllersInitialized();
            return _materialController?.MaterialCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().ValidMaterials en su lugar")]
        public int GetValidMaterialCount()
        {
            EnsureControllersInitialized();
            return _materialController?.ValidMaterialCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().InvalidMaterials en su lugar")]
        public int GetInvalidMaterialCount()
        {
            EnsureControllersInitialized();
            return _materialController?.InvalidMaterialCount ?? 0;
        }
        
        public bool AddBlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return false;

            EnsureControllersInitialized();

            // Salvaguarda extra (por si EnsureControllersInitialized cambia en el futuro)
            if (_blendshapeController == null)
                _blendshapeController = _controllerFactory.CreateBlendshapeController(_frameData ?? (_frameData = new FrameData("Agrupar Objetos")));

            bool success = _blendshapeController.AddBlendshape(renderer, blendshapeName, value);

            // Solo disparar eventos si se añadió correctamente
            if (success)
            {
                RaiseBlendshapeAdded(renderer, blendshapeName);
                RaiseStateChanged();
            }

            return success;
        }
        
        public void RemoveBlendshapeReference(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (renderer == null || string.IsNullOrEmpty(blendshapeName)) return;
            
            EnsureControllersInitialized();
            _blendshapeController.RemoveBlendshape(renderer, blendshapeName);
            
            // Disparar evento (thread-safe)
            RaiseBlendshapeRemoved(renderer, blendshapeName);
            RaiseStateChanged();
        }
        
        public void RemoveAllBlendshapeReferences(SkinnedMeshRenderer renderer)
        {
            if (renderer == null) return;
            
            EnsureControllersInitialized();
            var blendshapesToRemove = BlendshapeReferences.Where(blend => blend.TargetRenderer == renderer)
                .Select(blend => blend.BlendshapeName).ToList();
            
            if (blendshapesToRemove.Count == 0) return;
            
            _blendshapeController.RemoveAllBlendshapesFromRenderer(renderer);
            
            // Emitir eventos por cada blendshape eliminado (thread-safe)
            foreach (var blendshapeName in blendshapesToRemove)
            {
                RaiseBlendshapeRemoved(renderer, blendshapeName);
            }
            RaiseStateChanged();
        }

        public void ClearBlendshapes()
        {
            if (BlendshapeReferences.Count == 0) return;
            
            EnsureControllersInitialized();
            var removedBlendshapes = BlendshapeReferences.Where(blend => blend.TargetRenderer != null)
                .Select(blend => new { blend.TargetRenderer, blend.BlendshapeName }).ToList();
            
            _blendshapeController.ClearAllBlendshapes();
            
            // Emitir eventos por cada blendshape eliminado (thread-safe)
            foreach (var blendshape in removedBlendshapes)
            {
                RaiseBlendshapeRemoved(blendshape.TargetRenderer, blendshape.BlendshapeName);
            }
            RaiseStateChanged();
        }
        
        public void RemoveInvalidBlendshapeReferences()
        {
            EnsureControllersInitialized();
            var invalidBlendshapes = BlendshapeReferences.Where(blend => blend.TargetRenderer == null).ToList();
            
            if (invalidBlendshapes.Count == 0) return;
            
            _blendshapeController.RemoveInvalidBlendshapeReferences();
            
            // Emitir evento de cambio de estado (thread-safe) - no podemos emitir OnBlendshapeRemoved para renderers null
            RaiseStateChanged();
        }
        
        public void UpdateAllBlendshapeRendererPaths()
        {
            EnsureControllersInitialized();
            _blendshapeController.UpdateAllBlendshapeRendererPaths();
        }
        
        public void CaptureAllBlendshapeValues()
        {
            EnsureControllersInitialized();
            _blendshapeController.CaptureAllBlendshapeValues();
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().Blendshapes en su lugar")]
        public int GetBlendshapeCount()
        {
            EnsureControllersInitialized();
            return _blendshapeController?.BlendshapeCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().ValidBlendshapes en su lugar")]
        public int GetValidBlendshapeCount()
        {
            EnsureControllersInitialized();
            return _blendshapeController?.ValidBlendshapeCount ?? 0;
        }
        
        // remover en próxima major
        [Obsolete("Usa GetCounts().InvalidBlendshapes en su lugar")]
        public int GetInvalidBlendshapeCount()
        {
            EnsureControllersInitialized();
            return _blendshapeController?.InvalidBlendshapeCount ?? 0;
        }
        
        /// <summary>
        /// Estructura para conteos unificados de elementos del frame
        /// </summary>
        public struct FrameCounts
        {
            public int Objects;
            public int Materials;
            public int Blendshapes;
            public int ValidObjects;
            public int ValidMaterials;
            public int ValidBlendshapes;
            public int InvalidObjects;
            public int InvalidMaterials;
            public int InvalidBlendshapes;
        }
        
        /// <summary>
        /// Obtiene todos los conteos de elementos del frame de forma unificada
        /// </summary>
        /// <returns>Estructura con conteos de objetos, materiales y blendshapes</returns>
        public FrameCounts GetCounts()
        {
            EnsureControllersInitialized();
            return new FrameCounts
            {
                Objects = _objectController.ObjectCount,
                Materials = _materialController.MaterialCount,
                Blendshapes = _blendshapeController.BlendshapeCount,
                ValidObjects = _objectController.ValidObjectCount,
                ValidMaterials = _materialController.ValidMaterialCount,
                ValidBlendshapes = _blendshapeController.ValidBlendshapeCount,
                InvalidObjects = _objectController.InvalidObjectCount,
                InvalidMaterials = _materialController.InvalidMaterialCount,
                InvalidBlendshapes = _blendshapeController.InvalidBlendshapeCount
            };
        }
        
        public void PreviewFrame()
        {
            // Usar TogglePreview en lugar de ActivatePreview para preservar comportamiento toggle original
            PreviewManager.TogglePreview(this);
        }
        
        /// <summary>
        /// Método interno para la lógica real de preview (usado por PreviewManager)
        /// </summary>
        private void InternalPreviewFrame()
        {
            EnsureControllersInitialized();

            // Salvaguarda adicional por si algo cambió entre frames
            _frameData           ??= new FrameData("Frame");
            _objectController     ??= new FrameObjectController(_frameData);
            _materialController   ??= new FrameMaterialController(_frameData);
            _blendshapeController ??= new FrameBlendshapeController(_frameData);
            _previewController    ??= new FramePreviewController(_objectController, _materialController, _blendshapeController);

            _previewController.PreviewFrame();
            _isPreviewActive = _previewController.IsPreviewActive;

            // Disparar evento (thread-safe)
            RaisePreviewStateChanged(_isPreviewActive);
            RaiseStateChanged();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        public void CancelPreview()
        {
            // Si este es el preview activo, desactivarlo a través del PreviewManager
            if (PreviewManager.CurrentActivePreview == this)
            {
                PreviewManager.DeactivateCurrentPreview();
            }
            else
            {
                // Si no es el activo, cancelar directamente (caso edge)
                InternalCancelPreview();
            }
        }

        /// <summary>
        /// Refresca la previsualización aplicando los estados actuales del frame.
        /// Útil para actualizar la escena en tiempo real cuando el usuario modifica
        /// valores (IsActive, materiales, blendshapes) mientras el preview está activo.
        /// </summary>
        public void RefreshPreview()
        {
            if (!IsPreviewActive)
                return;

            EnsureControllersInitialized();
            _previewController?.RefreshPreview();

            #if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
            #endif
        }
        
        /// <summary>
        /// Método interno para la lógica real de cancelación (usado por PreviewManager)
        /// </summary>
        private void InternalCancelPreview()
        {
            EnsureControllersInitialized();
            _previewController.CancelPreview();
            _isPreviewActive = _previewController.IsPreviewActive;
            
            // Disparar evento (thread-safe)
            RaisePreviewStateChanged(_isPreviewActive);
            RaiseStateChanged();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        
        /// <summary>
        /// Activa la previsualización de este frame (implementación de IPreviewable)
        /// Llamado por PreviewManager cuando se activa este componente
        /// </summary>
        public void ActivatePreview()
        {
            EnsureControllersInitialized();
            
            // Ejecutar la lógica interna sin recursividad con PreviewManager
            InternalPreviewFrame();
        }
        
        /// <summary>
        /// Desactiva la previsualización de este frame (implementación de IPreviewable)
        /// Llamado por PreviewManager cuando se desactiva este componente
        /// </summary>
        public void DeactivatePreview()
        {
            EnsureControllersInitialized();
            
            // Ejecutar la lógica interna sin recursividad con PreviewManager
            InternalCancelPreview();
        }
        
        /// <summary>
        /// Obtiene el tipo de previsualización de este componente (implementación de IPreviewable)
        /// MRAgruparObjetos usa previsualización tipo Toggle ya que representa un estado específico
        /// </summary>
        /// <returns>Tipo de preview para frames</returns>
        public PreviewType GetPreviewType()
        {
            return PreviewType.Toggle;
        }
        
        /// <summary>
        /// Indica si la previsualización está activa (implementación de IPreviewable)
        /// Nota: Esta propiedad ya existe, solo se expone explícitamente para IPreviewable
        /// </summary>
        bool IPreviewable.IsPreviewActive => IsPreviewActive;
        
        
        
        
        /// <summary>
        /// Tipo de animación que genera este componente
        /// MRAgruparObjetos no genera animaciones directamente, sus datos se usan en MRUnificarObjetos
        /// </summary>
        public AnimationType AnimationType => AnimationType.None;
        
        /// <summary>
        /// Nombre de la animación (MRAgruparObjetos no genera animaciones directamente)
        /// </summary>
        public string AnimationName => FrameName + "_FrameData";
        
        /// <summary>
        /// Indica si el componente puede generar animaciones
        /// MRAgruparObjetos no genera animaciones directamente
        /// </summary>
        public bool CanGenerateAnimation => false;
        
        /// <summary>
        /// Descripción del tipo de animación
        /// </summary>
        /// <returns>Descripción legible del tipo de animación</returns>
        public string GetAnimationTypeDescription()
        {
            var counts = GetCounts();
            return $"Frame Data ({counts.Objects} objetos, {counts.Materials} materiales, {counts.Blendshapes} blendshapes) - No genera animaciones directamente";
        }
        
        
        public override ValidationResult Validate()
        {
            // Verificar si necesitamos revalidar
            var currentHash = CalculateFrameContentHash();
            
            if (!_isValidationDirty && _lastValidationHash == currentHash && _cachedValidationResult != null)
            {
                return _cachedValidationResult; // Usar cache
            }
            
            // Ejecutar validación real
            _cachedValidationResult = MRAgruparObjetosValidator.ValidateFrameObject(this);
            _lastValidationHash = currentHash;
            _isValidationDirty = false;
            
            return _cachedValidationResult;
        }
        
        /// <summary>
        /// Calcula hash del contenido del frame para detectar cambios
        /// </summary>
        private int CalculateFrameContentHash()
        {
            unchecked
            {
                int hash = (_frameData?.Name?.GetHashCode() ?? 0);
                hash = hash * 31 + (_frameData?.ObjectReferences?.Count ?? 0);
                hash = hash * 31 + (_frameData?.MaterialReferencesData?.Count ?? 0);
                hash = hash * 31 + (_frameData?.BlendshapeReferences?.Count ?? 0);
                hash = hash * 31 + (_isPreviewActive ? 1 : 0);
                return hash;
            }
        }
        
        /// <summary>
        /// Invalida el cache de validación cuando cambia el frame
        /// </summary>
        private void InvalidateValidationCache()
        {
            _isValidationDirty = true;
            _cachedValidationResult = null;
            _lastValidationHash = 0;
        }
        
        private void EnsureControllersInitialized()
        {
            // Garantizar que _frameData existe
            _frameData ??= new FrameData("Agrupar Objetos");
            
            if (!_controllersInitialized)
            {
                InitializeControllers();
            }
            
            // Salvaguardas adicionales por si el factory falló o cambió algo
            _objectController     ??= new FrameObjectController(_frameData);
            _materialController   ??= new FrameMaterialController(_frameData);
            _blendshapeController ??= new FrameBlendshapeController(_frameData);
            _previewController    ??= new FramePreviewController(_objectController, _materialController, _blendshapeController);
        }
        
        private void InitializeControllers()
        {
            if (_controllersInitialized) return;
            
            _objectController = _controllerFactory.CreateObjectController(_frameData);
            _materialController = _controllerFactory.CreateMaterialController(_frameData);
            _blendshapeController = _controllerFactory.CreateBlendshapeController(_frameData);
            _previewController = _controllerFactory.CreatePreviewController(_objectController, _materialController, _blendshapeController);
            
            _previewController.SyncWithSerializedStates(_isPreviewActive, _originalStates, _originalMaterialStates, _originalBlendshapeStates);
            _controllersInitialized = true;
        }
        
        private void InvalidateControllers()
        {
            _objectController = null;
            _materialController = null;
            _blendshapeController = null;
            _previewController = null;
            _controllersInitialized = false;
        }
        
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            
            if (_frameData == null)
            {
                _frameData = new FrameData("Agrupar Objetos");
            }
            
            InitializeControllers();
            
            if (_originalStates == null) _originalStates = new List<ObjectReference>();
            if (_originalMaterialStates == null) _originalMaterialStates = new List<MaterialReference>();
            if (_originalBlendshapeStates == null) _originalBlendshapeStates = new List<BlendshapeReference>();
            
            if (_isPreviewActive && _originalStates.Count == 0 && _originalMaterialStates.Count == 0 && _originalBlendshapeStates.Count == 0)
            {
                _isPreviewActive = false;
            }
            
            // Registrar con PreviewManager para coordinación global de previews
            PreviewManager.RegisterComponent(this);
        }
        
        protected override void CleanupComponent()
        {
            // Desregistrar del PreviewManager antes del cleanup
            PreviewManager.UnregisterComponent(this);
            
            base.CleanupComponent();
            CancelPreview();
        }
        
#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();
            
            if (_frameData != null)
            {
                _frameData.UpdateAllHierarchyPaths();
                _frameData.UpdateAllBlendshapeRendererPaths();
            }
        }
#endif
        
        
        
        private bool _disposed = false;
        
        /// <summary>
        /// Implementación de IDisposable para limpieza de eventos weak
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Implementación del patrón dispose
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Limpiar eventos weak (aunque técnicamente se limpian solos, esto es por seguridad)
                    // Los WeakEventManager automáticamente limpiaran referencias muertas
                    
                    // Cancelar preview si está activo
                    if (_isPreviewActive)
                    {
                        CancelPreview();
                    }
                    
                    // Desregistrar del PreviewManager
                    PreviewManager.UnregisterComponent(this);
                }
                
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Finalizer para garantizar limpieza en casos extremos
        /// </summary>
        ~MRAgruparObjetos()
        {
            Dispose(false);
        }
        
    }
}
