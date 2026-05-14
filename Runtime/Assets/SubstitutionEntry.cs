using System;
using UnityEngine;

namespace OpenApparatus.Unity
{
    [Serializable]
    public struct SubstitutionEntry
    {
        public string ObjectType;
        public GameObject Prefab;
        public Vector3 PositionOffset;
        public float RotationOffsetYDegrees;
        public Vector3 ScaleMultiplier;
    }
}
