using System;
using UnityEngine;

namespace OpenApparatus.Unity
{
    [Serializable]
    public sealed class ObjectSlotDefinition
    {
        public int Id;
        public string Shape;
        public Color Color = Color.white;
        public float Size = 0.3f;
        public string DisplayName;
        public string ObjectType;
    }
}
