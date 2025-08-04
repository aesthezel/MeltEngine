using System;
using System.IO;

namespace MeltEngine.Utils;

public static class PathHelper
{
    public static string GetEntityFolderPath()
    {
        DirectoryInfo parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent;
        var parentOfParent = parent?.Parent;
        var projectFolder = parentOfParent?.Parent;
        
        if (projectFolder == null) return string.Empty;
        var projectDirectory = projectFolder.FullName;
        
        var entityPath = Path.Combine(projectDirectory, "Entity");
            
        Console.WriteLine($"Entity Path: {entityPath}");
        return entityPath;
    }
}   