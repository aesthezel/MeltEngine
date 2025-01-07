using System;
using System.Numerics;
using Raylib_CsLo;

namespace MeltEngine.Entity.Components;

public class GameCamera : Behaviour
{
    public Camera3D Camera { get; private set; }  // Estructura de cámara de Raylib
    public Coord Target { get; private set; }     // El objeto que sigue la cámara
    public Vector3 Offset { get; set; } = new(0, 2, -10); // Desplazamiento desde el objetivo

    public GameCamera()
    {
        Camera = new Camera3D
        {
            position = new Vector3(0, 2, -10),  // Posición inicial de la cámara
            target = new Vector3(0, 0, 0),      // Objetivo inicial
            up = new Vector3(0, 1, 0),          // Vector "up" de la cámara
            fovy = 45.0f,                       // Campo de visión
            projection = (int)CameraProjection.CAMERA_PERSPECTIVE
        };
    }

    protected override void Update()
    {
        if (Target == null) return;
        Camera = Camera with { position = Target.Position + Offset };
        Camera = Camera with { target = Target.Position };
    }

    public void Follow(GameObject target)
    {
        Target = target.GetBehaviour<Coord>();
        if (Target == null)
        {
            Console.WriteLine("El GameObject objetivo no tiene un componente Coord.");
        }
    }
}