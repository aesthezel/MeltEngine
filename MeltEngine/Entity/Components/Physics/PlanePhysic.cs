using MagicPhysX;
using static MagicPhysX.NativeMethods;
using MeltEngine.Physics;
using System.Numerics;

namespace MeltEngine.Entity.Components.Physics;

public unsafe class PlanePhysic : Behaviour
{
    private PxRigidStatic* planeActor;
    private PhysicsSystem physicsSystem;

    private float width;
    private float height;
    private Vector3 position;
    
    public PlanePhysic(PhysicsSystem physicsSystem, float width, float height, Vector3 position)
    {
        this.physicsSystem = physicsSystem;
        this.width = width;
        this.height = height;
        this.position = position;
    }
    
    protected override void Start()
    {
        var boxGeo = PxBoxGeometry_new(width, 1, height);
        
        var pxPos = new PxVec3 { x = position.X, y = position.Y, z = position.Z };
        var transform = PxTransform_new_1(&pxPos);
        var shapeOffset = PxTransform_new_2(PxIDENTITY.PxIdentity);
        
        planeActor = physicsSystem.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&boxGeo, physicsSystem.GetMaterial(), &shapeOffset);
        
        physicsSystem.AddActor((PxActor*)planeActor);
    }
}