using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System;

unsafe public class MeshStreamerSystem : ComponentSystem
{
    // Queries
    private EntityQuery mMemoryMapQuery;
    private EntityQuery mStreamingRequestQuery;

    // GameObject management
    public Pool MeshGoPool;

    // Streaming Related
    private struct SoAMesh
    {
        public List<Vector3> Vertices;
        public List<Vector3> Normals;
        public List<Color32> Colors;
        public List<int> Indices;

        public SoAMesh(int vertexCount, int indexCount)
        {
            Vertices = new List<Vector3>(vertexCount);
            Normals = new List<Vector3>(vertexCount);
            Colors = new List<Color32>(vertexCount);
            Indices = new List<int>(indexCount);
            NoAllocHelpers.ResizeList(Vertices, vertexCount);
            NoAllocHelpers.ResizeList(Normals, vertexCount);
            NoAllocHelpers.ResizeList(Colors, vertexCount);
            NoAllocHelpers.ResizeList(Indices, indexCount);
        }

        public void GetArrays(out Vector3[] vertices, out Vector3[] normals, out Color32[] colors, out int[] indices)
        {
            vertices = NoAllocHelpers.ExtractArrayFromListT(Vertices);
            normals = NoAllocHelpers.ExtractArrayFromListT(Normals);
            colors = NoAllocHelpers.ExtractArrayFromListT(Colors);
            indices = NoAllocHelpers.ExtractArrayFromListT(Indices);
        }

        // Note: No checking that there is actually this many vertices in here.
        // However, it will increase capacity if needed.
        public void SetVertexCount(int count)
        {
            if (count > Vertices.Capacity)
            {
                Vertices.Capacity = count;
                Normals.Capacity = count;
                Colors.Capacity = count;
            }
            NoAllocHelpers.ResizeList(Vertices, count);
            NoAllocHelpers.ResizeList(Normals, count);
            NoAllocHelpers.ResizeList(Colors, count);
        }

        public void SetIndexCount(int count)
        {
            if (count > Indices.Capacity)
                Indices.Capacity = count;
            NoAllocHelpers.ResizeList(Indices, count);
        }

        public int VertexCount => Vertices.Count;
        public int IndexCount => Indices.Count;
    }

    private struct MeshGCHandles
    {
        public GCHandle VerticesHandle;
        public GCHandle NormalsHandle;
        public GCHandle ColorsHandle;
        public GCHandle IndicesHandle;

        public MeshGCHandles(Vector3[] vertices, Vector3[] normals, Color32[] colors, int[] indices)
        {
            VerticesHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            NormalsHandle = GCHandle.Alloc(normals, GCHandleType.Pinned);
            ColorsHandle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IndicesHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            VerticesHandle.Free();
            NormalsHandle.Free();
            ColorsHandle.Free();
            IndicesHandle.Free();
        }
    }

    private List<SoAMesh> mOpaqueMeshes = new List<SoAMesh>();
    private List<SoAMesh> mTransparentMeshes = new List<SoAMesh>();
    private List<MeshGCHandles> mOpaqueGCHandles = new List<MeshGCHandles>();
    private List<MeshGCHandles> mTransparentGCHandles = new List<MeshGCHandles>();

    private NativeList<int> mOpaqueResVerticesCountBuff;
    private NativeList<int> mOpaqueResIndicesCountBuff;

    private NativeList<int> mTransparentResVerticesCountBuff;
    private NativeList<int> mTransparentResIndicesCountBuff;

    private NativeList<Entity> mCorrespondingEntities;
    private NativeList<LoadVerticesJob> mVerticesJobs;
    private NativeList<JobHandle> mVerticesJobsHandles;
    private JobHandle mCombinedVerticesJobHandle;

    long mBytesLoaded = 0;

    private bool mFITLoaded = false;

    // State Related
    enum State
    {
        Preparing,
        LoadingVertices,
        InstantiatingMeshes,
    }

    private State mState = State.Preparing;

    private int mInstantiateCount = 0;

    private double mStartStreamTime;
    private double mBeginSSDLoadTime;

    protected override unsafe void OnCreate()
    {
        mMemoryMapQuery = GetEntityQuery(ComponentType.ReadOnly<MemoryMap>());
        mStreamingRequestQuery = GetEntityQuery(ComponentType.ReadWrite<GFXRequest>(), ComponentType.ReadOnly<StreamedInVoxel>());

        mVerticesJobs = new NativeList<LoadVerticesJob>(Allocator.Persistent);
        mVerticesJobsHandles = new NativeList<JobHandle>(Allocator.Persistent);
        mOpaqueResVerticesCountBuff = new NativeList<int>(Allocator.Persistent);
        mOpaqueResIndicesCountBuff = new NativeList<int>(Allocator.Persistent);
        mTransparentResVerticesCountBuff = new NativeList<int>(Allocator.Persistent);
        mTransparentResIndicesCountBuff = new NativeList<int>(Allocator.Persistent);
        mCorrespondingEntities = new NativeList<Entity>(Allocator.Persistent);
    }

    public unsafe void LoadFITLayer(MemoryMap map)
    {
        mOpaqueMeshes.Add(new SoAMesh(1, 1));
        mTransparentMeshes.Add(new SoAMesh(1, 1));
        mOpaqueGCHandles.Add(new MeshGCHandles());
        mTransparentGCHandles.Add(new MeshGCHandles());
        mOpaqueResVerticesCountBuff.ResizeUninitialized(1);
        mOpaqueResIndicesCountBuff.ResizeUninitialized(1);
        mTransparentResVerticesCountBuff.ResizeUninitialized(1);
        mTransparentResIndicesCountBuff.ResizeUninitialized(1);

        var parent = Globals.Instance.data.StaticParent;

        if (CreateVoxelLoadingJob(0, 0, 0, map, out LoadVerticesJob job))
        {
            job.Run();
            mOpaqueGCHandles[job.BuffIdx].Dispose();
            mTransparentGCHandles[job.BuffIdx].Dispose();

            if (job.OpaqueResVerticesCount[job.BuffIdx] != 0)
            {
                var go = CreateGameObject("Static IFCLayer Opaque",
                                          mOpaqueMeshes[job.BuffIdx],
                                          Globals.Instance.data.OpaqueMaterial);
                go.transform.parent = parent.transform;
            }
            if (job.TransparentResVerticesCount[job.BuffIdx] != 0)
            {
                var go = CreateGameObject("Static IFCLayer Transparent",
                                          mTransparentMeshes[job.BuffIdx],
                                          Globals.Instance.data.TransparentMaterial);
                go.transform.parent = parent.transform;
            }
        }
    }

    protected override void OnDestroy()
    {
        mVerticesJobs.Dispose();
        mVerticesJobsHandles.Dispose();
        mOpaqueResVerticesCountBuff.Dispose();
        mOpaqueResIndicesCountBuff.Dispose();
        mTransparentResVerticesCountBuff.Dispose();
        mTransparentResIndicesCountBuff.Dispose();
        mCorrespondingEntities.Dispose();
    }

    private unsafe bool CreateVoxelLoadingJob(int layer, int idx, int buffIdx, MemoryMap map, out LoadVerticesJob outJob)
    {
        var dimensions = map.Dimensions;
        int offset = 0;
        for (int i = 0; i < layer; i++)
            offset += dimensions[i].x * dimensions[i].y * dimensions[i].z;

        var begin = map.Contains[offset + idx].Begin;
        var end = map.Contains[offset + idx].End;
        var objectCount = end - begin;

        if (objectCount == 0)
        {
            outJob = new LoadVerticesJob();
            return false;
        }

        var vertexRangeBegin = map.VerticesInfo[begin].Vertex.Begin;
        var vertexRangeEnd = map.VerticesInfo[end - 1].Vertex.End;
        var vertexCount = vertexRangeEnd - vertexRangeBegin;
        Debug.Assert(vertexCount > 0, "0 vertexCount");

        mOpaqueMeshes[buffIdx].SetVertexCount(vertexCount);
        mTransparentMeshes[buffIdx].SetVertexCount(vertexCount);

        var indexRangeBegin = map.VerticesInfo[begin].Indices.Begin;
        var indexRangeEnd = map.VerticesInfo[end - 1].Indices.End;
        var indexCount = indexRangeEnd - indexRangeBegin;

        mOpaqueMeshes[buffIdx].SetIndexCount(indexCount);
        mTransparentMeshes[buffIdx].SetIndexCount(indexCount);

        mOpaqueMeshes[buffIdx].GetArrays(out Vector3[] opaqueVertices, out Vector3[] opaqueNormals,
                                         out Color32[] opaqueColors, out int[] opaqueIndices);

        mOpaqueGCHandles[buffIdx] = new MeshGCHandles(opaqueVertices, opaqueNormals, opaqueColors, opaqueIndices);

        mTransparentMeshes[buffIdx].GetArrays(out Vector3[] transparentVertices, out Vector3[] transparentNormals,
                                              out Color32[] transparentColors, out int[] transparentIndices);

        mTransparentGCHandles[buffIdx] = new MeshGCHandles(transparentVertices, transparentNormals, transparentColors, transparentIndices);

        var job = new LoadVerticesJob
        {
            OpaqueVerticesBuffer = (Vector3*)mOpaqueGCHandles[buffIdx].VerticesHandle.AddrOfPinnedObject(),
            OpaqueNormalsBuffer = (Vector3*)mOpaqueGCHandles[buffIdx].NormalsHandle.AddrOfPinnedObject(),
            OpaqueColorsBuffer = (Color32*)mOpaqueGCHandles[buffIdx].ColorsHandle.AddrOfPinnedObject(),
            OpaqueIndicesBuffer = (int*)mOpaqueGCHandles[buffIdx].IndicesHandle.AddrOfPinnedObject(),

            OpaqueResVerticesCount = (int*)mOpaqueResVerticesCountBuff.GetUnsafePtr(),
            OpaqueResIndicesCount = (int*)mOpaqueResIndicesCountBuff.GetUnsafePtr(),

            TransparentVerticesBuffer = (Vector3*)mTransparentGCHandles[buffIdx].VerticesHandle.AddrOfPinnedObject(),
            TransparentNormalsBuffer = (Vector3*)mTransparentGCHandles[buffIdx].NormalsHandle.AddrOfPinnedObject(),
            TransparentColorsBuffer = (Color32*)mTransparentGCHandles[buffIdx].ColorsHandle.AddrOfPinnedObject(),
            TransparentIndicesBuffer = (int*)mTransparentGCHandles[buffIdx].IndicesHandle.AddrOfPinnedObject(),

            TransparentResVerticesCount = (int*)mTransparentResVerticesCountBuff.GetUnsafePtr(),
            TransparentResIndicesCount = (int*)mTransparentResIndicesCountBuff.GetUnsafePtr(),

            Vertices = map.Vertices,
            Normals = map.Normals,
            Colors = map.Colors,
            Indices = map.Indices,
            Infos = &map.VerticesInfo[begin],
            InfosCount = objectCount,
            BuffIdx = buffIdx,

            // Need to find a cleaner way of dealing with this.
            Layer = layer,
            Index = idx
        };

        outJob = job;
        return true;
    }

    protected override unsafe void OnUpdate()
    {
        if (!mFITLoaded)
        {
            MemoryMap map;
            {
                var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
                map = tmp[0];
                tmp.Dispose();
            }
            mFITLoaded = true;
            LoadFITLayer(map);
        }

        if (mState == State.Preparing)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Preparing for job creation");
            mStartStreamTime = Time.realtimeSinceStartup;
            var ids = mStreamingRequestQuery.ToEntityArray(Allocator.TempJob);
            var requests = mStreamingRequestQuery.ToComponentDataArray<StreamedInVoxel>(Allocator.TempJob);
            PostUpdateCommands.RemoveComponent(mStreamingRequestQuery, typeof(GFXRequest));


            for (int i = mOpaqueMeshes.Count; i < requests.Length; i++)
            {
                mOpaqueMeshes.Add(new SoAMesh(1, 1));
                mTransparentMeshes.Add(new SoAMesh(1, 1));
                mOpaqueGCHandles.Add(new MeshGCHandles());
                mTransparentGCHandles.Add(new MeshGCHandles());
            }

            mOpaqueResVerticesCountBuff.ResizeUninitialized(requests.Length);
            mOpaqueResIndicesCountBuff.ResizeUninitialized(requests.Length);
            mTransparentResVerticesCountBuff.ResizeUninitialized(requests.Length);
            mTransparentResIndicesCountBuff.ResizeUninitialized(requests.Length);

            mVerticesJobs.Clear();
            mVerticesJobsHandles.Clear();
            mCorrespondingEntities.Clear();

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Creating Jobs");
            MemoryMap map;
            {
                var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
                map = tmp[0];
                tmp.Dispose();
            }

            mBeginSSDLoadTime = Time.realtimeSinceStartup;
            int buffIdx = 0;
            for (int i = 0; i < requests.Length; i++)
            {
                if (CreateVoxelLoadingJob(requests[i].Value.Layer, requests[i].Value.Index, buffIdx, map, out LoadVerticesJob job))
                {
                    mVerticesJobs.Add(job);
                    mVerticesJobsHandles.Add(mVerticesJobs[mVerticesJobs.Length - 1].Schedule());
                    mCorrespondingEntities.Add(ids[i]);
                    buffIdx++;
                }
            }
            mCombinedVerticesJobHandle = JobHandle.CombineDependencies(mVerticesJobsHandles.AsArray());

            if (mVerticesJobs.Length > 0)
            {
                mState = State.LoadingVertices;
                World.Active.GetExistingSystem<VoxelStreamRequesterSystem>().NotifyBusy();
            }

            requests.Dispose();
            ids.Dispose();

            UnityEngine.Profiling.Profiler.EndSample();
        }
        else if (mState == State.LoadingVertices && mCombinedVerticesJobHandle.IsCompleted)
        {
            mBytesLoaded = 0;

            UnityEngine.Profiling.Profiler.BeginSample("Complete Jobs");
            mCombinedVerticesJobHandle.Complete();

            for (int i = 0; i < mVerticesJobs.Length; i++)
            {
                var job = mVerticesJobs[i];
                mOpaqueMeshes[job.BuffIdx].SetVertexCount(job.OpaqueResVerticesCount[job.BuffIdx]);
                mOpaqueMeshes[job.BuffIdx].SetIndexCount(job.OpaqueResIndicesCount[job.BuffIdx]);
                mBytesLoaded += (job.TransparentResVerticesCount[job.BuffIdx] + job.OpaqueResVerticesCount[job.BuffIdx]) * (sizeof(Vector3) + sizeof(Vector3) + sizeof(Color32));

                mTransparentMeshes[job.BuffIdx].SetVertexCount(job.TransparentResVerticesCount[job.BuffIdx]);
                mTransparentMeshes[job.BuffIdx].SetIndexCount(job.TransparentResIndicesCount[job.BuffIdx]);
                mBytesLoaded += (job.TransparentResIndicesCount[job.BuffIdx] + job.OpaqueResIndicesCount[job.BuffIdx]) * sizeof(int);

                mTransparentGCHandles[job.BuffIdx].Dispose();
                mOpaqueGCHandles[job.BuffIdx].Dispose();
            }

            Debug.Log($"{mBytesLoaded / Math.Pow(10, 6)} MB, loaded from disk: {(Time.realtimeSinceStartup - mBeginSSDLoadTime) * 1000.0}ms");

            mState = State.InstantiatingMeshes;
            mInstantiateCount = 0;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        else if (mState == State.InstantiatingMeshes)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Instantiate Mesh");
            var job = mVerticesJobs[mInstantiateCount];
            var owner = mCorrespondingEntities[mInstantiateCount];

            var gfxMeshes = PostUpdateCommands.AddBuffer<GFXMeshes>(owner);

            // One for transparent, one for opaque
            gfxMeshes.Reserve(2);

            UnityEngine.Profiling.Profiler.BeginSample("Create Go");
            if (job.OpaqueResVerticesCount[job.BuffIdx] != 0)
            {
                var go = CreateGameObject($"Layer {job.Layer}, idx {job.Index}, Opaque",
                          mOpaqueMeshes[job.BuffIdx],
                          Globals.Instance.data.OpaqueMaterial);

                var e = EntityManager.CreateEntity();
                EntityManager.AddComponentObject(e, go);
                PostUpdateCommands.AddComponent(e, new Unity.Transforms.Frozen());
                gfxMeshes.Add(new GFXMeshes { Value = e });
            }
            if (job.TransparentResVerticesCount[job.BuffIdx] != 0)
            {
                var go = CreateGameObject($"Layer {job.Layer}, idx {job.Index}, Transparent",
                                          mTransparentMeshes[job.BuffIdx],
                                          Globals.Instance.data.TransparentMaterial);

                var e = EntityManager.CreateEntity();
                EntityManager.AddComponentObject(e, go);
                PostUpdateCommands.AddComponent(e, new Unity.Transforms.Frozen());
                gfxMeshes.Add(new GFXMeshes { Value = e });
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (++mInstantiateCount >= mVerticesJobs.Length)
            {
                Debug.Log($"{mBytesLoaded / Math.Pow(10, 6)} MB, loaded and instantiated in: {(Time.realtimeSinceStartup - mStartStreamTime) * 1000.0}ms");
                mState = State.Preparing;
                World.Active.GetExistingSystem<VoxelStreamRequesterSystem>().NotifyReady();
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    private GameObject CreateGameObject(string name, SoAMesh mesh, Material material)
    {
        var go = MeshGoPool.CreateObject();
        go.name = name;
        var filter = go.GetComponent<MeshFilter>();
        filter.mesh.Clear();
        filter.mesh.MarkDynamic();

        filter.mesh.indexFormat = (mesh.IndexCount >= UInt16.MaxValue)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        filter.mesh.SetVertices(mesh.Vertices);
        filter.mesh.SetNormals(mesh.Normals);
        filter.mesh.SetColors(mesh.Colors);
        filter.mesh.SetTriangles(mesh.Indices, 0);

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.material = material;
        return go;
    }

    [BurstCompile]
    unsafe private struct LoadVerticesJob : IJob
    {
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* OpaqueVerticesBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* OpaqueNormalsBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Color32* OpaqueColorsBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* OpaqueIndicesBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* OpaqueResVerticesCount;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* OpaqueResIndicesCount;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* TransparentVerticesBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* TransparentNormalsBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public Color32* TransparentColorsBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* TransparentIndicesBuffer;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* TransparentResVerticesCount;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public int* TransparentResIndicesCount;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* Vertices;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public Vector3* Normals;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public Color32* Colors;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public int* Indices;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public VertexInfo* Infos;

        public int InfosCount;
        public int BuffIdx;

        public int Layer;
        public int Index;

        public void Execute()
        {
            int opaqueCurrVertices = 0;
            int opaqueCurrIndices = 0;
            int transparentCurrVertices = 0;
            int transparentCurrIndices = 0;
            for (int i = 0; i < InfosCount; i++)
            {
                var vertexCount = Infos[i].Vertex.End - Infos[i].Vertex.Begin;
                var indicesCount = Infos[i].Indices.End - Infos[i].Indices.Begin;

                var colors = &Colors[Infos[i].Vertex.Begin];

                bool isTransparent = false;
                for (int j = 0; j < vertexCount; j++)
                {
                    if (colors[j].a != 0 && colors[j].a < 150)
                    {
                        isTransparent = true;
                        break;
                    }
                }

                if (isTransparent)
                {
                    UnsafeUtility.MemCpy(&TransparentVerticesBuffer[transparentCurrVertices], &Vertices[Infos[i].Vertex.Begin], vertexCount * sizeof(Vector3));
                    UnsafeUtility.MemCpy(&TransparentNormalsBuffer[transparentCurrVertices], &Normals[Infos[i].Vertex.Begin], vertexCount * sizeof(Vector3));
                    UnsafeUtility.MemCpy(&TransparentColorsBuffer[transparentCurrVertices], &Colors[Infos[i].Vertex.Begin], vertexCount * sizeof(Color32));
                    UnsafeUtility.MemCpy(&TransparentIndicesBuffer[transparentCurrIndices], &Indices[Infos[i].Indices.Begin], indicesCount * sizeof(int));

                    for (int j = transparentCurrIndices; j < transparentCurrIndices + indicesCount; j++)
                    {
                        TransparentIndicesBuffer[j] += transparentCurrVertices;
                    }
                    transparentCurrVertices += vertexCount;
                    transparentCurrIndices += indicesCount;

                }
                else
                {
                    UnsafeUtility.MemCpy(&OpaqueVerticesBuffer[opaqueCurrVertices], &Vertices[Infos[i].Vertex.Begin], vertexCount * sizeof(Vector3));
                    UnsafeUtility.MemCpy(&OpaqueNormalsBuffer[opaqueCurrVertices], &Normals[Infos[i].Vertex.Begin], vertexCount * sizeof(Vector3));
                    UnsafeUtility.MemCpy(&OpaqueColorsBuffer[opaqueCurrVertices], &Colors[Infos[i].Vertex.Begin], vertexCount * sizeof(Color32));
                    UnsafeUtility.MemCpy(&OpaqueIndicesBuffer[opaqueCurrIndices], &Indices[Infos[i].Indices.Begin], indicesCount * sizeof(int));

                    for (int j = opaqueCurrIndices; j < opaqueCurrIndices + indicesCount; j++)
                    {
                        OpaqueIndicesBuffer[j] += opaqueCurrVertices;
                    }
                    opaqueCurrVertices += vertexCount;
                    opaqueCurrIndices += indicesCount;
                }
            }
            OpaqueResVerticesCount[BuffIdx] = opaqueCurrVertices;
            OpaqueResIndicesCount[BuffIdx] = opaqueCurrIndices;
            TransparentResVerticesCount[BuffIdx] = transparentCurrVertices;
            TransparentResIndicesCount[BuffIdx] = transparentCurrIndices;
        }
    }
}
