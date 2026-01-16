#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Core.Common;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.MenuRadial.MRMenuRadialPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Estado para almacenar los valores por defecto de los parámetros
    /// </summary>
    internal class MRDefaultValues
    {
        public ImmutableDictionary<string, float> InitialValueOverrides = ImmutableDictionary<string, float>.Empty;
    }

    /// <summary>
    /// Plugin NDMF para MR Menu Radial.
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
            // Usar WithRequiredExtension para acceder al AnimatorServicesContext
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .WithRequiredExtension(typeof(AnimatorServicesContext), seq =>
                {
                    seq.Run(MRMenuRadialPass.Instance);
                    seq.Run(MRApplyDefaultValuesPass.Instance);
                });
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRMenuRadial NDMF] Error durante el procesamiento: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que ejecuta la integración del menú radial durante el build.
    /// </summary>
    internal class MRMenuRadialPass : Pass<MRMenuRadialPass>
    {
        public override string DisplayName => "MR Menu Radial - Integrar con Avatar";

        protected override void Execute(BuildContext context)
        {
            var avatarDescriptor = context.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                Debug.LogError("[MRMenuRadial NDMF] No se encontró VRCAvatarDescriptor en el avatar");
                return;
            }

            // Primero buscar MRMenuRadial dentro del avatar (caso ideal - es hijo del avatar)
            var menuRadials = context.AvatarRootObject.GetComponentsInChildren<MRMenuRadial>(true);

            if (menuRadials.Length == 0)
            {
                // Si no está dentro del avatar, buscar en la escena
                // Durante NDMF, context.AvatarRootObject es un CLON del avatar original
                // Necesitamos encontrar MRMenuRadial que referencien el avatar original por nombre
                string avatarName = context.AvatarRootObject.name;

                // Quitar sufijo "(Clone)" si existe
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

            if (menuRadials.Length == 0)
            {
                Debug.Log("[MRMenuRadial NDMF] No se encontraron componentes MRMenuRadial para este avatar");
                return;
            }

            // Verificar si el merge de VRChat está desactivado
            foreach (var menuRadial in menuRadials)
            {
                if (menuRadial != null && menuRadial.DisableVRChatMergeNDMF)
                {
                    Debug.Log("[MRMenuRadial NDMF] Merge de archivos VRChat DESACTIVADO desde MRMenuRadial. Saltando proceso.");
                    return;
                }
            }

            Debug.Log($"[MRMenuRadial NDMF] Procesando {menuRadials.Length} componente(s) MRMenuRadial...");

            // Obtener AnimatorServicesContext
            var asc = context.Extension<AnimatorServicesContext>();

            // Inicializar estado para valores por defecto
            var defaultValues = context.GetState<MRDefaultValues>();
            var valueOverrides = defaultValues.InitialValueOverrides;

            foreach (var menuRadial in menuRadials)
            {
                if (!menuRadial.enabled)
                {
                    Debug.Log($"[MRMenuRadial NDMF] Saltando '{menuRadial.gameObject.name}' (deshabilitado)");
                    continue;
                }

                try
                {
                    valueOverrides = ProcessMenuRadial(context, asc, avatarDescriptor, menuRadial, valueOverrides);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MRMenuRadial NDMF] Error procesando '{menuRadial.gameObject.name}': {e.Message}");
                    Debug.LogException(e);
                }
            }

            // Guardar valores por defecto en el estado
            defaultValues.InitialValueOverrides = valueOverrides;

            Debug.Log("[MRMenuRadial NDMF] Procesamiento completado");
        }

        private ImmutableDictionary<string, float> ProcessMenuRadial(
            BuildContext context,
            AnimatorServicesContext asc,
            VRCAvatarDescriptor avatar,
            MRMenuRadial menuRadial,
            ImmutableDictionary<string, float> valueOverrides)
        {
            var menuControl = menuRadial.GetComponentInChildren<MRMenuControl>(true);
            if (menuControl == null)
            {
                Debug.LogWarning($"[MRMenuRadial NDMF] No se encontró MRMenuControl en '{menuRadial.gameObject.name}'");
                return valueOverrides;
            }

            var slotInfoList = CollectSlotInfo(menuControl);
            if (slotInfoList.Count == 0)
            {
                Debug.LogWarning($"[MRMenuRadial NDMF] No hay slots válidos en '{menuRadial.gameObject.name}'");
                return valueOverrides;
            }

            Debug.Log($"[MRMenuRadial NDMF] Encontrados {slotInfoList.Count} slots válidos");

            // 1. Obtener el FX Controller virtual y añadir layers
            valueOverrides = MergeFXController(context, asc, avatar, menuRadial, slotInfoList, valueOverrides);

            // 2. Añadir parámetros al avatar
            MergeParameters(context, avatar, slotInfoList);

            // 3. Añadir menú
            MergeMenu(context, avatar, menuRadial, menuControl, slotInfoList);

            // 4. Limpiar componentes
            CleanupComponents(menuRadial);

            return valueOverrides;
        }

        private List<SlotInfo> CollectSlotInfo(MRMenuControl menuControl)
        {
            var result = new List<SlotInfo>();
            var slots = menuControl.AnimationSlots;

            foreach (var slot in slots)
            {
                if (slot == null || !slot.isValid || slot.targetObject == null)
                    continue;

                var animationType = slot.GetAnimationType();
                if (animationType == AnimationType.None || animationType == AnimationType.SubMenu)
                    continue;

                var provider = slot.GetAnimationProvider();
                if (provider == null)
                    continue;

                bool isIllumination = slot.targetObject.GetComponent<MRIluminacionRadial>() != null;

                bool defaultIsOn = false;
                if (provider is MRUnificarObjetos unificar)
                {
                    defaultIsOn = unificar.DefaultStateIsOn;
                }

                result.Add(new SlotInfo
                {
                    SlotName = slot.slotName,
                    AnimationType = animationType,
                    Provider = provider,
                    TargetObject = slot.targetObject,
                    IsIllumination = isIllumination,
                    DefaultStateIsOn = defaultIsOn
                });
            }

            return result;
        }

        private ImmutableDictionary<string, float> MergeFXController(
            BuildContext context,
            AnimatorServicesContext asc,
            VRCAvatarDescriptor avatar,
            MRMenuRadial menuRadial,
            List<SlotInfo> slotInfoList,
            ImmutableDictionary<string, float> valueOverrides)
        {
            // Obtener el FX Controller virtual a través de AnimatorServicesContext
            VirtualAnimatorController fxController;

            if (!asc.ControllerContext.Controllers.TryGetValue(VRCAvatarDescriptor.AnimLayerType.FX, out fxController))
            {
                // Crear nuevo controller si no existe
                fxController = VirtualAnimatorController.Create(asc.ControllerContext.CloneContext, "FX");
                asc.ControllerContext.Controllers[VRCAvatarDescriptor.AnimLayerType.FX] = fxController;
                Debug.Log("[MRMenuRadial NDMF] Creado nuevo FX Controller virtual");
            }

            bool writeDefaults = menuRadial.WriteDefaultValues;

            foreach (var slotInfo in slotInfoList)
            {
                Debug.Log($"[MRMenuRadial NDMF] Generando capa para '{slotInfo.SlotName}' ({slotInfo.AnimationType})");

                // Generar animaciones
                var animations = GenerateAnimationsForSlot(context, asc, slotInfo);
                if (animations == null || animations.Count == 0)
                {
                    Debug.LogWarning($"[MRMenuRadial NDMF] No se pudieron generar animaciones para '{slotInfo.SlotName}'");
                    continue;
                }

                // Crear layer en el FX Controller virtual
                CreateVirtualFXLayer(asc, fxController, slotInfo, animations, writeDefaults);

                // Registrar valor por defecto para que se aplique después
                float defaultValue = slotInfo.GetDefaultValue();
                valueOverrides = valueOverrides.SetItem(slotInfo.SlotName, defaultValue);

                Debug.Log($"[MRMenuRadial NDMF] Registrado valor por defecto para '{slotInfo.SlotName}': {defaultValue}");
            }

            return valueOverrides;
        }

        private Dictionary<string, VirtualClip> GenerateAnimationsForSlot(BuildContext context, AnimatorServicesContext asc, SlotInfo slotInfo)
        {
            var animations = new Dictionary<string, VirtualClip>();
            var cloneContext = asc.ControllerContext.CloneContext;

            if (slotInfo.Provider is MRUnificarObjetos unificarObjetos)
            {
                switch (slotInfo.AnimationType)
                {
                    case AnimationType.OnOff:
                        var onClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_on");
                        var offClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_off");
                        GenerateOnOffAnimations(context, unificarObjetos, onClip, true);
                        GenerateOnOffAnimations(context, unificarObjetos, offClip, false);
                        animations["on"] = onClip;
                        animations["off"] = offClip;
                        break;

                    case AnimationType.AB:
                        var aClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_A");
                        var bClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_B");
                        GenerateABAnimations(context, unificarObjetos, aClip, true);
                        GenerateABAnimations(context, unificarObjetos, bClip, false);
                        animations["A"] = aClip;
                        animations["B"] = bClip;
                        break;

                    case AnimationType.Linear:
                        var linClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_lin");
                        GenerateLinearAnimation(context, unificarObjetos, linClip);
                        animations["linear"] = linClip;
                        break;
                }
            }
            else if (slotInfo.Provider is MRIluminacionRadial iluminacion)
            {
                var linClip = CreateVirtualClip(context, cloneContext, $"{slotInfo.SlotName}_lin");
                GenerateIlluminationAnimation(context, iluminacion, linClip);
                animations["linear"] = linClip;
            }

            return animations;
        }

        private VirtualClip CreateVirtualClip(BuildContext context, CloneContext cloneContext, string name)
        {
            var clip = new AnimationClip();
            clip.name = name;
            return cloneContext.Clone(clip);
        }

        private void GenerateOnOffAnimations(BuildContext context, MRUnificarObjetos unificar, VirtualClip clip, bool activeState)
        {
            var frames = unificar.GetFrames();
            if (frames == null || frames.Count == 0) return;

            var frame = frames[0];
            var objects = frame.ObjectReferences;

            foreach (var objRef in objects)
            {
                if (objRef?.GameObject == null) continue;

                string path = GetRelativePath(context.AvatarRootTransform, objRef.GameObject.transform);
                float value = activeState ? (objRef.IsActive ? 1f : 0f) : (objRef.IsActive ? 0f : 1f);

                var binding = new EditorCurveBinding
                {
                    path = path,
                    type = typeof(GameObject),
                    propertyName = "m_IsActive"
                };

                var curve = new AnimationCurve(new Keyframe(0f, value));
                clip.SetFloatCurve(binding, curve);
            }
        }

        private void GenerateABAnimations(BuildContext context, MRUnificarObjetos unificar, VirtualClip clip, bool isA)
        {
            var frames = unificar.GetFrames();
            if (frames == null || frames.Count < 2) return;

            var frame = isA ? frames[0] : frames[1];
            var objects = frame.ObjectReferences;

            foreach (var objRef in objects)
            {
                if (objRef?.GameObject == null) continue;

                string path = GetRelativePath(context.AvatarRootTransform, objRef.GameObject.transform);
                float value = objRef.IsActive ? 1f : 0f;

                var binding = new EditorCurveBinding
                {
                    path = path,
                    type = typeof(GameObject),
                    propertyName = "m_IsActive"
                };

                var curve = new AnimationCurve(new Keyframe(0f, value));
                clip.SetFloatCurve(binding, curve);
            }
        }

        private void GenerateLinearAnimation(BuildContext context, MRUnificarObjetos unificar, VirtualClip clip)
        {
            var frames = unificar.GetFrames();
            if (frames == null || frames.Count < 3) return;

            int frameCount = frames.Count;

            var allObjects = new HashSet<GameObject>();
            foreach (var frame in frames)
            {
                foreach (var objRef in frame.ObjectReferences)
                {
                    if (objRef?.GameObject != null)
                        allObjects.Add(objRef.GameObject);
                }
            }

            foreach (var obj in allObjects)
            {
                string path = GetRelativePath(context.AvatarRootTransform, obj.transform);
                var keyframes = new List<Keyframe>();

                for (int i = 0; i < frameCount; i++)
                {
                    float time = i / 60f;
                    var frame = frames[i];
                    var objRef = frame.ObjectReferences.FirstOrDefault(r => r?.GameObject == obj);
                    float value = objRef != null && objRef.IsActive ? 1f : 0f;
                    keyframes.Add(new Keyframe(time, value));
                }

                var binding = new EditorCurveBinding
                {
                    path = path,
                    type = typeof(GameObject),
                    propertyName = "m_IsActive"
                };

                var curve = new AnimationCurve(keyframes.ToArray());
                clip.SetFloatCurve(binding, curve);
            }
        }

        private void GenerateIlluminationAnimation(BuildContext context, MRIluminacionRadial iluminacion, VirtualClip clip)
        {
            var rootObject = iluminacion.RootObject;
            if (rootObject == null) return;

            Debug.Log($"[MRMenuRadial NDMF] Generando animación de iluminación para '{iluminacion.gameObject.name}'");
        }

        private string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return "";

            var path = target.name;
            var current = target.parent;

            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private void CreateVirtualFXLayer(AnimatorServicesContext asc, VirtualAnimatorController controller, SlotInfo slotInfo, Dictionary<string, VirtualClip> animations, bool writeDefaults)
        {
            string layerName = $"MR_{slotInfo.SlotName}";
            string paramName = slotInfo.SlotName;
            var cloneContext = asc.ControllerContext.CloneContext;

            // Añadir parámetro al controller virtual
            var parameters = controller.Parameters;
            if (!parameters.ContainsKey(paramName))
            {
                float defaultValue = slotInfo.GetDefaultValue();

                AnimatorControllerParameter param;
                if (slotInfo.AnimationType == AnimationType.Linear)
                {
                    param = new AnimatorControllerParameter
                    {
                        name = paramName,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = defaultValue
                    };
                }
                else
                {
                    param = new AnimatorControllerParameter
                    {
                        name = paramName,
                        type = AnimatorControllerParameterType.Bool,
                        defaultBool = defaultValue > 0.5f
                    };
                }

                parameters = parameters.Add(paramName, param);
                controller.Parameters = parameters;
            }

            // Crear layer virtual
            var layer = controller.AddLayer(LayerPriority.Default, layerName);
            layer.DefaultWeight = 1f;

            // Crear state machine
            var stateMachine = VirtualStateMachine.Create(cloneContext, layerName);
            layer.StateMachine = stateMachine;

            switch (slotInfo.AnimationType)
            {
                case AnimationType.OnOff:
                case AnimationType.AB:
                    CreateVirtualToggleStateMachine(cloneContext, stateMachine, paramName, animations, writeDefaults, slotInfo);
                    break;

                case AnimationType.Linear:
                    CreateVirtualLinearStateMachine(cloneContext, stateMachine, paramName, animations, writeDefaults);
                    break;
            }
        }

        private void CreateVirtualToggleStateMachine(CloneContext cloneContext, VirtualStateMachine stateMachine, string paramName, Dictionary<string, VirtualClip> animations, bool writeDefaults, SlotInfo slotInfo)
        {
            var offClip = animations.ContainsKey("off") ? animations["off"] : animations.ContainsKey("A") ? animations["A"] : null;
            var onClip = animations.ContainsKey("on") ? animations["on"] : animations.ContainsKey("B") ? animations["B"] : null;

            if (offClip == null || onClip == null) return;

            // Crear estados virtuales
            var offState = stateMachine.AddState("Off", offClip);
            offState.WriteDefaultValues = writeDefaults;

            var onState = stateMachine.AddState("On", onClip);
            onState.WriteDefaultValues = writeDefaults;

            // Crear transición AnyState -> Off (cuando param = false)
            var toOff = VirtualStateTransition.Create();
            toOff.ExitTime = null; // Sin exit time
            toOff.Duration = 0f;
            toOff.CanTransitionToSelf = false;
            toOff.SetDestination(offState);
            toOff.Conditions = ImmutableList.Create(new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = paramName,
                threshold = 0
            });

            // Crear transición AnyState -> On (cuando param = true)
            var toOn = VirtualStateTransition.Create();
            toOn.ExitTime = null; // Sin exit time
            toOn.Duration = 0f;
            toOn.CanTransitionToSelf = false;
            toOn.SetDestination(onState);
            toOn.Conditions = ImmutableList.Create(new AnimatorCondition
            {
                mode = AnimatorConditionMode.If,
                parameter = paramName,
                threshold = 0
            });

            // Añadir transiciones al AnyStateTransitions
            stateMachine.AnyStateTransitions = stateMachine.AnyStateTransitions.Add(toOff).Add(toOn);

            // Estado por defecto
            bool defaultIsOn = slotInfo.AnimationType == AnimationType.OnOff && slotInfo.DefaultStateIsOn;
            stateMachine.DefaultState = defaultIsOn ? onState : offState;
        }

        private void CreateVirtualLinearStateMachine(CloneContext cloneContext, VirtualStateMachine stateMachine, string paramName, Dictionary<string, VirtualClip> animations, bool writeDefaults)
        {
            if (!animations.ContainsKey("linear")) return;

            var clip = animations["linear"];

            var state = stateMachine.AddState("Linear", clip);
            state.WriteDefaultValues = false; // Write Defaults OFF para radiales
            state.TimeParameter = paramName; // Esto automáticamente activa timeParameterActive
            state.Speed = 1f; // Speed 1 como los radiales estándar de VRChat

            stateMachine.DefaultState = state;
        }

        private void MergeParameters(BuildContext context, VRCAvatarDescriptor avatar, List<SlotInfo> slotInfoList)
        {
            if (avatar.expressionParameters == null)
            {
                avatar.expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                avatar.expressionParameters.name = "Parameters";
                context.AssetSaver.SaveAsset(avatar.expressionParameters);
            }
            else
            {
                var clone = UnityEngine.Object.Instantiate(avatar.expressionParameters);
                clone.name = avatar.expressionParameters.name;
                context.AssetSaver.SaveAsset(clone);
                avatar.expressionParameters = clone;
            }

            var paramList = new List<VRCExpressionParameters.Parameter>(
                avatar.expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>()
            );

            foreach (var slotInfo in slotInfoList)
            {
                if (paramList.Any(p => p.name == slotInfo.SlotName))
                {
                    Debug.Log($"[MRMenuRadial NDMF] Parámetro '{slotInfo.SlotName}' ya existe");
                    continue;
                }

                float defaultValue = slotInfo.GetDefaultValue();

                var param = new VRCExpressionParameters.Parameter
                {
                    name = slotInfo.SlotName,
                    valueType = slotInfo.AnimationType == AnimationType.Linear
                        ? VRCExpressionParameters.ValueType.Float
                        : VRCExpressionParameters.ValueType.Bool,
                    defaultValue = defaultValue,
                    saved = true,
                    networkSynced = true
                };

                paramList.Add(param);
                Debug.Log($"[MRMenuRadial NDMF] Añadido parámetro '{slotInfo.SlotName}' (default={defaultValue})");
            }

            avatar.expressionParameters.parameters = paramList.ToArray();
        }

        private void MergeMenu(BuildContext context, VRCAvatarDescriptor avatar, MRMenuRadial menuRadial, MRMenuControl menuControl, List<SlotInfo> slotInfoList)
        {
            if (avatar.expressionsMenu == null)
            {
                avatar.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                avatar.expressionsMenu.name = "Menu";
                context.AssetSaver.SaveAsset(avatar.expressionsMenu);
            }
            else
            {
                var clone = UnityEngine.Object.Instantiate(avatar.expressionsMenu);
                clone.name = avatar.expressionsMenu.name;
                context.AssetSaver.SaveAsset(clone);
                avatar.expressionsMenu = clone;
            }

            var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            subMenu.name = menuRadial.OutputPrefix ?? "Menu Radial";
            context.AssetSaver.SaveAsset(subMenu);

            foreach (var slotInfo in slotInfoList)
            {
                var control = new VRCExpressionsMenu.Control
                {
                    name = slotInfo.SlotName
                };

                switch (slotInfo.AnimationType)
                {
                    case AnimationType.OnOff:
                    case AnimationType.AB:
                        control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                        control.parameter = new VRCExpressionsMenu.Control.Parameter { name = slotInfo.SlotName };
                        break;

                    case AnimationType.Linear:
                        // Para RadialPuppet: parameter.name debe estar vacío, el parámetro va en subParameters
                        control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                        control.parameter = new VRCExpressionsMenu.Control.Parameter { name = "" };
                        control.subParameters = new VRCExpressionsMenu.Control.Parameter[]
                        {
                            new VRCExpressionsMenu.Control.Parameter { name = slotInfo.SlotName }
                        };
                        control.value = slotInfo.GetDefaultValue();
                        break;
                }

                subMenu.controls.Add(control);
            }

            SplitMenuIfNeeded(context, subMenu);

            var mainMenuControl = new VRCExpressionsMenu.Control
            {
                name = menuRadial.OutputPrefix ?? "Menu Radial",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu
            };

            avatar.expressionsMenu.controls.Add(mainMenuControl);
            SplitMenuIfNeeded(context, avatar.expressionsMenu);
        }

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

        private void CleanupComponents(MRMenuRadial menuRadial)
        {
            var componentsToDestroy = new List<Component>();

            var menuControl = menuRadial.GetComponentInChildren<MRMenuControl>(true);
            if (menuControl != null)
            {
                componentsToDestroy.AddRange(menuControl.GetComponentsInChildren<MRUnificarObjetos>(true));
                componentsToDestroy.AddRange(menuControl.GetComponentsInChildren<MRIluminacionRadial>(true));
                componentsToDestroy.Add(menuControl);
            }

            foreach (var comp in componentsToDestroy)
            {
                if (comp != null)
                {
                    UnityEngine.Object.DestroyImmediate(comp);
                }
            }

            UnityEngine.Object.DestroyImmediate(menuRadial);
        }

        private class SlotInfo
        {
            public string SlotName;
            public AnimationType AnimationType;
            public IAnimationProvider Provider;
            public GameObject TargetObject;
            public bool IsIllumination;
            public bool DefaultStateIsOn;

            public float GetDefaultValue()
            {
                if (AnimationType == AnimationType.Linear)
                {
                    return IsIllumination ? MRIlluminationConstants.VRCHAT_DEFAULT_VALUE : 0f;
                }
                else if (AnimationType == AnimationType.OnOff)
                {
                    return DefaultStateIsOn ? 1f : 0f;
                }
                return 0f;
            }
        }
    }

    /// <summary>
    /// Pass que aplica los valores por defecto a todos los controllers.
    /// Similar a ApplyAnimatorDefaultValuesPass de Modular Avatar.
    /// </summary>
    internal class MRApplyDefaultValuesPass : Pass<MRApplyDefaultValuesPass>
    {
        public override string DisplayName => "MR Menu Radial - Aplicar valores por defecto";

        protected override void Execute(BuildContext context)
        {
            var defaultValues = context.GetState<MRDefaultValues>();
            if (defaultValues == null || defaultValues.InitialValueOverrides.IsEmpty)
            {
                return;
            }

            var asc = context.Extension<AnimatorServicesContext>();

            // Aplicar valores por defecto a TODOS los controllers
            foreach (var controller in asc.ControllerContext.GetAllControllers())
            {
                var parameters = controller.Parameters;
                bool modified = false;

                foreach (var (name, defaultValue) in defaultValues.InitialValueOverrides)
                {
                    if (!parameters.TryGetValue(name, out var parameter)) continue;

                    switch (parameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            parameter.defaultBool = defaultValue != 0.0f;
                            break;
                        case AnimatorControllerParameterType.Int:
                            parameter.defaultInt = Mathf.RoundToInt(defaultValue);
                            break;
                        case AnimatorControllerParameterType.Float:
                            parameter.defaultFloat = defaultValue;
                            break;
                        default:
                            continue;
                    }

                    parameters = parameters.SetItem(name, parameter);
                    modified = true;
                    Debug.Log($"[MRMenuRadial NDMF] Aplicado valor por defecto '{name}' = {defaultValue}");
                }

                if (modified)
                {
                    controller.Parameters = parameters;
                }
            }
        }
    }
}
#endif
