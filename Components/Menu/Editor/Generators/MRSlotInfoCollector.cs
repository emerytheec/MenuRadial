using System;
using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;

#if UNITY_EDITOR
using UnityEditor;
using Bender_Dios.MenuRadial.AnimationSystem;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Recopilador de información de slots para generación de archivos VRChat.
    /// Responsabilidad: Extraer y organizar datos de slots y sus componentes.
    /// </summary>
    public class MRSlotInfoCollector
    {
        private readonly MRAnimationClipFinder _clipFinder;
        private string _outputDirectory;

        public MRSlotInfoCollector(MRAnimationClipFinder clipFinder = null)
        {
            _clipFinder = clipFinder ?? new MRAnimationClipFinder();
        }

        /// <summary>
        /// Establece el directorio de salida para las animaciones generadas.
        /// Si se establece, sobrescribe el AnimationPath de cada componente.
        /// </summary>
        public void SetOutputDirectory(string directory)
        {
            _outputDirectory = directory;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Recopila información de todos los slots de un menú
        /// </summary>
        /// <param name="menu">Menú del cual recopilar</param>
        /// <param name="generateAnimationsFirst">Si debe generar animaciones antes de buscar clips</param>
        /// <returns>Lista de información de slots</returns>
        public List<MRSlotInfo> CollectFromMenu(MRMenuControl menu, bool generateAnimationsFirst = false)
        {
            var result = new List<MRSlotInfo>();

            if (menu == null)
            {
                Debug.LogWarning("[MRSlotInfoCollector] CollectFromMenu: menu es null");
                return result;
            }

            var slots = menu.AnimationSlots;
            if (slots == null || slots.Count == 0)
            {
                Debug.LogWarning($"[MRSlotInfoCollector] CollectFromMenu: '{menu.name}' no tiene slots");
                return result;
            }

            // Generar animaciones primero si se solicita
            if (generateAnimationsFirst)
            {
                GenerateAnimationsForMenu(menu);
                // Importante: Refrescar AssetDatabase para que los clips recién creados sean encontrables
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[MRSlotInfoCollector] Procesando menú '{menu.name}' con {slots.Count} slots");

            foreach (var slot in slots)
            {
                var slotInfo = CollectSlotInfo(slot);
                if (slotInfo != null)
                {
                    result.Add(slotInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// Recopila información de un slot individual
        /// </summary>
        private MRSlotInfo CollectSlotInfo(MRAnimationSlot slot)
        {
            if (!slot.isValid || slot.targetObject == null)
            {
                Debug.Log($"[MRSlotInfoCollector] Slot '{slot?.slotName ?? "NULL"}' inválido o sin targetObject");
                return null;
            }

            var provider = slot.GetAnimationProvider();
            var animationType = slot.GetAnimationType();

            Debug.Log($"[MRSlotInfoCollector] Slot '{slot.slotName}': provider={provider?.GetType().Name ?? "NULL"}, animationType={animationType}, isUnifyMaterial={slot.CachedUnifyMaterial != null}");

            // Skip si es None
            if (animationType == AnimationType.None)
            {
                Debug.LogWarning($"[MRSlotInfoCollector] Slot '{slot.slotName}' tiene AnimationType.None, saltando...");
                return null;
            }

            var subMenuComponent = animationType == AnimationType.SubMenu ? slot.CachedControlMenu : null;

            var info = new MRSlotInfo
            {
                Slot = slot,
                AnimationType = animationType,
                AnimationProvider = provider,
                AnimationClips = _clipFinder.FindClipsForSlot(slot, provider, animationType),
                IsIllumination = slot.CachedIllumination != null,
                SubMenuComponent = subMenuComponent
            };

            // Para SubMenus, recopilar hijos recursivamente
            if (animationType == AnimationType.SubMenu && subMenuComponent != null)
            {
                Debug.Log($"[MRSlotInfoCollector] Slot '{slot.slotName}' es SubMenu, recopilando hijos...");

                // Generar animaciones del submenú primero
                GenerateAnimationsForMenu(subMenuComponent);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Ahora recopilar info de hijos
                info.ChildSlotInfos = CollectFromMenu(subMenuComponent, false);
                Debug.Log($"[MRSlotInfoCollector] SubMenu '{slot.slotName}' tiene {info.ChildSlotInfos.Count} slots hijos");
            }

            return info;
        }

        /// <summary>
        /// Genera animaciones para todos los componentes de un menú
        /// </summary>
        public void GenerateAnimationsForMenu(MRMenuControl menu)
        {
            if (menu == null) return;

            var slots = menu.AnimationSlots;
            if (slots == null || slots.Count == 0)
            {
                Debug.Log($"[MRSlotInfoCollector] GenerateAnimationsForMenu: '{menu.name}' no tiene slots");
                return;
            }

            Debug.Log($"[MRSlotInfoCollector] Generando animaciones para '{menu.name}' con {slots.Count} slots");
            int generatedCount = 0;

            foreach (var slot in slots)
            {
                if (!slot.isValid || slot.targetObject == null)
                    continue;

                // MRUnificarObjetos
                var radialMenu = slot.CachedRadialMenu;
                if (radialMenu != null && radialMenu.FrameCount > 0)
                {
                    try
                    {
                        // Sobrescribir AnimationPath si hay directorio de salida configurado
                        if (!string.IsNullOrEmpty(_outputDirectory))
                        {
                            radialMenu.AnimationPath = _outputDirectory;
                        }

                        RadialAnimationBuilder.GenerateAnimations(radialMenu);
                        Debug.Log($"[MRSlotInfoCollector] Auto-generadas animaciones para: {slot.slotName} (MRUnificarObjetos)");
                        generatedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MRSlotInfoCollector] Error generando animaciones para {slot.slotName}: {ex.Message}");
                    }
                    continue;
                }

                // MRIluminacionRadial
                var illumination = slot.CachedIllumination;
                if (illumination != null)
                {
                    try
                    {
                        // Sobrescribir AnimationPath si hay directorio de salida configurado
                        if (!string.IsNullOrEmpty(_outputDirectory))
                        {
                            illumination.AnimationPath = _outputDirectory;
                        }

                        // Auto-escanear materiales si no hay detectados
                        if (illumination.DetectedMaterials.Count == 0 && illumination.RootObject != null)
                        {
                            illumination.ScanMaterials();
                            Debug.Log($"[MRSlotInfoCollector] Auto-escaneados materiales para: {slot.slotName}");
                        }

                        if (illumination.CanGenerateAnimation)
                        {
                            illumination.GenerateIlluminationAnimation();
                            Debug.Log($"[MRSlotInfoCollector] Auto-generada animación para: {slot.slotName} (MRIluminacionRadial)");
                            generatedCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"[MRSlotInfoCollector] No se puede generar iluminación para {slot.slotName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MRSlotInfoCollector] Error generando iluminación para {slot.slotName}: {ex.Message}");
                    }
                    continue;
                }

                // MRUnificarMateriales
                var unifyMaterial = slot.CachedUnifyMaterial;
                if (unifyMaterial != null)
                {
                    try
                    {
                        // Sobrescribir AnimationPath si hay directorio de salida configurado
                        if (!string.IsNullOrEmpty(_outputDirectory))
                        {
                            unifyMaterial.AnimationPath = _outputDirectory;
                        }

                        if (unifyMaterial.CanGenerateAnimation)
                        {
                            UnifyMaterialAnimationBuilder.GenerateAnimation(unifyMaterial);
                            Debug.Log($"[MRSlotInfoCollector] Auto-generada animación para: {slot.slotName} (MRUnificarMateriales)");
                            generatedCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"[MRSlotInfoCollector] No se puede generar animación para {slot.slotName} (MRUnificarMateriales sin slots vinculados)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MRSlotInfoCollector] Error generando animación para {slot.slotName}: {ex.Message}");
                    }
                    continue;
                }
            }

            if (generatedCount > 0)
            {
                Debug.Log($"[MRSlotInfoCollector] Total de animaciones generadas para '{menu.name}': {generatedCount}");
            }
        }

        /// <summary>
        /// Valida que todos los slots tengan los clips necesarios
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <returns>Lista de errores encontrados</returns>
        public List<string> ValidateSlotClips(List<MRSlotInfo> slotInfoList)
        {
            var errors = new List<string>();

            ValidateSlotClipsRecursively(slotInfoList, errors);

            return errors;
        }

        /// <summary>
        /// Valida clips recursivamente
        /// </summary>
        private void ValidateSlotClipsRecursively(List<MRSlotInfo> slotInfoList, List<string> errors)
        {
            foreach (var slotInfo in slotInfoList)
            {
                if (slotInfo.AnimationType == AnimationType.SubMenu)
                {
                    if (slotInfo.ChildSlotInfos != null)
                    {
                        ValidateSlotClipsRecursively(slotInfo.ChildSlotInfos, errors);
                    }
                    continue;
                }

                if (slotInfo.AnimationType == AnimationType.None)
                    continue;

                int requiredClips = GetRequiredClipCount(slotInfo.AnimationType);
                if (slotInfo.AnimationClips.Count < requiredClips)
                {
                    errors.Add($"Slot '{slotInfo.DisplayName}' requiere {requiredClips} clip(s) pero tiene {slotInfo.AnimationClips.Count}");
                }
            }
        }

        /// <summary>
        /// Obtiene el número de clips requeridos para un tipo de animación
        /// </summary>
        private int GetRequiredClipCount(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.OnOff:
                case AnimationType.AB:
                    return 2;
                case AnimationType.Linear:
                    return 1;
                default:
                    return 0;
            }
        }
#endif
    }
}
