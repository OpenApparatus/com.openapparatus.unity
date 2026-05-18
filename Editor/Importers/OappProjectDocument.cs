using System.Collections.Generic;

namespace OpenApparatus.Unity.Editor.Importers
{
    // Serializable mirror of openapparatus-core's ProjectFile (.oapp schema v1).
    // Field names are camelCase to match ProjectIO's serializer; Newtonsoft is
    // case-insensitive on read, so casing is not load-bearing. Only the fields
    // the Unity importer consumes are mirrored.
    internal sealed class OappProjectDocument
    {
        public string version;
        public string title;

        public int gridWidth;
        public int gridLength;
        public int[] roomGrid;

        public float tileSize;
        public float wallThickness;
        public float wallHeight;
        public float doorWidth;
        public float doorHeight;
        public float windowWidth;
        public float windowHeight;
        public float windowSillHeight;
        public int gridSubdivision;
        public float defaultObjectY;

        public List<OappPassageOverride> passageOverrides;
        public List<OappObjectType> objectTypes;
        public List<OappObjectInstance> objects;

        public Dictionary<int, string> roomNames;
        public Dictionary<int, float[]> roomFloorColors;
        public Dictionary<int, float[]> roomCeilingColors;
        public Dictionary<int, float[]> roomSingleWallColors;
    }

    internal sealed class OappPassageOverride
    {
        public float startX;
        public float startZ;
        public float endX;
        public float endZ;
        public string kind;          // Closed / Open / Doorway
        public List<OappOpening> openings;
    }

    internal sealed class OappOpening
    {
        public float offset;
        public float width;
        public float height;
        public float sillHeight;
        public bool hingeAtEnd;
        public bool swingNegative;
    }

    internal sealed class OappObjectType
    {
        public string name;
        public string shape;
        public float[] color;
        public float size;
    }

    internal sealed class OappObjectInstance
    {
        public int slot;
        public int owningRoomId;
        public float x;
        public float y;
        public float z;
        public float rotation;
    }
}
