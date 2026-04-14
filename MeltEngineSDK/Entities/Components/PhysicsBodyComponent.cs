using System.Text.Json.Serialization;
using MagicPhysX;

namespace MeltEngine.Entities.Components;

public unsafe struct PhysicsBodyComponent
{
    [JsonIgnore]
    public PxRigidDynamic* Actor;
    public float Mass { get; init; }
    
    public bool UseLod { get; init; }
    public float LodDisableDistance { get; init; }
    
    [JsonIgnore]
    public bool IsLodDisabled { get; set; }
    
    [JsonIgnore]
    public bool WasForcedSleepByCamera { get; set; }
}