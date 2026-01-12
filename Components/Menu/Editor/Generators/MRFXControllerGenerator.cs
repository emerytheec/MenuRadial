using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.Radial;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Generador de AnimatorController FX para VRChat.
    /// Responsabilidad: Crear y configurar el controller con layers, parámetros y estados.
    /// </summary>
    public class MRFXControllerGenerator
    {
        private readonly MRVRChatConfig _config;
        private readonly string _outputDirectory;

        public MRFXControllerGenerator(MRVRChatConfig config, string outputDirectory)
        {
            _config = config;
            _outputDirectory = outputDirectory;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Crea o actualiza el FX AnimatorController
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <returns>AnimatorController creado o null si hay error</returns>
        public AnimatorController Generate(List<MRSlotInfo> slotInfoList)
        {
            string path = $"{_outputDirectory}/{_config.GetPrefixedFileName(_config.FXFileName)}";

            // Intentar cargar asset existente para editar in-place (mantener GUID)
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            bool isNewAsset = (controller == null);

            if (isNewAsset)
            {
                controller = new AnimatorController();
                controller.name = "FX_Menu_Radial";
            }
            else
            {
                ClearController(controller);
            }

            // Agregar parámetros y capas para cada slot
            AddSlotsToController(controller, slotInfoList);

            if (isNewAsset)
            {
                AssetDatabase.CreateAsset(controller, path);
            }

            // Guardar sub-assets
            SaveSubAssets(controller);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// Agrega slots al controller recursivamente
        /// </summary>
        private void AddSlotsToController(AnimatorController controller, List<MRSlotInfo> slotInfoList)
        {
            foreach (var slotInfo in slotInfoList)
            {
                Debug.Log($"[MRFXControllerGenerator] Procesando slot '{slotInfo.Slot.slotName}' tipo={slotInfo.AnimationType}");

                // SubMenu: procesar recursivamente
                if (slotInfo.AnimationType == AnimationType.SubMenu)
                {
                    if (slotInfo.ChildSlotInfos != null && slotInfo.ChildSlotInfos.Count > 0)
                    {
                        AddSlotsToController(controller, slotInfo.ChildSlotInfos);
                    }
                    continue;
                }

                // Agregar parámetro
                AddParameter(controller, slotInfo);

                // Crear layer
                CreateLayer(controller, slotInfo);
            }
        }

        /// <summary>
        /// Agrega un parámetro al controller
        /// </summary>
        private void AddParameter(AnimatorController controller, MRSlotInfo slotInfo)
        {
            string paramName = slotInfo.Slot.slotName;

            // Verificar si ya existe
            if (controller.parameters.Any(p => p.name == paramName))
                return;

            AnimatorControllerParameterType paramType;
            switch (slotInfo.AnimationType)
            {
                case AnimationType.OnOff:
                case AnimationType.AB:
                    paramType = AnimatorControllerParameterType.Bool;
                    break;
                case AnimationType.Linear:
                    paramType = AnimatorControllerParameterType.Float;
                    break;
                default:
                    return;
            }

            controller.AddParameter(paramName, paramType);
        }

        /// <summary>
        /// Crea una capa para un slot
        /// </summary>
        private void CreateLayer(AnimatorController controller, MRSlotInfo slotInfo)
        {
            string layerName = slotInfo.Slot.slotName;
            string paramName = slotInfo.Slot.slotName;

            var stateMachine = new AnimatorStateMachine
            {
                name = layerName,
                hideFlags = HideFlags.HideInHierarchy
            };

            var layer = new AnimatorControllerLayer
            {
                name = layerName,
                defaultWeight = 1.0f,
                stateMachine = stateMachine
            };

            switch (slotInfo.AnimationType)
            {
                case AnimationType.OnOff:
                case AnimationType.AB:
                    CreateToggleStates(stateMachine, slotInfo, paramName);
                    break;

                case AnimationType.Linear:
                    CreateLinearState(stateMachine, slotInfo, paramName);
                    break;
            }

            var layers = controller.layers.ToList();
            layers.Add(layer);
            controller.layers = layers.ToArray();
        }

        /// <summary>
        /// Crea estados para Toggle (OnOff o AB)
        /// </summary>
        private void CreateToggleStates(AnimatorStateMachine stateMachine, MRSlotInfo slotInfo, string paramName)
        {
            if (slotInfo.AnimationClips.Count < 2)
            {
                Debug.LogWarning($"[MRFXControllerGenerator] Slot '{slotInfo.Slot.slotName}' no tiene suficientes animaciones para Toggle.");
                return;
            }

            // Estado OFF (o A)
            var stateOff = stateMachine.AddState("OFF", new Vector3(250, 0, 0));
            stateOff.motion = slotInfo.AnimationClips[1]; // _off o _B
            stateOff.writeDefaultValues = _config.writeDefaultValues;

            // Estado ON (o B)
            var stateOn = stateMachine.AddState("ON", new Vector3(250, 80, 0));
            stateOn.motion = slotInfo.AnimationClips[0]; // _on o _A
            stateOn.writeDefaultValues = _config.writeDefaultValues;

            // Determinar estado inicial según configuración
            bool defaultIsOn = false;
            if (slotInfo.AnimationType == AnimationType.OnOff && slotInfo.AnimationProvider is MRUnificarObjetos radialMenu)
            {
                defaultIsOn = radialMenu.DefaultStateIsOn;
            }

            stateMachine.defaultState = defaultIsOn ? stateOn : stateOff;

            // Transición: Any State → OFF cuando param = false
            var transitionToOff = stateMachine.AddAnyStateTransition(stateOff);
            transitionToOff.hasExitTime = false;
            transitionToOff.duration = 0f;
            transitionToOff.canTransitionToSelf = false;
            transitionToOff.AddCondition(AnimatorConditionMode.IfNot, 0, paramName);

            // Transición: Any State → ON cuando param = true
            var transitionToOn = stateMachine.AddAnyStateTransition(stateOn);
            transitionToOn.hasExitTime = false;
            transitionToOn.duration = 0f;
            transitionToOn.canTransitionToSelf = false;
            transitionToOn.AddCondition(AnimatorConditionMode.If, 0, paramName);
        }

        /// <summary>
        /// Crea estado para Linear (con Motion Time)
        /// </summary>
        private void CreateLinearState(AnimatorStateMachine stateMachine, MRSlotInfo slotInfo, string paramName)
        {
            if (slotInfo.AnimationClips.Count < 1)
            {
                Debug.LogWarning($"[MRFXControllerGenerator] Slot '{slotInfo.Slot.slotName}' no tiene animación linear.");
                return;
            }

            var state = stateMachine.AddState("Linear", new Vector3(250, 0, 0));
            state.motion = slotInfo.AnimationClips[0];
            state.writeDefaultValues = _config.writeDefaultValues;

            // Motion Time: el parámetro Float controla la posición en la animación
            state.timeParameterActive = true;
            state.timeParameter = paramName;
            state.speed = 1f;

            stateMachine.defaultState = state;
        }

        /// <summary>
        /// Limpia un AnimatorController existente
        /// </summary>
        private void ClearController(AnimatorController controller)
        {
            var subAssetsToDestroy = new List<UnityEngine.Object>();

            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine != null)
                {
                    foreach (var transition in layer.stateMachine.anyStateTransitions)
                    {
                        if (transition != null)
                            subAssetsToDestroy.Add(transition);
                    }

                    foreach (var childState in layer.stateMachine.states)
                    {
                        if (childState.state != null)
                        {
                            foreach (var stateTransition in childState.state.transitions)
                            {
                                if (stateTransition != null)
                                    subAssetsToDestroy.Add(stateTransition);
                            }
                            subAssetsToDestroy.Add(childState.state);
                        }
                    }

                    foreach (var childStateMachine in layer.stateMachine.stateMachines)
                    {
                        if (childStateMachine.stateMachine != null)
                            subAssetsToDestroy.Add(childStateMachine.stateMachine);
                    }

                    subAssetsToDestroy.Add(layer.stateMachine);
                }
            }

            foreach (var subAsset in subAssetsToDestroy)
            {
                if (subAsset != null)
                {
                    UnityEngine.Object.DestroyImmediate(subAsset, true);
                }
            }

            controller.layers = new AnimatorControllerLayer[0];

            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }
        }

        /// <summary>
        /// Guarda sub-assets del controller
        /// </summary>
        private void SaveSubAssets(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine != null)
                {
                    if (!AssetDatabase.IsSubAsset(layer.stateMachine))
                    {
                        AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
                    }

                    foreach (var state in layer.stateMachine.states)
                    {
                        if (!AssetDatabase.IsSubAsset(state.state))
                        {
                            AssetDatabase.AddObjectToAsset(state.state, controller);
                        }
                    }

                    foreach (var transition in layer.stateMachine.anyStateTransitions)
                    {
                        if (!AssetDatabase.IsSubAsset(transition))
                        {
                            AssetDatabase.AddObjectToAsset(transition, controller);
                        }
                    }
                }
            }
        }
#endif
    }
}
