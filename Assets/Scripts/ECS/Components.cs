using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


public struct Range
{
    public int Begin;
    public int End;
}

public struct VertexInfo
{
    public Range Vertex;
    public Range Indices;
}

// Memory Map Components
public unsafe struct MemoryMap : IComponentData
{
    public float3* BoundsCenters => (float3*)mBoundsCenters;
    public Vector3* Vertices => (Vector3*)mVertices;
    public Vector3* Normals => (Vector3*)mNormals;
    public Color32* Colors => (Color32*)mColors;
    public float3* VoxelSizes => (float3*)mVoxelSizes;
    public int3* Dimensions => (int3*)mDimensions;
    public VertexInfo* VerticesInfo => (VertexInfo*)mVerticesInfo;
    public Range* Contains => (Range*)mContains;
    public Range* ColliderBlobInfo => (Range*)mColliderBlobInfo;
    public void* ColliderBlobs => mColliderBlobs;

    // Pointers to the different sections in the memory map.
    public int* Indices;
    public int Layers;
    public byte* mNames;

    // Public for convenience, but don't access these, use the properties.
    public void* mContains;
    public void* mVerticesInfo; // Also turning these to void ptrs, as I had some trouble with unknown errors on them.
    public void* mVoxelSizes; // Vector3
    public void* mDimensions; // Vector3
    public void* mBoundsCenters; // Vector3
    public void* mVertices; // Vector3
    public void* mNormals; // Vector3
    public void* mColors; // Color32
    public void* mColliderBlobInfo; // Range
    public void* mColliderBlobs; // Blobs
}

public struct GFXRequest : IComponentData
{

}

public struct ColliderRequest : IComponentData
{

}

public struct RayRequest : IComponentData
{
    public Unity.Physics.Ray Value;
}

public struct PlayerTag : IComponentData
{

}

public struct IFCGuidUIPosition : IComponentData
{
    public float3 Value;
}

public struct DeselectedTag : IComponentData
{

}

unsafe public struct IFCGuid : IComponentData
{
    unsafe public string AsString()
    {
        return System.Text.Encoding.ASCII.GetString(mValue, 22);
    }
    public byte* mValue;
}

// Enough information to get the MemoryMap location of this IFCObject
public struct IFCObjectID : IComponentData
{
    public Voxel Voxel;
    public int Index;
}

public struct Voxel : System.IEquatable<Voxel>, IComponentData
{
    public int Layer;
    public int Index;

    public bool Equals(Voxel other)
    {
        return Layer == other.Layer && Index == other.Index;
    }

    public override int GetHashCode()
    { 
        // Hopefully this should work. Don't think we will ever need more than 8 bit to represent layers
        // TODO: Assert on these values!
        return (Layer << 8) | (Index & (System.Int32.MinValue >> 8));
    }
}

public struct VoxelStreamRequest : IComponentData
{
    public Voxel Value;
}

public struct VoxelRemovalTag : IComponentData
{
}

public struct StreamedInVoxel : IComponentData
{
    public Voxel Value;
}

[InternalBufferCapacity(4)]
public struct GFXMeshes : IBufferElementData
{
    public Entity Value;
}

[InternalBufferCapacity(256)]
public struct ColliderMeshes : IBufferElementData
{
    public Entity Value;
}

public struct Layers : IBufferElementData
{
    public Entity Value;
}

public struct Dimensions : IBufferElementData
{
    public int3 Value;
}

[InternalBufferCapacity(8)]
public struct Colors : IBufferElementData
{
    public static implicit operator Color32(Colors e) { return e.Value; }
    public static implicit operator Colors(Color32 e) { return new Colors { Value = e }; }

    public Color32 Value;
}

public struct VoxelAABB : IBufferElementData
{
    public AABB Value;
}
