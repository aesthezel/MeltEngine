# MeltEngine Architecture

MeltEngine use Pure ECS (Entity-Component-System) structure. Similar Unity DOTS.

## Core Parts

1. **ECSOperator**
   - Manage entity ID.
   - Store components in `ComponentArray<T>`. Data-Oriented Design (DOD).
   - Fast iteration. Contiguous memory per component type.
   - `ThreadSafeECSOperator` handle pending operations for thread safety.

2. **Workflow**
   - Main loop.
   - Init Raylib (1920x1080 @ 60 FPS target).
   - Fixed Timestep for Physics (`fixedDeltaTime = 1/60s`).
   - Variable Timestep for Render/Lifecycle.
   - Handle scene load/reload (F5).

3. **Window**
   - Wrap Raylib initialization.

## Loop Flow

1. Process ECS pending ops.
2. Accumulate delta time.
3. While accumulator >= 1/60s:
   a. Update logical Systems (`Movement`, `Camera`).
   b. Update `PhysicsSystem`.
   c. Sub accumulator.
4. Update `LifecycleSystem`.
5. Update `RenderSystem`. 
6. Repeat.
