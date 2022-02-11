using System;
using System.Numerics;

namespace MeltEngine.Entity.Component
{
    public class Coord : Behaviour
    {
        public Vector3 Position { get; set; }
        public Vector4 Rotation { get; set; }
        public Vector3 Size { get; set; }

        // private void Start()
        // {
        //     Console.WriteLine($"{gameObject.Name}: {Position}");
        // }
        //
        // private void Update()
        // {
        //     Console.WriteLine($"{gameObject.Name}: {Rotation}");
        // }
    }
}