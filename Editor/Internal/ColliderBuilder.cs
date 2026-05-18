using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class ColliderBuilder
    {
        const string WallColliderChildName    = "WallCollider";
        const string FloorColliderChildName   = "FloorCollider";
        const string CeilingColliderChildName = "CeilingCollider";
        const float SurfaceColliderThickness  = 0.1f;

        public static void Apply(GameObject environmentRoot,
                                 ColliderMode mode,
                                 EnvironmentParameters parameters)
        {
            if (mode == ColliderMode.None || environmentRoot == null || parameters == null) return;

            if ((mode & ColliderMode.Walls) != 0)
                foreach (var wall in environmentRoot.GetComponentsInChildren<Wall>(includeInactive: true))
                    AddWallCollider(wall, parameters);

            if ((mode & (ColliderMode.Floors | ColliderMode.Ceilings)) != 0)
                foreach (var room in environmentRoot.GetComponentsInChildren<Room>(includeInactive: true))
                {
                    if ((mode & ColliderMode.Floors) != 0)
                        AddSurfaceCollider(room, "Floor", FloorColliderChildName,
                                           parameters, atCeiling: false);
                    if ((mode & ColliderMode.Ceilings) != 0)
                        AddSurfaceCollider(room, "Ceiling", CeilingColliderChildName,
                                           parameters, atCeiling: true);
                }

            // Placeholder colliders (ColliderMode.Objects) are owned by the
            // spawner, not by this pass. Spawner removes the primitive's
            // default collider when the flag is off; substituted prefabs are
            // never touched (prefab author decides).
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

        // One BoxCollider per tile, all attached to a single child node under
        // the room's Floor or Ceiling surface object. Floor sits a hair below
        // y=0; ceiling sits a hair above wallHeight — both flush with the
        // visible mesh but invisible to the agent.
        static void AddSurfaceCollider(Room room,
                                       string surfaceChildName,
                                       string colliderChildName,
                                       EnvironmentParameters parameters,
                                       bool atCeiling)
        {
            if (room.TileIndices == null || room.TileIndices.Length == 0) return;

            var surface = room.transform.Find(surfaceChildName);
            if (surface == null) return;

            ClearExistingChild(surface, colliderChildName);
            var holder = new GameObject(colliderChildName);
            holder.transform.SetParent(surface, worldPositionStays: false);

            float t = parameters.TileSize;
            float colliderCenterY = atCeiling
                ? parameters.WallHeight + SurfaceColliderThickness * 0.5f
                : -SurfaceColliderThickness * 0.5f;

            // Tile indices are Studio coords; mirror X to share the room's
            // Unity-space frame (same as the floor/ceiling mesh builder).
            foreach (var idx in room.TileIndices)
            {
                var col = holder.AddComponent<BoxCollider>();
                col.size = new Vector3(t, SurfaceColliderThickness, t);
                col.center = new Vector3(
                    -(idx.x + 0.5f) * t,
                    colliderCenterY,
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
