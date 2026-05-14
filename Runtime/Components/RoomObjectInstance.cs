using UnityEngine;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class RoomObjectInstance : MonoBehaviour
    {
        public int Slot;
        public int OwningRoomId;
        public float LocalRotationY;
    }
}
