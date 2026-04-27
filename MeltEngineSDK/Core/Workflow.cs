using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MeltEngine.Entities.Components;
using MeltEngine.Systems;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Scenes;
using MeltEngine.Utils;
using Raylib_cs;

namespace MeltEngine.Core
{
    public static class Workflow
    {
        private static SceneService _sceneService = new SceneService();

        public static async Task RunAsync()
        {
            try
            {
                Raylib.InitWindow(1920, 1080, "MeltEngineSDK - Scene System");
                Raylib.SetTargetFPS(120);

                var entityOperator = new ThreadSafeECSOperator();
                var physicsSystem = new PhysicsManager();
                var chunkCuller = new ChunkCullerSystem();

                await LoadDefaultScene(entityOperator, physicsSystem);

                var systems = new List<ISystem>
                {
                    new PhysicsInitSystem(physicsSystem),
                    new BulletSpawnerSystem(),
                    new JumpSystem(physicsSystem),
                    new MovementSystem(physicsSystem),
                    new CameraSystem(),
                    new LifecycleSystem(),
                    chunkCuller,
                    _worldGenerator!,
                    new BlockInteractionSystem(_worldGenerator!)
                };

                var updateSystems = systems.Where(s => s is not RenderSystem and not LifecycleSystem).ToArray();
                var lifecycleSystem = systems.OfType<LifecycleSystem>().FirstOrDefault();
                var renderSystem = new RenderSystem(physicsSystem, chunkCuller);

                float physicsAccumulator = 0f;
                var stopwatch = Stopwatch.StartNew();

                while (!Raylib.WindowShouldClose())
                {
                    var frameTime = Raylib.GetFrameTime();
                    Time.DeltaTime = frameTime;

                    entityOperator.ProcessPendingOperations();

                    if (Raylib.IsKeyPressed(KeyboardKey.F5))
                    {
                        Console.WriteLine("Recargando escena...");
                        await ReloadScene(entityOperator, physicsSystem);
                    }

                    foreach (var system in updateSystems)
                    {
                        system.Update(entityOperator, frameTime);
                    }

                    physicsAccumulator += frameTime;
                    int physicsSteps = 0;
                    bool physicsStepped = false;

                    while (physicsAccumulator >= PhysicsManager.FIXED_DELTA_TIME && physicsSteps < 3)
                    {
                        physicsSystem.Simulate(PhysicsManager.FIXED_DELTA_TIME);
                        physicsSystem.Update(entityOperator); // Captura PreviousPosition y actualiza Position
                        physicsAccumulator -= PhysicsManager.FIXED_DELTA_TIME;
                        physicsSteps++;
                        physicsStepped = true;
                    }

                    if (physicsAccumulator > PhysicsManager.FIXED_DELTA_TIME * 4f)
                        physicsAccumulator = 0f;

                    // Calcular Alpha para la interpolación visual
                    Time.Alpha = physicsAccumulator / PhysicsManager.FIXED_DELTA_TIME;

                    if (physicsStepped)
                    {
                        physicsSystem.UpdateLod(entityOperator);
                    }

                    lifecycleSystem?.Update(entityOperator, frameTime);
                    renderSystem.Update(entityOperator, frameTime);
                }

                BlockMeshGenerator.UnloadCache();
                physicsSystem.Cleanup();
                Raylib.CloseWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
        
        public static void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task LoadDefaultScene(ECSOperator entityOperator, PhysicsManager physicsManager)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("  MELT ENGINE - MINECRAFT DEMO ");
            Console.WriteLine("==============================================");
            Console.WriteLine("Generando mundo 256x256 con ECS y Jolt Physics");
            Console.WriteLine("Presiona F5 para regenerar el mundo");
            Console.WriteLine("----------------------------------------------");

            // Load block definitions from JSON (data-driven block system)
            string blocksJsonPath = Path.Combine(AppContext.BaseDirectory, "Resources", "blocks.json");
            BlockRegistry.LoadDatabase(blocksJsonPath);
            
            var worldGenerator = new WorldGeneratorSystem(
                seed: 283102319,
                worldSizeX: 256,
                worldSizeZ: 256,
                renderRadius: 10,
                physicsManager: physicsManager
            );
            
            _worldGenerator = worldGenerator;
            
            var spawnPos = worldGenerator.GetSpawnPosition();
            Console.WriteLine($"==============================================");
            Console.WriteLine($"Spawn: ({spawnPos.X}, {spawnPos.Y}, {spawnPos.Z})");
            Console.WriteLine("==============================================");
            
            CreatePlayerAndCamera(entityOperator, spawnPos);
        }

        private static void CreatePlayerAndCamera(ECSOperator entityOperator, System.Numerics.Vector3 spawnPosition)
        {
            var playerCubeEntity = entityOperator.CreateEntity();
            entityOperator.AddComponent(playerCubeEntity, new CoordComponent
            {
                Position = spawnPosition,
                Scale = new System.Numerics.Vector3(1, 2, 1)
            });
            entityOperator.AddComponent(playerCubeEntity, new CubeRendererComponent());
            entityOperator.AddComponent(playerCubeEntity, new PlayerControllableComponent
            {
                Speed = 10f,
                IsGodMode = true
            });
            entityOperator.AddComponent(playerCubeEntity, new EnabledComponent());
            entityOperator.AddComponent(playerCubeEntity, new PhysicsBodyComponent { Mass = 1f });
            entityOperator.AddComponent(playerCubeEntity, new SceneMemberComponent("World"));
            
            var cameraEntity = entityOperator.CreateEntity();
            var cameraOffset = new System.Numerics.Vector3(0, 5, -8);
            
            entityOperator.AddComponent(cameraEntity, new GameCameraComponent
            {
                TargetEntity = playerCubeEntity,
                Offset = cameraOffset,
                IsOrbitMode = true,
                Distance = 8.0f,
                Yaw = 0.0f,
                Pitch = 30.0f,
                DrawDistance = 150f,
                Camera = new Camera3D
                {
                    Position = spawnPosition + cameraOffset,
                    Target = spawnPosition,
                    Up = new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),
                    FovY = 60.0f,
                    Projection = CameraProjection.Perspective
                }
            });
            entityOperator.AddComponent(cameraEntity, new SceneMemberComponent("World"));
            
            Console.WriteLine($"Player entity: {playerCubeEntity.Id}");
            Console.WriteLine($"Camera entity: {cameraEntity.Id}");
        }

        private static WorldGeneratorSystem? _worldGenerator;

        private static async Task ReloadScene(ECSOperator entityOperator, PhysicsManager physicsManager)
        {
            Console.WriteLine("Regenerando mundo...");
            SceneService.ClearScene(entityOperator);
            await LoadDefaultScene(entityOperator, physicsManager);
        }

        private static async Task CreateFallbackScene(ECSOperator entityOperator)
        {
            Console.WriteLine("=== CREANDO ESCENA DE FALLBACK ===");

            // ⭐ SUELO CON RENDERING VISIBLE
            var planeEntity = entityOperator.CreateEntity();
            entityOperator.AddComponent(planeEntity, new CoordComponent
            {
                Position = new System.Numerics.Vector3(0, -0.5f, 0),
                Scale = new System.Numerics.Vector3(50, 1, 50)
            });
            entityOperator.AddComponent(planeEntity, new CubeRendererComponent());
            entityOperator.AddComponent(planeEntity, new EnabledComponent());
            entityOperator.AddComponent(planeEntity, new StaticPhysicsBodyComponent());
            entityOperator.AddComponent(planeEntity, new SceneMemberComponent("FallbackScene"));
            Console.WriteLine("✅ Suelo creado con rendering visible");

            var playerInitialPosition = new System.Numerics.Vector3(0, 2, 0);

            var playerCubeEntity = entityOperator.CreateEntity();
            entityOperator.AddComponent(playerCubeEntity, new CoordComponent
            {
                Position = playerInitialPosition,
                Scale = new System.Numerics.Vector3(1, 1, 1)
            });
            entityOperator.AddComponent(playerCubeEntity, new CubeRendererComponent());
            entityOperator.AddComponent(playerCubeEntity, new PlayerControllableComponent
            {
                Speed = 5f,
                IsGodMode = true
            });
            entityOperator.AddComponent(playerCubeEntity, new EnabledComponent());
            entityOperator.AddComponent(playerCubeEntity, new PhysicsBodyComponent { Mass = 1f });
            entityOperator.AddComponent(playerCubeEntity, new SceneMemberComponent("FallbackScene"));
            Console.WriteLine($"✅ Jugador creado en posición: {playerInitialPosition}");
            
            for (int i = 0; i < 5; i++)
            {
                var physicsCubeEntity = entityOperator.CreateEntity();
                entityOperator.AddComponent(physicsCubeEntity, new CoordComponent
                {
                    Position = new System.Numerics.Vector3(-2.0f, 1.0f + (i * 2.0f), -2.0f),
                    Scale = new System.Numerics.Vector3(1, 1, 1)
                });
                entityOperator.AddComponent(physicsCubeEntity, new CubeRendererComponent());
                entityOperator.AddComponent(physicsCubeEntity, new EnabledComponent());
                entityOperator.AddComponent(physicsCubeEntity, new PhysicsBodyComponent { Mass = 1f });
                entityOperator.AddComponent(physicsCubeEntity, new SceneMemberComponent("FallbackScene"));
            }

            Console.WriteLine("✅ Cubos de física creados");
            
            var cameraEntity = entityOperator.CreateEntity();
            var cameraOffset = new System.Numerics.Vector3(0, 5, -10);
            var initialCameraPos = playerInitialPosition + cameraOffset;

            entityOperator.AddComponent(cameraEntity, new GameCameraComponent
            {
                TargetEntity = playerCubeEntity,
                Offset = cameraOffset,
                IsOrbitMode = true,
                Distance = 10.0f,
                Yaw = 0.0f,
                Pitch = 30.0f,
                Camera = new Camera3D
                {
                    Position = initialCameraPos,
                    Target = playerInitialPosition,
                    Up = new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),
                    FovY = 45.0f,
                    Projection = CameraProjection.Perspective
                }
            });
            entityOperator.AddComponent(cameraEntity, new SceneMemberComponent("FallbackScene"));

            Console.WriteLine($"✅ Cámara creada:");
            Console.WriteLine($"    Posición: {initialCameraPos}");
            Console.WriteLine($"    Target: {playerInitialPosition}");
            Console.WriteLine($"    Offset: {cameraOffset}");
            Console.WriteLine($"    Siguiendo a entidad: {playerCubeEntity.Id}");

            Console.WriteLine("=== ESCENA DE FALLBACK COMPLETADA ===");
        }

        private static void PrewarmStressTest(ECSOperator entityOperator)
        {
            const int count = 100;
            Console.WriteLine($"=== INICIANDO PREWARM DE STRESS TEST: {count} CUBOS (TORRE MASSIVA) ===");

            int sizeXZ = 32;
            int layers = 2000;
            float spacing = 1.05f;
            float startX = -25f;
            float startZ = -25f;

            int built = 0;

            for (int y = 0; y < layers; y++)
            {
                for (int x = 0; x < sizeXZ; x++)
                {
                    for (int z = 0; z < sizeXZ; z++)
                    {
                        if (built >= count) break;

                        // Condición para torre hueca: Solo instanciar si estamos en los bordes del cuadrado XZ
                        bool isEdge = (x == 0 || x == sizeXZ - 1 || z == 0 || z == sizeXZ - 1);
                        if (!isEdge) continue;

                        var entity = entityOperator.CreateEntity();

                        float posX = startX + (x * spacing) - ((sizeXZ * spacing) / 2f);
                        float posY = 0.55f + (y * spacing);
                        float posZ = startZ + (z * spacing) - ((sizeXZ * spacing) / 2f);

                        entityOperator.AddComponent(entity, new CoordComponent
                        {
                            Position = new System.Numerics.Vector3(posX, posY, posZ),
                            Scale = System.Numerics.Vector3.One
                        });

                        entityOperator.AddComponent(entity, new CubeRendererComponent());
                        entityOperator.AddComponent(entity, new EnabledComponent());

                        entityOperator.AddComponent(entity, new PhysicsBodyComponent
                        {
                            Mass = 1.0f,
                            UseLod = true,
                            LodDisableDistance = 250.0f
                        });

                        entityOperator.AddComponent(entity, new SceneMemberComponent("StressTest"));

                        built++;
                    }

                    if (built >= count) break;
                }

                if (built >= count) break;
            }

            Console.WriteLine($"=== PREWARM COMPLETADO: {built} ENTIDADES CREADAS ===");
        }
    }
}