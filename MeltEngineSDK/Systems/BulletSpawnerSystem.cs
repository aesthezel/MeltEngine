using System;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems;

public class BulletSpawnerSystem : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var spawnerArray = entityOperator.GetComponentArray<BulletSpawnerComponent>();
        var coordArray = entityOperator.GetComponentArray<CoordComponent>();
        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();
        var bulletArray = entityOperator.GetComponentArray<BulletComponent>();
        var destroyedArray = entityOperator.GetComponentArray<DestroyedComponent>();

        foreach (var (entity, spawner) in spawnerArray.Components)
        {
            var mutableSpawner = spawner;

            if (mutableSpawner.CurrentCooldown > 0)
            {
                mutableSpawner.CurrentCooldown -= deltaTime;
                entityOperator.AddComponent(entity, mutableSpawner);
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                if (mutableSpawner.CurrentCooldown <= 0 && coordArray.Components.TryGetValue(entity, out var coord))
                {
                    Vector3 direction = Vector3.UnitZ;
                    
                    foreach (var cam in cameraArray.Components.Values)
                    {
                        Vector3 forward = Vector3.Normalize(cam.Camera.Target - cam.Camera.Position);
                        if (forward != Vector3.Zero)
                            direction = forward;
                        break;
                    }

                    SpawnBullet(entityOperator, coord.Position, direction, mutableSpawner);

                    mutableSpawner.CurrentCooldown = mutableSpawner.Cooldown;
                    entityOperator.AddComponent(entity, mutableSpawner);
                }
            }
        }

        for (int i = 0; i < bulletArray.Count; i++)
        {
            var bulletEntity = bulletArray.DenseEntities[i];
            if (!bulletArray.Components.TryGetValue(bulletEntity, out var bullet)) continue;
            if (destroyedArray.Components.ContainsKey(bulletEntity)) continue;

            bullet.Lifetime += deltaTime;
            
            if (bullet.Lifetime >= bullet.MaxLifetime)
            {
                entityOperator.AddComponent(bulletEntity, new DestroyedComponent());
                Console.WriteLine($"Bala {bulletEntity.Id} destruida por tiempo");
            }
            else
            {
                bulletArray.Components[bulletEntity] = bullet;
            }
        }
    }

    private void SpawnBullet(ECSOperator entityOperator, Vector3 playerPosition, Vector3 direction, BulletSpawnerComponent spawner)
    {
        var bulletEntity = entityOperator.CreateEntity();

        Vector3 spawnPos = playerPosition + direction * 1.5f;
        spawnPos.Y += 0.5f;

        entityOperator.AddComponent(bulletEntity, new CoordComponent
        {
            Position = spawnPos,
            Scale = spawner.BulletScale,
            Rotation = Quaternion.Identity
        });

        entityOperator.AddComponent(bulletEntity, new BulletRendererComponent());
        entityOperator.AddComponent(bulletEntity, new EnabledComponent());
        
        entityOperator.AddComponent(bulletEntity, new PhysicsBodyComponent
        {
            Mass = spawner.BulletMass,
            UseLod = false
        });
        
        entityOperator.AddComponent(bulletEntity, new BulletComponent(5f));

        entityOperator.AddComponent(bulletEntity, new InitialVelocityComponent
        {
            LinearVelocity = direction * spawner.BulletSpeed
        });

        entityOperator.AddComponent(bulletEntity, new SceneMemberComponent("Bullets"));

        Console.WriteLine($"Bala disparada desde {spawnPos} con velocidad {direction * spawner.BulletSpeed}");
    }
}