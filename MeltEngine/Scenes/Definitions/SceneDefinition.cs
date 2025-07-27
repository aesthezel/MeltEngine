using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class SceneDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("entities")]
        public List<EntityDefinition> Entities { get; set; }
    }
}
