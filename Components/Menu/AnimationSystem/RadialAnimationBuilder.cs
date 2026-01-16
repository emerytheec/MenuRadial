#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Core.Common;

namespace Bender_Dios.MenuRadial.AnimationSystem
{
    /// <summary>
    /// Sistema de generación de animaciones .anim para MR Radial Menu
    /// Implementa las especificaciones exactas del prompt por etapas
    /// </summary>
    public static class RadialAnimationBuilder
    {
        #region Helpers Utilitarios

        // Precisiones de tiempo - usando constantes centralizadas
        private static float ToSec(int frame) => (float)(frame / MRAnimationConstants.FRAME_RATE_DOUBLE);
        private static float ToSecClamp255(int frame) => (float)(Math.Min(frame, MRAnimationConstants.TOTAL_FRAMES) / MRAnimationConstants.FRAME_RATE_DOUBLE);

        // Cálculo de regiones (entero + sobrante en última)
        private static List<(int start, int end)> ComputeRegions(int N)
        {
            var regions = new List<(int start, int end)>(N);
            if (N <= 0) return regions;
            int steps = MRAnimationConstants.TOTAL_FRAMES / N;
            for (int i = 0; i < N - 1; i++)
                regions.Add((i * steps, (i + 1) * steps - 1));
            regions.Add(((N - 1) * steps, MRAnimationConstants.TOTAL_FRAMES));
            return regions;
        }

        // Path y renderer
        private static string CalcPath(Transform target, Transform avatarRoot)
            => AnimationUtility.CalculateTransformPath(target, avatarRoot);

        // Devuelve el renderer válido y su tipo, o null si no hay
        private static (Renderer renderer, System.Type type)? GetRendererAndType(GameObject go)
        {
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null) return (smr, typeof(SkinnedMeshRenderer));
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) return (mr, typeof(MeshRenderer));
            return null;
        }

        // Set/Append para PPtr (Material) - Dedupe por tiempo
        private static void SetOrAppendObjectRefKey(AnimationClip clip, EditorCurveBinding binding, float timeSec, UnityEngine.Object value)
        {
            var existing = AnimationUtility.GetObjectReferenceCurve(clip, binding) ?? new ObjectReferenceKeyframe[0];
            var map = new SortedDictionary<double, UnityEngine.Object>();
            foreach (var k in existing) map[(double)k.time] = k.value;

            map[(double)timeSec] = value; // sobreescribe si ya existía

            var keys = new ObjectReferenceKeyframe[map.Count];
            int idx = 0;
            foreach (var kv in map)
                keys[idx++] = new ObjectReferenceKeyframe { time = (float)kv.Key, value = kv.Value };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }

        // Set/Append para float con dedupe por tiempo
        private static void SetOrAppendFloatKey(AnimationClip clip, EditorCurveBinding binding, float timeSec, float value)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            
            // buscar key en mismo tiempo (tolerancia pequeña)
            int existingIndex = -1;
            for (int i = 0; i < curve.keys.Length; i++)
                if (Mathf.Abs(curve.keys[i].time - timeSec) <= 1e-6f) { existingIndex = i; break; }

            if (existingIndex >= 0)
            {
                var k = curve.keys[existingIndex];
                k.value = value;
                curve.MoveKey(existingIndex, k);
            }
            else
            {
                int idx = curve.AddKey(new Keyframe(timeSec, value));
                AnimationUtility.SetKeyLeftTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
            }

            // Asegura tangentes Constant en todos los keys
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        // Construcción de binding de material con validaciones
        private static bool TryBuildMaterialBinding(Transform avatarRoot, Renderer renderer, System.Type type, int slot, out EditorCurveBinding binding)
        {
            binding = default;
            var mats = renderer.sharedMaterials;
            if (mats == null || slot < 0 || slot >= mats.Length) return false;

            binding = new EditorCurveBinding
            {
                path = CalcPath(renderer.transform, avatarRoot),
                type = type,
                propertyName = $"m_Materials.Array.data[{slot}]"
            };
            return true;
        }

        #endregion
        
        /// <summary>
        /// Genera todas las animaciones necesarias para un MRUnificarObjetos
        /// </summary>
        /// <param name="menu">Componente MRUnificarObjetos a procesar</param>
        /// <returns>Lista de AnimationClip generados</returns>
        public static List<AnimationClip> GenerateAnimations(MRUnificarObjetos menu)
        {
            if (menu == null)
                throw new ArgumentNullException(nameof(menu), "MRUnificarObjetos no puede ser null");

            // ETAPA 1: Acceso y Validación de Datos
            var animationData = ExtractAndValidateData(menu);
            
            // ETAPA 2: Cálculo de Regiones Temporales  
            var timeRegions = CalculateTimeRegions(animationData.ValidFrames.Count);
            
            // ETAPA 3: Generación de Curvas de Animación
            var animations = GenerateAnimationCurves(animationData, timeRegions);
            
            // ETAPA 4: Naming y Guardado
            SaveAnimations(animations, animationData);
            
            return animations;
        }
        
        #region Etapa 1: Acceso y Validación de Datos
        
        /// <summary>
        /// Datos validados extraídos del MRUnificarObjetos
        /// </summary>
        private struct AnimationData
        {
            public Transform AvatarRoot;
            public List<MRAgruparObjetos> ValidFrames;
            public string AnimationName;
            public string AnimationPath;
            public string AnimationType;
        }
        
        /// <summary>
        /// Extrae y valida los datos necesarios del componente MR Radial Menu
        /// </summary>
        private static AnimationData ExtractAndValidateData(MRUnificarObjetos menu)
        {
            // 1. Detectar el objeto raíz del avatar (con VRC_AvatarDescriptor)
            Transform avatarRoot = FindAvatarRoot(menu.transform);
            if (avatarRoot == null)
                throw new InvalidOperationException("No se pudo encontrar VRC_AvatarDescriptor en la jerarquía del GameObject");
            
            // 2. Obtener la lista de MR Frame Object desde el componente
            var allFrames = menu.FrameObjects ?? new List<MRAgruparObjetos>();
            
            // 3. Ignorar entradas null
            var validFrames = allFrames.Where(frame => frame != null).ToList();
            
            // 4. Verificar que cada MR Frame Object tenga al menos una sección válida
            var framesToProcess = new List<MRAgruparObjetos>();
            foreach (var frame in validFrames)
            {
                if (HasValidSection(frame))
                    framesToProcess.Add(frame);
            }
            
            // 5. Lanzar errores claros si faltan datos críticos
            if (framesToProcess.Count == 0)
                throw new InvalidOperationException("No hay frames válidos configurados. Cada frame debe tener al menos un GameObject, material o blendshape configurado");
            
            string animationName = !string.IsNullOrEmpty(menu.AnimationName) ? menu.AnimationName : menu.name;
            if (string.IsNullOrEmpty(animationName))
                throw new InvalidOperationException("Nombre de animación requerido");
            
            string animationType = DetermineAnimationType(framesToProcess.Count);
            
            return new AnimationData
            {
                AvatarRoot = avatarRoot,
                ValidFrames = framesToProcess,
                AnimationName = animationName,
                AnimationPath = menu.AnimationPath,
                AnimationType = animationType
            };
        }
        
        /// <summary>
        /// Encuentra el objeto raíz del avatar buscando VRC_AvatarDescriptor
        /// </summary>
        private static Transform FindAvatarRoot(Transform startTransform)
        {
            Transform current = startTransform;
            
            // Buscar hacia arriba en la jerarquía
            while (current != null)
            {
                if (current.GetComponent<VRC_AvatarDescriptor>() != null)
                    return current;
                current = current.parent;
            }
            
            return null;
        }
        
        /// <summary>
        /// Verifica si un frame tiene al menos una sección válida (blendshape, material o GameObject)
        /// </summary>
        private static bool HasValidSection(MRAgruparObjetos frame)
        {
            if (frame == null) return false;
            
            // Verificar GameObjects válidos
            if (frame.ObjectReferences != null && frame.ObjectReferences.Any(obj => obj != null && obj.GameObject != null))
                return true;
            
            // Verificar materiales válidos  
            if (frame.MaterialReferences != null && frame.MaterialReferences.Any(mat => mat != null && mat.TargetRenderer != null))
                return true;
            
            // Verificar blendshapes válidos
            if (frame.BlendshapeReferences != null && frame.BlendshapeReferences.Any(blend => blend != null && blend.TargetRenderer != null && !string.IsNullOrEmpty(blend.BlendshapeName)))
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Determina el tipo de animación basado en el número de frames
        /// </summary>
        private static string DetermineAnimationType(int frameCount)
        {
            return frameCount switch
            {
                1 => "OnOff",
                2 => "AB", 
                >= 3 => "Linear",
                _ => "None"
            };
        }
        
        #endregion
        
        #region Etapa 2: Cálculo de Regiones Temporales
        
        /// <summary>
        /// Información de región temporal para animaciones
        /// </summary>
        private struct TimeRegion
        {
            public int StartStep;
            public int EndStep;
            public int StepsInRegion;
        }
        
        /// <summary>
        /// Calcula como se divide la línea de tiempo de la animación .anim
        /// </summary>
        private static List<TimeRegion> CalculateTimeRegions(int frameCount)
        {
            var regions = new List<TimeRegion>();

            if (frameCount <= 0) return regions;

            // Para On/Off y A/B no usamos TOTAL_FRAMES pasos, solo paso 0
            if (frameCount <= 2)
            {
                regions.Add(new TimeRegion { StartStep = 0, EndStep = 0, StepsInRegion = 1 });
                return regions;
            }

            // Para Linear: dividir TOTAL_FRAMES pasos en N regiones
            int stepsPerRegion = (int)((float)MRAnimationConstants.TOTAL_FRAMES / frameCount);
            int currentStep = 0;

            for (int i = 0; i < frameCount; i++)
            {
                int stepsInThisRegion = stepsPerRegion;

                // La última región recibe los pasos sobrantes para completar TOTAL_FRAMES
                if (i == frameCount - 1)
                {
                    stepsInThisRegion = MRAnimationConstants.TOTAL_FRAMES - currentStep;
                }

                regions.Add(new TimeRegion
                {
                    StartStep = currentStep,
                    EndStep = currentStep + stepsInThisRegion - 1,
                    StepsInRegion = stepsInThisRegion
                });

                currentStep += stepsInThisRegion;
            }

            return regions;
        }
        
        #endregion
        
        #region Etapa 3: Generación de Curvas de Animación
        
        /// <summary>
        /// Genera las curvas de animación para cada tipo: blendshape, material y GameObject
        /// </summary>
        private static List<AnimationClip> GenerateAnimationCurves(AnimationData animationData, List<TimeRegion> timeRegions)
        {
            var animations = new List<AnimationClip>();
            
            switch (animationData.AnimationType)
            {
                case "OnOff":
                    animations.AddRange(GenerateOnOffAnimations(animationData));
                    break;
                    
                case "AB":
                    animations.AddRange(GenerateABAnimations(animationData));
                    break;
                    
                case "Linear":
                    animations.Add(GenerateLinearAnimation(animationData, timeRegions));
                    break;
            }
            
            return animations;
        }
        
        /// <summary>
        /// Genera animaciones On/Off (1 frame)
        /// </summary>
        private static List<AnimationClip> GenerateOnOffAnimations(AnimationData animationData)
        {
            var animations = new List<AnimationClip>();
            
            var onAnimation = CreateAnimationClip($"{animationData.AnimationName}_on");
            var offAnimation = CreateAnimationClip($"{animationData.AnimationName}_off");
            
            // Configurar frameRate y duración correcta
            onAnimation.frameRate = MRAnimationConstants.FRAME_RATE;
            offAnimation.frameRate = MRAnimationConstants.FRAME_RATE;
            
            var frame = animationData.ValidFrames[0];
            
            // Añadir curvas en t=0f (solo una clave por binding)
            AddFrameCurvesToClipOnOffAB(onAnimation, frame, animationData.AvatarRoot, 0f, true);  // Estado activo
            AddFrameCurvesToClipOnOffAB(offAnimation, frame, animationData.AvatarRoot, 0f, false); // Estado base
            
            animations.Add(onAnimation);
            animations.Add(offAnimation);
            
            return animations;
        }
        
        /// <summary>
        /// Genera animaciones A/B (2 frames)
        /// </summary>
        private static List<AnimationClip> GenerateABAnimations(AnimationData animationData)
        {
            var animations = new List<AnimationClip>();
            
            var aAnimation = CreateAnimationClip($"{animationData.AnimationName}_A");
            var bAnimation = CreateAnimationClip($"{animationData.AnimationName}_B");
            
            // Configurar frameRate y duración correcta
            aAnimation.frameRate = MRAnimationConstants.FRAME_RATE;
            bAnimation.frameRate = MRAnimationConstants.FRAME_RATE;
            
            var frameA = animationData.ValidFrames[0];
            var frameB = animationData.ValidFrames[1];
            
            // Añadir curvas en t=0f
            AddFrameCurvesToClipOnOffAB(aAnimation, frameA, animationData.AvatarRoot, 0f, true);  // Frame A activo
            AddFrameCurvesToClipOnOffAB(aAnimation, frameB, animationData.AvatarRoot, 0f, false); // Frame B base
            
            AddFrameCurvesToClipOnOffAB(bAnimation, frameA, animationData.AvatarRoot, 0f, false); // Frame A base
            AddFrameCurvesToClipOnOffAB(bAnimation, frameB, animationData.AvatarRoot, 0f, true);  // Frame B activo
            
            animations.Add(aAnimation);
            animations.Add(bAnimation);
            
            return animations;
        }
        
        /// <summary>
        /// Genera animación Linear (3+ frames)
        /// </summary>
        private static AnimationClip GenerateLinearAnimation(AnimationData animationData, List<TimeRegion> timeRegions)
        {
            var animation = CreateAnimationClip($"{animationData.AnimationName}_lin");

            // Configurar frameRate usando constantes centralizadas
            animation.frameRate = MRAnimationConstants.FRAME_RATE;

            // Calcular tiempo final usando helper de precisión
            float tEnd = ToSec(MRAnimationConstants.TOTAL_FRAMES);
            
            // Diccionarios para agrupar bindings por tipo
            // CORREGIDO: Ahora guarda isActive para respetar el valor configurado en cada frame
            var gameObjectBindings = new Dictionary<string, List<(int frameIndex, float tStart, bool isActive)>>();
            var materialBindings = new Dictionary<string, List<(int frameIndex, float tStart, Material activeMat, Material baseMat)>>();
            var blendshapeBindings = new Dictionary<string, List<(int frameIndex, float tStart, float activeValue, float baseValue)>>();
            
            // PASO 1: Recopilar todos los bindings y sus tiempos de región
            for (int regionIndex = 0; regionIndex < timeRegions.Count; regionIndex++)
            {
                var region = timeRegions[regionIndex];
                var activeFrame = animationData.ValidFrames[regionIndex];
                
                // Calcular tiempo en segundos del inicio de esta región usando helper
                float tStart = ToSec(region.StartStep);
                
                // GameObjects
                if (activeFrame.ObjectReferences?.Any() == true)
                {
                    foreach (var objRef in activeFrame.ObjectReferences)
                    {
                        if (objRef?.GameObject == null) continue;

                        string relativePath = GetRelativePath(animationData.AvatarRoot, objRef.GameObject.transform);
                        if (string.IsNullOrEmpty(relativePath)) continue;

                        string bindingKey = $"{relativePath}|GameObject|m_IsActive";

                        if (!gameObjectBindings.ContainsKey(bindingKey))
                            gameObjectBindings[bindingKey] = new List<(int frameIndex, float tStart, bool isActive)>();

                        // CORREGIDO: Guardar el valor IsActive configurado por el usuario
                        gameObjectBindings[bindingKey].Add((regionIndex, tStart, objRef.IsActive));
                    }
                }
                
                // Materiales
                if (activeFrame.MaterialReferences?.Any() == true)
                {
                    foreach (var matRef in activeFrame.MaterialReferences)
                    {
                        if (matRef?.TargetRenderer == null) continue;
                        
                        var skinnedRenderer = matRef.TargetRenderer as SkinnedMeshRenderer;
                        if (skinnedRenderer == null) continue;
                        
                        if (matRef.MaterialIndex < 0 || matRef.MaterialIndex >= skinnedRenderer.sharedMaterials.Length)
                            continue;
                        
                        string relativePath = GetRelativePath(animationData.AvatarRoot, skinnedRenderer.transform);
                        if (string.IsNullOrEmpty(relativePath)) continue;
                        
                        string bindingKey = $"{relativePath}|{matRef.MaterialIndex}";
                        
                        Material activeMat = matRef.HasAlternativeMaterial ? matRef.AlternativeMaterial : matRef.OriginalMaterial;
                        Material baseMat = matRef.OriginalMaterial;
                        
                        if (activeMat == null || baseMat == null) continue;
                        
                        if (!materialBindings.ContainsKey(bindingKey))
                            materialBindings[bindingKey] = new List<(int frameIndex, float tStart, Material activeMat, Material baseMat)>();
                            
                        materialBindings[bindingKey].Add((regionIndex, tStart, activeMat, baseMat));
                    }
                }
                
                // Blendshapes
                if (activeFrame.BlendshapeReferences?.Any() == true)
                {
                    foreach (var blendRef in activeFrame.BlendshapeReferences)
                    {
                        if (blendRef?.TargetRenderer == null || string.IsNullOrEmpty(blendRef.BlendshapeName)) 
                            continue;
                        
                        string relativePath = GetRelativePath(animationData.AvatarRoot, blendRef.TargetRenderer.transform);
                        if (string.IsNullOrEmpty(relativePath)) continue;
                        
                        string bindingKey = $"{relativePath}|SkinnedMeshRenderer|blendShape.{blendRef.BlendshapeName}";
                        
                        if (!blendshapeBindings.ContainsKey(bindingKey))
                            blendshapeBindings[bindingKey] = new List<(int frameIndex, float tStart, float activeValue, float baseValue)>();
                            
                        blendshapeBindings[bindingKey].Add((regionIndex, tStart, blendRef.Value, 0f));
                    }
                }
            }
            
            // PASO 2: Crear curvas para GameObjects
            foreach (var kvp in gameObjectBindings)
            {
                var bindingKey = kvp.Key;
                var regions = kvp.Value;

                var bindingParts = bindingKey.Split('|');
                string path = bindingParts[0];

                var curve = new AnimationCurve();

                // Añadir keyframes para cada región
                for (int regionIndex = 0; regionIndex < timeRegions.Count; regionIndex++)
                {
                    float tStart = ToSec(timeRegions[regionIndex].StartStep);

                    // CORREGIDO: Buscar si el objeto está registrado en esta región y usar su valor IsActive
                    var regionData = regions.FirstOrDefault(r => r.frameIndex == regionIndex);
                    float value;
                    if (regions.Any(r => r.frameIndex == regionIndex))
                    {
                        // El objeto está en esta región, usar su valor IsActive configurado
                        value = regionData.isActive ? 1f : 0f;
                    }
                    else
                    {
                        // El objeto NO está en esta región, ponerlo en 0
                        value = 0f;
                    }

                    var keyframe = new Keyframe(tStart, value);
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = 0f;
                    var idx = curve.AddKey(keyframe);

                    AnimationUtility.SetKeyLeftTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
                }

                // Keyframe final en tEnd con valor de la última región
                var lastRegionData = regions.FirstOrDefault(r => r.frameIndex == timeRegions.Count - 1);
                float finalValue;
                if (regions.Any(r => r.frameIndex == timeRegions.Count - 1))
                {
                    finalValue = lastRegionData.isActive ? 1f : 0f;
                }
                else
                {
                    finalValue = 0f;
                }

                var finalKeyframe = new Keyframe(tEnd, finalValue);
                finalKeyframe.inTangent = 0f;
                finalKeyframe.outTangent = 0f;
                var finalIdx = curve.AddKey(finalKeyframe);

                AnimationUtility.SetKeyLeftTangentMode(curve, finalIdx, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, finalIdx, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetEditorCurve(animation, new EditorCurveBinding
                {
                    path = path,
                    type = typeof(GameObject),
                    propertyName = "m_IsActive"
                }, curve);
            }
            
            // PASO 3: Crear curvas para Materiales (usando PPtr)
            foreach (var kvp in materialBindings)
            {
                var bindingKey = kvp.Key;
                var regions = kvp.Value;
                
                var bindingParts = bindingKey.Split('|');
                string path = bindingParts[0];
                int materialIndex = int.Parse(bindingParts[1]);
                
                var keyframes = new List<ObjectReferenceKeyframe>();
                
                // Añadir keyframes para cada región
                for (int regionIndex = 0; regionIndex < timeRegions.Count; regionIndex++)
                {
                    float tStart = ToSec(timeRegions[regionIndex].StartStep);
                    
                    var activeRegion = regions.FirstOrDefault(r => r.frameIndex == regionIndex);
                    Material materialToUse = activeRegion.activeMat != null ? activeRegion.activeMat : 
                                           regions.FirstOrDefault().baseMat;
                    
                    keyframes.Add(new ObjectReferenceKeyframe
                    {
                        time = tStart,
                        value = materialToUse
                    });
                }
                
                // Keyframe final en tEnd con material de la última región
                var lastRegion = regions.FirstOrDefault(r => r.frameIndex == timeRegions.Count - 1);
                Material finalMaterial = lastRegion.activeMat != null ? lastRegion.activeMat : 
                                       regions.FirstOrDefault().baseMat;
                
                keyframes.Add(new ObjectReferenceKeyframe
                {
                    time = tEnd,
                    value = finalMaterial
                });
                
                AnimationUtility.SetObjectReferenceCurve(animation, new EditorCurveBinding
                {
                    path = path,
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = $"m_Materials.Array.data[{materialIndex}]"
                }, keyframes.ToArray());
            }
            
            // PASO 4: Crear curvas para Blendshapes
            foreach (var kvp in blendshapeBindings)
            {
                var bindingKey = kvp.Key;
                var regions = kvp.Value;
                
                var bindingParts = bindingKey.Split('|');
                string path = bindingParts[0];
                string propertyName = bindingParts[2];
                
                var curve = new AnimationCurve();
                
                // Añadir keyframes para cada región
                for (int regionIndex = 0; regionIndex < timeRegions.Count; regionIndex++)
                {
                    float tStart = ToSec(timeRegions[regionIndex].StartStep);
                    
                    var activeRegion = regions.FirstOrDefault(r => r.frameIndex == regionIndex);
                    float value = activeRegion.activeValue != 0f ? activeRegion.activeValue : activeRegion.baseValue;
                    
                    var keyframe = new Keyframe(tStart, value);
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = 0f;
                    var idx = curve.AddKey(keyframe);
                    
                    AnimationUtility.SetKeyLeftTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, idx, AnimationUtility.TangentMode.Constant);
                }
                
                // Keyframe final en tEnd con valor de la última región
                var lastRegion = regions.FirstOrDefault(r => r.frameIndex == timeRegions.Count - 1);
                float finalValue = lastRegion.activeValue != 0f ? lastRegion.activeValue : lastRegion.baseValue;
                
                var finalKeyframe = new Keyframe(tEnd, finalValue);
                finalKeyframe.inTangent = 0f;
                finalKeyframe.outTangent = 0f;
                var finalIdx = curve.AddKey(finalKeyframe);
                
                AnimationUtility.SetKeyLeftTangentMode(curve, finalIdx, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, finalIdx, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetEditorCurve(animation, new EditorCurveBinding
                {
                    path = path,
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = propertyName
                }, curve);
            }
            
            return animation;
        }
        
        /// <summary>
        /// Crea un AnimationClip base con interpolación Constant
        /// </summary>
        private static AnimationClip CreateAnimationClip(string name)
        {
            var clip = new AnimationClip
            {
                name = name,
                legacy = false
            };
            
            return clip;
        }
        
        /// <summary>
        /// Añade las curvas de un frame al AnimationClip especificado (SOLO para Linear)
        /// </summary>
        private static void AddFrameCurvesToClip(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            // Curvas para GameObjects
            AddGameObjectCurvesToClip(clip, frame, avatarRoot, time, useActiveValues);
            
            // Curvas para Materiales
            AddMaterialCurvesToClip(clip, frame, avatarRoot, time, useActiveValues);
            
            // Curvas para Blendshapes
            AddBlendshapeCurvesToClip(clip, frame, avatarRoot, time, useActiveValues);
        }
        
        /// <summary>
        /// Añade las curvas de un frame al AnimationClip especificado (para On/Off/A/B con PPtr)
        /// </summary>
        private static void AddFrameCurvesToClipOnOffAB(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            // Curvas para GameObjects
            AddGameObjectCurvesToClip(clip, frame, avatarRoot, time, useActiveValues);
            
            // Curvas para Materiales usando PPtr
            AddMaterialCurvesToClipPPtr(clip, frame, avatarRoot, time, useActiveValues);
            
            // Curvas para Blendshapes
            AddBlendshapeCurvesToClip(clip, frame, avatarRoot, time, useActiveValues);
        }
        
        /// <summary>
        /// Añade curvas de GameObject al clip
        /// </summary>
        private static void AddGameObjectCurvesToClip(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            if (frame.ObjectReferences == null) return;

            foreach (var objRef in frame.ObjectReferences)
            {
                if (objRef?.GameObject == null) continue;

                string relativePath = CalcPath(objRef.GameObject.transform, avatarRoot);
                if (string.IsNullOrEmpty(relativePath)) continue;

                // CORREGIDO: Cuando useActiveValues=false, usar el INVERSO de objRef.IsActive
                // Esto asegura que en animaciones A/B los objetos alternen correctamente
                float value = useActiveValues ? (objRef.IsActive ? 1f : 0f) : (objRef.IsActive ? 0f : 1f);

                var binding = new EditorCurveBinding
                {
                    path = relativePath,
                    type = typeof(GameObject),
                    propertyName = "m_IsActive"
                };

                // Usar helper para añadir clave con dedupe
                SetOrAppendFloatKey(clip, binding, time, value);
            }
        }
        
        /// <summary>
        /// Añade curvas de Material al clip (usado solo por linear - OBSOLETO para On/Off/A/B)
        /// </summary>
        private static void AddMaterialCurvesToClip(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            if (frame.MaterialReferences == null) return;
            
            foreach (var matRef in frame.MaterialReferences)
            {
                if (matRef?.TargetRenderer == null) continue;
                
                // Validar que el GameObject existe y tiene SkinnedMeshRenderer
                var skinnedRenderer = matRef.TargetRenderer as SkinnedMeshRenderer;
                if (skinnedRenderer == null) continue;
                
                // Validar que el índice del material es válido
                if (matRef.MaterialIndex < 0 || matRef.MaterialIndex >= skinnedRenderer.sharedMaterials.Length) 
                    continue;
                
                string relativePath = GetRelativePath(avatarRoot, skinnedRenderer.transform);
                if (string.IsNullOrEmpty(relativePath)) continue;
                
                var curve = new AnimationCurve();
                
                // Para materiales activos usamos el material alternativo si existe, sino valor base
                float value = useActiveValues ? 
                    (matRef.HasAlternativeMaterial ? 1f : 0f) : 
                    0f;
                
                var keyframe = new Keyframe(time, value);
                keyframe.inTangent = 0f;
                keyframe.outTangent = 0f;
                curve.AddKey(keyframe);
                
                // Aplicar interpolación Constant
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                }
                
                string propertyPath = $"m_Materials.Array.data[{matRef.MaterialIndex}]";
                clip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), propertyPath, curve);
            }
        }
        
        /// <summary>
        /// Añade curvas de Material al clip usando PPtr (para On/Off/A/B)
        /// </summary>
        private static void AddMaterialCurvesToClipPPtr(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            if (frame.MaterialReferences == null) return;
            
            foreach (var matRef in frame.MaterialReferences)
            {
                if (matRef?.TargetRenderer == null) continue;
                
                // Usar helper para obtener renderer y tipo
                var rendererInfo = GetRendererAndType(matRef.TargetRenderer.gameObject);
                if (!rendererInfo.HasValue) continue;
                
                var (targetRenderer, rendererType) = rendererInfo.Value;
                
                // Determinar material a usar
                Material materialToUse = null;
                if (useActiveValues)
                {
                    materialToUse = matRef.HasAlternativeMaterial ? matRef.AlternativeMaterial : matRef.OriginalMaterial;
                }
                else
                {
                    materialToUse = matRef.OriginalMaterial;
                }
                
                if (materialToUse == null) continue;
                
                // Usar helper para construir binding con validaciones
                if (!TryBuildMaterialBinding(avatarRoot, targetRenderer, rendererType, matRef.MaterialIndex, out var binding))
                    continue;
                
                // Usar helper para añadir clave con dedupe
                SetOrAppendObjectRefKey(clip, binding, time, materialToUse);
            }
        }
        
        /// <summary>
        /// Añade curvas de Blendshape al clip
        /// </summary>
        private static void AddBlendshapeCurvesToClip(AnimationClip clip, MRAgruparObjetos frame, Transform avatarRoot, float time, bool useActiveValues)
        {
            if (frame.BlendshapeReferences == null) return;
            
            foreach (var blendRef in frame.BlendshapeReferences)
            {
                if (blendRef?.TargetRenderer == null || string.IsNullOrEmpty(blendRef.BlendshapeName)) continue;
                
                string relativePath = CalcPath(blendRef.TargetRenderer.transform, avatarRoot);
                if (string.IsNullOrEmpty(relativePath)) continue;
                
                float value = useActiveValues ? blendRef.Value : 0f;
                
                var binding = new EditorCurveBinding
                {
                    path = relativePath,
                    type = typeof(SkinnedMeshRenderer),
                    propertyName = $"blendShape.{blendRef.BlendshapeName}"
                };
                
                // Usar helper para añadir clave con dedupe
                SetOrAppendFloatKey(clip, binding, time, value);
            }
        }
        
        /// <summary>
        /// Obtiene la ruta relativa desde el avatar root hasta el transform especificado
        /// </summary>
        private static string GetRelativePath(Transform avatarRoot, Transform target)
        {
            if (avatarRoot == null || target == null) return string.Empty;
            
            var pathSegments = new List<string>();
            Transform current = target;
            
            while (current != null && current != avatarRoot)
            {
                pathSegments.Insert(0, current.name);
                current = current.parent;
            }
            
            if (current != avatarRoot) return string.Empty; // Target no es hijo de avatarRoot
            
            return string.Join("/", pathSegments);
        }
        
        #endregion
        
        #region Etapa 4: Naming y Guardado
        
        /// <summary>
        /// Guarda los archivos .anim de forma clara y sin colisiones
        /// </summary>
        private static void SaveAnimations(List<AnimationClip> animations, AnimationData animationData)
        {
            if (animations == null || animations.Count == 0) return;
            
            string savePath = animationData.AnimationPath;
            if (string.IsNullOrEmpty(savePath))
                savePath = Core.Common.MRConstants.ANIMATION_OUTPUT_PATH;
            
            // Asegurar que el directorio existe
            Directory.CreateDirectory(savePath);
            
            foreach (var animation in animations)
            {
                if (animation == null) continue;
                
                string fileName = $"{animation.name}.anim";
                string fullPath = Path.Combine(savePath, fileName);
                
                // Normalizar la ruta para Unity
                fullPath = fullPath.Replace('\\', '/');

                // CORREGIDO: Sobrescribir archivo existente en lugar de crear uno nuevo
                var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
                if (existingClip != null)
                {
                    AssetDatabase.DeleteAsset(fullPath);
                }

                AssetDatabase.CreateAsset(animation, fullPath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// Recopila todos los bindings posibles de un frame
        /// </summary>
        private static void CollectAllBindings(HashSet<string> allBindings, MRAgruparObjetos frame, Transform avatarRoot)
        {
            if (frame == null) return;
            
            // GameObjects
            if (frame.ObjectReferences != null)
            {
                foreach (var objRef in frame.ObjectReferences)
                {
                    if (objRef?.GameObject == null) continue;
                    string relativePath = GetRelativePath(avatarRoot, objRef.GameObject.transform);
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    allBindings.Add($"{relativePath}|GameObject|m_IsActive");
                }
            }
            
            // Materials
            if (frame.MaterialReferences != null)
            {
                foreach (var matRef in frame.MaterialReferences)
                {
                    if (matRef?.TargetRenderer == null) continue;
                    var skinnedRenderer = matRef.TargetRenderer as SkinnedMeshRenderer;
                    if (skinnedRenderer == null) continue;
                    if (matRef.MaterialIndex < 0 || matRef.MaterialIndex >= skinnedRenderer.sharedMaterials.Length) continue;
                    
                    string relativePath = GetRelativePath(avatarRoot, skinnedRenderer.transform);
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    string propertyPath = $"m_Materials.Array.data[{matRef.MaterialIndex}]";
                    allBindings.Add($"{relativePath}|SkinnedMeshRenderer|{propertyPath}");
                }
            }
            
            // Blendshapes
            if (frame.BlendshapeReferences != null)
            {
                foreach (var blendRef in frame.BlendshapeReferences)
                {
                    if (blendRef?.TargetRenderer == null || string.IsNullOrEmpty(blendRef.BlendshapeName)) continue;
                    string relativePath = GetRelativePath(avatarRoot, blendRef.TargetRenderer.transform);
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    string propertyPath = $"blendShape.{blendRef.BlendshapeName}";
                    allBindings.Add($"{relativePath}|SkinnedMeshRenderer|{propertyPath}");
                }
            }
        }
        
        /// <summary>
        /// Verifica si un binding existe en un frame específico
        /// </summary>
        private static bool BindingExistsInFrame(string bindingKey, MRAgruparObjetos frame, Transform avatarRoot)
        {
            var bindingParts = bindingKey.Split('|');
            if (bindingParts.Length != 3) return false;
            
            string path = bindingParts[0];
            string typeName = bindingParts[1];
            string propertyName = bindingParts[2];
            
            // GameObjects
            if (typeName == "GameObject" && propertyName == "m_IsActive")
            {
                if (frame.ObjectReferences != null)
                {
                    return frame.ObjectReferences.Any(obj => 
                        obj?.GameObject != null && 
                        GetRelativePath(avatarRoot, obj.GameObject.transform) == path);
                }
            }
            
            // Materials
            if (typeName == "SkinnedMeshRenderer" && propertyName.StartsWith("m_Materials.Array.data["))
            {
                if (frame.MaterialReferences != null)
                {
                    return frame.MaterialReferences.Any(mat => 
                        mat?.TargetRenderer != null && 
                        GetRelativePath(avatarRoot, mat.TargetRenderer.transform) == path &&
                        propertyName == $"m_Materials.Array.data[{mat.MaterialIndex}]");
                }
            }
            
            // Blendshapes
            if (typeName == "SkinnedMeshRenderer" && propertyName.StartsWith("blendShape."))
            {
                if (frame.BlendshapeReferences != null)
                {
                    string blendshapeName = propertyName.Substring("blendShape.".Length);
                    return frame.BlendshapeReferences.Any(blend => 
                        blend?.TargetRenderer != null && 
                        GetRelativePath(avatarRoot, blend.TargetRenderer.transform) == path &&
                        blend.BlendshapeName == blendshapeName);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Obtiene el valor de un binding específico para un frame
        /// </summary>
        private static float GetBindingValueForFrame(string bindingKey, MRAgruparObjetos frame, Transform avatarRoot, bool useActiveValues)
        {
            var bindingParts = bindingKey.Split('|');
            if (bindingParts.Length != 3) return 0f;
            
            string path = bindingParts[0];
            string typeName = bindingParts[1];
            string propertyName = bindingParts[2];
            
            // GameObjects
            if (typeName == "GameObject" && propertyName == "m_IsActive" && frame.ObjectReferences != null)
            {
                var objRef = frame.ObjectReferences.FirstOrDefault(obj =>
                    obj?.GameObject != null &&
                    GetRelativePath(avatarRoot, obj.GameObject.transform) == path);

                if (objRef != null)
                {
                    // CORREGIDO: Cuando useActiveValues=false, usar el INVERSO de objRef.IsActive
                    return useActiveValues ? (objRef.IsActive ? 1f : 0f) : (objRef.IsActive ? 0f : 1f);
                }
            }
            
            // Materials
            if (typeName == "SkinnedMeshRenderer" && propertyName.StartsWith("m_Materials.Array.data[") && frame.MaterialReferences != null)
            {
                var matRef = frame.MaterialReferences.FirstOrDefault(mat => 
                    mat?.TargetRenderer != null && 
                    GetRelativePath(avatarRoot, mat.TargetRenderer.transform) == path &&
                    propertyName == $"m_Materials.Array.data[{mat.MaterialIndex}]");
                    
                if (matRef != null)
                {
                    return useActiveValues ? (matRef.HasAlternativeMaterial ? 1f : 0f) : 0f;
                }
            }
            
            // Blendshapes
            if (typeName == "SkinnedMeshRenderer" && propertyName.StartsWith("blendShape.") && frame.BlendshapeReferences != null)
            {
                string blendshapeName = propertyName.Substring("blendShape.".Length);
                var blendRef = frame.BlendshapeReferences.FirstOrDefault(blend => 
                    blend?.TargetRenderer != null && 
                    GetRelativePath(avatarRoot, blend.TargetRenderer.transform) == path &&
                    blend.BlendshapeName == blendshapeName);
                    
                if (blendRef != null)
                {
                    return useActiveValues ? blendRef.Value : 0f;
                }
            }
            
            return 0f;
        }
        
        #endregion
    }
}
#endif