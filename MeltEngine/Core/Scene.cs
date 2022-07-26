namespace MeltEngine.Core
{
    public class Scene
    {
        private static uint _id;
        public uint Id { get; private set; }
        public string Name { get; private set; }

        public Scene(string name)
        {
            Id = _id++;
            Name = name;
        }
    }
}