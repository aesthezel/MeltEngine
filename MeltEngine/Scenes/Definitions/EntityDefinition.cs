using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class EntityDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // Could be used for prefab types or specific entity logic

        [JsonPropertyName("components")]
        public Dictionary<string, JsonElement> Components { get; set; }
    }
}
