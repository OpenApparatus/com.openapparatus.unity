using System;
using UnityEngine;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity
{
    [Serializable]
    public sealed class WallData
    {
        public int Number;
        public Vector3 StartLocal;
        public Vector3 EndLocal;
        public int NeighbourRoomId = -1;
        public PassageKind PassageKind;
        public OpeningSpec[] Openings;
    }

    [Serializable]
    public sealed class ObjectInstanceData
    {
        public int Slot;
        public Vector3 LocalPosition;
        public float LocalRotationY;
    }

    [Serializable]
    public sealed class RoomData
    {
        public int Id;
        public RoomType RoomType;
        public Vector2 GridPositionStudio;
        public Vector2Int[] TileIndices;
        public WallData[] Walls;
        public ObjectInstanceData[] Objects;
    }
}
