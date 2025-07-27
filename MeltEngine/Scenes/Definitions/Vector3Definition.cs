using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class Vector3Definition
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }
    }
}
