using MagicPhysX;

namespace MeltEngine.Entities.Components;

public unsafe struct PhysicsBodyComponent
{
    public PxRigidDynamic* Actor;
    public float Mass { get; init; }
}