using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class MeshColliderHelper
{
    public static unsafe BlobAssetReference<Collider> Create(float3* vertices, int* indices, int numVertices, int numIndices, CollisionFilter? filter = null, Material? material = null)
    {
        var v = new NativeArray<float3>(numVertices, Allocator.Temp);
        var vptr = NativeArrayUnsafeUtility.GetUnsafePtr(v);
        UnsafeUtility.MemCpy(vptr, vertices, numVertices * sizeof(float3));

        var i = new NativeArray<int>(numIndices, Allocator.Temp);
        var iptr = NativeArrayUnsafeUtility.GetUnsafePtr(i);
        UnsafeUtility.MemCpy(iptr, indices, numIndices * sizeof(int));

        var collider = MeshCollider.Create(v, i);
        v.Dispose();
        i.Dispose();
        return collider;
    }
}
