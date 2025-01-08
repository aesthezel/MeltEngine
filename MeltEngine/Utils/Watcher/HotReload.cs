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
                Console.WriteLine($"Reference {reference} don't exist.");
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
            Console.WriteLine("Compilation failed.");
        }
    }

    private static void ReplaceAssembly(Assembly assembly)
    {
        _currentAssembly = assembly;
        Console.WriteLine($"Loaded new assembly: {assembly.FullName}");
    
        LoadedTypes.Clear();
        foreach (var type in assembly.GetTypes())
        {
            if (type.FullName != null)
            {
                Console.WriteLine($"Registering type: {type.FullName}");
                LoadedTypes[type.FullName] = type;
            }
        }
    
        ReloadBehavioursInActiveScene();
    }
    
    private static void ReloadBehavioursInActiveScene()
    {
        var scene = SceneService.GetActiveScene();
        if (scene == null) return;

        foreach (var gameObject in scene.GetGameObjects())
        {
            var movement = gameObject.GetBehaviour<Movement>();
            if (movement == null) continue;
        
            Console.WriteLine($"[HotReload] Removed movement from {gameObject.Name}");
            gameObject.RemoveBehaviour(movement);
            
            var movementType = LoadedTypes.TryGetValue("MeltEngine.Entity.Components.Gameplay.Movement", out var type) ? type : null;

            if (movementType != null)
            {
                var constructor = movementType.GetConstructor(new[] { typeof(float) });
                if (constructor != null)
                {
                    var newMovement = (IComponent)constructor.Invoke([200f]);
                    gameObject.AddBehaviour((Behaviour)newMovement);
                }
                else
                {
                    Console.WriteLine("[HotReload] Constructor not found for Movement.");
                }
            }
        }

        Workflow.OnInit?.Invoke(); // TODO: create a new method for Init and another for Start
    }
}