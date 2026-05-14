using System;
using Newtonsoft.Json.Linq;

namespace OpenApparatus.Unity.Editor.Importers
{
    internal static class JsonEnvironmentDiscriminator
    {
        public const string SchemaMarker = "openapparatus.environment";

        public static bool IsOpenApparatus(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var root = JToken.Parse(json) as JObject;
                if (root == null) return false;

                var schema = root["schema"]?.Type == JTokenType.String
                    ? (string)root["schema"]
                    : null;
                if (schema == SchemaMarker) return true;

                var version = root["version"];
                if (version == null || version.Type != JTokenType.Integer ||
                    (int)version != 3)
                {
                    return false;
                }

                if (root["parameters"] == null) return false;
                if (root["grid"] == null) return false;
                var rooms = root["rooms"];
                return rooms != null && rooms.Type == JTokenType.Array;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
