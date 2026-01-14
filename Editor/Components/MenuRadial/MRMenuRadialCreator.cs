using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.MenuRadial;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.OrganizaPB;
using Bender_Dios.MenuRadial.Components.AjustarBounds;
using Bender_Dios.MenuRadial.Components.Menu;

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Clase estática con MenuItems para crear MRMenuRadial desde los menús de Unity.
    /// </summary>
    public static class MRMenuRadialCreator
    {
        private const string MENU_PATH_GAMEOBJECT = "GameObject/Bender Dios/MR Menu Radial";
        private const string MENU_PATH_TOOLS = "Tools/Menu Radial/MR Menu Radial";

        private const int MENU_PRIORITY_GAMEOBJECT = 10;
        private const int MENU_PRIORITY_TOOLS = 100;

        /// <summary>
        /// Crea MR Menu Radial desde el menú contextual del Hierarchy (click derecho).
        /// </summary>
        [MenuItem(MENU_PATH_GAMEOBJECT, false, MENU_PRIORITY_GAMEOBJECT)]
        public static void CreateFromHierarchyMenu(MenuCommand menuCommand)
        {
            CreateMRMenuRadial(menuCommand.context as GameObject);
        }

        /// <summary>
        /// Crea MR Menu Radial desde el menú Tools.
        /// </summary>
        [MenuItem(MENU_PATH_TOOLS, false, MENU_PRIORITY_TOOLS)]
        public static void CreateFromToolsMenu()
        {
            CreateMRMenuRadial(Selection.activeGameObject);
        }

        /// <summary>
        /// Crea el GameObject MR Menu Radial con todos sus hijos.
        /// </summary>
        /// <param name="parent">GameObject padre opcional (si se seleccionó algo en el Hierarchy)</param>
        private static void CreateMRMenuRadial(GameObject parent)
        {
            // Crear el GameObject principal
            var menuRadialGO = new GameObject("MR Menu Radial");

            // Si hay un padre seleccionado, hacer hijo de él
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(menuRadialGO, parent);
            }

            // Agregar el componente principal
            var menuRadial = menuRadialGO.AddComponent<MRMenuRadial>();

            // Crear los hijos con sus componentes (en el orden especificado)
            CreateChildWithComponent<MRCoserRopa>(menuRadialGO, "Coser Ropa");
            CreateChildWithComponent<MROrganizaPB>(menuRadialGO, "Organiza PB");
            CreateChildWithComponent<MRMenuControl>(menuRadialGO, "Menu Control");
            CreateChildWithComponent<MRAjustarBounds>(menuRadialGO, "Ajustar Bounds");

            // Registrar para Undo
            Undo.RegisterCreatedObjectUndo(menuRadialGO, "Create MR Menu Radial");

            // Seleccionar el objeto creado
            Selection.activeGameObject = menuRadialGO;

            // Invalidar cache para que detecte los nuevos hijos
            menuRadial.InvalidateCache();

            // Forzar repaint del inspector
            EditorUtility.SetDirty(menuRadial);
        }

        /// <summary>
        /// Crea un GameObject hijo con el componente especificado.
        /// </summary>
        /// <typeparam name="T">Tipo del componente a agregar</typeparam>
        /// <param name="parent">GameObject padre</param>
        /// <param name="name">Nombre del hijo</param>
        /// <returns>El componente creado</returns>
        private static T CreateChildWithComponent<T>(GameObject parent, string name) where T : Component
        {
            var childGO = new GameObject(name);
            childGO.transform.SetParent(parent.transform);
            childGO.transform.localPosition = Vector3.zero;
            childGO.transform.localRotation = Quaternion.identity;
            childGO.transform.localScale = Vector3.one;

            return childGO.AddComponent<T>();
        }
    }
}
