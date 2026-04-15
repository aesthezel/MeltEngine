using System;
using System.Collections.Generic;
using System.Numerics;
using JoltPhysicsSharp;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems;

public class PhysicsInitSystem(PhysicsManager physicsSystem) : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var coords = entityOperator.GetComponentArray<CoordComponent>();
        var dynamicBodies = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        
        var bodyInterface = physicsSystem.BodyInterface;
        List<(Entity, PhysicsBodyComponent)> dynamicUpdates = null;

        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();
        Vector3 cameraPos = new Vector3(0, 2, 0);
        bool hasCamera = false;

        foreach (var cam in cameraArray.Components.Values)
        {
            cameraPos = cam.Camera.Position;
            hasCamera = true;
            break;
        }

        var velocityArray = entityOperator.GetComponentArray<InitialVelocityComponent>();

        for (int i = 0; i < dynamicBodies.Count; i++)
        {
            var entity = dynamicBodies.DenseEntities[i];
            var body = dynamicBodies.Components[entity];

            if (!body.BodyId.IsInvalid) continue;
            if (!coords.Components.TryGetValue(entity, out var coord)) continue;

            var halfExtents = new Vector3(coord.Scale.X * 0.5f, coord.Scale.Y * 0.5f, coord.Scale.Z * 0.5f);
            var boxShape = new BoxShape(halfExtents, 0.0f);

            var rotation = coord.Rotation.LengthSquared() < 0.001f ? Quaternion.Identity : Quaternion.Normalize(coord.Rotation);

            float distSq;
            if (hasCamera)
            {
                distSq = Vector3.DistanceSquared(cameraPos, coord.Position);
            }
            else
            {
                distSq = coord.Position.X * coord.Position.X + coord.Position.Z * coord.Position.Z;
            }

            ObjectLayer layer;
            bool isFar = body.UseLod && distSq > 1600f; // 40 units

            if (isFar)
            {
                layer = JoltConfig.LayerDynamicFar;
            }
            else
            {
                layer = JoltConfig.LayerMoving;
            }

            var settings = new BodyCreationSettings(
                boxShape,
                coord.Position,
                rotation,
                MotionType.Dynamic,
                layer);

            settings.LinearDamping = 0.1f;
            settings.AngularDamping = 0.1f;
            settings.Friction = 0.3f;
            settings.Restitution = 0.05f;

            var bodyId = bodyInterface.CreateAndAddBody(settings, isFar ? Activation.DontActivate : Activation.Activate);

            if (bodyId.IsInvalid) continue;

            dynamicUpdates ??= new List<(Entity, PhysicsBodyComponent)>();
            dynamicUpdates.Add((entity, body with { BodyId = bodyId }));
        }

        if (dynamicUpdates != null)
        {
            for (int i = 0; i < dynamicUpdates.Count; i++)
            {
                var update = dynamicUpdates[i];
                var entity = update.Item1;
                var body = update.Item2;

                if (hasCamera && body.UseLod && coords.Components.TryGetValue(entity, out var coord))
                {
                    float distSq = Vector3.DistanceSquared(cameraPos, coord.Position);
                    float disableDistSq = body.LodDisableDistance * body.LodDisableDistance;
                    if (distSq > disableDistSq)
                    {
                        body.IsLodDisabled = true;
                        bodyInterface.RemoveBody(body.BodyId);
                    }
                }

                if (velocityArray.Components.TryGetValue(entity, out var velocity) && !body.IsLodDisabled)
                {
                    if (velocity.LinearVelocity != Vector3.Zero)
                    {
                        bodyInterface.SetLinearVelocity(body.BodyId, velocity.LinearVelocity);
                    }
                    if (velocity.AngularVelocity != Vector3.Zero)
                    {
                        bodyInterface.SetAngularVelocity(body.BodyId, velocity.AngularVelocity);
                    }
                }

                dynamicBodies.Components[entity] = body;
            }
        }

        var staticBodies = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        List<(Entity, StaticPhysicsBodyComponent)> staticUpdates = null;

        for (int i = 0; i < staticBodies.Count; i++)
        {
            var entity = staticBodies.DenseEntities[i];
            var body = staticBodies.Components[entity];

            if (!body.BodyId.IsInvalid) continue;
            if (!coords.Components.TryGetValue(entity, out var coord)) continue;

            var halfExtents = new Vector3(coord.Scale.X * 0.5f, coord.Scale.Y * 0.5f, coord.Scale.Z * 0.5f);
            var boxShape = new BoxShape(halfExtents, 0.0f);

            var rotation = coord.Rotation.LengthSquared() < 0.001f ? Quaternion.Identity : Quaternion.Normalize(coord.Rotation);

            var settings = new BodyCreationSettings(
                boxShape,
                coord.Position,
                rotation,
                MotionType.Static,
                JoltConfig.LayerNonMoving);

            settings.Friction = 0.5f;

            var bodyId = bodyInterface.CreateAndAddBody(settings, Activation.DontActivate);

            if (bodyId.IsInvalid) continue;

            staticUpdates ??= new List<(Entity, StaticPhysicsBodyComponent)>();
            staticUpdates.Add((entity, body with { BodyId = bodyId }));
        }

        if (staticUpdates != null)
        {
            for (int i = 0; i < staticUpdates.Count; i++)
            {
                var update = staticUpdates[i];
                var entity = update.Item1;
                var body = update.Item2;

                if (hasCamera && body.UseLod && coords.Components.TryGetValue(entity, out var coord))
                {
                    float distSq = Vector3.DistanceSquared(cameraPos, coord.Position);
                    float disableDistSq = body.LodDisableDistance * body.LodDisableDistance;
                    if (distSq > disableDistSq)
                    {
                        body.IsLodDisabled = true;
                        bodyInterface.RemoveBody(body.BodyId);
                    }
                }

                staticBodies.Components[entity] = body;
            }

            physicsSystem.OptimizeBroadPhase();
        }
    }
}
