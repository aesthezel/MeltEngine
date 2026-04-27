using System.Text.Json.Serialization;
using JoltPhysicsSharp;

namespace MeltEngine.Entities.Components;

public struct PhysicsBodyComponent
{
    [JsonIgnore]
    public BodyID BodyId = BodyID.Invalid;
    public float Mass { get; init; }

    public bool UseLod { get; init; }
    public float LodDisableDistance { get; init; }

    [JsonIgnore]
    public bool IsLodDisabled { get; set; }

    [JsonIgnore]
    public bool IsBuried { get; set; }

    [JsonIgnore]
    public byte ObjectsAboveCount { get; set; }

    [JsonIgnore]
    public byte BuriedFrames { get; set; }

    [JsonIgnore]
    public MotionQuality CurrentMotionQuality { get; set; } = MotionQuality.Discrete;

    [JsonIgnore]
    public float MotionQualityHighDistance { get; set; } = 30f;

    public PhysicsBodyComponent() { }
}