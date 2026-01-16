using System.Collections.Generic;
using UnityEngine;
using Bender_Dios.MenuRadial.Core.Common;
using Bender_Dios.MenuRadial.Components.Menu;

#if UNITY_EDITOR
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace Bender_Dios.MenuRadial.Components.Menu.Generators
{
    /// <summary>
    /// Generador de VRCExpressionsMenu para VRChat.
    /// Responsabilidad: Crear el asset de menú de expresiones con controles.
    /// </summary>
    public class MRMenuGenerator
    {
        private readonly MRVRChatConfig _config;
        private readonly string _outputDirectory;

        public MRMenuGenerator(MRVRChatConfig config, string outputDirectory)
        {
            _config = config;
            _outputDirectory = outputDirectory;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Crea o actualiza VRCExpressionsMenu
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <param name="menuName">Nombre del menú</param>
        /// <returns>VRCExpressionsMenu creado o null si hay error</returns>
        public VRCExpressionsMenu Generate(List<MRSlotInfo> slotInfoList, string menuName)
        {
            string path = $"{_outputDirectory}/{_config.GetPrefixedFileName(_config.MenuFileName)}";
            return GenerateRecursive(slotInfoList, path, menuName);
        }

        /// <summary>
        /// Crea un VRCExpressionsMenu recursivamente (para soportar submenús)
        /// </summary>
        private VRCExpressionsMenu GenerateRecursive(List<MRSlotInfo> slotInfoList, string path, string menuName)
        {
            Debug.Log($"[MRMenuGenerator] Creando menú '{menuName}' en '{path}' con {slotInfoList.Count} slots");

            // Intentar cargar asset existente para editar in-place
            var menu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
            bool isNewAsset = (menu == null);

            if (isNewAsset)
            {
                Debug.Log($"[MRMenuGenerator] Creando nuevo asset de menú: {path}");
                menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                menu.name = menuName;
            }
            else
            {
                Debug.Log($"[MRMenuGenerator] Reutilizando asset de menú existente: {path}");
            }

            // Limpiar controles existentes
            menu.controls = new List<VRCExpressionsMenu.Control>();

            foreach (var slotInfo in slotInfoList)
            {
                var control = CreateControl(slotInfo);
                if (control != null)
                {
                    menu.controls.Add(control);
                }
            }

            if (isNewAsset)
            {
                AssetDatabase.CreateAsset(menu, path);
            }

            EditorUtility.SetDirty(menu);
            return menu;
        }

        /// <summary>
        /// Crea un control VRChat para un slot
        /// </summary>
        private VRCExpressionsMenu.Control CreateControl(MRSlotInfo slotInfo)
        {
            // Obtener icono
            Texture2D icon = slotInfo.Slot.iconImage;
            if (icon == null)
            {
                icon = MRIconLoader.GetIconForAnimationType(slotInfo.AnimationType);
            }

            var control = new VRCExpressionsMenu.Control
            {
                name = slotInfo.Slot.slotName,
                icon = icon
            };

            switch (slotInfo.AnimationType)
            {
                case AnimationType.OnOff:
                case AnimationType.AB:
                    control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                    control.parameter = new VRCExpressionsMenu.Control.Parameter
                    {
                        name = slotInfo.Slot.slotName
                    };
                    break;

                case AnimationType.Linear:
                    // Para RadialPuppet: parameter.name debe estar vacío, el parámetro va en subParameters
                    control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                    control.parameter = new VRCExpressionsMenu.Control.Parameter { name = "" };
                    control.subParameters = new VRCExpressionsMenu.Control.Parameter[]
                    {
                        new VRCExpressionsMenu.Control.Parameter
                        {
                            name = slotInfo.Slot.slotName
                        }
                    };
                    // Establecer valor por defecto para el RadialPuppet (0 para normal, 0.5 para iluminación)
                    control.value = slotInfo.IsIllumination ? MRIlluminationConstants.VRCHAT_DEFAULT_VALUE : 0f;
                    Debug.Log($"[MRMenuGenerator] Creando RadialPuppet para '{slotInfo.Slot.slotName}' (Linear) con valor={control.value}");
                    break;

                case AnimationType.SubMenu:
                    control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                    Debug.Log($"[MRMenuGenerator] Creando control SubMenu para '{slotInfo.Slot.slotName}'");

                    // Crear menú hijo recursivamente
                    if (slotInfo.ChildSlotInfos != null && slotInfo.ChildSlotInfos.Count > 0)
                    {
                        // Usar prefijo si está configurado
                        string subMenuFileName = _config.HasPrefix
                            ? $"{_config.OutputPrefix}_Menu_{slotInfo.Slot.slotName}.asset"
                            : $"Menu_{slotInfo.Slot.slotName}.asset";
                        string subMenuPath = $"{_outputDirectory}/{subMenuFileName}";

                        var childMenu = GenerateRecursive(slotInfo.ChildSlotInfos, subMenuPath, slotInfo.Slot.slotName);
                        control.subMenu = childMenu;

                        Debug.Log($"[MRMenuGenerator] Creado submenú: {subMenuFileName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MRMenuGenerator] SubMenu '{slotInfo.Slot.slotName}' no tiene slots hijos válidos");
                    }
                    break;

                default:
                    return null;
            }

            return control;
        }

        /// <summary>
        /// Crea un submenú en una ruta específica
        /// </summary>
        /// <param name="slotInfoList">Lista de información de slots</param>
        /// <param name="subMenuName">Nombre del submenú</param>
        /// <returns>Ruta del archivo creado</returns>
        public string CreateSubMenu(List<MRSlotInfo> slotInfoList, string subMenuName)
        {
            string fileName = $"Menu_{subMenuName}.asset";
            string path = $"{_outputDirectory}/{fileName}";

            GenerateRecursive(slotInfoList, path, subMenuName);

            return path;
        }
#endif
    }
}
