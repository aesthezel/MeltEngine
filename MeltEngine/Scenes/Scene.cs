using System;
using System.Collections.Generic;
using System.Linq;
using MeltEngine.Entities;
// using MeltEngine.Entities.Components; // GameCameraComponent might be used later
// using Raylib_CsLo; // Not used directly in Scene management logic for now

namespace MeltEngine.Core.Scenes
{
    public class Scene
    {
        private static uint _nextId;
        public uint Id { get; private set; }
        public string Name { get; private set; }

        // public GameCamera MainCamera { get; private set; } // To be handled by ECS Entity + GameCameraComponent

        private readonly List<Entity> _entities = new();

        public Scene(string name)
        {
            Id = _nextId++;
            Name = name;
        }

        public void AddEntity(Entity entity)
        {
            if (entity != null && !_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
        }

        public void RemoveEntity(Entity entity, ECSOperator ecsOperator)
        {
            if (entity != null && _entities.Remove(entity))
            {
                ecsOperator.DestroyEntity(entity);
            }
        }

        public IEnumerable<Entity> GetEntities() => _entities.AsReadOnly();

        // public void Present()
        // {
        //     Workflow.OnUpdate?.Invoke();
        //     
        //     Raylib.BeginDrawing();
        //     Raylib.ClearBackground(Raylib.RAYWHITE);
        //        
        //     // if (MainCamera != null) Raylib.BeginMode3D(MainCamera.Camera);
        //     Workflow.OnRender?.Invoke();
        //     
        //     Raylib.DrawGrid(50, 1);
        //     
        //     Workflow.OnEndRender?.Invoke();
        //     // if (MainCamera != null) Raylib.EndMode3D();
        //         
        //     Raylib.EndDrawing();
        // }
    }
}