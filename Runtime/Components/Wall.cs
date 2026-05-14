using UnityEngine;

namespace OpenApparatus.Unity
{
    [DisallowMultipleComponent]
    public sealed class Wall : MonoBehaviour
    {
        public int WallNumber;
        public Vector3 StartLocal;
        public Vector3 EndLocal;

        // -1 = outer wall (no neighbour). Sentinel rather than int? for Unity serialisation.
        public int NeighbourRoomId = -1;

        public PassageKind PassageKind;
        public OpeningSpec[] Openings;

        public bool IsOuterWall => NeighbourRoomId < 0;
    }
}
