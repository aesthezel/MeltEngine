using System.Collections.Generic;
using System.Text.Json;

namespace MeltEngine.Scenes
{
    public class EntityDefinition
    {
        public string Name { get; set; } = "Unnamed Entity";
        public string Type { get; set; } = "Default"; // Player, StaticObject, Camera, etc.
        public Dictionary<string, JsonElement> Components { get; set; } = new();
        
        // Constructor sin parámetros para deserialización
        public EntityDefinition() { }
        
        // Constructor con parámetros para facilidad de uso
        public EntityDefinition(string name, string type = "Default")
        {
            Name = name;
            Type = type;
        }
    }
}