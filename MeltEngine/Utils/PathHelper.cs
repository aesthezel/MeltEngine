using System;
using System.IO;

namespace MeltEngine.Utils;

public static class PathHelper
{
    public static string GetEntityFolderPath()
    {
        // Obtiene el directorio del proyecto (la raíz del código fuente)
        DirectoryInfo parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent;
        var parentOfParent = parent?.Parent;
        var projectFolder = parentOfParent?.Parent;
        
        if (projectFolder == null) return string.Empty;
        var projectDirectory = projectFolder.FullName;

        // Une el path con la carpeta 'Entity' dentro del proyecto
        var entityPath = Path.Combine(projectDirectory, "Entity");
            
        Console.WriteLine($"Entity Path: {entityPath}");
        return entityPath;
    }
}   