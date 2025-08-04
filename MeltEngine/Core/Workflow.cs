using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MeltEngine.Entities.Components;
using MeltEngine.Systems;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Core
{
    public static class Workflow
    {
        public static void Run()
        {
            try
            {
                var entityOperator = new ThreadSafeECSOperator();
                var physicsSystem = new PhysicsSystem();
                
                var planeEntity = entityOperator.CreateEntity();
                entityOperator.AddComponent(planeEntity, new CoordComponent { 
                    Position = new Vector3(0, -0.5f, 0), 
                    Scale = new Vector3(50, 1, 50) 
                });
                entityOperator.AddComponent(planeEntity, new EnabledComponent());
                entityOperator.AddComponent(planeEntity, new StaticPhysicsBodyComponent());
                
                var playerInitialPosition = new Vector3(0, 2, 0);
                
                var playerCubeEntity = entityOperator.CreateEntity();
                entityOperator.AddComponent(playerCubeEntity, new CoordComponent { 
                    Position = playerInitialPosition, 
                    Scale = new Vector3(1, 1, 1) 
                });
                entityOperator.AddComponent(playerCubeEntity, new CubeRendererComponent());
                entityOperator.AddComponent(playerCubeEntity, new PlayerControllableComponent { Speed = 5f });
                entityOperator.AddComponent(playerCubeEntity, new EnabledComponent());
                entityOperator.AddComponent(playerCubeEntity, new PhysicsBodyComponent { Mass = 1f });
                
                for (int i = 0; i < 3000; i++)
                {
                    var physicsCubeEntity = entityOperator.CreateEntity();
                    entityOperator.AddComponent(physicsCubeEntity, new CoordComponent {
                        Position = new Vector3(-2.0f,(i * 2.0f), -2.0f),
                        Scale = new Vector3(1, 1, 1)
                    });
                    entityOperator.AddComponent(physicsCubeEntity, new CubeRendererComponent());
                    entityOperator.AddComponent(physicsCubeEntity, new EnabledComponent());
                    entityOperator.AddComponent(physicsCubeEntity, new PhysicsBodyComponent { Mass = 1f });
                }

                var cameraEntity = entityOperator.CreateEntity();
                var cameraOffset = new Vector3(0, 5, -10);
                entityOperator.AddComponent(cameraEntity, new GameCameraComponent
                {
                    TargetEntity = playerCubeEntity,
                    Offset = cameraOffset,
                    Camera = new Camera3D()
                    {
                        Position = playerInitialPosition + cameraOffset,
                        Target = playerInitialPosition,
                        Up = new Vector3(0.0f, 1.0f, 0.0f),
                        FovY = 45.0f,
                        Projection = (int)CameraProjection.Perspective
                    }
                });
                
                // IMPORTANTE: Mantener TODOS los sistemas en el hilo principal por ahora
                // Para evitar problemas con PhysX
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
    }
}