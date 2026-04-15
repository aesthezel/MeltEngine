using System.Numerics;

namespace MeltEngine.Entities.Components;

public struct BulletComponent
{
    public float Lifetime;
    public float MaxLifetime;

    public BulletComponent(float maxLifetime = 5f)
    {
        Lifetime = 0f;
        MaxLifetime = maxLifetime;
    }
}