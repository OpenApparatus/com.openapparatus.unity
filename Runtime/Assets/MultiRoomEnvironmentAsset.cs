using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenApparatus.Unity
{
    [Serializable]
    internal struct RoomMeshEntry
    {
        public int RoomId;
        public Mesh Mesh;
    }

    [Serializable]
    public struct RoomNameEntry
    {
        public int RoomId;
        public string Name;
    }

    [Serializable]
    public struct RoomColorEntry
    {
        public int RoomId;
        public Color Color;
    }

    public sealed class MultiRoomEnvironmentAsset : ScriptableObject
    {
        public int SchemaVersion;
        public EnvironmentParameters Parameters = new EnvironmentParameters();
        public RoomData[] Rooms;
        public ObjectSlotDefinition[] ObjectSlots;
        public ObjectInstanceData[] OutsideObjects;

        // Tile -> room-id ownership grid, row-major: RoomGrid[x * GridLength + z].
        // -1 marks an empty tile. The geometry pipeline rebuilds the Core
        // MultiRoomEnvironment from this grid (see EnvironmentTopology).
        public int[] RoomGrid;
        public int GridWidth;
        public int GridLength;

        // Editor-state extras carried by .oapp projects (empty for .oae imports).
        public RoomNameEntry[] RoomNames;
        public RoomColorEntry[] RoomFloorColors;
        public RoomColorEntry[] RoomCeilingColors;
        public RoomColorEntry[] RoomWallColors;

        [SerializeField] private List<RoomMeshEntry> _roomMeshes = new List<RoomMeshEntry>();

        public ColliderMode ColliderMode = ColliderMode.None;
        public PrefabSubstitutionTable Substitution;

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
                    _roomMeshes[i] = new RoomMeshEntry { RoomId = roomId, Mesh = mesh };
                    return;
                }
            }
            _roomMeshes.Add(new RoomMeshEntry { RoomId = roomId, Mesh = mesh });
        }

        public void ClearRoomMeshes() => _roomMeshes.Clear();
    }
}
