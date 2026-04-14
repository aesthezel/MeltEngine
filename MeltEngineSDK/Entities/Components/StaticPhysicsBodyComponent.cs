using System.Text.Json.Serialization;
using MagicPhysX;

namespace MeltEngine.Entities.Components;

public unsafe struct StaticPhysicsBodyComponent
{
    [JsonIgnore]
    public PxRigidStatic* Actor;

    public bool UseLod { get; init; }
    public float LodDisableDistance { get; init; }
    
    [JsonIgnore]
    public bool IsLodDisabled { get; set; }
}