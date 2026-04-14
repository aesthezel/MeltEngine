using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Systems;

public unsafe class PhysicsSystem
{
    private readonly PxScene* _scene;
    private readonly PxPhysics* _physics;
    private readonly PxMaterial* _material;
    private int _frameCount = 0;

    public PxPhysics* Physics => _physics;
    public PxMaterial* Material => _material;

    public PhysicsSystem()
    {
        var foundation = physx_create_foundation();
        _physics = physx_create_physics(foundation);

        var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(_physics));
        sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f, z = 0.0f };

        uint numThreads = (uint)System.Environment.ProcessorCount;
        var dispatcher =
            phys_PxDefaultCpuDispatcherCreate(numThreads, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
        sceneDesc.cpuDispatcher = (PxCpuDispatcher*)dispatcher;
        sceneDesc.filterShader = get_default_simulation_filter_shader();

        _scene = _physics->CreateSceneMut(&sceneDesc);

        _material = _physics->CreateMaterialMut(0.8f, 0.6f, 0.1f);
    }

    private readonly List<(Entity, PhysicsBodyComponent)> _dynamicLodUpdates = new();
    private readonly List<(Entity, StaticPhysicsBodyComponent)> _staticLodUpdates = new();

    public void AddActor(PxActor* actor)
    {
        _scene->AddActorMut(actor, null);
    }

    public void RemoveActor(PxActor* actor)
    {
        _scene->RemoveActorMut(actor, false);
    }

    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var sw = Stopwatch.StartNew();

        _scene->SimulateMut(deltaTime, null, null, 0, true);
        uint error = 0;
        _scene->FetchResultsMut(true, &error);
        _frameCount++;

        var transformArray = entityOperator.GetComponentArray<CoordComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        var staticPhysicsArray = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();

        Vector3 cameraPos = Vector3.Zero;
        Vector3 cameraDir = Vector3.UnitZ;
        bool hasCamera = false;

        foreach (var cam in cameraArray.Components.Values)
        {
            cameraPos = cam.Camera.Position;
            cameraDir = Vector3.Normalize(cam.Camera.Target - cam.Camera.Position);
            hasCamera = true;
            break;
        }

        _dynamicLodUpdates.Clear();
        for (int i = 0; i < physicsArray.Count; i++)
        {
            var entity = physicsArray.DenseEntities[i];
            var physicsBody = physicsArray.Components[entity];

            if (physicsBody.Actor == null) continue;
            if (!transformArray.Components.TryGetValue(entity, out var currentTransform)) continue;

            // Instantáneo LOD checks per frame (en C# es super rápido ya que está en una Dense Array en memoria contigua)
            if (hasCamera && physicsBody.UseLod)
            {
                float distSq = Vector3.DistanceSquared(cameraPos, currentTransform.Position);
                float disableDistSq = physicsBody.LodDisableDistance * physicsBody.LodDisableDistance;

                bool shouldDisable = distSq > disableDistSq;

                if (shouldDisable && !physicsBody.IsLodDisabled)
                {
                    physicsBody.IsLodDisabled = true;
                    _scene->RemoveActorMut((PxActor*)physicsBody.Actor, false);
                    _dynamicLodUpdates.Add((entity, physicsBody));
                }
                else if (!shouldDisable && physicsBody.IsLodDisabled)
                {
                    physicsBody.IsLodDisabled = false;
                    _scene->AddActorMut((PxActor*)physicsBody.Actor, null);
                    _dynamicLodUpdates.Add((entity, physicsBody));
                }
                else if (!physicsBody.IsLodDisabled)
                {
                    // Si el objeto está activo y DENTRO del LOD distance, aplicamos Culling de Fisicas de Camara Instantaneo.
                    // Solo suspendemos cosas no-proximas (distSq > 100)
                    if (distSq > 100.0f) 
                    {
                        Vector3 dirToEntity = Vector3.Normalize(currentTransform.Position - cameraPos);
                        float dot = Vector3.Dot(cameraDir, dirToEntity);
                        
                        if (dot < -0.2f) // Out of Screen FOV backward
                        {
                            if (!physicsBody.WasForcedSleepByCamera)
                            {
                                NativeMethods.PxRigidDynamic_putToSleep_mut((PxRigidDynamic*)physicsBody.Actor);
                                physicsBody.WasForcedSleepByCamera = true;
                                _dynamicLodUpdates.Add((entity, physicsBody));
                            }
                        }
                        else
                        {
                            if (physicsBody.WasForcedSleepByCamera)
                            {
                                NativeMethods.PxRigidDynamic_wakeUp_mut((PxRigidDynamic*)physicsBody.Actor);
                                physicsBody.WasForcedSleepByCamera = false;
                                _dynamicLodUpdates.Add((entity, physicsBody));
                            }
                        }
                    }
                }
            }

            // Ya no usamos LOD retardado
            if (physicsBody.IsLodDisabled) continue;

            // --- OPTIMIZACION DE SUEÑO ---
            // Si el motor físico ya puso el cubo a dormir, no mutamos la matriz Transform de forma inútil
            if (NativeMethods.PxRigidDynamic_isSleeping((PxRigidDynamic*)physicsBody.Actor)) continue;

            var pose = PxRigidActor_getGlobalPose((PxRigidActor*)physicsBody.Actor);

            var updatedTransform = currentTransform;
            updatedTransform.PreviousPosition = currentTransform.Position;
            updatedTransform.Position = new Vector3(pose.p.x, pose.p.y, pose.p.z);
            updatedTransform.Rotation = new Quaternion(pose.q.x, pose.q.y, pose.q.z, pose.q.w);

            transformArray.Components[entity] = updatedTransform;
        }

        foreach (var update in _dynamicLodUpdates)
        {
            physicsArray.Components[update.Item1] = update.Item2;
        }

        _staticLodUpdates.Clear();
        foreach (var kvp in staticPhysicsArray.Components)
        {
            var entity = kvp.Key;
            var staticBody = kvp.Value;

            if (staticBody.Actor == null) continue;
            if (!transformArray.Components.TryGetValue(entity, out var currentTransform)) continue;

            if (hasCamera && staticBody.UseLod)
            {
                float distSq = Vector3.DistanceSquared(cameraPos, currentTransform.Position);
                float disableDistSq = staticBody.LodDisableDistance * staticBody.LodDisableDistance;

                bool shouldDisable = distSq > disableDistSq;

                if (shouldDisable && !staticBody.IsLodDisabled)
                {
                    staticBody.IsLodDisabled = true;
                    _scene->RemoveActorMut((PxActor*)staticBody.Actor, false);
                    _staticLodUpdates.Add((entity, staticBody));
                }
                else if (!shouldDisable && staticBody.IsLodDisabled)
                {
                    staticBody.IsLodDisabled = false;
                    _scene->AddActorMut((PxActor*)staticBody.Actor, null);
                    _staticLodUpdates.Add((entity, staticBody));
                }
            }
        }

        foreach (var update in _staticLodUpdates)
        {
            staticPhysicsArray.Components[update.Item1] = update.Item2;
        }

        sw.Stop();
        EngineStats.PhysicsTimeMs = sw.Elapsed.TotalMilliseconds;
    }

    public void Cleanup()
    {
        PxScene_release_mut(_scene);
        PxPhysics_release_mut(_physics);
    }
}