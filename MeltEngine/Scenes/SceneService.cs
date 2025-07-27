using System;
using System.Collections.Generic;

namespace MeltEngine.Core.Scenes;

public static class SceneService
{
    private static readonly Dictionary<string, Scene> Scenes = new();
    private static Scene _activeScene;

    public static void AddScene(Scene scene)
    {
        Scenes.TryAdd(scene.Name, scene);
    }

    public static void LoadScene(string name)
    {
        if (!Scenes.TryGetValue(name, out var scene)) return;
        _activeScene = scene;
        Console.WriteLine($"Scene {name} loaded");
    }

    public static Scene GetActiveScene() => _activeScene;
}