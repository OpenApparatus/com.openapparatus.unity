using System.Collections.Generic;
using UnityEngine;
using OpenApparatus.Geometry;
using OpenApparatus.Topology;
using CoreRoom = OpenApparatus.Topology.Room;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Unity-side counterpart to Core's <c>GltfExporter.BuildModel</c>: walks a
    /// <see cref="MultiRoomEnvironment"/> and emits a <c>Room_{id}</c> →
    /// Floor / Ceiling / Walls GameObject tree. Geometry comes from the same Core
    /// builders (<see cref="RectangleInteriorBuilder"/>, <see cref="BoundaryWallBuilder"/>)
    /// that Studio's glTF export uses, so an imported environment is identical to
    /// a Studio-baked <c>.glb</c> by construction — including doorway cut-outs.
    ///
    /// Shared internal walls are split by face normal: RoomA keeps the +N body
    /// face, RoomB the -N face, and frame pieces (caps, lintels, sills, door
    /// thresholds) go to the lower-id room — matching the glTF exporter.
    /// </summary>
    internal static class EnvironmentModelBuilder
    {
        // A wall body face aligned with the adjacency normal past this threshold
        // is owned by RoomA; aligned against it, RoomB; in between, it is a frame
        // piece owned by the lower-id room.
        const float NormalAlign = 0.9f;

        public static GameObject Build(string rootName, MultiRoomEnvironment plan,
                                       int[,] roomGrid, EnvironmentParameters p,
                                       EnvironmentBuildOptions opts)
        {
            opts ??= new EnvironmentBuildOptions();
            var root = new GameObject(string.IsNullOrEmpty(rootName) ? "OpenApparatus" : rootName);
            root.AddComponent<EnvironmentRoot>();

            float t = p.WallThickness;
            float h = p.WallHeight;
            var interiorBuilder = new RectangleInteriorBuilder();
            var wallBuilder = new BoundaryWallBuilder();
            var tilesByRoom = TilesByRoom(roomGrid);

            // One wall mesh per adjacency, shared by both adjoining rooms.
            var wallMeshes = new Dictionary<Adjacency, MeshData>();
            foreach (var adj in plan.Adjacencies)
                wallMeshes[adj] = wallBuilder.Build(adj, t, h);

            foreach (var room in plan.Rooms)
            {
                var roomGo = new GameObject($"Room_{room.Id}");
                roomGo.transform.SetParent(root.transform, worldPositionStays: false);
                AddRoomComponent(roomGo, room, tilesByRoom);

                // Interior carries Floor + Ceiling submeshes; each wall contributes
                // to Walls (and Floor, for door thresholds). Combine merges by
                // submesh so the result has Floor(0)/Walls(1)/Ceiling(2) for this room.
                var parts = new List<MeshData> { interiorBuilder.Build(room, t, h) };
                foreach (var adj in plan.Adjacencies)
                {
                    if (adj.RoomA != room && adj.RoomB != room) continue;
                    parts.Add(RoomShareOfWall(wallMeshes[adj], adj, room));
                }
                var combined = MeshData.Combine(parts);

                if (opts.GenerateFloors)
                    CreatePart(roomGo.transform, "Floor", combined, SubmeshIndex.Floor,
                               $"OpenApparatus_Floor_{room.Id}", opts);
                if (opts.GenerateWalls)
                    CreatePart(roomGo.transform, "Walls", combined, SubmeshIndex.Walls,
                               $"OpenApparatus_Walls_{room.Id}", opts);
                if (opts.GenerateCeilings)
                    CreatePart(roomGo.transform, "Ceiling", combined, SubmeshIndex.Ceiling,
                               $"OpenApparatus_Ceiling_{room.Id}", opts);
            }
            return root;
        }

        /// <summary>
        /// Filters one adjacency's wall mesh down to the triangles a given room
        /// renders: its own body face, plus frame + door-threshold geometry when
        /// the room is the lower-id owner. Other submeshes are emptied.
        /// </summary>
        static MeshData RoomShareOfWall(MeshData wall, Adjacency adj, CoreRoom room)
        {
            bool roomIsA = adj.RoomA == room;
            bool isOwner = LowerIdOwner(adj) == room;
            var n2 = adj.SharedSegment.Normal;

            var wallsIn = wall.SubmeshIndices[SubmeshIndex.Walls];
            var wallsOut = new List<int>(wallsIn.Length);
            for (int i = 0; i + 2 < wallsIn.Length; i += 3)
            {
                var fn = wall.Normals[wallsIn[i]];
                float dot = fn.X * n2.X + fn.Z * n2.Y;
                bool keep = dot > NormalAlign ? roomIsA
                          : dot < -NormalAlign ? !roomIsA
                          : isOwner;
                if (!keep) continue;
                wallsOut.Add(wallsIn[i]);
                wallsOut.Add(wallsIn[i + 1]);
                wallsOut.Add(wallsIn[i + 2]);
            }

            // Door thresholds live in the Floor submesh; the lower-id owner keeps them.
            var floorOut = isOwner ? wall.SubmeshIndices[SubmeshIndex.Floor] : System.Array.Empty<int>();

            var submeshes = new int[SubmeshIndex.Count][];
            submeshes[SubmeshIndex.Floor] = floorOut;
            submeshes[SubmeshIndex.Walls] = wallsOut.ToArray();
            submeshes[SubmeshIndex.Ceiling] = System.Array.Empty<int>();
            return new MeshData(wall.Vertices, wall.Normals, wall.Uv0, submeshes);
        }

        static void CreatePart(Transform parent, string name, MeshData combined,
                               int submesh, string materialName, EnvironmentBuildOptions opts)
        {
            var tris = combined.SubmeshIndices[submesh];
            if (tris.Length == 0) return;

            var single = new MeshData(combined.Vertices, combined.Normals, combined.Uv0,
                                      new[] { tris });
            var mesh = UnityMeshAdapter.ToUnityMesh(single, materialName, mirrorX: true);

            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialResolver.Resolve(materialName, opts.MaterialOverrides);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        }

        static void AddRoomComponent(GameObject roomGo, CoreRoom room,
                                     Dictionary<int, List<Vector2Int>> tilesByRoom)
        {
            var component = roomGo.AddComponent<Room>();
            component.RoomId = room.Id;
            component.RoomType = room.RoomType;
            component.GridPositionStudio = new Vector2(room.Position.X, room.Position.Y);
            component.TileIndices = tilesByRoom.TryGetValue(room.Id, out var tiles)
                ? tiles.ToArray()
                : System.Array.Empty<Vector2Int>();
        }

        static Dictionary<int, List<Vector2Int>> TilesByRoom(int[,] grid)
        {
            var result = new Dictionary<int, List<Vector2Int>>();
            if (grid == null) return result;
            int width = grid.GetLength(0);
            int length = grid.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int z = 0; z < length; z++)
                {
                    int id = grid[x, z];
                    if (id < 0) continue;
                    if (!result.TryGetValue(id, out var list))
                        result[id] = list = new List<Vector2Int>();
                    list.Add(new Vector2Int(x, z));
                }
            return result;
        }

        static CoreRoom LowerIdOwner(Adjacency adj)
        {
            if (adj.IsOuter) return adj.RoomA;
            return adj.RoomA.Id < adj.RoomB.Id ? adj.RoomA : adj.RoomB;
        }
    }
}
