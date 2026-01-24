using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Profiling;
using Bender_Dios.MenuRadial.Components.PesoTexturas;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Calculador de tamano de assets usando Profiler API.
    /// Calcula meshes, animaciones, materials y audio.
    /// </summary>
    public static class AssetSizeCalculator
    {
        #region Public Methods - Meshes

        /// <summary>
        /// Escanea todos los meshes de un GameObject y calcula su VRAM.
        /// </summary>
        public static MeshScanResult ScanMeshes(GameObject root)
        {
            var result = new MeshScanResult();

            if (root == null)
                return result;

            var processedMeshes = new HashSet<Mesh>();

            // Escanear SkinnedMeshRenderers
            var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                if (smr == null || smr.sharedMesh == null)
                    continue;

                var mesh = smr.sharedMesh;
                if (processedMeshes.Contains(mesh))
                    continue;

                processedMeshes.Add(mesh);

                var info = MeshVRAMCalculator.GetMeshInfo(mesh, isSkinned: true);
                result.MeshInfos.Add(info);
                result.TotalVertexVRAM += info.VertexAttributesVRAM;
                result.TotalIndexVRAM += info.IndexBufferVRAM;
                result.TotalBlendShapeVRAM += info.BlendShapesVRAM;
                result.TotalVertexCount += info.VertexCount;
                result.TotalTriangleCount += info.TriangleCount;
                result.TotalBlendShapeCount += info.BlendShapeCount;
            }

            // Escanear MeshFilters (meshes estaticos)
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var mesh = mf.sharedMesh;
                if (processedMeshes.Contains(mesh))
                    continue;

                processedMeshes.Add(mesh);

                var info = MeshVRAMCalculator.GetMeshInfo(mesh, isSkinned: false);
                result.MeshInfos.Add(info);
                result.TotalVertexVRAM += info.VertexAttributesVRAM;
                result.TotalIndexVRAM += info.IndexBufferVRAM;
                // Los MeshFilter no tienen blend shapes
                result.TotalVertexCount += info.VertexCount;
                result.TotalTriangleCount += info.TriangleCount;
            }

            // Escanear ParticleSystemRenderers
            var particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var psr in particleRenderers)
            {
                if (psr == null || psr.mesh == null)
                    continue;

                var mesh = psr.mesh;
                if (processedMeshes.Contains(mesh))
                    continue;

                processedMeshes.Add(mesh);

                var info = MeshVRAMCalculator.GetMeshInfo(mesh, isSkinned: false);
                result.MeshInfos.Add(info);
                result.TotalVertexVRAM += info.VertexAttributesVRAM;
                result.TotalIndexVRAM += info.IndexBufferVRAM;
                result.TotalVertexCount += info.VertexCount;
                result.TotalTriangleCount += info.TriangleCount;
            }

            result.MeshCount = processedMeshes.Count;
            return result;
        }

        #endregion

        #region Public Methods - Animations

        /// <summary>
        /// Escanea todas las animaciones del avatar y calcula su tamano.
        /// </summary>
        public static AnimationScanResult ScanAnimations(GameObject root)
        {
            var result = new AnimationScanResult();

            if (root == null)
                return result;

            var processedClips = new HashSet<AnimationClip>();

            // Buscar en Animator principal
            var animator = root.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                CollectAnimationClips(animator.runtimeAnimatorController, processedClips, result);
            }

#if VRC_SDK_VRCSDK3
            // Buscar en VRCAvatarDescriptor
            var avatarDescriptor = root.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                // Base animation layers
                foreach (var layer in avatarDescriptor.baseAnimationLayers)
                {
                    if (layer.animatorController != null)
                    {
                        CollectAnimationClips(layer.animatorController, processedClips, result);
                    }
                }

                // Special animation layers
                foreach (var layer in avatarDescriptor.specialAnimationLayers)
                {
                    if (layer.animatorController != null)
                    {
                        CollectAnimationClips(layer.animatorController, processedClips, result);
                    }
                }
            }
#endif

            // Buscar Animators en hijos
            var childAnimators = root.GetComponentsInChildren<Animator>(true);
            foreach (var childAnimator in childAnimators)
            {
                if (childAnimator == animator)
                    continue;

                if (childAnimator.runtimeAnimatorController != null)
                {
                    CollectAnimationClips(childAnimator.runtimeAnimatorController, processedClips, result);
                }
            }

            result.ClipCount = processedClips.Count;
            return result;
        }

        private static void CollectAnimationClips(RuntimeAnimatorController controller, HashSet<AnimationClip> processedClips, AnimationScanResult result)
        {
            if (controller == null)
                return;

            // Obtener todos los clips
            var clips = controller.animationClips;

            foreach (var clip in clips)
            {
                if (clip == null || processedClips.Contains(clip))
                    continue;

                processedClips.Add(clip);

                // Usar Profiler API para obtener el tamano
                long size = Profiler.GetRuntimeMemorySizeLong(clip);
                result.TotalSize += size;

                result.ClipInfos.Add(new AnimationClipInfo
                {
                    ClipName = clip.name,
                    Size = size,
                    Length = clip.length,
                    FrameRate = clip.frameRate,
                    IsLooping = clip.isLooping,
                });
            }
        }

        #endregion

        #region Public Methods - Materials

        /// <summary>
        /// Escanea todos los materiales del avatar y calcula su tamano.
        /// </summary>
        public static MaterialScanResult ScanMaterials(GameObject root)
        {
            var result = new MaterialScanResult();

            if (root == null)
                return result;

            var processedMaterials = new HashSet<Material>();

            // Obtener todos los renderers
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                foreach (var material in materials)
                {
                    if (material == null || processedMaterials.Contains(material))
                        continue;

                    // Ignorar materiales built-in
                    string assetPath = AssetDatabase.GetAssetPath(material);
                    if (string.IsNullOrEmpty(assetPath) ||
                        assetPath.StartsWith("Resources/") ||
                        assetPath.StartsWith("Packages/") ||
                        assetPath.StartsWith("Library/"))
                        continue;

                    processedMaterials.Add(material);

                    long size = Profiler.GetRuntimeMemorySizeLong(material);
                    result.TotalSize += size;

                    result.MaterialInfos.Add(new MaterialInfo
                    {
                        MaterialName = material.name,
                        ShaderName = material.shader != null ? material.shader.name : "Unknown",
                        Size = size,
                    });
                }
            }

            result.MaterialCount = processedMaterials.Count;
            return result;
        }

        #endregion

        #region Public Methods - Audio

        /// <summary>
        /// Escanea todos los clips de audio del avatar.
        /// </summary>
        public static AudioScanResult ScanAudio(GameObject root)
        {
            var result = new AudioScanResult();

            if (root == null)
                return result;

            var processedClips = new HashSet<AudioClip>();

            // Buscar AudioSources
            var audioSources = root.GetComponentsInChildren<AudioSource>(true);

            foreach (var audioSource in audioSources)
            {
                if (audioSource == null || audioSource.clip == null)
                    continue;

                var clip = audioSource.clip;
                if (processedClips.Contains(clip))
                    continue;

                // Ignorar clips built-in
                string assetPath = AssetDatabase.GetAssetPath(clip);
                if (string.IsNullOrEmpty(assetPath) ||
                    assetPath.StartsWith("Resources/") ||
                    assetPath.StartsWith("Packages/") ||
                    assetPath.StartsWith("Library/"))
                    continue;

                processedClips.Add(clip);

                long size = Profiler.GetRuntimeMemorySizeLong(clip);
                result.TotalSize += size;

                result.AudioInfos.Add(new AudioClipInfo
                {
                    ClipName = clip.name,
                    Size = size,
                    Length = clip.length,
                    Channels = clip.channels,
                    Frequency = clip.frequency,
                    Samples = clip.samples,
                });
            }

            result.ClipCount = processedClips.Count;
            return result;
        }

        #endregion

        #region Public Methods - Complete Scan

        /// <summary>
        /// Realiza un escaneo completo de todos los assets del avatar.
        /// </summary>
        public static AvatarAssetScanResult ScanAllAssets(GameObject root)
        {
            var result = new AvatarAssetScanResult
            {
                Meshes = ScanMeshes(root),
                Animations = ScanAnimations(root),
                Materials = ScanMaterials(root),
                Audio = ScanAudio(root),
            };

            return result;
        }

        #endregion
    }

    #region Result Classes

    /// <summary>
    /// Resultado del escaneo de meshes.
    /// </summary>
    [Serializable]
    public class MeshScanResult
    {
        public List<MeshVRAMInfo> MeshInfos = new();
        public int MeshCount;
        public int TotalVertexCount;
        public int TotalTriangleCount;
        public int TotalBlendShapeCount;
        public long TotalVertexVRAM;
        public long TotalIndexVRAM;
        public long TotalBlendShapeVRAM;

        public long TotalVRAM => TotalVertexVRAM + TotalIndexVRAM + TotalBlendShapeVRAM;
        public string TotalVRAMLabel => VRChatTextureWeightCalculator.FormatBytes(TotalVRAM);
    }

    /// <summary>
    /// Resultado del escaneo de animaciones.
    /// </summary>
    [Serializable]
    public class AnimationScanResult
    {
        public List<AnimationClipInfo> ClipInfos = new();
        public int ClipCount;
        public long TotalSize;

        public string TotalSizeLabel => VRChatTextureWeightCalculator.FormatBytes(TotalSize);
    }

    /// <summary>
    /// Informacion de un AnimationClip.
    /// </summary>
    [Serializable]
    public struct AnimationClipInfo
    {
        public string ClipName;
        public long Size;
        public float Length;
        public float FrameRate;
        public bool IsLooping;

        public string SizeLabel => VRChatTextureWeightCalculator.FormatBytes(Size);
    }

    /// <summary>
    /// Resultado del escaneo de materiales.
    /// </summary>
    [Serializable]
    public class MaterialScanResult
    {
        public List<MaterialInfo> MaterialInfos = new();
        public int MaterialCount;
        public long TotalSize;

        public string TotalSizeLabel => VRChatTextureWeightCalculator.FormatBytes(TotalSize);
    }

    /// <summary>
    /// Informacion de un Material.
    /// </summary>
    [Serializable]
    public struct MaterialInfo
    {
        public string MaterialName;
        public string ShaderName;
        public long Size;

        public string SizeLabel => VRChatTextureWeightCalculator.FormatBytes(Size);
    }

    /// <summary>
    /// Resultado del escaneo de audio.
    /// </summary>
    [Serializable]
    public class AudioScanResult
    {
        public List<AudioClipInfo> AudioInfos = new();
        public int ClipCount;
        public long TotalSize;

        public string TotalSizeLabel => VRChatTextureWeightCalculator.FormatBytes(TotalSize);
    }

    /// <summary>
    /// Informacion de un AudioClip.
    /// </summary>
    [Serializable]
    public struct AudioClipInfo
    {
        public string ClipName;
        public long Size;
        public float Length;
        public int Channels;
        public int Frequency;
        public int Samples;

        public string SizeLabel => VRChatTextureWeightCalculator.FormatBytes(Size);
    }

    /// <summary>
    /// Resultado completo del escaneo de assets.
    /// </summary>
    [Serializable]
    public class AvatarAssetScanResult
    {
        public MeshScanResult Meshes = new();
        public AnimationScanResult Animations = new();
        public MaterialScanResult Materials = new();
        public AudioScanResult Audio = new();

        /// <summary>
        /// Tamano total de todos los assets (sin texturas).
        /// </summary>
        public long TotalNonTextureSize =>
            Meshes.TotalVRAM +
            Animations.TotalSize +
            Materials.TotalSize +
            Audio.TotalSize;

        public string TotalNonTextureSizeLabel => VRChatTextureWeightCalculator.FormatBytes(TotalNonTextureSize);
    }

    #endregion
}
