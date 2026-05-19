using System;
using UnityEngine;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity
{
    [Serializable]
    public sealed class OApparatusWallInfo
    {
        public int Number;
        public Vector3 StartLocal;
        public Vector3 EndLocal;
        public int NeighbourRoomId = -1;
        public OApparatusPassageKind OApparatusPassageKind;
        public OApparatusOpeningInfo[] Openings;
    }

    [Serializable]
    public sealed class OApparatusObjectInfo
    {
        public int Slot;
        public Vector3 LocalPosition;
        public float LocalRotationY;
    }

    [Serializable]
    public sealed class OApparatusRoomInfo
    {
        public int Id;
        public RoomType RoomType;
        public Vector2 GridPositionStudio;
        public Vector2Int[] TileIndices;
        public OApparatusWallInfo[] Walls;
        public OApparatusObjectInfo[] Objects;
    }
}
