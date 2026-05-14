using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class ColliderBuilderTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.json";

        [TestCase(ColliderMode.None, 0, 0)]
        [TestCase(ColliderMode.WallsOnly, 4, 0)]
        [TestCase(ColliderMode.FloorsOnly, 0, 4)]
        [TestCase(ColliderMode.All, 4, 4)]
        public void Apply_ProducesExpectedColliderCounts(
            ColliderMode mode, int expectedWallColliders, int expectedFloorColliders)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            asset.ColliderMode = mode;
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                int wallColliders = 0;
                int floorColliders = 0;
                foreach (var col in root.GetComponentsInChildren<BoxCollider>(includeInactive: true))
                {
                    var name = col.gameObject.name;
                    if (name == "WallCollider") wallColliders++;
                    else if (name == "FloorColliders") floorColliders++;
                    else Assert.Fail($"Unexpected BoxCollider parent: {name}");
                }
                Assert.AreEqual(expectedWallColliders, wallColliders, "wall collider count");
                Assert.AreEqual(expectedFloorColliders, floorColliders, "floor tile collider count");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                asset.ColliderMode = ColliderMode.None;
            }
        }
    }
}
