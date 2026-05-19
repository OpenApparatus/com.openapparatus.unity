using System;
using System.Collections.Generic;
using UnityEngine;
using OpenApparatus;
using OpenApparatus.Topology;
using OpenApparatus.Unity.Internal;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Rebuilds the Core <see cref="MultiRoomEnvironment"/> from a
    /// <see cref="OApparatusAsset"/>: derives topology from the stored
    /// room grid via <see cref="MultiRoomEnvironmentBuilder.FromGrid"/>, then
    /// applies each adjacency's passage from the matching imported wall. This is
    /// the bridge from the imported asset to the Core-backed geometry pipeline.
    /// </summary>
    internal static class OApparatusTopology
    {
        const float MatchEpsilon = 1e-3f;

        public static MultiRoomEnvironment Rebuild(OApparatusAsset asset)
        {
            var grid = ToGrid(asset);
            var plan = MultiRoomEnvironmentBuilder.FromGrid(grid, asset.Parameters.TileSize);
            ApplyPassages(plan, asset);
            return plan;
        }

        /// <summary>Unflattens the asset's row-major RoomGrid into an [x, z] array.</summary>
        public static int[,] ToGrid(OApparatusAsset asset)
        {
            int w = Mathf.Max(0, asset.GridWidth);
            int l = Mathf.Max(0, asset.GridLength);
            var grid = new int[w, l];
            var flat = asset.RoomGrid;
            for (int x = 0; x < w; x++)
                for (int z = 0; z < l; z++)
                {
                    int i = x * l + z;
                    grid[x, z] = flat != null && i < flat.Length ? flat[i] : -1;
                }
            return grid;
        }

        static void ApplyPassages(MultiRoomEnvironment plan, OApparatusAsset asset)
        {
            if (asset.Rooms == null) return;
            var roomsById = new Dictionary<int, OApparatusRoomInfo>();
            foreach (var rd in asset.Rooms)
                if (rd != null) roomsById[rd.Id] = rd;

            foreach (var adj in plan.Adjacencies)
            {
                // The lower-id room's imported wall shares the adjacency
                // segment's orientation, so opening offsets transfer directly.
                int ownerId = adj.IsOuter
                    ? adj.RoomA.Id
                    : Mathf.Min(adj.RoomA.Id, adj.RoomB.Id);
                if (!roomsById.TryGetValue(ownerId, out var owner) || owner.Walls == null)
                    continue;

                var wall = MatchWall(owner.Walls, adj.SharedSegment);
                if (wall != null)
                    adj.Passage = ToCorePassage(wall);
            }
        }

        // OApparatusWallInfo endpoints are X-mirrored Unity space; un-mirror X to compare
        // against the Studio-space adjacency segment.
        static OApparatusWallInfo MatchWall(OApparatusWallInfo[] walls, EdgeSegment seg)
        {
            foreach (var w in walls)
            {
                if (w == null) continue;
                if (Approx(-w.StartLocal.x, seg.Start.X) && Approx(w.StartLocal.z, seg.Start.Y) &&
                    Approx(-w.EndLocal.x,   seg.End.X)   && Approx(w.EndLocal.z,   seg.End.Y))
                    return w;
            }
            return null;
        }

        static bool Approx(float a, float b) => Mathf.Abs(a - b) < MatchEpsilon;

        /// <summary>
        /// Converts a Core topology into the importer's <see cref="OApparatusRoomInfo"/>
        /// array — one room per Core room, each carrying its walls (described
        /// from that room's perspective, like the JSON exporter). Object lists
        /// are left empty for the caller to populate.
        /// </summary>
        public static OApparatusRoomInfo[] BuildRoomData(MultiRoomEnvironment plan, int[,] grid)
        {
            var tilesByRoom = TilesByRoom(grid);
            var result = new OApparatusRoomInfo[plan.Rooms.Count];
            for (int i = 0; i < plan.Rooms.Count; i++)
            {
                var room = plan.Rooms[i];
                var walls = new List<OApparatusWallInfo>();
                int wallNumber = 1;
                foreach (var adj in plan.Adjacencies)
                {
                    if (adj.RoomA != room && adj.RoomB != room) continue;

                    bool roomIsA = adj.RoomA == room;
                    var seg = adj.SharedSegment;
                    var s = roomIsA ? seg.Start : seg.End;
                    var e = roomIsA ? seg.End : seg.Start;
                    walls.Add(new OApparatusWallInfo
                    {
                        Number = wallNumber++,
                        StartLocal = OApparatusSpace.ToUnity(new Vector3(s.X, 0f, s.Y)),
                        EndLocal = OApparatusSpace.ToUnity(new Vector3(e.X, 0f, e.Y)),
                        NeighbourRoomId = roomIsA ? (adj.RoomB?.Id ?? -1) : adj.RoomA.Id,
                        OApparatusPassageKind = ToUnityPassageKind(adj.Passage),
                        Openings = ToOpeningSpecs(adj.Passage),
                    });
                }
                result[i] = new OApparatusRoomInfo
                {
                    Id = room.Id,
                    RoomType = room.RoomType,
                    GridPositionStudio = new Vector2(room.Position.X, room.Position.Y),
                    TileIndices = tilesByRoom.TryGetValue(room.Id, out var t)
                        ? t.ToArray()
                        : Array.Empty<Vector2Int>(),
                    Walls = walls.ToArray(),
                    Objects = Array.Empty<OApparatusObjectInfo>(),
                };
            }
            return result;
        }

        static Dictionary<int, List<Vector2Int>> TilesByRoom(int[,] grid)
        {
            var result = new Dictionary<int, List<Vector2Int>>();
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

        static OApparatusPassageKind ToUnityPassageKind(Passage p)
        {
            if (p is Passage.Open) return OApparatusPassageKind.Open;
            if (p is Passage.Doorway) return OApparatusPassageKind.Doorway;
            return OApparatusPassageKind.Closed;
        }

        static OApparatusOpeningInfo[] ToOpeningSpecs(Passage p)
        {
            if (p is not Passage.Doorway d) return Array.Empty<OApparatusOpeningInfo>();
            var specs = new OApparatusOpeningInfo[d.Openings.Count];
            for (int i = 0; i < specs.Length; i++)
            {
                var o = d.Openings[i];
                specs[i] = new OApparatusOpeningInfo
                {
                    OffsetAlongEdge = o.OffsetAlongEdge,
                    Width = o.Width,
                    Height = o.Height,
                    SillHeight = o.SillHeight,
                };
            }
            return specs;
        }

        static Passage ToCorePassage(OApparatusWallInfo wall)
        {
            switch (wall.OApparatusPassageKind)
            {
                case OApparatusPassageKind.Open:
                    return Passage.Open.Instance;
                case OApparatusPassageKind.Doorway:
                    if (wall.Openings == null || wall.Openings.Length == 0)
                        return Passage.Closed.Instance;
                    var openings = new Opening[wall.Openings.Length];
                    for (int i = 0; i < openings.Length; i++)
                    {
                        var o = wall.Openings[i];
                        openings[i] = new Opening(o.OffsetAlongEdge, o.Width, o.Height, o.SillHeight);
                    }
                    return new Passage.Doorway(openings);
                default:
                    return Passage.Closed.Instance;
            }
        }
    }
}
