using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;
using OpenApparatus.Unity.Editor.Importers;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class JsonEnvironmentImporterTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.json";
        const string ForeignPath = "Packages/com.openapparatus.unity/Tests/Fixtures/foreign.json";

        [Test]
        public void Discriminator_AcceptsOpenApparatusJson()
        {
            var text = File.ReadAllText(FixturePath);
            Assert.IsTrue(JsonEnvironmentDiscriminator.IsOpenApparatus(text));
        }

        [Test]
        public void Discriminator_RejectsForeignJson()
        {
            var text = File.ReadAllText(ForeignPath);
            Assert.IsFalse(JsonEnvironmentDiscriminator.IsOpenApparatus(text));
        }

        [Test]
        public void Discriminator_RejectsMalformedJson()
        {
            Assert.IsFalse(JsonEnvironmentDiscriminator.IsOpenApparatus("{ not json"));
        }

        [Test]
        public void Import_ProducesMultiRoomEnvironmentAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            Assert.IsNotNull(asset, "single_room.json should import as MultiRoomEnvironmentAsset.");
            Assert.AreEqual(1, asset.Rooms.Length);
            Assert.AreEqual(4, asset.Rooms[0].Walls.Length);
            Assert.AreEqual(1, asset.Rooms[0].Objects.Length);
        }

        [Test]
        public void Import_RouteWallPositionsThroughOpenApparatusSpace()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            var firstWall = asset.Rooms[0].Walls[0];
            // South wall in fixture: start [0,0] -> end [7,0] in Studio coords.
            // ToUnity negates X, so endLocal.x should be -7, startLocal.x should be 0.
            Assert.AreEqual(0f, firstWall.StartLocal.x, 1e-4f);
            Assert.AreEqual(-7f, firstWall.EndLocal.x, 1e-4f);
        }

        [Test]
        public void Import_RecognisesDoorwayPassage()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            var northWall = asset.Rooms[0].Walls[2];
            Assert.AreEqual(PassageKind.Doorway, northWall.PassageKind);
            Assert.AreEqual(1, northWall.Openings.Length);
            Assert.AreEqual(1.2f, northWall.Openings[0].Width, 1e-4f);
        }

        [Test]
        public void Spawn_ProducesExpectedHierarchy()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                Assert.IsNotNull(root);
                Assert.IsNotNull(root.GetComponent<EnvironmentRoot>());
                Assert.AreEqual(1, root.GetComponentsInChildren<Room>().Length);
                Assert.AreEqual(4, root.GetComponentsInChildren<Wall>().Length);
                Assert.AreEqual(1, root.GetComponentsInChildren<RoomObjectInstance>().Length);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }
    }
}
