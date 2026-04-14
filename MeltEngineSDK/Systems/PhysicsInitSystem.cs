using System;
using System.Collections.Generic;
using System.Numerics;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Systems;

public class PhysicsInitSystem(PhysicsSystem physicsSystem) : ISystem
{
    public unsafe void Update(ECSOperator entityOperator, float deltaTime)
    {
        var coords = entityOperator.GetComponentArray<CoordComponent>();

        var dynamicBodies = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        List<(Entity, PhysicsBodyComponent)> dynamicUpdates = null;

        for (int i = 0; i < dynamicBodies.Count; i++)
        {
            var entity = dynamicBodies.DenseEntities[i];
            var body = dynamicBodies.Components[entity];

            if (body.Actor != null) continue;

            if (!coords.Components.TryGetValue(entity, out var coord))
            {
                Console.WriteLine(
                    $"ADVERTENCIA: Entidad {entity.Id} tiene PhysicsBodyComponent pero no CoordComponent. Se omitirá.");
                continue;
            }

            var boxGeo = PxBoxGeometry_new(coord.Scale.X / 2f, coord.Scale.Y / 2f, coord.Scale.Z / 2f);
            var pxPos = new PxVec3 { x = coord.Position.X, y = coord.Position.Y, z = coord.Position.Z };
            var transform = PxTransform_new_1(&pxPos);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

            var actor = physicsSystem.Physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&boxGeo,
                physicsSystem.Material, body.Mass, &identity);

            if (actor == null)
            {
                Console.WriteLine($"ERROR FATAL: PhysPxCreateDynamic devolvió null para la entidad {entity.Id}.");
                continue;
            }

            // --- OPTIMIZACION: Umbral de Sueño Agresivo ---
            // Le dice a PhysX que si el cubo se mueve muy lento (ej. < 0.5 de energia), lo duerma inmediatamente.
            NativeMethods.PxRigidDynamic_setSleepThreshold_mut((PxRigidDynamic*)actor, 0.5f);
            NativeMethods.PxRigidDynamic_setStabilizationThreshold_mut((PxRigidDynamic*)actor, 0.2f);
            
            // --- OPTIMIZACION DE CPU MÁXIMA MASA DE COMPONENTES ---
            // PhysX por defecto usa 4 iteraciones de posicion y 1 de velocidad. 
            // Para un test de estrés de 50mil cubos cayendo a la vez, bajarlo a 2 reduce el costo de la isla a la mitad (40ms -> 20ms)
            NativeMethods.PxRigidDynamic_setSolverIterationCounts_mut((PxRigidDynamic*)actor, 2, 1);

            dynamicUpdates ??= new List<(Entity, PhysicsBodyComponent)>();
            dynamicUpdates.Add((entity, body with { Actor = actor }));
        }

        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();
        Vector3 cameraPos = Vector3.Zero;
        bool hasCamera = false;
        foreach (var cam in cameraArray.Components.Values)
        {
            cameraPos = cam.Camera.Position;
            hasCamera = true;
            break;
        }

        if (dynamicUpdates != null)
        {
            foreach (var update in dynamicUpdates)
            {
                var entity = update.Item1;
                var body = update.Item2;

                if (hasCamera && body.UseLod && coords.Components.TryGetValue(entity, out var coord))
                {
                    float distSq = Vector3.DistanceSquared(cameraPos, coord.Position);
                    float disableDistSq = body.LodDisableDistance * body.LodDisableDistance;
                    if (distSq > disableDistSq)
                    {
                        body.IsLodDisabled = true;
                    }
                }

                dynamicBodies.Components[entity] = body;
                if (!body.IsLodDisabled)
                {
                    physicsSystem.AddActor((PxActor*)body.Actor);
                }
            }
        }

        var staticBodies = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        List<(Entity, StaticPhysicsBodyComponent)> staticUpdates = null;

        foreach (var kvp in staticBodies.Components)
        {
            var entity = kvp.Key;
            var body = kvp.Value;

            if (body.Actor != null) continue;

            if (!coords.Components.TryGetValue(entity, out var coord))
            {
                Console.WriteLine(
                    $"ADVERTENCIA: Entidad {entity.Id} tiene StaticPhysicsBodyComponent pero no CoordComponent. Se omitirá.");
                continue;
            }

            var boxGeo = PxBoxGeometry_new(coord.Scale.X / 2f, coord.Scale.Y / 2f, coord.Scale.Z / 2f);
            var pxPos = new PxVec3 { x = coord.Position.X, y = coord.Position.Y, z = coord.Position.Z };
            var transform = PxTransform_new_1(&pxPos);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

            var actor = physicsSystem.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&boxGeo,
                physicsSystem.Material, &identity);

            if (actor == null)
            {
                Console.WriteLine($"ERROR FATAL: PhysPxCreateStatic devolvió null para la entidad {entity.Id}.");
                continue;
            }

            staticUpdates ??= new List<(Entity, StaticPhysicsBodyComponent)>();
            staticUpdates.Add((entity, body with { Actor = actor }));
        }

        if (staticUpdates != null)
        {
            foreach (var update in staticUpdates)
            {
                var entity = update.Item1;
                var body = update.Item2;

                if (hasCamera && body.UseLod && coords.Components.TryGetValue(entity, out var coord))
                {
                    float distSq = Vector3.DistanceSquared(cameraPos, coord.Position);
                    float disableDistSq = body.LodDisableDistance * body.LodDisableDistance;
                    if (distSq > disableDistSq)
                    {
                        body.IsLodDisabled = true;
                    }
                }

                staticBodies.Components[entity] = body;
                if (!body.IsLodDisabled)
                {
                    physicsSystem.AddActor((PxActor*)body.Actor);
                }
            }
        }
    }
}