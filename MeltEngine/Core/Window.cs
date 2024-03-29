﻿using Raylib_CsLo;

namespace MeltEngine.Core
{
    public static class Window
    {
        public static void Init(int width, int height, string name = "[MeltEngine]", int frameRate = 60)
        {
            Raylib.InitWindow(width, height, name);
            Raylib.SetTargetFPS(frameRate);

            Workflow.Run();
        }
    }
}