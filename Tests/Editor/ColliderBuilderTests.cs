using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class ColliderBuilderTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oae";

        // Object collider count is tracked separately because primitive
        // placeholders already carry their own collider from CreatePrimitive
        // (one per RoomObjectInstance, regardless of mode); only the Objects
        // flag *additionally* ensures substituted prefabs without colliders
        // get one. The fixture has no substitution, so toggling Objects does
        // not change the unprimitive-collider count.
        [TestCase(ColliderMode.None,                                   0, 0, 0)]
        [TestCase(ColliderMode.Walls,                                  4, 0, 0)]
        [TestCase(ColliderMode.Floors,                                 0, 4, 0)]
        [TestCase(ColliderMode.Ceilings,                               0, 0, 4)]
        [TestCase(ColliderMode.Walls | ColliderMode.Floors,            4, 4, 0)]
        [TestCase(ColliderMode.All,                                    4, 4, 4)]
        public void Apply_ProducesExpectedColliderCounts(
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
                foreach (var col in root.GetComponentsInChildren<BoxCollider>(includeInactive: true))
                {
                    var name = col.gameObject.name;
                    if      (name == "WallCollider")    wallColliders++;
                    else if (name == "FloorCollider")   floorColliders++;
                    else if (name == "CeilingCollider") ceilingColliders++;
                    else Assert.Fail($"Unexpected BoxCollider parent: {name}");
                }
                Assert.AreEqual(expectedWallColliders, wallColliders, "wall collider count");
                Assert.AreEqual(expectedFloorColliders, floorColliders, "floor tile collider count");
                Assert.AreEqual(expectedCeilingColliders, ceilingColliders, "ceiling tile collider count");
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
                Assert.IsNotNull(placeholder.GetComponent<Collider>(),
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
                Assert.IsNull(placeholder.GetComponent<Collider>(),
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
