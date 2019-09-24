using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public class BenchmarkSystem : ComponentSystem
{
    private int mSceneIdx = 0;
    private int mCurrentFrame = 0;
    private Vector3 mAwayPosition = new Vector3(-1000, -1000, -1000);
    private int mIdx = 0;

    // Vector order:
    // Outer idx = Scene
    // Inner idx = Closests, Largest, Closest to AVG
    private Vector3[][] mPositions = new Vector3[][]
    {
        new Vector3[] // Westside2Large
        {
            new Vector3(-57.1f, 96.4f, -3.9f),
            new Vector3(-21.1f, 72.4f, 8.1f),
            new Vector3(38.9f, 84.4f, -3.9f),
        }
    };

    protected override void OnCreate()
    {
        // Get Scene Name
        if (Globals.Instance.data.StreamFilename.Contains("Westside2"))
            mSceneIdx = 0;

        Debug.Log($"{mPositions[mSceneIdx][0]}, {mPositions[mSceneIdx][1]}, {mPositions[mSceneIdx][2]}");
        // Randomize update order
    }

    protected override void OnUpdate()
    {
        //if (mCurrentFrame >= 5)
        //{
        //    // TODO: Need to introduce randomness in the update order.
        //    // However, while random, I want each position to be loaded an equal amount of times
        //    // And no item should directly repeat.

        //    Entities.WithAll<PlayerTag>().ForEach((Entity e, ref Translation t) =>
        //    {
        //        var transform = EntityManager.GetComponentObject<UnityEngine.Transform>(e);

        //        if (mCurrentFrame % 50 == 0)
        //        {
        //            t.Value = mPositions[mSceneIdx][mIdx];
        //            mIdx = (mIdx + 1) % mPositions[mSceneIdx].Length;
        //        }
        //        //else if (mCurrentFrame % 3 == 0)
        //        //{
        //        //    t.Value = mPositions[mSceneIdx][1];
        //        //}
        //        //else if (mCurrentFrame % 7 == 0)
        //        //{
        //        //    t.Value = mPositions[mSceneIdx][2];
        //        //}
        //        else
        //        {
        //            t.Value = mAwayPosition;
        //        }

        //        if (t.Value.x != -1000)
        //            Debug.Log($"{t.Value}");

        //        transform.position = t.Value;
        //    });
        //}

        //if (mCurrentFrame >= 301 && mCurrentFrame < 302)
        //    Debug.Break();

        Entities.WithAll<PlayerTag>().ForEach((Entity e, ref Translation t) =>
        {
            var transform = EntityManager.GetComponentObject<UnityEngine.Transform>(e);

            if (mCurrentFrame == 50)
            {
                t.Value = mPositions[mSceneIdx][2];
                //mIdx = (mIdx + 1) % mPositions[mSceneIdx].Length;
                transform.position = t.Value;
            }
            //else if (mCurrentFrame % 3 == 0)
            //{
            //    t.Value = mPositions[mSceneIdx][1];
            //}
            //else if (mCurrentFrame % 7 == 0)
            //{
            //    t.Value = mPositions[mSceneIdx][2];
            //}
            //else
            //{
            //    t.Value = mAwayPosition;
            //}

            //if (t.Value.x != -1000)
            //    Debug.Log($"{t.Value}");

        });

        mCurrentFrame++;
    }
}
