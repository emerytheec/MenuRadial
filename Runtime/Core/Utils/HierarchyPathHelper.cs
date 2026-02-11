using UnityEngine;

namespace Bender_Dios.MenuRadial.Core.Utils
{
    /// <summary>
    /// Utilidades comunes para gestión de rutas jerárquicas
    /// Responsabilidad única: Calcular y manejar rutas de Transform
    /// Elimina duplicación de código en ObjectReference, MaterialReference y BlendshapeReference
    /// </summary>
    public static class HierarchyPathHelper
    {
        /// <summary>
        /// Obtiene la ruta jerárquica completa de un Transform para animaciones VRChat/Unity
        /// CORREGIDO: Retorna cadena vacía para el avatar root (requerido por AnimationClip.SetCurve)
        /// </summary>
        /// <param name="transform">Transform del que obtener la ruta</param>
        /// <param name="root">Transform raíz (avatar root con Animator)</param>
        /// <returns>Ruta jerárquica válida para Unity AnimationClip</returns>
        public static string GetHierarchyPath(Transform transform, Transform root = null)
        {
            if (transform == null)
                return "[Missing Transform]";
                
            // ✅ CRÍTICO: Si el transform ES el root, retornar cadena vacía (requerido por Unity)
            if (transform == root)
                return "";
                
            // Si no hay padre o el padre es el root, solo retornar el nombre
            if (transform.parent == null)
            {
                // Si no hay root especificado, usar el nombre del transform
                return root == null ? transform.name : "";
            }
            
            // ✅ CRÍTICO: Si el padre es el root, solo retornar el nombre (sin ruta padre)
            if (transform.parent == root)
                return transform.name;
            
            // Construir ruta recursiva
            string parentPath = GetHierarchyPath(transform.parent, root);
            return string.IsNullOrEmpty(parentPath) ? transform.name : parentPath + "/" + transform.name;
        }
        
        /// <summary>
        /// Obtiene la ruta jerárquica de un GameObject
        /// </summary>
        /// <param name="gameObject">GameObject del que obtener la ruta</param>
        /// <param name="root">Transform raíz opcional</param>
        /// <returns>Ruta jerárquica como string</returns>
        public static string GetHierarchyPath(GameObject gameObject, Transform root = null)
        {
            if (gameObject == null)
                return "[Missing GameObject]";
                
            return GetHierarchyPath(gameObject.transform, root);
        }
        
        /// <summary>
        /// Obtiene la ruta jerárquica de un Component
        /// </summary>
        /// <param name="component">Component del que obtener la ruta</param>
        /// <param name="root">Transform raíz opcional</param>
        /// <returns>Ruta jerárquica como string</returns>
        public static string GetHierarchyPath(Component component, Transform root = null)
        {
            if (component == null)
                return "[Missing Component]";
                
            return GetHierarchyPath(component.transform, root);
        }
        
        /// <summary>
        /// Verifica si una ruta jerárquica es válida (no es null, vacía o indica missing)
        /// </summary>
        /// <param name="path">Ruta a verificar</param>
        /// <returns>True si la ruta es válida</returns>
        public static bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path) && 
                   !path.StartsWith("[Missing") && 
                   !path.Equals("[Missing Reference]");
        }
        
    }
}
