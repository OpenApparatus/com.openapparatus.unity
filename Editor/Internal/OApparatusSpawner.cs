using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class OApparatusSpawner
    {
        public static GameObject Spawn(OApparatusAsset asset)
            => Spawn(asset, new OApparatusBuildOptions());

        public static GameObject Spawn(OApparatusAsset asset, OApparatusBuildOptions options)
        {
            if (asset == null) return null;
            options ??= new OApparatusBuildOptions();

            // Geometry: rebuild the Core topology from the stored grid and
            // build it through the same Core builders Studio's glTF export
            // uses, so an imported environment matches a Studio .glb.
            var plan = OApparatusTopology.Rebuild(asset);
            var grid = OApparatusTopology.ToGrid(asset);
            var root = OApparatusModelBuilder.Build(asset.name, plan, grid,
                                                     asset.Parameters, options);

            var rootComponent = root.GetComponent<OApparatusRootManager>();
            if (rootComponent != null) rootComponent.Asset = asset;

            SpawnObjects(root.transform, asset);
            ConfigureRooms(root.transform, asset);

            OApparatusSubstitutionApplicator.Apply(root, asset.Substitution, asset.ObjectSlots);
            OApparatusColliderBuilder.Apply(root, asset.OApparatusColliderMode, asset.Parameters);

            Undo.RegisterCreatedObjectUndo(root, "Spawn OpenApparatus environment");
            return root;
        }

        static void SpawnObjects(Transform root, OApparatusAsset asset)
        {
            if (asset.Rooms != null)
            {
                foreach (var rd in asset.Rooms)
                {
                    if (rd?.Objects == null || rd.Objects.Length == 0) continue;
                    var roomGo = root.Find($"Room_{rd.Id}");
                    var parent = roomGo != null ? roomGo : root;
                    foreach (var od in rd.Objects)
                        SpawnObject(parent, od, rd.Id, asset);
                }
            }

            if (asset.OutsideObjects != null && asset.OutsideObjects.Length > 0)
            {
                var outside = new GameObject("Outside");
                outside.transform.SetParent(root, worldPositionStays: false);
                foreach (var od in asset.OutsideObjects)
                    SpawnObject(outside.transform, od, -1, asset);
            }
        }

        static void SpawnObject(Transform parent, OApparatusObjectInfo od, int owningRoomId,
                                OApparatusAsset asset)
        {
            var slot = ResolveSlot(asset.ObjectSlots, od.Slot);

            // The slot is a clean logical node: identity + placement only. Its
            // visual lives on a separate "StandIn" child so prefab substitution
            // can swap the visual without disturbing the slot's identity.
            var slotGo = new GameObject($"Object_Slot{od.Slot}");
            slotGo.transform.SetParent(parent, worldPositionStays: false);
            slotGo.transform.localPosition = od.LocalPosition;
            slotGo.transform.localRotation = Quaternion.Euler(0f, od.LocalRotationY * Mathf.Rad2Deg, 0f);

            var instance = slotGo.AddComponent<OApparatusObjectManager>();
            instance.Slot = od.Slot;
            instance.OwningRoomId = owningRoomId;
            instance.LocalRotationY = od.LocalRotationY;
            instance.ObjectType = slot != null
                ? (!string.IsNullOrEmpty(slot.ObjectType) ? slot.ObjectType : slot.DisplayName)
                : null;

            var standIn = GameObject.CreatePrimitive(ChoosePrimitive(slot?.Shape));
            standIn.name = "StandIn";
            standIn.transform.SetParent(slotGo.transform, worldPositionStays: false);
            if (slot != null && slot.Size > 0f)
                standIn.transform.localScale = Vector3.one * slot.Size;

            // CreatePrimitive auto-attaches a Collider. The Objects flag controls
            // whether the stand-in keeps it; substituted prefabs are untouched.
            if ((asset.OApparatusColliderMode & OApparatusColliderMode.Objects) == 0)
            {
                foreach (var c in standIn.GetComponents<Collider>())
                    Object.DestroyImmediate(c);
            }
        }

        static OApparatusObjectSlot ResolveSlot(OApparatusObjectSlot[] slots, int slotNumber)
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

        // Wires per-room component data (wall list) and applies the .oapp
        // editor-state extras: palette colours tint each part's material, and
        // room names extend the GameObject name.
        static void ConfigureRooms(Transform root, OApparatusAsset asset)
        {
            if (asset.Rooms == null) return;
            foreach (var rd in asset.Rooms)
            {
                var roomGo = root.Find($"Room_{rd.Id}");
                if (roomGo == null) continue;

                var room = roomGo.GetComponent<OApparatusRoomManager>();
                if (room != null) room.Walls = rd.Walls;

                Tint(roomGo, "Floor", asset.RoomFloorColors, rd.Id);
                Tint(roomGo, "Walls", asset.RoomWallColors, rd.Id);
                Tint(roomGo, "Ceiling", asset.RoomCeilingColors, rd.Id);

                var name = FindRoomName(asset.RoomNames, rd.Id);
                if (!string.IsNullOrEmpty(name))
                    roomGo.gameObject.name = $"Room_{rd.Id}_{name.Trim()}";
            }
        }

        static void Tint(Transform room, string partName, OApparatusRoomColorEntry[] colors, int roomId)
        {
            if (colors == null) return;
            bool found = false;
            Color color = default;
            foreach (var c in colors)
                if (c.RoomId == roomId) { color = c.Color; found = true; break; }
            if (!found) return;

            var part = room.Find(partName);
            if (part == null) return;
            var mr = part.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null) return;

            // Instance the material so tinting one room never alters another.
            var tinted = new Material(mr.sharedMaterial) { name = mr.sharedMaterial.name };
            tinted.color = color;
            mr.sharedMaterial = tinted;
        }

        static string FindRoomName(OApparatusRoomNameEntry[] names, int roomId)
        {
            if (names == null) return null;
            foreach (var n in names)
                if (n.RoomId == roomId) return n.Name;
            return null;
        }
    }
}
