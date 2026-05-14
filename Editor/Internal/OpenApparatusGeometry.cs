using System.Collections.Generic;
using UnityEngine;
using OpenApparatus.Geometry;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity.Editor.Internal
{
    public readonly struct RoomMesh
    {
        public readonly int RoomId;
        public readonly Mesh Mesh;
        public RoomMesh(int roomId, Mesh mesh) { RoomId = roomId; Mesh = mesh; }
    }

    public static class OpenApparatusGeometry
    {
        public static IReadOnlyList<RoomMesh> AssembleMeshes(
            MultiRoomEnvironment plan,
            float wallThickness,
            float wallHeight)
        {
            var assembled = new MultiRoomEnvironmentMeshAssembler()
                .Assemble(plan, wallThickness, wallHeight);

            var result = new List<RoomMesh>(assembled.Count);
            foreach (var cell in assembled)
            {
                var mesh = UnityMeshAdapter.ToUnityMesh(cell.Mesh,
                    $"OpenApparatus_Room_{cell.Room.Id}");
                result.Add(new RoomMesh(cell.Room.Id, mesh));
            }
            return result;
        }
    }
}
