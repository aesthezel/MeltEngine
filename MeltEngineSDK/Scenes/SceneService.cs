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
                    throw new FileNotFoundException($"ERROR FATAL: No se encontró el archivo de escena en la ruta solicitada: {fullPath}");
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

                case "JumpComponent":
                    var jumpComp = compJsonElement.Deserialize<JumpComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, jumpComp);
                    Console.WriteLine($"  → JumpComponent agregado");
                    break;

                case "CubeSpawnerComponent":
                    var spawner = compJsonElement.Deserialize<CubeSpawnerComponent>(SceneSerializer.Options);
                    entityOperator.AddComponent(entity, spawner);
                    Console.WriteLine($"  → CubeSpawnerComponent: Cooldown={spawner.Cooldown}");
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
                        IsOrbitMode = cameraData.IsOrbitMode,
                        Yaw = cameraData.Yaw,
                        Pitch = cameraData.Pitch,
                        Distance = cameraData.Distance,
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


    }
}