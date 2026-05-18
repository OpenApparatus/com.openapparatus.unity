using UnityEngine;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class EnvironmentRoot : MonoBehaviour
    {
        public MultiRoomEnvironmentAsset Asset;
        public Material[] FloorMaterialOverrides;
        public Material[] WallMaterialOverrides;
        public Material[] CeilingMaterialOverrides;

        /// <summary>Every spawned room under this environment.</summary>
        public Room[] Rooms => GetComponentsInChildren<Room>();

        /// <summary>The room with the given id, or null if there is none.</summary>
        public Room GetRoom(int roomId)
        {
            foreach (var room in GetComponentsInChildren<Room>())
                if (room.RoomId == roomId) return room;
            return null;
        }
    }
}
