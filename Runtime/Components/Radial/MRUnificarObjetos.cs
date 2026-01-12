using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.Frame;

namespace Bender_Dios.MenuRadial.Components.Radial
{
    /// <summary>
    /// Componente MR Unificar Objetos (antes MRRadialMenu)
    /// Agrupa frames (MRAgruparObjetos) y genera animaciones OnOff/AB/Linear
    /// </summary>
    [System.Serializable]
    [AddComponentMenu("MR/MR Unificar Objetos")]
    public class MRUnificarObjetos : MRComponentBase, IAnimationProvider
    {
        [SerializeField] private List<MRAgruparObjetos> _frames = new List<MRAgruparObjetos>();
        [SerializeField] private int _activeFrameIndex = 0;
        [SerializeField] private string _animationName = "RadialToggle";
        [SerializeField] private string _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        [SerializeField] private bool _autoUpdatePaths = true;

        /// <summary>
        /// Para animaciones OnOff (1 frame): determina si el estado por defecto en el FX es ON (true) o OFF (false)
        /// </summary>
        [SerializeField] private bool _defaultStateIsOn = false;
        
        // Estado agregado que centraliza dependencias
        private RadialMenuState _menuState;
        
        // Controladores especializados
        private RadialMenuPreviewController _previewController;
        private UnifiedPreviewStrategy _previewStrategy;
        
        // Cache para validación
        private ValidationResult _lastValidationResult;
        private bool _validationCacheValid = false;
        
        // Propiedades públicas
        
        /// <summary>
        /// Número de frames válidos
        /// </summary>
        public int FrameCount => _frames?.Count(f => f != null) ?? 0;
        
        /// <summary>
        /// Frame activo actual
        /// </summary>
        public MRAgruparObjetos ActiveFrame 
        { 
            get 
            {
                if (FrameCount == 0) return null;
                int safeIndex = Mathf.Clamp(_activeFrameIndex, 0, FrameCount - 1);
                return _frames?.Where(f => f != null).ElementAtOrDefault(safeIndex);
            }
        }
        
        /// <summary>
        /// Nombre de la animación
        /// </summary>
        public string AnimationName 
        { 
            get => _animationName;
            set 
            {
                if (_animationName != value)
                {
                    _animationName = value;
                    _menuState?.UpdateProperties(animationName: value);
                    InvalidateValidation();
                }
            }
        }
        
        /// <summary>
        /// Ruta de la animación
        /// </summary>
        public string AnimationPath 
        { 
            get => _animationPath;
            set 
            {
                if (_animationPath != value)
                {
                    _animationPath = value;
                    _menuState?.UpdateProperties(animationPath: value);
                    InvalidateValidation();
                }
            }
        }
        
        /// <summary>
        /// Auto-actualización de rutas
        /// </summary>
        public bool AutoUpdatePaths
        {
            get => _autoUpdatePaths;
            set
            {
                if (_autoUpdatePaths != value)
                {
                    _autoUpdatePaths = value;
                    _menuState?.UpdateProperties(autoUpdatePaths: value);
                    InvalidateValidation();
                }
            }
        }

        /// <summary>
        /// Para OnOff: si true, el estado por defecto en el FX Controller será ON en lugar de OFF.
        /// Solo aplica cuando FrameCount == 1 (AnimationType.OnOff)
        /// </summary>
        public bool DefaultStateIsOn
        {
            get => _defaultStateIsOn;
            set
            {
                if (_defaultStateIsOn != value)
                {
                    _defaultStateIsOn = value;
                    InvalidateValidation();
                }
            }
        }
        
        // Propiedades de compatibilidad
        public List<MRAgruparObjetos> FrameObjects => _frames;
        public string FullAnimationPath => System.IO.Path.Combine(_animationPath, _animationName + ".anim");
        
        
        // Frame Component Implementation
        
        public int ActiveFrameIndex 
        { 
            get => _activeFrameIndex;
            set 
            {
                if (_activeFrameIndex != value)
                {
                    _activeFrameIndex = value;
                    InvalidateValidation();
                }
            }
        }
        
        /// <summary>
        /// Selecciona el siguiente frame disponible en la secuencia
        /// </summary>
        public void SelectNextFrame()
        {
            if (FrameCount == 0) return;
            ActiveFrameIndex = (ActiveFrameIndex + 1) % FrameCount;
        }
        
        /// <summary>
        /// Selecciona el frame anterior en la secuencia
        /// </summary>
        public void SelectPreviousFrame()
        {
            if (FrameCount == 0) return;
            ActiveFrameIndex = (ActiveFrameIndex - 1 + FrameCount) % FrameCount;
        }
        
        /// <summary>
        /// Selecciona el frame según el índice especificado
        /// </summary>
        /// <param name="index">Índice del frame a seleccionar</param>
        public void SelectFrameByIndex(int index)
        {
            if (FrameCount == 0) return;
            ActiveFrameIndex = Mathf.Clamp(index, 0, FrameCount - 1);
        }
        
        /// <summary>
        /// Aplica inmediatamente el frame activo seleccionado
        /// </summary>
        public void ApplyCurrentFrame()
        {
            EnsurePreviewControllerInitialized();
            _previewController?.ApplyCurrentFrame();
        }

        /// <summary>
        /// Restaura todos los objetos, materiales y blendshapes al estado neutral.
        /// Estado neutral = todos los objetos APAGADOS, materiales originales, blendshapes en valor base.
        /// Útil para limpiar previews sin aplicar ningún frame específico.
        /// </summary>
        public void RestoreToNeutralState()
        {
            if (_frames == null || _frames.Count == 0) return;

            // Recopilar y desactivar todos los objetos de todos los frames
            var processedObjects = new HashSet<GameObject>();
            var processedRenderers = new HashSet<Renderer>();
            var processedBlendshapes = new HashSet<(SkinnedMeshRenderer, string)>();

            foreach (var frame in _frames)
            {
                if (frame == null) continue;

                // 1. Desactivar todos los GameObjects
                foreach (var objRef in frame.ObjectReferences)
                {
                    if (objRef?.GameObject != null && !processedObjects.Contains(objRef.GameObject))
                    {
                        objRef.GameObject.SetActive(false);
                        processedObjects.Add(objRef.GameObject);
                    }
                }

                // 2. Restaurar materiales originales
                foreach (var matRef in frame.MaterialReferences)
                {
                    if (matRef?.TargetRenderer == null || matRef.OriginalMaterial == null) continue;
                    if (processedRenderers.Contains(matRef.TargetRenderer)) continue;

                    var renderer = matRef.TargetRenderer;
                    var materials = renderer.sharedMaterials;
                    if (matRef.MaterialIndex < materials.Length)
                    {
                        materials[matRef.MaterialIndex] = matRef.OriginalMaterial;
                        renderer.sharedMaterials = materials;
                    }
                    processedRenderers.Add(renderer);
                }

                // 3. Restaurar blendshapes a su valor base (ActualValue)
                foreach (var blendRef in frame.BlendshapeReferences)
                {
                    if (blendRef?.TargetRenderer == null) continue;
                    var key = (blendRef.TargetRenderer, blendRef.BlendshapeName);
                    if (processedBlendshapes.Contains(key)) continue;

                    var renderer = blendRef.TargetRenderer;
                    if (renderer.sharedMesh != null)
                    {
                        int blendIndex = renderer.sharedMesh.GetBlendShapeIndex(blendRef.BlendshapeName);
                        if (blendIndex >= 0)
                        {
                            // Restaurar al valor base (ActualValue es el valor original capturado)
                            renderer.SetBlendShapeWeight(blendIndex, blendRef.ActualValue);
                        }
                    }
                    processedBlendshapes.Add(key);
                }
            }

            // Resetear el índice de frame activo a 0 (sin aplicarlo)
            _activeFrameIndex = 0;
        }

        /// <summary>
        /// Asegura que el controlador de preview esté inicializado (lazy initialization)
        /// Necesario para cuando el componente se accede desde el editor sin pasar por Awake()
        /// </summary>
        private void EnsurePreviewControllerInitialized()
        {
            if (_previewController == null && _frames != null)
            {
                _previewController = new RadialMenuPreviewController(
                    _frames,
                    () => _activeFrameIndex,
                    () => ActiveFrame
                );
            }
        }
        
        
        // Gestión de frames
        
        /// <summary>
        /// Añade un frame al menú radial
        /// </summary>
        /// <param name="frameObject">Frame a añadir</param>
        public void AddFrame(MRAgruparObjetos frameObject)
        {
            if (frameObject == null || _frames.Contains(frameObject))
                return;
                
            _frames.Add(frameObject);
            _menuState?.UpdateFrames(_frames);
            InvalidateValidation();
        }
        
        /// <summary>
        /// Remueve un frame del menú radial
        /// </summary>
        /// <param name="frameObject">Frame a remover</param>
        public void RemoveFrame(MRAgruparObjetos frameObject)
        {
            if (frameObject == null) return;
                
            if (_frames.Remove(frameObject))
            {
                // Ajustar índice activo si es necesario
                if (_activeFrameIndex >= FrameCount)
                    _activeFrameIndex = Mathf.Max(0, FrameCount - 1);
                    
                _menuState?.UpdateFrames(_frames);
                InvalidateValidation();
            }
        }
        
        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRAgruparObjetos y lo añade a la lista de frames
        /// </summary>
        /// <returns>El MRAgruparObjetos creado o null si falla</returns>
        public MRAgruparObjetos CreateFrameObject()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Crear Agrupar Objetos");
#endif

            // Generar nombre único
            string baseName = "AgruparObjetos";
            string uniqueName = GenerateUniqueFrameName(baseName);

            // Crear GameObject como hijo
            GameObject newObject = new GameObject(uniqueName);
            newObject.transform.SetParent(transform);
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Crear Agrupar Objetos");
#endif

            // Añadir componente
            var frameObject = newObject.AddComponent<MRAgruparObjetos>();

            // Añadir a la lista de frames
            _frames.Add(frameObject);
            _menuState?.UpdateFrames(_frames);
            InvalidateValidation();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(frameObject);
            UnityEditor.Selection.activeGameObject = newObject;
#endif

            return frameObject;
        }

        /// <summary>
        /// Genera un nombre único para un nuevo frame
        /// </summary>
        private string GenerateUniqueFrameName(string baseName)
        {
            var existingNames = new HashSet<string>();

            // Nombres de frames existentes
            foreach (var frame in _frames)
            {
                if (frame != null)
                    existingNames.Add(frame.name);
            }

            // Nombres de hijos
            for (int i = 0; i < transform.childCount; i++)
            {
                existingNames.Add(transform.GetChild(i).name);
            }

            string uniqueName = baseName;
            int counter = 1;

            while (existingNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}_{counter:00}";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// Limpia frames inválidos
        /// </summary>
        public void CleanupInvalidFrames()
        {
            var originalCount = _frames.Count;
            _frames.RemoveAll(f => f == null);
            
            if (originalCount != _frames.Count)
            {
                // Ajustar índice activo si es necesario
                if (_activeFrameIndex >= FrameCount)
                    _activeFrameIndex = Mathf.Max(0, FrameCount - 1);
                    
                _menuState?.UpdateFrames(_frames);
                InvalidateValidation();
            }
        }
        
        
        // Generación de animaciones
        
        /// <summary>
        /// Genera una animación directamente
        /// </summary>
        /// <returns>AnimationClip generado</returns>
        public AnimationClip GenerateAnimation()
        {
            if (FrameCount == 0 || string.IsNullOrEmpty(_animationName))
                return null;
                
            return CreateAnimationClip();
        }
        
        
        /// <summary>
        /// Crea el AnimationClip
        /// </summary>
        private AnimationClip CreateAnimationClip()
        {
            // Crear animación
            return new AnimationClip { name = _animationName };
        }
        
        
        // Validation System
        
        /// <summary>
        /// Validación principal del componente usando estado agregado
        /// </summary>
        /// <returns>Resultado de validación</returns>
        public override ValidationResult Validate()
        {
            if (!_validationCacheValid)
            {
                _lastValidationResult = ValidateInternal();
                _validationCacheValid = true;
            }
            return _lastValidationResult;
        }
        
        /// <summary>
        /// Validación interna mejorada
        /// </summary>
        private ValidationResult ValidateInternal()
        {
            // Validar usando estado agregado si está disponible
            if (_menuState != null && _menuState.IsInitialized)
            {
                return _menuState.ValidateComplete();
            }
            
            // Validación fallback básica
            if (_frames == null || FrameCount == 0)
                return new ValidationResult("No hay frames configurados", false, ValidationSeverity.Error);
                
            if (string.IsNullOrEmpty(_animationName))
                return new ValidationResult("Nombre de animación requerido", false, ValidationSeverity.Error);
                
            var validFrames = _frames.Where(f => f != null).ToList();
            if (validFrames.Count == 0)
                return new ValidationResult("Todos los frames son null", false, ValidationSeverity.Error);
                
            return new ValidationResult("Componente válido", true, ValidationSeverity.Info);
        }
        
        /// <summary>
        /// Invalida el cache de validación
        /// </summary>
        private void InvalidateValidation()
        {
            _validationCacheValid = false;
        }
        
        /// <summary>
        /// Fuerza validación completa para botones del Inspector
        /// </summary>
        /// <returns>Resultado de validación actualizado</returns>
        public ValidationResult ValidateForce()
        {
            InvalidateValidation();
            return Validate();
        }
        
        
        // IAnimationProvider Implementation

        /// <summary>
        /// Tipo de animación basado en el número de frames (implementación de IAnimationProvider)
        /// </summary>
        public AnimationType AnimationType => FrameCount switch
        {
            0 => Core.Common.AnimationType.None,
            1 => Core.Common.AnimationType.OnOff,
            2 => Core.Common.AnimationType.AB,
            _ => Core.Common.AnimationType.Linear
        };

        /// <summary>
        /// Indica si el componente puede generar animaciones (implementación de IAnimationProvider)
        /// </summary>
        public bool CanGenerateAnimation => FrameCount > 0 && !string.IsNullOrEmpty(AnimationName);

        /// <summary>
        /// Descripción del tipo de animación (implementación de IAnimationProvider)
        /// </summary>
        /// <returns>Descripción legible del tipo de animación</returns>
        public string GetAnimationTypeDescription()
        {
            return AnimationType switch
            {
                Core.Common.AnimationType.None => "Sin frames configurados",
                Core.Common.AnimationType.OnOff => "Toggle ON/OFF (1 frame)",
                Core.Common.AnimationType.AB => "Alternancia A/B (2 frames)",
                Core.Common.AnimationType.Linear => $"Animación lineal ({FrameCount} frames)",
                _ => "Tipo desconocido"
            };
        }
        
        
        // Implementación de preview

        /// <summary>
        /// Indica si el sistema de previsualización está activo
        /// </summary>
        public bool IsPreviewActive
        {
            get
            {
                EnsurePreviewControllerInitialized();
                return _previewController?.IsPreviewActive ?? false;
            }
        }

        /// <summary>
        /// Obtiene el tipo de previsualización para este componente
        /// </summary>
        /// <returns>Tipo de preview basado en el AnimationType</returns>
        public string GetPreviewType()
        {
            EnsurePreviewControllerInitialized();
            return _previewController?.GetPreviewType() ?? "None";
        }

        /// <summary>
        /// Activa el sistema de previsualización
        /// </summary>
        public void ActivatePreview()
        {
            EnsurePreviewControllerInitialized();
            _previewController?.ActivatePreview();
        }

        /// <summary>
        /// Desactiva el sistema de previsualización y restaura el estado original
        /// </summary>
        public void DeactivatePreview()
        {
            EnsurePreviewControllerInitialized();
            _previewController?.DeactivatePreview();
        }

        /// <summary>
        /// Alterna el estado de preview (activar/desactivar)
        /// </summary>
        public void TogglePreview()
        {
            EnsurePreviewControllerInitialized();
            _previewController?.TogglePreview();
        }

        /// <summary>
        /// Establece un valor específico para previews lineales
        /// </summary>
        /// <param name="normalizedValue">Valor entre 0 y 1</param>
        public void SetPreviewValue(float normalizedValue)
        {
            EnsurePreviewControllerInitialized();
            _previewController?.SetPreviewValue(normalizedValue, index => ActiveFrameIndex = index);
        }
        
        
        
        // Ciclo de vida Unity
        
        /// <summary>
        /// Inicialización del componente usando estado agregado
        /// </summary>
        void Awake()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Validación en editor optimizada
        /// </summary>
        void OnValidate()
        {
            InvalidateValidation();
            
            if (_autoUpdatePaths)
            {
                CleanupInvalidFrames();
            }
            
            // Actualizar estado agregado si existe
            _menuState?.UpdateFrames(_frames);
            _menuState?.UpdateProperties(_animationName, _animationPath, _autoUpdatePaths);
        }
        
        /// <summary>
        /// Cleanup de recursos
        /// </summary>
        void OnDestroy()
        {
            _menuState?.Cleanup();
            _previewController?.Cleanup();
            _previewStrategy?.Cleanup();
        }
        
        
        // Component Base Implementation
        
        /// <summary>
        /// Inicialización base del componente refactorizada
        /// </summary>
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            
            // Asegurar que listas estén inicializadas
            if (_frames == null)
                _frames = new List<MRAgruparObjetos>();
            
            // Validar índice activo
            if (_activeFrameIndex >= _frames.Count)
                _activeFrameIndex = Mathf.Max(0, _frames.Count - 1);
                
            // Inicializar estado agregado
            InitializeAggregatedState();
        }
        
        /// <summary>
        /// Inicializa el estado agregado y controladores
        /// </summary>
        private void InitializeAggregatedState()
        {
            // Validaciones defensivas
            if (_frames == null) return;
            if (string.IsNullOrEmpty(_animationName)) return;
            
            // Crear estado agregado centralizado
            _menuState = new RadialMenuState(
                this,
                _frames,
                _activeFrameIndex,
                _animationName,
                _animationPath,
                _autoUpdatePaths
            );
            
            // Crear controlador de preview
            _previewController = new RadialMenuPreviewController(
                _frames,
                () => _activeFrameIndex,
                () => ActiveFrame
            );
            
            // Crear estrategia unificada de preview
            _previewStrategy = new UnifiedPreviewStrategy(name)
                .AsToggle(
                    onActivate: () => _previewController?.ActivatePreview(),
                    onDeactivate: () => _previewController?.DeactivatePreview()
                );
        }
        
        
        // Compatibility Methods
        
        /// <summary>
        /// Método de compatibilidad para acceso directo a frames
        /// </summary>
        /// <returns>Lista de frames para compatibilidad</returns>
        [System.Obsolete("Use FrameObjects property instead")]
        public List<MRAgruparObjetos> GetFrames() => _frames;
        
        /// <summary>
        /// Método de compatibilidad para validación simple
        /// </summary>
        /// <returns>True si el componente es válido</returns>
        public bool IsValid()
        {
            return Validate().IsValid;
        }
        
    }
}
