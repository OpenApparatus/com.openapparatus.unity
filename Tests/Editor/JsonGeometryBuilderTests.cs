using NUnit.Framework;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class JsonGeometryBuilderTests
    {
        static EnvironmentParameters Params() => new EnvironmentParameters
        {
            TileSize = 3.5f,
            WallThickness = 0.2f,
            WallHeight = 3.0f,
        };

        static RoomData SingleTileRoom() => new RoomData
        {
            Id = 0,
            TileIndices = new[] { new Vector2Int(0, 0) },
        };

        [Test]
        public void RoomInteriorCentroid_SingleTile_IsMirroredTileCentre()
        {
            var p = Params();
            var centroid = JsonGeometryBuilder.RoomInteriorCentroid(SingleTileRoom(), p);
            Assert.AreEqual(new Vector3(-0.5f * p.TileSize, 0f, 0.5f * p.TileSize), centroid);
        }

        [Test]
        public void RoomInteriorCentroid_NoTiles_IsZero()
        {
            var centroid = JsonGeometryBuilder.RoomInteriorCentroid(new RoomData(), Params());
            Assert.AreEqual(Vector3.zero, centroid);
        }

        // The south edge of the single-tile room lies on z = 0 with the room
        // interior at +z. However the wall is wound, the slab must extrude
        // into the room (0 <= z <= thickness), never outward.
        [TestCase(false)]
        [TestCase(true)]
        public void BuildWallMesh_ExtrudesTowardInterior_RegardlessOfWinding(bool reversed)
        {
            var p = Params();
            var room = SingleTileRoom();
            var west = new Vector3(-p.TileSize, 0f, 0f);
            var east = new Vector3(0f, 0f, 0f);
            var wall = new WallData
            {
                Number = 0,
                StartLocal = reversed ? east : west,
                EndLocal = reversed ? west : east,
            };

            var mesh = JsonGeometryBuilder.BuildWallMesh(wall, room, p, "test");

            foreach (var v in mesh.vertices)
            {
                Assert.GreaterOrEqual(v.z, -1e-4f);
                Assert.LessOrEqual(v.z, p.WallThickness + 1e-4f);
            }
        }
    }
}
