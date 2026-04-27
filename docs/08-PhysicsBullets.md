# Physics Bullets (Jolt)

This document describes the bullet-related components and how they interact with the Jolt Physics system in MeltEngine.

## Bullet lifecycle
- BulletSpawnerComponent periodically spawns bullets with a given speed, scale and mass.
- BulletComponent tracks the lifetime of a bullet and allows automatic removal when the lifetime elapses.
- BulletRendererComponent marks an entity to be drawn as a bullet (no data carried).

## Components
- BulletComponent
  - Lifetime: current life time in seconds.
  - MaxLifetime: maximum life before the bullet is removed.
  - Constructor: BulletComponent(maxLifetime = 5f).

- BulletRendererComponent
  - Marker only; used by RenderSystem to draw bullets.

- BulletSpawnerComponent
  - Cooldown: seconds between spawns.
  - CurrentCooldown: internal timer counting down to next spawn.
  - BulletSpeed: initial speed of spawned bullets.
  - BulletScale: local scale of the spawned bullet.
  - BulletMass: mass of the spawned bullet.

## Quick usage example
```csharp
// Create a spawner on an entity
var spawner = new BulletSpawnerComponent
{
  Cooldown = 0.2f,
  BulletSpeed = 40f,
  BulletScale = new Vector3(0.25f,0.25f,0.5f),
  BulletMass = 0.5f
};
```

```csharp
// Bullet component on a bullet entity
var bullet = new BulletComponent
{
  MaxLifetime = 5f
};
```

## Notes
- Bullet rendering is decoupled from physics to allow flexible visuals.
- The system assumes a bullet is a lightweight dynamic body with a defined velocity.
