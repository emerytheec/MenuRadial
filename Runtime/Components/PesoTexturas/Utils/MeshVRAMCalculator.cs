using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bender_Dios.MenuRadial.Components.PesoTexturas
{
    /// <summary>
    /// Calculador de VRAM para Meshes y BlendShapes.
    /// Basado en la investigacion de d4rkc0d3r y la implementacion de Thry's VRAM Calculator.
    /// </summary>
    public static class MeshVRAMCalculator
    {
        #region Constants

        /// <summary>
        /// Bytes por vertice afectado en un BlendShape.
        /// Estructura: vertexIndex(4) + deltaPosition(12) + deltaNormal(12) + deltaTangent(12) = 40 bytes
        /// </summary>
        public const int BYTES_PER_BLENDSHAPE_VERTEX = 40;

        /// <summary>
        /// Bytes por indice en un index buffer de 16 bits
        /// </summary>
        public const int BYTES_PER_INDEX_16BIT = 2;

        /// <summary>
        /// Bytes por indice en un index buffer de 32 bits
        /// </summary>
        public const int BYTES_PER_INDEX_32BIT = 4;

        #endregion

        #region Vertex Attribute Sizes

        /// <summary>
        /// Diccionario con el tamano en bytes de cada tipo de dato de vertice
        /// </summary>
        private static readonly Dictionary<UnityEngine.Rendering.VertexAttributeFormat, int> VertexFormatSizes = new()
        {
            { UnityEngine.Rendering.VertexAttributeFormat.Float32, 4 },
            { UnityEngine.Rendering.VertexAttributeFormat.Float16, 2 },
            { UnityEngine.Rendering.VertexAttributeFormat.UNorm8, 1 },
            { UnityEngine.Rendering.VertexAttributeFormat.SNorm8, 1 },
            { UnityEngine.Rendering.VertexAttributeFormat.UNorm16, 2 },
            { UnityEngine.Rendering.VertexAttributeFormat.SNorm16, 2 },
            { UnityEngine.Rendering.VertexAttributeFormat.UInt8, 1 },
            { UnityEngine.Rendering.VertexAttributeFormat.SInt8, 1 },
            { UnityEngine.Rendering.VertexAttributeFormat.UInt16, 2 },
            { UnityEngine.Rendering.VertexAttributeFormat.SInt16, 2 },
            { UnityEngine.Rendering.VertexAttributeFormat.UInt32, 4 },
            { UnityEngine.Rendering.VertexAttributeFormat.SInt32, 4 },
        };

        /// <summary>
        /// Atributos que se duplican en skinned meshes
        /// </summary>
        private static readonly HashSet<UnityEngine.Rendering.VertexAttribute> SkinnedDuplicatedAttributes = new()
        {
            UnityEngine.Rendering.VertexAttribute.Position,
            UnityEngine.Rendering.VertexAttribute.Normal,
            UnityEngine.Rendering.VertexAttribute.Tangent,
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Calcula el VRAM total de un mesh (vertices + indices + blendshapes).
        /// </summary>
        /// <param name="mesh">El mesh a analizar</param>
        /// <param name="isSkinned">Si el mesh es skinned (SkinnedMeshRenderer)</param>
        /// <returns>Tamano en bytes</returns>
        public static long CalculateMeshVRAM(Mesh mesh, bool isSkinned)
        {
            if (mesh == null)
                return 0;

            long total = 0;

            // 1. Vertex attributes
            total += CalculateVertexAttributesVRAM(mesh, isSkinned);

            // 2. Index buffer
            total += CalculateIndexBufferVRAM(mesh);

            // 3. BlendShapes
            total += CalculateBlendShapesVRAM(mesh);

            return total;
        }

        /// <summary>
        /// Calcula el VRAM de los atributos de vertices.
        /// Para skinned meshes, Position, Normal y Tangent se duplican.
        /// </summary>
        public static long CalculateVertexAttributesVRAM(Mesh mesh, bool isSkinned)
        {
            if (mesh == null)
                return 0;

            long bytesPerVertex = 0;

            // Obtener los atributos del vertex buffer
            var attributes = mesh.GetVertexAttributes();

            foreach (var attr in attributes)
            {
                int formatSize = GetVertexFormatSize(attr.format);
                int dimension = attr.dimension;
                int multiplier = 1;

                // Para skinned meshes, algunos atributos se duplican
                if (isSkinned && SkinnedDuplicatedAttributes.Contains(attr.attribute))
                {
                    multiplier = 2;
                }

                bytesPerVertex += formatSize * dimension * multiplier;
            }

            return bytesPerVertex * mesh.vertexCount;
        }

        /// <summary>
        /// Calcula el VRAM del index buffer.
        /// </summary>
        public static long CalculateIndexBufferVRAM(Mesh mesh)
        {
            if (mesh == null)
                return 0;

            int indexCount = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                indexCount += (int)mesh.GetIndexCount(i);
            }

            int bytesPerIndex = mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16
                ? BYTES_PER_INDEX_16BIT
                : BYTES_PER_INDEX_32BIT;

            return indexCount * bytesPerIndex;
        }

        /// <summary>
        /// Calcula el VRAM de todos los BlendShapes.
        /// Solo cuenta vertices con delta no-cero.
        /// </summary>
        public static long CalculateBlendShapesVRAM(Mesh mesh)
        {
            if (mesh == null || mesh.blendShapeCount == 0)
                return 0;

            long totalBytes = 0;
            int vertexCount = mesh.vertexCount;

            // Arrays temporales para leer los deltas
            Vector3[] deltaVertices = new Vector3[vertexCount];
            Vector3[] deltaNormals = new Vector3[vertexCount];
            Vector3[] deltaTangents = new Vector3[vertexCount];

            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
            {
                int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    // Obtener los deltas del frame
                    mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

                    // Contar vertices con delta no-cero
                    int affectedVertices = CountNonZeroVertices(deltaVertices, deltaNormals, deltaTangents);

                    totalBytes += affectedVertices * BYTES_PER_BLENDSHAPE_VERTEX;
                }
            }

            return totalBytes;
        }

        /// <summary>
        /// Calcula el VRAM de BlendShapes de forma rapida (estimacion).
        /// Usa el total de vertices afectados sin verificar si son cero.
        /// Menos preciso pero mas rapido para meshes grandes.
        /// </summary>
        public static long CalculateBlendShapesVRAMFast(Mesh mesh)
        {
            if (mesh == null || mesh.blendShapeCount == 0)
                return 0;

            // Estimacion: asumimos que cada blendshape afecta al 30% de los vertices en promedio
            // Esto es una aproximacion conservadora
            int totalFrames = 0;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                totalFrames += mesh.GetBlendShapeFrameCount(i);
            }

            // Estimacion conservadora: 30% de vertices afectados por frame
            long estimatedAffectedVertices = (long)(mesh.vertexCount * 0.3f * totalFrames);
            return estimatedAffectedVertices * BYTES_PER_BLENDSHAPE_VERTEX;
        }

        /// <summary>
        /// Obtiene informacion detallada de un mesh.
        /// </summary>
        public static MeshVRAMInfo GetMeshInfo(Mesh mesh, bool isSkinned)
        {
            if (mesh == null)
                return new MeshVRAMInfo();

            return new MeshVRAMInfo
            {
                MeshName = mesh.name,
                VertexCount = mesh.vertexCount,
                TriangleCount = mesh.triangles.Length / 3,
                BlendShapeCount = mesh.blendShapeCount,
                SubMeshCount = mesh.subMeshCount,
                IsSkinned = isSkinned,
                VertexAttributesVRAM = CalculateVertexAttributesVRAM(mesh, isSkinned),
                IndexBufferVRAM = CalculateIndexBufferVRAM(mesh),
                BlendShapesVRAM = CalculateBlendShapesVRAM(mesh),
            };
        }

        /// <summary>
        /// Formatea bytes a string legible.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            return VRChatTextureWeightCalculator.FormatBytes(bytes);
        }

        #endregion

        #region Private Methods

        private static int GetVertexFormatSize(UnityEngine.Rendering.VertexAttributeFormat format)
        {
            return VertexFormatSizes.TryGetValue(format, out int size) ? size : 4;
        }

        private static int CountNonZeroVertices(Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents)
        {
            int count = 0;
            float epsilon = 0.0001f;

            for (int i = 0; i < deltaVertices.Length; i++)
            {
                // Un vertice cuenta si tiene cualquier delta no-cero
                if (deltaVertices[i].sqrMagnitude > epsilon ||
                    deltaNormals[i].sqrMagnitude > epsilon ||
                    deltaTangents[i].sqrMagnitude > epsilon)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }

    /// <summary>
    /// Informacion detallada de VRAM de un mesh.
    /// </summary>
    [Serializable]
    public struct MeshVRAMInfo
    {
        public string MeshName;
        public int VertexCount;
        public int TriangleCount;
        public int BlendShapeCount;
        public int SubMeshCount;
        public bool IsSkinned;
        public long VertexAttributesVRAM;
        public long IndexBufferVRAM;
        public long BlendShapesVRAM;

        /// <summary>
        /// VRAM total del mesh
        /// </summary>
        public long TotalVRAM => VertexAttributesVRAM + IndexBufferVRAM + BlendShapesVRAM;

        /// <summary>
        /// Etiqueta formateada del tamano total
        /// </summary>
        public string TotalVRAMLabel => MeshVRAMCalculator.FormatBytes(TotalVRAM);
    }
}
