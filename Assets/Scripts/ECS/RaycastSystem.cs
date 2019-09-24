using Unity.Entities;
using UnityEngine;

unsafe public class RaycastSystem : ComponentSystem
{
    private EntityQuery mRaycastRequestsQuery;

    protected override void OnCreate()
    {
        mRaycastRequestsQuery = GetEntityQuery(ComponentType.ReadOnly<RayRequest>());
    }

    // This should be a job rather than happening on the main thread. However, it doesn't really take a long time, and happens infrequently.
    protected override void OnUpdate()
    {
        var physicsWorldSystem = World.Active.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        Entities.With(mRaycastRequestsQuery).ForEach((Entity entity, ref RayRequest r) =>
            {
                Unity.Physics.RaycastInput input = new Unity.Physics.RaycastInput()
                {
                    Start = r.Value.Origin,
                    End = r.Value.Displacement,
                    Filter = Unity.Physics.CollisionFilter.Default,
                };

                Debug.DrawLine(input.Start, input.End, Color.blue, 0.5f);

                var hit = new Unity.Physics.RaycastHit();
                if (collisionWorld.CastRay(input, out hit))
                {
                    var e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                    if (EntityManager.HasComponent<IFCGuidUIPosition>(e))
                    {
                        PostUpdateCommands.RemoveComponent<IFCGuidUIPosition>(e);
                        PostUpdateCommands.AddComponent<DeselectedTag>(e);
                    }
                    else
                    {
                        PostUpdateCommands.AddComponent(e, new IFCGuidUIPosition { Value = hit.Position });
                    }
                }

                PostUpdateCommands.DestroyEntity(entity);
            });
    }
}
