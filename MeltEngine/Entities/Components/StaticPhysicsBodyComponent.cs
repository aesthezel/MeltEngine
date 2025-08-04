using MagicPhysX;

namespace MeltEngine.Entities.Components;

public unsafe struct StaticPhysicsBodyComponent
{
    public PxRigidStatic* Actor;
}