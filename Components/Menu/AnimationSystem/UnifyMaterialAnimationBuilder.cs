#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.AnimationSystem
{
    /// <summary>
    /// Sistema de generacion de animaciones .anim para MR Unify Material.
    /// Genera una animacion lineal de 255 frames con curvas de material PPtr
    /// donde cada grupo de materiales tiene su propia distribucion de frames.
    /// </summary>
    public static class UnifyMaterialAnimationBuilder
    {
        #region Helpers Utilitarios

        private static float ToSec(int frame) => (float)(frame / MRAnimationConstants.FRAME_RATE_DOUBLE);

        private static string CalcPath(Transform target, Transform avatarRoot)
            => AnimationUtility.CalculateTransformPath(target, avatarRoot);

        private static (Renderer renderer, Type type)? GetRendererAndType(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr) return (smr, typeof(SkinnedMeshRenderer));
            if (r is MeshRenderer mr) return (mr, typeof(MeshRenderer));
            return null;
        }

        #endregion

        /// <summary>
        /// Genera la animacion lineal para un MRUnificarMateriales
        /// </summary>
        /// <param name="unifyMaterial">Componente MRUnificarMateriales a procesar</param>
        /// <returns>AnimationClip generado o null si falla</returns>
        public static AnimationClip GenerateAnimation(MRUnificarMateriales unifyMaterial)
        {
            if (unifyMaterial == null)
                throw new ArgumentNullException(nameof(unifyMaterial), "MRUnificarMateriales no puede ser null");

            // ETAPA 1: Validar datos
            Transform avatarRoot = FindAvatarRoot(unifyMaterial.transform);
            if (avatarRoot == null)
                throw new InvalidOperationException("No se pudo encontrar VRC_AvatarDescriptor en la jerarquia del GameObject");

            var animationData = unifyMaterial.CollectAnimationData();
            if (animationData.Count == 0)
                throw new InvalidOperationException("No hay slots vinculados a grupos validos para animar");

            // ETAPA 2: Crear el AnimationClip
            string animationName = unifyMaterial.AnimationName + MRAnimationSuffixes.LINEAR;
            var clip = CreateAnimationClip(animationName);

            // ETAPA 3: Generar curvas de material para cada slot
            foreach (var slotData in animationData)
            {
                AddMaterialCurveForSlot(clip, slotData, avatarRoot);
            }

            // ETAPA 4: Guardar la animacion
            SaveAnimation(clip, unifyMaterial.AnimationPath);

            return clip;
        }

        /// <summary>
        /// Busca el objeto raiz del avatar con VRC_AvatarDescriptor
        /// </summary>
        private static Transform FindAvatarRoot(Transform startTransform)
        {
            Transform current = startTransform;

            while (current != null)
            {
                if (current.GetComponent<VRC_AvatarDescriptor>() != null)
                    return current;
                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Crea un AnimationClip configurado correctamente
        /// </summary>
        private static AnimationClip CreateAnimationClip(string name)
        {
            return new AnimationClip
            {
                name = name,
                frameRate = MRAnimationConstants.FRAME_RATE,
                legacy = false
            };
        }

        /// <summary>
        /// Agrega una curva de material PPtr para un slot especifico
        /// </summary>
        private static void AddMaterialCurveForSlot(AnimationClip clip, UnifySlotAnimationData slotData, Transform avatarRoot)
        {
            var slot = slotData.Slot;
            if (!slot.IsValid) return;

            // Obtener tipo de renderer
            var rendererInfo = GetRendererAndType(slot.TargetRenderer);
            if (!rendererInfo.HasValue) return;

            var (renderer, rendererType) = rendererInfo.Value;

            // Calcular path relativo desde avatar root
            string relativePath = CalcPath(renderer.transform, avatarRoot);
            if (string.IsNullOrEmpty(relativePath)) return;

            // Crear binding para el material
            var binding = new EditorCurveBinding
            {
                path = relativePath,
                type = rendererType,
                propertyName = $"m_Materials.Array.data[{slot.MaterialIndex}]"
            };

            // Crear keyframes para cada material segun su distribucion de frames
            var keyframes = new List<ObjectReferenceKeyframe>();

            foreach (var range in slotData.FrameDistribution)
            {
                Material material = slotData.Materials[range.MaterialIndex];
                if (material == null) continue;

                // Keyframe al inicio de la region
                float startTime = ToSec(range.StartFrame);
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = startTime,
                    value = material
                });
            }

            // Keyframe final en frame 255 con el ultimo material
            if (slotData.Materials.Count > 0)
            {
                Material lastMaterial = slotData.Materials[slotData.Materials.Count - 1];
                float endTime = ToSec(MRAnimationConstants.TOTAL_FRAMES);

                // Solo agregar si no hay ya un keyframe en ese tiempo
                if (!keyframes.Any(k => Mathf.Abs(k.time - endTime) < 0.0001f))
                {
                    keyframes.Add(new ObjectReferenceKeyframe
                    {
                        time = endTime,
                        value = lastMaterial
                    });
                }
            }

            // Ordenar keyframes por tiempo
            keyframes = keyframes.OrderBy(k => k.time).ToList();

            // Asignar la curva al clip
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
        }

        /// <summary>
        /// Guarda la animacion en disco
        /// </summary>
        private static void SaveAnimation(AnimationClip clip, string savePath)
        {
            if (clip == null) return;

            if (string.IsNullOrEmpty(savePath))
                savePath = MRConstants.ANIMATION_OUTPUT_PATH;

            // Asegurar que el directorio existe
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            string fileName = $"{clip.name}{MRFileExtensions.ANIMATION}";
            string fullPath = Path.Combine(savePath, fileName).Replace('\\', '/');

            // Sobrescribir si existe
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
            if (existingClip != null)
            {
                AssetDatabase.DeleteAsset(fullPath);
            }

            AssetDatabase.CreateAsset(clip, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MR Unify Material] Animacion guardada: {fullPath}");
        }

        /// <summary>
        /// Valida si un MRUnificarMateriales puede generar animaciones
        /// </summary>
        public static bool CanGenerate(MRUnificarMateriales unifyMaterial, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (unifyMaterial == null)
            {
                errorMessage = "MRUnificarMateriales es null";
                return false;
            }

            Transform avatarRoot = FindAvatarRoot(unifyMaterial.transform);
            if (avatarRoot == null)
            {
                errorMessage = "No se encontro VRC_AvatarDescriptor en la jerarquia";
                return false;
            }

            if (string.IsNullOrEmpty(unifyMaterial.AnimationName))
            {
                errorMessage = "Nombre de animacion vacio";
                return false;
            }

            if (unifyMaterial.AlternativeMaterialCount == 0)
            {
                errorMessage = "No hay MR Alternative Material agregados";
                return false;
            }

            var animationData = unifyMaterial.CollectAnimationData();
            if (animationData.Count == 0)
            {
                errorMessage = "No hay slots vinculados a grupos validos";
                return false;
            }

            return true;
        }
    }
}
#endif
