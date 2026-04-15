using System.Numerics;

namespace MeltEngine.Entities.Components;

public struct InitialVelocityComponent
{
    public Vector3 LinearVelocity;
    public Vector3 AngularVelocity;

    public InitialVelocityComponent(Vector3 linearVelocity = default, Vector3 angularVelocity = default)
    {
        LinearVelocity = linearVelocity;
        AngularVelocity = angularVelocity;
    }
}