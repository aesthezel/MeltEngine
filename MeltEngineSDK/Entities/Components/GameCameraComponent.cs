using System.Numerics;
using Raylib_cs;

namespace MeltEngine.Entities.Components
{
    public struct GameCameraComponent
    {
        public Camera3D Camera;
        public Vector3 Offset;
        public Entity TargetEntity;
        
        // Modo Órbita
        public bool IsOrbitMode;
        public float Yaw;
        public float Pitch;
        public float Distance;
    }
}