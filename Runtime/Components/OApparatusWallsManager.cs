using UnityEngine;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Sits on a room's combined "Walls" mesh part and carries that room's
    /// per-wall data, each wall described from the room's interior side. The
    /// spawn pipeline fills <see cref="Walls"/> when the component is added.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OApparatusWallsManager : MonoBehaviour
    {
        public OApparatusWallInfo[] Walls;
    }
}
