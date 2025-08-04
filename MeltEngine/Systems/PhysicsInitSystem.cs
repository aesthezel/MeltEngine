using System;
using System.Linq;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Systems;

public class PhysicsInitSystem(PhysicsSystem physicsSystem) : ISystem
{
    private bool _initialized = false;

    public unsafe void Update(ECSOperator entityOperator, float deltaTime)
    {
        if (_initialized) return;

        Console.WriteLine("Iniciando PhysicsInitSystem...");
        
        var coords = entityOperator.GetComponentArray<CoordComponent>();

        var dynamicBodies = entityOperator.GetComponentArray<PhysicsBodyComponent>();

        foreach (var (entity, body) in dynamicBodies.Components.ToList())
        {

            if (!coords.Components.TryGetValue(entity, out var coord))
            {
                Console.WriteLine($"ADVERTENCIA: Entidad {entity.Id} tiene PhysicsBodyComponent pero no CoordComponent. Se omitirá.");
                continue;
            }

            Console.WriteLine($"Creando cuerpo dinámico para la entidad {entity.Id}...");
            var boxGeo = PxBoxGeometry_new(coord.Scale.X, coord.Scale.Y, coord.Scale.Z);
            var pxPos = new PxVec3 { x = coord.Position.X, y = coord.Position.Y, z = coord.Position.Z };
            var transform = PxTransform_new_1(&pxPos);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            
            var actor = physicsSystem.Physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&boxGeo, physicsSystem.Material, body.Mass, &identity);
            
            if (actor == null)
            {
                Console.WriteLine($"ERROR FATAL: PhysPxCreateDynamic devolvió null para la entidad {entity.Id}.");
                continue;
            }

            dynamicBodies.Components[entity] = body with { Actor = actor };
            physicsSystem.AddActor((PxActor*)actor);
            Console.WriteLine($"Cuerpo dinámico para la entidad {entity.Id} creado y añadido a la escena.");
        }

        var staticBodies = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        foreach (var (entity, body) in staticBodies.Components.ToList())
        {
            if (!coords.Components.TryGetValue(entity, out var coord))
            {
                Console.WriteLine($"ADVERTENCIA: Entidad {entity.Id} tiene StaticPhysicsBodyComponent pero no CoordComponent. Se omitirá.");
                continue;
            }

            Console.WriteLine($"Creando cuerpo estático para la entidad {entity.Id}...");
            var boxGeo = PxBoxGeometry_new(coord.Scale.X, coord.Scale.Y, coord.Scale.Z);
            var pxPos = new PxVec3 { x = coord.Position.X, y = coord.Position.Y, z = coord.Position.Z };
            var transform = PxTransform_new_1(&pxPos);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);

            var actor = physicsSystem.Physics->PhysPxCreateStatic(&transform, (PxGeometry*)&boxGeo, physicsSystem.Material, &identity);

            if (actor == null)
            {
                Console.WriteLine($"ERROR FATAL: PhysPxCreateStatic devolvió null para la entidad {entity.Id}.");
                continue;
            }

            staticBodies.Components[entity] = body with { Actor = actor };
            physicsSystem.AddActor((PxActor*)actor);
            Console.WriteLine($"Cuerpo estático para la entidad {entity.Id} creado y añadido a la escena.");
        }
        
        _initialized = true;
        Console.WriteLine("PhysicsInitSystem completado.");
    }
}