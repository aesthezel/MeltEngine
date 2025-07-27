using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class QuaternionDefinition
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonPropertyName("w")]
        public float W { get; set; }
    }
}
