using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenApparatus.Unity
{
    [Serializable]
    internal struct OApparatusRoomMeshEntry
    {
        public int RoomId;
        public Mesh Mesh;
    }

    [Serializable]
    public struct OApparatusRoomNameEntry
    {
        public int RoomId;
        public string Name;
    }

    [Serializable]
    public struct OApparatusRoomColorEntry
    {
        public int RoomId;
        public Color Color;
    }

    public sealed class OApparatusAsset : ScriptableObject
    {
        public int SchemaVersion;
        public OApparatusParameters Parameters = new OApparatusParameters();
        public OApparatusRoomInfo[] Rooms;
        public OApparatusObjectSlot[] ObjectSlots;
        public OApparatusObjectInfo[] OutsideObjects;

        // Tile -> room-id ownership grid, row-major: RoomGrid[x * GridLength + z].
        // -1 marks an empty tile. The geometry pipeline rebuilds the Core
        // MultiRoomEnvironment from this grid (see OApparatusTopology).
        public int[] RoomGrid;
        public int GridWidth;
        public int GridLength;

        // Editor-state extras carried by .oapp projects (empty for .oae imports).
        public OApparatusRoomNameEntry[] RoomNames;
        public OApparatusRoomColorEntry[] RoomFloorColors;
        public OApparatusRoomColorEntry[] RoomCeilingColors;
        public OApparatusRoomColorEntry[] RoomWallColors;

        [SerializeField] private List<OApparatusRoomMeshEntry> _roomMeshes = new List<OApparatusRoomMeshEntry>();

        public OApparatusColliderMode ColliderMode = OApparatusColliderMode.None;
        public OApparatusSubstitutionTable Substitution;

        public Mesh GetRoomMesh(int roomId)
        {
            for (int i = 0; i < _roomMeshes.Count; i++)
                if (_roomMeshes[i].RoomId == roomId) return _roomMeshes[i].Mesh;
            return null;
        }

        public void SetRoomMesh(int roomId, Mesh mesh)
        {
            for (int i = 0; i < _roomMeshes.Count; i++)
            {
                if (_roomMeshes[i].RoomId == roomId)
                {
                    _roomMeshes[i] = new OApparatusRoomMeshEntry { RoomId = roomId, Mesh = mesh };
                    return;
                }
            }
            _roomMeshes.Add(new OApparatusRoomMeshEntry { RoomId = roomId, Mesh = mesh });
        }

        public void ClearRoomMeshes() => _roomMeshes.Clear();
    }
}
