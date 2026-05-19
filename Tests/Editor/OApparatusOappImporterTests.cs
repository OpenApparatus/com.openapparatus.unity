using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class OApparatusOappImporterTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oapp";

        [Test]
        public void Import_ProducesApparatusAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            Assert.IsNotNull(asset, "single_room.oapp should import as OApparatusAsset.");
            Assert.AreEqual(1, asset.Rooms.Length);
            Assert.AreEqual(4, asset.Rooms[0].Walls.Length);
            Assert.AreEqual(1, asset.Rooms[0].Objects.Length);
        }

        [Test]
        public void Import_StoresRoomGrid()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            Assert.AreEqual(2, asset.GridWidth);
            Assert.AreEqual(2, asset.GridLength);
            Assert.AreEqual(4, asset.RoomGrid.Length);
        }

        [Test]
        public void Import_AppliesDoorwayFromPassageOverride()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            int doorways = 0;
            foreach (var w in asset.Rooms[0].Walls)
                if (w.OApparatusPassageKind == OApparatusPassageKind.Doorway) doorways++;
            Assert.AreEqual(1, doorways, "the passage override should produce exactly one doorway");
        }

        [Test]
        public void Import_StoresRoomNamesAndColors()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            Assert.AreEqual(1, asset.RoomNames.Length);
            Assert.AreEqual(0, asset.RoomNames[0].RoomId);
            Assert.AreEqual("TestRoom", asset.RoomNames[0].Name);
            Assert.AreEqual(1, asset.RoomFloorColors.Length);
            Assert.AreEqual(1, asset.RoomCeilingColors.Length);
            Assert.AreEqual(1, asset.RoomWallColors.Length);
        }

        [Test]
        public void Spawn_AppliesRoomNameToGameObject()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = OApparatusSpawner.Spawn(asset);
                Assert.IsNotNull(root.transform.Find("Room_0_TestRoom"),
                    "the room GameObject should carry its .oapp name");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Spawn_ProducesRoomWithParts()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OApparatusAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = OApparatusSpawner.Spawn(asset);
                Assert.IsNotNull(root);
                Assert.AreEqual(1, root.GetComponentsInChildren<OApparatusRoomManager>().Length);
                var room = root.GetComponentInChildren<OApparatusRoomManager>().transform;
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
