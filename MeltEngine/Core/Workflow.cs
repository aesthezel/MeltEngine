using System;
using MeltEngine.Entity;
using Raylib_CsLo;

namespace MeltEngine.Core
{
    public static class Workflow
    {
        public static Action OnInit;
        public static Action OnUpdate;
        public static Action OnRender;
        public static Action OnQuit;

        public static void Run()
        {
            // TODO: Scenes goes here!
            
            GameObject goTest = new("Test", true);

            OnInit?.Invoke();

            while (!Raylib.WindowShouldClose())
            {
                // Update
                OnUpdate?.Invoke();

                // Draw frame
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Raylib.RAYWHITE); // TODO: Add into a scene...
                OnRender?.Invoke();
                Raylib.EndDrawing();
            }
            
            OnQuit?.Invoke();
            
            Raylib.CloseWindow();
        }
    }
}