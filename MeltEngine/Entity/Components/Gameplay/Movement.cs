using System;
using System.Numerics;
using MeltEngine.Entity.Components.Physics;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components.Gameplay;

public class Movement : Behaviour
{
    private CubePhysics _physicBody;
    public float Speed { get; set; }

    public Movement()
    {
        Speed = 100f;
    }

    public Movement(float speed)
    {
        Speed = speed;
    }
    
    public override void Start()
    {
        Console.WriteLine($"{GetType().Name}: Start");
        _physicBody = GameObject.GetBehaviour<CubePhysics>();
    }

    public override void Update()
    {
        if (_physicBody == null)
        {
            Console.WriteLine("No physic body defined");
            return;
        }
        
        Vector3 force = Vector3.Zero;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
            force.Z += Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
            force.Z -= Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            force.X += Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            force.X -= Speed;

        // if (Raylib.IsKeyDown(KeyboardKey.KEY_SPACE))
        //     force.Y += Speed * 5;
        
        _physicBody.ApplyForce(force * Raylib.GetFrameTime());
    }
}