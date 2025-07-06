using System.Numerics;
using System.Runtime.InteropServices;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Systems;

public unsafe class PhysicsSystem
{
    private readonly PxScene* _scene;
    private readonly PxPhysics* _physics;
    private readonly PxMaterial* _material;

    public PxPhysics* Physics => _physics;
    public PxMaterial* Material => _material;

    public PhysicsSystem()
    {
        var foundation = physx_create_foundation();
        _physics = physx_create_physics(foundation);
            
        var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(_physics));
        sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f, z = 0.0f };

        var dispatcher = phys_PxDefaultCpuDispatcherCreate(1U, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
        sceneDesc.cpuDispatcher = (PxCpuDispatcher*)dispatcher;
        sceneDesc.filterShader = get_default_simulation_filter_shader();

        _scene = _physics->CreateSceneMut(&sceneDesc);
        
        _material = _physics->CreateMaterialMut(0.8f, 0.6f, 0.1f);
    }

    public void AddActor(PxActor* actor)
    {
        _scene->AddActorMut(actor, null);
    }

    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        _scene->SimulateMut(deltaTime, null, null, 0, true);
        uint error = 0;
        _scene->FetchResultsMut(true, &error);
        
        var transformArray = entityOperator.GetComponentArray<CoordComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();

        foreach (var (entity, physicsBody) in physicsArray.Components)
        {
            if (!transformArray.Components.TryGetValue(entity, out var currentTransform)) continue;

            var pose = PxRigidActor_getGlobalPose((PxRigidActor*)physicsBody.Actor);
            
            var updatedTransform = currentTransform;
            updatedTransform.PreviousPosition = currentTransform.Position;
            updatedTransform.Position = new Vector3(pose.p.x, pose.p.y, pose.p.z);
            updatedTransform.Rotation = new Quaternion(pose.q.x, pose.q.y, pose.q.z, pose.q.w);
            
            transformArray.Components[entity] = updatedTransform;
        }
    }

    public void Cleanup()
    {
        PxScene_release_mut(_scene);
        PxPhysics_release_mut(_physics);
    }
}