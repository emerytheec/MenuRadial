using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;

namespace Bender_Dios.MenuRadial.Components.MenuRadial
{
    /// <summary>
    /// Generador automático de estructura de menú basado en las ropas detectadas.
    /// Crea MRMenuControl → MRUnificarObjetos → MRAgruparObjetos para cada ropa y el avatar.
    /// </summary>
    public class AutoMenuGenerator
    {
        #region Campos privados

        private readonly MRMenuRadial _menuRadial;
        private readonly MRCoserRopa _coserRopa;
        private readonly GameObject _avatarRoot;

        #endregion

        #region Constructor

        /// <summary>
        /// Crea una instancia del generador automático de menú
        /// </summary>
        /// <param name="menuRadial">Componente MRMenuRadial padre</param>
        public AutoMenuGenerator(MRMenuRadial menuRadial)
        {
            _menuRadial = menuRadial;
            _coserRopa = menuRadial?.CoserRopa;
            _avatarRoot = menuRadial?.AvatarRoot;
        }

        #endregion

        #region API Pública

        /// <summary>
        /// Resultado de la generación automática
        /// </summary>
        public class GenerationResult
        {
            public bool Success;
            public string Message;
            public Component MenuControl;
            public MRUnificarObjetos UnificarObjetos;
            public MRIluminacionRadial IluminacionRadial;
            public List<MRAgruparObjetos> CreatedFrames;
            public int ClothingFramesCreated;
            public int AvatarMeshesIncluded;
            public int AvatarMeshesExcluded;
        }

        /// <summary>
        /// Genera la estructura automática de menú basada en las ropas detectadas
        /// </summary>
        /// <returns>Resultado de la generación</returns>
        public GenerationResult Generate()
        {
            var result = new GenerationResult
            {
                Success = false,
                CreatedFrames = new List<MRAgruparObjetos>()
            };

            // Validaciones
            if (!ValidatePrerequisites(result))
            {
                return result;
            }

            // Obtener o crear MRMenuControl
            var menuControl = GetOrCreateMenuControl();
            if (menuControl == null)
            {
                result.Message = "No se pudo obtener o crear MRMenuControl";
                return result;
            }
            result.MenuControl = menuControl;

            // Crear MRUnificarObjetos
            var unificarObjetos = CreateUnificarObjetos(menuControl);
            if (unificarObjetos == null)
            {
                result.Message = "No se pudo crear MRUnificarObjetos";
                return result;
            }
            result.UnificarObjetos = unificarObjetos;

            // Crear MRIluminacionRadial
            var iluminacionRadial = CreateIluminacionRadial(menuControl);
            if (iluminacionRadial != null)
            {
                result.IluminacionRadial = iluminacionRadial;
            }

            // Crear frame para el avatar PRIMERO (solo accesorios, sin body/head/hair)
            var avatarFrame = CreateFrameForAvatar(unificarObjetos, out int included, out int excluded);
            if (avatarFrame != null)
            {
                result.CreatedFrames.Add(avatarFrame);
            }
            result.AvatarMeshesIncluded = included;
            result.AvatarMeshesExcluded = excluded;

            // Crear frames para cada ropa detectada (si hay CoserRopa y ropas)
            if (_coserRopa != null && _coserRopa.DetectedClothings != null)
            {
                foreach (var clothing in _coserRopa.DetectedClothings)
                {
                    if (!clothing.IsValid)
                        continue;

                    var frame = CreateFrameForClothing(unificarObjetos, clothing);
                    if (frame != null)
                    {
                        result.CreatedFrames.Add(frame);
                        result.ClothingFramesCreated++;
                    }
                }
            }

            // Resultado exitoso
            result.Success = true;
            result.Message = $"Generación exitosa: {result.ClothingFramesCreated} ropas, " +
                           $"{result.AvatarMeshesIncluded} meshes de avatar incluidos, " +
                           $"{result.AvatarMeshesExcluded} excluidos, " +
                           $"{result.CreatedFrames.Count} frames totales";

            return result;
        }

        /// <summary>
        /// Verifica si ya existe una estructura generada (cualquier MRUnificarObjetos o slot con targetObject)
        /// </summary>
        public bool HasExistingStructure()
        {
            if (_menuRadial == null)
                return false;

            var menuControl = FindMenuControlInChildren();
            if (menuControl == null)
                return false;

            // Verificar si existe CUALQUIER MRUnificarObjetos como hijo del MenuControl
            var unificarComponents = menuControl.GetComponentsInChildren<MRUnificarObjetos>(true);
            if (unificarComponents != null && unificarComponents.Length > 0)
                return true;

            // Verificar si existe CUALQUIER MRIluminacionRadial como hijo del MenuControl
            var iluminacionComponents = menuControl.GetComponentsInChildren<MRIluminacionRadial>(true);
            if (iluminacionComponents != null && iluminacionComponents.Length > 0)
                return true;

            // También verificar si hay slots con targetObject asignado en MRMenuControl
            // Esto cubre el caso donde el usuario creó componentes manualmente
            var animationSlotsProperty = menuControl.GetType().GetProperty("AnimationSlots");
            if (animationSlotsProperty != null)
            {
                var slots = animationSlotsProperty.GetValue(menuControl) as System.Collections.IList;
                if (slots != null && slots.Count > 0)
                {
                    foreach (var slot in slots)
                    {
                        var targetObjectField = slot.GetType().GetField("targetObject");
                        if (targetObjectField != null)
                        {
                            var targetObject = targetObjectField.GetValue(slot) as GameObject;
                            if (targetObject != null)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Métodos privados

        /// <summary>
        /// Valida los prerrequisitos para la generación
        /// </summary>
        private bool ValidatePrerequisites(GenerationResult result)
        {
            if (_menuRadial == null)
            {
                result.Message = "MRMenuRadial es null";
                return false;
            }

            if (_avatarRoot == null)
            {
                result.Message = "No hay avatar asignado";
                return false;
            }

            // MRCoserRopa y ropas son opcionales - si no existen,
            // igual generamos la estructura con el frame del Avatar

            return true;
        }

        /// <summary>
        /// Obtiene el MRMenuControl existente o crea uno nuevo
        /// </summary>
        private Component GetOrCreateMenuControl()
        {
            // Buscar existente
            var existing = FindMenuControlInChildren();
            if (existing != null)
                return existing;

            // Buscar por nombre "Menu Control"
            var menuControlTransform = _menuRadial.transform.Find("Menu Control");
            if (menuControlTransform != null)
            {
                existing = FindMenuControlComponent(menuControlTransform.gameObject);
                if (existing != null)
                    return existing;

                // Añadir componente si no existe
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(menuControlTransform.gameObject, "Add MRMenuControl");
#endif
                return AddMenuControlComponent(menuControlTransform.gameObject);
            }

            // Crear nuevo GameObject con MRMenuControl
#if UNITY_EDITOR
            var newGO = new GameObject("Menu Control");
            UnityEditor.Undo.RegisterCreatedObjectUndo(newGO, "Create Menu Control");
#else
            var newGO = new GameObject("Menu Control");
#endif
            newGO.transform.SetParent(_menuRadial.transform);
            newGO.transform.localPosition = Vector3.zero;
            newGO.transform.localRotation = Quaternion.identity;
            newGO.transform.localScale = Vector3.one;

            return AddMenuControlComponent(newGO);
        }

        /// <summary>
        /// Crea un MRUnificarObjetos como hijo del MenuControl
        /// </summary>
        private MRUnificarObjetos CreateUnificarObjetos(Component menuControl)
        {
            string componentName = "Outfits";

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(menuControl, "Create UnificarObjetos");
            var componentObject = new GameObject(componentName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Create UnificarObjetos");
#else
            var componentObject = new GameObject(componentName);
#endif

            componentObject.transform.SetParent(menuControl.transform);
            componentObject.transform.localPosition = Vector3.zero;
            componentObject.transform.localRotation = Quaternion.identity;
            componentObject.transform.localScale = Vector3.one;

            var unificarObjetos = componentObject.AddComponent<MRUnificarObjetos>();

            // Añadir al slot del MenuControl
            AddToMenuControlSlot(menuControl, componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(menuControl);
            UnityEditor.EditorUtility.SetDirty(unificarObjetos);
#endif

            return unificarObjetos;
        }

        /// <summary>
        /// Crea un MRIluminacionRadial como hijo del MenuControl
        /// </summary>
        private MRIluminacionRadial CreateIluminacionRadial(Component menuControl)
        {
            string componentName = "Iluminacion";

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(menuControl, "Create IluminacionRadial");
            var componentObject = new GameObject(componentName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Create IluminacionRadial");
#else
            var componentObject = new GameObject(componentName);
#endif

            componentObject.transform.SetParent(menuControl.transform);
            componentObject.transform.localPosition = Vector3.zero;
            componentObject.transform.localRotation = Quaternion.identity;
            componentObject.transform.localScale = Vector3.one;

            var iluminacionRadial = componentObject.AddComponent<MRIluminacionRadial>();

            // Asignar el avatar como RootObject del componente
            if (_avatarRoot != null)
            {
                iluminacionRadial.RootObject = _avatarRoot;
            }

            // Añadir al slot del MenuControl
            AddToMenuControlSlot(menuControl, componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(menuControl);
            UnityEditor.EditorUtility.SetDirty(iluminacionRadial);
#endif

            return iluminacionRadial;
        }

        /// <summary>
        /// Añade un componente al slot del MenuControl via reflexión.
        /// Primero busca un slot vacío existente, si no hay crea uno nuevo.
        /// </summary>
        private void AddToMenuControlSlot(Component menuControl, GameObject targetObject, string slotName)
        {
            if (menuControl == null)
                return;

            var type = menuControl.GetType();

            // Obtener AnimationSlots via reflexión
            var slotsProperty = type.GetProperty("AnimationSlots");
            if (slotsProperty == null)
                return;

            var slots = slotsProperty.GetValue(menuControl) as System.Collections.IList;
            if (slots == null)
                return;

            // Obtener tipo del slot
            var slotType = type.Assembly.GetType("Bender_Dios.MenuRadial.Components.Menu.MRAnimationSlot");
            if (slotType == null)
                return;

            var slotNameField = slotType.GetField("slotName");
            var targetObjectField = slotType.GetField("targetObject");

            // Buscar primer slot vacío (targetObject == null)
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var existingTarget = targetObjectField?.GetValue(slot) as GameObject;
                if (existingTarget == null)
                {
                    // Usar el slot vacío existente
                    if (slotNameField != null) slotNameField.SetValue(slot, slotName);
                    if (targetObjectField != null) targetObjectField.SetValue(slot, targetObject);
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(menuControl);
#endif
                    return;
                }
            }

            // Si no hay slot vacío, crear uno nuevo si hay espacio
            var maxSlotsField = type.GetField("MAX_SLOTS", BindingFlags.Public | BindingFlags.Static);
            int maxSlots = maxSlotsField != null ? (int)maxSlotsField.GetValue(null) : 8;

            if (slots.Count >= maxSlots)
                return;

            var newSlot = System.Activator.CreateInstance(slotType);

            if (slotNameField != null) slotNameField.SetValue(newSlot, slotName);
            if (targetObjectField != null) targetObjectField.SetValue(newSlot, targetObject);

            slots.Add(newSlot);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(menuControl);
#endif
        }

        /// <summary>
        /// Crea un MRAgruparObjetos para una ropa
        /// </summary>
        private MRAgruparObjetos CreateFrameForClothing(MRUnificarObjetos unificarObjetos, ClothingEntry clothing)
        {
            if (clothing?.GameObject == null || clothing.ArmatureReference == null)
                return null;

            // Encontrar el armature de la ropa
            Transform armature = clothing.ArmatureReference.ArmatureRoot;
            if (armature == null)
            {
                armature = BodyMeshDetector.FindArmature(clothing.GameObject.transform);
            }

            if (armature == null)
                return null;

            // Obtener meshes hermanos del armature
            var meshes = BodyMeshDetector.GetAllSiblingMeshes(armature);
            if (meshes.Count == 0)
                return null;

            // Crear el MRAgruparObjetos
            var frame = CreateAgruparObjetos(unificarObjetos, clothing.Name);
            if (frame == null)
                return null;

            // Añadir cada mesh al frame con su estado actual
            foreach (var mesh in meshes)
            {
                bool isActive = mesh.gameObject.activeSelf;
                frame.AddGameObject(mesh.gameObject, isActive);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(frame);
#endif

            return frame;
        }

        /// <summary>
        /// Crea un MRAgruparObjetos para el avatar (solo accesorios)
        /// </summary>
        private MRAgruparObjetos CreateFrameForAvatar(
            MRUnificarObjetos unificarObjetos,
            out int includedCount,
            out int excludedCount)
        {
            includedCount = 0;
            excludedCount = 0;

            if (_avatarRoot == null)
                return null;

            // Obtener el animator del avatar
            var animator = _avatarRoot.GetComponent<Animator>();

            // Encontrar el armature del avatar
            Transform avatarArmature = null;
            if (animator != null)
            {
                // Intentar obtener desde Hips
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null)
                {
                    avatarArmature = hips.parent;
                }
            }

            if (avatarArmature == null)
            {
                avatarArmature = BodyMeshDetector.FindArmature(_avatarRoot.transform);
            }

            if (avatarArmature == null)
                return null;

            // Analizar meshes del avatar
            var results = BodyMeshDetector.AnalyzeMeshes(avatarArmature, animator);

            var includedMeshes = results.Where(r => !r.ShouldExclude && r.Mesh != null).ToList();
            var excludedMeshes = results.Where(r => r.ShouldExclude && r.Mesh != null).ToList();

            includedCount = includedMeshes.Count;
            excludedCount = excludedMeshes.Count;

            // Si no hay meshes para incluir, no crear frame
            if (includedCount == 0)
                return null;

            // Crear el MRAgruparObjetos para el avatar
            var frame = CreateAgruparObjetos(unificarObjetos, "Avatar");
            if (frame == null)
                return null;

            // Añadir meshes incluidos
            foreach (var included in includedMeshes)
            {
                bool isActive = included.Mesh.gameObject.activeSelf;
                frame.AddGameObject(included.Mesh.gameObject, isActive);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(frame);
#endif

            return frame;
        }

        /// <summary>
        /// Crea un MRAgruparObjetos como hijo del MRUnificarObjetos
        /// </summary>
        private MRAgruparObjetos CreateAgruparObjetos(MRUnificarObjetos unificarObjetos, string frameName)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(unificarObjetos, "Create AgruparObjetos");
            var frameGO = new GameObject(frameName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(frameGO, "Create AgruparObjetos");
#else
            var frameGO = new GameObject(frameName);
#endif

            frameGO.transform.SetParent(unificarObjetos.transform);
            frameGO.transform.localPosition = Vector3.zero;
            frameGO.transform.localRotation = Quaternion.identity;
            frameGO.transform.localScale = Vector3.one;

            var agruparObjetos = frameGO.AddComponent<MRAgruparObjetos>();

            // Añadir al MRUnificarObjetos
            unificarObjetos.AddFrame(agruparObjetos);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(unificarObjetos);
            UnityEditor.EditorUtility.SetDirty(agruparObjetos);
#endif

            return agruparObjetos;
        }

        #endregion

        #region Métodos de Reflexión para MRMenuControl

        /// <summary>
        /// Busca el componente MRMenuControl en los hijos usando reflexión
        /// </summary>
        private Component FindMenuControlInChildren()
        {
            if (_menuRadial == null) return null;

            var children = _menuRadial.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                if (child != null && child.GetType().Name == "MRMenuControl")
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Busca el componente MRMenuControl en un GameObject específico
        /// </summary>
        private Component FindMenuControlComponent(GameObject go)
        {
            if (go == null) return null;

            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType().Name == "MRMenuControl")
                {
                    return comp;
                }
            }
            return null;
        }

        /// <summary>
        /// Añade el componente MRMenuControl a un GameObject via reflexión
        /// </summary>
        private Component AddMenuControlComponent(GameObject go)
        {
            if (go == null) return null;

            // Buscar el tipo MRMenuControl en todos los assemblies
            System.Type menuControlType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                menuControlType = assembly.GetType("Bender_Dios.MenuRadial.Components.Menu.MRMenuControl");
                if (menuControlType != null)
                    break;
            }

            if (menuControlType == null)
            {
                Debug.LogError("[AutoMenuGenerator] No se encontró el tipo MRMenuControl");
                return null;
            }

            return go.AddComponent(menuControlType);
        }

        /// <summary>
        /// Obtiene el SlotCount de MRMenuControl via reflexión
        /// </summary>
        private int GetSlotCount(Component menuControl)
        {
            if (menuControl == null) return 0;

            var property = menuControl.GetType().GetProperty("SlotCount");
            if (property != null)
            {
                return (int)property.GetValue(menuControl);
            }

            return 0;
        }

        #endregion
    }
}
