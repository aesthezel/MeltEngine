# Engine Vision: Unity Style (2D/3D)

MeltEngine map to Unity paradigms but strictly ECS.

## Unity vs MeltEngine Mapping

- **GameObject** → `Entity` (just an ID).
- **MonoBehaviour** → Split into `Component` (data) + `ISystem` (logic).
- **Update()** → Variable tick Systems (`LifecycleSystem`, `RenderSystem`).
- **FixedUpdate()** → Fixed tick Systems (`PhysicsSystem`, `MovementSystem`).
- **SceneManager** → `SceneService`.
- **Prefabs** → Entity JSON definition nodes.
- **Transform** → `CoordComponent`.

## 2D and 3D Support

Engine ready for 2D/3D dual support.

**To expand Unity-like 2D:**
1. Add `Camera2D` fields to `GameCameraComponent`.
2. Add `SpriteRendererComponent`/`RectRendererComponent`.
3. Add `PhysicsBody2DComponent`.
4. Update `RenderSystem`: branch Raylib `BeginMode2D` vs `BeginMode3D` depending on active camera type. Read 2D components.
5. Raylib natively supports both smoothly. ECS architecture scales identically for 2D.
