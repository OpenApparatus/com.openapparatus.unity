using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using OpenApparatus.Topology;
using OpenApparatus.Unity.Editor.Internal;
using OpenApparatus.Unity.Internal;

namespace OpenApparatus.Unity.Editor.Importers
{
    [ScriptedImporter(version: 1, ext: "json")]
    public sealed class JsonEnvironmentImporter : ScriptedImporter
    {
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

            if (!JsonEnvironmentDiscriminator.IsOpenApparatus(text)) return;

            JsonEnvironmentDocument doc;
            try
            {
                doc = JsonConvert.DeserializeObject<JsonEnvironmentDocument>(text);
            }
            catch (JsonException e)
            {
                ctx.LogImportError($"OpenApparatus JSON parse failed: {e.Message}");
                return;
            }
            if (doc == null) return;

            var asset = ScriptableObject.CreateInstance<MultiRoomEnvironmentAsset>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            asset.SchemaVersion = doc.version;
            asset.Parameters = MapParameters(doc.parameters);
            asset.ObjectSlots = MapSlots(doc.objectSlots);
            asset.Rooms = MapRooms(doc.rooms);
            asset.OutsideObjects = MapOutside(doc.outside);

            ctx.AddObjectToAsset("environment", asset);
            ctx.SetMainObject(asset);

            if (asset.Rooms != null)
            {
                foreach (var room in asset.Rooms)
                {
                    var mesh = JsonGeometryBuilder.BuildRoomMesh(room, asset.Parameters,
                        $"OpenApparatus_Room_{room.Id}");
                    ctx.AddObjectToAsset($"mesh_room_{room.Id}", mesh);
                    asset.SetRoomMesh(room.Id, mesh);
                }
            }
        }

        static EnvironmentParameters MapParameters(JsonParameters p)
        {
            if (p == null) return new EnvironmentParameters();
            return new EnvironmentParameters
            {
                TileSize = p.tileSize,
                WallThickness = p.wallThickness,
                WallHeight = p.wallHeight,
                DoorWidth = p.doorWidth,
                DoorHeight = p.doorHeight,
                WindowWidth = p.windowWidth,
                WindowHeight = p.windowHeight,
                WindowSillHeight = p.windowSillHeight,
                GridSubdivision = p.gridSubdivision,
                DefaultObjectY = p.defaultObjectY,
            };
        }

        static ObjectSlotDefinition[] MapSlots(List<JsonObjectSlot> slots)
        {
            if (slots == null) return Array.Empty<ObjectSlotDefinition>();
            var result = new ObjectSlotDefinition[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                result[i] = new ObjectSlotDefinition
                {
                    Id = s.id,
                    Shape = s.shape,
                    Color = s.color != null && s.color.Count >= 3
                        ? new Color(s.color[0], s.color[1], s.color[2])
                        : Color.white,
                    Size = s.size,
                    DisplayName = s.displayName,
                    ObjectType = !string.IsNullOrEmpty(s.objectType) ? s.objectType : s.displayName,
                };
            }
            return result;
        }

        static RoomData[] MapRooms(List<JsonRoom> rooms)
        {
            if (rooms == null) return Array.Empty<RoomData>();
            var result = new RoomData[rooms.Count];
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                result[i] = new RoomData
                {
                    Id = r.id,
                    RoomType = ParseRoomType(r.shape),
                    GridPositionStudio = r.position != null && r.position.Count >= 2
                        ? new Vector2(r.position[0], r.position[1])
                        : Vector2.zero,
                    TileIndices = MapTiles(r.tiles),
                    Walls = MapWalls(r.walls),
                    Objects = MapObjects(r.objects),
                };
            }
            return result;
        }

        static RoomType ParseRoomType(JsonRoomShape shape)
        {
            if (shape == null) return RoomType.Square;
            if (string.Equals(shape.type, "rectangle", StringComparison.OrdinalIgnoreCase) &&
                !Mathf.Approximately(shape.width, shape.depth))
            {
                return RoomType.Rectangle;
            }
            return RoomType.Square;
        }

        static Vector2Int[] MapTiles(List<List<int>> tiles)
        {
            if (tiles == null) return Array.Empty<Vector2Int>();
            var result = new Vector2Int[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
            {
                var pair = tiles[i];
                result[i] = pair != null && pair.Count >= 2
                    ? new Vector2Int(pair[0], pair[1])
                    : Vector2Int.zero;
            }
            return result;
        }

        static WallData[] MapWalls(List<JsonWall> walls)
        {
            if (walls == null) return Array.Empty<WallData>();
            var result = new WallData[walls.Count];
            for (int i = 0; i < walls.Count; i++)
            {
                var w = walls[i];
                var startStudio = w.start != null && w.start.Count >= 2
                    ? new Vector3(w.start[0], 0, w.start[1])
                    : Vector3.zero;
                var endStudio = w.end != null && w.end.Count >= 2
                    ? new Vector3(w.end[0], 0, w.end[1])
                    : Vector3.zero;

                result[i] = new WallData
                {
                    Number = w.number,
                    StartLocal = OpenApparatusSpace.ToUnity(startStudio),
                    EndLocal = OpenApparatusSpace.ToUnity(endStudio),
                    NeighbourRoomId = w.neighborRoomId ?? -1,
                    PassageKind = ParsePassageKind(w.passage?.type),
                    Openings = MapOpenings(w.passage?.openings),
                };
            }
            return result;
        }

        static PassageKind ParsePassageKind(string type)
        {
            if (string.Equals(type, "doorway", StringComparison.OrdinalIgnoreCase))
                return PassageKind.Doorway;
            if (string.Equals(type, "open", StringComparison.OrdinalIgnoreCase))
                return PassageKind.Open;
            return PassageKind.Closed;
        }

        static OpeningSpec[] MapOpenings(List<JsonOpening> openings)
        {
            if (openings == null) return Array.Empty<OpeningSpec>();
            var result = new OpeningSpec[openings.Count];
            for (int i = 0; i < openings.Count; i++)
            {
                var o = openings[i];
                result[i] = new OpeningSpec
                {
                    OffsetAlongEdge = o.offsetAlongEdge,
                    Width = o.width,
                    Height = o.height,
                    SillHeight = o.sillHeight,
                };
            }
            return result;
        }

        static ObjectInstanceData[] MapObjects(List<JsonObjectInstance> objs)
        {
            if (objs == null) return Array.Empty<ObjectInstanceData>();
            var result = new ObjectInstanceData[objs.Count];
            for (int i = 0; i < objs.Count; i++)
            {
                var o = objs[i];
                var studioPos = o.position != null && o.position.Count >= 3
                    ? new Vector3(o.position[0], o.position[1], o.position[2])
                    : Vector3.zero;
                result[i] = new ObjectInstanceData
                {
                    Slot = o.slot,
                    LocalPosition = OpenApparatusSpace.ToUnity(studioPos),
                    LocalRotationY = OpenApparatusSpace.YawToUnity(o.rotation),
                };
            }
            return result;
        }

        static ObjectInstanceData[] MapOutside(JsonOutside outside)
        {
            return outside == null ? Array.Empty<ObjectInstanceData>() : MapObjects(outside.objects);
        }
    }
}
