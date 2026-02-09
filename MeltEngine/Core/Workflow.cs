using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeltEngine.Entities.Components;
using MeltEngine.Systems;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Scenes;
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
                // Inicializar Raylib
                Raylib.InitWindow(1920, 1080, "MeltEngine - Scene System");
                Raylib.SetTargetFPS(60);

                var entityOperator = new ThreadSafeECSOperator();
                var physicsSystem = new PhysicsSystem();

                // Cargar escena desde archivo JSON
                await LoadDefaultScene(entityOperator);

                // Sistemas del motor
                var systems = new List<ISystem>
                {
                    new PhysicsInitSystem(physicsSystem),
                    new MovementSystem(),
                    new CameraSystem(),
                    new RenderSystem(),
                    new LifecycleSystem()
                };

                const float fixedDeltaTime = 1.0f / 60.0f;
                var accumulator = 0.0f;

                while (!Raylib.WindowShouldClose())
                {
                    var deltaTime = Raylib.GetFrameTime();

                    // Procesar operaciones pendientes
                    entityOperator.ProcessPendingOperations();

                    // Detectar recarga de escena con F5
                    if (Raylib.IsKeyPressed(KeyboardKey.F5))
                    {
                        Console.WriteLine("Recargando escena...");
                        await ReloadScene(entityOperator);
                    }

                    accumulator += deltaTime;

                    while (accumulator >= fixedDeltaTime)
                    {
                        foreach (var system in systems.Where(s => s is not RenderSystem and not LifecycleSystem))
                        {
                            system.Update(entityOperator, fixedDeltaTime);
                        }

                        physicsSystem.Update(entityOperator, fixedDeltaTime);
                        accumulator -= fixedDeltaTime;
                    }

                    systems.OfType<LifecycleSystem>().FirstOrDefault()?.Update(entityOperator, deltaTime);
                    systems.OfType<RenderSystem>().FirstOrDefault()?.Update(entityOperator, deltaTime);
                }

                physicsSystem.Cleanup();
                Raylib.CloseWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        // Método síncrono para compatibilidad
        public static void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task LoadDefaultScene(ECSOperator entityOperator)
        {
            // Intenta cargar la escena por defecto
            string[] possibleScenes =
            {
                "Scenes/DefaultScene.json",
                "DefaultScene.json",
                "Scenes/MainScene.json",
                "MainScene.json"
            };

            foreach (var scenePath in possibleScenes)
            {
                try
                {
                    await _sceneService.LoadScene(scenePath, entityOperator);
                    Console.WriteLine($"Escena cargada exitosamente: {scenePath}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"No se pudo cargar {scenePath}: {ex.Message}");
                }
            }

            // Si no se puede cargar ninguna escena, crear una por defecto
            Console.WriteLine("Creando escena por defecto...");
            await CreateFallbackScene(entityOperator);
        }

        private static async Task ReloadScene(ECSOperator entityOperator)
        {
            await LoadDefaultScene(entityOperator);
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
            entityOperator.AddComponent(planeEntity, new CubeRendererComponent()); // ⭐ IMPORTANTE: Para que sea visible
            entityOperator.AddComponent(planeEntity, new EnabledComponent());
            entityOperator.AddComponent(planeEntity, new StaticPhysicsBodyComponent());
            entityOperator.AddComponent(planeEntity, new SceneMemberComponent("FallbackScene"));
            Console.WriteLine("✅ Suelo creado con rendering visible");

            var playerInitialPosition = new System.Numerics.Vector3(0, 2, 0); // ⭐ Más cerca del suelo

            var playerCubeEntity = entityOperator.CreateEntity();
            entityOperator.AddComponent(playerCubeEntity, new CoordComponent
            {
                Position = playerInitialPosition,
                Scale = new System.Numerics.Vector3(1, 1, 1)
            });
            entityOperator.AddComponent(playerCubeEntity, new CubeRendererComponent());
            entityOperator.AddComponent(playerCubeEntity, new PlayerControllableComponent { Speed = 5f });
            entityOperator.AddComponent(playerCubeEntity, new EnabledComponent());
            entityOperator.AddComponent(playerCubeEntity, new PhysicsBodyComponent { Mass = 1f });
            entityOperator.AddComponent(playerCubeEntity, new SceneMemberComponent("FallbackScene"));
            Console.WriteLine($"✅ Jugador creado en posición: {playerInitialPosition}");

            // ⭐ MENOS CUBOS PARA FALLBACK, MÁS CERCA DEL SUELO
            for (int i = 0; i < 5; i++)
            {
                var physicsCubeEntity = entityOperator.CreateEntity();
                entityOperator.AddComponent(physicsCubeEntity, new CoordComponent
                {
                    Position = new System.Numerics.Vector3(-2.0f, 1.0f + (i * 2.0f), -2.0f), // ⭐ Empezar desde Y=1
                    Scale = new System.Numerics.Vector3(1, 1, 1)
                });
                entityOperator.AddComponent(physicsCubeEntity, new CubeRendererComponent());
                entityOperator.AddComponent(physicsCubeEntity, new EnabledComponent());
                entityOperator.AddComponent(physicsCubeEntity, new PhysicsBodyComponent { Mass = 1f });
                entityOperator.AddComponent(physicsCubeEntity, new SceneMemberComponent("FallbackScene"));
            }

            Console.WriteLine("✅ Cubos de física creados");

            // ⭐ CÁMARA CON CONFIGURACIÓN CORRECTA
            var cameraEntity = entityOperator.CreateEntity();
            var cameraOffset = new System.Numerics.Vector3(0, 5, -10);
            var initialCameraPos = playerInitialPosition + cameraOffset;

            entityOperator.AddComponent(cameraEntity, new GameCameraComponent
            {
                TargetEntity = playerCubeEntity,
                Offset = cameraOffset,
                Camera = new Camera3D()
                {
                    Position = initialCameraPos,
                    Target = playerInitialPosition,
                    Up = new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),
                    FovY = 45.0f,
                    Projection = (int)CameraProjection.Perspective
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
    }
}