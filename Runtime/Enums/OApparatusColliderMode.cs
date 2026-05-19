using System;

namespace OpenApparatus.Unity
{
    [Flags]
    public enum OApparatusColliderMode
    {
        None     = 0,
        Walls    = 1 << 0,
        Floors   = 1 << 1,
        Ceilings = 1 << 2,
        Objects  = 1 << 3,
        All      = Walls | Floors | Ceilings | Objects,
    }
}
