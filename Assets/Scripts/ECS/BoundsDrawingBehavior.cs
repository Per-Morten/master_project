using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

// Technically a mono behavior, but acts as a system. Just so I can use Handles, which you need to do in DrawGizmos.
public class BoundsDrawingBehavior : MonoBehaviour
{
    private List<Entity> mBoundsEntities = new List<Entity>();

    public void OnDrawGizmos()
    {
        if (World.Active == null || !Globals.Instance.data.DrawBounds)
            return;

        DrawSelectedLayer();
    }

    private void DrawSelectedLayer()
    {
        var layers = World.Active.EntityManager.GetBuffer<Layers>(mBoundsEntities[0]);
        var colors = World.Active.EntityManager.GetBuffer<Colors>(mBoundsEntities[0]);
        var idx = System.Math.Max(0, System.Math.Min(Globals.Instance.data.BoundsLayer, layers.Length - 1));
        var voxels = World.Active.EntityManager.GetBuffer<VoxelAABB>(layers[idx].Value);
        for (int j = 0; j < voxels.Length; j++)
        {
            BoundsDrawer.Draw(voxels[j].Value, colors[idx].Value);
        }
    }

    private void DrawOutermostLayers()
    {
        var layers = World.Active.EntityManager.GetBuffer<Layers>(mBoundsEntities[0]);
        var colors = World.Active.EntityManager.GetBuffer<Colors>(mBoundsEntities[0]);
        var outer = World.Active.EntityManager.GetBuffer<VoxelAABB>(layers[1].Value).Reinterpret<Unity.Mathematics.AABB>();
        var inner = World.Active.EntityManager.GetBuffer<VoxelAABB>(layers[2].Value).Reinterpret<Unity.Mathematics.AABB>();

        for (int i = 0; i < inner.Length; i++)
        {
            BoundsDrawer.Draw(inner[i], colors[1].Value);
            //Handles.Label(inner[i].Center, $"{i}");
        }

        for (int i = 0; i < outer.Length; i++)
        {
            var pos = outer[i].Center;
            pos.x -= outer[i].Extents.x;
            pos.y -= outer[i].Extents.y;
            pos.z -= outer[i].Extents.z;
            BoundsDrawer.Draw(outer[i], colors[2].Value);
            //Handles.Label(pos, $"{i}");
        }
    }

    public void Update()
    {
        if (mBoundsEntities.Count == 0)
        {
            var entities = World.Active.EntityManager.GetAllEntities();
            for (int i = 0; i < entities.Length; i++)
                if (World.Active.EntityManager.HasComponent<Layers>(entities[i]))
                    mBoundsEntities.Add(entities[i]);
        }
    }
}
