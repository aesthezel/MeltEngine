using System;
using System.Numerics;
using MeltEngine.Entity.Components.Physics;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components.Gameplay;

public class Movement(float speed) : Behaviour
{
    private CubePhysics _physicBody;
    public float Speed { get; set; } = speed;

    protected override void Start()
    {
        // Obtener referencia al componente CubePhysics
        Console.WriteLine($"{GetType().Name}: Start");
        _physicBody = GameObject.GetBehaviour<CubePhysics>();
    }

    protected override void Update()
    {
        if (_physicBody == null)
        {
            Console.WriteLine("No physic body defined");
            return;
        }

        // Variables para determinar las fuerzas a aplicar
        Vector3 force = Vector3.Zero;

        // Leer input de Raylib y mover el cubo aplicando fuerzas
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
            force.Z += Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
            force.Z -= Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            force.X += Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            force.X -= Speed;

        if (Raylib.IsKeyDown(KeyboardKey.KEY_SPACE))
            force.Y += Speed * 5;

        // Aplicar la fuerza al cuerpo físico
        _physicBody.ApplyForce(force * Raylib.GetFrameTime());
    }
}