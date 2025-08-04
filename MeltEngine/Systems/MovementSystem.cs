using System.Numerics;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems;

public class MovementSystem : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var controllableArray = entityOperator.GetComponentArray<PlayerControllableComponent>();
        var coordArray = entityOperator.GetComponentArray<CoordComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        var enabledArray = entityOperator.GetComponentArray<EnabledComponent>();

        foreach (var (entity, controllable) in controllableArray.Components)
        {
            if (!enabledArray.Components.ContainsKey(entity) || 
                !physicsArray.Components.TryGetValue(entity, out var physicsBody))
            {
                continue;
            }

            Vector3 force = Vector3.Zero;

            if (Raylib.IsKeyDown(KeyboardKey.W)) force.Z += controllable.Speed;
            if (Raylib.IsKeyDown(KeyboardKey.S)) force.Z -= controllable.Speed;
            if (Raylib.IsKeyDown(KeyboardKey.A)) force.X += controllable.Speed;
            if (Raylib.IsKeyDown(KeyboardKey.D)) force.X -= controllable.Speed;
            if (Raylib.IsKeyDown(KeyboardKey.Space)) force.Y += controllable.Speed * 2;

            if (force != Vector3.Zero)
            {
                unsafe
                {
                    var pxForce = new PxVec3 { x = force.X, y = force.Y, z = force.Z };
                    NativeMethods.PxRigidDynamic_setLinearVelocity_mut(physicsBody.Actor, &pxForce, true);
                }
            }
        }
    }
}