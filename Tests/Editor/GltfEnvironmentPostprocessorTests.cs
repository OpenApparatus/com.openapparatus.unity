using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class GltfEnvironmentPostprocessorTests
    {
        [Test]
        public void AttachComponents_RecognisesRoomWallObjectNodes()
        {
            var root = new GameObject("Model");
            try
            {
                var room0  = MakeChild(root.transform, "Room_0");
                var floor  = MakeChild(room0.transform, "Room_0_floor");
                var wall1  = MakeChild(room0.transform, "Room_0_wall_1");
                var wall2  = MakeChild(room0.transform, "Room_0_wall_2");
                var obj    = MakeChild(room0.transform, "Room_0_object_3_0");

                InvokeAttachComponents(root.transform);

                var roomComp = room0.GetComponent<Room>();
                Assert.IsNotNull(roomComp, "Room_0 should receive a Room component");
                Assert.AreEqual(0, roomComp.RoomId);

                var wall1Comp = wall1.GetComponent<Wall>();
                Assert.IsNotNull(wall1Comp);
                Assert.AreEqual(1, wall1Comp.WallNumber);

                var wall2Comp = wall2.GetComponent<Wall>();
                Assert.IsNotNull(wall2Comp);
                Assert.AreEqual(2, wall2Comp.WallNumber);

                var instance = obj.GetComponent<RoomObjectInstance>();
                Assert.IsNotNull(instance);
                Assert.AreEqual(0, instance.OwningRoomId);
                Assert.AreEqual(3, instance.Slot);

                Assert.IsNull(floor.GetComponent<Room>(),
                    "Floor nodes should not get Room components.");
                Assert.IsNull(floor.GetComponent<Wall>(),
                    "Floor nodes should not get Wall components.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AttachComponents_IgnoresForeignHierarchy()
        {
            var root = new GameObject("Model");
            try
            {
                MakeChild(root.transform, "RandomMesh");
                MakeChild(root.transform, "SomeArmature");
                InvokeAttachComponents(root.transform);

                foreach (Transform t in root.GetComponentsInChildren<Transform>())
                {
                    Assert.IsNull(t.gameObject.GetComponent<Room>());
                    Assert.IsNull(t.gameObject.GetComponent<Wall>());
                    Assert.IsNull(t.gameObject.GetComponent<RoomObjectInstance>());
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static GameObject MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        static void InvokeAttachComponents(Transform root)
        {
            var type = typeof(OpenApparatus.Unity.Editor.Importers.JsonEnvironmentImporter).Assembly
                .GetType("OpenApparatus.Unity.Editor.Importers.GltfEnvironmentPostprocessor");
            Assert.IsNotNull(type, "GltfEnvironmentPostprocessor type not found.");
            var method = type.GetMethod("AttachComponents",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "AttachComponents method not found.");
            method.Invoke(null, new object[] { root });
        }
    }
}
