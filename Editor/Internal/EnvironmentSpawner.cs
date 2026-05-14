using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class EnvironmentSpawner
    {
        public static GameObject Spawn(MultiRoomEnvironmentAsset asset)
        {
            if (asset == null) return null;

            var root = new GameObject(asset.name);
            var rootComponent = root.AddComponent<EnvironmentRoot>();
            rootComponent.Asset = asset;

            if (asset.Rooms != null)
                foreach (var roomData in asset.Rooms)
                    SpawnRoom(root.transform, roomData, asset);

            PrefabSubstitutionApplicator.Apply(root, asset.Substitution, asset.ObjectSlots);
            ColliderBuilder.Apply(root, asset.ColliderMode, asset.Parameters);

            Undo.RegisterCreatedObjectUndo(root, "Spawn OpenApparatus environment");
            return root;
        }

        static void SpawnRoom(Transform parent, RoomData roomData, MultiRoomEnvironmentAsset asset)
        {
            var roomGo = new GameObject($"Room_{roomData.Id}");
            roomGo.transform.SetParent(parent, worldPositionStays: false);

            var p = asset.Parameters;
            roomGo.transform.localPosition = new Vector3(
                -roomData.GridPositionStudio.x * p.TileSize,
                0f,
                roomData.GridPositionStudio.y * p.TileSize);

            var roomComponent = roomGo.AddComponent<Room>();
            roomComponent.RoomId = roomData.Id;
            roomComponent.RoomType = roomData.RoomType;
            roomComponent.GridPositionStudio = roomData.GridPositionStudio;
            roomComponent.TileIndices = roomData.TileIndices;

            var mesh = asset.GetRoomMesh(roomData.Id);
            if (mesh != null)
            {
                var mf = roomGo.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = roomGo.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[]
                {
                    MaterialResolver.Resolve($"OpenApparatus_Floor_{roomData.Id}"),
                    MaterialResolver.Resolve($"OpenApparatus_Walls_{roomData.Id}"),
                    MaterialResolver.Resolve($"OpenApparatus_Ceiling_{roomData.Id}"),
                };
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }

            if (roomData.Walls != null)
                foreach (var wd in roomData.Walls)
                    SpawnWall(roomGo.transform, wd);

            if (roomData.Objects != null)
                foreach (var od in roomData.Objects)
                    SpawnObject(roomGo.transform, od, roomData.Id, asset);
        }

        static void SpawnWall(Transform parent, WallData wd)
        {
            var go = new GameObject($"Wall_{wd.Number}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var w = go.AddComponent<Wall>();
            w.WallNumber = wd.Number;
            w.StartLocal = wd.StartLocal;
            w.EndLocal = wd.EndLocal;
            w.NeighbourRoomId = wd.NeighbourRoomId;
            w.PassageKind = wd.PassageKind;
            w.Openings = wd.Openings;
        }

        static void SpawnObject(Transform parent, ObjectInstanceData od, int owningRoomId,
                                MultiRoomEnvironmentAsset asset)
        {
            var slot = ResolveSlot(asset.ObjectSlots, od.Slot);
            var primitive = ChoosePrimitive(slot?.Shape);
            var go = GameObject.CreatePrimitive(primitive);
            go.name = $"Object_Slot{od.Slot}";
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = od.LocalPosition;
            go.transform.localRotation = Quaternion.Euler(0f, od.LocalRotationY * Mathf.Rad2Deg, 0f);
            if (slot != null && slot.Size > 0f)
                go.transform.localScale = Vector3.one * slot.Size;

            var instance = go.AddComponent<RoomObjectInstance>();
            instance.Slot = od.Slot;
            instance.OwningRoomId = owningRoomId;
            instance.LocalRotationY = od.LocalRotationY;
        }

        static ObjectSlotDefinition ResolveSlot(ObjectSlotDefinition[] slots, int slotNumber)
        {
            int idx = slotNumber - 1;
            if (slots == null || idx < 0 || idx >= slots.Length) return null;
            return slots[idx];
        }

        static PrimitiveType ChoosePrimitive(string shape)
        {
            if (string.IsNullOrEmpty(shape)) return PrimitiveType.Cube;
            return shape.ToLowerInvariant() switch
            {
                "sphere" => PrimitiveType.Sphere,
                "cylinder" => PrimitiveType.Cylinder,
                "capsule" => PrimitiveType.Capsule,
                _ => PrimitiveType.Cube,
            };
        }
    }
}
