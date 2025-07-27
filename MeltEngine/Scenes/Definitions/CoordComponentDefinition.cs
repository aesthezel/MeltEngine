using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class CoordComponentDefinition
    {
        [JsonPropertyName("position")]
        public Vector3Definition Position { get; set; }

        [JsonPropertyName("rotation")]
        public QuaternionDefinition Rotation { get; set; }

        [JsonPropertyName("scale")]
        public Vector3Definition Scale { get; set; }
    }
}
