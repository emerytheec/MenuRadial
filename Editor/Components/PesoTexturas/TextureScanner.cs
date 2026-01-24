using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Bender_Dios.MenuRadial.Components.PesoTexturas;
using Bender_Dios.MenuRadial.Components.PesoTexturas.Models;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace Bender_Dios.MenuRadial.Editor.Components.PesoTexturas
{
    /// <summary>
    /// Utilidad de editor para escanear texturas de GameObjects.
    /// Usa AssetDatabase para obtener informacion del TextureImporter.
    /// Incluye busqueda en AnimationClips para materiales alternativos.
    /// </summary>
    public static class TextureScanner
    {
        #region Public Methods

        /// <summary>
        /// Escanea todas las texturas de un GameObject y sus hijos.
        /// Incluye texturas de Renderers y opcionalmente de AnimationClips.
        /// </summary>
        /// <param name="root">GameObject raiz para escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <param name="includeAnimations">Si debe buscar texturas en AnimationClips</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanTextures(GameObject root, HashSet<string> processedPaths, bool includeAnimations = false)
        {
            var result = new List<TextureEntry>();

            if (root == null)
                return result;

            processedPaths ??= new HashSet<string>();

            // Obtener todos los Renderers
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                // Obtener materiales compartidos
                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                foreach (var material in materials)
                {
                    if (material == null)
                        continue;

                    // Obtener todas las propiedades de textura del material
                    ScanMaterialTextures(material, processedPaths, result);
                }

                // Escanear trail materials de ParticleSystemRenderer
                if (renderer is ParticleSystemRenderer psr && psr.trailMaterial != null)
                {
                    ScanMaterialTextures(psr.trailMaterial, processedPaths, result);
                }
            }

            // Escanear texturas en AnimationClips si se solicita
            if (includeAnimations)
            {
                ScanAnimatorTextures(root, processedPaths, result);
            }

            // Escanear Reflection Probes
            ScanReflectionProbes(root, processedPaths, result);

            // Escanear UI Sprites (Image components)
            ScanUISprites(root, processedPaths, result);

            return result;
        }

        /// <summary>
        /// Escanea texturas de UI Image components (sprites).
        /// </summary>
        public static void ScanUISprites(GameObject root, HashSet<string> processedPaths, List<TextureEntry> result)
        {
            if (root == null)
                return;

            var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);

            foreach (var image in images)
            {
                if (image == null || image.sprite == null)
                    continue;

                // Textura principal del sprite
                if (image.sprite.texture != null)
                {
                    AddSpriteTexture(image.sprite.texture, processedPaths, result, "_UISprite");
                }

                // Alpha split texture (usado en algunas configuraciones de sprite)
                if (image.sprite.associatedAlphaSplitTexture != null)
                {
                    AddSpriteTexture(image.sprite.associatedAlphaSplitTexture, processedPaths, result, "_UISprite_Alpha");
                }
            }

            // También escanear RawImage components
            var rawImages = root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);

            foreach (var rawImage in rawImages)
            {
                if (rawImage == null || rawImage.texture == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(rawImage.texture);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (assetPath.StartsWith("Resources/") ||
                    assetPath.StartsWith("Packages/") ||
                    assetPath.StartsWith("Library/") ||
                    assetPath.Contains("unity_builtin"))
                    continue;

                if (processedPaths.Contains(assetPath))
                    continue;

                processedPaths.Add(assetPath);

                if (rawImage.texture is Texture2D texture2D)
                {
                    var entry = CreateTextureEntry(texture2D, assetPath, false, "_UIRawImage");
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
                else
                {
                    var entry = CreateGenericTextureEntry(rawImage.texture, assetPath, false, "_UIRawImage");
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
            }
        }

        private static void AddSpriteTexture(Texture2D texture, HashSet<string> processedPaths, List<TextureEntry> result, string propertyName)
        {
            if (texture == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (assetPath.StartsWith("Resources/") ||
                assetPath.StartsWith("Packages/") ||
                assetPath.StartsWith("Library/") ||
                assetPath.Contains("unity_builtin"))
                return;

            if (processedPaths.Contains(assetPath))
                return;

            processedPaths.Add(assetPath);

            var entry = CreateTextureEntry(texture, assetPath, false, propertyName);
            if (entry != null)
            {
                result.Add(entry);
            }
        }

        /// <summary>
        /// Escanea texturas de Reflection Probes (cubemaps).
        /// </summary>
        public static void ScanReflectionProbes(GameObject root, HashSet<string> processedPaths, List<TextureEntry> result)
        {
            if (root == null)
                return;

            var reflectionProbes = root.GetComponentsInChildren<ReflectionProbe>(true);

            foreach (var probe in reflectionProbes)
            {
                if (probe == null || probe.texture == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(probe.texture);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // Ignorar texturas built-in
                if (assetPath.StartsWith("Resources/") ||
                    assetPath.StartsWith("Packages/") ||
                    assetPath.StartsWith("Library/") ||
                    assetPath.Contains("unity_builtin"))
                    continue;

                if (processedPaths.Contains(assetPath))
                    continue;

                processedPaths.Add(assetPath);

                // Crear entrada para el cubemap
                if (probe.texture is Cubemap cubemap)
                {
                    var entry = CreateCubemapEntry(cubemap, assetPath, false, "_ReflectionProbe");
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
                else
                {
                    // Fallback para otros tipos de textura de reflection
                    var entry = CreateGenericTextureEntry(probe.texture, assetPath, false, "_ReflectionProbe");
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Escanea Lightmaps de la escena (opcional, para escenas con baked lighting).
        /// Nota: Los lightmaps generalmente NO se incluyen en el bundle del avatar,
        /// pero pueden afectar si el avatar tiene lightmaps asignados directamente.
        /// </summary>
        public static List<TextureEntry> ScanLightmaps(HashSet<string> processedPaths)
        {
            var result = new List<TextureEntry>();
            processedPaths ??= new HashSet<string>();

            var lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || lightmaps.Length == 0)
                return result;

            foreach (var lightmapData in lightmaps)
            {
                if (lightmapData == null)
                    continue;

                // Lightmap Color
                if (lightmapData.lightmapColor != null)
                {
                    AddLightmapTexture(lightmapData.lightmapColor, processedPaths, result, "LightmapColor");
                }

                // Lightmap Direction (para normal mapping)
                if (lightmapData.lightmapDir != null)
                {
                    AddLightmapTexture(lightmapData.lightmapDir, processedPaths, result, "LightmapDir");
                }

                // Shadow Mask
                if (lightmapData.shadowMask != null)
                {
                    AddLightmapTexture(lightmapData.shadowMask, processedPaths, result, "ShadowMask");
                }
            }

            return result;
        }

        private static void AddLightmapTexture(Texture2D texture, HashSet<string> processedPaths, List<TextureEntry> result, string propertyName)
        {
            if (texture == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (processedPaths.Contains(assetPath))
                return;

            processedPaths.Add(assetPath);

            var entry = CreateTextureEntry(texture, assetPath, false, propertyName);
            if (entry != null)
            {
                result.Add(entry);
            }
        }

        /// <summary>
        /// Escanea texturas referenciadas en AnimationClips del avatar.
        /// Busca en Animator y VRCAvatarDescriptor.
        /// </summary>
        public static void ScanAnimatorTextures(GameObject root, HashSet<string> processedPaths, List<TextureEntry> result)
        {
            if (root == null)
                return;

            processedPaths ??= new HashSet<string>();
            var processedMaterials = new HashSet<Material>();

            // Buscar en Animator
            var animator = root.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null)
                {
                    var materials = GetMaterialsFromAnimatorController(controller, processedMaterials);
                    foreach (var material in materials)
                    {
                        ScanMaterialTextures(material, processedPaths, result, isFromAlternativeMaterial: true);
                    }
                }
            }

#if VRC_SDK_VRCSDK3
            // Buscar en VRCAvatarDescriptor
            var avatarDescriptor = root.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                // Buscar en baseAnimationLayers
                foreach (var layer in avatarDescriptor.baseAnimationLayers)
                {
                    if (layer.animatorController != null)
                    {
                        var controller = layer.animatorController as AnimatorController;
                        if (controller != null)
                        {
                            var materials = GetMaterialsFromAnimatorController(controller, processedMaterials);
                            foreach (var material in materials)
                            {
                                ScanMaterialTextures(material, processedPaths, result, isFromAlternativeMaterial: true);
                            }
                        }
                    }
                }

                // Buscar en specialAnimationLayers
                foreach (var layer in avatarDescriptor.specialAnimationLayers)
                {
                    if (layer.animatorController != null)
                    {
                        var controller = layer.animatorController as AnimatorController;
                        if (controller != null)
                        {
                            var materials = GetMaterialsFromAnimatorController(controller, processedMaterials);
                            foreach (var material in materials)
                            {
                                ScanMaterialTextures(material, processedPaths, result, isFromAlternativeMaterial: true);
                            }
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Obtiene todos los materiales referenciados en un AnimatorController.
        /// </summary>
        public static List<Material> GetMaterialsFromAnimatorController(AnimatorController controller, HashSet<Material> processedMaterials = null)
        {
            var result = new List<Material>();
            processedMaterials ??= new HashSet<Material>();

            if (controller == null)
                return result;

            // Obtener todos los clips del controller
            var clips = GetAllAnimationClips(controller);

            foreach (var clip in clips)
            {
                var materials = GetMaterialsFromAnimationClip(clip);
                foreach (var material in materials)
                {
                    if (material != null && !processedMaterials.Contains(material))
                    {
                        processedMaterials.Add(material);
                        result.Add(material);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene materiales referenciados en un AnimationClip via ObjectReferenceCurve.
        /// </summary>
        public static List<Material> GetMaterialsFromAnimationClip(AnimationClip clip)
        {
            var result = new List<Material>();

            if (clip == null)
                return result;

            // Obtener bindings de referencia a objetos
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // Obtener keyframes
                var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                foreach (var keyframe in keyframes)
                {
                    if (keyframe.value is Material material)
                    {
                        result.Add(material);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene todos los AnimationClips de un AnimatorController.
        /// </summary>
        public static List<AnimationClip> GetAllAnimationClips(AnimatorController controller)
        {
            var result = new List<AnimationClip>();

            if (controller == null)
                return result;

            foreach (var layer in controller.layers)
            {
                CollectClipsFromStateMachine(layer.stateMachine, result);
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Recolecta clips de una state machine recursivamente.
        /// </summary>
        private static void CollectClipsFromStateMachine(AnimatorStateMachine stateMachine, List<AnimationClip> result)
        {
            if (stateMachine == null)
                return;

            // Clips de estados
            foreach (var state in stateMachine.states)
            {
                CollectClipsFromMotion(state.state.motion, result);
            }

            // Sub state machines
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                CollectClipsFromStateMachine(subStateMachine.stateMachine, result);
            }
        }

        /// <summary>
        /// Recolecta clips de un Motion (AnimationClip o BlendTree).
        /// </summary>
        private static void CollectClipsFromMotion(Motion motion, List<AnimationClip> result)
        {
            if (motion == null)
                return;

            if (motion is AnimationClip clip)
            {
                if (!result.Contains(clip))
                    result.Add(clip);
            }
            else if (motion is BlendTree blendTree)
            {
                foreach (var child in blendTree.children)
                {
                    CollectClipsFromMotion(child.motion, result);
                }
            }
        }


        /// <summary>
        /// Crea una entrada de textura a partir de una Texture2D.
        /// Extrae el formato real de la textura para calculo preciso de VRAM.
        /// </summary>
        /// <param name="texture">Textura a procesar</param>
        /// <param name="assetPath">Ruta del asset</param>
        /// <param name="isFromAlternativeMaterial">Si la textura proviene de un material alternativo</param>
        /// <param name="shaderPropertyName">Nombre de la propiedad del shader donde se usa la textura</param>
        /// <returns>TextureEntry con informacion calculada</returns>
        public static TextureEntry CreateTextureEntry(Texture2D texture, string assetPath, bool isFromAlternativeMaterial = false, string shaderPropertyName = null)
        {
            if (texture == null || string.IsNullOrEmpty(assetPath))
                return null;

            // Para texturas embebidas en FBX, extraer la ruta base
            string importerPath = assetPath;
            if (assetPath.Contains("#"))
            {
                // Textura embebida: Assets/Models/avatar.fbx#texture_name
                importerPath = assetPath.Substring(0, assetPath.IndexOf('#'));
            }

            // Obtener el TextureImporter para esta textura
            var importer = AssetImporter.GetAtPath(importerPath) as TextureImporter;

            int maxSize = 2048; // Default
            bool hasAlpha = false;
            bool hasMipmaps = true;
            bool hasMipStreaming = true;
            TextureCompressionType compressionType = TextureCompressionType.Default;

            // Obtener formato real y mipmap count de la textura
            TextureFormat textureFormat = texture.format;
            int mipmapCount = texture.mipmapCount;

            if (importer != null)
            {
                maxSize = importer.maxTextureSize;
                hasMipmaps = importer.mipmapEnabled;
                hasAlpha = DetectAlphaChannel(texture, importer);
                hasMipStreaming = importer.streamingMipmaps;
                compressionType = DetectCompressionType(importer);

                // Si el importer no dice que es NormalMap, verificar por propiedad del shader
                if (compressionType == TextureCompressionType.Default)
                {
                    compressionType = DetectCompressionTypeFromShaderProperty(shaderPropertyName);
                }

                // Fallback final: verificar por nombre de textura
                if (compressionType == TextureCompressionType.Default)
                {
                    compressionType = DetectCompressionTypeFromName(texture.name);
                }
            }
            else
            {
                // Fallback: detectar por propiedad del shader primero
                compressionType = DetectCompressionTypeFromShaderProperty(shaderPropertyName);

                // Luego por nombre
                if (compressionType == TextureCompressionType.Default)
                {
                    compressionType = DetectCompressionTypeFromName(texture.name);
                }

                // Fallback: detectar alpha del formato de textura
                hasAlpha = HasAlphaFromFormat(texture.format);
            }

            // Usar constructor completo con formato de textura
            return new TextureEntry(
                texture,
                assetPath,
                texture.width,
                texture.height,
                maxSize,
                hasAlpha,
                hasMipmaps,
                hasMipStreaming,
                isFromAlternativeMaterial,
                compressionType,
                textureFormat,
                mipmapCount);
        }

        /// <summary>
        /// Detecta el tipo de compresion basado en el TextureImporter.
        /// </summary>
        /// <param name="importer">TextureImporter de la textura</param>
        /// <returns>Tipo de compresion detectado</returns>
        public static TextureCompressionType DetectCompressionType(TextureImporter importer)
        {
            if (importer == null)
                return TextureCompressionType.Default;

            // Normal maps
            if (importer.textureType == TextureImporterType.NormalMap)
                return TextureCompressionType.NormalMap;

            // Single channel (R channel only)
            if (importer.textureType == TextureImporterType.SingleChannel)
                return TextureCompressionType.SingleChannel;

            return TextureCompressionType.Default;
        }

        /// <summary>
        /// Detecta el tipo de compresion basado en el nombre de la propiedad del shader.
        /// Solo detecta Normal Maps de forma confiable. SingleChannel NO se detecta por nombre
        /// de propiedad porque muchas texturas "mask" en lilToon/Poiyomi son RGBA empaquetadas.
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad del shader</param>
        /// <returns>Tipo de compresion detectado</returns>
        public static TextureCompressionType DetectCompressionTypeFromShaderProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return TextureCompressionType.Default;

            string lowerName = propertyName.ToLowerInvariant();

            // Propiedades de Normal Map comunes en shaders de Unity
            // Standard: _BumpMap, _DetailNormalMap
            // lilToon: _BumpMap, _Bump2ndMap, _BumpMap2nd
            // Poiyomi: _BumpMap, _DetailNormalMap
            if (lowerName.Contains("bump") || lowerName.Contains("normal"))
                return TextureCompressionType.NormalMap;

            // NO detectamos SingleChannel por nombre de propiedad porque:
            // - En lilToon/Poiyomi, texturas "mask" suelen ser RGBA empaquetadas
            // - Solo el TextureImporter sabe si realmente es SingleChannel

            return TextureCompressionType.Default;
        }

        /// <summary>
        /// Detecta el tipo de compresion basado en el nombre de la textura (fallback).
        /// Solo detecta Normal Maps. SingleChannel NO se detecta por nombre porque
        /// muchas texturas con nombres como "mask" son RGBA empaquetadas.
        /// </summary>
        /// <param name="textureName">Nombre de la textura</param>
        /// <returns>Tipo de compresion detectado</returns>
        public static TextureCompressionType DetectCompressionTypeFromName(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
                return TextureCompressionType.Default;

            string lowerName = textureName.ToLowerInvariant();

            // Detectar normal maps por nombre
            if (lowerName.Contains("normal") || lowerName.Contains("_nrm") || lowerName.Contains("bump"))
                return TextureCompressionType.NormalMap;

            // Detectar por sufijo _n (comun en normal maps)
            // Pero evitar falsos positivos como "skin", "button", etc.
            if (lowerName.EndsWith("_n") || lowerName.Contains("_n_") || lowerName.Contains("_n."))
                return TextureCompressionType.NormalMap;

            // NO detectamos SingleChannel por nombre - demasiados falsos positivos
            // Solo el TextureImporter puede determinar esto con precision

            return TextureCompressionType.Default;
        }

        /// <summary>
        /// Detecta si una textura tiene canal alpha.
        /// </summary>
        /// <param name="texture">Textura a analizar</param>
        /// <param name="importer">TextureImporter de la textura</param>
        /// <returns>True si tiene canal alpha</returns>
        public static bool DetectAlphaChannel(Texture2D texture, TextureImporter importer)
        {
            if (importer != null)
            {
                // Primero intentar con el metodo del importer
                if (importer.DoesSourceTextureHaveAlpha())
                    return true;

                // Verificar configuracion de alpha
                if (importer.alphaSource != TextureImporterAlphaSource.None)
                    return true;
            }

            // Fallback: verificar formato de textura
            return HasAlphaFromFormat(texture.format);
        }

        /// <summary>
        /// Aplica un step-down (reduccion de resolucion) a una textura.
        /// Registra el estado original en el historial si es la primera modificacion.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool ApplyStepDown(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            int nextSize = entry.GetNextStepDownSize();
            if (nextSize >= entry.CurrentMaxSize)
                return false;

            // Registrar estado original antes de modificar (solo la primera vez)
            TextureHistoryManager.RegisterOriginal(entry.AssetPath, entry.CurrentMaxSize);

            bool success = ApplyMaxSize(entry, nextSize);

            if (success)
            {
                // Incrementar contador de step-downs
                TextureHistoryManager.IncrementStepDown(entry.AssetPath);
                TextureHistoryManager.SaveIfDirty();
            }

            return success;
        }

        /// <summary>
        /// Aplica un tamanio maximo especifico a una textura.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <param name="newMaxSize">Nuevo tamanio maximo</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool ApplyMaxSize(TextureEntry entry, int newMaxSize)
        {
            if (entry == null || !entry.IsValid)
                return false;

            var importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
            if (importer == null)
                return false;

            // Registrar Undo
            Undo.RecordObject(importer, "Change Texture Max Size");

            importer.maxTextureSize = newMaxSize;

            // Guardar y reimportar
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Actualizar la entrada
            entry.UpdateMaxSize(newMaxSize);

            return true;
        }

        /// <summary>
        /// Aplica step-down solo a las texturas con la resolucion maxima del grupo.
        /// Esto permite un enfoque gradual: primero se reducen las mas grandes,
        /// y en sucesivas llamadas se van reduciendo las siguientes.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int ApplyStepDownToGroup(TextureGroupEntry group)
        {
            if (group == null || group.TextureCount == 0)
                return 0;

            // Encontrar la resolucion maxima del grupo
            int maxResolution = group.MaxResolution;
            if (maxResolution <= VRChatTextureWeightCalculator.MIN_RESOLUTION)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                // Solo aplicar step-down a texturas con la resolucion maxima
                if (texture.CurrentMaxSize == maxResolution)
                {
                    if (ApplyStepDown(texture))
                        count++;
                }
            }

            group.RecalculateAll();
            return count;
        }

        /// <summary>
        /// Aplica un tamanio maximo a todas las texturas de un grupo.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <param name="maxSize">Tamanio maximo a aplicar</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int ApplyMaxSizeToGroup(TextureGroupEntry group, int maxSize)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (texture.CurrentMaxSize > maxSize)
                {
                    if (ApplyMaxSize(texture, maxSize))
                        count++;
                }
            }

            group.RecalculateAll();
            return count;
        }

        /// <summary>
        /// Habilita Mip Streaming en una textura.
        /// Requerido por VRChat para texturas con mipmaps.
        /// </summary>
        /// <param name="entry">Entrada de textura a modificar</param>
        /// <returns>True si se aplico el cambio</returns>
        public static bool EnableMipStreaming(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            // Ya tiene Mip Streaming habilitado
            if (entry.HasMipStreaming)
                return false;

            var importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
            if (importer == null)
                return false;

            // Solo aplica si tiene mipmaps habilitados
            if (!importer.mipmapEnabled)
                return false;

            // Registrar Undo
            Undo.RecordObject(importer, "Enable Mip Streaming");

            importer.streamingMipmaps = true;

            // Guardar y reimportar
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Actualizar la entrada
            entry.HasMipStreaming = true;

            return true;
        }

        /// <summary>
        /// Habilita Mip Streaming en todas las texturas de un grupo.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int EnableMipStreamingInGroup(TextureGroupEntry group)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (EnableMipStreaming(texture))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Habilita Mip Streaming en todas las texturas de una lista.
        /// </summary>
        /// <param name="entries">Lista de entradas de textura</param>
        /// <returns>Cantidad de texturas modificadas</returns>
        public static int EnableMipStreamingInList(IEnumerable<TextureEntry> entries)
        {
            if (entries == null)
                return 0;

            int count = 0;
            foreach (var texture in entries)
            {
                if (EnableMipStreaming(texture))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Escanea texturas de una lista de materiales.
        /// Util para escanear materiales alternativos de MRAgruparMateriales.
        /// </summary>
        /// <param name="materials">Lista de materiales a escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <param name="isFromAlternativeMaterial">Si los materiales son alternativos (no asignados a renderers)</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanMaterials(IEnumerable<Material> materials, HashSet<string> processedPaths, bool isFromAlternativeMaterial = false)
        {
            var result = new List<TextureEntry>();

            if (materials == null)
                return result;

            processedPaths ??= new HashSet<string>();

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                ScanMaterialTextures(material, processedPaths, result, isFromAlternativeMaterial);
            }

            return result;
        }

        /// <summary>
        /// Escanea texturas de un solo material.
        /// </summary>
        /// <param name="material">Material a escanear</param>
        /// <param name="processedPaths">HashSet de rutas ya procesadas para evitar duplicados</param>
        /// <returns>Lista de TextureEntry encontradas</returns>
        public static List<TextureEntry> ScanSingleMaterial(Material material, HashSet<string> processedPaths)
        {
            var result = new List<TextureEntry>();

            if (material == null)
                return result;

            processedPaths ??= new HashSet<string>();
            ScanMaterialTextures(material, processedPaths, result);

            return result;
        }

        #region Restore Methods

        /// <summary>
        /// Restaura una textura a su resolucion original.
        /// </summary>
        /// <param name="entry">Entrada de textura a restaurar</param>
        /// <returns>True si se restauro correctamente</returns>
        public static bool RestoreTexture(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            int originalSize = TextureHistoryManager.GetOriginalMaxSize(entry.AssetPath);
            if (originalSize <= 0)
                return false; // No hay historial

            // Ya esta en su tamano original
            if (entry.CurrentMaxSize == originalSize)
            {
                TextureHistoryManager.ClearHistory(entry.AssetPath);
                TextureHistoryManager.SaveIfDirty();
                return false;
            }

            bool success = ApplyMaxSizeWithoutHistory(entry, originalSize);

            if (success)
            {
                // Limpiar historial de esta textura
                TextureHistoryManager.ClearHistory(entry.AssetPath);
                TextureHistoryManager.SaveIfDirty();
            }

            return success;
        }

        /// <summary>
        /// Restaura todas las texturas de un grupo a su resolucion original.
        /// </summary>
        /// <param name="group">Grupo de texturas</param>
        /// <returns>Cantidad de texturas restauradas</returns>
        public static int RestoreGroup(TextureGroupEntry group)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (RestoreTexture(texture))
                    count++;
            }

            group.RecalculateAll();
            return count;
        }

        /// <summary>
        /// Restaura todas las texturas con historial a su resolucion original.
        /// </summary>
        /// <returns>Cantidad de texturas restauradas</returns>
        public static int RestoreAllFromHistory()
        {
            var entries = TextureHistoryManager.GetAllEntries();
            int count = 0;

            foreach (var historyItem in entries)
            {
                var importer = AssetImporter.GetAtPath(historyItem.assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                int originalSize = historyItem.entry.originalMaxSize;
                if (importer.maxTextureSize == originalSize)
                {
                    // Ya esta en su tamano original, solo limpiar historial
                    TextureHistoryManager.ClearHistory(historyItem.assetPath);
                    continue;
                }

                Undo.RecordObject(importer, "Restore Texture Size");
                importer.maxTextureSize = originalSize;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                TextureHistoryManager.ClearHistory(historyItem.assetPath);
                count++;
            }

            TextureHistoryManager.SaveIfDirty();
            return count;
        }

        /// <summary>
        /// Obtiene la cantidad de texturas que se pueden restaurar en un grupo.
        /// </summary>
        public static int GetRestorableCountInGroup(TextureGroupEntry group)
        {
            if (group == null)
                return 0;

            int count = 0;
            foreach (var texture in group.Textures)
            {
                if (TextureHistoryManager.HasHistory(texture.AssetPath))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Verifica si una textura puede ser restaurada (tiene historial).
        /// </summary>
        public static bool CanRestore(TextureEntry entry)
        {
            if (entry == null || !entry.IsValid)
                return false;

            return TextureHistoryManager.HasHistory(entry.AssetPath);
        }

        /// <summary>
        /// Obtiene informacion del historial de una textura.
        /// </summary>
        /// <param name="entry">Entrada de textura</param>
        /// <param name="originalSize">Tamano original (output)</param>
        /// <param name="stepDownCount">Cantidad de step-downs (output)</param>
        /// <returns>True si tiene historial</returns>
        public static bool GetTextureHistoryInfo(TextureEntry entry, out int originalSize, out int stepDownCount)
        {
            originalSize = 0;
            stepDownCount = 0;

            if (entry == null || !entry.IsValid)
                return false;

            originalSize = TextureHistoryManager.GetOriginalMaxSize(entry.AssetPath);
            if (originalSize <= 0)
                return false;

            stepDownCount = TextureHistoryManager.GetStepDownCount(entry.AssetPath);
            return true;
        }

        /// <summary>
        /// Aplica un tamano maximo sin registrar en historial (usado para restaurar).
        /// </summary>
        private static bool ApplyMaxSizeWithoutHistory(TextureEntry entry, int newMaxSize)
        {
            if (entry == null || !entry.IsValid)
                return false;

            var importer = AssetImporter.GetAtPath(entry.AssetPath) as TextureImporter;
            if (importer == null)
                return false;

            Undo.RecordObject(importer, "Restore Texture Max Size");
            importer.maxTextureSize = newMaxSize;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            entry.UpdateMaxSize(newMaxSize);
            return true;
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Escanea todas las texturas de un material.
        /// Soporta Texture2D, Cubemap y otros tipos de textura.
        /// </summary>
        private static void ScanMaterialTextures(
            Material material,
            HashSet<string> processedPaths,
            List<TextureEntry> result,
            bool isFromAlternativeMaterial = false)
        {
            if (material == null)
                return;

            // Obtener todas las propiedades de textura del shader
            var shader = material.shader;
            if (shader == null)
                return;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propertyName);

                if (texture == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // Ignorar texturas que VRChat no cuenta en el peso del avatar:
                // - Resources/unity_builtin_extra: texturas built-in de Unity
                // - Packages/: texturas de paquetes instalados localmente
                // - Library/PackageCache/: texturas de paquetes en cache (lilToon, VRChat SDK, NDMF, etc.)
                //   Estas vienen con los shaders/SDK y no se cuentan en el bundle del avatar
                if (assetPath.StartsWith("Resources/") ||
                    assetPath.StartsWith("Packages/") ||
                    assetPath.StartsWith("Library/") ||
                    assetPath.Contains("unity_builtin"))
                    continue;

                // Evitar duplicados
                if (processedPaths.Contains(assetPath))
                    continue;

                processedPaths.Add(assetPath);

                // Manejar diferentes tipos de textura
                if (texture is Texture2D texture2D)
                {
                    var entry = CreateTextureEntry(texture2D, assetPath, isFromAlternativeMaterial, propertyName);
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
                else if (texture is Cubemap cubemap)
                {
                    // Crear entrada para Cubemap (6 caras)
                    var entry = CreateCubemapEntry(cubemap, assetPath, isFromAlternativeMaterial, propertyName);
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
                else
                {
                    // Para otros tipos de textura, usar Profiler API como fallback
                    var entry = CreateGenericTextureEntry(texture, assetPath, isFromAlternativeMaterial, propertyName);
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Crea una entrada para un Cubemap.
        /// Los Cubemaps tienen 6 caras, por lo que el calculo es diferente.
        /// </summary>
        public static TextureEntry CreateCubemapEntry(Cubemap cubemap, string assetPath, bool isFromAlternativeMaterial = false, string shaderPropertyName = null)
        {
            if (cubemap == null || string.IsNullOrEmpty(assetPath))
                return null;

            // Obtener formato y mipmaps del cubemap
            TextureFormat textureFormat = cubemap.format;
            int mipmapCount = cubemap.mipmapCount;

            // Para cubemaps, el tamaño efectivo es width * height * 6 caras
            // Usamos el calculador con depth = 6
            long estimatedVRAM = VRChatTextureWeightCalculator.CalculateVRAMBytesWithMipmapsAndDepth(
                cubemap.width,
                cubemap.height,
                6, // 6 caras del cubemap
                VRChatTextureWeightCalculator.GetBitsPerPixel(textureFormat) ?? 8,
                mipmapCount);

            // Crear entrada con los valores calculados
            // Usamos el constructor basico y seteamos el VRAM manualmente
            var entry = new TextureEntry(
                null, // No tenemos Texture2D
                assetPath,
                cubemap.width,
                cubemap.height,
                cubemap.width, // maxSize = actual size
                true, // Asumimos alpha para ser conservadores
                mipmapCount > 1,
                true, // hasMipStreaming
                isFromAlternativeMaterial,
                TextureCompressionType.Default,
                textureFormat,
                mipmapCount);

            // Sobreescribir el VRAM calculado con el correcto para cubemap
            entry.EstimatedVRAMBytes = estimatedVRAM;

            return entry;
        }

        /// <summary>
        /// Crea una entrada para texturas genericas usando Profiler API.
        /// Usado como fallback para tipos de textura no soportados directamente.
        /// </summary>
        public static TextureEntry CreateGenericTextureEntry(Texture texture, string assetPath, bool isFromAlternativeMaterial = false, string shaderPropertyName = null)
        {
            if (texture == null || string.IsNullOrEmpty(assetPath))
                return null;

            // Usar Profiler API para obtener el tamaño
            long estimatedVRAM = VRChatTextureWeightCalculator.GetRuntimeMemorySize(texture);

            // Obtener formato si es posible
            TextureFormat? format = VRChatTextureWeightCalculator.GetTextureFormat(texture);
            TextureFormat textureFormat = format ?? TextureFormat.RGBA32;

            // Crear entrada con valores aproximados
            var entry = new TextureEntry(
                null, // No tenemos Texture2D
                assetPath,
                texture.width,
                texture.height,
                texture.width, // maxSize = actual size
                true, // Asumimos alpha
                texture.mipmapCount > 1,
                true, // hasMipStreaming
                isFromAlternativeMaterial,
                TextureCompressionType.Default,
                textureFormat,
                texture.mipmapCount);

            // Sobreescribir el VRAM con el valor del Profiler
            entry.EstimatedVRAMBytes = estimatedVRAM;

            return entry;
        }

        /// <summary>
        /// Determina si un formato de textura tiene canal alpha.
        /// </summary>
        private static bool HasAlphaFromFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32:
                case TextureFormat.RGBA32:
                case TextureFormat.BGRA32:
                case TextureFormat.ARGB4444:
                case TextureFormat.RGBA4444:
                case TextureFormat.Alpha8:
                case TextureFormat.DXT5:
                case TextureFormat.BC7:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                    return true;

                default:
                    return false;
            }
        }

        #endregion
    }
}
