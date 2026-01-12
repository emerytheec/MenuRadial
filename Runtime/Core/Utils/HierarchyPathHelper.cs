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
        
        /// <summary>
        /// Busca un objeto por su ruta jerárquica (útil para recuperación de referencias perdidas)
        /// </summary>
        /// <param name="hierarchyPath">Ruta jerárquica completa</param>
        /// <param name="root">Transform raíz donde buscar (opcional)</param>
        /// <returns>GameObject encontrado o null</returns>
        public static GameObject FindGameObjectByPath(string hierarchyPath, Transform root = null)
        {
            if (!IsValidPath(hierarchyPath))
                return null;
            
            // Si no hay raíz especificada, buscar en toda la escena
            if (root == null)
            {
                return GameObject.Find(hierarchyPath);
            }
            
            // Buscar relativo a la raíz
            Transform found = root.Find(hierarchyPath);
            return found != null ? found.gameObject : null;
        }
        
        /// <summary>
        /// Obtiene el nombre del objeto desde una ruta (último elemento)
        /// </summary>
        /// <param name="hierarchyPath">Ruta jerárquica</param>
        /// <returns>Nombre del objeto</returns>
        public static string GetObjectNameFromPath(string hierarchyPath)
        {
            if (!IsValidPath(hierarchyPath))
                return "[Unknown]";
            
            int lastSlash = hierarchyPath.LastIndexOf('/');
            return lastSlash >= 0 ? hierarchyPath.Substring(lastSlash + 1) : hierarchyPath;
        }
        
        /// <summary>
        /// Obtiene la ruta del padre desde una ruta jerárquica
        /// </summary>
        /// <param name="hierarchyPath">Ruta jerárquica completa</param>
        /// <returns>Ruta del padre</returns>
        public static string GetParentPathFromPath(string hierarchyPath)
        {
            if (!IsValidPath(hierarchyPath))
                return "";
            
            int lastSlash = hierarchyPath.LastIndexOf('/');
            return lastSlash > 0 ? hierarchyPath.Substring(0, lastSlash) : "";
        }
        
        /// <summary>
        /// Compara dos rutas jerárquicas ignorando diferencias menores
        /// </summary>
        /// <param name="path1">Primera ruta</param>
        /// <param name="path2">Segunda ruta</param>
        /// <returns>True si las rutas son equivalentes</returns>
        public static bool ArePathsEquivalent(string path1, string path2)
        {
            if (path1 == path2) return true;
            
            if (!IsValidPath(path1) || !IsValidPath(path2))
                return false;
            
            // Normalizar rutas (quitar espacios, etc.)
            var normalized1 = path1.Trim().Replace("\\", "/");
            var normalized2 = path2.Trim().Replace("\\", "/");
            
            return normalized1.Equals(normalized2, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
