using UnityEngine;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Identifies a spawned room and exposes a scripting API for re-skinning it
    /// and placing objects against its walls. Custom researcher scripts start
    /// from <see cref="EnvironmentRoot"/> and drill in via these methods.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Room : MonoBehaviour
    {
        public int RoomId;
        public RoomType RoomType;
        public Vector2 GridPositionStudio;
        public Vector2Int[] TileIndices;

        /// <summary>This room's walls, each described from the room's interior side.</summary>
        public WallData[] Walls;

        // --- Re-skinning ---

        public void SetFloorColor(Color color)   => TintPart("Floor", color);
        public void SetWallColor(Color color)    => TintPart("Walls", color);
        public void SetCeilingColor(Color color) => TintPart("Ceiling", color);

        public void SetFloorMaterial(Material material)   => SetPartMaterial("Floor", material);
        public void SetWallMaterial(Material material)    => SetPartMaterial("Walls", material);
        public void SetCeilingMaterial(Material material) => SetPartMaterial("Ceiling", material);

        // --- Object placement ---

        /// <summary>
        /// Instantiates <paramref name="prefab"/> against wall <paramref name="wallIndex"/>:
        /// <paramref name="distanceMetres"/> along the wall from its start,
        /// <paramref name="heightMetres"/> above the floor, facing into the room.
        /// Returns the instance, or null if the prefab or wall index is invalid.
        /// </summary>
        public GameObject AddObjectToWall(int wallIndex, float distanceMetres,
                                          float heightMetres, GameObject prefab)
        {
            if (prefab == null || Walls == null || wallIndex < 0 || wallIndex >= Walls.Length)
                return null;

            var wall = Walls[wallIndex];
            var delta = wall.EndLocal - wall.StartLocal;
            float length = delta.magnitude;
            if (length < 1e-4f) return null;
            var direction = delta / length;

            var position = wall.StartLocal
                + direction * Mathf.Clamp(distanceMetres, 0f, length)
                + Vector3.up * heightMetres;

            var instance = Instantiate(prefab, transform);
            instance.transform.localPosition = position;

            var toInterior = InteriorCentroid() - position;
            toInterior.y = 0f;
            if (toInterior.sqrMagnitude > 1e-6f)
                instance.transform.localRotation = Quaternion.LookRotation(toInterior, Vector3.up);
            return instance;
        }

        // Approximate room centre as the average of wall midpoints — exact for
        // a rectangular room enclosed by its outer walls.
        Vector3 InteriorCentroid()
        {
            if (Walls == null || Walls.Length == 0) return Vector3.zero;
            var sum = Vector3.zero;
            foreach (var w in Walls)
                sum += (w.StartLocal + w.EndLocal) * 0.5f;
            return sum / Walls.Length;
        }

        void TintPart(string partName, Color color)
        {
            var renderer = PartRenderer(partName);
            if (renderer == null || renderer.sharedMaterial == null) return;
            var instanced = new Material(renderer.sharedMaterial) { name = renderer.sharedMaterial.name };
            instanced.color = color;
            renderer.sharedMaterial = instanced;
        }

        void SetPartMaterial(string partName, Material material)
        {
            var renderer = PartRenderer(partName);
            if (renderer != null) renderer.sharedMaterial = material;
        }

        MeshRenderer PartRenderer(string partName)
        {
            var part = transform.Find(partName);
            return part != null ? part.GetComponent<MeshRenderer>() : null;
        }
    }
}
