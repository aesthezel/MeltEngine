using System;
using System.Numerics;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components
{
    public class Coord : Behaviour
    {
        public Vector3 Position { get; set; }
        public Vector4 Rotation { get; set; } 
        public Vector3 Size { get; set; }

        protected override void Update()
        {
            //Console.WriteLine($"{GameObject.Name}: {Rotation}");
        }
        
        protected override void Render()
        {
            Raylib.DrawSphere(Position, 0.1f, Raylib.RED);
        }
    }
}