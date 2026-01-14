using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu.Generators;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu
{
    /// <summary>
    /// Generador de archivos VRChat (FX Controller, Parameters, Menu).
    /// REFACTORIZADO: Ahora actúa como orquestador de generadores especializados.
    /// Responsabilidad: Coordinar la generación y validar el proceso completo.
    /// </summary>
    public class MRVRChatFileGenerator
    {
        private readonly MRMenuControl _ownerMenu;
        private readonly MRSlotManager _slotManager;
        private readonly MRVRChatConfig _config;
        private readonly string _basePath;

        // Generadores especializados
        private readonly MRAnimationClipFinder _clipFinder;
        private readonly MRSlotInfoCollector _slotInfoCollector;
        private readonly MRFXControllerGenerator _fxGenerator;
        private readonly MRParametersGenerator _parametersGenerator;
        private readonly MRMenuGenerator _menuGenerator;

        /// <summary>
        /// Obtiene el directorio de salida basado en la configuración y ruta base
        /// </summary>
        private string OutputDirectory => _config.GetOutputDirectory(_basePath).TrimEnd('/');

        public MRVRChatFileGenerator(MRMenuControl owner, MRSlotManager slotManager, MRVRChatConfig config, string basePath = null)
        {
            _ownerMenu = owner;
            _slotManager = slotManager;
            _config = config;
            _basePath = basePath;

            // Obtener directorio de salida de la configuración con la ruta base
            string outputDir = config.GetOutputDirectory(basePath).TrimEnd('/');

            // Inicializar generadores especializados con rutas personalizadas
            _clipFinder = new MRAnimationClipFinder(outputDir);
            _slotInfoCollector = new MRSlotInfoCollector(_clipFinder);
            _slotInfoCollector.SetOutputDirectory(outputDir);
            _fxGenerator = new MRFXControllerGenerator(config, outputDir);
            _parametersGenerator = new MRParametersGenerator(config, outputDir);
            _menuGenerator = new MRMenuGenerator(config, outputDir);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Genera todos los archivos VRChat necesarios
        /// </summary>
        /// <returns>True si la generación fue exitosa</returns>
        public bool CreateVRChatFiles()
        {
            try
            {
                // Paso 1: Validar configuración
                if (!ValidateConfiguration())
                {
                    return false;
                }

                // Paso 1.5: Validar conflictos de nombres
                if (!ValidateAndResolveConflicts())
                {
                    return false;
                }

                // Paso 2: Asegurar directorio de salida
                EnsureDirectoryExists(OutputDirectory);

                // Paso 3: Generar animaciones y recopilar información de slots
                Debug.Log("[MRVRChatFileGenerator] Generando animaciones y recopilando información...");
                _slotInfoCollector.GenerateAnimationsForMenu(_ownerMenu);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var slotInfoList = _slotInfoCollector.CollectFromMenu(_ownerMenu, false);
                if (slotInfoList.Count == 0)
                {
                    Debug.LogError("[MRVRChatFileGenerator] No hay slots con animaciones válidas.");
                    return false;
                }

                // Paso 4: Validar clips de animación
                var clipErrors = _slotInfoCollector.ValidateSlotClips(slotInfoList);
                if (clipErrors.Count > 0)
                {
                    foreach (var error in clipErrors)
                    {
                        Debug.LogWarning($"[MRVRChatFileGenerator] {error}");
                    }
                }

                // Paso 5: Generar FX Controller
                Debug.Log("[MRVRChatFileGenerator] Generando FX Controller...");
                var fxController = _fxGenerator.Generate(slotInfoList);
                if (fxController == null)
                {
                    Debug.LogError("[MRVRChatFileGenerator] Error al crear FX Controller.");
                    return false;
                }

                // Paso 6: Generar Expression Parameters
                Debug.Log("[MRVRChatFileGenerator] Generando Expression Parameters...");
                var expressionParameters = _parametersGenerator.Generate(slotInfoList);
                if (expressionParameters == null)
                {
                    Debug.LogError("[MRVRChatFileGenerator] Error al crear Expression Parameters.");
                    return false;
                }

                // Calcular y mostrar costo de bits
                int bitCost = _parametersGenerator.CalculateBitCost(slotInfoList);
                Debug.Log($"[MRVRChatFileGenerator] Costo total de parámetros: {bitCost} bits de 256 disponibles");

                // Paso 7: Generar Expressions Menu
                Debug.Log("[MRVRChatFileGenerator] Generando Expressions Menu...");
                var expressionsMenu = _menuGenerator.Generate(slotInfoList, _ownerMenu.name);
                if (expressionsMenu == null)
                {
                    Debug.LogError("[MRVRChatFileGenerator] Error al crear Expressions Menu.");
                    return false;
                }

                // Paso 8: Guardar y finalizar
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LogSuccess();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MRVRChatFileGenerator] Error durante la generación: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Valida la configuración antes de generar
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (_slotManager.SlotCount > MRSlotManager.MAX_SLOTS)
            {
                Debug.LogError($"[MRVRChatFileGenerator] Demasiados slots ({_slotManager.SlotCount}). Máximo permitido: {MRSlotManager.MAX_SLOTS}.");
                return false;
            }

            if (!_slotManager.AllSlotsValid)
            {
                Debug.LogError("[MRVRChatFileGenerator] No todos los slots son válidos. Verifica la configuración.");
                return false;
            }

            if (_slotManager.SlotCount == 0)
            {
                Debug.LogError("[MRVRChatFileGenerator] No hay slots configurados.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida conflictos de nombres y ofrece resolución automática
        /// </summary>
        /// <returns>True si no hay conflictos o se resolvieron, false si el usuario cancela</returns>
        private bool ValidateAndResolveConflicts()
        {
            var conflicts = _slotManager.DetectConflicts();
            if (conflicts.Count == 0)
                return true;

            // Construir mensaje de conflictos
            var conflictMessages = conflicts.Select(c => c.GetDescription()).ToList();
            string conflictSummary = string.Join("\n", conflictMessages.Take(5));
            if (conflictMessages.Count > 5)
            {
                conflictSummary += $"\n... y {conflictMessages.Count - 5} más";
            }

            Debug.LogWarning($"[MRVRChatFileGenerator] Se detectaron {conflicts.Count} conflictos de nombres:\n{conflictSummary}");

            // Mostrar diálogo al usuario
            bool shouldAutoResolve = EditorUtility.DisplayDialog(
                "Conflictos de Nombres Detectados",
                $"Se encontraron {conflicts.Count} conflicto(s) de nombres:\n\n{conflictSummary}\n\n¿Desea auto-renombrar los slots duplicados para resolver?",
                "Auto-Renombrar",
                "Cancelar"
            );

            if (shouldAutoResolve)
            {
                _slotManager.AutoResolveConflicts();
                Debug.Log("[MRVRChatFileGenerator] Conflictos de nombres resueltos automáticamente.");
                return true;
            }

            Debug.Log("[MRVRChatFileGenerator] Generación cancelada por el usuario debido a conflictos de nombres.");
            return false;
        }

        /// <summary>
        /// Registra el éxito de la generación
        /// </summary>
        private void LogSuccess()
        {
            Debug.Log($"[MRVRChatFileGenerator] Archivos VRChat generados exitosamente en {OutputDirectory}/");
            Debug.Log($"  - {_config.GetPrefixedFileName(_config.FXFileName)}");
            Debug.Log($"  - {_config.GetPrefixedFileName(_config.ParametersFileName)}");
            Debug.Log($"  - {_config.GetPrefixedFileName(_config.MenuFileName)}");
        }

        /// <summary>
        /// Asegura que el directorio existe
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
#endif

        /// <summary>
        /// Obtiene preview de parámetros VRChat
        /// </summary>
        public string GetVRChatParametersPreview()
        {
            string preview = "Preview de parámetros VRChat:\n";

            foreach (var slot in _slotManager.Slots)
            {
                if (!slot.isValid) continue;

                var animationType = slot.GetAnimationType();
                var provider = slot.GetAnimationProvider();

                string paramType = animationType switch
                {
                    AnimationType.OnOff or AnimationType.AB => "Bool",
                    AnimationType.Linear => "Float",
                    AnimationType.SubMenu => "SubMenu",
                    _ => "None"
                };

                string defaultVal = GetDefaultValueDescription(animationType, provider);

                preview += $"  '{slot.slotName}' - {paramType} - Default: {defaultVal}\n";
            }

            return preview;
        }

        /// <summary>
        /// Obtiene la descripción del valor por defecto
        /// </summary>
        private string GetDefaultValueDescription(AnimationType animationType, IAnimationProvider provider)
        {
            if (animationType == AnimationType.OnOff && provider is Radial.MRUnificarObjetos radialMenu)
            {
                return radialMenu.DefaultStateIsOn ? "true (ON)" : "false (OFF)";
            }

            return animationType switch
            {
                AnimationType.AB => "false",
                AnimationType.Linear => "0.0f",
                _ => "N/A"
            };
        }
    }
}
