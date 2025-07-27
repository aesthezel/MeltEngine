using System.Text.Json.Serialization;

namespace MeltEngine.Scenes.Definitions
{
    public class PlayerControllableComponentDefinition
    {
        [JsonPropertyName("speed")]
        public float Speed { get; set; }
    }
}
