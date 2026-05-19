using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor.AssetImporters;
using UnityEngine;
using OpenApparatus.Topology;
using OpenApparatus.Unity.Editor.Internal;
using OpenApparatus.Unity.Internal;

namespace OpenApparatus.Unity.Editor.Importers
{
    /// <summary>
    /// Imports a Studio <c>.oapp</c> project file as a
    /// <see cref="OApparatusAsset"/>. Unlike <c>.oae</c>, an <c>.oapp</c>
    /// carries the authored room grid rather than derived topology, so the
    /// importer rebuilds topology with <see cref="MultiRoomEnvironmentBuilder.FromGrid"/>
    /// and applies the project's passage overrides — yielding the same asset
    /// shape the JSON importer produces.
    /// </summary>
    [ScriptedImporter(version: 1, ext: "oapp")]
    public sealed class OApparatusOappImporter : ScriptedImporter
    {
        public OApparatusColliderMode OApparatusColliderMode = OApparatusColliderMode.None;
        public OApparatusSubstitutionTable Substitution;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string text;
            try
            {
                text = File.ReadAllText(ctx.assetPath);
            }
            catch (Exception)
            {
                return;
            }

            OApparatusOappDocument doc;
            try
            {
                doc = JsonConvert.DeserializeObject<OApparatusOappDocument>(text);
            }
            catch (JsonException e)
            {
                ctx.LogImportError($"OpenApparatus .oapp parse failed: {e.Message}");
                return;
            }
            if (doc == null) return;

            if (doc.version == null || !doc.version.StartsWith("1."))
            {
                ctx.LogImportError(
                    $"Unsupported .oapp version '{doc.version}'. This package reads version 1.x.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<OApparatusAsset>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            asset.Parameters = MapParameters(doc);
            asset.ObjectSlots = MapSlots(doc.objectTypes);
            asset.GridWidth = doc.gridWidth;
            asset.GridLength = doc.gridLength;
            asset.RoomGrid = doc.roomGrid ?? Array.Empty<int>();

            var grid = OApparatusTopology.ToGrid(asset);
            MultiRoomEnvironment plan;
            try
            {
                plan = MultiRoomEnvironmentBuilder.FromGrid(grid, asset.Parameters.TileSize);
            }
            catch (Exception e)
            {
                ctx.LogImportError($"OpenApparatus .oapp topology build failed: {e.Message}");
                return;
            }

            ApplyOverrides(plan, doc.passageOverrides);
            asset.Rooms = OApparatusTopology.BuildRoomData(plan, grid);
            AssignObjects(asset, doc.objects);

            asset.RoomNames = MapNames(doc.roomNames);
            asset.RoomFloorColors = MapColors(doc.roomFloorColors);
            asset.RoomCeilingColors = MapColors(doc.roomCeilingColors);
            asset.RoomWallColors = MapColors(doc.roomSingleWallColors);

            asset.OApparatusColliderMode = OApparatusColliderMode;
            asset.Substitution = Substitution;

            ctx.AddObjectToAsset("environment", asset);
            ctx.SetMainObject(asset);
        }

        static OApparatusParameters MapParameters(OApparatusOappDocument doc) => new OApparatusParameters
        {
            TileSize = doc.tileSize,
            WallThickness = doc.wallThickness,
            WallHeight = doc.wallHeight,
            DoorWidth = doc.doorWidth,
            DoorHeight = doc.doorHeight,
            WindowWidth = doc.windowWidth,
            WindowHeight = doc.windowHeight,
            WindowSillHeight = doc.windowSillHeight,
            GridSubdivision = doc.gridSubdivision,
            DefaultObjectY = doc.defaultObjectY,
        };

        static OApparatusObjectSlot[] MapSlots(List<OApparatusOappObjectType> types)
        {
            if (types == null) return Array.Empty<OApparatusObjectSlot>();
            var result = new OApparatusObjectSlot[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                result[i] = new OApparatusObjectSlot
                {
                    Id = i + 1,
                    Shape = t.shape,
                    Color = t.color != null && t.color.Length >= 3
                        ? new Color(t.color[0], t.color[1], t.color[2])
                        : Color.white,
                    Size = t.size,
                    DisplayName = t.name,
                    ObjectType = t.name,
                };
            }
            return result;
        }

        static void ApplyOverrides(MultiRoomEnvironment plan, List<OApparatusOappPassageOverride> overrides)
        {
            if (overrides == null) return;
            foreach (var ov in overrides)
            {
                foreach (var adj in plan.Adjacencies)
                {
                    var seg = adj.SharedSegment;
                    bool forward = Approx(seg.Start.X, ov.startX) && Approx(seg.Start.Y, ov.startZ)
                                && Approx(seg.End.X, ov.endX)     && Approx(seg.End.Y, ov.endZ);
                    bool reversed = Approx(seg.Start.X, ov.endX)  && Approx(seg.Start.Y, ov.endZ)
                                 && Approx(seg.End.X, ov.startX)  && Approx(seg.End.Y, ov.startZ);
                    if (!forward && !reversed) continue;
                    adj.Passage = ToCorePassage(ov, reversed, seg.Length);
                    break;
                }
            }
        }

        static bool Approx(float a, float b) => Mathf.Abs(a - b) < 1e-3f;

        static Passage ToCorePassage(OApparatusOappPassageOverride ov, bool reversed, float segLength)
        {
            if (string.Equals(ov.kind, "Open", StringComparison.OrdinalIgnoreCase))
                return Passage.Open.Instance;
            if (!string.Equals(ov.kind, "Doorway", StringComparison.OrdinalIgnoreCase))
                return Passage.Closed.Instance;
            if (ov.openings == null || ov.openings.Count == 0)
                return Passage.Closed.Instance;

            var openings = new Opening[ov.openings.Count];
            for (int i = 0; i < openings.Length; i++)
            {
                var o = ov.openings[i];
                // Opening offsets run from the override segment's start; mirror
                // them when the override runs opposite the derived adjacency.
                float offset = reversed ? segLength - o.offset - o.width : o.offset;
                openings[i] = new Opening(Mathf.Max(0f, offset), o.width, o.height, o.sillHeight);
            }
            return new Passage.Doorway(openings);
        }

        static void AssignObjects(OApparatusAsset asset, List<OApparatusOappObjectInstance> objects)
        {
            if (asset.Rooms == null) return;

            var roomIds = new HashSet<int>();
            foreach (var r in asset.Rooms) roomIds.Add(r.Id);

            var byRoom = new Dictionary<int, List<OApparatusObjectInfo>>();
            var outside = new List<OApparatusObjectInfo>();
            if (objects != null)
            {
                foreach (var o in objects)
                {
                    var data = new OApparatusObjectInfo
                    {
                        Slot = o.slot,
                        LocalPosition = OApparatusSpace.ToUnity(new Vector3(o.x, o.y, o.z)),
                        LocalRotationY = OApparatusSpace.YawToUnity(o.rotation),
                    };
                    if (roomIds.Contains(o.owningRoomId))
                    {
                        if (!byRoom.TryGetValue(o.owningRoomId, out var list))
                            byRoom[o.owningRoomId] = list = new List<OApparatusObjectInfo>();
                        list.Add(data);
                    }
                    else
                    {
                        outside.Add(data);
                    }
                }
            }

            foreach (var r in asset.Rooms)
                r.Objects = byRoom.TryGetValue(r.Id, out var list)
                    ? list.ToArray()
                    : Array.Empty<OApparatusObjectInfo>();
            asset.OutsideObjects = outside.ToArray();
        }

        static OApparatusRoomNameEntry[] MapNames(Dictionary<int, string> names)
        {
            if (names == null) return Array.Empty<OApparatusRoomNameEntry>();
            var result = new List<OApparatusRoomNameEntry>(names.Count);
            foreach (var kv in names)
                if (!string.IsNullOrEmpty(kv.Value))
                    result.Add(new OApparatusRoomNameEntry { RoomId = kv.Key, Name = kv.Value });
            return result.ToArray();
        }

        static OApparatusRoomColorEntry[] MapColors(Dictionary<int, float[]> colors)
        {
            if (colors == null) return Array.Empty<OApparatusRoomColorEntry>();
            var result = new List<OApparatusRoomColorEntry>(colors.Count);
            foreach (var kv in colors)
                if (kv.Value != null && kv.Value.Length >= 3)
                    result.Add(new OApparatusRoomColorEntry
                    {
                        RoomId = kv.Key,
                        Color = new Color(kv.Value[0], kv.Value[1], kv.Value[2]),
                    });
            return result.ToArray();
        }
    }
}
