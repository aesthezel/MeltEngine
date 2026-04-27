using System.Text.Json.Serialization;
using JoltPhysicsSharp;

namespace MeltEngine.Entities.Components;

public struct StaticPhysicsBodyComponent
{
    [JsonIgnore]
    public BodyID BodyId = BodyID.Invalid;

    public bool UseLod { get; init; }
    public float LodDisableDistance { get; init; }

    [JsonIgnore]
    public bool IsLodDisabled { get; set; }

    public StaticPhysicsBodyComponent() { }
}