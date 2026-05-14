using UnityEngine;

namespace OpenApparatus.Unity.Internal
{
    public static class OpenApparatusSpace
    {
        public static Vector3 ToUnity(Vector3 studio) =>
            new Vector3(-studio.x, studio.y, studio.z);

        public static Vector2 ToUnityXZ(Vector2 studioXz) =>
            new Vector2(-studioXz.x, studioXz.y);

        public static float YawToUnity(float studioRadiansCcw) =>
            -studioRadiansCcw;
    }
}
