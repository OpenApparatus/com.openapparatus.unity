using UnityEngine;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Scene handle on a generated apparatus prefab. Sits on the prefab root,
    /// above the apparatus geometry, and links the instance back to the
    /// <see cref="OApparatusConfig"/> that built it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OApparatusManager : MonoBehaviour
    {
        public OApparatusConfig Config;
    }
}
