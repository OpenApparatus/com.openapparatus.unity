using System.Collections.Generic;

namespace OpenApparatus.Unity.Editor.Importers
{
    internal sealed class OApparatusOaeDocument
    {
        public string schema { get; set; }
        public int version { get; set; }
        public OApparatusOaeParameters parameters { get; set; }
        public OApparatusOaeGrid grid { get; set; }
        public List<OApparatusOaeObjectSlot> objectSlots { get; set; }
        public List<OApparatusOaeRoom> rooms { get; set; }
        public OApparatusOaeOutside outside { get; set; }
    }

    internal sealed class OApparatusOaeParameters
    {
        public float tileSize { get; set; }
        public float wallThickness { get; set; }
        public float wallHeight { get; set; }
        public float doorWidth { get; set; }
        public float doorHeight { get; set; }
        public float windowWidth { get; set; }
        public float windowHeight { get; set; }
        public float windowSillHeight { get; set; }
        public int gridSubdivision { get; set; }
        public float defaultObjectY { get; set; }
    }

    internal sealed class OApparatusOaeGrid
    {
        public int width { get; set; }
        public int length { get; set; }
        public List<List<int>> tiles { get; set; }
    }

    internal sealed class OApparatusOaeObjectSlot
    {
        public int id { get; set; }
        public string shape { get; set; }
        public List<float> color { get; set; }
        public float size { get; set; }
        public string displayName { get; set; }
        public string objectType { get; set; }
    }

    internal sealed class OApparatusOaeRoom
    {
        public int id { get; set; }
        public OApparatusOaeRoomShape shape { get; set; }
        public List<float> position { get; set; }
        public List<List<int>> tiles { get; set; }
        public List<OApparatusOaeWall> walls { get; set; }
        public List<OApparatusOaeObjectInstance> objects { get; set; }
    }

    internal sealed class OApparatusOaeRoomShape
    {
        public string type { get; set; }
        public float width { get; set; }
        public float depth { get; set; }
    }

    internal sealed class OApparatusOaeWall
    {
        public int number { get; set; }
        public string side { get; set; }
        public List<float> start { get; set; }
        public List<float> end { get; set; }
        public int? neighborRoomId { get; set; }
        public OApparatusOaePassage passage { get; set; }
    }

    internal sealed class OApparatusOaePassage
    {
        public string type { get; set; }
        public List<OApparatusOaeOpening> openings { get; set; }
    }

    internal sealed class OApparatusOaeOpening
    {
        public float offsetAlongEdge { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public float sillHeight { get; set; }
    }

    internal sealed class OApparatusOaeObjectInstance
    {
        public int slot { get; set; }
        public List<float> position { get; set; }
        public float rotation { get; set; }
    }

    internal sealed class OApparatusOaeOutside
    {
        public List<OApparatusOaeObjectInstance> objects { get; set; }
    }
}
