using System.Collections.Generic;

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Interfaz base para controladores de referencias en frames.
    /// Define operaciones comunes para gestión de cualquier tipo de referencia.
    /// </summary>
    /// <typeparam name="TReference">Tipo de referencia (ObjectReference, MaterialReference, etc.)</typeparam>
    /// <typeparam name="TState">Tipo de estado capturado</typeparam>
    public interface IReferenceController<TReference, TState>
    {
        #region Propiedades de Conteo

        /// <summary>
        /// Número total de referencias
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Número de referencias válidas
        /// </summary>
        int ValidCount { get; }

        /// <summary>
        /// Número de referencias inválidas
        /// </summary>
        int InvalidCount { get; }

        /// <summary>
        /// Lista de referencias (solo lectura para consulta)
        /// </summary>
        List<TReference> References { get; }

        #endregion

        #region Operaciones CRUD

        /// <summary>
        /// Limpia todas las referencias
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Elimina referencias inválidas
        /// </summary>
        void RemoveInvalidReferences();

        #endregion

        #region Operaciones de Estado

        /// <summary>
        /// Aplica los estados de las referencias en la escena
        /// </summary>
        void ApplyStates();

        /// <summary>
        /// Captura los estados actuales de las referencias
        /// </summary>
        /// <returns>Lista de estados capturados</returns>
        List<TState> CaptureCurrentStates();

        /// <summary>
        /// Restaura estados previamente capturados
        /// </summary>
        /// <param name="savedStates">Estados a restaurar</param>
        void RestoreStates(List<TState> savedStates);

        #endregion
    }

    /// <summary>
    /// Interfaz específica para controlador de objetos
    /// </summary>
    public interface IObjectReferenceController : IReferenceController<Core.Common.ObjectReference, Core.Common.ObjectReference>
    {
        /// <summary>
        /// Añade un objeto con estado de activación
        /// </summary>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        bool AddObject(UnityEngine.GameObject gameObject, bool isActive = true);

        /// <summary>
        /// Elimina un objeto
        /// </summary>
        void RemoveObject(UnityEngine.GameObject gameObject);

        /// <summary>
        /// Selecciona todos los objetos (marca como activos)
        /// </summary>
        void SelectAllObjects();

        /// <summary>
        /// Deselecciona todos los objetos (marca como inactivos)
        /// </summary>
        void DeselectAllObjects();

        /// <summary>
        /// Recalcula rutas jerárquicas
        /// </summary>
        void RecalculateAllPaths();

        /// <summary>
        /// Busca un objeto en las referencias
        /// </summary>
        Core.Common.ObjectReference FindObjectReference(UnityEngine.GameObject gameObject);

        /// <summary>
        /// Verifica si contiene un objeto
        /// </summary>
        bool ContainsObject(UnityEngine.GameObject gameObject);
    }

    /// <summary>
    /// Interfaz específica para controlador de materiales
    /// </summary>
    public interface IMaterialReferenceController : IReferenceController<Core.Common.MaterialReference, Core.Common.MaterialReference>
    {
        /// <summary>
        /// Añade una referencia de material
        /// </summary>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        bool AddMaterial(UnityEngine.Renderer renderer, int materialIndex = 0, UnityEngine.Material alternativeMaterial = null);

        /// <summary>
        /// Elimina una referencia de material
        /// </summary>
        void RemoveMaterial(UnityEngine.Renderer renderer, int materialIndex = 0);

        /// <summary>
        /// Actualiza los materiales originales
        /// </summary>
        void UpdateAllOriginalMaterials();

        /// <summary>
        /// Actualiza las rutas de los renderers
        /// </summary>
        void UpdateAllMaterialRendererPaths();

        /// <summary>
        /// Busca una referencia de material
        /// </summary>
        Core.Common.MaterialReference FindMaterialReference(UnityEngine.Renderer renderer, int materialIndex = 0);

        /// <summary>
        /// Verifica si contiene un material
        /// </summary>
        bool ContainsMaterial(UnityEngine.Renderer renderer, int materialIndex = 0);
    }

    /// <summary>
    /// Interfaz específica para controlador de blendshapes
    /// </summary>
    public interface IBlendshapeReferenceController : IReferenceController<Core.Common.BlendshapeReference, Core.Common.BlendshapeReference>
    {
        /// <summary>
        /// Añade una referencia de blendshape
        /// </summary>
        /// <returns>true si se añadió correctamente, false si falló</returns>
        bool AddBlendshape(UnityEngine.SkinnedMeshRenderer renderer, string blendshapeName, float value = 0f);

        /// <summary>
        /// Elimina una referencia de blendshape
        /// </summary>
        void RemoveBlendshape(UnityEngine.SkinnedMeshRenderer renderer, string blendshapeName);

        /// <summary>
        /// Elimina todos los blendshapes de un renderer
        /// </summary>
        void RemoveAllBlendshapesFromRenderer(UnityEngine.SkinnedMeshRenderer renderer);

        /// <summary>
        /// Actualiza rutas de los renderers
        /// </summary>
        void UpdateAllBlendshapeRendererPaths();

        /// <summary>
        /// Captura valores actuales de blendshapes
        /// </summary>
        void CaptureAllBlendshapeValues();

        /// <summary>
        /// Busca una referencia de blendshape
        /// </summary>
        Core.Common.BlendshapeReference FindBlendshapeReference(UnityEngine.SkinnedMeshRenderer renderer, string blendshapeName);

        /// <summary>
        /// Verifica si contiene un blendshape
        /// </summary>
        bool ContainsBlendshape(UnityEngine.SkinnedMeshRenderer renderer, string blendshapeName);

        /// <summary>
        /// Obtiene blendshapes por renderer
        /// </summary>
        List<Core.Common.BlendshapeReference> GetBlendshapesByRenderer(UnityEngine.SkinnedMeshRenderer renderer);
    }
}
