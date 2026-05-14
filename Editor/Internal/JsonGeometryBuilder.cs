using System.Collections.Generic;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    internal static class JsonGeometryBuilder
    {
        public static Mesh BuildRoomMesh(RoomData room, EnvironmentParameters p, string meshName)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var floorTris = new List<int>();
            var wallTris = new List<int>();
            var ceilingTris = new List<int>();

            BuildTileSurfaces(room, p, verts, normals, uvs, floorTris, ceilingTris);

            if (room.Walls != null)
                foreach (var w in room.Walls)
                    BuildWall(w, p, verts, normals, uvs, wallTris);

            var mesh = new Mesh { name = meshName };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(floorTris, 0, calculateBounds: false);
            mesh.SetTriangles(wallTris, 1, calculateBounds: false);
            mesh.SetTriangles(ceilingTris, 2, calculateBounds: false);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void BuildTileSurfaces(RoomData room, EnvironmentParameters p,
                                      List<Vector3> verts, List<Vector3> normals,
                                      List<Vector2> uvs,
                                      List<int> floorTris, List<int> ceilingTris)
        {
            if (room.TileIndices == null) return;
            float t = p.TileSize;
            float h = p.WallHeight;

            foreach (var idx in room.TileIndices)
            {
                float x0 = idx.x * t;
                float z0 = idx.y * t;
                float x1 = x0 + t;
                float z1 = z0 + t;

                AddQuad(verts, normals, uvs, floorTris,
                    new Vector3(x0, 0, z0), new Vector3(x1, 0, z0),
                    new Vector3(x1, 0, z1), new Vector3(x0, 0, z1),
                    Vector3.up);

                AddQuad(verts, normals, uvs, ceilingTris,
                    new Vector3(x0, h, z1), new Vector3(x1, h, z1),
                    new Vector3(x1, h, z0), new Vector3(x0, h, z0),
                    Vector3.down);
            }
        }

        static void BuildWall(WallData wall, EnvironmentParameters p,
                              List<Vector3> verts, List<Vector3> normals,
                              List<Vector2> uvs, List<int> tris)
        {
            var start = wall.StartLocal;
            var end = wall.EndLocal;
            var delta = end - start;
            float length = delta.magnitude;
            if (length < 1e-4f) return;

            float h = p.WallHeight;
            float th = p.WallThickness;
            var tangent = delta / length;
            var inward = new Vector3(-tangent.z, 0, tangent.x);

            var a = start;
            var b = end;
            var c = end + Vector3.up * h;
            var d = start + Vector3.up * h;

            var inward2 = inward * th;
            var ai = a + inward2;
            var bi = b + inward2;
            var ci = c + inward2;
            var di = d + inward2;

            var outwardNormal = -inward;
            var inwardNormal = inward;

            AddQuad(verts, normals, uvs, tris, a, b, c, d, outwardNormal);
            AddQuad(verts, normals, uvs, tris, bi, ai, di, ci, inwardNormal);
            AddQuad(verts, normals, uvs, tris, b, bi, ci, c, tangent);
            AddQuad(verts, normals, uvs, tris, ai, a, d, di, -tangent);
            AddQuad(verts, normals, uvs, tris, d, c, ci, di, Vector3.up);
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
