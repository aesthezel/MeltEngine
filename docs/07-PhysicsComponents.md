# Physics Components

Components for physics simulation in MeltEngine using Jolt Physics.

## PhysicsBodyComponent

Dynamic physics body affected by forces, gravity, and collisions.

```csharp
public struct PhysicsBodyComponent
{
    public BodyID BodyId;              // Jolt body handle (runtime)
    public float Mass;                  // Body mass
    public bool UseLod;                 // Enable LOD culling
    public float LodDisableDistance;   // Distance to disable physics
    
    // Runtime state (JsonIgnore)
    public bool IsLodDisabled;          // Currently culled?
    public bool IsBuried;               // Underground check (render skip)
    public byte ObjectsAboveCount;      // Bodies resting on this
    public byte BuriedFrames;           // Frames underground
    public MotionQuality CurrentMotionQuality; // Discrete or Linear
    public float MotionQualityHighDistance;    // Distance to upgrade quality
}
```

**Default Damping/Friction:**
- Linear Damping: 0.1
- Angular Damping: 0.1
- Friction: 0.3
- Restitution: 0.05

**Render Integration:**
- `IsBuried = true` → skipped by RenderSystem (invisible).
- `IsLodDisabled = true` → skipped by RenderSystem.

**Usage:**
```csharp
// Create dynamic physics body
entityOperator.AddComponent(entity, new PhysicsBodyComponent
{
    Mass = 1.0f,
    UseLod = true,
    LodDisableDistance = 100f
});
```

**Default Values:**
- Linear Damping: 0.1
- Angular Damping: 0.1
- Friction: 0.3
- Restitution (bounciness): 0.05

**JSON Serialization:**
```json
{
  "PhysicsBodyComponent": {
    "Mass": 1.0,
    "UseLod": true,
    "LodDisableDistance": 100.0
  }
}
```

## StaticPhysicsBodyComponent

Static collider that doesn't move but participates in collisions.

```csharp
public struct StaticPhysicsBodyComponent
{
    public BodyID BodyId;              // Jolt body handle (runtime)
    public bool UseLod;                // Enable LOD culling
    public float LodDisableDistance;   // Distance to disable
    
    // Runtime state (JsonIgnore)
    public bool IsLodDisabled;         // Currently culled?
}
```

**Usage:**
```csharp
// Create static collider (floor, wall, etc.)
entityOperator.AddComponent(entity, new StaticPhysicsBodyComponent
{
    UseLod = true,
    LodDisableDistance = 200f
});
```

**Default Values:**
- Friction: 0.5
- MotionType: Static (never moves)

## InitialVelocityComponent

Set initial linear and angular velocity when body is created.

```csharp
public struct InitialVelocityComponent
{
    public Vector3 LinearVelocity;    // Movement direction/speed
    public Vector3 AngularVelocity;    // Rotation speed
}
```

**Usage:**
```csharp
// Throw object with initial velocity
entityOperator.AddComponent(entity, new InitialVelocityComponent
{
    LinearVelocity = new Vector3(10f, 5f, 0f),
    AngularVelocity = new Vector3(0f, 0f, 2f)  // Spin on Z axis
});
```

**JSON Serialization:**
```json
{
  "InitialVelocityComponent": {
    "LinearVelocity": {"X": 10, "Y": 5, "Z": 0},
    "AngularVelocity": {"X": 0, "Y": 0, "Z": 2}
  }
}
```

## JumpComponent

Configurable jump behavior with random force variation.

```csharp
public struct JumpComponent
{
    public float Cooldown;             // Time between jumps
    public float Timer;                // Current cooldown timer
    public Vector3 MinJumpForce;       // Minimum impulse force
    public Vector3 MaxJumpForce;       // Maximum impulse force
}
```

**Usage:**
```csharp
// Add jumping to an entity
entityOperator.AddComponent(entity, new JumpComponent
{
    Cooldown = 0.5f,
    Timer = 0f,
    MinJumpForce = new Vector3(-1f, 8f, -1f),
    MaxJumpForce = new Vector3(1f, 12f, 1f)
});
```

**Behavior:**
- Timer counts down each frame
- When Timer <= 0, applies random force between Min and Max
- Force is interpolated: `Lerp(Min, Max, Random)`
- Timer resets to Cooldown after jump

**JSON Serialization:**
```json
{
  "JumpComponent": {
    "Cooldown": 0.5,
    "MinJumpForce": {"X": -1, "Y": 8, "Z": -1},
    "MaxJumpForce": {"X": 1, "Y": 12, "Z": 1}
  }
}
```

## Required Setup

For physics to work, entities need:

### Dynamic Body Setup
```
Entity
├── CoordComponent        (position, scale)
├── PhysicsBodyComponent  (mass, LOD config)
├── CubeRendererComponent (visual)
└── EnabledComponent      (active)
```

### With Initial Velocity
```
Entity
├── CoordComponent
├── PhysicsBodyComponent
├── InitialVelocityComponent  (velocity at spawn)
├── CubeRendererComponent
└── EnabledComponent
```

### With Jumping
```
Entity
├── CoordComponent
├── PhysicsBodyComponent
├── JumpComponent         (jump config)
├── CubeRendererComponent
└── EnabledComponent
```

### Static Collider Setup
```
Entity
├── CoordComponent              (position, scale)
├── StaticPhysicsBodyComponent  (static collider)
└── CubeRendererComponent       (optional visual)
```

## Coordinate System

Physics uses same coordinate system as CoordComponent:
- **X**: Left/Right
- **Y**: Up/Down (gravity is -Y)
- **Z**: Forward/Back

**Gravity:** Vector3(0, -9.81, 0) m/s²

**Shape:** All bodies currently use BoxShape derived from CoordComponent.Scale:
```csharp
var halfExtents = new Vector3(scale.X * 0.5f, scale.Y * 0.5f, scale.Z * 0.5f);
```

## API Overview

- BulletComponent: Lifetime, MaxLifetime
- BulletRendererComponent: Marker only (no fields)
- BulletSpawnerComponent: Cooldown, CurrentCooldown, BulletSpeed, BulletScale, BulletMass
- InitialVelocityComponent: LinearVelocity, AngularVelocity
- JumpComponent: Cooldown, Timer, MinJumpForce, MaxJumpForce
- PhysicsBodyComponent: Mass, UseLod, LodDisableDistance, IsLodDisabled, IsBuried, ObjectsAboveCount, BuriedFrames, CurrentMotionQuality, MotionQualityHighDistance
- StaticPhysicsBodyComponent: UseLod, LodDisableDistance, IsLodDisabled

Each component is designed to be simple data holders; their behavior is implemented by corresponding systems (e.g., BulletSpawnerSystem, PhysicsManager, JumpSystem).
