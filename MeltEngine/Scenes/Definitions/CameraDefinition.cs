using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class CameraDefinition
    {
        [JsonPropertyName("position")]
        public Vector3Definition Position { get; set; }

        [JsonPropertyName("target")]
        public Vector3Definition Target { get; set; }

        [JsonPropertyName("up")]
        public Vector3Definition Up { get; set; }

        [JsonPropertyName("fovy")]
        public float Fovy { get; set; }

        [JsonPropertyName("projection")]
        public int Projection { get; set; } // Assuming 0 for Perspective, 1 for Orthographic as in Raylib
    }
}
