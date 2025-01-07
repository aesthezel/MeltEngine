using System;
using System.Numerics;
using MeltEngine.Core.Scenes;
using MeltEngine.Entity;
using MeltEngine.Entity.Components;
using MeltEngine.Entity.Components.Gameplay;
using Raylib_CsLo;

namespace MeltEngine.Core
{
    public static class Workflow
    {
        public static Action OnInit;
        public static Action OnUpdate;
        public static Action OnRender;
        public static Action OnEndRender;
        public static Action OnQuit;

        public static void Run()
        {
            GameObject cubeObject = new("Cube", true);
            cubeObject.AddBehaviour(new Coord { Position = new Vector3(1, 1, 1), Size = new Vector3(1, 1, 1) });
            cubeObject.AddBehaviour(new CubeRenderer());
            cubeObject.AddBehaviour(new Movement());
            
            GameObject cameraObject = new("Camera", true);
            GameCamera camera = new();
            cameraObject.AddBehaviour(camera);
            
            camera.Follow(cubeObject);
            
            Scene mainScene = new("Main", cameraObject);
            mainScene.AddGameObject(cubeObject);
            
            SceneService.AddScene(mainScene);
            SceneService.LoadScene("Main");

            OnInit?.Invoke();

            while (!Raylib.WindowShouldClose())
            {
                SceneService.GetActiveScene().Present();
            }

            Raylib.CloseWindow();
        }
    }
}