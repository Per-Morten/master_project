using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[
    UpdateBefore(typeof(MeshStreamerSystem)), 
    UpdateBefore(typeof(ColliderStreamerSystem))
]
public class VoxelStreamRequesterSystem : ComponentSystem
{
    private EntityQuery mPlayerQuery;
    private EntityQuery mVoxelQuery;
    private EntityQuery mCurrentVoxelsQuery;
    private EntityArchetype mRequestArchetype;

    private int mSystemsBusy = 0;

    NativeHashMap<Voxel, Empty> mRelevantBounds;

    // Utility
    private struct Empty { };

    protected override void OnCreate()
    {
        mPlayerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Unity.Transforms.Translation>());
        mVoxelQuery = GetEntityQuery(ComponentType.ReadOnly<Layers>());
        mCurrentVoxelsQuery = GetEntityQuery(ComponentType.ReadOnly<StreamedInVoxel>());
        mRequestArchetype = EntityManager.CreateArchetype(new ComponentType[] { typeof(StreamedInVoxel), typeof(GFXRequest), typeof(ColliderRequest) });

        mRelevantBounds = new NativeHashMap<Voxel, Empty>(512, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        mRelevantBounds.Dispose();
    }

    public void NotifyBusy()
    {
        mSystemsBusy++;
    }

    public void NotifyReady()
    {
        mSystemsBusy--;
        Debug.Assert(mSystemsBusy >= 0);
    }

    public static int3 FindInLayer(DynamicBuffer<AABB> bounds, int3 dimensions, float3 target, int3 begin, int3 end)
    {
        for (int z = begin.z; z < end.z; z++)
        {
            for (int y = begin.y; y < end.y; y++)
            {
                for (int x = begin.x; x < end.x; x++)
                {
                    var coords = new int3(x, y, z);
                    if (bounds[IndexUtility.CoordsToIdx(coords, dimensions)].Contains(target))
                    {
                        return coords;
                    }
                }
            }
        }

        return -1;
    }

    unsafe protected override void OnUpdate()
    {
        if (mSystemsBusy != 0)
            return;

        UnityEngine.Profiling.Profiler.BeginSample("Finding Player");

        // Gather Necessary Entities
        float3 playerPos;
        {
            var tmp = mPlayerQuery.ToComponentDataArray<Unity.Transforms.Translation>(Allocator.TempJob);
            playerPos = tmp[0].Value;
            tmp.Dispose();
        }

        Entity grid;
        {
            var tmp = mVoxelQuery.ToEntityArray(Allocator.TempJob);
            grid = tmp[0];
            tmp.Dispose();
        }

        var layers = EntityManager.GetBuffer<Layers>(grid).Reinterpret<Entity>();
        var dimensions = EntityManager.GetBuffer<Dimensions>(grid).Reinterpret<int3>();

        UnityEngine.Profiling.Profiler.BeginSample("Find Player In Layer");
        // Find Player in Layer
        // TODO: Check if player is within same bounds as last time, cause In that case we don't need to do anything.
        var targetCoords = int3.zero;
        {
            var prevDim = new int3(1);
            for (int i = 1; i < layers.Length; i++)
            {
                var lengths = dimensions[i] / prevDim;
                var begin = IndexUtility.CoordsToSubGrid(targetCoords, prevDim, dimensions[i]);
                var end = begin + lengths;
                var bounds = EntityManager.GetBuffer<VoxelAABB>(layers[i]).Reinterpret<AABB>();

                targetCoords = FindInLayer(bounds, dimensions[i], playerPos, begin, end);
                prevDim = dimensions[i];

                if (targetCoords.x == -1)
                    break;
            }
        }

        UnityEngine.Profiling.Profiler.EndSample();


        UnityEngine.Profiling.Profiler.BeginSample("Find Relevant Bounds");
        // Find All Relevant Bounds
        mRelevantBounds.Clear();

        // We couldn't find the player in any of the bounds.
        // We don't just want to return here, but we want to skip the step of finding relevant bounds.
        if (targetCoords.x >= 0)
        {
            const int boxesInEachDirection = 3;
            var lowestLayer = dimensions[dimensions.Length - 1];

            for (int z = math.max(targetCoords.z - boxesInEachDirection, 0); z <= math.min(targetCoords.z + boxesInEachDirection, lowestLayer.z - 1); z++)
            {
                for (int y = math.max(targetCoords.y - boxesInEachDirection, 0); y <= math.min(targetCoords.y + boxesInEachDirection, lowestLayer.y - 1); y++)
                {
                    for (int x = math.max(targetCoords.x - boxesInEachDirection, 0); x <= math.min(targetCoords.x + boxesInEachDirection, lowestLayer.x - 1); x++)
                    {
                        // "Me"
                        var coords = new int3(x, y, z);
                        mRelevantBounds.TryAdd(new Voxel { Layer = layers.Length - 1, Index = IndexUtility.CoordsToIdx(coords, lowestLayer) }, new Empty());

                        // Everyone who contains "Me", Ignoring top layer box as that is static.
                        for (int i = layers.Length - 2; i > 0; i--)
                        {
                            mRelevantBounds.TryAdd(new Voxel { Layer = i, Index = IndexUtility.CoordsToIdx(IndexUtility.CoordsToOuterGrid(coords, lowestLayer, dimensions[i]), dimensions[i]) }, new Empty());
                        }
                    }
                }
            }
        }

        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Mark Bounds for Deletion");

        //Deletion
        // Mark Entities for Deletion
        Entities.With(mCurrentVoxelsQuery).ForEach((Entity e, ref StreamedInVoxel v) =>
        {
            if (!mRelevantBounds.TryGetValue(v.Value, out Empty _))
            {
                PostUpdateCommands.AddComponent(e, new VoxelRemovalTag());
            }
            else
            {
                mRelevantBounds.Remove(v.Value);
            }
        });


        var toStream = mRelevantBounds.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < toStream.Length; i++)
            PostUpdateCommands.SetComponent(PostUpdateCommands.CreateEntity(mRequestArchetype), new StreamedInVoxel { Value = toStream[i] });

        toStream.Dispose();

        UnityEngine.Profiling.Profiler.EndSample();
        UnityEngine.Profiling.Profiler.EndSample();
    }
}
