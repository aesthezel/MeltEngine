using System.Numerics;

namespace MeltEngine.Entities.Components;

public struct BulletSpawnerComponent
{
    public float Cooldown;
    public float CurrentCooldown;
    public float BulletSpeed;
    public Vector3 BulletScale;
    public float BulletMass;

    public BulletSpawnerComponent()
    {
        Cooldown = 0.1f;
        CurrentCooldown = 0f;
        BulletSpeed = 30f;
        BulletScale = new Vector3(0.2f, 0.2f, 0.4f);
        BulletMass = 1f;
    }
}