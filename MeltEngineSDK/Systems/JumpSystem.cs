using System;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems;

public class JumpSystem(PhysicsManager physicsSystem) : ISystem
{
    private readonly Random _random = new Random();

    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var jumps = entityOperator.GetComponentArray<JumpComponent>();
        var physics = entityOperator.GetComponentArray<PhysicsBodyComponent>();

        for (int i = 0; i < jumps.Count; i++)
        {
            var entity = jumps.DenseEntities[i];
            var jump = jumps.Components[entity];

            if (!physics.Components.TryGetValue(entity, out var body) || body.BodyId.IsInvalid)
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

    private void ApplyJump(PhysicsBodyComponent body, JumpComponent jump)
    {
        float forceX = Lerp(jump.MinJumpForce.X, jump.MaxJumpForce.X, (float)_random.NextDouble());
        float forceY = Lerp(jump.MinJumpForce.Y, jump.MaxJumpForce.Y, (float)_random.NextDouble());
        float forceZ = Lerp(jump.MinJumpForce.Z, jump.MaxJumpForce.Z, (float)_random.NextDouble());

        var impulse = new Vector3(forceX, forceY, forceZ);
        physicsSystem.BodyInterface.SetLinearVelocity(body.BodyId, impulse);
    }

    private float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}
