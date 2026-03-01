#if MR_NDMF_AVAILABLE
using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;
using Bender_Dios.MenuRadial.Components.MenuRadial;

[assembly: ExportsPlugin(typeof(Bender_Dios.MenuRadial.Editor.Components.MenuRadial.MRMeshImportFixPlugin))]

namespace Bender_Dios.MenuRadial.Editor.Components.MenuRadial
{
    /// <summary>
    /// Plugin NDMF para detectar problemas de importacion de meshes.
    /// Reporta (no corrige automaticamente):
    /// - Read/Write disabled (isReadable)
    /// - Blendshape Normals en "Calculate" (debe ser "Import/Legacy")
    ///
    /// Se ejecuta al inicio del build. Los problemas se reportan como warnings en consola
    /// para que el usuario los corrija manualmente en el ModelImporter.
    /// </summary>
    public class MRMeshImportFixPlugin : Plugin<MRMeshImportFixPlugin>
    {
        public override string QualifiedName => "bender_dios.menu_radial.mesh_import_fix";
        public override string DisplayName => "MR Mesh Import Fix";

        // Color tema: Azul
        public override Color? ThemeColor => new Color(0x21 / 255f, 0x96 / 255f, 0xF3 / 255f, 1);

        protected override void Configure()
        {
            // Ejecutar muy temprano, en la fase Resolving (antes de cualquier transformacion)
            InPhase(BuildPhase.Resolving)
                .Run(MRMeshImportFixPass.Instance);
        }

        protected override void OnUnhandledException(Exception e)
        {
            Debug.LogError($"[MRMeshImportFix NDMF] Error durante el procesamiento: {e.Message}");
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Pass que detecta y reporta problemas de importacion de meshes.
    /// </summary>
    internal class MRMeshImportFixPass : Pass<MRMeshImportFixPass>
    {
        public override string DisplayName => "Detect Mesh Import Issues";

        protected override void Execute(BuildContext context)
        {
            var avatarRoot = context.AvatarRootObject;
            if (avatarRoot == null)
                return;

            // Recolectar todos los meshes del avatar
            var meshesToFix = new Dictionary<Mesh, MeshImportIssues>();

            // Buscar en SkinnedMeshRenderers
            var skinnedRenderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                if (smr != null && smr.sharedMesh != null)
                {
                    CheckMesh(smr.sharedMesh, meshesToFix);
                }
            }

            // Buscar en MeshFilters
            var meshFilters = avatarRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.sharedMesh != null)
                {
                    CheckMesh(mf.sharedMesh, meshesToFix);
                }
            }

            // Buscar en ParticleSystemRenderers
            var particleRenderers = avatarRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var psr in particleRenderers)
            {
                if (psr != null && psr.mesh != null)
                {
                    CheckMesh(psr.mesh, meshesToFix);
                }
            }

            if (meshesToFix.Count == 0)
            {
                return; // No hay problemas que corregir
            }

            Debug.LogWarning($"[MRMeshImportFix NDMF] Encontrados {meshesToFix.Count} mesh(es) con problemas de importación que requieren corrección manual:");

            foreach (var kvp in meshesToFix)
            {
                var mesh = kvp.Key;
                var issues = kvp.Value;
                string fixes = "";

                if (issues.NeedsReadWrite)
                    fixes += "Read/Write desactivado";
                if (issues.NeedsLegacyBlendShapeNormals)
                    fixes += (fixes.Length > 0 ? ", " : "") + "BlendShape Normals en Calculate (debe ser Import)";

                Debug.LogWarning($"[MRMeshImportFix NDMF] '{mesh.name}' ({issues.AssetPath}): {fixes}");
            }
        }

        /// <summary>
        /// Verifica si un mesh tiene problemas de importacion.
        /// </summary>
        private void CheckMesh(Mesh mesh, Dictionary<Mesh, MeshImportIssues> meshesToFix)
        {
            if (mesh == null || meshesToFix.ContainsKey(mesh))
                return;

            string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(assetPath))
                return;

            // Ignorar meshes built-in o de paquetes
            if (assetPath.StartsWith("Resources/") ||
                assetPath.StartsWith("Packages/") ||
                assetPath.StartsWith("Library/") ||
                assetPath.Contains("unity_builtin"))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return;

            var issues = new MeshImportIssues
            {
                AssetPath = assetPath,
                NeedsReadWrite = !importer.isReadable,
                NeedsLegacyBlendShapeNormals = mesh.blendShapeCount > 0 &&
                    importer.importBlendShapeNormals == ModelImporterNormals.Calculate
            };

            if (issues.HasIssues)
            {
                meshesToFix[mesh] = issues;
            }
        }

    }

    /// <summary>
    /// Estructura para almacenar los problemas encontrados en un mesh.
    /// </summary>
    internal struct MeshImportIssues
    {
        public string AssetPath;
        public bool NeedsReadWrite;
        public bool NeedsLegacyBlendShapeNormals;

        public bool HasIssues => NeedsReadWrite || NeedsLegacyBlendShapeNormals;
    }
}
#endif
