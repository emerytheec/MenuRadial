#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Bender_Dios.MenuRadial.AnimationSystem.Interfaces;
using Bender_Dios.MenuRadial.AnimationSystem.Services;
using Bender_Dios.MenuRadial.Core.Services;

namespace Bender_Dios.MenuRadial.AnimationSystem.EditorBridge
{
    /// <summary>
    /// Conecta las APIs del Editor con IlluminationAnimationGenerator:
    /// - Aplica curvas (AnimationUtility.SetEditorCurve)
    /// - Configura clip (frameRate, loop)
    /// - Guarda el .anim (AssetDatabase)
    /// </summary>
    [InitializeOnLoad]
    public static class IlluminationAnimationGeneratorEditorInitializer
    {
        static IlluminationAnimationGeneratorEditorInitializer()
        {
            // Obtiene la instancia registrada por el bootstrap
            var generator = MenuRadialServiceBootstrap.GetService<IIlluminationAnimationGenerator>() as IlluminationAnimationGenerator;
            if (generator == null)
            {
                return;
            }

            generator.SetEditorCurveApplicator(EditorCurveApplicator);
            generator.SetEditorClipConfigurator(EditorClipConfigurator);
            generator.SetEditorClipSaver(EditorClipSaver);
        }

        private static void EditorCurveApplicator(AnimationClip clip, object bindingObj, AnimationCurve curve)
        {
            EditorCurveBinding binding;

            if (bindingObj is EditorCurveBinding ecb)
            {
                binding = ecb;
            }
            else if (bindingObj is ValueTuple<string, System.Type, string> tuple)
            {
                binding = new EditorCurveBinding
                {
                    path = tuple.Item1,
                    type = tuple.Item2,
                    propertyName = tuple.Item3
                };
            }
            else
            {
                return;
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void EditorClipConfigurator(AnimationClip clip)
        {
            if (clip == null) return;

            clip.frameRate = 60f;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; // ajústalo si quieres loop
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void EditorClipSaver(AnimationClip clip, string basePath, string clipName)
        {
            if (clip == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(clipName))
                clipName = "RadialIllumination";

            // Normaliza ruta dentro de Assets
            string path = string.IsNullOrWhiteSpace(basePath) ? "Assets/Animations" : basePath.Replace('\\','/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                path = "Assets/" + path.TrimStart('/');

            if (!path.EndsWith("/")) path += "/";

            string assetPath = path + (clipName.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ? clipName : clipName + ".anim");

            // Asegura carpeta - validación defensiva
            var projectRoot = Directory.GetCurrentDirectory()?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(projectRoot) && !string.IsNullOrEmpty(path))
            {
                var fullDir = Path.Combine(projectRoot, path)?.Replace('\\','/');
                if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
                {
                    Directory.CreateDirectory(fullDir);
                }
            }

            // CORREGIDO: Sobrescribir archivo existente en lugar de crear uno nuevo
            // Verificar si ya existe el archivo
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (existingClip != null)
            {
                // Eliminar el asset existente para poder sobrescribirlo
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(clip);

        }
    }
}
#endif
