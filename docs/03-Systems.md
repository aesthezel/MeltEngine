# Systems

Logic split into isolated classes implementing `ISystem`. No logic in entities/components.

## Core Systems

### 1. PhysicsSystem & PhysicsInitSystem
- Fixed update loop (1/60s).
- Apply forces / gravity.
- Sync position with `CoordComponent`.
- *Unity equiv*: Physics FixedUpdate.

### 2. MovementSystem
- Fixed update loop.
- Read input (arrow keys).
- Move entities with `PlayerControllableComponent` + `PhysicsBodyComponent`.
- *Unity equiv*: Input.GetAxis + Rigidbody.AddForce.

### 3. CameraSystem
- Fixed update loop.
- Follow `TargetEntity` based on `Offset`.
- Write to `Camera3D` struct in `GameCameraComponent`.
- *Unity equiv*: LateUpdate Camera Follow.

### 4. RenderSystem
- Variable update loop.
- Alpha lerp `PreviousPosition` → `Position` for smooth render between fixed ticks.
- Call Raylib `BeginMode3D` / `DrawCube`.
- Handle UI debug overlay.
- *Unity equiv*: Rendering Pipeline.

### 5. LifecycleSystem
- Variable update loop.
- Find `DestroyedComponent`.
- Call `ECSOperator.DestroyEntity()`.
- *Unity equiv*: Garbage Collection / Post-Frame Destroy.
