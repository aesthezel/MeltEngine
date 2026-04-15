using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using JoltPhysicsSharp;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;

namespace MeltEngine.Systems;

public class PhysicsManager : IDisposable
{
    public const float FIXED_TICK_RATE = 50f;
    public const float FIXED_DELTA_TIME = 1f / FIXED_TICK_RATE;

    private readonly JoltPhysicsSharp.PhysicsSystem _physics;
    private readonly BodyInterface _bodyInterface;
    private readonly JobSystemThreadPool _jobSystem;
    private readonly BroadPhaseLayerInterfaceTable _bpLayerInterface;
    private readonly ObjectVsBroadPhaseLayerFilterTable _objVsBpFilter;
    private readonly ObjectLayerPairFilterTable _objLayerPairFilter;
    private bool _isWarmedUp;
    private bool _broadPhaseOptimized;

    public JoltPhysicsSharp.PhysicsSystem Physics => _physics;
    public BodyInterface BodyInterface => _bodyInterface;

    public void AddShape(Shape shape) { }
    public bool OptimizeBroadPhase() 
    { 
        if (_broadPhaseOptimized) return false;
        _physics.OptimizeBroadPhase();
        _broadPhaseOptimized = true;
        return true;
    }

    public PhysicsManager()
    {
        if (!Foundation.Init())
        {
            throw new Exception("Error fatal: No se pudo inicializar JoltPhysics.");
        }

        uint numThreads = (uint)System.Environment.ProcessorCount;
        var jobSystemConfig = new JobSystemThreadPoolConfig
        {
            maxJobs = Foundation.MaxPhysicsJobs,
            maxBarriers = Foundation.MaxPhysicsBarriers,
            numThreads = Math.Max(1, (int)numThreads - 1)
        };
        _jobSystem = new JobSystemThreadPool(jobSystemConfig);

        _bpLayerInterface = new BroadPhaseLayerInterfaceTable(JoltConfig.NumObjectLayers, JoltConfig.NumBroadPhaseLayers);
        _bpLayerInterface.MapObjectToBroadPhaseLayer(JoltConfig.LayerNonMoving, JoltConfig.BPNonMoving);
        _bpLayerInterface.MapObjectToBroadPhaseLayer(JoltConfig.LayerMoving, JoltConfig.BPMoving);
        _bpLayerInterface.MapObjectToBroadPhaseLayer(JoltConfig.LayerDynamicFar, JoltConfig.BPFar);
        _bpLayerInterface.MapObjectToBroadPhaseLayer(JoltConfig.LayerDisabled, JoltConfig.BPNonMoving);

        _objLayerPairFilter = new ObjectLayerPairFilterTable(JoltConfig.NumObjectLayers);
        
        _objLayerPairFilter.EnableCollision(JoltConfig.LayerNonMoving, JoltConfig.LayerMoving);
        _objLayerPairFilter.EnableCollision(JoltConfig.LayerMoving, JoltConfig.LayerMoving);
        
        _objLayerPairFilter.EnableCollision(JoltConfig.LayerNonMoving, JoltConfig.LayerDynamicFar);
        _objLayerPairFilter.EnableCollision(JoltConfig.LayerMoving, JoltConfig.LayerDynamicFar);
        
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDynamicFar, JoltConfig.LayerDynamicFar);
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDynamicFar, JoltConfig.LayerDisabled);
        
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDisabled, JoltConfig.LayerDisabled);
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDisabled, JoltConfig.LayerNonMoving);
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDisabled, JoltConfig.LayerMoving);
        _objLayerPairFilter.DisableCollision(JoltConfig.LayerDisabled, JoltConfig.LayerDynamicFar);

        _objVsBpFilter = new ObjectVsBroadPhaseLayerFilterTable(
            _bpLayerInterface, JoltConfig.NumBroadPhaseLayers,
            _objLayerPairFilter, JoltConfig.NumObjectLayers);

        var settings = new PhysicsSystemSettings
        {
            MaxBodies = 100000,
            NumBodyMutexes = 0,
            MaxBodyPairs = 50000,
            MaxContactConstraints = 50000,
            BroadPhaseLayerInterface = _bpLayerInterface,
            ObjectVsBroadPhaseLayerFilter = _objVsBpFilter,
            ObjectLayerPairFilter = _objLayerPairFilter
        };

        _physics = new JoltPhysicsSharp.PhysicsSystem(settings);
        _physics.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        _bodyInterface = _physics.BodyInterface;
    }

    public void Simulate(float deltaTime)
    {
        var sw = Stopwatch.StartNew();
        
        if (!_isWarmedUp)
        {
            PreWarm();
            _isWarmedUp = true;
        }

        _physics.Update(deltaTime, 1, _jobSystem);
        sw.Stop();
        EngineStats.PhysicsTimeMs = sw.Elapsed.TotalMilliseconds;
    }

    public void PreWarm()
    {
        for (int i = 0; i < 3; i++)
        {
            _physics.Update(FIXED_DELTA_TIME, 1, _jobSystem);
        }
    }

    public void Update(ECSOperator entityOperator)
    {
        var transformArray = entityOperator.GetComponentArray<CoordComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();

        var bodyInterface = _bodyInterface;
        var transforms = transformArray.Components;
        var bodies = physicsArray.Components;
        var denseEntities = physicsArray.DenseEntities;
        var count = physicsArray.Count;

        for (int i = 0; i < count; i++)
        {
            var entity = denseEntities[i];
            if (!bodies.TryGetValue(entity, out var physicsBody)) continue;
            if (physicsBody.BodyId.IsInvalid || physicsBody.IsLodDisabled) continue;
            if (!bodyInterface.IsActive(physicsBody.BodyId)) continue;
            if (!transforms.TryGetValue(entity, out var currentTransform)) continue;

            currentTransform.PreviousPosition = currentTransform.Position;
            currentTransform.Position = bodyInterface.GetPosition(physicsBody.BodyId);
            currentTransform.Rotation = bodyInterface.GetRotation(physicsBody.BodyId);
            transforms[entity] = currentTransform;
        }
    }

    public void UpdateLod(ECSOperator entityOperator)
    {
        var transformArray = entityOperator.GetComponentArray<CoordComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        var staticPhysicsArray = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();

        Vector3 cameraPos = Vector3.Zero;
        bool hasCamera = false;

        foreach (var cam in cameraArray.Components.Values)
        {
            cameraPos = cam.Camera.Position;
            hasCamera = true;
            break;
        }

        if (!hasCamera) return;

        var bodyInterface = _bodyInterface;
        var physicsBodies = physicsArray.Components;
        var physicsDense = physicsArray.DenseEntities;
        var physicsCount = physicsArray.Count;
        var transforms = transformArray.Components;

        for (int i = 0; i < physicsCount; i++)
        {
            var entity = physicsDense[i];
            if (!physicsBodies.TryGetValue(entity, out var physicsBody)) continue;
            if (physicsBody.BodyId.IsInvalid) continue;
            if (!physicsBody.UseLod) continue;
            if (!transforms.TryGetValue(entity, out var currentTransform)) continue;

            float distSq = Vector3.DistanceSquared(cameraPos, currentTransform.Position);
            float disableDistSq = physicsBody.LodDisableDistance * physicsBody.LodDisableDistance;

            if (distSq > disableDistSq && !physicsBody.IsLodDisabled)
            {
                physicsBody.IsLodDisabled = true;
                bodyInterface.RemoveBody(physicsBody.BodyId);
                physicsBodies[entity] = physicsBody;
            }
            else if (distSq <= disableDistSq && physicsBody.IsLodDisabled)
            {
                physicsBody.IsLodDisabled = false;
                bodyInterface.AddBody(physicsBody.BodyId, Activation.Activate);
                physicsBodies[entity] = physicsBody;
            }
        }

        var staticBodies = staticPhysicsArray.Components;
        var staticDense = staticPhysicsArray.DenseEntities;
        var staticCount = staticPhysicsArray.Count;

        for (int i = 0; i < staticCount; i++)
        {
            var entity = staticDense[i];
            if (!staticBodies.TryGetValue(entity, out var staticBody)) continue;
            if (staticBody.BodyId.IsInvalid) continue;
            if (!staticBody.UseLod) continue;
            if (!transforms.TryGetValue(entity, out var currentTransform)) continue;

            float distSq = Vector3.DistanceSquared(cameraPos, currentTransform.Position);
            float disableDistSq = staticBody.LodDisableDistance * staticBody.LodDisableDistance;

            if (distSq > disableDistSq && !staticBody.IsLodDisabled)
            {
                staticBody.IsLodDisabled = true;
                bodyInterface.RemoveBody(staticBody.BodyId);
                staticBodies[entity] = staticBody;
            }
            else if (distSq <= disableDistSq && staticBody.IsLodDisabled)
            {
                staticBody.IsLodDisabled = false;
                bodyInterface.AddBody(staticBody.BodyId, Activation.Activate);
                staticBodies[entity] = staticBody;
            }
        }
    }

    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _physics?.Dispose();
        _jobSystem?.Dispose();
        _bpLayerInterface?.Dispose();
        _objVsBpFilter?.Dispose();
        _objLayerPairFilter?.Dispose();
    }
}
