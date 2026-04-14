using System;
using System.Numerics;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems;

public class JumpSystem : ISystem
{
    private readonly Random _random = new Random();

    public unsafe void Update(ECSOperator entityOperator, float deltaTime)
    {
        var jumps = entityOperator.GetComponentArray<JumpComponent>();
        var physics = entityOperator.GetComponentArray<PhysicsBodyComponent>();

        for (int i = 0; i < jumps.Count; i++)
        {
            var entity = jumps.DenseEntities[i];
            var jump = jumps.Components[entity];

            if (!physics.Components.TryGetValue(entity, out var body) || body.Actor == null)
                continue;

            if (body.IsLodDisabled) continue;

            var mutableJump = jump;
            mutableJump.Timer -= deltaTime;

            if (mutableJump.Timer <= 0)
            {
                ApplyJump(body, mutableJump);
                mutableJump.Timer = mutableJump.Cooldown;
            }

            jumps.Components[entity] = mutableJump;
        }
    }

    private unsafe void ApplyJump(PhysicsBodyComponent body, JumpComponent jump)
    {
        float forceX = Lerp(jump.MinJumpForce.X, jump.MaxJumpForce.X, (float)_random.NextDouble());
        float forceY = Lerp(jump.MinJumpForce.Y, jump.MaxJumpForce.Y, (float)_random.NextDouble());
        float forceZ = Lerp(jump.MinJumpForce.Z, jump.MaxJumpForce.Z, (float)_random.NextDouble());

        var impulse = new PxVec3 { x = forceX, y = forceY, z = forceZ };

        // Obtenemos la velocidad actual para sumar el salto vertical sin perder memento horizontal si se desea, 
        // pero aquí aplicamos directamente un setLinearVelocity como impulso para simplificar el comportamiento de "salto" ECS.
        NativeMethods.PxRigidDynamic_setLinearVelocity_mut(body.Actor, &impulse, true);
    }

    private float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}
