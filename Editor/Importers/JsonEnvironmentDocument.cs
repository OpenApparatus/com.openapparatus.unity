using System.Collections.Generic;

namespace OpenApparatus.Unity.Editor.Importers
{
    internal sealed class JsonEnvironmentDocument
    {
        public string schema { get; set; }
        public int version { get; set; }
        public JsonParameters parameters { get; set; }
        public JsonGrid grid { get; set; }
        public List<JsonObjectSlot> objectSlots { get; set; }
        public List<JsonRoom> rooms { get; set; }
        public JsonOutside outside { get; set; }
    }

    internal sealed class JsonParameters
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

    internal sealed class JsonGrid
    {
        public int width { get; set; }
        public int length { get; set; }
        public List<List<int>> tiles { get; set; }
    }

    internal sealed class JsonObjectSlot
    {
        public int id { get; set; }
        public string shape { get; set; }
        public List<float> color { get; set; }
        public float size { get; set; }
        public string displayName { get; set; }
        public string objectType { get; set; }
    }

    internal sealed class JsonRoom
    {
        public int id { get; set; }
        public JsonRoomShape shape { get; set; }
        public List<float> position { get; set; }
        public List<List<int>> tiles { get; set; }
        public List<JsonWall> walls { get; set; }
        public List<JsonObjectInstance> objects { get; set; }
    }

    internal sealed class JsonRoomShape
    {
        public string type { get; set; }
        public float width { get; set; }
        public float depth { get; set; }
    }

    internal sealed class JsonWall
    {
        public int number { get; set; }
        public string side { get; set; }
        public List<float> start { get; set; }
        public List<float> end { get; set; }
        public int? neighborRoomId { get; set; }
        public JsonPassage passage { get; set; }
    }

    internal sealed class JsonPassage
    {
        public string type { get; set; }
        public List<JsonOpening> openings { get; set; }
    }

    internal sealed class JsonOpening
    {
        public float offsetAlongEdge { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public float sillHeight { get; set; }
    }

    internal sealed class JsonObjectInstance
    {
        public int slot { get; set; }
        public List<float> position { get; set; }
        public float rotation { get; set; }
    }

    internal sealed class JsonOutside
    {
        public List<JsonObjectInstance> objects { get; set; }
    }
}
