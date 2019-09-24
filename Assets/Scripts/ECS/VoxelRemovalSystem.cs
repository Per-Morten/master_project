using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;

[
    UpdateAfter(typeof(MeshStreamerSystem)),
    UpdateAfter(typeof(ColliderStreamerSystem))
]
unsafe public class VoxelRemovalSystem : ComponentSystem
{
    // Queries
    private EntityQuery mGFXMeshesQuery;
    private EntityQuery mEmptyVoxelQuery;
    private EntityQuery mColliderMeshesQuery;
    public Pool MeshGoPool;

    protected override void OnCreate()
    {
        mEmptyVoxelQuery = GetEntityQuery(ComponentType.ReadOnly<VoxelRemovalTag>());
        mGFXMeshesQuery = GetEntityQuery(ComponentType.ReadOnly<VoxelRemovalTag>(), ComponentType.ReadOnly<GFXMeshes>());
        mColliderMeshesQuery = GetEntityQuery(ComponentType.ReadOnly<VoxelRemovalTag>(), ComponentType.ReadOnly<ColliderMeshes>());
    }

    protected override void OnUpdate()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Destroying GFX Meshes");
        Entities.With(mGFXMeshesQuery).ForEach((Entity e, DynamicBuffer<GFXMeshes> gfxMeshes) =>
        {
            for (int i = 0; i < gfxMeshes.Length; i++)
            {
                PostUpdateCommands.DestroyEntity(gfxMeshes[i].Value);
                var trans = EntityManager.GetComponentObject<GameObject>(gfxMeshes[i].Value);
                MeshGoPool.DestroyObject(trans);
            }
        });
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Destroying Collider Meshes");
        Entities.With(mColliderMeshesQuery).ForEach((Entity e, DynamicBuffer<ColliderMeshes> colliderMeshes) =>
        {
            var colliders = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(colliderMeshes.Length, Allocator.TempJob);
            for (int i = 0; i < colliderMeshes.Length; i++)
            {
                if (EntityManager.HasComponent<GameObject>(colliderMeshes[i].Value))
                    GameObject.Destroy(EntityManager.GetComponentObject<GameObject>(colliderMeshes[i].Value));

                PostUpdateCommands.DestroyEntity(colliderMeshes[i].Value);
                
                // Pretty sure I need to dispose of these components as I keep recreating new ones each time I spawn a new object,
                // Rather than re-using them.
                colliders[i] = (EntityManager.GetComponentData<Unity.Physics.PhysicsCollider>(colliderMeshes[i].Value).Value);
            }

            new RemoveColliderMeshesJob
            {
                Colliders = colliders
            }.Schedule(colliders.Length, 100);
        });
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Destroying Entities");
        PostUpdateCommands.DestroyEntity(mEmptyVoxelQuery);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    [Unity.Burst.BurstCompile]
    public struct RemoveColliderMeshesJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<BlobAssetReference<Unity.Physics.Collider>> Colliders;

        public void Execute(int index)
        {
            Colliders[index].Dispose();
        }
    }
}

