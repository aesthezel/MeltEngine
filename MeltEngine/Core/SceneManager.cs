using System.Collections.Generic;

namespace MeltEngine.Core
{
    public static class SceneManager
    {
        private static readonly List<Scene> scenesInGame = new();
        private static bool lockInstance = false;

        public static void AddScenes(Scene[] scenes)
        {
            if (!lockInstance) return;
            scenesInGame.AddRange(scenes);
            lockInstance = true;
        }
    }
}