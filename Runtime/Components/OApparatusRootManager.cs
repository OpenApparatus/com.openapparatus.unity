using UnityEngine;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class OApparatusRootManager : MonoBehaviour
    {
        public OApparatusAsset Asset;
        public Material[] FloorMaterialOverrides;
        public Material[] WallMaterialOverrides;
        public Material[] CeilingMaterialOverrides;

        /// <summary>Every spawned room under this environment.</summary>
        public OApparatusRoomManager[] Rooms => GetComponentsInChildren<OApparatusRoomManager>();

        /// <summary>The room with the given id, or null if there is none.</summary>
        public OApparatusRoomManager GetRoom(int roomId)
        {
            foreach (var room in GetComponentsInChildren<OApparatusRoomManager>())
                if (room.RoomId == roomId) return room;
            return null;
        }
    }
}
