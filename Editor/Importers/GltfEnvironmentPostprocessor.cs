using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Importers
{
    internal sealed class GltfEnvironmentPostprocessor : AssetPostprocessor
    {
        static readonly Regex RoomNameRx       = new Regex(@"^Room_(\d+)$",                     RegexOptions.Compiled);
        static readonly Regex WallNameRx       = new Regex(@"^Room_(\d+)_wall_(\d+)$",          RegexOptions.Compiled);
        static readonly Regex ObjectNameRx     = new Regex(@"^Room_(\d+)_object_(\d+)_(\d+)$",  RegexOptions.Compiled);

        void OnPostprocessModel(GameObject root)
        {
            if (root == null) return;
            if (!LooksLikeOpenApparatusGltf(root)) return;

            AttachComponents(root.transform);
        }

        static bool LooksLikeOpenApparatusGltf(GameObject root)
        {
            foreach (Transform child in root.transform)
            {
                if (RoomNameRx.IsMatch(child.name)) return true;
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (RoomNameRx.IsMatch(t.name)) return true;
            }
            return false;
        }

        static void AttachComponents(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                var m = RoomNameRx.Match(t.name);
                if (m.Success)
                {
                    var room = t.gameObject.GetComponent<Room>() ?? t.gameObject.AddComponent<Room>();
                    room.RoomId = int.Parse(m.Groups[1].Value);
                    continue;
                }

                m = WallNameRx.Match(t.name);
                if (m.Success)
                {
                    var wall = t.gameObject.GetComponent<Wall>() ?? t.gameObject.AddComponent<Wall>();
                    wall.WallNumber = int.Parse(m.Groups[2].Value);
                    wall.PassageKind = PassageKind.Closed;
                    var bounds = ComputeMeshBoundsLocal(t.gameObject);
                    if (bounds.HasValue)
                    {
                        var b = bounds.Value;
                        wall.StartLocal = new Vector3(b.min.x, 0f, b.min.z);
                        wall.EndLocal   = new Vector3(b.max.x, 0f, b.max.z);
                    }
                    continue;
                }

                m = ObjectNameRx.Match(t.name);
                if (m.Success)
                {
                    var instance = t.gameObject.GetComponent<RoomObjectInstance>()
                        ?? t.gameObject.AddComponent<RoomObjectInstance>();
                    instance.OwningRoomId = int.Parse(m.Groups[1].Value);
                    instance.Slot         = int.Parse(m.Groups[2].Value);
                    instance.LocalRotationY = t.localRotation.eulerAngles.y * Mathf.Deg2Rad;
                    continue;
                }
            }
        }

        static Bounds? ComputeMeshBoundsLocal(GameObject go)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            return mf.sharedMesh.bounds;
        }
    }
}
