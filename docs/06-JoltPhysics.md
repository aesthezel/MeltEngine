# Jolt Physics Integration

MeltEngine uses [Jolt Physics](https://github.com/jphdm/jolt) via JoltPhysicsSharp wrapper for all physics simulation.

## Why Jolt?

- **Performance**: Optimized for games with high body counts
- **Deterministic**: Cross-platform reproducible physics
- **MIT License**: Open source, permissive
- **Multithreaded**: Job system with thread pool
- **Continuous Collision Detection (CCD)**: Prevents tunneling

## Architecture

### PhysicsManager

Core singleton managing Jolt Physics system:

```csharp
public class PhysicsManager : IDisposable
```

**Key Properties:**
- `PhysicsSystem` - Raw Jolt physics system
- `BodyInterface` - API for body creation/destruction
- `FIXED_TICK_RATE = 50f` - Physics simulation rate
- `FIXED_DELTA_TIME = 1/50s` - Time step per tick

### Initialization Flow

1. `Foundation.Init()` - Initialize Jolt native library
2. Create `JobSystemThreadPool` based on CPU cores
3. Configure layer filtering system
4. Create `PhysicsSystem` with collision settings
5. Set gravity (default: 9.81 m/s² downward)

## Collision Layers

Jolt uses a two-tier collision filtering:

### Object Layers

| Layer | ID | Purpose |
|-------|-----|---------|
| `LayerNonMoving` | 0 | Static geometry (walls, floors) |
| `LayerMoving` | 1 | Active dynamic bodies |
| `LayerDynamicFar` | 2 | LOD-culled distant bodies |
| `LayerDisabled` | 3 | Inactive/disabled bodies |

### Broad Phase Layers

| Layer | ID | Objects |
|-------|-----|---------|
| `BPNonMoving` | 0 | Static colliders |
| `BPMoving` | 1 | Active bodies |
| `BPFar` | 2 | Distant LOD bodies |

### Collision Matrix

```
NonMoving <-> Moving      ✓ Enabled
Moving     <-> Moving     ✓ Enabled
NonMoving  <-> DynamicFar ✓ Enabled
Moving     <-> DynamicFar ✓ Enabled
DynamicFar <-> DynamicFar ✗ Disabled
DynamicFar <-> Disabled   ✗ Disabled
Disabled   <-> *          ✗ Disabled
```

## LOD Physics System

Objects far from camera can be deactivated to save CPU:

```csharp
public void UpdateLod(ECSOperator entityOperator)
```

**Behavior:**
- Calculate distance from camera to each physics body
- If distance > `LodDisableDistance`, remove body from simulation
- Re-activate when object comes back in range
- Static and dynamic bodies support LOD independently

**Distance Calculation:**
```csharp
float distSq = Vector3.DistanceSquared(cameraPos, position);
float disableDistSq = lodDisableDistance * lodDisableDistance;
```

## Physics Settings

```csharp
var settings = new PhysicsSystemSettings
{
    MaxBodies = 100000,           // Maximum concurrent bodies
    NumBodyMutexes = 0,           // Mutexes for body locking
    MaxBodyPairs = 50000,         // Contact pair cache
    MaxContactConstraints = 50000 // Contact constraints
};
```

## Simulation Loop

```csharp
public void Simulate(float deltaTime)
{
    if (!_isWarmedUp)
    {
        PreWarm();  // 3 warmup ticks
        _isWarmedUp = true;
    }
    
    _physics.Update(deltaTime, 1, _jobSystem);
    EngineStats.PhysicsTimeMs = elapsed;
}
```

### Pre-warming

Runs 3 simulation ticks at startup to warm up internal caches and reduce first-frame hitches.

## System Integration

### PhysicsInitSystem

Creates physics bodies for entities with physics components:

1. Iterate entities with `PhysicsBodyComponent` / `StaticPhysicsBodyComponent`
2. Skip if body already created (BodyID != Invalid)
3. Create `BoxShape` from entity's `CoordComponent.Scale`
4. Assign collision layer based on LOD distance
5. Create body with motion type (Dynamic/Static)
6. Apply initial velocities from `InitialVelocityComponent`

### PhysicsSystem (Sync)

Reads physics simulation results back to ECS:

1. Iterate all dynamic bodies
2. Read position/rotation from Jolt
3. Update `CoordComponent` (Position, Rotation)
4. Store previous position for interpolation

### JumpSystem

Applies random impulse forces for bouncing objects:

```csharp
// Apply randomized jump force within bounds
var impulse = new Vector3(
    Lerp(minForce.X, maxForce.X, random),
    Lerp(minForce.Y, maxForce.Y, random),
    Lerp(minForce.Z, maxForce.Z, random)
);
physicsSystem.BodyInterface.SetLinearVelocity(bodyId, impulse);
```

## Performance Considerations

1. **Thread Pool**: Uses all CPU cores minus one for physics jobs
2. **Body Limits**: Pre-allocate max bodies to avoid runtime allocations
3. **Broad Phase**: Auto-optimizes after static bodies are added
4. **LOD**: Deactivates distant bodies entirely
5. **Shape Caching**: Box shapes created once per entity
