using System.Numerics;

namespace MeltEngine.Entities.Components;

public struct JumpComponent
{
    public float Cooldown { get; set; }
    public float Timer { get; set; }
    public Vector3 MinJumpForce { get; set; }
    public Vector3 MaxJumpForce { get; set; }
}
