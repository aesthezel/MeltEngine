# Systems

Logic split into isolated classes implementing `ISystem`. No logic in entities/components.

## Core Systems

### 1. PhysicsInitSystem
- Fixed update loop (1/50s).
- Create Jolt physics bodies for entities with physics components.
- Configure body properties (damping, friction, restitution).
- Assign collision layers based on distance to camera.
- *Unity equiv*: Rigidbody initialization in Awake.

### 2. PhysicsManager (PhysicsSystem)
- Fixed update loop (1/50s via Jolt).
- Step physics simulation with `PhysicsManager.Simulate()`.
- Sync Jolt body positions/rotations to `CoordComponent`.
- LOD management: activate/deactivate distant bodies.
- *Unity equiv*: Physics.FixedUpdate.

### 3. MovementSystem
- Fixed update loop.
- Read input (arrow keys).
- Move entities with `PlayerControllableComponent` + `PhysicsBodyComponent`.
- *Unity equiv*: Input.GetAxis + Rigidbody.AddForce.

### 4. JumpSystem
- Fixed update loop.
- Apply random impulse forces to entities with `JumpComponent`.
- Cooldown-based jump timing.
- Random force within min/max bounds per axis.
- *Unity equiv*: Rigidbody.AddExplosionForce / AddImpulse.

### 5. CameraSystem
- Fixed update loop.
- Follow `TargetEntity` based on `Offset`.
- Write to `Camera3D` struct in `GameCameraComponent`.
- *Unity equiv*: LateUpdate Camera Follow.

### 6. RenderSystem
- Variable update loop.
- Alpha lerp `PreviousPosition` → `Position` for smooth render between fixed ticks.
- Call Raylib `BeginMode3D` / `DrawCube`.
- Handle UI debug overlay.
- *Unity equiv*: Rendering Pipeline.

### 7. LifecycleSystem
- Variable update loop.
- Find `DestroyedComponent`.
- Call `ECSOperator.DestroyEntity()`.
- *Unity equiv*: Garbage Collection / Post-Frame Destroy.

### 8. Bullet System (Spawner & Lifetimes)
- Integrates with BulletSpawnerComponent, BulletComponent, InitialVelocityComponent and BulletRendererComponent.
- BulletSpawnerSystem handles spawning bullets when cooldown expires and input (e.g., mouse click).
- Spawned bullets get: CoordComponent, BulletRendererComponent, EnabledComponent, PhysicsBodyComponent, BulletComponent, InitialVelocityComponent, SceneMemberComponent.
- Bullet lifetimes are managed by BulletSpawnerSystem: increments Lifetime and destroys bullets when Lifetime >= MaxLifetime via DestroyedComponent.
- This system cooperates with Physics to apply initial velocities and with Render to visualize bullets.

## Physics Integration

See [06-JoltPhysics.md](./06-JoltPhysics.md) for detailed Jolt Physics documentation.

See [07-PhysicsComponents.md](./07-PhysicsComponents.md) for physics component reference.

## Additional Systems (Latest Additions)

### 9. CubeSpawnerSystem
- Variable update loop.
- Spawns dynamic cubes on LClick with random position offset.
- Each cube gets `JumpComponent` for bouncing behavior.
- Entities assigned to scene group `"DynamicCubes"`.
- *Unity equiv*: Instantiate + AddComponent in Update.

### 10. WorldGeneratorSystem
- Procedural voxel world generation with chunk system.
- Async chunk generation via `Task.Run()` + `ConcurrentQueue`.
- Only creates visible surface blocks (culls buried blocks).
- Enforces per-frame limits: 2000 blocks, 4 chunks, 100 entity destructions.
- See [11-WorldAndChunks.md](./11-WorldAndChunks.md).

### 11. ChunkCullerSystem
- Frustum-based chunk visibility culling.
- Extracts 6 view frustum planes from Camera3D.
- Tests AABB vs frustum for each chunk (16 chunk radius).
- Camera movement threshold (2 units) avoids redundant recalculation.
- Feeds visibility data to render system for chunk-level culling.
- See [11-WorldAndChunks.md](./11-WorldAndChunks.md).

### 12. RenderSystem (Updated)
- GPU instanced rendering with custom GLSL vertex/fragment shaders.
- Color-coded physics states: **Green** = awake dynamic, **Blue** = sleeping dynamic, **Red** = static renders, **Yellow** = bullets.
- Frustum culling + distance culling (configurable `DrawDistance` from camera, default 120).
- Back-face dot product culling for objects beyond 25 units.
- Dirty-flag caching: `ModelMatrix` recalculated only when `IsDirty` is true.
- Debug HUD overlay showing memory, frame time, physics time, render culling time, entity counts.

### 13. CameraSystem (Updated)
- Two modes: **Fixed Offset** (original) and **Orbit Mode** (mouse-driven).
- Orbit mode: mouse Yaw/Pitch with sensitivity 0.05, clamped Pitch (-89 to 89).
- Scroll wheel zoom (distance ≥ 2.0).
- Smooth mouse capture: first delta frame skipped to avoid jump.
- Max delta per frame clamped to 50 to avoid erratic movement.
- Automatic cursor hide/show when activating/deactivating orbit mode.

### 14. MovementSystem (Updated)
- **God Mode** toggle (G key): 5x speed, camera-relative WASD, Space=up, Shift=down.
- Camera-relative movement in normal mode (projects camera forward onto XZ plane).
- Sets zero velocity when idle in god mode (stop on key release).
- Fallback to axis-aligned movement when no active camera.

### 15. PhysicsManager (Updated)
- Complete rewrite: Jolt lifecycle manager with BodyInterface, JobSystemThreadPool.
- Pre-warming: 3 simulation ticks at startup.
- OptimizeBroadPhase after static body batch creation.
- Full LOD system for dynamic and static bodies.
- See [06-JoltPhysics.md](./06-JoltPhysics.md).
