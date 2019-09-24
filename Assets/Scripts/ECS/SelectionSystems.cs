using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

unsafe public class SelectionSystem : ComponentSystem
{
    private EntityQuery mSelectionQuery;
    private EntityQuery mMemoryMapQuery;

    protected override void OnCreate()
    {
        mSelectionQuery = GetEntityQuery(
            new EntityQueryDesc
            {
                None = new ComponentType[] { typeof(GameObject) },
                All = new ComponentType[] { ComponentType.ReadOnly<IFCGuidUIPosition>(), ComponentType.ReadOnly<IFCObjectID>(), ComponentType.ReadOnly<IFCGuid>() }
            });

        mMemoryMapQuery = GetEntityQuery(ComponentType.ReadOnly<MemoryMap>());
    }

    protected override void OnUpdate()
    {
        MemoryMap map;
        {
            var tmp = mMemoryMapQuery.ToComponentDataArray<MemoryMap>(Allocator.TempJob);
            map = tmp[0];
            tmp.Dispose();
        }

        Entities.With(mSelectionQuery).ForEach((Entity e, ref IFCObjectID obj, ref IFCGuid guid, ref IFCGuidUIPosition tag) =>
        {
            var stringGuid = guid.AsString();
            var go = CreateMesh(guid.AsString(), obj, map);
            var ui = CreateIFCGuidUI(stringGuid, tag.Value - (math.normalize(tag.Value - (float3)Camera.main.transform.position)));
            ui.transform.SetParent(go.transform, false);
            
            EntityManager.AddComponentObject(e, go);
        });
    }

    private static GameObject CreateMesh(string ifcGuid, IFCObjectID ifcObjectData, MemoryMap map)
    {
        var dimensions = map.Dimensions;
        int offset = 0;
        for (int i = 0; i < ifcObjectData.Voxel.Layer; i++)
            offset += dimensions[i].x * dimensions[i].y * dimensions[i].z;

        var begin = map.Contains[offset + ifcObjectData.Voxel.Index].Begin;

        var vertexInfo = map.VerticesInfo[begin + ifcObjectData.Index].Vertex;
        var vertexCount = vertexInfo.End - vertexInfo.Begin;
        var indexInfo = map.VerticesInfo[begin + ifcObjectData.Index].Indices;
        var indexCount = indexInfo.End - indexInfo.Begin;
        Debug.Assert(indexCount > 0);

        var vertices = new List<Vector3>(vertexCount);
        NoAllocHelpers.ResizeList(vertices, vertexCount);
        fixed (Vector3* p = NoAllocHelpers.ExtractArrayFromListT(vertices))
        {
            UnsafeUtility.MemCpy(p, &map.Vertices[vertexInfo.Begin], vertexCount * sizeof(Vector3));
        }

        var normals = new List<Vector3>(vertexCount);
        NoAllocHelpers.ResizeList(normals, vertexCount);
        fixed (Vector3* p = NoAllocHelpers.ExtractArrayFromListT(normals))
        {
            UnsafeUtility.MemCpy(p, &map.Normals[vertexInfo.Begin], vertexCount * sizeof(Vector3));
        }

        var colors = new List<Color32>(vertexCount);
        NoAllocHelpers.ResizeList(colors, vertexCount);
        for (int i = 0; i < colors.Count; i++)
            colors[i] = Color.magenta;

        var indices = new List<int>(indexCount);
        NoAllocHelpers.ResizeList(indices, indexCount);
        fixed (int* p = NoAllocHelpers.ExtractArrayFromListT(indices))
        {
            UnsafeUtility.MemCpy(p, &map.Indices[indexInfo.Begin], indexCount * sizeof(int));
        }

        for (int i = 0; i < indices.Count; i++)
        {
            vertices[indices[i]] += normals[indices[i]] * 0.01f;
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetTriangles(indices, 0);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = ifcGuid;
        var filter = go.GetComponent<MeshFilter>();
        filter.mesh = mesh;

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.material = Globals.Instance.data.OpaqueMaterial;

        return go;
    }

    private static GameObject CreateIFCGuidUI(string ifcGuid, float3 position)
    {
        var ui = GameObject.Instantiate(Globals.Instance.data.IFCGuidUIPrefab, position, Quaternion.identity);
        ui.transform.LookAt(Camera.main.transform.parent.position);
        var text = ui.GetComponentInChildren<UnityEngine.UI.Text>();
        text.text = $"IFCGuid: {ifcGuid}";
        return ui;
    }
}

public class DeselectionSystem : ComponentSystem
{
    private EntityQuery mSelectionQuery;

    protected override void OnCreate()
    {
        mSelectionQuery = GetEntityQuery(ComponentType.ReadOnly<DeselectedTag>(), typeof(GameObject));
    }

    protected override void OnUpdate()
    {
        Entities.With(mSelectionQuery).ForEach((Entity e, GameObject obj) =>
        {
            EntityManager.RemoveComponent(e, typeof(GameObject));
            GameObject.Destroy(obj);
            PostUpdateCommands.RemoveComponent(e, typeof(DeselectedTag));
        });
    }
}