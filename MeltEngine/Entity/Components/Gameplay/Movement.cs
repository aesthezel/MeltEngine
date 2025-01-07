using System;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components.Gameplay;

public class Movement : Behaviour
{
    private Coord _coord;
    public float Speed { get; set; } = 5f;

    protected override void Start()
    {
        // Obtener referencia al componente Coord
        Console.WriteLine($"{GetType().Name}: Start");
        _coord = GameObject.GetBehaviour<Coord>();
    }

    protected override void Update()
    {
        if (_coord == null)
        {
            Console.WriteLine("No coord defined");
            return;
        }

        // Leer input de Raylib y mover el cubo
        if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
            _coord.Position = _coord.Position with { Z = _coord.Position.Z + Speed * Raylib.GetFrameTime() };

        if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
            _coord.Position = _coord.Position with { Z = _coord.Position.Z - Speed * Raylib.GetFrameTime() };

        if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            _coord.Position = _coord.Position with { X = _coord.Position.X + Speed * Raylib.GetFrameTime() };

        if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            _coord.Position = _coord.Position with { X = _coord.Position.X - Speed * Raylib.GetFrameTime() };
    }
}