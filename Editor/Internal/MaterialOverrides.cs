using System.Collections.Generic;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public sealed class MaterialOverrides
    {
        public Dictionary<string, Material> ByStudioName;
        public Material FloorDefault;
        public Material WallDefault;
        public Material CeilingDefault;
    }
}
