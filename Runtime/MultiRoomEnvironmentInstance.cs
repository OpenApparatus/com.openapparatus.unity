using UnityEngine;
using OpenApparatus;
using OpenApparatus.Geometry;
using OpenApparatus.Topology;
using OpenApparatus.Topology.Assigners;
using OpenApparatus.Topology.Generators;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Drop this component on an empty GameObject, configure parameters, and
    /// the component spawns one child GameObject per generated room with a
    /// MeshFilter + MeshRenderer. Live-regenerates in the editor whenever
    /// any parameter changes.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class MultiRoomEnvironmentInstance : MonoBehaviour
    {
        public enum StartingRoomTypeChoice { NoPreference, Square, Rectangle }

        [Header("Floor dimensions")]
        [Min(1)] public int floorWidthCells = 4;
        [Min(1)] public int floorLengthCells = 4;
        [Min(0.5f)] public float tileSize = 3.5f;

        [Header("Rooms")]
        [Min(0)] public int rectangleRoomCount = 0;
        public RectangleOrientation rectangleOrientation = RectangleOrientation.Random;

        [Header("Walls")]
        [Min(0.05f)] public float wallThickness = 0.2f;
        [Min(0.5f)] public float wallHeight = 3f;

        [Header("Doorways")]
        public bool includeOuterEntrance = true;
        public StartingRoomTypeChoice startingRoomType = StartingRoomTypeChoice.NoPreference;
        [Min(0.5f)] public float doorWidth = 1.2f;
        [Min(1.5f)] public float doorHeight = 2.2f;

        [Header("Determinism")]
        public int seed = 42;

        [Header("Materials (optional)")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material ceilingMaterial;

        [Header("Editor")]
        [Tooltip("Auto-rebuild when any field above changes (editor only).")]
        public bool autoRegenerateInEditor = true;

        const string GeneratedChildName = "Generated";

#if UNITY_EDITOR
        [System.NonSerialized] bool _regenPending;

        void OnValidate()
        {
            if (Application.isPlaying) return;
            if (!autoRegenerateInEditor) return;
            ScheduleRegenerate();
        }

        void OnEnable()
        {
            if (Application.isPlaying) return;
            if (!autoRegenerateInEditor) return;
            ScheduleRegenerate();
        }

        internal void ScheduleRegenerate()
        {
            if (_regenPending) return;
            _regenPending = true;
            EditorApplication.delayCall += DeferredRegenerate;
        }

        void DeferredRegenerate()
        {
            _regenPending = false;
            if (this == null) return;
            if (Application.isPlaying) return;
            if (!isActiveAndEnabled) return;
            Regenerate();
        }
#endif

        public void Regenerate()
        {
#if UNITY_EDITOR
            ClearGenerated();
#else
            // Runtime: also clear if there's something there.
            var existing = transform.Find(GeneratedChildName);
            if (existing != null) DestroyImmediate(existing.gameObject);
#endif

            MultiRoomEnvironment plan;
            try
            {
                var gen = new GridDominoGenerator
                {
                    FloorWidthCells = floorWidthCells,
                    FloorLengthCells = floorLengthCells,
                    RectangleRoomCount = rectangleRoomCount,
                    TileSize = tileSize,
                    Orientation = rectangleOrientation,
                };
                plan = gen.Generate(new SeededRandom(seed));
                new SpanningTreePassageAssigner
                {
                    IncludeOuterEntrance = includeOuterEntrance,
                    DoorWidth = doorWidth,
                    DoorHeight = doorHeight,
                    PreferEntranceRoomType = startingRoomType switch
                    {
                        StartingRoomTypeChoice.Square => RoomType.Square,
                        StartingRoomTypeChoice.Rectangle => RoomType.Rectangle,
                        _ => null,
                    },
                }.Assign(plan, new SeededRandom(seed));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MultiRoomEnvironmentInstance] Generation failed: {ex.Message}", this);
                return;
            }

            var meshes = new MultiRoomEnvironmentMeshAssembler().Assemble(plan, wallThickness, wallHeight);

            var root = new GameObject(GeneratedChildName);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(root, "Regenerate Floor Plan");
            Undo.SetTransformParent(root.transform, transform, "Regenerate Floor Plan");
#else
            root.transform.SetParent(transform, false);
#endif
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            foreach (var assembled in meshes)
                SpawnCell(root.transform, assembled);
        }

        public void ClearGenerated()
        {
            var existing = transform.Find(GeneratedChildName);
            if (existing == null) return;
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(existing.gameObject);
#else
            DestroyImmediate(existing.gameObject);
#endif
        }

        void SpawnCell(Transform parent, AssembledRoomMesh assembled)
        {
            var go = new GameObject($"Cell_{assembled.Room.Id}_{assembled.Room.RoomType}",
                typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);

            var mesh = UnityMeshAdapter.ToUnityMesh(assembled.Mesh,
                $"FloorPlan_Cell_{assembled.Room.Id}");
            go.GetComponent<MeshFilter>().sharedMesh = mesh;

            var mr = go.GetComponent<MeshRenderer>();
            // Materials must match submesh order: 0=Floor, 1=Walls, 2=Ceiling.
            mr.sharedMaterials = new[] { floorMaterial, wallMaterial, ceilingMaterial };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(go, "Regenerate Floor Plan Room");
#endif
        }
    }
}
