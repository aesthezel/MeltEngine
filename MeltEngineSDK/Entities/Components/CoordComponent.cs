using System.Numerics;

namespace MeltEngine.Entities.Components
{
    public struct CoordComponent
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;

        public Vector3 Position 
        { 
            get => _position; 
            set { _position = value; IsDirty = true; } 
        }

        public Quaternion Rotation 
        { 
            get => _rotation; 
            set { _rotation = value; IsDirty = true; } 
        }

        public Vector3 Scale 
        { 
            get => _scale; 
            set { _scale = value; IsDirty = true; } 
        }

        public Vector3 PreviousPosition { get; set; }

        public Matrix4x4 ModelMatrix;
        public bool IsDirty;

        public CoordComponent()
        {
            _position = Vector3.Zero;
            _rotation = Quaternion.Identity;
            _scale = Vector3.One;
            PreviousPosition = Vector3.Zero;
            ModelMatrix = Matrix4x4.Identity;
            IsDirty = true;
        }
    }
}