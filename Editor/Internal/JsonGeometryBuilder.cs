using System.Collections.Generic;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    internal static class JsonGeometryBuilder
    {
        // Tile indices are in Studio coords (right-handed, +X east). Wall
        // start/end positions stored on WallData are already mirrored to
        // Unity by the importer. Floor and ceiling vertices must apply the
        // same X-mirror so they share the room's coordinate frame; otherwise
        // floors sit at +X while walls sit at -X (a 7-unit gap).
        public static Mesh BuildFloorMesh(RoomData room, EnvironmentParameters p, string meshName)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            if (room.TileIndices != null)
            {
                float t = p.TileSize;
                foreach (var idx in room.TileIndices)
                {
                    // After X-mirror the lower bound becomes -(idx.x+1)*t
                    // and the upper bound -idx.x*t so we still have x0 < x1.
                    float x0 = -(idx.x + 1) * t, z0 = idx.y * t;
                    float x1 = -idx.x * t,       z1 = z0 + t;
                    AddQuad(verts, normals, uvs, tris,
                        new Vector3(x0, 0, z0), new Vector3(x1, 0, z0),
                        new Vector3(x1, 0, z1), new Vector3(x0, 0, z1),
                        Vector3.up);
                }
            }
            return BuildMesh(meshName, verts, normals, uvs, tris);
        }

        public static Mesh BuildCeilingMesh(RoomData room, EnvironmentParameters p, string meshName)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            if (room.TileIndices != null)
            {
                float t = p.TileSize;
                float h = p.WallHeight;
                foreach (var idx in room.TileIndices)
                {
                    float x0 = -(idx.x + 1) * t, z0 = idx.y * t;
                    float x1 = -idx.x * t,       z1 = z0 + t;
                    AddQuad(verts, normals, uvs, tris,
                        new Vector3(x0, h, z1), new Vector3(x1, h, z1),
                        new Vector3(x1, h, z0), new Vector3(x0, h, z0),
                        Vector3.down);
                }
            }
            return BuildMesh(meshName, verts, normals, uvs, tris);
        }

        public static Mesh BuildWallMesh(WallData wall, EnvironmentParameters p, string meshName)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            var start = wall.StartLocal;
            var end = wall.EndLocal;
            var delta = end - start;
            float length = delta.magnitude;
            if (length < 1e-4f) return BuildMesh(meshName, verts, normals, uvs, tris);

            float h = p.WallHeight;
            float th = p.WallThickness;
            var tangent = delta / length;
            var inward = new Vector3(-tangent.z, 0, tangent.x);
            var inwardOffset = inward * th;

            var a  = start;
            var b  = end;
            var c  = end   + Vector3.up * h;
            var d  = start + Vector3.up * h;
            var ai = a + inwardOffset;
            var bi = b + inwardOffset;
            var ci = c + inwardOffset;
            var di = d + inwardOffset;

            AddQuad(verts, normals, uvs, tris, a, b, c, d, -inward);
            AddQuad(verts, normals, uvs, tris, bi, ai, di, ci, inward);
            AddQuad(verts, normals, uvs, tris, b, bi, ci, c, tangent);
            AddQuad(verts, normals, uvs, tris, ai, a, d, di, -tangent);
            AddQuad(verts, normals, uvs, tris, d, c, ci, di, Vector3.up);

            return BuildMesh(meshName, verts, normals, uvs, tris);
        }

        static Mesh BuildMesh(string name,
                              List<Vector3> verts, List<Vector3> normals,
                              List<Vector2> uvs, List<int> tris)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 1;
            mesh.SetTriangles(tris, 0, calculateBounds: false);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddQuad(List<Vector3> verts, List<Vector3> normals,
                            List<Vector2> uvs, List<int> tris,
                            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
                            Vector3 normal)
        {
            int baseIdx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
            tris.Add(baseIdx); tris.Add(baseIdx + 2); tris.Add(baseIdx + 3);
        }
    }
}
