# MeltEngine Architecture

MeltEngine use Pure ECS (Entity-Component-System) structure. Similar Unity DOTS.

## Core Parts

1. **ECSOperator**
   - Manage entity ID.
   - Store components in `ComponentArray<T>`. Data-Oriented Design (DOD).
   - Fast iteration. Contiguous memory per component type.
   - `ThreadSafeECSOperator` extends with `ReaderWriterLockSlim` + pending operation queue.

2. **Workflow**
   - Main async loop (`RunAsync()`).
   - Init Raylib (1920x1080 @ 120 FPS target).
   - Fixed Timestep for Physics via Jolt (`FIXED_DELTA_TIME = 1/50s`).
   - Variable Timestep for update systems and render.
   - F5 regenerates world scene.
   - Default scene: procedural 32x32 world with chunks.

3. **Window**
   - Wrap Raylib initialization.

4. **PhysicsManager** (PhysicsSystem)
   - Jolt Physics Sharp wrapper. Singletons: `BodyInterface`, `JobSystemThreadPool`.
   - Physics simulation at 50Hz fixed rate (max 3 steps/frame, anti-spiral guard).
   - Thread pool job system (all cores minus 1).
   - LOD support for distant body culling.
   - Pre-warming (3 ticks) to avoid first-frame hitches.

5. **EngineStats**
   - Static class tracking `PhysicsTimeMs` and `RenderCullingTimeMs`.

6. **MultiThreadedSystemManager** (reserved)
   - Background/main thread system separation. Not yet active in main loop.

7. **ThreadSafeECSOperator**
   - Extends `ECSOperator` with `ReaderWriterLockSlim` locks.
   - `ProcessPendingOperations()` flushes queued component mutations.

## Loop Flow (Current - Physics Jolt Phase 1)

1. `ProcessPendingOperations()` - flush ECS pending queue.
2. Run all `updateSystems` (PhysicsInit, BulletSpawner, Jump, Movement, Camera, ChunkCuller, WorldGenerator).
3. **Fixed Timestep Physics**: Accumulate delta -> while >= 1/50s (max 3 steps):
   a. `PhysicsManager.Simulate()` - Jolt step.
   b. Sub accumulator.
   c. Anti-spiral guard: reset if accumulator > 4/50s.
4. `PhysicsManager.UpdateLod()` - LOD culling.
5. `PhysicsManager.Update()` - sync Jolt positions to CoordComponent.
6. `LifecycleSystem.Update()` - destroy marked entities.
7. `RenderSystem.Update()` - instanced rendering with alpha interpolation.

### System Order
- `PhysicsInitSystem` - create Jolt bodies for physics entities.
- `BulletSpawnerSystem` - spawn bullets on LClick, manage lifetimes.
- `JumpSystem` - random impulse jumps on cooldown.
- `MovementSystem` - WASD + god mode input.
- `CameraSystem` - follow/orbit camera.
- `ChunkCullerSystem` - frustum culling for chunks.
- `WorldGeneratorSystem` - procedural terrain generation.
- `LifecycleSystem` - entity destruction.
- `RenderSystem` - GPU instanced drawing.
