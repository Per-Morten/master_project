using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRSelect : MonoBehaviour
{
    public Valve.VR.SteamVR_Action_Single Action;

    public Valve.VR.InteractionSystem.Hand Hand;

    public GameObject LaserPointer;
    public MeshRenderer Renderer;
    public Material DefaultMaterial;
    public Material SelectedMaterial;

    private float mTimeSinceLast;

    private void OnEnable()
    {
        if (Hand == null)
            Hand = GetComponent<Valve.VR.InteractionSystem.Hand>();

        Debug.Assert(Action != null, "No action assigned");

        Action.AddOnUpdateListener(OnSingleUpdate, Hand.handType);
    }

    private void OnDisable()
    {
        if (Action != null)
            Action.RemoveOnUpdateListener(OnSingleUpdate, Hand.handType);
    }


    private void OnSingleUpdate(Valve.VR.SteamVR_Action_Single from, Valve.VR.SteamVR_Input_Sources inputSource, float axis, float delta)
    {
        mTimeSinceLast -= Time.deltaTime;
        Debug.DrawRay(Hand.transform.gameObject.transform.position, Hand.transform.gameObject.transform.forward, Color.blue);

        if (axis >= 0.5f && mTimeSinceLast <= 0.0f)
        {
            Renderer.material = SelectedMaterial;
            var e = Unity.Entities.World.Active.EntityManager.CreateEntity();
            Unity.Entities.World.Active.EntityManager.AddComponentData(e, new RayRequest { Value = new Unity.Physics.Ray { Origin = Hand.transform.position, Displacement = Hand.transform.position + Hand.transform.forward * 100.0f } });
            mTimeSinceLast = 0.5f;
        }
        else
        {
            Renderer.material = DefaultMaterial;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Globals.Instance.data.MeshParent.SetActive(!Globals.Instance.data.MeshParent.activeSelf);
            Globals.Instance.data.StaticParent.SetActive(!Globals.Instance.data.StaticParent.activeSelf);
        }
    }
}
