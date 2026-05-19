using System;

namespace OpenApparatus.Unity
{
    [Serializable]
    public sealed class OApparatusParameters
    {
        public float TileSize = 3.5f;
        public float WallThickness = 0.2f;
        public float WallHeight = 3.0f;
        public float DoorWidth = 1.2f;
        public float DoorHeight = 2.2f;
        public float WindowWidth = 1.0f;
        public float WindowHeight = 1.2f;
        public float WindowSillHeight = 0.9f;
        public int GridSubdivision = 1;
        public float DefaultObjectY;
    }
}
