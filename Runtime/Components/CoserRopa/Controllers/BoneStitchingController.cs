using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa.Interfaces;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Validation.Models;

namespace Bender_Dios.MenuRadial.Components.CoserRopa.Controllers
{
    /// <summary>
    /// Controlador responsable del proceso de cosido/fusion de huesos.
    ///
    /// Dos modos de operacion:
    /// - Stitch (Coser): Reparenta huesos de ropa bajo huesos del avatar (duplicados)
    /// - Merge (Fusionar): Actualiza SkinnedMeshRenderer para usar huesos del avatar (sin duplicados)
    ///
    /// Incluye recálculo de bind poses para preservar deformación correcta de meshes.
    /// </summary>
    public class BoneStitchingController : IStitchingController
    {
        /// <summary>
        /// Pila de operaciones para deshacer
        /// </summary>
        private readonly List<(Transform bone, Transform originalParent, int siblingIndex)> _undoStack
            = new List<(Transform, Transform, int)>();

        /// <summary>
        /// Retargeter de meshes para recálculo de bind poses
        /// </summary>
        private readonly MeshRetargeter _meshRetargeter = new MeshRetargeter();

        /// <summary>
        /// Detector de PhysBones para preservar cadenas de física
        /// </summary>
        private readonly PhysBoneDetector _physBoneDetector = new PhysBoneDetector();

        /// <summary>
        /// Ejecuta el proceso de cosido (modo Stitch - reparentar huesos)
        /// </summary>
        public StitchingResult ExecuteStitching(List<BoneMapping> mappings)
        {
            return ExecuteStitching(mappings, StitchingMode.Stitch, null);
        }

        /// <summary>
        /// Ejecuta el proceso de cosido o fusion segun el modo especificado
        /// </summary>
        public StitchingResult ExecuteStitching(List<BoneMapping> mappings, StitchingMode mode, GameObject clothingRoot)
        {
            var result = new StitchingResult { Success = true };
            _undoStack.Clear();

            if (mappings == null || mappings.Count == 0)
            {
                return StitchingResult.CreateFailure("No hay mapeos de huesos para coser");
            }

            // Filtrar solo mapeos validos (ambos huesos existen)
            var validMappings = mappings.Where(m => m.IsValid).ToList();

            if (validMappings.Count == 0)
            {
                return StitchingResult.CreateFailure("No hay mapeos validos (ambos huesos deben existir)");
            }

#if UNITY_EDITOR
            // Verificar si la ropa es un prefab y desempaquetarlo
            var firstClothingBone = validMappings.First().ClothingBone;
            if (firstClothingBone != null)
            {
                // Buscar la raiz mas externa del prefab
                GameObject prefabRoot = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(firstClothingBone.gameObject);
                if (prefabRoot != null && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(prefabRoot))
                {
                    Debug.Log($"[BoneStitchingController] La ropa es un prefab. Desempaquetando '{prefabRoot.name}'...");
                    UnityEditor.Undo.RegisterFullObjectHierarchyUndo(prefabRoot, "Desempaquetar prefab de ropa");
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(prefabRoot, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.UserAction);
                    Debug.Log($"[BoneStitchingController] Prefab desempaquetado correctamente.");
                }
            }
#endif

            // Ejecutar segun el modo
            if (mode == StitchingMode.Merge)
            {
                return ExecuteMerge(validMappings, clothingRoot, result);
            }
            else
            {
                return ExecuteStitch(validMappings, result);
            }
        }

        /// <summary>
        /// Modo STITCH: Reparenta huesos de ropa bajo huesos del avatar
        /// </summary>
        private StitchingResult ExecuteStitch(List<BoneMapping> validMappings, StitchingResult result)
        {
            // Ordenar por profundidad de jerarquia (padres primero)
            var sortedMappings = SortByHierarchyDepth(validMappings);

            Debug.Log($"[BoneStitchingController] Modo COSER: Reparentando {sortedMappings.Count} huesos...");

            foreach (var mapping in sortedMappings)
            {
                // Verificar que el hueso de la ropa no sea ya hijo del hueso del avatar
                if (IsAlreadyStitched(mapping))
                {
                    result.BonesSkipped++;
                    mapping.WasStitched = false;
                    result.AddWarning($"{mapping.BoneType}: Ya esta cosido");
                    continue;
                }

                // Ejecutar cosido
                StitchSingleBone(mapping, result);
            }

            // Contar huesos no-humanoid que se preservaron
            result.NonHumanoidBonesPreserved = CountNonHumanoidChildren(sortedMappings);

            Debug.Log($"[BoneStitchingController] Cosido completado: {result.GetSummary()}");

            return result;
        }

        /// <summary>
        /// Modo MERGE: Actualiza SkinnedMeshRenderers para usar huesos del avatar directamente
        /// Solo fusiona huesos HUMANOID. Los huesos no-humanoid y PhysBones se mueven pero no se fusionan.
        /// Similar a Modular Avatar Merge Armature
        /// </summary>
        private StitchingResult ExecuteMerge(List<BoneMapping> validMappings, GameObject clothingRoot, StitchingResult result)
        {
            if (clothingRoot == null)
            {
                return StitchingResult.CreateFailure("Se requiere clothingRoot para modo Merge");
            }

            Debug.Log($"[BoneStitchingController] Modo FUSIONAR: Actualizando SkinnedMeshRenderers...");

            // PASO 0: Detectar cadenas de PhysBones que deben preservarse
            var physBoneChains = _physBoneDetector.DetectPhysBoneChains(clothingRoot.transform);
            var physBoneInfo = _physBoneDetector.GetPhysBoneInfo(clothingRoot.transform);

            if (physBoneChains.Count > 0)
            {
                Debug.Log($"[BoneStitchingController] {physBoneInfo}");
            }

            // Crear diccionario de mapeo: hueso ropa -> hueso avatar (SOLO humanoid)
            // EXCLUIR huesos que son parte de cadenas PhysBone
            var boneMap = new Dictionary<Transform, Transform>();
            int excludedPhysBones = 0;

            foreach (var mapping in validMappings)
            {
                if (mapping.ClothingBone != null && mapping.AvatarBone != null)
                {
                    // NO mapear huesos que son parte de cadenas PhysBone
                    if (physBoneChains.Contains(mapping.ClothingBone))
                    {
                        Debug.Log($"[BoneStitchingController] Hueso '{mapping.ClothingBone.name}' excluido del mapeo (PhysBone)");
                        excludedPhysBones++;
                        continue;
                    }

                    boneMap[mapping.ClothingBone] = mapping.AvatarBone;
                }
            }

            // Encontrar todos los SkinnedMeshRenderers en la ropa
            var skinnedMeshRenderers = clothingRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (skinnedMeshRenderers.Length == 0)
            {
                return StitchingResult.CreateFailure("No se encontraron SkinnedMeshRenderers en la ropa");
            }

            Debug.Log($"[BoneStitchingController] Encontrados {skinnedMeshRenderers.Length} SkinnedMeshRenderers");

            // PASO 1: Identificar TODOS los huesos no-humanoid usados por los meshes
            // Incluye huesos no mapeados Y huesos PhysBone
            var nonHumanoidBonesInUse = new HashSet<Transform>();
            foreach (var smr in skinnedMeshRenderers)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone != null && !boneMap.ContainsKey(bone))
                    {
                        // Este hueso NO esta en el mapeo humanoid = es no-humanoid o PhysBone
                        nonHumanoidBonesInUse.Add(bone);
                    }
                }
            }

            // Agregar todas las cadenas PhysBone (aunque no esten directamente en SMR bones)
            foreach (var physBone in physBoneChains)
            {
                nonHumanoidBonesInUse.Add(physBone);
            }

            Debug.Log($"[BoneStitchingController] Huesos a preservar: {nonHumanoidBonesInUse.Count} (incluye {physBoneChains.Count} PhysBones)");

            // PASO 2: Mover huesos no-humanoid ANTES de modificar el SMR
            MoveNonHumanoidBonesToAvatar(validMappings, boneMap, nonHumanoidBonesInUse);

            // PASO 3: Usar MeshRetargeter para actualizar SMRs con recálculo de bind poses
            Debug.Log($"[BoneStitchingController] Retargeteando meshes con recálculo de bind poses...");

            // Obtener estadísticas antes
            var stats = _meshRetargeter.GetRetargetingStats(clothingRoot, boneMap);
            Debug.Log($"[BoneStitchingController] {stats}");

            // Retargetear todos los meshes
            int totalRetargeted = _meshRetargeter.RetargetMeshes(clothingRoot, boneMap);

            result.BonesStitched = stats.TotalBonestoRetarget;
            result.BonesMerged = stats.TotalBonestoRetarget;
            result.NonHumanoidBonesPreserved = nonHumanoidBonesInUse.Count;
            result.PhysBonesPreserved = physBoneChains.Count;

            // PASO 4: Limpiar huesos no usados y armature
            CleanupArmature(clothingRoot, nonHumanoidBonesInUse);

            Debug.Log($"[BoneStitchingController] Fusion completada: {totalRetargeted} SMRs retargeteados, {nonHumanoidBonesInUse.Count} huesos preservados ({physBoneChains.Count} PhysBones)");

            return result;
        }

        /// <summary>
        /// Mueve huesos no-humanoid bajo sus padres humanoid correspondientes en el avatar
        /// </summary>
        private void MoveNonHumanoidBonesToAvatar(
            List<BoneMapping> validMappings,
            Dictionary<Transform, Transform> boneMap,
            HashSet<Transform> nonHumanoidBonesInUse)
        {
            // Para cada hueso no-humanoid, encontrar su padre humanoid mas cercano
            // y moverlo bajo el equivalente en el avatar
            foreach (var nonHumanoidBone in nonHumanoidBonesInUse)
            {
                if (nonHumanoidBone == null) continue;

                // Buscar el padre humanoid mas cercano en la jerarquia de la ropa
                Transform clothingParent = nonHumanoidBone.parent;
                Transform avatarParent = null;

                while (clothingParent != null)
                {
                    if (boneMap.TryGetValue(clothingParent, out avatarParent))
                    {
                        break; // Encontramos el padre humanoid
                    }
                    clothingParent = clothingParent.parent;
                }

                if (avatarParent != null)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(nonHumanoidBone, $"Mover {nonHumanoidBone.name}");
#endif
                    nonHumanoidBone.SetParent(avatarParent, worldPositionStays: true);
                    Debug.Log($"[BoneStitchingController] Hueso no-humanoid '{nonHumanoidBone.name}' movido bajo '{avatarParent.name}'");

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(nonHumanoidBone.gameObject);
#endif
                }
                else
                {
                    Debug.LogWarning($"[BoneStitchingController] No se encontro padre humanoid para '{nonHumanoidBone.name}'");
                }
            }
        }

        /// <summary>
        /// Ejecuta fusion DESPUES de haber cosido (modo manual paso 2)
        /// Detecta huesos con sufijo "(Ropa)" y actualiza SMR para usar el padre (hueso avatar)
        /// </summary>
        public StitchingResult ExecuteMergeAfterStitch(GameObject clothingRoot)
        {
            var result = new StitchingResult { Success = true };

            if (clothingRoot == null)
            {
                return StitchingResult.CreateFailure("Se requiere clothingRoot");
            }

            Debug.Log($"[BoneStitchingController] Modo FUSIONAR DESPUES DE COSER...");

            // Encontrar todos los SkinnedMeshRenderers en la ropa
            var skinnedMeshRenderers = clothingRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (skinnedMeshRenderers.Length == 0)
            {
                return StitchingResult.CreateFailure("No se encontraron SkinnedMeshRenderers en la ropa");
            }

            // Crear mapeo: hueso cosido (Ropa) -> hueso avatar (padre)
            var stitchedToAvatarMap = new Dictionary<Transform, Transform>();
            var bonesStillInUse = new HashSet<Transform>();

            foreach (var smr in skinnedMeshRenderers)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone == null) continue;

                    // Detectar si es un hueso cosido (tiene sufijo "(Ropa)")
                    if (bone.name.EndsWith(CLOTHING_BONE_SUFFIX))
                    {
                        // El padre deberia ser el hueso del avatar
                        if (bone.parent != null && !bone.parent.name.EndsWith(CLOTHING_BONE_SUFFIX))
                        {
                            stitchedToAvatarMap[bone] = bone.parent;
                        }
                    }
                    else
                    {
                        // Hueso no cosido, mantener referencia
                        bonesStillInUse.Add(bone);
                    }
                }
            }

            if (stitchedToAvatarMap.Count == 0)
            {
                return StitchingResult.CreateFailure("No se encontraron huesos cosidos (con sufijo '(Ropa)'). Ejecuta 'Coser' primero.");
            }

            Debug.Log($"[BoneStitchingController] Encontrados {stitchedToAvatarMap.Count} huesos cosidos para fusionar");

            // Actualizar SkinnedMeshRenderers
            int totalRemapped = 0;

            foreach (var smr in skinnedMeshRenderers)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(smr, "Fusionar huesos cosidos");
#endif

                var bones = smr.bones;
                var newBones = new Transform[bones.Length];
                int remapped = 0;

                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];

                    if (bone != null && stitchedToAvatarMap.TryGetValue(bone, out Transform avatarBone))
                    {
                        newBones[i] = avatarBone;
                        remapped++;
                    }
                    else
                    {
                        newBones[i] = bone;
                        if (bone != null) bonesStillInUse.Add(bone);
                    }
                }

                smr.bones = newBones;

                // Actualizar rootBone si es un hueso cosido
                if (smr.rootBone != null && stitchedToAvatarMap.TryGetValue(smr.rootBone, out Transform newRoot))
                {
                    smr.rootBone = newRoot;
                }

                totalRemapped += remapped;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(smr);
#endif
            }

            result.BonesMerged = totalRemapped;

            // Eliminar huesos cosidos que ya no se usan
            int deletedStitched = 0;
            foreach (var kvp in stitchedToAvatarMap)
            {
                var stitchedBone = kvp.Key;
                if (stitchedBone != null && !bonesStillInUse.Contains(stitchedBone))
                {
                    // Mover hijos no-cosidos al padre antes de eliminar
                    var childrenToMove = new List<Transform>();
                    foreach (Transform child in stitchedBone)
                    {
                        if (!child.name.EndsWith(CLOTHING_BONE_SUFFIX))
                        {
                            childrenToMove.Add(child);
                        }
                    }

                    foreach (var child in childrenToMove)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RecordObject(child, "Mover hijo");
#endif
                        child.SetParent(stitchedBone.parent, worldPositionStays: true);
                    }

#if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(stitchedBone.gameObject);
#else
                    Object.DestroyImmediate(stitchedBone.gameObject);
#endif
                    deletedStitched++;
                }
            }

            Debug.Log($"[BoneStitchingController] Fusion post-cosido completada: {totalRemapped} referencias actualizadas, {deletedStitched} huesos cosidos eliminados");

            return result;
        }

        /// <summary>
        /// Verifica si hay huesos cosidos (con sufijo "(Ropa)") en la escena
        /// </summary>
        public bool HasStitchedBones(GameObject clothingRoot)
        {
            if (clothingRoot == null) return false;

            var smrs = clothingRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                foreach (var bone in smr.bones)
                {
                    if (bone != null && bone.name.EndsWith(CLOTHING_BONE_SUFFIX))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Limpia el armature: elimina huesos no usados y el armature si queda vacio
        /// </summary>
        private void CleanupArmature(GameObject clothingRoot, HashSet<Transform> bonesInUse)
        {
            Transform armature = FindArmatureRoot(clothingRoot);
            if (armature == null) return;

            // Recopilar todos los huesos en el armature
            var allBonesInArmature = new List<Transform>();
            CollectAllChildren(armature, allBonesInArmature);

            // Encontrar huesos que no estan en uso (no referenciados por ningun SMR)
            var unusedBones = new List<Transform>();
            foreach (var bone in allBonesInArmature)
            {
                if (!bonesInUse.Contains(bone))
                {
                    unusedBones.Add(bone);
                }
            }

            // Eliminar huesos no usados (de hojas hacia raiz para evitar problemas de jerarquia)
            unusedBones = unusedBones.OrderByDescending(b => GetHierarchyDepth(b)).ToList();

            int deletedCount = 0;
            foreach (var unusedBone in unusedBones)
            {
                // Solo eliminar si no tiene hijos (o todos sus hijos ya fueron eliminados)
                if (unusedBone != null && unusedBone.childCount == 0)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(unusedBone.gameObject);
#else
                    Object.DestroyImmediate(unusedBone.gameObject);
#endif
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                Debug.Log($"[BoneStitchingController] Eliminados {deletedCount} huesos no usados del armature");
            }

            // Verificar si el armature quedo vacio
            if (armature != null && armature.childCount == 0)
            {
#if UNITY_EDITOR
                Debug.Log($"[BoneStitchingController] Eliminando armature vacio '{armature.name}'");
                UnityEditor.Undo.DestroyObjectImmediate(armature.gameObject);
#else
                Object.DestroyImmediate(armature.gameObject);
#endif
            }
        }

        /// <summary>
        /// Encuentra la raiz del armature en un GameObject
        /// </summary>
        private Transform FindArmatureRoot(GameObject root)
        {
            foreach (Transform child in root.transform)
            {
                string nameLower = child.name.ToLowerInvariant();
                if (nameLower.Contains("armature") || nameLower.Contains("skeleton"))
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Recopila todos los hijos de un transform recursivamente
        /// </summary>
        private void CollectAllChildren(Transform parent, List<Transform> result)
        {
            foreach (Transform child in parent)
            {
                result.Add(child);
                CollectAllChildren(child, result);
            }
        }

        /// <summary>
        /// Sufijo para marcar huesos de ropa (para rastreo)
        /// </summary>
        private const string CLOTHING_BONE_SUFFIX = " (Ropa)";

        /// <summary>
        /// Cose un solo hueso (reparenta bajo el hueso del avatar)
        /// </summary>
        private void StitchSingleBone(BoneMapping mapping, StitchingResult result)
        {
            var clothingBone = mapping.ClothingBone;
            var avatarBone = mapping.AvatarBone;

            // Guardar estado original para undo (incluyendo nombre original)
            string originalName = clothingBone.name;
            _undoStack.Add((clothingBone, clothingBone.parent, clothingBone.GetSiblingIndex()));

#if UNITY_EDITOR
            // Registrar para Undo antes de hacer cambios
            UnityEditor.Undo.RecordObject(clothingBone.gameObject, $"Coser {mapping.BoneType}");
            UnityEditor.Undo.RecordObject(clothingBone, $"Coser {mapping.BoneType}");
#endif

            // Renombrar hueso para rastreo
            if (!clothingBone.name.EndsWith(CLOTHING_BONE_SUFFIX))
            {
                clothingBone.name = clothingBone.name + CLOTHING_BONE_SUFFIX;
            }

            // Reparentar el hueso de la ropa bajo el hueso del avatar
            // worldPositionStays=true mantiene la posicion/rotacion/escala world
            clothingBone.SetParent(avatarBone, worldPositionStays: true);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(clothingBone.gameObject);
#endif

            mapping.WasStitched = true;
            result.BonesStitched++;

            Debug.Log($"[BoneStitchingController] Cosido: {mapping.BoneType} ('{originalName}' -> bajo '{avatarBone.name}')");
        }

        /// <summary>
        /// Verifica si un mapeo ya esta cosido (hueso de ropa es hijo de hueso de avatar)
        /// </summary>
        private bool IsAlreadyStitched(BoneMapping mapping)
        {
            if (mapping.ClothingBone == null || mapping.AvatarBone == null)
                return false;

            return mapping.ClothingBone.parent == mapping.AvatarBone;
        }

        /// <summary>
        /// Ordena mapeos por profundidad de jerarquia (padres primero)
        /// Esto asegura que cosamos Hips antes que Spine, Spine antes que Chest, etc.
        /// </summary>
        private List<BoneMapping> SortByHierarchyDepth(List<BoneMapping> mappings)
        {
            return mappings
                .Where(m => m.ClothingBone != null)
                .OrderBy(m => GetHierarchyDepth(m.ClothingBone))
                .ToList();
        }

        /// <summary>
        /// Obtiene la profundidad de un transform en la jerarquia
        /// </summary>
        private int GetHierarchyDepth(Transform transform)
        {
            int depth = 0;
            var current = transform;

            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        /// <summary>
        /// Cuenta huesos no-humanoid que se preservaron como hijos
        /// </summary>
        private int CountNonHumanoidChildren(List<BoneMapping> mappings)
        {
            int count = 0;

            foreach (var mapping in mappings)
            {
                if (mapping.ClothingBone == null) continue;

                // Contar hijos directos que no son huesos humanoid
                foreach (Transform child in mapping.ClothingBone)
                {
                    // Si el hijo no esta en la lista de mapeos, es no-humanoid
                    bool isHumanoid = mappings.Any(m =>
                        m.ClothingBone == child);

                    if (!isHumanoid)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Valida si el cosido puede realizarse
        /// </summary>
        public ValidationResult ValidateForStitching(ArmatureReference avatar, ArmatureReference clothing)
        {
            var result = new ValidationResult();

            // Validar avatar
            if (avatar == null || avatar.RootObject == null)
            {
                result.AddChild(ValidationResult.Error("Avatar no asignado"));
            }
            else
            {
                if (avatar.Animator == null)
                {
                    result.AddChild(ValidationResult.Error("Avatar no tiene componente Animator"));
                }
                else if (!avatar.IsHumanoid)
                {
                    result.AddChild(ValidationResult.Warning(
                        $"Avatar '{avatar.RootObject.name}' no esta configurado como Humanoid. " +
                        "Se recomienda configurar Animation Type como Humanoid en Import Settings."));
                }
            }

            // Validar ropa
            if (clothing == null || clothing.RootObject == null)
            {
                result.AddChild(ValidationResult.Error("Ropa no asignada"));
            }
            else
            {
                if (clothing.Animator == null)
                {
                    result.AddChild(ValidationResult.Warning(
                        $"Ropa '{clothing.RootObject.name}' no tiene Animator. " +
                        "Se buscara armature por nombre de huesos."));
                }
                else if (!clothing.IsHumanoid)
                {
                    result.AddChild(ValidationResult.Warning(
                        $"Ropa '{clothing.RootObject.name}' no esta configurada como Humanoid. " +
                        "Se usara busqueda por nombre de huesos como fallback."));
                }

                if (clothing.ArmatureRoot == null)
                {
                    result.AddChild(ValidationResult.Warning(
                        $"No se encontro raiz de armature en '{clothing.RootObject.name}'."));
                }
            }

            return result;
        }

        /// <summary>
        /// Deshace la ultima operacion de cosido
        /// </summary>
        public bool UndoStitching()
        {
            if (_undoStack.Count == 0)
                return false;

#if UNITY_EDITOR
            // En editor, usar sistema de Undo de Unity
            UnityEditor.Undo.PerformUndo();
#else
            // En runtime, restaurar manualmente
            foreach (var (bone, originalParent, siblingIndex) in _undoStack.AsEnumerable().Reverse())
            {
                if (bone != null)
                {
                    bone.SetParent(originalParent);
                    bone.SetSiblingIndex(siblingIndex);
                }
            }
#endif

            _undoStack.Clear();
            Debug.Log("[BoneStitchingController] Cosido deshecho");
            return true;
        }

        /// <summary>
        /// Limpia la pila de undo
        /// </summary>
        public void ClearUndoStack()
        {
            _undoStack.Clear();
        }
    }
}
