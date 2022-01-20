namespace MeltEngine.Core
{
    public class Scene
    {
        private static uint _id;
        public uint ID { get; private set; }

        public Scene()
        {
            ID = _id++;
        }
    }
}