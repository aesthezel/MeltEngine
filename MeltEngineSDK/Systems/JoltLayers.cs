using JoltPhysicsSharp;

namespace MeltEngine.Systems;

public static class JoltConfig
{
    public const uint NumObjectLayers = 4;
    public const uint NumBroadPhaseLayers = 3;

    public static readonly ObjectLayer LayerNonMoving = new ObjectLayer(0);
    public static readonly ObjectLayer LayerMoving = new ObjectLayer(1);
    public static readonly ObjectLayer LayerDynamicFar = new ObjectLayer(2);
    public static readonly ObjectLayer LayerDisabled = new ObjectLayer(3);

    public static readonly BroadPhaseLayer BPNonMoving = new BroadPhaseLayer(0);
    public static readonly BroadPhaseLayer BPMoving = new BroadPhaseLayer(1);
    public static readonly BroadPhaseLayer BPFar = new BroadPhaseLayer(2);
}
