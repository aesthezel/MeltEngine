using System.Collections.Generic;
using System.Numerics;
using MeltEngine.Entity;
using MeltEngine.Entity.Components;
using Raylib_CsLo;

namespace MeltEngine.Core.Scenes
{
    public class Scene
    {
        private static uint _id;
        public uint Id { get; private set; }
        public string Name { get; private set; }
        public GameCamera MainCamera { get; private set; }
        
        private readonly List<GameObject> _gameObjects = new();

        public Scene(string name, GameObject cameraGameObject = null)
        {
            Id = _id++;
            Name = name;

            // Config main camera
            AddGameObject(cameraGameObject);
            var camera = cameraGameObject.GetBehaviour<GameCamera>();
            if(camera != null) MainCamera = camera;
        }
        
        public void AddGameObject(GameObject gameObject)
        {
            _gameObjects.Add(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            _gameObjects.Remove(gameObject);
            gameObject.Destroy();
        }

        public void Present()
        {
            Workflow.OnUpdate?.Invoke();
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.RAYWHITE);
               
            if (MainCamera != null) Raylib.BeginMode3D(MainCamera.Camera);
            Workflow.OnRender?.Invoke();
            
            Raylib.DrawGrid(50, 1);
            
            Workflow.OnEndRender?.Invoke();
            if (MainCamera != null) Raylib.EndMode3D();
                
            Raylib.EndDrawing();
            

        }
    }
}