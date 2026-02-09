using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Utils.Serialization;
using Raylib_cs;

namespace MeltEngine.Scenes
{
    public class SceneService
    {
        private string _currentScenePath = "";
        private string _currentSceneName = "";

        public async Task LoadScene(string scenePath, ECSOperator entityOperator)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), scenePath);

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"ADVERTENCIA: No se encontró el archivo de escena: {fullPath}");
                    Console.WriteLine("Creando escena de ejemplo...");
                    CreateExampleScene(fullPath);
                }

                var scene = SceneSerializer.LoadScene(fullPath);
                if (scene is null)
                {
                    throw new JsonException($"Error al deserializar la escena: {fullPath}");
                }

                Console.WriteLine($"Cargando escena: {scene.Name}");
                ClearScene(entityOperator);

                _currentScenePath = fullPath;
                _currentSceneName = scene.Name;

                // Mapeo de nombres a entidades
                var entityNameMap = new Dictionary<string, Entity>();
                var cameraTargetRequests =
                    new List<(Entity cameraEntity, string targetName, GameCameraComponent camera)>();

                // PRIMERA PASADA: Crear todas las entidades y mapear nombres
                foreach (var entityDef in scene.Entities)
                {
                    var entity = entityOperator.CreateEntity();
                    entityOperator.AddComponent(entity, new SceneMemberComponent(scene.Name));

                    entityNameMap[entityDef.Name] = entity;
                    Console.WriteLine($"Creando entidad: {entityDef.Name} → Entity {entity.Id}");

                    foreach (var (compTypeName, compJsonElement) in entityDef.Components)
                    {
                        await AddComponentToEntity(entity, compTypeName, compJsonElement, entityOperator,
                            cameraTargetRequests, entityDef.Name);
                    }
                }

                // SEGUNDA PASADA: Resolver referencias de cámaras
                foreach (var (cameraEntity, targetName, camera) in cameraTargetRequests)
                {
                    if (entityNameMap.TryGetValue(targetName, out var targetEntity))
                    {
                        var finalCamera = camera;
                        finalCamera.TargetEntity = targetEntity;
                        entityOperator.AddComponent(cameraEntity, finalCamera);
                        Console.WriteLine(
                            $"✅ Cámara Entity {cameraEntity.Id} ahora sigue a '{targetName}' (Entity {targetEntity.Id})");
                    }
                    else
                    {
                        Console.WriteLine($"❌ ERROR: No se encontró entidad con nombre '{targetName}' para la cámara");
                        entityOperator.AddComponent(cameraEntity, camera);
                    }
                }

                Console.WriteLine("Escena cargada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("!!!!!!!!!! EXCEPCIÓN EN SceneService.LoadScene !!!!!!!!!!");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }


        private async Task AddComponentToEntity(Entity entity, string compTypeName, JsonElement compJsonElement,
            ECSOperator entityOperator, List<(Entity, string, GameCameraComponent)> cameraTargetRequests,
            string entityName)
        {
            switch (compTypeName)
            {
                case "CoordComponent":
                    var coord = compJsonElement.Deserialize<CoordComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, coord);
                    Console.WriteLine($"  → CoordComponent: Pos={coord.Position}, Scale={coord.Scale}");
                    break;

                case "EnabledComponent":
                    var enabled = compJsonElement.Deserialize<EnabledComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, enabled);
                    Console.WriteLine($"  → EnabledComponent agregado");
                    break;

                case "CubeRendererComponent":
                    var cube = compJsonElement.Deserialize<CubeRendererComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, cube);
                    Console.WriteLine($"  → CubeRendererComponent agregado");
                    break;

                case "PlayerControllableComponent":
                    var controllable =
                        compJsonElement.Deserialize<PlayerControllableComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, controllable);
                    Console.WriteLine($"  → PlayerControllableComponent: Speed={controllable.Speed}");
                    break;

                case "StaticPhysicsBodyComponent":
                    var staticBody = compJsonElement.Deserialize<StaticPhysicsBodyComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, staticBody);
                    Console.WriteLine($"  → StaticPhysicsBodyComponent agregado");
                    break;

                case "PhysicsBodyComponent":
                    var physicsBody = compJsonElement.Deserialize<PhysicsBodyComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, physicsBody);
                    Console.WriteLine($"  → PhysicsBodyComponent: Mass={physicsBody.Mass}");
                    break;

                case "GameCameraComponent":
                    var cameraData = compJsonElement.Deserialize<GameCameraComponentData>(SceneSerializer.Options);

                    var camera = new GameCameraComponent
                    {
                        Offset = cameraData.Offset,
                        Camera = new Camera3D
                        {
                            Position = cameraData.Camera.Position,
                            Target = cameraData.Camera.Target,
                            Up = cameraData.Camera.Up,
                            FovY = cameraData.Camera.Fovy,
                            Projection = (CameraProjection)cameraData.Camera.Projection
                        }
                    };

                    cameraTargetRequests.Add((entity, cameraData.TargetEntityName, camera));
                    Console.WriteLine(
                        $"  → GameCameraComponent: Target='{cameraData.TargetEntityName}', Offset={cameraData.Offset}");
                    Console.WriteLine($"    Pos={cameraData.Camera.Position}, Target={cameraData.Camera.Target}");
                    break;

                default:
                    Console.WriteLine($"  ⚠️ ADVERTENCIA: Componente '{compTypeName}' no manejado. Saltando.");
                    break;
            }
        }

        public static void ClearScene(ECSOperator entityOperator)
        {
            var sceneMembers = entityOperator.GetComponentArray<SceneMemberComponent>();
            var entitiesToDestroy = sceneMembers.Components.Keys.ToList();

            if (entitiesToDestroy.Any())
            {
                Console.WriteLine($"Limpiando {entitiesToDestroy.Count} entidades de la escena anterior...");
                foreach (var entity in entitiesToDestroy)
                {
                    entityOperator.DestroyEntity(entity);
                }
            }
        }

        public string GetCurrentSceneName() => _currentSceneName;
        public string GetCurrentScenePath() => _currentScenePath;

        private void CreateExampleScene(string path)
        {
            // Crear directorio si no existe
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var exampleScene = new Scene
            {
                Name = "Escena de Ejemplo Configurable v2",
                Description = "Una escena básica configurable externamente con suelo sólido",
                Entities = new List<EntityDefinition>
                {
                    // ⭐ SUELO CON RENDERING VISIBLE
                    new EntityDefinition("Ground", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent
                            {
                                Position = new Vector3(0, -0.5f, 0),
                                Scale = new Vector3(50, 1, 50)
                            }),
                            ["CubeRendererComponent"] =
                                JsonSerializer.SerializeToElement(
                                    new CubeRendererComponent()), // ⭐ IMPORTANTE: Para que sea visible
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent()),
                            ["StaticPhysicsBodyComponent"] =
                                JsonSerializer.SerializeToElement(new StaticPhysicsBodyComponent())
                        }
                    },

                    // ⭐ JUGADOR EN POSICIÓN SEGURA SOBRE EL SUELO
                    new EntityDefinition("Player", "Player")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent
                            {
                                Position = new Vector3(0, 2f, 0), // ⭐ Más cerca del suelo
                                Scale = new Vector3(1, 1, 1)
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["PlayerControllableComponent"] =
                                JsonSerializer.SerializeToElement(new PlayerControllableComponent
                                    { Speed = 5f }), // ⭐ Velocidad más conservadora
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent()),
                            ["PhysicsBodyComponent"] =
                                JsonSerializer.SerializeToElement(new PhysicsBodyComponent { Mass = 1f })
                        }
                    },

                    // ⭐ CÁMARA CON CONFIGURACIÓN MEJORADA
                    new EntityDefinition("MainCamera", "Camera")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["GameCameraComponent"] = JsonSerializer.SerializeToElement(new GameCameraComponentData
                            {
                                TargetEntityName = "Player",
                                Offset = new Vector3(0, 5, -10), // ⭐ Offset más conservador
                                Camera = new CameraData
                                {
                                    Position = new Vector3(0, 7,
                                        -10), // ⭐ Posición inicial calculada: Player(0,2,0) + Offset(0,5,-10)
                                    Target = new Vector3(0, 2, 0), // ⭐ Apunta al jugador inicialmente
                                    Up = new Vector3(0.0f, 1.0f, 0.0f),
                                    Fovy = 45.0f,
                                    Projection = (int)CameraProjection.Perspective
                                }
                            })
                        }
                    },

                    // Objetos de prueba
                    new EntityDefinition("TestCube1", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent
                            {
                                Position = new Vector3(5f, 1f, 0f),
                                Scale = new Vector3(1, 1, 1)
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent())
                        }
                    },

                    new EntityDefinition("TestCube2", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent
                            {
                                Position = new Vector3(-5f, 1f, 0f),
                                Scale = new Vector3(1, 1, 1)
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent())
                        }
                    }
                }
            };

            SceneSerializer.SaveScene(exampleScene, path);
            Console.WriteLine($"Escena de ejemplo v2 creada en: {path}");
        }
    }
}