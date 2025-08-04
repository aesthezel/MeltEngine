using System.Numerics;
using MagicPhysX;
using MeltEngine.Physics;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Entity.Components.Physics;

public unsafe class CubePhysics : Behaviour
{
    private Coord _coord;
    private PxRigidDynamic* cubeActor;
    private PhysicsSystem physicsSystem;

    private Vector3 initialPosition;
    private float size;
    private float mass;

    public Vector3 Position { get; private set; }

    // Constructor que recibe parámetros del cubo
    public CubePhysics(PhysicsSystem physicsSystem, Vector3 position, float size, float mass)
    {
        this.physicsSystem = physicsSystem;
        this.initialPosition = position;
        this.size = size;
        this.mass = mass;
    }
    
    public override void Start()
    {
        _coord = GameObject.GetBehaviour<Coord>();
        var boxGeo = PxBoxGeometry_new(size, size, size);
        var pxPos = new PxVec3 { x = initialPosition.X, y = initialPosition.Y, z = initialPosition.Z };
        var transform = PxTransform_new_1(&pxPos);
        var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
        
        cubeActor = physicsSystem.GetPhysics()->PhysPxCreateDynamic(
            &transform, (PxGeometry*)&boxGeo, physicsSystem.GetMaterial(), mass, &identity);
        
        physicsSystem.AddActor((PxActor*)cubeActor);
    }
    
    public void ApplyForce(Vector3 force)
    {
        if (cubeActor == null) return;
        var pxForce = new PxVec3 { x = force.X, y = force.Y, z = force.Z };
        PxRigidDynamic_setLinearVelocity_mut(cubeActor, &pxForce, true);
    }
    
    public override void Update()
    {
        if (cubeActor == null) return;
        var pose = PxRigidActor_getGlobalPose((PxRigidActor*)cubeActor);
        Position = new Vector3(pose.p.x, pose.p.y, pose.p.z);
        _coord.Position = Position;
    }
    
    public override void DestroyGameObject(GameObject go)
    {
        if (cubeActor != null)
        {
            PxRigidDynamic_putToSleep_mut(cubeActor);
            cubeActor = null;
        }
        base.DestroyGameObject(go);
    }
}