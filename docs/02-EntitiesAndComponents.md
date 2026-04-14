# Entities & Components

Pure ECS. Entity = just ID (uint). Component = pure data (struct).

## Entity Lifecycle

- `ECSOperator.CreateEntity()` → return `Entity` struct. Reuse old ID if destroyed.
- `ECSOperator.DestroyEntity(Entity)` → clear components, recycle ID.
- Queued in `ThreadSafeECSOperator` for safe destruction.

## Built-in Components

| Component | Purpose | Unity Equivalent |
|-----------|---------|-------------------|
| `CoordComponent` | Pos / Scale / PrevPos (for lerp) | Transform |
| `CubeRendererComponent` | Flag for 3D render | MeshRenderer |
| `EnabledComponent` | Active flag | GameObject.activeSelf |
| `DestroyedComponent` | Mark for cleanup | Object.Destroy |
| `PhysicsBodyComponent` | Dynamic mass body | Rigidbody |
| `StaticPhysicsBodyComponent` | Static collider | MeshCollider / BoxCollider |
| `PlayerControllableComponent` | Input speed config | PlayerController Script |
| `GameCameraComponent` | Cam offset / target ref | Camera + Follow Script |
| `SceneMemberComponent` | Track scene ownership | SceneManager bounds |
