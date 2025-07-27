using System;
using System.IO;
// System.Numerics is likely still needed by components or Raylib types, so keep it.
using System.Numerics;
using MeltEngine.Core.Scenes;
// Old Entity system references are removed as we shift to ECS for scene loading
// using MeltEngine.Entity;
// using MeltEngine.Entity.Components;
// using MeltEngine.Entity.Components.Gameplay;
// using MeltEngine.Entity.Components.Physics;
// using MeltEngine.Physics; // Old physics system
using MeltEngine.Utils.Watcher; // For FileWatcher
using MeltEngine.Utils.Serialization; // Required for SceneSerializer
using MeltEngine.Entities; // Required for ECSOperator
// using MeltEngine.Systems; // Namespace for your ECS Systems (MovementSystem, RenderSystem, etc.)
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
            // 1. Initialize ECS Operator
            ECSOperator ecsOperator = new ECSOperator();

            // TODO: Initialize your ECS Systems here. They will operate on entities and components.
            // These systems would typically take ecsOperator as a dependency.
            // Examples:
            // var lifecycleSystem = new LifecycleSystem(ecsOperator); // Handles entity creation/destruction events
            // var movementSystem = new MovementSystem(ecsOperator);
            // var physicsSystemECS = new PhysicSystem(ecsOperator);
            // var cameraSystem = new CameraSystem(ecsOperator);
            // var renderSystem = new RenderSystem(ecsOperator, window); // Window might be needed for rendering context

            // Configure FileWatcher if still needed and if it has an enable/disable mechanism
            // For now, assuming it's always enabled if used.
            // if (FileWatcher.IsEnabled)
            // {
            //     FileWatcher.StartWatching();
            // }
            // HotReload.Init(ecsOperator); // Example if HotReload needs ECSOperator

            // 2. Load the initial scene using SceneSerializer
            string sceneToLoad = "default.json"; // Or read from config, command line, etc.
            string sceneFilePath;

            // Attempt to locate Assets folder relative to the executable
            string executionPath = AppDomain.CurrentDomain.BaseDirectory;
            // Try to find the project root. This assumes the executable is in a subdirectory like bin/Debug/netX.Y
            // Adjust the number of ".." if your build output depth is different.
            string projectRootGuess = Path.GetFullPath(Path.Combine(executionPath, @"..\..\..\"));

            // Path to the Assets/Scenes directory from the guessed project root
            string assetsScenesPathFromProjectRoot = Path.Combine(projectRootGuess, "Assets", "Scenes");
            // Path to the Assets/Scenes directory if it's directly next to the executable
            string assetsScenesPathFromExecutionDir = Path.Combine(executionPath, "Assets", "Scenes");

            if (Directory.Exists(assetsScenesPathFromExecutionDir))
            {
                sceneFilePath = Path.Combine(assetsScenesPathFromExecutionDir, sceneToLoad);
                Console.WriteLine($"Found Assets/Scenes next to executable: {assetsScenesPathFromExecutionDir}");
            }
            // Check if "MeltEngine/Assets/Scenes" exists from project root (if project is cloned inside another folder)
            else if (Directory.Exists(Path.Combine(projectRootGuess, "MeltEngine", "Assets", "Scenes")))
            {
                sceneFilePath = Path.Combine(projectRootGuess, "MeltEngine", "Assets", "Scenes", sceneToLoad);
                Console.WriteLine($"Found Assets/Scenes in MeltEngine subfolder from project root: {Path.Combine(projectRootGuess, "MeltEngine", "Assets", "Scenes")}");
            }
            // Check if "Assets/Scenes" exists directly from project root
            else if (Directory.Exists(assetsScenesPathFromProjectRoot))
            {
                 sceneFilePath = Path.Combine(assetsScenesPathFromProjectRoot, sceneToLoad);
                 Console.WriteLine($"Found Assets/Scenes directly in project root: {assetsScenesPathFromProjectRoot}");
            }
            else // Fallback or error
            {
                Console.WriteLine($"Warning: Could not automatically determine Assets/Scenes path. Trying relative path '{sceneToLoad}'.");
                sceneFilePath = sceneToLoad; // May or may not work depending on CWD
            }

            // If you have a PathHelper class designed for this:
            // sceneFilePath = MeltEngine.Utils.PathHelper.GetPath($"Assets/Scenes/{sceneToLoad}");

            Console.WriteLine($"Attempting to load scene from: {sceneFilePath}");
            Scene loadedScene = SceneSerializer.LoadSceneFromJson(sceneFilePath, ecsOperator);

            if (loadedScene == null)
            {
                Console.WriteLine($"CRITICAL: Failed to load the initial scene from '{sceneFilePath}'. Application may not function correctly.");
                // Fallback: Create a minimal default scene or exit
                loadedScene = new Scene("FallbackEmptyScene");
                SceneService.AddScene(loadedScene);
                SceneService.LoadScene(loadedScene.Name);
                Console.WriteLine("Loaded a fallback empty scene.");
                // Alternatively, to exit:
                // Raylib.CloseWindow();
                // OnQuit?.Invoke(); // Call OnQuit before returning if exiting
                // return;
            }

            // Old manual scene and GameObject setup is now replaced by JSON loading.
            // All GameObject, mainScene, old physicsSystem creation is removed.
            // SceneService.AddScene and LoadScene are now handled by SceneSerializer.

            OnInit?.Invoke(); // General initialization event for other parts of the application

            // Main game loop
            while (!Raylib.WindowShouldClose())
            {
                float deltaTime = Raylib.GetFrameTime(); // Time since last frame

                // --- Update Logic ---
                OnUpdate?.Invoke(); // General update event for non-ECS logic (e.g., global input handling)

                // ECS Systems Update (Order might be important)
                // lifecycleSystem?.Update(deltaTime); // Process entities/components added/removed
                // inputSystem?.Update(deltaTime); // If you have one
                // movementSystem?.Update(deltaTime);
                // physicsSystemECS?.Update(deltaTime);
                // cameraSystem?.Update(deltaTime);
                // ... any other game logic systems ...

                // --- Render Logic ---
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib.LIGHTGRAY); // Example background color

                OnRender?.Invoke(); // Pre-render event

                // ECS Render System would draw entities based on their components
                // Example of how systems might interact for rendering:
                // Camera3D activeCamera;
                // if (cameraSystem != null && cameraSystem.TryGetActiveCamera(out activeCamera))
                // {
                //    Raylib.BeginMode3D(activeCamera);
                //    renderSystem?.Draw3D(activeCamera); // Pass camera for culling, matrices
                //    Raylib.DrawGrid(100, 1.0f); // Optional grid for reference
                //    Raylib.EndMode3D();
                // }
                // else
                // {
                //    Raylib.DrawText("No active camera found by CameraSystem.", 10, 30, 20, Raylib.ORANGE);
                // }
                // renderSystem?.Draw2D(); // For UI or 2D elements on top

                Raylib.DrawFPS(10, 10); // Display FPS
                Raylib.DrawText($"Active Scene: {SceneService.GetActiveScene()?.Name ?? "None"}", 10, 30, 20, Raylib.GREEN);
                Raylib.DrawText($"Entities in ECS: {ecsOperator.ActiveEntityCount}", 10, 50, 20, Raylib.GREEN);


                OnEndRender?.Invoke(); // Post-render event
                Raylib.EndDrawing();
            }

            OnQuit?.Invoke(); // Cleanup event for all parts of the application

            Raylib.CloseWindow();

            // Stop FileWatcher if it was started
            // if (FileWatcher.IsEnabled)
            // {
            //     FileWatcher.StopWatching();
            // }
        }
    }
}
