using System;
using System.IO;
using System.Numerics;
using MeltEngine.Core.Scenes;
using MeltEngine.Entity;
using MeltEngine.Entity.Components;
using MeltEngine.Entity.Components.Gameplay;
using MeltEngine.Entity.Components.Physics;
using MeltEngine.Physics;
using MeltEngine.Utils.Watcher;
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
            FileWatcher.StartWatching();

            var physicsSystem = new PhysicsSystem();

            GameObject planeObject = new("Plane", true);
            planeObject.AddBehaviour(new PlanePhysic(physicsSystem, 500f, 500f, new Vector3(0, 0, 0)));

            Vector3 cubePosition = new Vector3(0, 10, 0);
            GameObject cubeObject = new("Cube", true);
            cubeObject.AddBehaviour(new Coord { Position = cubePosition, Size = new Vector3(1, 1, 1) });
            cubeObject.AddBehaviour(new CubeRenderer());
            cubeObject.AddBehaviour(new Movement(200f));
            cubeObject.AddBehaviour(new CubePhysics(physicsSystem, cubePosition, 1, 300f));

            GameObject cameraObject = new("Camera", true);
            GameCamera camera = new();
            cameraObject.AddBehaviour(camera);

            camera.Follow(cubeObject);

            Scene mainScene = new("Main", cameraObject);
            mainScene.AddGameObject(cubeObject);
            mainScene.AddGameObject(planeObject);

            SceneService.AddScene(mainScene);
            SceneService.LoadScene("Main");

            OnInit?.Invoke();

            while (!Raylib.WindowShouldClose())
            {
                OnUpdate?.Invoke(); // Llamar al evento OnUpdate si se ha definido

                physicsSystem.Simulate(1.0f / 60.0f);
                SceneService.GetActiveScene().Present();
            }

            Raylib.CloseWindow();

            FileWatcher.StopWatching();
        }
    }
}
