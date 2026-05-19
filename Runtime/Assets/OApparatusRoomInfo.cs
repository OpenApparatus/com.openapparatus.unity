using System;
using UnityEngine;
using OpenApparatus.Topology;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// One wall of a room, described from that room's interior side. Mirrors a
    /// wall entry in the apparatus JSON.
    /// </summary>
    [Serializable]
    public sealed class OApparatusWallInfo
    {
        public int Number;
        public Vector3 StartLocal;
        public Vector3 EndLocal;

        // -1 = outer wall (no neighbouring room).
        public int NeighbourRoomId = -1;
        public OApparatusPassageKind PassageKind;
        public OApparatusOpeningInfo[] Openings;
    }

    /// <summary>
    /// One placed object. Mirrors an object entry in the apparatus JSON,
    /// including the identity strings authored in Studio.
    /// <see cref="OwningRoomId"/> and <see cref="ObjectType"/> are resolved when
    /// the object is spawned and are not part of the source file.
    /// </summary>
    [Serializable]
    public sealed class OApparatusObjectInfo
    {
        public int Slot;
        public Vector3 LocalPosition;
        public float LocalRotationY;

        // Identity strings authored in Studio (see Core's RoomObject). Not
        // unique — two objects may legitimately share any of them.
        public string GlobalId;
        public string TypeId;
        public string CustomId;
        public string Name;

        // Resolved at spawn time.
        public int OwningRoomId = -1;
        public string ObjectType;
    }

    /// <summary>
    /// One room: its identity, grid placement, walls and objects. Mirrors a
    /// room entry in the apparatus JSON.
    /// </summary>
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
