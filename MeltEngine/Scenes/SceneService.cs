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

namespace MeltEngine.Scenes
{
    public class SceneService
    {
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
                
                // NUEVO: Mapeo de nombres a entidades
                var entityNameMap = new Dictionary<string, Entity>();
                var cameraTargetRequests = new List<(Entity cameraEntity, string targetName, GameCameraComponent camera)>();
                
                // PRIMERA PASADA: Crear todas las entidades y mapear nombres
                foreach (var entityDef in scene.Entities)
                {
                    var entity = entityOperator.CreateEntity();
                    entityOperator.AddComponent(entity, new SceneMemberComponent());
                    
                    entityNameMap[entityDef.Name] = entity;
                    Console.WriteLine($"Creando entidad: {entityDef.Name} → Entity {entity.Id}");

                    foreach (var (compTypeName, compJsonElement) in entityDef.Components)
                    {
                        switch (compTypeName)
                        {
                            case "CoordComponent":
                                var coord = compJsonElement.Deserialize<CoordComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, coord);
                                break;
                            
                            case "EnabledComponent":
                                var enabled = compJsonElement.Deserialize<EnabledComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, enabled);
                                break;

                            case "CubeRendererComponent":
                                var cube = compJsonElement.Deserialize<CubeRendererComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, cube);
                                break;

                            case "PlayerControllableComponent":
                                var controllable = compJsonElement.Deserialize<PlayerControllableComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, controllable);
                                break;
                                
                            case "StaticPhysicsBodyComponent":
                                var staticBody = compJsonElement.Deserialize<StaticPhysicsBodyComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, staticBody);
                                break;
                                
                            case "PhysicsBodyComponent":
                                var physicsBody = compJsonElement.Deserialize<PhysicsBodyComponent>(SceneSerializer.Options);
                                entityOperator.AddComponent(entity, physicsBody);
                                break;
                                
                            case "GameCameraComponent":
                                // NUEVO: Deserializar datos de cámara con nombre de target
                                var cameraData = compJsonElement.Deserialize<GameCameraComponentData>(SceneSerializer.Options);
                                
                                var camera = new GameCameraComponent
                                {
                                    Offset = cameraData.Offset,
                                    Camera = new Raylib_CsLo.Camera3D
                                    {
                                        position = cameraData.Camera.Position,
                                        target = cameraData.Camera.Target,
                                        up = cameraData.Camera.Up,
                                        fovy = cameraData.Camera.Fovy,
                                        projection = cameraData.Camera.Projection
                                    }
                                };
                                
                                cameraTargetRequests.Add((entity, cameraData.TargetEntityName, camera));
                                Console.WriteLine($"Cámara '{entityDef.Name}' solicita seguir a '{cameraData.TargetEntityName}'");
                                break;
                            
                            default:
                                Console.WriteLine($"ADVERTENCIA: Componente '{compTypeName}' no manejado. Saltando.");
                                break;
                        }
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
                        Console.WriteLine($"✅ Cámara Entity {cameraEntity.Id} ahora sigue a '{targetName}' (Entity {targetEntity.Id})");
                        Console.WriteLine($"   Offset: {finalCamera.Offset}");
                        Console.WriteLine($"   Initial Cam Pos: {finalCamera.Camera.position}");
                        Console.WriteLine($"   Initial Cam Target: {finalCamera.Camera.target}");
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
                Name = "Escena de Ejemplo v4.0",
                Description = "Una escena básica con un jugador, suelo, cámara que sigue al jugador y algunos objetos",
                Entities = new List<EntityDefinition>
                {
                    new EntityDefinition("Ground", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent 
                            { 
                                Position = new Vector3(0, 5, 0),
                                Scale = new Vector3(1, 1, 1) 
                            }),
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent()),
                            ["StaticPhysicsBodyComponent"] = JsonSerializer.SerializeToElement(new StaticPhysicsBodyComponent())
                        }
                    },

            
                    // Jugador - Posición inicial más alta
                    new EntityDefinition("Player", "Player")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent 
                            { 
                                Position = new Vector3(0, 5, 0), // ← CAMBIO: Más alto (5 en lugar de 2)
                                Scale = new Vector3(1, 1, 1) 
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["PlayerControllableComponent"] = JsonSerializer.SerializeToElement(new PlayerControllableComponent { Speed = 10f }), // ← CAMBIO: Velocidad más baja
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent()),
                            ["PhysicsBodyComponent"] = JsonSerializer.SerializeToElement(new PhysicsBodyComponent { Mass = 1f })
                        }
                    },
            
                    // ⭐ CÁMARA CON OFFSET EXPLÍCITO
                    new EntityDefinition("MainCamera", "Camera")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["GameCameraComponent"] = JsonSerializer.SerializeToElement(new GameCameraComponentData
                            {
                                TargetEntityName = "Player",
                                Offset = new Vector3(0, 8, -15), // ← OFFSET EXPLÍCITO PARA TERCERA PERSONA
                                Camera = new CameraData
                                {
                                    Position = new Vector3(0, 8, -15),
                                    Target = new Vector3(0, 5, 0),
                                    Up = new Vector3(0.0f, 1.0f, 0.0f),
                                    Fovy = 45.0f,
                                    Projection = (int)Raylib_CsLo.CameraProjection.CAMERA_PERSPECTIVE
                                }
                            })
                        }
                    },
            
                    // Cubos decorativos - Más altos también
                    new EntityDefinition("Cube1", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent 
                            { 
                                Position = new Vector3(5f, 1f, 0f), // ← CAMBIO: Más alto y alejado
                                Scale = new Vector3(1, 1, 1) 
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent())
                        }
                    },
            
                    new EntityDefinition("Cube2", "StaticObject")
                    {
                        Components = new Dictionary<string, JsonElement>
                        {
                            ["CoordComponent"] = JsonSerializer.SerializeToElement(new CoordComponent 
                            { 
                                Position = new Vector3(-5f, 1f, 0f), // ← CAMBIO: Más alto y alejado
                                Scale = new Vector3(1, 1, 1) 
                            }),
                            ["CubeRendererComponent"] = JsonSerializer.SerializeToElement(new CubeRendererComponent()),
                            ["EnabledComponent"] = JsonSerializer.SerializeToElement(new EnabledComponent())
                        }
                    }
                }
            };
    
            SceneSerializer.SaveScene(exampleScene, path);
            Console.WriteLine($"Escena de ejemplo v4.0 creada en: {path}");
        }
    }
}