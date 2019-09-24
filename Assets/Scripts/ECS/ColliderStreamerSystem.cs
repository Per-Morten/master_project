using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

unsafe public class ColliderStreamerSystem : ComponentSystem
{
    private EntityQuery mMemoryMapQuery;
    private EntityQuery mStreamingRequestQuery;

    private enum State
    {
        Ready,
        Working,
    }

    private float mStreamStart = 0.0f;

    State mState = State.Ready;

    private bool mFITLoaded = false;

    List<NativeList<BlobAssetReference<Unity.Physics.Collider>>> mColliders;
    NativeList<Entity> mEntities;
    JobHandle mJobHandle;
    NativeList<Voxel> mVoxels;
    EntityArchetype mIFCArchetype;

    protected override unsafe void OnCreate()
    {
        mMemoryMapQuery = GetEntityQuery(ComponentType.ReadOnly<MemoryMap>());
        mStreamingRequestQuery = GetEntityQuery(ComponentType.ReadOnly<ColliderRequest>(), ComponentType.ReadOnly<StreamedInVoxel>());

        mColliders = new List<NativeList<BlobAssetReference<Unity.Physics.Collider>>>();
        mEntities = new NativeList<Entity>(Allocator.Persistent);
        mVoxels = new NativeList<Voxel>(Allocator.Persistent);

        mIFCArchetype = EntityManager.CreateArchetype(typeof(Unity.Transforms.Translation),
                                                      typeof(Unity.Transforms.Rotation),
                                                      typeof(Unity.Physics.PhysicsCollider),
                                                      typeof(Unity.Transforms.Frozen),
                                                      typeof(IFCGuid),
                                                      typeof(IFCObjectID));
    }

    public unsafe void LoadFITLayer(MemoryMap map)
    {
        var objectCount = GetObjectCount(0, 0, map);
        if (objectCount == 0)
            return;

        mColliders.Add(new NativeList<BlobAssetReference<Unity.Physics.Collider>>(Allocator.Persistent));
        mColliders[0].ResizeUninitialized(objectCount);
        CreateLoadParallelJob(0, 0, map, mColliders[0]).Schedule(objectCount, 100).Complete();

        for (int i = 0; i < mColliders[0].Length; i++)
        {
            var e = PostUpdateCommands.CreateEntity(mIFCArchetype);

            PostUpdateCommands.SetComponent(e, new IFCGuid { mValue = &map.mNames[i * 22] });
            PostUpdateCommands.SetComponent(e, new IFCObjectID { Voxel = new Voxel { Layer = 0, Index = 0 }, Index = i });
            PostUpdateCommands.SetComponent(e, new Unity.Physics.PhysicsCollider { Value = mColliders[0][i] });
        }
    }

    protected override void OnDestroy()
    {
        mEntities.Dispose();
        mVoxels.Dispose();

        for (int i = 0; i < mColliders.Count; i++)
            mColliders[i].Dispose();
    }

    private int GetObjectCount(int layer, int idx, MemoryMap map)
    {
        var dimensions = map.Dimensions;
        int offset = 0;
        for (int i = 0; i < layer; i++)
            offset += dimensions[i].x * dimensions[i].y * dimensions[i].z;

        var begin = map.Contains[offset + idx].Begin;
        var end = map.Contains[offset + idx].End;
        var objectCount = end - begin;

        return objectCount;
    }

    private unsafe LoadColliderParallelJob CreateLoadParallelJob(int layer, int voxelIdx, MemoryMap map, NativeList<BlobAssetReference<Unity.Physics.Collider>> buffer)
    {
        var dimensions = map.Dimensions;
        int offset = 0;
        for (int i = 0; i < layer; i++)
            offset += dimensions[i].x * dimensions[i].y * dimensions[i].z;

        var begin = map.Contains[offset + voxelIdx].Begin;
        var end = map.Contains[offset + voxelIdx].End;
        var objectCount = end - begin;
        Debug.Assert(objectCount > 0);

        var blobBegin = map.ColliderBlobInfo[begin].Begin;
        var blob = map.ColliderBlobInfo[begin].End;
        var blobEnd = map.ColliderBlobInfo[end - 1].End;

        var job = new LoadColliderParallelJob
        {
            OutColliders = buffer.AsParallelWriter(),
            BlobInfo = &map.ColliderBlobInfo[begin],
            Colliders = map.ColliderBlobs,
        };

        return job;
    }

    private byte* GetGuid(int layer, int voxelIdx, MemoryMap map, int objectIdx)
    {
        var dimensions = map.Dimensions;
        int offset = 0;
        for (int i = 0; i < layer; i++)
            offset += dimensions[i].x * dimensions[i].y * dimensions[i].z;

        var begin = map.Contains[offset + voxelIdx].Begin;
        var end = map.Contains[offset + voxelIdx].End;
        var objectCount = end - begin;
        Debug.Assert(objectCount > 0);

        return &map.mNames[(begin + objectIdx) * 22];
    }

    unsafe protected override void OnUpdate()
    {
        if (!mFITLoaded)
        {
            MemoryMap map;
            {
                var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
                map = tmp[0];
                tmp.Dispose();
            }
            LoadFITLayer(map);
            mFITLoaded = true;
        }

        ParallelForUpdate();
    }

    unsafe public void ParallelForUpdate()
    {
        if (mState == State.Ready)
        {
            mStreamStart = Time.realtimeSinceStartup;

            var entities = mStreamingRequestQuery.ToEntityArray(Allocator.TempJob);
            var requests = mStreamingRequestQuery.ToComponentDataArray<StreamedInVoxel>(Allocator.TempJob);
            PostUpdateCommands.RemoveComponent(mStreamingRequestQuery, typeof(ColliderRequest));

            MemoryMap map;
            {
                var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
                map = tmp[0];
                tmp.Dispose();
            }

            mEntities.Clear();
            mVoxels.Clear();
            for (int i = mColliders.Count; i < requests.Length; i++)
            {
                mColliders.Add(new NativeList<BlobAssetReference<Unity.Physics.Collider>>(Allocator.Persistent));
            }


            int count = 0;
            var handles = new NativeList<JobHandle>(Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
            {
                var objectCount = GetObjectCount(requests[i].Value.Layer, requests[i].Value.Index, map);
                if (objectCount > 0)
                {
                    mColliders[count].ResizeUninitialized(objectCount);
                    mEntities.Add(entities[i]);
                    mVoxels.Add(requests[i].Value);
                    handles.Add(CreateLoadParallelJob(requests[i].Value.Layer, requests[i].Value.Index, map, mColliders[count]).Schedule(objectCount, 100));
                    count++;
                }
            }
            mJobHandle = JobHandle.CombineDependencies(handles.AsArray());
            JobHandle.ScheduleBatchedJobs();
            handles.Dispose();

            if (mEntities.Length > 0)
            {
                mState = State.Working;
                World.Active.GetExistingSystem<VoxelStreamRequesterSystem>().NotifyBusy();
            }
            entities.Dispose();
            requests.Dispose();
        }
        else if (mState == State.Working && mJobHandle.IsCompleted)
        {
            MemoryMap map;
            {
                var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
                map = tmp[0];
                tmp.Dispose();
            }

            mJobHandle.Complete();
            int count = 0;
            for (int i = 0; i < mEntities.Length; i++)
            {
                var buff = PostUpdateCommands.AddBuffer<ColliderMeshes>(mEntities[i]);
                buff.Reserve(mColliders[i].Length);

                for (int j = 0; j < mColliders[i].Length; j++)
                {
                    var e = PostUpdateCommands.CreateEntity(mIFCArchetype);

                    PostUpdateCommands.SetComponent(e, new Unity.Physics.PhysicsCollider { Value = mColliders[i][j] });
                    PostUpdateCommands.SetComponent(e, new IFCGuid { mValue = GetGuid(mVoxels[i].Layer, mVoxels[i].Index, map, j) });
                    PostUpdateCommands.SetComponent(e, new IFCObjectID { Voxel = mVoxels[i], Index = j });
                    count++;
                    buff.Add(new ColliderMeshes { Value = e });
                }
            }

            mState = State.Ready;
            World.Active.GetExistingSystem<VoxelStreamRequesterSystem>().NotifyReady();

            Debug.Log($"Loaded: {count} colliders in {Time.realtimeSinceStartup - mStreamStart} seconds");
        }
    }

    struct LoadColliderParallelJob : IJobParallelFor
    {
        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public Range* BlobInfo;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public void* Colliders;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BlobAssetReference<Unity.Physics.Collider>> OutColliders;

        public void Execute(int index)
        {
            var colliderStart = BlobInfo[index].Begin;
            var colliderEnd = BlobInfo[index].End;
            var length = colliderEnd - colliderStart;

            var reader = new Unity.Entities.Serialization.MemoryBinaryReader((byte*)Colliders + colliderStart);
            var coll = reader.Read<Unity.Physics.Collider>();
            reader.Dispose();
            OutColliders[index] = coll;
        }
    }
}
