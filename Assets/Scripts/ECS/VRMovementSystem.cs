using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class VRMovementSystem : ComponentSystem
{
    private Transform mPlayer;

    protected override void OnUpdate()
    {
        if (mPlayer == null)
            mPlayer = GameObject.Find("Player").transform;

        Entities.WithAll<PlayerTag>().ForEach((Entity e, ref Translation t) =>
        {
            t.Value = mPlayer.position;
        });
    }
}
