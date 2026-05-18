using UnityEngine;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class RoomObjectInstance : MonoBehaviour
    {
        public int Slot;
        public int OwningRoomId;
        public float LocalRotationY;

        /// <summary>The slot's object-type name (from the environment's object
        /// slots), so scripts can query objects by type without the asset.</summary>
        public string ObjectType;
    }
}
