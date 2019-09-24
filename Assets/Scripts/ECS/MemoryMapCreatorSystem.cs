using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

public class MemoryMapCreatorSystem : ComponentSystem
{
    private string mFolderPath = @"\StreamingFiles\";
    private List<MemoryMappedFile> mFiles = new List<MemoryMappedFile>();
    private List<MemoryMappedViewAccessor> mAccessors = new List<MemoryMappedViewAccessor>();

    private Entity mMemoryMapEntity;

    unsafe protected override void OnCreate()
    {
        mFolderPath = Application.dataPath + "/../StreamingFiles/";
        // Setup Memory map
        try
        {
            mFiles.Add(MemoryMappedFile.CreateFromFile(mFolderPath + Globals.Instance.data.StreamFilename, FileMode.Open));
            mAccessors.Add(mFiles[0].CreateViewAccessor());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception encountered: {ex.Message}, on file {mFolderPath + Globals.Instance.data.StreamFilename}");
            return;
        }

        var basePtr = (byte*)0;
        mAccessors[0].SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);

        var map = mAccessors[0];

        long headerPtr = 128;
        var layers = map.ReadInt32(headerPtr);
        headerPtr += sizeof(int);
        var voxelSizes = (Vector3*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 0));
        var dimensions = (int3*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 1));
        var boundsCenters = (Vector3*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 2));
        var contains = (Range*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 3));
        var verticesInfo = (VertexInfo*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 4));
        var vertices = (Vector3*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 5));
        var normals = (Vector3*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 6));
        var colors = (Color32*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 7));
        var indices = (int*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 8));
        var names = (byte*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 9));
        var blobInfos = (Range*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 10));
        var colliderBlobs = (byte*)(basePtr + map.ReadInt64(headerPtr + sizeof(long) * 11));

        var m = new MemoryMap
        {
            Layers = layers,
            mVoxelSizes = voxelSizes,
            mDimensions = dimensions,
            mBoundsCenters = boundsCenters,
            mColors = colors,
            mContains = contains,
            mVertices = vertices,
            mVerticesInfo = verticesInfo,
            Indices = indices,
            mNormals = normals,
            mNames = names,
            mColliderBlobInfo = blobInfos,
            mColliderBlobs = colliderBlobs,
        };
        LoadBoundingBoxes(m);

        mMemoryMapEntity = EntityManager.CreateEntity(typeof(MemoryMap));
        EntityManager.SetComponentData(mMemoryMapEntity, m);
    }

    protected override void OnDestroy()
    {
        foreach (var map in mAccessors)
            map.SafeMemoryMappedViewHandle.ReleasePointer();

        foreach (var file in mFiles)
            file.Dispose();
    }

    private unsafe void LoadBoundingBoxes(MemoryMap m)
    {
        var defaultColors = new Color[4] { Color.red, Color.green, Color.magenta, Color.blue };

        var buffer = new EntityCommandBuffer(Allocator.TempJob);
        var grid = buffer.CreateEntity();
        var layers = buffer.AddBuffer<Layers>(grid);
        var colors = buffer.AddBuffer<Colors>(grid);
        var dimensions = buffer.AddBuffer<Dimensions>(grid);
        layers.Reserve(m.Layers);
        colors.Reserve(m.Layers);
        dimensions.Reserve(m.Layers);
        var ptr = m.BoundsCenters;
        var dim = m.Dimensions;

        for (int i = 0; i < m.Layers; i++)
        {
            var e = buffer.CreateEntity();
            layers.Add(new Layers { Value = e });
            dimensions.Add(new Dimensions { Value = ((int3*)m.mDimensions)[i] });
            colors.Add(new Colors { Value = defaultColors[i % defaultColors.Length] });
            var voxels = buffer.AddBuffer<VoxelAABB>(e);

            int size = m.Dimensions[i].z * m.Dimensions[i].y * m.Dimensions[i].x;
            voxels.Reserve(size);

            for (long j = 0; j < size; j++, ptr++)
            {
                voxels.Add(new VoxelAABB { Value = new AABB { Center = *ptr, Extents = m.VoxelSizes[i] / 2 } });
            }
        }

        buffer.Playback(EntityManager);
        buffer.Dispose();
    }

    protected override void OnUpdate()
    {

    }
}
