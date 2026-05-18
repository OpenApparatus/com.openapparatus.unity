using NUnit.Framework;
using UnityEngine;
using OpenApparatus.Topology;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class EnvironmentModelBuilderTests
    {
        GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        // Two 1x1 rooms side by side: one internal adjacency, six outer boundaries.
        static (MultiRoomEnvironment plan, int[,] grid) TwoRoomPlan()
        {
            var grid = new int[2, 1];
            grid[0, 0] = 0;
            grid[1, 0] = 1;
            return (MultiRoomEnvironmentBuilder.FromGrid(grid, 3.5f), grid);
        }

        static EnvironmentParameters Params() => new EnvironmentParameters
        {
            TileSize = 3.5f,
            WallThickness = 0.2f,
            WallHeight = 3.0f,
        };

        [Test]
        public void Build_ProducesOneNodePerRoom()
        {
            var (plan, grid) = TwoRoomPlan();
            _root = EnvironmentModelBuilder.Build("Env", plan, grid, Params(),
                                                  new EnvironmentBuildOptions());

            Assert.AreEqual("Env", _root.name);
            Assert.IsNotNull(_root.GetComponent<EnvironmentRoot>());
            Assert.IsNotNull(_root.transform.Find("Room_0"));
            Assert.IsNotNull(_root.transform.Find("Room_1"));
        }

        [Test]
        public void Build_EachRoomHasNonEmptyFloorWallsCeiling()
        {
            var (plan, grid) = TwoRoomPlan();
            _root = EnvironmentModelBuilder.Build("Env", plan, grid, Params(),
                                                  new EnvironmentBuildOptions());

            foreach (var roomName in new[] { "Room_0", "Room_1" })
            {
                var room = _root.transform.Find(roomName);
                foreach (var part in new[] { "Floor", "Walls", "Ceiling" })
                {
                    var child = room.Find(part);
                    Assert.IsNotNull(child, $"{roomName}/{part} missing");
                    var mesh = child.GetComponent<MeshFilter>().sharedMesh;
                    Assert.IsNotNull(mesh, $"{roomName}/{part} has no mesh");
                    Assert.Greater(mesh.vertexCount, 0, $"{roomName}/{part} mesh is empty");
                }
            }
        }

        [Test]
        public void Build_RespectsGenerateToggles()
        {
            var (plan, grid) = TwoRoomPlan();
            var opts = new EnvironmentBuildOptions
            {
                GenerateFloors = true,
                GenerateWalls = false,
                GenerateCeilings = false,
            };
            _root = EnvironmentModelBuilder.Build("Env", plan, grid, Params(), opts);

            var room0 = _root.transform.Find("Room_0");
            Assert.IsNotNull(room0.Find("Floor"));
            Assert.IsNull(room0.Find("Walls"));
            Assert.IsNull(room0.Find("Ceiling"));
        }
    }
}
