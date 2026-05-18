using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class RoomComponentTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oae";

        [Test]
        public void EnvironmentRoot_GetRoom_ReturnsTheRoom()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var envRoot = root.GetComponent<EnvironmentRoot>();
                Assert.IsNotNull(envRoot.GetRoom(0));
                Assert.IsNull(envRoot.GetRoom(999));
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }

        [Test]
        public void Room_CarriesItsWalls_AfterSpawn()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var room = root.GetComponent<EnvironmentRoot>().GetRoom(0);
                Assert.IsNotNull(room.Walls);
                Assert.AreEqual(4, room.Walls.Length);
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }

        [Test]
        public void Room_AddObjectToWall_ParentsInstanceToRoom()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var room = root.GetComponent<EnvironmentRoot>().GetRoom(0);
                var placed = room.AddObjectToWall(0, 1f, 1f, prefab);
                Assert.IsNotNull(placed);
                Assert.AreEqual(room.transform, placed.transform.parent);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Spawn_SetsObjectTypeOnInstance()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var obj = root.GetComponentInChildren<RoomObjectInstance>();
                Assert.AreEqual("Cup", obj.ObjectType);
            }
            finally { if (root != null) Object.DestroyImmediate(root); }
        }
    }
}
