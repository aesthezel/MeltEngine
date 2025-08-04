using System.Collections.Generic;

namespace MeltEngine.Scenes
{
    public class Scene
    {
        public string Name { get; set; } = "Default Scene";
        public string Description { get; set; } = "";
        public List<EntityDefinition> Entities { get; set; } = new();
    }
}