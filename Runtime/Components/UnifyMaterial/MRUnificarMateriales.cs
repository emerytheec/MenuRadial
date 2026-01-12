using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Validation.Models;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;

namespace Bender_Dios.MenuRadial.Components.UnifyMaterial
{
    /// <summary>
    /// Componente MR Unificar Materiales (antes MRUnifyMaterial)
    /// Unifica varios MRAgruparMateriales para generar una sola animación Linear
    /// </summary>
    [AddComponentMenu("MR/MR Unificar Materiales")]
    public class MRUnificarMateriales : MRComponentBase, IAnimationProvider
    {
        [SerializeField] private string _animationName = "UnifyMaterial";
        [SerializeField] private string _animationPath = MRConstants.ANIMATION_OUTPUT_PATH;
        [SerializeField] private List<MRAgruparMateriales> _alternativeMaterials = new List<MRAgruparMateriales>();

        // Cache de validacion
        private ValidationResult _lastValidationResult;
        private bool _validationCacheValid = false;

        #region Properties

        /// <summary>
        /// Nombre de la animacion que se generara
        /// </summary>
        public string AnimationName
        {
            get => _animationName;
            set
            {
                if (_animationName != value)
                {
                    _animationName = value;
                    InvalidateValidation();
                }
            }
        }

        /// <summary>
        /// Ruta donde se guardara la animacion
        /// </summary>
        public string AnimationPath
        {
            get => _animationPath;
            set
            {
                if (_animationPath != value)
                {
                    _animationPath = value;
                    InvalidateValidation();
                }
            }
        }

        /// <summary>
        /// Lista de MRAgruparMateriales incluidos
        /// </summary>
        public List<MRAgruparMateriales> AlternativeMaterials => _alternativeMaterials;

        /// <summary>
        /// Cantidad de MRAgruparMateriales
        /// </summary>
        public int AlternativeMaterialCount => _alternativeMaterials?.Count(am => am != null) ?? 0;

        /// <summary>
        /// Ruta completa del archivo de animacion
        /// </summary>
        public string FullAnimationPath => System.IO.Path.Combine(_animationPath, _animationName + MRAnimationSuffixes.LINEAR + MRFileExtensions.ANIMATION);

        #endregion

        #region IAnimationProvider Implementation

        /// <summary>
        /// Siempre genera animacion Linear (255 frames)
        /// </summary>
        public AnimationType AnimationType => AnimationType.Linear;

        /// <summary>
        /// Indica si puede generar animaciones
        /// </summary>
        public bool CanGenerateAnimation => AlternativeMaterialCount > 0 &&
                                            !string.IsNullOrEmpty(_animationName) &&
                                            GetTotalLinkedSlots() > 0;

        /// <summary>
        /// Descripcion del tipo de animacion
        /// </summary>
        public string GetAnimationTypeDescription()
        {
            int linkedSlots = GetTotalLinkedSlots();
            if (linkedSlots == 0)
                return "Sin slots vinculados para animar";

            return $"Animacion lineal (255 frames) - {linkedSlots} slots vinculados";
        }

        #endregion

        #region Alternative Material Management

        /// <summary>
        /// Agrega un MRAgruparMateriales a la lista
        /// </summary>
        /// <param name="alternativeMaterial">Componente a agregar</param>
        /// <returns>True si se agrego, false si ya existia o es null</returns>
        public bool AddAlternativeMaterial(MRAgruparMateriales alternativeMaterial)
        {
            if (alternativeMaterial == null) return false;
            if (_alternativeMaterials.Contains(alternativeMaterial)) return false;

            _alternativeMaterials.Add(alternativeMaterial);
            InvalidateValidation();
            return true;
        }

        /// <summary>
        /// Elimina un MRAgruparMateriales de la lista
        /// </summary>
        /// <param name="alternativeMaterial">Componente a eliminar</param>
        /// <returns>True si se elimino</returns>
        public bool RemoveAlternativeMaterial(MRAgruparMateriales alternativeMaterial)
        {
            if (alternativeMaterial == null) return false;

            bool removed = _alternativeMaterials.Remove(alternativeMaterial);
            if (removed) InvalidateValidation();
            return removed;
        }

        /// <summary>
        /// Elimina un MRAgruparMateriales por indice
        /// </summary>
        /// <param name="index">Indice a eliminar</param>
        /// <returns>True si se elimino</returns>
        public bool RemoveAlternativeMaterialAt(int index)
        {
            if (index < 0 || index >= _alternativeMaterials.Count) return false;

            _alternativeMaterials.RemoveAt(index);
            InvalidateValidation();
            return true;
        }

        /// <summary>
        /// Limpia todos los MRAgruparMateriales
        /// </summary>
        public void ClearAllAlternativeMaterials()
        {
            _alternativeMaterials.Clear();
            InvalidateValidation();
        }

        /// <summary>
        /// Elimina referencias nulas de la lista
        /// </summary>
        /// <returns>Cantidad de referencias eliminadas</returns>
        public int RemoveNullReferences()
        {
            int initialCount = _alternativeMaterials.Count;
            _alternativeMaterials.RemoveAll(am => am == null);
            int removed = initialCount - _alternativeMaterials.Count;
            if (removed > 0) InvalidateValidation();
            return removed;
        }

        /// <summary>
        /// Crea un nuevo GameObject hijo con componente MRAgruparMateriales y lo añade a la lista
        /// </summary>
        /// <returns>El MRAgruparMateriales creado o null si falla</returns>
        public MRAgruparMateriales CreateAlternativeMaterial()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Crear Agrupar Materiales");
#endif

            // Generar nombre único
            string baseName = "AgruparMateriales";
            string uniqueName = GenerateUniqueName(baseName);

            // Crear GameObject como hijo
            GameObject newObject = new GameObject(uniqueName);
            newObject.transform.SetParent(transform);
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Crear Agrupar Materiales");
#endif

            // Añadir componente
            var altMaterial = newObject.AddComponent<MRAgruparMateriales>();

            // Añadir a la lista
            _alternativeMaterials.Add(altMaterial);
            InvalidateValidation();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(altMaterial);
            UnityEditor.Selection.activeGameObject = newObject;
#endif

            return altMaterial;
        }

        /// <summary>
        /// Genera un nombre único para un nuevo componente hijo
        /// </summary>
        private string GenerateUniqueName(string baseName)
        {
            var existingNames = new HashSet<string>();

            // Nombres de la lista
            foreach (var am in _alternativeMaterials)
            {
                if (am != null)
                    existingNames.Add(am.name);
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

        #endregion

        #region Animation Data Collection

        /// <summary>
        /// Obtiene la cantidad total de slots vinculados a grupos en todos los MRAgruparMateriales
        /// </summary>
        /// <returns>Cantidad de slots vinculados</returns>
        public int GetTotalLinkedSlots()
        {
            int total = 0;
            foreach (var am in _alternativeMaterials.Where(a => a != null))
            {
                total += am.LinkedSlotsCount;
            }
            return total;
        }

        /// <summary>
        /// Recopila todos los datos necesarios para generar la animacion.
        /// Retorna informacion de cada slot vinculado con su grupo y distribucion de frames.
        /// </summary>
        /// <returns>Lista de datos de animacion por slot</returns>
        public List<UnifySlotAnimationData> CollectAnimationData()
        {
            var result = new List<UnifySlotAnimationData>();

            foreach (var altMat in _alternativeMaterials.Where(am => am != null))
            {
                var linkedSlots = altMat.GetLinkedSlots();

                foreach (var slot in linkedSlots)
                {
                    if (!slot.IsValid) continue;

                    var group = altMat.GetGroupForSlot(slot);
                    if (group == null || !group.IsValid) continue;

                    var validMaterials = group.GetValidMaterials();
                    if (validMaterials.Count < 2) continue;

                    var frameDistribution = CalculateFrameDistribution(validMaterials.Count);

                    result.Add(new UnifySlotAnimationData
                    {
                        Slot = slot,
                        Group = group,
                        Materials = validMaterials,
                        FrameDistribution = frameDistribution,
                        SourceComponent = altMat
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Calcula la distribucion de frames para un numero de materiales.
        /// Divide los 255 frames equitativamente, el ultimo material recibe los sobrantes.
        /// El frame 255 siempre repite el ultimo material.
        /// </summary>
        /// <param name="materialCount">Cantidad de materiales</param>
        /// <returns>Lista de rangos de frames (startFrame, endFrame) por material</returns>
        public List<FrameRange> CalculateFrameDistribution(int materialCount)
        {
            var distribution = new List<FrameRange>();
            if (materialCount <= 0) return distribution;

            int totalFrames = MRAnimationConstants.TOTAL_FRAMES; // 255
            int framesPerMaterial = totalFrames / materialCount;
            int remainder = totalFrames % materialCount;

            int currentFrame = 0;

            for (int i = 0; i < materialCount; i++)
            {
                int startFrame = currentFrame;
                int frameCount = framesPerMaterial;

                // El ultimo material recibe los frames sobrantes
                if (i == materialCount - 1)
                {
                    frameCount += remainder;
                }

                int endFrame = startFrame + frameCount - 1;

                // El ultimo material tambien tiene el frame 255 para cerrar
                if (i == materialCount - 1)
                {
                    endFrame = totalFrames; // Frame 255 incluido
                }

                distribution.Add(new FrameRange
                {
                    MaterialIndex = i,
                    StartFrame = startFrame,
                    EndFrame = endFrame
                });

                currentFrame = endFrame + 1;
            }

            return distribution;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valida el estado del componente
        /// </summary>
        public override ValidationResult Validate()
        {
            if (!_validationCacheValid)
            {
                _lastValidationResult = ValidateInternal();
                _validationCacheValid = true;
            }
            return _lastValidationResult;
        }

        private ValidationResult ValidateInternal()
        {
            var result = new ValidationResult("MR Unificar Materiales");

            // Validar nombre de animacion
            if (string.IsNullOrEmpty(_animationName))
            {
                result.AddChild(ValidationResult.Error("Nombre de animacion requerido"));
            }

            // Validar que haya MRAgruparMateriales
            if (AlternativeMaterialCount == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay MRAgruparMateriales agregados"));
                return result;
            }

            // Validar referencias nulas
            int nullCount = _alternativeMaterials.Count(am => am == null);
            if (nullCount > 0)
            {
                result.AddChild(ValidationResult.Warning($"{nullCount} referencias nulas en la lista"));
            }

            // Validar slots vinculados
            int totalLinkedSlots = GetTotalLinkedSlots();
            if (totalLinkedSlots == 0)
            {
                result.AddChild(ValidationResult.Warning("No hay slots vinculados a grupos. No se generara animacion."));
            }
            else
            {
                result.AddChild(ValidationResult.Info($"{totalLinkedSlots} slots seran animados"));
            }

            // Recopilar datos para validacion detallada
            var animationData = CollectAnimationData();
            if (animationData.Count > 0)
            {
                result.AddChild(ValidationResult.Success($"Configuracion valida: {animationData.Count} curvas de material"));
            }

            return result;
        }

        /// <summary>
        /// Invalida el cache de validacion
        /// </summary>
        private void InvalidateValidation()
        {
            _validationCacheValid = false;
        }

        /// <summary>
        /// Fuerza validacion completa
        /// </summary>
        public ValidationResult ValidateForce()
        {
            InvalidateValidation();
            return Validate();
        }

        #endregion

        #region Unity Lifecycle

        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            if (_alternativeMaterials == null)
                _alternativeMaterials = new List<MRAgruparMateriales>();
        }

#if UNITY_EDITOR
        protected override void ValidateInEditor()
        {
            base.ValidateInEditor();
            InvalidateValidation();
        }
#endif

        #endregion
    }

    /// <summary>
    /// Datos de animacion para un slot individual
    /// </summary>
    [Serializable]
    public class UnifySlotAnimationData
    {
        /// <summary>
        /// Slot de material a animar
        /// </summary>
        public MRMaterialSlot Slot;

        /// <summary>
        /// Grupo de materiales del slot
        /// </summary>
        public MRMaterialGroup Group;

        /// <summary>
        /// Lista de materiales validos del grupo
        /// </summary>
        public List<Material> Materials;

        /// <summary>
        /// Distribucion de frames para cada material
        /// </summary>
        public List<FrameRange> FrameDistribution;

        /// <summary>
        /// Componente MRAgruparMateriales de origen
        /// </summary>
        public MRAgruparMateriales SourceComponent;
    }

    /// <summary>
    /// Rango de frames para un material
    /// </summary>
    [Serializable]
    public struct FrameRange
    {
        /// <summary>
        /// Indice del material en el grupo
        /// </summary>
        public int MaterialIndex;

        /// <summary>
        /// Frame inicial (inclusive)
        /// </summary>
        public int StartFrame;

        /// <summary>
        /// Frame final (inclusive)
        /// </summary>
        public int EndFrame;

        /// <summary>
        /// Cantidad de frames en este rango
        /// </summary>
        public int FrameCount => EndFrame - StartFrame + 1;

        public override string ToString()
        {
            return $"Material {MaterialIndex}: frames {StartFrame}-{EndFrame} ({FrameCount} frames)";
        }
    }
}
