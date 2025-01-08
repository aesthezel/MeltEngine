using System;
using MeltEngine.Entity.Components.Physics;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components;

public class CubeRenderer : Behaviour
{
    private Coord _coord;
    private CubePhysics _cubePhysics;
    
    protected override void Start()
    {
        _coord = GameObject.GetBehaviour<Coord>();
        _cubePhysics = GameObject.GetBehaviour<CubePhysics>();
    }

    protected override void Render()
    {
        if (_coord == null) return;
        //Console.WriteLine($"{_coord.Position.X}, {_coord.Position.Y}, {_coord.Position.Z}");
        Raylib.DrawCube(_cubePhysics.Position, _coord.Size.X, _coord.Size.Y, _coord.Size.Z, Raylib.BLUE);
    }
}