using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Bender_Dios.MenuRadial.Components.CoserRopa;
using Bender_Dios.MenuRadial.Components.CoserRopa.Models;
using Bender_Dios.MenuRadial.Components.Frame;
using Bender_Dios.MenuRadial.Components.Radial;
using Bender_Dios.MenuRadial.Components.Illumination;
using Bender_Dios.MenuRadial.Components.UnifyMaterial;
using Bender_Dios.MenuRadial.Components.AlternativeMaterial;
using Bender_Dios.MenuRadial.Core.Utils;

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
            public int PieceFramesCreated;
            public int AvatarMeshesIncluded;
            public int AvatarMeshesExcluded;

            // Material system fields
            public MRUnificarMateriales UnificarMateriales;
            public List<MRAgruparMateriales> CreatedMaterialFrames;
            public int MaterialSlotsDetected;

            // Wig material system fields
            public MRUnificarMateriales UnificarMaterialesPelucas;
            public List<MRAgruparMateriales> CreatedWigMaterialFrames;
            public int WigMaterialSlotsDetected;

            // Wig system fields
            public MRUnificarObjetos UnificarPelucas;
            public List<MRAgruparObjetos> CreatedWigFrames;
            public int WigFramesCreated;
        }

        /// <summary>
        /// Resultado de la sincronización incremental
        /// </summary>
        public class SyncResult
        {
            public bool Success;
            public string Message;
            public int FramesAdded;
            public int MaterialFramesAdded;
            public int OrphanedFrames;
            public List<string> AddedPieceNames = new List<string>();
            public List<string> OrphanedFrameNames = new List<string>();

            // Wig sync fields
            public int WigFramesAdded;
            public int WigMaterialFramesAdded;
            public List<string> AddedWigNames = new List<string>();
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
                CreatedFrames = new List<MRAgruparObjetos>(),
                CreatedMaterialFrames = new List<MRAgruparMateriales>(),
                CreatedWigMaterialFrames = new List<MRAgruparMateriales>(),
                CreatedWigFrames = new List<MRAgruparObjetos>()
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

            // Detectar pelucas ANTES de crear estructura
            var detectedPieces = new List<PieceEntry>();
            if (_coserRopa?.DetectedPieces != null)
            {
                foreach (var c in _coserRopa.DetectedPieces)
                {
                    if (c.IsValid)
                        detectedPieces.Add(c);
                }
            }

            var wigCandidates = WigDetector.DetectWigs(_avatarRoot, detectedPieces);

            // Clasificar pelucas: completas (se saltan en ropa) vs híbridas (ropa + pelo)
            // Para híbridas, separar meshes de pelo y crear frame de ropa sin ellos
            var wigPieceIndices = new HashSet<int>();
            var hybridWigMeshes = new Dictionary<int, HashSet<SkinnedMeshRenderer>>();

            foreach (var wig in wigCandidates)
            {
                if (wig.PieceEntryIndex >= 0)
                {
                    // Verificar si es híbrido (tiene meshes de ropa además de pelo)
                    var piece = detectedPieces[wig.PieceEntryIndex];
                    Transform armature = piece.ArmatureReference?.ArmatureRoot;
                    if (armature != null)
                    {
                        var allMeshes = BodyMeshDetector.GetAllSiblingMeshes(armature);
                        var wigMeshSet = new HashSet<SkinnedMeshRenderer>(wig.Meshes);
                        bool hasNonWigMeshes = false;
                        foreach (var m in allMeshes)
                        {
                            if (!wigMeshSet.Contains(m))
                            {
                                hasNonWigMeshes = true;
                                break;
                            }
                        }

                        if (hasNonWigMeshes && wig.Meshes.Count < allMeshes.Count)
                        {
                            // Híbrido: no saltar la ropa, pero excluir meshes de pelo
                            hybridWigMeshes[wig.PieceEntryIndex] = wigMeshSet;
                        }
                        else
                        {
                            // Peluca completa: saltar en ropa
                            wigPieceIndices.Add(wig.PieceEntryIndex);
                        }
                    }
                    else
                    {
                        wigPieceIndices.Add(wig.PieceEntryIndex);
                    }
                }
            }

            // Crear MRUnificarObjetos para Outfits
            var unificarObjetos = CreateUnificarObjetos(menuControl, "Outfits");
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

            // Crear frames para cada ropa detectada
            if (_coserRopa != null && _coserRopa.DetectedPieces != null)
            {
                for (int i = 0; i < _coserRopa.DetectedPieces.Count; i++)
                {
                    var piece = _coserRopa.DetectedPieces[i];
                    if (!piece.IsValid)
                        continue;

                    // Saltar si fue reclasificado como peluca completa
                    if (wigPieceIndices.Contains(i))
                        continue;

                    // Para híbridos, excluir meshes de pelo del frame de ropa
                    HashSet<SkinnedMeshRenderer> excludeMeshes = null;
                    if (hybridWigMeshes.ContainsKey(i))
                        excludeMeshes = hybridWigMeshes[i];

                    var frame = CreateFrameForPiece(unificarObjetos, piece, excludeMeshes);
                    if (frame != null)
                    {
                        result.CreatedFrames.Add(frame);
                        result.PieceFramesCreated++;
                    }
                }
            }

            // Crear radial "Pelucas" si hay pelucas detectadas
            if (wigCandidates.Count > 0)
            {
                var unificarPelucas = CreateUnificarObjetos(menuControl, "Pelucas");
                if (unificarPelucas != null)
                {
                    result.UnificarPelucas = unificarPelucas;

                    foreach (var wig in wigCandidates)
                    {
                        var wigFrame = CreateFrameForWig(unificarPelucas, wig);
                        if (wigFrame != null)
                        {
                            result.CreatedWigFrames.Add(wigFrame);
                            result.WigFramesCreated++;
                        }
                    }
                }
            }

            // Crear MRUnificarMateriales para outfits (excluyendo pelucas)
            if (_coserRopa != null && _coserRopa.DetectedPieces != null)
            {
                bool hasNonWigPiece = false;
                for (int i = 0; i < _coserRopa.DetectedPieces.Count; i++)
                {
                    if (_coserRopa.DetectedPieces[i].IsValid && !wigPieceIndices.Contains(i))
                    {
                        hasNonWigPiece = true;
                        break;
                    }
                }

                if (hasNonWigPiece)
                {
                    var unificarMateriales = CreateUnificarMateriales(menuControl, "Materiales Outfits");
                    if (unificarMateriales != null)
                    {
                        result.UnificarMateriales = unificarMateriales;

                        for (int i = 0; i < _coserRopa.DetectedPieces.Count; i++)
                        {
                            var piece = _coserRopa.DetectedPieces[i];
                            if (!piece.IsValid)
                                continue;

                            // Excluir pelucas del sistema de materiales outfits
                            if (wigPieceIndices.Contains(i))
                                continue;

                            var materialFrame = CreateMaterialFrameForPiece(unificarMateriales, piece);
                            if (materialFrame != null)
                            {
                                result.CreatedMaterialFrames.Add(materialFrame);
                                result.MaterialSlotsDetected += materialFrame.SlotCount;
                            }
                        }
                    }
                }
            }

            // Crear MRUnificarMateriales para pelucas
            if (wigCandidates.Count > 0)
            {
                var unificarMaterialesPelucas = CreateUnificarMateriales(menuControl, "Materiales Pelucas");
                if (unificarMaterialesPelucas != null)
                {
                    result.UnificarMaterialesPelucas = unificarMaterialesPelucas;

                    foreach (var wig in wigCandidates)
                    {
                        if (wig.PieceEntryIndex >= 0)
                        {
                            var piece = detectedPieces[wig.PieceEntryIndex];
                            if (piece.IsValid)
                            {
                                var materialFrame = CreateMaterialFrameForPiece(unificarMaterialesPelucas, piece);
                                if (materialFrame != null)
                                {
                                    result.CreatedWigMaterialFrames.Add(materialFrame);
                                    result.WigMaterialSlotsDetected += materialFrame.SlotCount;
                                }
                            }
                        }
                    }
                }
            }

            // Resultado exitoso
            result.Success = true;
            result.Message = $"Generación exitosa: {result.PieceFramesCreated} ropas, " +
                           $"{result.WigFramesCreated} pelucas, " +
                           $"{result.AvatarMeshesIncluded} meshes de avatar incluidos, " +
                           $"{result.AvatarMeshesExcluded} excluidos, " +
                           $"{result.CreatedFrames.Count} frames totales, " +
                           $"{result.CreatedMaterialFrames?.Count ?? 0} frames de materiales outfits, " +
                           $"{result.MaterialSlotsDetected} slots de material outfits, " +
                           $"{result.CreatedWigMaterialFrames?.Count ?? 0} frames de materiales pelucas, " +
                           $"{result.WigMaterialSlotsDetected} slots de material pelucas";

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

            // Verificar si existe CUALQUIER MRUnificarMateriales como hijo del MenuControl
            var unificarMaterialesComponents = menuControl.GetComponentsInChildren<MRUnificarMateriales>(true);
            if (unificarMaterialesComponents != null && unificarMaterialesComponents.Length > 0)
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

        /// <summary>
        /// Sincroniza la estructura existente con las ropas detectadas.
        /// Agrega frames/materiales para ropas nuevas sin tocar lo existente.
        /// Reporta frames huérfanos (sin ropa asociada) pero no los elimina.
        /// </summary>
        public SyncResult SyncStructure()
        {
            var result = new SyncResult { Success = false };

            // Validaciones básicas
            if (_menuRadial == null || _avatarRoot == null)
            {
                result.Message = "No hay avatar asignado";
                return result;
            }

            var menuControl = FindMenuControlInChildren();
            if (menuControl == null)
            {
                result.Message = "No se encontró MRMenuControl";
                return result;
            }

            // Buscar MRUnificarObjetos existentes por nombre
            var unificarComponents = menuControl.GetComponentsInChildren<MRUnificarObjetos>(true);
            if (unificarComponents == null || unificarComponents.Length == 0)
            {
                result.Message = "No se encontró MRUnificarObjetos";
                return result;
            }

            // Identificar radiales de Outfits y Pelucas por nombre
            MRUnificarObjetos unificarOutfits = null;
            MRUnificarObjetos unificarPelucas = null;
            foreach (var uo in unificarComponents)
            {
                if (uo.gameObject.name == "Pelucas")
                    unificarPelucas = uo;
                else if (unificarOutfits == null)
                    unificarOutfits = uo; // Primer no-Pelucas es Outfits
            }

            if (unificarOutfits == null)
            {
                result.Message = "No se encontró MRUnificarObjetos de Outfits";
                return result;
            }

            // Buscar MRUnificarMateriales existentes (puede no existir)
            var unificarMaterialesComponents = _menuRadial.GetComponentsInChildren<MRUnificarMateriales>(true);
            MRUnificarMateriales unificarMateriales = null;
            MRUnificarMateriales unificarMaterialesPelucas = null;
            if (unificarMaterialesComponents != null)
            {
                foreach (var um in unificarMaterialesComponents)
                {
                    if (um.gameObject.name == "Materiales Pelucas")
                        unificarMaterialesPelucas = um;
                    else if (unificarMateriales == null)
                        unificarMateriales = um;
                }
            }

            // Re-detectar ropas para asegurar lista actualizada
            if (_coserRopa != null)
            {
                _coserRopa.DetectPiecesInAvatar();
            }

            // Obtener ropas detectadas
            var detectedPieces = new List<PieceEntry>();
            if (_coserRopa?.DetectedPieces != null)
            {
                foreach (var c in _coserRopa.DetectedPieces)
                {
                    if (c.IsValid)
                        detectedPieces.Add(c);
                }
            }

            // Detectar pelucas entre las ropas detectadas
            var wigCandidates = WigDetector.DetectWigs(_avatarRoot, detectedPieces);

            // Clasificar pelucas: completas vs híbridas
            var wigPieceIndices = new HashSet<int>();
            var hybridWigMeshes = new Dictionary<int, HashSet<SkinnedMeshRenderer>>();

            foreach (var wig in wigCandidates)
            {
                if (wig.PieceEntryIndex >= 0)
                {
                    var piece = detectedPieces[wig.PieceEntryIndex];
                    Transform armature = piece.ArmatureReference?.ArmatureRoot;
                    if (armature != null)
                    {
                        var allMeshes = BodyMeshDetector.GetAllSiblingMeshes(armature);
                        var wigMeshSet = new HashSet<SkinnedMeshRenderer>(wig.Meshes);
                        bool hasNonWigMeshes = false;
                        foreach (var m in allMeshes)
                        {
                            if (!wigMeshSet.Contains(m))
                            {
                                hasNonWigMeshes = true;
                                break;
                            }
                        }

                        if (hasNonWigMeshes && wig.Meshes.Count < allMeshes.Count)
                            hybridWigMeshes[wig.PieceEntryIndex] = wigMeshSet;
                        else
                            wigPieceIndices.Add(wig.PieceEntryIndex);
                    }
                    else
                    {
                        wigPieceIndices.Add(wig.PieceEntryIndex);
                    }
                }
            }

            // Separar outfits de pelucas completas (los híbridos se incluyen como outfits)
            var outfitPieces = new List<PieceEntry>();
            var outfitPieceOriginalIndices = new List<int>();
            for (int i = 0; i < detectedPieces.Count; i++)
            {
                if (!wigPieceIndices.Contains(i))
                {
                    outfitPieces.Add(detectedPieces[i]);
                    outfitPieceOriginalIndices.Add(i);
                }
            }

            // --- SYNC OUTFITS ---
            var existingOutfitFrameNames = new HashSet<string>();
            var existingOutfitFrames = unificarOutfits.FrameObjects;
            if (existingOutfitFrames != null)
            {
                foreach (var frame in existingOutfitFrames)
                {
                    if (frame != null && frame.gameObject.name != "Avatar")
                        existingOutfitFrameNames.Add(frame.gameObject.name);
                }
            }

            // Crear set de nombres de outfits detectados
            var detectedOutfitNames = new HashSet<string>();
            foreach (var piece in outfitPieces)
                detectedOutfitNames.Add(piece.Name);

            // DIFF: Detectar outfits nuevos
            var newOutfitPieces = new List<PieceEntry>();
            var newOutfitOriginalIndices = new List<int>();
            for (int j = 0; j < outfitPieces.Count; j++)
            {
                if (!existingOutfitFrameNames.Contains(outfitPieces[j].Name))
                {
                    newOutfitPieces.Add(outfitPieces[j]);
                    newOutfitOriginalIndices.Add(outfitPieceOriginalIndices[j]);
                }
            }

            // DIFF: Detectar frames huérfanos en Outfits
            if (existingOutfitFrames != null)
            {
                foreach (var frame in existingOutfitFrames)
                {
                    if (frame != null && frame.gameObject.name != "Avatar")
                    {
                        if (!detectedOutfitNames.Contains(frame.gameObject.name))
                        {
                            result.OrphanedFrames++;
                            result.OrphanedFrameNames.Add(frame.gameObject.name);
                        }
                    }
                }
            }

            // Crear frames para outfits nuevos
            for (int j = 0; j < newOutfitPieces.Count; j++)
            {
                var piece = newOutfitPieces[j];
                int origIdx = newOutfitOriginalIndices[j];

                // Para híbridos, excluir meshes de pelo del frame de ropa
                HashSet<SkinnedMeshRenderer> excludeMeshes = null;
                if (hybridWigMeshes.ContainsKey(origIdx))
                    excludeMeshes = hybridWigMeshes[origIdx];

                var frame = CreateFrameForPiece(unificarOutfits, piece, excludeMeshes);
                if (frame != null)
                {
                    result.FramesAdded++;
                    result.AddedPieceNames.Add(piece.Name);
                }
            }

            // --- SYNC PELUCAS ---
            if (wigCandidates.Count > 0)
            {
                // Crear radial "Pelucas" si no existe
                if (unificarPelucas == null)
                {
                    unificarPelucas = CreateUnificarObjetos(menuControl, "Pelucas");
                }

                if (unificarPelucas != null)
                {
                    // Nombres de frames existentes en Pelucas
                    var existingWigFrameNames = new HashSet<string>();
                    var existingWigFrames = unificarPelucas.FrameObjects;
                    if (existingWigFrames != null)
                    {
                        foreach (var frame in existingWigFrames)
                        {
                            if (frame != null)
                                existingWigFrameNames.Add(frame.gameObject.name);
                        }
                    }

                    // Agregar pelucas nuevas
                    foreach (var wig in wigCandidates)
                    {
                        if (!existingWigFrameNames.Contains(wig.Name))
                        {
                            var wigFrame = CreateFrameForWig(unificarPelucas, wig);
                            if (wigFrame != null)
                            {
                                result.WigFramesAdded++;
                                result.AddedWigNames.Add(wig.Name);
                            }
                        }
                    }
                }
            }

            // --- SYNC MATERIALES OUTFITS (excluyendo pelucas) ---
            // Obtener SourceGameObjects de material groups existentes
            var existingMaterialSources = new HashSet<GameObject>();
            if (unificarMateriales?.AlternativeMaterials != null)
            {
                foreach (var matGroup in unificarMateriales.AlternativeMaterials)
                {
                    if (matGroup != null && matGroup.SourceGameObject != null)
                        existingMaterialSources.Add(matGroup.SourceGameObject);
                }
            }

            if (unificarMateriales != null)
            {
                foreach (var piece in newOutfitPieces)
                {
                    if (piece.GameObject != null && !existingMaterialSources.Contains(piece.GameObject))
                    {
                        var materialFrame = CreateMaterialFrameForPiece(unificarMateriales, piece);
                        if (materialFrame != null)
                        {
                            result.MaterialFramesAdded++;
                        }
                    }
                }
            }
            else if (newOutfitPieces.Count > 0)
            {
                // Crear MRUnificarMateriales si no existe y hay ropas nuevas
                unificarMateriales = CreateUnificarMateriales(menuControl, "Materiales Outfits");
                if (unificarMateriales != null)
                {
                    foreach (var piece in newOutfitPieces)
                    {
                        var materialFrame = CreateMaterialFrameForPiece(unificarMateriales, piece);
                        if (materialFrame != null)
                        {
                            result.MaterialFramesAdded++;
                        }
                    }
                }
            }

            // --- SYNC MATERIALES PELUCAS ---
            if (wigCandidates.Count > 0)
            {
                if (unificarMaterialesPelucas == null)
                    unificarMaterialesPelucas = CreateUnificarMateriales(menuControl, "Materiales Pelucas");

                if (unificarMaterialesPelucas != null)
                {
                    var existingWigMaterialSources = new HashSet<GameObject>();
                    if (unificarMaterialesPelucas.AlternativeMaterials != null)
                    {
                        foreach (var matGroup in unificarMaterialesPelucas.AlternativeMaterials)
                        {
                            if (matGroup != null && matGroup.SourceGameObject != null)
                                existingWigMaterialSources.Add(matGroup.SourceGameObject);
                        }
                    }

                    foreach (var wig in wigCandidates)
                    {
                        if (wig.PieceEntryIndex >= 0)
                        {
                            var piece = detectedPieces[wig.PieceEntryIndex];
                            if (piece.IsValid && piece.GameObject != null
                                && !existingWigMaterialSources.Contains(piece.GameObject))
                            {
                                var materialFrame = CreateMaterialFrameForPiece(unificarMaterialesPelucas, piece);
                                if (materialFrame != null)
                                    result.WigMaterialFramesAdded++;
                            }
                        }
                    }
                }
            }

            result.Success = true;
            int totalAdded = result.FramesAdded + result.WigFramesAdded + result.MaterialFramesAdded + result.WigMaterialFramesAdded;
            result.Message = totalAdded > 0
                ? $"Sincronización exitosa: {result.FramesAdded} ropas, {result.WigFramesAdded} pelucas, " +
                  $"{result.MaterialFramesAdded} materiales outfits, {result.WigMaterialFramesAdded} materiales pelucas agregados"
                : "La estructura ya está sincronizada";

            return result;
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
        /// Obtiene el MRMenuControl existente o crea uno nuevo.
        /// Esta es la UNICA ruta de creación de Menu Control desde Runtime.
        /// RecreateChildComponents (Editor) NO crea Menu Control — delega aquí.
        /// </summary>
        private Component GetOrCreateMenuControl()
        {
            // Buscar existente por componente
            var existing = FindMenuControlInChildren();
            if (existing != null)
                return existing;

            // Buscar por nombre "Menu Control" — si el GameObject existe, reutilizarlo
            var menuControlTransform = _menuRadial.transform.Find("Menu Control");
            if (menuControlTransform != null)
            {
                existing = FindMenuControlComponent(menuControlTransform.gameObject);
                if (existing != null)
                    return existing;

                // Verificar que el tipo esté disponible antes de intentar añadir
                if (FindMenuControlType() == null)
                    return null;

#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(menuControlTransform.gameObject, "Add MRMenuControl");
#endif
                return AddMenuControlComponent(menuControlTransform.gameObject);
            }

            // Verificar que el tipo esté disponible ANTES de crear el GameObject
            if (FindMenuControlType() == null)
                return null;

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
        /// Busca el tipo MRMenuControl en los assemblies cargados.
        /// </summary>
        private System.Type FindMenuControlType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Bender_Dios.MenuRadial.Components.Menu.MRMenuControl");
                if (type != null)
                    return type;
            }
            return null;
        }

        /// <summary>
        /// Crea un MRUnificarObjetos como hijo del MenuControl
        /// </summary>
        private MRUnificarObjetos CreateUnificarObjetos(Component menuControl, string componentName = "Outfits")
        {

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
            unificarObjetos.AnimationName = componentName;

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
        /// Crea un MRUnificarMateriales como hijo del MenuControl
        /// </summary>
        private MRUnificarMateriales CreateUnificarMateriales(Component menuControl, string componentName = "Materiales Outfits")
        {

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(menuControl, "Create UnificarMateriales");
            var componentObject = new GameObject(componentName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(componentObject, "Create UnificarMateriales");
#else
            var componentObject = new GameObject(componentName);
#endif

            componentObject.transform.SetParent(menuControl.transform);
            componentObject.transform.localPosition = Vector3.zero;
            componentObject.transform.localRotation = Quaternion.identity;
            componentObject.transform.localScale = Vector3.one;

            var unificarMateriales = componentObject.AddComponent<MRUnificarMateriales>();

            // Añadir al slot del MenuControl
            AddToMenuControlSlot(menuControl, componentObject, componentName);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(menuControl);
            UnityEditor.EditorUtility.SetDirty(unificarMateriales);
#endif

            return unificarMateriales;
        }

        /// <summary>
        /// Crea un MRAgruparMateriales como hijo del MRUnificarMateriales
        /// </summary>
        private MRAgruparMateriales CreateAgruparMateriales(
            MRUnificarMateriales unificarMateriales,
            string frameName,
            GameObject sourceObject)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(unificarMateriales, "Create AgruparMateriales");
            var frameGO = new GameObject(frameName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(frameGO, "Create AgruparMateriales");
#else
            var frameGO = new GameObject(frameName);
#endif

            frameGO.transform.SetParent(unificarMateriales.transform);
            frameGO.transform.localPosition = Vector3.zero;
            frameGO.transform.localRotation = Quaternion.identity;
            frameGO.transform.localScale = Vector3.one;

            var agruparMateriales = frameGO.AddComponent<MRAgruparMateriales>();

            // Guardar referencia al GameObject fuente para re-escaneo
            agruparMateriales.SourceGameObject = sourceObject;
            agruparMateriales.ComponentName = frameName;

            // Escanear materiales del GameObject fuente
            agruparMateriales.ScanGameObject(sourceObject, includeChildren: true);

            // Añadir al MRUnificarMateriales
            unificarMateriales.AddAlternativeMaterial(agruparMateriales);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(unificarMateriales);
            UnityEditor.EditorUtility.SetDirty(agruparMateriales);
#endif

            return agruparMateriales;
        }

        /// <summary>
        /// Crea un MRAgruparMateriales para una ropa específica
        /// </summary>
        private MRAgruparMateriales CreateMaterialFrameForPiece(
            MRUnificarMateriales unificarMateriales,
            PieceEntry piece)
        {
            if (piece?.GameObject == null)
                return null;

            // Crear el MRAgruparMateriales con el nombre de la ropa
            var frame = CreateAgruparMateriales(unificarMateriales, piece.Name, piece.GameObject);

            return frame;
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
        /// Crea un MRAgruparObjetos para una ropa.
        /// Opcionalmente excluye meshes específicos (ej: meshes de pelo que irán al radial Pelucas).
        /// </summary>
        /// <param name="unificarObjetos">Radial donde agregar el frame.</param>
        /// <param name="piece">Ropa detectada.</param>
        /// <param name="excludeMeshes">Meshes a excluir del frame (puede ser null).</param>
        private MRAgruparObjetos CreateFrameForPiece(
            MRUnificarObjetos unificarObjetos,
            PieceEntry piece,
            HashSet<SkinnedMeshRenderer> excludeMeshes = null)
        {
            if (piece?.GameObject == null || piece.ArmatureReference == null)
                return null;

            // Encontrar el armature de la ropa
            Transform armature = piece.ArmatureReference.ArmatureRoot;
            if (armature == null)
            {
                // Usar busqueda avanzada con ArmatureFinder
                var findResult = BodyMeshDetector.FindArmatureDetailed(piece.GameObject.transform);
                armature = findResult.Armature;

                if (armature != null)
                {
                    Debug.Log($"[AutoMenuGenerator] Armature encontrado para '{piece.Name}': " +
                             $"'{armature.name}' via {findResult.Method} ({findResult.HumanoidBoneCount} huesos humanoid). " +
                             $"{findResult.Details}");
                }
            }

            if (armature == null)
            {
                Debug.LogWarning($"[AutoMenuGenerator] No se pudo encontrar armature para ropa '{piece.Name}'. " +
                                "Verifique que la ropa tenga un armature con huesos humanoid.");
                return null;
            }

            // Obtener meshes hermanos del armature
            var meshes = BodyMeshDetector.GetAllSiblingMeshes(armature);
            if (meshes.Count == 0)
                return null;

            // Crear el MRAgruparObjetos
            var frame = CreateAgruparObjetos(unificarObjetos, piece.Name);
            if (frame == null)
                return null;

            // Añadir cada mesh al frame con IsActive = true, excluyendo los meshes de pelo si aplica
            foreach (var mesh in meshes)
            {
                if (excludeMeshes != null && excludeMeshes.Contains(mesh))
                    continue;

                frame.AddGameObject(mesh.gameObject, isActive: true);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(frame);
#endif

            return frame;
        }

        /// <summary>
        /// Crea un MRAgruparObjetos para una peluca
        /// </summary>
        private MRAgruparObjetos CreateFrameForWig(MRUnificarObjetos unificarObjetos, WigDetector.WigCandidate wig)
        {
            if (wig.Root == null || wig.Meshes == null || wig.Meshes.Count == 0)
                return null;

            // Crear el MRAgruparObjetos
            var frame = CreateAgruparObjetos(unificarObjetos, wig.Name);
            if (frame == null)
                return null;

            // Añadir cada mesh al frame con IsActive = true
            foreach (var mesh in wig.Meshes)
            {
                if (mesh != null)
                    frame.AddGameObject(mesh.gameObject, isActive: true);
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

            // Añadir meshes incluidos con IsActive = true
            // Los meshes se agregan siempre como activos porque representan el estado "visible" del avatar
            foreach (var included in includedMeshes)
            {
                frame.AddGameObject(included.Mesh.gameObject, isActive: true);
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

            var menuControlType = FindMenuControlType();

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
