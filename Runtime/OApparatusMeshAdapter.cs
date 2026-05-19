using UnityEngine;
using OpenApparatus.Geometry;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Converts an engine-agnostic <see cref="MeshData"/> from OpenApparatus.Core
    /// into a UnityEngine.Mesh ready for assignment to a MeshFilter.
    /// </summary>
    public static class OApparatusMeshAdapter
    {
        /// <param name="mirrorX">When true, negate X on every vertex and normal and
        /// reverse triangle winding. Core geometry is authored in Studio space
        /// (right-handed, +X east); Unity is left-handed. This applies the same
        /// X-mirror as <see cref="Internal.OApparatusSpace.ToUnity"/>, at the
        /// single Core-to-Unity mesh conversion boundary.</param>
        public static Mesh ToUnityMesh(MeshData data, string meshName = "OpenApparatus.Mesh",
                                       bool mirrorX = false)
        {
            var mesh = new Mesh { name = meshName };

            // Use 32-bit indices to avoid hitting the 65535-vertex cap on larger floor plans.
            mesh.indexFormat = data.VertexCount > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            float sx = mirrorX ? -1f : 1f;

            var verts = new Vector3[data.VertexCount];
            for (int i = 0; i < data.VertexCount; i++)
                verts[i] = new Vector3(sx * data.Vertices[i].X, data.Vertices[i].Y, data.Vertices[i].Z);
            mesh.SetVertices(verts);

            var normals = new Vector3[data.VertexCount];
            for (int i = 0; i < data.VertexCount; i++)
                normals[i] = new Vector3(sx * data.Normals[i].X, data.Normals[i].Y, data.Normals[i].Z);
            mesh.SetNormals(normals);

            var uvs = new Vector2[data.VertexCount];
            for (int i = 0; i < data.VertexCount; i++)
                uvs[i] = new Vector2(data.Uv0[i].X, data.Uv0[i].Y);
            mesh.SetUVs(0, uvs);

            mesh.subMeshCount = data.SubmeshCount;
            for (int s = 0; s < data.SubmeshCount; s++)
            {
                var tris = data.SubmeshIndices[s];
                if (mirrorX)
                {
                    // Negating X flips triangle orientation; swap the last two
                    // indices of each triangle to keep winding consistent.
                    tris = (int[])tris.Clone();
                    for (int i = 0; i + 2 < tris.Length; i += 3)
                        (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
                }
                mesh.SetTriangles(tris, s, calculateBounds: false);
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
