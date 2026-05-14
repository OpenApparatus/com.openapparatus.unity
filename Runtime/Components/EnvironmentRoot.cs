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
    }
}
