namespace MeltEngine.Core
{
    public static class Time
    {
        public static float DeltaTime { get; set; }
        public static float FixedDeltaTime { get; set; } = 1f / 50f;
        public static float Alpha { get; set; }
    }
}
