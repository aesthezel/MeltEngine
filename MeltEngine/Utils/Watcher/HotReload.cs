using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using MeltEngine.Core;
using MeltEngine.Core.Scenes;
using MeltEngine.Entity;
using MeltEngine.Entity.Components;
using MeltEngine.Entity.Components.Gameplay;

namespace MeltEngine.Utils.Watcher;

public static class HotReload
{
    private static Assembly _currentAssembly;
    private static readonly Dictionary<string, Type> LoadedTypes = new();

    public static void Reload(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location)
            .Where(location => !string.IsNullOrEmpty(location))
            .ToArray();

        foreach (var reference in references)
        {
            if (!File.Exists(reference))
            {
                Console.WriteLine($"[HotReload] Reference {reference} don't exist.");
            }
        }

        var compilation = CSharpCompilation.Create("DynamicAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(references.Select(r => MetadataReference.CreateFromFile(r)))
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (result.Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());

            ReplaceAssembly(assembly);
        }
        else
        {
            Console.WriteLine("[HotReload] Compilation failed.");
        }
    }

    private static void ReplaceAssembly(Assembly assembly)
    {
        _currentAssembly = assembly;
        Console.WriteLine($"[HotReload] Loaded new assembly: {assembly.FullName}");

        var modifiedTypes = new List<Type>();
        LoadedTypes.Clear();

        foreach (var type in assembly.GetTypes())
        {
            if (type.FullName == null) continue;

            LoadedTypes[type.FullName] = type;

            var oldType = _currentAssembly?.GetType(type.FullName);
            if (oldType != null && oldType.FullName == type.FullName) 
            {
                // Si el tipo existe, lo agregamos a la lista de modificados
                modifiedTypes.Add(type);
                Console.WriteLine($"[HotReload] Type modified: {type.FullName}");
            }
        }

        ReloadBehavioursInActiveScene(modifiedTypes);
    }

    private static void ReloadBehavioursInActiveScene(List<Type> modifiedTypes)
    {
        var scene = SceneService.GetActiveScene();
        if (scene == null) return;

        Console.WriteLine("[HotReload] Reloading behaviours in active scene...");

        foreach (var gameObject in scene.GetGameObjects())
        {
            foreach (var type in modifiedTypes)
            {
                Console.WriteLine($"[HotReload] Checking for {type.Name} in {gameObject.Name}...");

                // Buscar comportamientos por nombre de tipo
                var behaviours = gameObject.GetBehaviours();
                var behaviour = behaviours.FirstOrDefault(b => b is Movement);

                if (behaviour == null)
                {
                    Console.WriteLine($"[HotReload] No {type.Name} found in {gameObject.Name}.");
                    continue;
                }

                Console.WriteLine($"[HotReload] Found {type.Name} in {gameObject.Name}. Removing...");

                gameObject.RemoveBehaviour(behaviour);

                // Instanciar nuevo comportamiento del tipo actualizado
                if (LoadedTypes.TryGetValue(type.FullName, out var newType))
                {
                    var constructor = newType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        var newBehaviour = (Behaviour)constructor.Invoke(null);
                        gameObject.AddBehaviour(newBehaviour);
                        Console.WriteLine($"[HotReload] Added new {newType.Name} to {gameObject.Name}.");
                    }
                    else
                    {
                        Console.WriteLine($"[HotReload] Constructor not found for {type.Name}.");
                    }
                }
            }
        }

        Workflow.OnInit?.Invoke();
    }
}