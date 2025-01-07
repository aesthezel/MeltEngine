using System;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components;

public class CubeRenderer : Behaviour
{
    private Coord _coord;
    
    protected override void Start()
    {
        _coord = GameObject.GetBehaviour<Coord>();
    }

    protected override void Render()
    {
        if (_coord == null) return;
        Console.WriteLine($"{_coord.Position.X}, {_coord.Position.Y}, {_coord.Position.Z}");
        Raylib.DrawCube(_coord.Position, _coord.Size.X, _coord.Size.Y, _coord.Size.Z, Raylib.BLUE);
    }
}