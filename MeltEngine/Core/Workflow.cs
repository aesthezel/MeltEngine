using System;
using MeltEngine.Entity;
using Raylib_CsLo;

namespace MeltEngine.Core
{
    public static class Workflow
    {
        public static event Action OnInit;
        public static event Action OnUpdate;
        public static event Action OnRender;
        public static event Action OnQuit;

        public static void Run()
        {
            // TODO: Scenes goes here!
            GameObject goTest = new("Test", true);

            OnInit?.Invoke();

            while (!Raylib.WindowShouldClose())
            {
                // Update
                OnUpdate?.Invoke();

                if(Raylib.IsKeyPressed(KeyboardKey.KEY_A))
                {
                    goTest.Enabled = !goTest.Enabled;
                    Console.WriteLine($"{goTest.Name} has enabled? {goTest.Enabled}");
                }

                // Draw frame
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib.RAYWHITE); // TODO: Create a camera static component
                OnRender?.Invoke();
                Raylib.EndDrawing();
            }
            
            OnQuit?.Invoke();
            
            Raylib.CloseWindow();
        }
    }
}