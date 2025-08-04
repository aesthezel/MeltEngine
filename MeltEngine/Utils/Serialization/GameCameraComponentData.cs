using System.Numerics;

namespace MeltEngine.Utils.Serialization
{
    public class GameCameraComponentData
    {
        public string TargetEntityName { get; set; } = "";
        public Vector3 Offset { get; set; }
        public CameraData Camera { get; set; } = new();
    }
    
    public class CameraData
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }
        public float Fovy { get; set; }
        public int Projection { get; set; }
    }
}