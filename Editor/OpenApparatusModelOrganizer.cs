using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OpenApparatus.EditorTools
{
    /// <summary>
    /// Reorganizes an imported OpenApparatus OBJ so each room's floor / ceiling /
    /// wall meshes sit under a per-room parent GameObject. Unity's built-in OBJ
    /// importer flattens every group into siblings under the model root; this
    /// post-processes that flat hierarchy into one parent per room.
    ///
    /// Usage: select the imported model's root GameObject in the scene (the one
    /// whose children are named `room_0_floor`, `room_0_wall_1`, etc.) and run
    /// GameObject → OpenApparatus → Group by Room.
    /// </summary>
    public static class OpenApparatusModelOrganizer
    {
        const string MenuPath = "GameObject/OpenApparatus/Group by Room";

        [MenuItem(MenuPath, false, 10)]
        static void GroupByRoom(MenuCommand command)
        {
            // The MenuCommand fires once per selected object when used from the
            // GameObject menu; guard so we only process the right one.
            var root = command.context as GameObject;
            if (root == null || command.context != Selection.activeObject) return;

            int grouped = Apply(root);
            if (grouped == 0)
            {
                EditorUtility.DisplayDialog(
                    "OpenApparatus",
                    $"No children of '{root.name}' look like OpenApparatus pieces. " +
                    "Expected names like 'room_0_floor', 'room_0_wall_1'.",
                    "OK");
            }
            else
            {
                Debug.Log($"[OpenApparatus] Grouped {grouped} room(s) under '{root.name}'.", root);
            }
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateGroupByRoom(MenuCommand command) =>
            command.context is GameObject;

        /// <summary>
        /// Reparents children of <paramref name="root"/> matching `room_&lt;id&gt;_*`
        /// under per-room parents. Returns the number of room parents created.
        /// </summary>
        public static int Apply(GameObject root)
        {
            if (root == null) return 0;

            var byRoom = new Dictionary<int, List<Transform>>();
            // Snapshot — we'll mutate the hierarchy below.
            var children = new List<Transform>(root.transform.childCount);
            foreach (Transform c in root.transform) children.Add(c);

            foreach (var child in children)
            {
                if (!TryParseRoomId(child.name, out int id)) continue;
                if (!byRoom.TryGetValue(id, out var list))
                    byRoom[id] = list = new List<Transform>();
                list.Add(child);
            }
            if (byRoom.Count == 0) return 0;

            Undo.RegisterFullObjectHierarchyUndo(root, "Group by Room");

            // Sort room ids so the new parents appear in stable order.
            var ids = new List<int>(byRoom.Keys);
            ids.Sort();

            foreach (var id in ids)
            {
                var parentName = $"Room_{id}";
                var existing = root.transform.Find(parentName);
                GameObject parent;
                if (existing != null)
                {
                    parent = existing.gameObject;
                }
                else
                {
                    parent = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parent, "Group by Room");
                    parent.transform.SetParent(root.transform, worldPositionStays: false);
                }

                foreach (var child in byRoom[id])
                {
                    Undo.SetTransformParent(child, parent.transform, "Group by Room");
                }
            }
            return byRoom.Count;
        }

        static bool TryParseRoomId(string name, out int id)
        {
            id = -1;
            const string prefix = "room_";
            if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix)) return false;
            int sep = name.IndexOf('_', prefix.Length);
            if (sep < 0) return false;
            return int.TryParse(name.Substring(prefix.Length, sep - prefix.Length), out id);
        }
    }
}
