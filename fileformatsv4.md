# Data-Fileformat:

## Header (128 bytes)
<reserved> (128 bytes)
<number-of-layers> (int32)
## LayerInfo (128 bytes)
<sizes-start-address> (int64) 
<dimensions-start-address> (int64) 
<bounds-start-address> (int64) 
<objectsinbounds-start-address> (int64) 
<verticesinfo-start-address> (int64) 
<vertices-start-address> (int64) 
<normals-start-address> (int64) 
<colors-start-address> (int64) 
<indices-start-address> (int64) 
<names-start-address> (int64)
<colliderblobinfo-start-address> (int64)
<colliderblobs> (int64)
<reserved (28 bytes)>

## Sizes (fmt: Vector3)
<size-of-bounds-l0>
<size-of-bounds-l1>
<size-of-bounds-l2>
...
<size-of-bounds-lN>

## Dimensions (fmt: Vector3Int)
<columns (pos x), rows (pos y), depth (pos z)> // This is levelFIT
<columns (pos x), rows (pos y), depth (pos z)> 
<columns (pos x), rows (pos y), depth (pos z)>
...
<size-of-bounds-lN><columns (pos x), rows (pos y), depth (pos z)> // N = 4 currently.

## Bounds (fmt: center Vector3)
<l0-bounds-d0-r0-c0_Bounds><l0-bounds-d0-r0-c1_Bounds><l0-bounds-d0-r0-c2_Bounds>...<l0-bounds-d0-r1-c0_Bounds><l0-bounds-d0-r1-c1_Bounds>...<l0-bounds-d1-r0-c0_Bounds><l0-bounds-d1-r0-c1_Bounds>...<l0-bounds-dD-rR-cC_Bounds>
...
<lN-bounds-d0-r0-c0_Bounds><lN-bounds-d0-r0-c1_Bounds><lN-bounds-d0-r0-c2_Bounds>...<lN-bounds-d0-r1-c0_Bounds><lN-bounds-d0-r1-c1_Bounds>...<lN-bounds-d1-r0-c0_Bounds><lN-bounds-d1-r0-c1_Bounds>...<lN-bounds-dD-rR-cC_Bounds>

## Objects in bounds location (fmt: objects start (int32), objects end (int32)) // objects start = Index to where to find VerticesInfo & Names of each object within bounds (0 based, as if VerticesInfo were an array)
<l0-bounds-d0-r0-c0_ObjectStart><l0-bounds-d0-r0-c1_ObjectStart><l0-bounds-d0-r0-c2_ObjectStart>...<l0-bounds-d0-r1-c0_ObjectStart><l0-bounds-d0-r1-c1_ObjectStart>...<l0-bounds-d1-r0-c0_ObjectStart><l0-bounds-d1-r0-c1_ObjectStart>...<l0-bounds-dD-rR-cC_ObjectStart>
...
<lN-bounds-d0-r0-c0_ObjectStart><lN-bounds-d0-r0-c1_ObjectStart><lN-bounds-d0-r0-c2_ObjectStart>...<lN-bounds-d0-r1-c0_ObjectStart><lN-bounds-d0-r1-c1_ObjectStart>...<lN-bounds-d1-r0-c0_ObjectStart><lN-bounds-d1-r0-c1_ObjectStart>...<lN-bounds-dD-rR-cC_ObjectStart>

## VerticesInfo (fmt: vertex begin (int32), vertex end (int32), index begin (int32), index end (int32) // Vertex start is a ptr/index hybrid. Vertex start is first byte of vertices.positions. (However, it is 0 based with vertices[0].positions having vertex start of 0)
<l0-bounds-d0-r0-c0_Object0><l0-bounds-d0-r0-c0_Object1>...<l0-bounds-d0-r0-c0_ObjectM><l0-bounds-d0-r0-c1_Object0><l0-bounds-d0-r0-c1_Object1><l0-bounds-d0-r0-c1_Object2>...<l0-bounds-d0-r0-c0_ObjectM>
...
<lN-bounds-dD-rR-cR_Object0><lN-bounds-dD-rR-cC_Object1>...<lN-bounds-dD-rR-cC_ObjectM>

// All follow the same format!
## Vertices (fmt: positions (Vector3[]))
## Normals (fmt: normals (Vector3[]))
## Colors (32-bit RGBA[]), 
## Indices (int32[]))
<l0-bounds-d0-r0-c0_VerticesInfo0><l0-bounds-d0-r0-c0_VerticesInfo1>...<l0-bounds-d0-r0-c0_VerticesInfoM><l0-bounds-d0-r0-c1_VerticesInfo0><l0-bounds-d0-r0-c1_VerticesInfo1><l0-bounds-d0-r0-c1_VerticesInfo2>...<l0-bounds-d0-r0-c0_VerticesInfoM>
...
<lN-bounds-dD-rR-cR_VerticesInfo0><lN-bounds-dD-rR-cC_VerticesInfo1>...<lN-bounds-dD-rR-cC_VerticesInfoM>

## Names (fmt: char[22])
<l0-bounds-d0-r0-c0_Name0><l0-bounds-d0-r0-c0_Name1>...<l0-bounds-d0-r0-c0_NameM><l0-bounds-d0-r0-c1_Name0><l0-bounds-d0-r0-c1_Name1><l0-bounds-d0-r0-c1_Name2>...<l0-bounds-d0-r0-c0_NameM>
...
<lN-bounds-dD-rR-cR_Name0><lN-bounds-dD-rR-cC_Name1>...<lN-bounds-dD-rR-cC_NameM>

## ColliderBlobInfo (fmt: Collider begin (int32), Collider end (int32))
### Same layout as VerticesInfo

## ColliderBlobs
### Same layout as Vertices, just memcpy it in, or just give the ptr to the blob.
