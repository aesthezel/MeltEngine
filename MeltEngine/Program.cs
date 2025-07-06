using System;
using System.Threading.Tasks;
using MeltEngine.Core;

namespace MeltEngine
{
    class Program
    {
        private static async Task Main()
        {
            // Adjuntar un gestor para CUALQUIER excepción no controlada en el dominio de la aplicación.
            // Esto es nuestra mejor herramienta para atrapar errores que silencian la aplicación.
            AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;

            // Iniciar la ventana y el motor
            await Window.Init(1280, 720, frameRate: 120);
        }

        static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("!!!!!!!!!! SE HA PRODUCIDO UNA EXCEPCIÓN GLOBAL NO CONTROLADA !!!!!!!!!!");
            Console.WriteLine("La aplicación se cerrará.");
            Console.WriteLine("Detalles del error:");
            Console.WriteLine(e.ToString());
            
            // Pausar la consola para poder leer el error antes de que se cierre.
            Console.WriteLine("\nPresiona Enter para cerrar...");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}