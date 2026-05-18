using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class OappProjectImporterTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oapp";

        [Test]
        public void Import_ProducesMultiRoomEnvironmentAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            Assert.IsNotNull(asset, "single_room.oapp should import as MultiRoomEnvironmentAsset.");
            Assert.AreEqual(1, asset.Rooms.Length);
            Assert.AreEqual(4, asset.Rooms[0].Walls.Length);
            Assert.AreEqual(1, asset.Rooms[0].Objects.Length);
        }

        [Test]
        public void Import_StoresRoomGrid()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            Assert.AreEqual(2, asset.GridWidth);
            Assert.AreEqual(2, asset.GridLength);
            Assert.AreEqual(4, asset.RoomGrid.Length);
        }

        [Test]
        public void Import_AppliesDoorwayFromPassageOverride()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            int doorways = 0;
            foreach (var w in asset.Rooms[0].Walls)
                if (w.PassageKind == PassageKind.Doorway) doorways++;
            Assert.AreEqual(1, doorways, "the passage override should produce exactly one doorway");
        }

        [Test]
        public void Spawn_ProducesRoomWithParts()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                Assert.IsNotNull(root);
                Assert.AreEqual(1, root.GetComponentsInChildren<Room>().Length);
                var room = root.GetComponentInChildren<Room>().transform;
                Assert.IsNotNull(room.Find("Floor"), "room should have a Floor part");
                Assert.IsNotNull(room.Find("Walls"), "room should have a Walls part");
                Assert.IsNotNull(room.Find("Ceiling"), "room should have a Ceiling part");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }
    }
}
