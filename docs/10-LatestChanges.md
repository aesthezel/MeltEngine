# Latest Changes - Physics with Jolt Phase 1

Commits: `377cdcf` + `1a3341f`

## Commit 377cdcf - "Added Physics with Jolt phase 1"

### Physics Overhaul

- **PhysicsManager (PhysicsSystem)** replaces old `PhysicSystem`. Full Jolt lifecycle manager.
  - `FIXED_TICK_RATE = 50f`, `FIXED_DELTA_TIME = 1/50s`
  - `JobSystemThreadPool` using all CPU cores minus 1
  - `BodyInterface` for body CRUD
  - `Simulate()` with pre-warming (3 warmup ticks to avoid first-frame hitches)
  - `Update()` syncs Jolt positions/rotations to `CoordComponent`
  - `UpdateLod()` disables/reactivates distant dynamic and static bodies
  - `OptimizeBroadPhase()` called after static bodies are created
  - Full cleanup on exit (`Dispose` for PhysicsSystem, JobSystem, filters)

- **JoltConfig (JoltLayers)** - Static layer definitions:
  - `LayerNonMoving(0)`, `LayerMoving(1)`, `LayerDynamicFar(2)`, `LayerDisabled(3)`
  - `BPNonMoving(0)`, `BPMoving(1)`, `BPFar(2)`
  - Collision matrix: NonMoving<->Moving, Moving<->Moving, NonMoving<->DynamicFar, Moving<->DynamicFar. DynamicFar<->DynamicFar disabled.

- **PhysicsInitSystem** rewritten:
  - Creates `BoxShape` from `CoordComponent.Scale`
  - Camera-distance-based layer assignment (LOD far threshold: 40 units)
  - Sets damping (0.1 linear/angular), friction (0.3 dynamic, 0.5 static), restitution (0.05)
  - Applies `InitialVelocityComponent` (linear + angular)
  - LOD culling right after creation if entity is beyond disable distance

### New Components

| Component | Fields |
|-----------|--------|
| `BulletComponent` | `Lifetime`, `MaxLifetime` |
| `BulletRendererComponent` | Marker (no data) |
| `BulletSpawnerComponent` | `Cooldown`, `CurrentCooldown`, `BulletSpeed`, `BulletScale`, `BulletMass` |
| `InitialVelocityComponent` | `LinearVelocity`, `AngularVelocity` |
| `JumpComponent` | `Cooldown`, `Timer`, `MinJumpForce`, `MaxJumpForce` |
| `SurfaceBlockComponent` | Marker for terrain surface blocks |
| `BlockTypeComponent` | Stores block type enum |

### New/Updated Systems

- **BulletSpawnerSystem**: LClick to spawn bullets, camera-aligned direction, lifetime tracking, auto-destroy
- **JumpSystem**: Cooldown-based random impulse jumps using physics body interface
- **MovementSystem**: WASD + god mode (G key), camera-relative movement, Space/Shift for vertical in god mode
- **CameraSystem**: Orbit mode (`IsOrbitMode`) with mouse control, pitch/yaw clamping, scroll zoom, smooth mouse capture
- **RenderSystem**: GPU instanced rendering with custom GLSL shaders, frustum culling, color-coded physics states (green=awake, blue=sleep, red=static, yellow=bullets), debug HUD overlay
- **CubeSpawnerSystem**: LClick spawns dynamic cubes with random offset + jump components

### Workflow Restructured

- Main loop: update systems -> accumulate physics ticks -> simulate -> LOD -> sync -> lifecycle -> render
- `ThreadSafeECSOperator` replaces raw `ECSOperator` with `ReaderWriterLockSlim`
- `ProcessPendingOperations()` at start of each frame
- Default scene: procedural 32x32 world with chunks
- F5 regenerates the entire world

---

## Commit 1a3341f - "feat: implement core ECS components and systems"

### Project Migration

- **MeltEngine** SDK extracted from old `MeltEngine` project
- Old files deleted: `MeltEngine/` (old csproj, components, systems, scenes)
- New target: `MeltEngineSDK` with full class library

### Core ECS Foundation

- `ECSOperator` with `ComponentArray<T>` sparse-dense storage
- `ThreadSafeECSOperator` with `ReaderWriterLockSlim` + `ConcurrentQueue<Action>`
- `MultiThreadedSystemManager` - background/main thread system separation (reserved, not active)
- `EngineStats` - global perf counters

### Components (all under `MeltEngineSDK/Entities/Components/`)

- `CoordComponent` - Position/Scale/Rotation/PreviousPosition/ModelMatrix/IsDirty
- `CubeRendererComponent` - Flag for 3D render
- `EnabledComponent` - Active flag
- `DestroyedComponent` - Mark for cleanup
- `PhysicsBodyComponent` - Dynamic mass, LOD, bury detection
- `StaticPhysicsBodyComponent` - Static collider, LOD
- `PlayerControllableComponent` - Speed, IsGodMode flag
- `GameCameraComponent` - Camera3D, TargetEntity, Offset, IsOrbitMode, Distance, Yaw, Pitch, DrawDistance
- `JumpComponent` - Configurable jump forces
- `SceneMemberComponent` - Scene ownership tracking
- `CubeSpawnerComponent` - Spawn config (cooldown, count)
- `ComponentArray<T>` - Core dense storage with `DenseEntities[]` + `Dictionary<Entity, int>`

### Systems (all under `MeltEngineSDK/Systems/`)

- `PhysicsInitSystem` - Jolt body creation
- `PhysicsSystem` (PhysicsManager) - Jolt simulation + sync
- `MovementSystem` - Input-driven movement
- `JumpSystem` - Random impulse jumps
- `CameraSystem` - Follow + orbit camera
- `RenderSystem` - Instanced rendering with custom shaders
- `LifecycleSystem` - Destroy marked entities
- `CubeSpawnerSystem` - Spawn dynamic cubes
- `BulletSpawnerSystem` - Spawn + manage bullets
- `ISystem` interface - `Update(ECSOperator, float deltaTime)`

### Scenes & Serialization

- `SceneService` - Scene loading pipeline with multi-pass entity resolution
- `SceneSerializer` - JSON component deserialization
- `EntityDefinition` - Scene JSON entity model
- `GameCameraComponentData` - Camera serialization helper

### Utilities

- `FileWatcher` - File polling with debounce
- `HotReload` - Scene reload on file change
- `PathHelper` - Asset path resolution

### Scene JSON

- `Scenes/MainScene.json` - Default scene with player, camera, ground plane, bullet spawner

---
