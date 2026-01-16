#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Bender_Dios.MenuRadial.Components.MenuRadial;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Core.Common;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.MenuRadial.MRMenuRadialPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Plugin NDMF para MR Menu Radial.
    /// USA LOS ARCHIVOS GENERADOS por el botón "Generar Archivos VRChat".
    /// No genera animaciones nuevas, solo mezcla los archivos existentes con el avatar.
    /// </summary>
    public class MRMenuRadialPlugin : Plugin<MRMenuRadialPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.menu_radial";
        public override string DisplayName => "MR Menu Radial";

        // Color tema: Naranja
        public override Color? ThemeColor => new Color(0xFF / 255f, 0x69 / 255f, 0x00 / 255f, 1);

        protected override void Configure()
        {
            // Ejecutar en fase Transforming, después de Modular Avatar
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .WithRequiredExtension(typeof(AnimatorServicesContext), seq =>
                {
                    seq.Run(MRMenuRadialPass.Instance);
                });
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRMenuRadial NDMF] Error durante el procesamiento: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que mezcla los archivos VRChat generados con el avatar.
    /// </summary>
    internal class MRMenuRadialPass : Pass<MRMenuRadialPass>
    {
        public override string DisplayName => "MR Menu Radial - Mezclar archivos generados";

        protected override void Execute(BuildContext context)
        {
            var avatarDescriptor = context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                Debug.LogError("[MRMenuRadial NDMF] No se encontró VRCAvatarDescriptor en el avatar");
                return;
            }

            // Buscar MRMenuRadial
            var menuRadials = FindMenuRadials(context);

            if (menuRadials.Length == 0)
            {
                Debug.Log("[MRMenuRadial NDMF] No se encontraron componentes MRMenuRadial para este avatar");
                return;
            }

            // Verificar si el merge está desactivado
            foreach (var menuRadial in menuRadials)
            {
                if (menuRadial != null && menuRadial.DisableVRChatMergeNDMF)
                {
                    Debug.Log("[MRMenuRadial NDMF] Merge de archivos VRChat DESACTIVADO desde MRMenuRadial. Saltando proceso.");
                    return;
                }
            }

            Debug.Log($"[MRMenuRadial NDMF] Procesando {menuRadials.Length} componente(s) MRMenuRadial...");

            var asc = context.Extension<AnimatorServicesContext>();

            // Determinar cuáles están dentro del avatar (se pueden limpiar) y cuáles están fuera (no tocar)
            var internalMenuRadials = context.AvatarRootObject.GetComponentsInChildren<MRMenuRadial>(true);
            var internalSet = new HashSet<MRMenuRadial>(internalMenuRadials);

            foreach (var menuRadial in menuRadials)
            {
                if (menuRadial == null) continue;

                // Guardar nombre antes de cualquier operación
                string menuRadialName = "Unknown";
                try { menuRadialName = menuRadial.gameObject.name; } catch { }

                if (!menuRadial.enabled)
                {
                    Debug.Log($"[MRMenuRadial NDMF] Saltando '{menuRadialName}' (deshabilitado)");
                    continue;
                }

                bool isInternal = internalSet.Contains(menuRadial);

                try
                {
                    ProcessMenuRadial(context, asc, avatarDescriptor, menuRadial, isInternal);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MRMenuRadial NDMF] Error procesando '{menuRadialName}': {e.Message}");
                    Debug.LogException(e);
                }
            }

            Debug.Log("[MRMenuRadial NDMF] Procesamiento completado");
        }

        private MRMenuRadial[] FindMenuRadials(BuildContext context)
        {
            // Primero buscar dentro del avatar
            var menuRadials = context.AvatarRootObject.GetComponentsInChildren<MRMenuRadial>(true);

            if (menuRadials.Length == 0)
            {
                // Buscar MRMenuRadial externo que referencie este avatar
                string avatarName = context.AvatarRootObject.name;
                if (avatarName.EndsWith("(Clone)"))
                {
                    avatarName = avatarName.Substring(0, avatarName.Length - 7).Trim();
                }

                Debug.Log($"[MRMenuRadial NDMF] Buscando MRMenuRadial externo para avatar '{avatarName}'...");

                var allMenuRadials = UnityEngine.Object.FindObjectsByType<MRMenuRadial>(FindObjectsSortMode.None);
                menuRadials = allMenuRadials
                    .Where(mr => mr != null && mr.AvatarRoot != null && mr.AvatarRoot.name == avatarName)
                    .ToArray();
            }

            return menuRadials;
        }

        private void ProcessMenuRadial(
            BuildContext context,
            AnimatorServicesContext asc,
            VRCAvatarDescriptor avatar,
            MRMenuRadial menuRadial,
            bool isInternalToAvatar)
        {
            // Guardar datos antes de cualquier operación que pueda destruir el objeto
            string outputDir = menuRadial.GetVRChatOutputDirectory();
            string prefix = menuRadial.OutputPrefix;
            bool writeDefaults = menuRadial.WriteDefaultValues;
            string menuRadialName = menuRadial.gameObject.name;

            Debug.Log($"[MRMenuRadial NDMF] Buscando archivos en: {outputDir}");

            // Construir nombres de archivos
            string fxFileName = string.IsNullOrEmpty(prefix)
                ? "FX_Menu_Radial.controller"
                : $"{prefix}_FX_Menu_Radial.controller";
            string paramsFileName = string.IsNullOrEmpty(prefix)
                ? "Parametro_Menu_Radial.asset"
                : $"{prefix}_Parametro_Menu_Radial.asset";
            string menuFileName = string.IsNullOrEmpty(prefix)
                ? "Menu_Menu_Radial.asset"
                : $"{prefix}_Menu_Menu_Radial.asset";

            string fxPath = $"{outputDir}{fxFileName}";
            string paramsPath = $"{outputDir}{paramsFileName}";
            string menuPath = $"{outputDir}{menuFileName}";

            // Cargar archivos generados
            var generatedFX = AssetDatabase.LoadAssetAtPath<AnimatorController>(fxPath);
            var generatedParams = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(paramsPath);
            var generatedMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(menuPath);

            // Verificar que existan
            if (generatedFX == null)
            {
                Debug.LogWarning($"[MRMenuRadial NDMF] No se encontró FX Controller en: {fxPath}");
                Debug.LogWarning("[MRMenuRadial NDMF] Primero usa el botón 'Generar Archivos VRChat' en MRMenuRadial.");
                return;
            }

            if (generatedParams == null)
            {
                Debug.LogWarning($"[MRMenuRadial NDMF] No se encontró Parameters en: {paramsPath}");
                return;
            }

            if (generatedMenu == null)
            {
                Debug.LogWarning($"[MRMenuRadial NDMF] No se encontró Menu en: {menuPath}");
                return;
            }

            Debug.Log($"[MRMenuRadial NDMF] Archivos encontrados. Mezclando con avatar...");

            // 1. Mezclar FX Controller
            MergeFXController(asc, generatedFX, writeDefaults);

            // 2. Mezclar Parameters
            MergeParameters(context, avatar, generatedParams);

            // 3. Mezclar Menu
            MergeMenu(context, avatar, generatedMenu, prefix);

            // 4. Limpiar componentes MR del avatar clonado SOLO si es interno
            // NO destruir MRMenuRadial externos porque no son parte del clon
            if (isInternalToAvatar && menuRadial != null)
            {
                CleanupComponents(menuRadial);
            }

            Debug.Log($"[MRMenuRadial NDMF] Merge completado para '{menuRadialName}'");
        }

        /// <summary>
        /// Mezcla el FX Controller generado con el del avatar.
        /// Copia todas las capas del FX generado al FX del avatar.
        /// </summary>
        private void MergeFXController(AnimatorServicesContext asc, AnimatorController generatedFX, bool writeDefaults)
        {
            if (generatedFX == null)
            {
                Debug.LogWarning("[MRMenuRadial NDMF] generatedFX es null");
                return;
            }

            // Obtener o crear el FX Controller virtual del avatar
            if (!asc.ControllerContext.Controllers.TryGetValue(VRCAvatarDescriptor.AnimLayerType.FX, out var avatarFX))
            {
                avatarFX = VirtualAnimatorController.Create(asc.ControllerContext.CloneContext, "FX");
                asc.ControllerContext.Controllers[VRCAvatarDescriptor.AnimLayerType.FX] = avatarFX;
                Debug.Log("[MRMenuRadial NDMF] Creado nuevo FX Controller virtual");
            }

            var cloneContext = asc.ControllerContext.CloneContext;

            // Copiar parámetros del FX generado
            if (generatedFX.parameters != null)
            {
                foreach (var param in generatedFX.parameters)
                {
                    if (param == null || string.IsNullOrEmpty(param.name)) continue;

                    if (!avatarFX.Parameters.ContainsKey(param.name))
                    {
                        avatarFX.Parameters = avatarFX.Parameters.Add(param.name, param);
                        Debug.Log($"[MRMenuRadial NDMF] Añadido parámetro FX: {param.name}");
                    }
                }
            }

            // Copiar capas del FX generado (excepto la base layer vacía si existe)
            if (generatedFX.layers != null)
            {
                for (int i = 0; i < generatedFX.layers.Length; i++)
                {
                    var sourceLayer = generatedFX.layers[i];
                    if (sourceLayer.stateMachine == null) continue;

                    // Saltar la primera capa si está vacía (es la capa base por defecto)
                    if (i == 0 && (sourceLayer.stateMachine.states == null || sourceLayer.stateMachine.states.Length == 0))
                    {
                        continue;
                    }

                    try
                    {
                        // Crear capa virtual
                        var virtualLayer = avatarFX.AddLayer(LayerPriority.Default, sourceLayer.name ?? $"Layer_{i}");
                        virtualLayer.DefaultWeight = sourceLayer.defaultWeight;

                        // Clonar la state machine
                        var virtualStateMachine = CloneStateMachine(cloneContext, sourceLayer.stateMachine, writeDefaults);
                        virtualLayer.StateMachine = virtualStateMachine;

                        Debug.Log($"[MRMenuRadial NDMF] Añadida capa FX: {sourceLayer.name}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MRMenuRadial NDMF] Error añadiendo capa '{sourceLayer.name}': {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clona una StateMachine para el sistema virtual de NDMF.
        /// </summary>
        private VirtualStateMachine CloneStateMachine(CloneContext cloneContext, AnimatorStateMachine source, bool writeDefaults)
        {
            if (source == null)
            {
                Debug.LogWarning("[MRMenuRadial NDMF] StateMachine source es null");
                return VirtualStateMachine.Create(cloneContext, "Empty");
            }

            var virtualSM = VirtualStateMachine.Create(cloneContext, source.name ?? "StateMachine");

            // Diccionario para mapear estados originales a virtuales
            var stateMap = new Dictionary<AnimatorState, VirtualState>();

            // Clonar estados
            if (source.states != null)
            {
                foreach (var childState in source.states)
                {
                    var state = childState.state;
                    if (state == null) continue;

                    VirtualMotion virtualMotion = null;

                    try
                    {
                        if (state.motion != null)
                        {
                            if (state.motion is AnimationClip clip && clip != null)
                            {
                                virtualMotion = cloneContext.Clone(clip);
                            }
                            else if (state.motion is BlendTree blendTree && blendTree != null)
                            {
                                virtualMotion = cloneContext.Clone(blendTree);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MRMenuRadial NDMF] Error clonando motion de estado '{state.name}': {e.Message}");
                    }

                    var virtualState = virtualSM.AddState(state.name ?? "State", virtualMotion);
                    virtualState.WriteDefaultValues = writeDefaults;

                    // Copiar propiedades del estado
                    try
                    {
                        if (!string.IsNullOrEmpty(state.timeParameter))
                        {
                            virtualState.TimeParameter = state.timeParameter;
                        }
                        virtualState.Speed = state.speed;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MRMenuRadial NDMF] Error copiando propiedades de estado '{state.name}': {e.Message}");
                    }

                    stateMap[state] = virtualState;

                    // Si es el estado por defecto, marcarlo
                    if (source.defaultState != null && source.defaultState == state)
                    {
                        virtualSM.DefaultState = virtualState;
                    }
                }
            }

            // Clonar transiciones AnyState
            if (source.anyStateTransitions != null)
            {
                foreach (var transition in source.anyStateTransitions)
                {
                    if (transition == null) continue;
                    if (transition.destinationState == null) continue;
                    if (!stateMap.TryGetValue(transition.destinationState, out var destVirtual)) continue;

                    try
                    {
                        var virtualTransition = VirtualStateTransition.Create();
                        virtualTransition.SetDestination(destVirtual);
                        virtualTransition.Duration = transition.duration;
                        virtualTransition.ExitTime = transition.hasExitTime ? transition.exitTime : (float?)null;
                        virtualTransition.CanTransitionToSelf = transition.canTransitionToSelf;

                        // Copiar condiciones
                        if (transition.conditions != null && transition.conditions.Length > 0)
                        {
                            virtualTransition.Conditions = System.Collections.Immutable.ImmutableList.CreateRange(transition.conditions);
                        }

                        virtualSM.AnyStateTransitions = virtualSM.AnyStateTransitions.Add(virtualTransition);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MRMenuRadial NDMF] Error clonando transición: {e.Message}");
                    }
                }
            }

            return virtualSM;
        }

        /// <summary>
        /// Mezcla los parámetros generados con los del avatar.
        /// </summary>
        private void MergeParameters(BuildContext context, VRCAvatarDescriptor avatar, VRCExpressionParameters generatedParams)
        {
            // Obtener o crear parameters del avatar
            if (avatar.expressionParameters == null)
            {
                avatar.expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                avatar.expressionParameters.name = "Parameters";
                context.AssetSaver.SaveAsset(avatar.expressionParameters);
            }
            else
            {
                // Clonar para no modificar el original
                var clone = UnityEngine.Object.Instantiate(avatar.expressionParameters);
                clone.name = avatar.expressionParameters.name;
                context.AssetSaver.SaveAsset(clone);
                avatar.expressionParameters = clone;
            }

            // Lista de parámetros existentes
            var paramList = new List<VRCExpressionParameters.Parameter>(
                avatar.expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>()
            );

            // Añadir parámetros del archivo generado que no existan
            foreach (var param in generatedParams.parameters)
            {
                if (!paramList.Any(p => p.name == param.name))
                {
                    paramList.Add(new VRCExpressionParameters.Parameter
                    {
                        name = param.name,
                        valueType = param.valueType,
                        defaultValue = param.defaultValue,
                        saved = param.saved,
                        networkSynced = param.networkSynced
                    });
                    Debug.Log($"[MRMenuRadial NDMF] Añadido parámetro: {param.name} (default={param.defaultValue})");
                }
                else
                {
                    Debug.Log($"[MRMenuRadial NDMF] Parámetro '{param.name}' ya existe, saltando");
                }
            }

            avatar.expressionParameters.parameters = paramList.ToArray();
        }

        /// <summary>
        /// Mezcla el menú generado con el del avatar.
        /// Añade el menú generado como submenú del menú principal del avatar.
        /// </summary>
        private void MergeMenu(BuildContext context, VRCAvatarDescriptor avatar, VRCExpressionsMenu generatedMenu, string prefix)
        {
            // Obtener o crear menú del avatar
            if (avatar.expressionsMenu == null)
            {
                avatar.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                avatar.expressionsMenu.name = "Menu";
                context.AssetSaver.SaveAsset(avatar.expressionsMenu);
            }
            else
            {
                // Clonar para no modificar el original
                var clone = UnityEngine.Object.Instantiate(avatar.expressionsMenu);
                clone.name = avatar.expressionsMenu.name;
                context.AssetSaver.SaveAsset(clone);
                avatar.expressionsMenu = clone;
            }

            // Clonar el menú generado y sus submenús recursivamente
            var clonedMenu = CloneMenuRecursive(context, generatedMenu);

            // Nombre del submenú
            string subMenuName = string.IsNullOrEmpty(prefix) ? "Menu Radial" : prefix;

            // Verificar si ya existe un control con ese nombre
            var existingControl = avatar.expressionsMenu.controls.FirstOrDefault(c => c.name == subMenuName);
            if (existingControl != null)
            {
                // Actualizar el submenú existente
                existingControl.subMenu = clonedMenu;
                Debug.Log($"[MRMenuRadial NDMF] Actualizado submenú existente: {subMenuName}");
            }
            else
            {
                // Añadir como nuevo submenú
                var control = new VRCExpressionsMenu.Control
                {
                    name = subMenuName,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = clonedMenu
                };

                avatar.expressionsMenu.controls.Add(control);
                Debug.Log($"[MRMenuRadial NDMF] Añadido submenú: {subMenuName}");

                // Si excede el límite de 8 controles, crear submenú "More"
                SplitMenuIfNeeded(context, avatar.expressionsMenu);
            }
        }

        /// <summary>
        /// Clona un menú y todos sus submenús recursivamente.
        /// </summary>
        private VRCExpressionsMenu CloneMenuRecursive(BuildContext context, VRCExpressionsMenu source)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name;
            context.AssetSaver.SaveAsset(clone);

            // Clonar submenús recursivamente
            for (int i = 0; i < clone.controls.Count; i++)
            {
                var control = clone.controls[i];
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                {
                    control.subMenu = CloneMenuRecursive(context, control.subMenu);
                }
            }

            return clone;
        }

        /// <summary>
        /// Divide el menú si excede el límite de 8 controles.
        /// </summary>
        private void SplitMenuIfNeeded(BuildContext context, VRCExpressionsMenu menu)
        {
            const int MAX_CONTROLS = 8;

            while (menu.controls.Count > MAX_CONTROLS)
            {
                var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                newMenu.name = menu.name + "_More";
                context.AssetSaver.SaveAsset(newMenu);

                int keepCount = MAX_CONTROLS - 1;
                newMenu.controls.AddRange(menu.controls.Skip(keepCount));
                menu.controls.RemoveRange(keepCount, menu.controls.Count - keepCount);

                menu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = "More",
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = newMenu
                });

                menu = newMenu;
            }
        }

        /// <summary>
        /// Limpia los componentes MR del avatar clonado después del procesamiento.
        /// Elimina TODOS los componentes del sistema MR para evitar advertencias de VRChat.
        /// </summary>
        private void CleanupComponents(MRMenuRadial menuRadial)
        {
            if (menuRadial == null) return;

            var rootObject = menuRadial.gameObject;

            // Lista de tipos de componentes MR a eliminar (en orden de dependencia inversa)
            var typesToRemove = new[]
            {
                // Componentes de Frame
                typeof(Bender_Dios.MenuRadial.Components.Frame.MRAgruparObjetos),
                // Componentes de Radial
                typeof(Bender_Dios.MenuRadial.Components.Radial.MRUnificarObjetos),
                // Componentes de Illumination
                typeof(Bender_Dios.MenuRadial.Components.Illumination.MRIluminacionRadial),
                // Componentes de Menu (buscar por nombre ya que están en assembly diferente)
                typeof(MRMenuControl),
            };

            // Eliminar componentes por tipo
            foreach (var type in typesToRemove)
            {
                var components = rootObject.GetComponentsInChildren(type, true);
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        UnityEngine.Object.DestroyImmediate(comp);
                    }
                }
            }

            // Eliminar cualquier MonoBehaviour cuyo tipo contenga "MR" en el namespace Bender_Dios
            var allMonoBehaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;
                var typeName = mb.GetType().FullName;
                if (typeName != null && typeName.StartsWith("Bender_Dios.MenuRadial"))
                {
                    UnityEngine.Object.DestroyImmediate(mb);
                }
            }

            // Finalmente eliminar MRMenuRadial
            if (menuRadial != null)
            {
                UnityEngine.Object.DestroyImmediate(menuRadial);
            }

            Debug.Log("[MRMenuRadial NDMF] Componentes MR limpiados del avatar");
        }
    }
}
#endif
