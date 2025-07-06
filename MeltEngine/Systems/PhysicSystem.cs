using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Physics;

public unsafe class PhysicsSystem
{
    private readonly PxScene* scene;
    private readonly PxPhysics* physics;
    private readonly PxMaterial* material;

    public PhysicsSystem()
    {
        var foundation = physx_create_foundation();
        physics = physx_create_physics(foundation);
        
        var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(physics));
        sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f * 10, z = 0.0f };

        var dispatcher = phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
        sceneDesc.cpuDispatcher = (PxCpuDispatcher*)dispatcher;
        sceneDesc.filterShader = get_default_simulation_filter_shader();

        scene = physics->CreateSceneMut(&sceneDesc);
        material = physics->CreateMaterialMut(0.5f, 0.5f, 0.6f);
    }

    public PxScene* GetScene() => scene;
    public PxPhysics* GetPhysics() => physics;
    public PxMaterial* GetMaterial() => material;

    public void Simulate(float deltaTime)
    {
        scene->SimulateMut(deltaTime, null, null, 0, true);
        uint error = 0;
        scene->FetchResultsMut(true, &error);
    }

    public void AddActor(PxActor* actor)
    {
        scene->AddActorMut(actor, null);
    }

    public void Cleanup()
    {
        PxScene_release_mut(scene);
        PxPhysics_release_mut(physics);
    }
}