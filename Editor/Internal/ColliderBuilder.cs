using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class ColliderBuilder
    {
        const string WallColliderChildName = "WallCollider";
        const string FloorCollidersChildName = "FloorColliders";
        const float FloorColliderThickness = 0.1f;

        public static void Apply(GameObject environmentRoot,
                                 ColliderMode mode,
                                 EnvironmentParameters parameters)
        {
            if (mode == ColliderMode.None || environmentRoot == null || parameters == null) return;

            bool doWalls  = mode == ColliderMode.WallsOnly  || mode == ColliderMode.All;
            bool doFloors = mode == ColliderMode.FloorsOnly || mode == ColliderMode.All;

            if (doWalls)
                foreach (var wall in environmentRoot.GetComponentsInChildren<Wall>(includeInactive: true))
                    AddWallCollider(wall, parameters);

            if (doFloors)
                foreach (var room in environmentRoot.GetComponentsInChildren<Room>(includeInactive: true))
                    AddFloorColliders(room, parameters);
        }

        static void AddWallCollider(Wall wall, EnvironmentParameters parameters)
        {
            var delta = wall.EndLocal - wall.StartLocal;
            float length = delta.magnitude;
            if (length < 1e-4f) return;

            ClearExistingChild(wall.transform, WallColliderChildName);

            var anchor = new GameObject(WallColliderChildName);
            anchor.transform.SetParent(wall.transform, worldPositionStays: false);

            var midpoint = (wall.StartLocal + wall.EndLocal) * 0.5f;
            midpoint.y += parameters.WallHeight * 0.5f;
            anchor.transform.localPosition = midpoint;

            float yawDeg = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
            anchor.transform.localRotation = Quaternion.Euler(0f, -yawDeg, 0f);

            var col = anchor.AddComponent<BoxCollider>();
            col.size = new Vector3(length, parameters.WallHeight, parameters.WallThickness);
        }

        static void AddFloorColliders(Room room, EnvironmentParameters parameters)
        {
            if (room.TileIndices == null || room.TileIndices.Length == 0) return;

            ClearExistingChild(room.transform, FloorCollidersChildName);

            var holder = new GameObject(FloorCollidersChildName);
            holder.transform.SetParent(room.transform, worldPositionStays: false);

            float t = parameters.TileSize;
            foreach (var idx in room.TileIndices)
            {
                var col = holder.AddComponent<BoxCollider>();
                col.size = new Vector3(t, FloorColliderThickness, t);
                col.center = new Vector3(
                    (idx.x + 0.5f) * t,
                    -FloorColliderThickness * 0.5f,
                    (idx.y + 0.5f) * t);
            }
        }

        static void ClearExistingChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }
    }
}
