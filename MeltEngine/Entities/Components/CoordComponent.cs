using System.Numerics;

namespace MeltEngine.Entities.Components
{
    public struct CoordComponent
    {
        public Vector3 Position { get; set; }
        public Vector3 PreviousPosition { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
    }
}