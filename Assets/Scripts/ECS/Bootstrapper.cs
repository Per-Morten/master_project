using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void IntitializeAfterScene()
    {
        if (SceneManager.GetActiveScene().name.Contains("ECS"))
        {
            var player = World.Active.EntityManager.CreateEntity();
            World.Active.EntityManager.AddComponentData(player, new PlayerTag { });
            World.Active.EntityManager.AddComponentData(player, new Unity.Transforms.Translation { Value = Unity.Mathematics.float3.zero });

            if (!SceneManager.GetActiveScene().name.Contains("VR"))
            {
                var go = GameObject.Find("Player");
                GameObjectEntity.AddToEntity(World.Active.EntityManager, go, player);
                Camera.main.transform.localPosition = go.transform.position;
                Camera.main.transform.parent = go.transform;
            }

            SetupPool();
        }
    }

    public static void SetupPool()
    {

        {
            Pool mMeshGOPool;
            var mIFCObjectParent = new GameObject("IFCVoxelRepresentations");
            Globals.Instance.data.MeshParent = mIFCObjectParent;
            var tmp = new GameObject("VoxelRep");
            tmp.transform.parent = mIFCObjectParent.transform;
            var filter = tmp.AddComponent<MeshFilter>();
            filter.mesh.MarkDynamic();
            var renderer = tmp.AddComponent<MeshRenderer>();
            renderer.material = Globals.Instance.data.OpaqueMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            mMeshGOPool = new Pool(tmp, 4096);
            GameObject.Destroy(tmp);

            var parent = new GameObject("StaticGFXParent");
            Globals.Instance.data.StaticParent = parent;

            World.Active.GetOrCreateSystem<VoxelRemovalSystem>().MeshGoPool = mMeshGOPool;
            World.Active.GetOrCreateSystem<MeshStreamerSystem>().MeshGoPool = mMeshGOPool;
        }

        Debug.Log($"Refresh Rate: {UnityEngine.XR.XRDevice.refreshRate}");

        var debugDisplayData = World.Active.EntityManager.CreateEntity(ComponentType.ReadOnly<Unity.Physics.Authoring.PhysicsDebugDisplayData>());
        var debugDisplayConfig = new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawColliders = 1, //1,
            DrawBroadphase = 0,
            DrawColliderAabbs = 0,
            DrawColliderEdges = 1,
            DrawCollisionEvents = 0,
            DrawContacts = 0,
            DrawJoints = 0,
            DrawMassProperties = 0,
            DrawTriggerEvents = 0,
        };
        //World.Active.GetOrCreateSystem<Unity.Physics.Authoring.DisplayBodyColliders>().SetSingleton(debugDisplayConfig);
    }
}
