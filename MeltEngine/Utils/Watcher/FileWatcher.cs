using System;
using System.IO;

namespace MeltEngine.Utils.Watcher
{
    public static class FileWatcher
    {
        private static FileSystemWatcher _watcher;

        public static void StartWatching()
        {
            string entityFolderPath = PathHelper.GetEntityFolderPath();
            
            if (string.IsNullOrEmpty(entityFolderPath))
            {
                Console.WriteLine("[FileWatcher] Entity folder path is empty.");
                return;
            }

            if (!Directory.Exists(entityFolderPath))
            {
                Console.WriteLine($"[FileWatcher] Directory {entityFolderPath} don't exists.");
                return;
            }
            
            _watcher = new FileSystemWatcher(entityFolderPath)
            {
                Filter = "*.cs", // Filtro para observar archivos .cs
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true // Incluye subdirectorios
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnDeleted;

            _watcher.EnableRaisingEvents = true;
            Console.WriteLine($"[FileWatcher] Init on: {entityFolderPath}");
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File changed: {e.FullPath}");

            // Verificar si la ruta no está vacía o nula
            if (string.IsNullOrEmpty(e.FullPath))
            {
                Console.WriteLine("[FileWatcher] Empty or null path");
                return;
            }

            System.Threading.Thread.Sleep(100);
            
            try
            {
                using FileStream fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader sr = new StreamReader(fs);
                var code = sr.ReadToEnd();
                HotReload.Reload(code);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[FileWatcher] Error to access into file: {ex.Message}");
            }
        }



        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File renamed: {e.OldFullPath} -> {e.FullPath}");
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File are deleted: {e.FullPath}");
        }

        public static void StopWatching()
        {
            _watcher?.Dispose();
            Console.WriteLine("[FileWatcher] Stopped");
        }
    }
}