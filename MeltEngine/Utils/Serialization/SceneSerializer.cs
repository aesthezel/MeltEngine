using System;
using System.IO;
using System.Text.Json;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using MeltEngine.Core;
using MeltEngine.Core.Scenes;
using MeltEngine.Scenes.Definitions;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
// Asegúrate que Raylib_CsLo.Camera3D es accesible, si no, añade el using correspondiente.
// using Raylib_CsLo;

namespace MeltEngine.Utils.Serialization
{
    public static class SceneSerializer
    {
        public static Scene LoadSceneFromJson(string filePath, ECSOperator ecsOperator)
        {
            if (ecsOperator == null)
            {
                Console.WriteLine("Error: ECSOperator cannot be null.");
                return null;
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Scene file not found at {filePath}");
                return null;
            }

            string jsonString;
            try
            {
                jsonString = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading scene file {filePath}: {ex.Message}");
                return null;
            }

            SceneDefinition sceneDefinition;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                sceneDefinition = JsonSerializer.Deserialize<SceneDefinition>(jsonString, options);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing scene JSON from {filePath}: {ex.Message}");
                if (ex.Path != null)
                {
                    Console.WriteLine($"JSON Path: {ex.Path}, Line: {ex.LineNumber}, BytePosition: {ex.BytePositionInLine}");
                }
                return null;
            }

            if (sceneDefinition == null)
            {
                Console.WriteLine($"Error: Deserialized scene definition from {filePath} is null.");
                return null;
            }

            Scene newScene = new Scene(sceneDefinition.Name ?? Path.GetFileNameWithoutExtension(filePath));
            Dictionary<string, Entity> entityNameMap = new Dictionary<string, Entity>();

            if(sceneDefinition.Entities == null)
            {
                Console.WriteLine($"Warning: Scene '{newScene.Name}' has no entities defined in the JSON.");
                sceneDefinition.Entities = new List<EntityDefinition>(); // Evita NullReferenceException más adelante
            }

            foreach (var entityDef in sceneDefinition.Entities)
            {
                if (entityDef == null)
                {
                    Console.WriteLine("Warning: Found a null entity definition in the scene file. Skipping.");
                    continue;
                }
                Entity newEntity = ecsOperator.CreateEntity();
                newScene.AddEntity(newEntity);

                if (!string.IsNullOrEmpty(entityDef.Name))
                {
                    if (!entityNameMap.TryAdd(entityDef.Name, newEntity))
                    {
                        Console.WriteLine($"Warning: Duplicate entity name '{entityDef.Name}' in scene '{newScene.Name}'. Only the first instance will be reliably targetable by this name.");
                    }
                }
            }

            List<Entity> createdEntities = newScene.GetEntities().ToList();

            for (int i = 0; i < sceneDefinition.Entities.Count; i++)
            {
                EntityDefinition entityDef = sceneDefinition.Entities[i];
                 if (entityDef == null) continue; // Ya se advirtió antes

                Entity currentEntity;
                if (!string.IsNullOrEmpty(entityDef.Name) && entityNameMap.TryGetValue(entityDef.Name, out var namedEntity))
                {
                    currentEntity = namedEntity;
                }
                else if (i < createdEntities.Count)
                {
                    currentEntity = createdEntities[i];
                }
                else
                {
                    Console.WriteLine($"Critical Error: Could not retrieve created entity for definition (Name: {entityDef.Name ?? "N/A"}, Index: {i}). This should not happen. Skipping component processing.");
                    continue;
                }

                if (entityDef.Components == null)
                {
                    continue;
                }

                foreach (var componentEntry in entityDef.Components)
                {
                    string componentName = componentEntry.Key;
                    JsonElement componentData = componentEntry.Value;

                    try
                    {
                        ProcessComponent(ecsOperator, currentEntity, entityDef, componentName, componentData, entityNameMap);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing component '{componentName}' for entity '{entityDef.Name ?? "Unnamed"}': {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                }
            }

            SceneService.AddScene(newScene);
            SceneService.LoadScene(newScene.Name);

            Console.WriteLine($"Scene '{newScene.Name}' loaded successfully with {newScene.GetEntities().Count()} entities.");
            return newScene;
        }

        private static void ProcessComponent(ECSOperator ecsOperator, Entity currentEntity, EntityDefinition entityDef, string componentName, JsonElement componentData, Dictionary<string, Entity> entityNameMap)
        {
            var componentJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            switch (componentName)
            {
                case "CoordComponent":
                    var coordDef = componentData.Deserialize<CoordComponentDefinition>(componentJsonOptions);
                    if (coordDef != null)
                    {
                        ecsOperator.AddComponent(currentEntity, new CoordComponent
                        {
                            Position = new Vector3(coordDef.Position?.X ?? 0f, coordDef.Position?.Y ?? 0f, coordDef.Position?.Z ?? 0f),
                            Rotation = coordDef.Rotation != null ?
                                new Quaternion(coordDef.Rotation.X, coordDef.Rotation.Y, coordDef.Rotation.Z, coordDef.Rotation.W) :
                                Quaternion.Identity,
                            Scale = new Vector3(coordDef.Scale?.X ?? 1f, coordDef.Scale?.Y ?? 1f, coordDef.Scale?.Z ?? 1f)
                        });
                    }
                    else { Console.WriteLine($"Warning: Could not deserialize CoordComponent data for entity '{entityDef.Name ?? "Unnamed"}'."); }
                    break;

                case "EnabledComponent":
                    ecsOperator.AddComponent(currentEntity, new EnabledComponent());
                    break;

                case "StaticPhysicsBodyComponent":
                    ecsOperator.AddComponent(currentEntity, new StaticPhysicsBodyComponent());
                    break;

                case "CubeRendererComponent":
                    ecsOperator.AddComponent(currentEntity, new CubeRendererComponent());
                    break;

                case "PlayerControllableComponent":
                    var playerCtrlDef = componentData.Deserialize<PlayerControllableComponentDefinition>(componentJsonOptions);
                    if (playerCtrlDef != null)
                    {
                        ecsOperator.AddComponent(currentEntity, new PlayerControllableComponent
                        {
                            Speed = playerCtrlDef.Speed
                        });
                    }
                    else { Console.WriteLine($"Warning: Could not deserialize PlayerControllableComponent data for entity '{entityDef.Name ?? "Unnamed"}'."); }
                    break;

                case "GameCameraComponent":
                    var gameCamDef = componentData.Deserialize<GameCameraComponentDefinition>(componentJsonOptions);
                    if (gameCamDef != null)
                    {
                        Entity targetEntity = null;
                        if (!string.IsNullOrEmpty(gameCamDef.TargetEntityName))
                        {
                            if (entityNameMap.TryGetValue(gameCamDef.TargetEntityName, out var foundTarget))
                            {
                                targetEntity = foundTarget;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Target entity '{gameCamDef.TargetEntityName}' not found for GameCameraComponent on entity '{entityDef.Name ?? "Unnamed"}'. Camera target will be unassigned.");
                            }
                        }

                        var camDef = gameCamDef.Camera;
                        var camPosDef = camDef?.Position;
                        var camTgtDef = camDef?.Target;
                        var camUpDef = camDef?.Up;

                        ecsOperator.AddComponent(currentEntity, new GameCameraComponent
                        {
                            TargetEntity = targetEntity, // Assign even if null
                            Offset = new Vector3(gameCamDef.Offset?.X ?? 0f, gameCamDef.Offset?.Y ?? 0f, gameCamDef.Offset?.Z ?? 0f),
                            Camera = new Raylib_CsLo.Camera3D // Ensure Raylib_CsLo is available
                            {
                                position = new Vector3(camPosDef?.X ?? 0f, camPosDef?.Y ?? 0f, camPosDef?.Z ?? 0f),
                                target = new Vector3(camTgtDef?.X ?? 0f, camTgtDef?.Y ?? 0f, camTgtDef?.Z ?? 0f),
                                up = new Vector3(camUpDef?.X ?? 0f, camUpDef?.Y ?? 1.0f, camUpDef?.Z ?? 0f),
                                fovy = camDef?.Fovy ?? 45.0f,
                                projection = camDef?.Projection ?? 0
                            }
                        });
                    }
                    else { Console.WriteLine($"Warning: Could not deserialize GameCameraComponent data for entity '{entityDef.Name ?? "Unnamed"}'."); }
                    break;

                default:
                    Console.WriteLine($"Warning: Unknown component type '{componentName}' for entity '{entityDef.Name ?? "Unnamed"}'. This component will be skipped.");
                    break;
            }
        }
    }
}
