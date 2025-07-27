using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class GameCameraComponentDefinition
    {
        [JsonPropertyName("targetEntityName")]
        public string TargetEntityName { get; set; }

        [JsonPropertyName("offset")]
        public Vector3Definition Offset { get; set; }

        [JsonPropertyName("camera")]
        public CameraDefinition Camera { get; set; }
    }
}
