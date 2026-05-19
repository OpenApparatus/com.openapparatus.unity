using UnityEngine;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Marks a spawned object slot and carries its <see cref="OApparatusObjectInfo"/>
    /// — placement plus the identity strings authored in Studio. The spawn
    /// pipeline fills <see cref="Info"/> when the component is added.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OApparatusObjectManager : MonoBehaviour
    {
        public OApparatusObjectInfo Info = new OApparatusObjectInfo();

        public int Slot => Info != null ? Info.Slot : 0;
        public int OwningRoomId => Info != null ? Info.OwningRoomId : -1;
        public float LocalRotationY => Info != null ? Info.LocalRotationY : 0f;

        /// <summary>The slot's object-type name, so scripts can query objects by
        /// type without the source asset.</summary>
        public string ObjectType => Info != null ? Info.ObjectType : null;
    }
}
