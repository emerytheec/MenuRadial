using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;
using Bender_Dios.MenuRadial.Components.Radial;

#if UNITY_EDITOR
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Generador de VRCExpressionParameters para VRChat.
    /// Responsabilidad: Crear el asset de parámetros de expresiones.
    /// </summary>
    public class MRParametersGenerator
    {
        private readonly MRVRChatConfig _config;
        private readonly string _outputDirectory;

        public MRParametersGenerator(MRVRChatConfig config, string outputDirectory)
        {
            _config = config;
            _outputDirectory = outputDirectory;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Crea o actualiza VRCExpressionParameters
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <returns>VRCExpressionParameters creado o null si hay error</returns>
        public VRCExpressionParameters Generate(List<MRSlotInfo> slotInfoList)
        {
            string path = $"{_outputDirectory}/{_config.GetPrefixedFileName(_config.ParametersFileName)}";

            // Intentar cargar asset existente para editar in-place (mantener GUID)
            var parameters = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(path);
            bool isNewAsset = (parameters == null);

            if (isNewAsset)
            {
                parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            }

            // Construir lista de parámetros recursivamente
            var paramList = new List<VRCExpressionParameters.Parameter>();
            CollectParametersRecursively(slotInfoList, paramList);

            // Actualizar el array de parámetros
            parameters.parameters = paramList.ToArray();

            if (isNewAsset)
            {
                AssetDatabase.CreateAsset(parameters, path);
            }

            EditorUtility.SetDirty(parameters);
            return parameters;
        }

        /// <summary>
        /// Recopila parámetros recursivamente incluyendo submenús
        /// </summary>
        private void CollectParametersRecursively(List<MRSlotInfo> slotInfoList, List<VRCExpressionParameters.Parameter> paramList)
        {
            foreach (var slotInfo in slotInfoList)
            {
                // SubMenu: procesar recursivamente
                if (slotInfo.AnimationType == AnimationType.SubMenu)
                {
                    if (slotInfo.ChildSlotInfos != null)
                    {
                        CollectParametersRecursively(slotInfo.ChildSlotInfos, paramList);
                    }
                    continue;
                }

                if (slotInfo.AnimationType == AnimationType.None)
                    continue;

                // Evitar duplicados
                if (paramList.Any(p => p.name == slotInfo.Slot.slotName))
                    continue;

                var param = CreateParameter(slotInfo);
                if (param != null)
                {
                    paramList.Add(param);
                }
            }
        }

        /// <summary>
        /// Crea un parámetro VRChat para un slot
        /// </summary>
        private VRCExpressionParameters.Parameter CreateParameter(MRSlotInfo slotInfo)
        {
            var param = new VRCExpressionParameters.Parameter
            {
                name = slotInfo.Slot.slotName,
                saved = true,
                networkSynced = true
            };

            switch (slotInfo.AnimationType)
            {
                case AnimationType.OnOff:
                    param.valueType = VRCExpressionParameters.ValueType.Bool;
                    bool defaultIsOn = false;
                    if (slotInfo.AnimationProvider is MRUnificarObjetos radialMenu)
                    {
                        defaultIsOn = radialMenu.DefaultStateIsOn;
                    }
                    param.defaultValue = defaultIsOn ? 1f : 0f;
                    break;

                case AnimationType.AB:
                    param.valueType = VRCExpressionParameters.ValueType.Bool;
                    param.defaultValue = 0f;
                    break;

                case AnimationType.Linear:
                    param.valueType = VRCExpressionParameters.ValueType.Float;
                    param.defaultValue = slotInfo.IsIllumination ? MRIlluminationConstants.VRCHAT_DEFAULT_VALUE : 0f;
                    break;

                default:
                    return null;
            }

            return param;
        }

        /// <summary>
        /// Calcula el costo en bits de los parámetros
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <returns>Costo total en bits</returns>
        public int CalculateBitCost(List<MRSlotInfo> slotInfoList)
        {
            int totalBits = 0;
            var processedNames = new HashSet<string>();

            CalculateBitCostRecursively(slotInfoList, processedNames, ref totalBits);

            return totalBits;
        }

        /// <summary>
        /// Calcula el costo en bits recursivamente
        /// </summary>
        private void CalculateBitCostRecursively(List<MRSlotInfo> slotInfoList, HashSet<string> processedNames, ref int totalBits)
        {
            foreach (var slotInfo in slotInfoList)
            {
                if (slotInfo.AnimationType == AnimationType.SubMenu)
                {
                    if (slotInfo.ChildSlotInfos != null)
                    {
                        CalculateBitCostRecursively(slotInfo.ChildSlotInfos, processedNames, ref totalBits);
                    }
                    continue;
                }

                if (slotInfo.AnimationType == AnimationType.None)
                    continue;

                if (processedNames.Contains(slotInfo.Slot.slotName))
                    continue;

                processedNames.Add(slotInfo.Slot.slotName);

                switch (slotInfo.AnimationType)
                {
                    case AnimationType.OnOff:
                    case AnimationType.AB:
                        totalBits += 1; // Bool = 1 bit
                        break;

                    case AnimationType.Linear:
                        totalBits += 8; // Float = 8 bits
                        break;
                }
            }
        }
#endif
    }
}
