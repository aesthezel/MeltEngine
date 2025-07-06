using System;
using System.Numerics;
using Raylib_CsLo;

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