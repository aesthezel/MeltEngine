using System;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems;

public class CubeSpawnerSystem : ISystem
{
    private readonly Random _random = new Random();

    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var spawnerArray = entityOperator.GetComponentArray<CubeSpawnerComponent>();
        var coordArray = entityOperator.GetComponentArray<CoordComponent>();

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
                    SpawnCube(entityOperator, coord.Position + new Vector3(0, 0, 2));

                    mutableSpawner.CurrentCooldown = mutableSpawner.Cooldown;
                    entityOperator.AddComponent(entity, mutableSpawner);
                }
            }
        }
    }

    private void SpawnCube(ECSOperator entityOperator, Vector3 playerPosition)
    {
        var cubeEntity = entityOperator.CreateEntity();

        // Spawn slightly ahead or randomly around the player so they don't instakill each other's collision points perfectly.
        float offsetX = (float)(_random.NextDouble() * 0.5 - 0.25);
        float offsetZ = (float)(_random.NextDouble() * 0.5 - 0.25);
        var spawnPos = new Vector3(playerPosition.X + offsetX, playerPosition.Y + 2.0f, playerPosition.Z + offsetZ);

        entityOperator.AddComponent(cubeEntity, new CoordComponent
        {
            Position = spawnPos,
            Scale = new Vector3(1, 1, 1)
        });

        entityOperator.AddComponent(cubeEntity, new CubeRendererComponent());
        entityOperator.AddComponent(cubeEntity, new EnabledComponent());
        entityOperator.AddComponent(cubeEntity, new PhysicsBodyComponent { Mass = 0.5f });
        
        // Añadimos el sistema de saltos configurables
        entityOperator.AddComponent(cubeEntity, new JumpComponent
        {
            Cooldown = 2.0f, // Salta cada 2 segundos
            Timer = (float)(_random.NextDouble() * 2.0), // Randomizamos el inicio para que no salten todos a la vez
            MinJumpForce = new Vector3(-1f, 1f, -1f),
            MaxJumpForce = new Vector3(1f, 10f, 1f)
        });

        entityOperator.AddComponent(cubeEntity, new SceneMemberComponent("DynamicCubes"));

        Console.WriteLine($"Instanciado cubo dinamico en Entity {cubeEntity.Id} at {spawnPos}");
    }
}
