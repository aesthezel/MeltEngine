using System;
using System.Threading.Tasks;
using MeltEngine.Core;

namespace MeltEngine;

public class Program
{
    private static async Task Main()
    {
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
        await Window.Init(1920, 1080, frameRate: 120);
    }

    static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Exception e = (Exception)args.ExceptionObject;
        Console.WriteLine("!!!!!!!!!! SE HA PRODUCIDO UNA EXCEPCIÓN GLOBAL NO CONTROLADA !!!!!!!!!!");
        Console.WriteLine("La aplicación se cerrará.");
        Console.WriteLine("Detalles del error:");
        Console.WriteLine(e.ToString());
            
        Console.WriteLine("\nPresiona Enter para cerrar...");
        Console.ReadLine();
        Environment.Exit(1);
    }
}