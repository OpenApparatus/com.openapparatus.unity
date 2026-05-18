using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class ColliderBuilderTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oae";

        // Each selected part (Floor / Walls / Ceiling) gets one MeshCollider;
        // the single-room fixture therefore yields one collider per flag.
        // Object placeholder colliders are not MeshColliders and are named
        // Object_*, so they never count here.
        [TestCase(ColliderMode.None,                        0, 0, 0)]
        [TestCase(ColliderMode.Walls,                       1, 0, 0)]
        [TestCase(ColliderMode.Floors,                      0, 1, 0)]
        [TestCase(ColliderMode.Ceilings,                    0, 0, 1)]
        [TestCase(ColliderMode.Walls | ColliderMode.Floors, 1, 1, 0)]
        [TestCase(ColliderMode.All,                         1, 1, 1)]
        public void Apply_AddsMeshCollidersToSelectedParts(
            ColliderMode mode,
            int expectedWallColliders,
            int expectedFloorColliders,
            int expectedCeilingColliders)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            asset.ColliderMode = mode;
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                int wallColliders = 0;
                int floorColliders = 0;
                int ceilingColliders = 0;
                foreach (var col in root.GetComponentsInChildren<MeshCollider>(includeInactive: true))
                {
                    switch (col.gameObject.name)
                    {
                        case "Walls":   wallColliders++;    break;
                        case "Floor":   floorColliders++;   break;
                        case "Ceiling": ceilingColliders++; break;
                    }
                }
                Assert.AreEqual(expectedWallColliders, wallColliders, "wall collider count");
                Assert.AreEqual(expectedFloorColliders, floorColliders, "floor collider count");
                Assert.AreEqual(expectedCeilingColliders, ceilingColliders, "ceiling collider count");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                asset.ColliderMode = ColliderMode.None;
            }
        }

        [Test]
        public void Placeholder_KeepsColliderWhenObjectsFlagSet()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            asset.ColliderMode = ColliderMode.Objects;
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var placeholder = root.GetComponentInChildren<RoomObjectInstance>();
                Assert.IsNotNull(placeholder);
                Assert.IsNotNull(placeholder.GetComponentInChildren<Collider>(),
                    "Placeholder should keep its default primitive collider when Objects flag is on.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                asset.ColliderMode = ColliderMode.None;
            }
        }

        [Test]
        public void Placeholder_DropsColliderWhenObjectsFlagOff()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            asset.ColliderMode = ColliderMode.None;
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var placeholder = root.GetComponentInChildren<RoomObjectInstance>();
                Assert.IsNotNull(placeholder);
                Assert.IsNull(placeholder.GetComponentInChildren<Collider>(),
                    "Placeholder should have its primitive collider removed when Objects flag is off.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SubstitutedPrefab_ColliderIsPrefabAuthorsChoice()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            asset.ColliderMode = ColliderMode.Objects;  // Should not influence prefabs.

            // Bare prefab with no collider — substitution should NOT add one.
            var bare = new GameObject("BarePrefab", typeof(MeshFilter), typeof(MeshRenderer));
            var table = ScriptableObject.CreateInstance<PrefabSubstitutionTable>();
            table.Entries = new[]
            {
                new SubstitutionEntry { ObjectType = "Cup", Prefab = bare,
                    ScaleMultiplier = Vector3.one }
            };
            asset.Substitution = table;

            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var instance = root.GetComponentInChildren<RoomObjectInstance>();
                Assert.IsNotNull(instance);
                Assert.IsNull(instance.GetComponentInChildren<Collider>(),
                    "Objects flag must not add a collider to a substituted prefab.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                Object.DestroyImmediate(bare);
                asset.Substitution = null;
                Object.DestroyImmediate(table);
                asset.ColliderMode = ColliderMode.None;
            }
        }
    }
}
