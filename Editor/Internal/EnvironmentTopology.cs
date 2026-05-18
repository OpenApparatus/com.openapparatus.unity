using System.Collections.Generic;
using UnityEngine;
using OpenApparatus;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Rebuilds the Core <see cref="MultiRoomEnvironment"/> from a
    /// <see cref="MultiRoomEnvironmentAsset"/>: derives topology from the stored
    /// room grid via <see cref="MultiRoomEnvironmentBuilder.FromGrid"/>, then
    /// applies each adjacency's passage from the matching imported wall. This is
    /// the bridge from the imported asset to the Core-backed geometry pipeline.
    /// </summary>
    internal static class EnvironmentTopology
    {
        const float MatchEpsilon = 1e-3f;

        public static MultiRoomEnvironment Rebuild(MultiRoomEnvironmentAsset asset)
        {
            var grid = ToGrid(asset);
            var plan = MultiRoomEnvironmentBuilder.FromGrid(grid, asset.Parameters.TileSize);
            ApplyPassages(plan, asset);
            return plan;
        }

        /// <summary>Unflattens the asset's row-major RoomGrid into an [x, z] array.</summary>
        public static int[,] ToGrid(MultiRoomEnvironmentAsset asset)
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

        static void ApplyPassages(MultiRoomEnvironment plan, MultiRoomEnvironmentAsset asset)
        {
            if (asset.Rooms == null) return;
            var roomsById = new Dictionary<int, RoomData>();
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

        // WallData endpoints are X-mirrored Unity space; un-mirror X to compare
        // against the Studio-space adjacency segment.
        static WallData MatchWall(WallData[] walls, EdgeSegment seg)
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

        static Passage ToCorePassage(WallData wall)
        {
            switch (wall.PassageKind)
            {
                case PassageKind.Open:
                    return Passage.Open.Instance;
                case PassageKind.Doorway:
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
