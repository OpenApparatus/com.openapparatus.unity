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

    public sealed class MultiRoomEnvironmentAsset : ScriptableObject
    {
        public int SchemaVersion;
        public EnvironmentParameters Parameters = new EnvironmentParameters();
        public RoomData[] Rooms;
        public ObjectSlotDefinition[] ObjectSlots;
        public ObjectInstanceData[] OutsideObjects;

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
