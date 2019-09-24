using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMove : MonoBehaviour
{
    public Valve.VR.SteamVR_Action_Vector2 Action;

    public Valve.VR.InteractionSystem.Hand Hand;

    public GameObject TPArea;

    private Transform mPlayer;

    private void OnEnable()
    {
        if (Hand == null)
            Hand = GetComponent<Valve.VR.InteractionSystem.Hand>();

        Debug.Assert(Action != null, "No action assigned");

        mPlayer = GameObject.Find("Player").transform; 

        Action.AddOnUpdateListener(OnVector2Update, Hand.handType);
    }

    private void OnDisable()
    {
        if (Action != null)
            Action.RemoveOnUpdateListener(OnVector2Update, Hand.handType);
    }

    private void OnVector2Update(Valve.VR.SteamVR_Action_Vector2 from, Valve.VR.SteamVR_Input_Sources inputSource, Vector2 axis, Vector2 delta)
    {
        if (Mathf.Abs(axis.y) >= 0.5f)
        {
            var pos = mPlayer.position;
            pos.y += axis.y * Time.deltaTime * 10.0f;
            mPlayer.position = pos;

            var tpPos = TPArea.transform.position;
            tpPos.y += axis.y * Time.deltaTime * 10.0f;
            TPArea.transform.position = tpPos;
        }

        if (Mathf.Abs(axis.x) >= 0.5f)
        {
            var currentRot = mPlayer.transform.rotation.eulerAngles;
            currentRot.y += axis.x * Time.deltaTime * 50.0f;
            mPlayer.transform.rotation = Quaternion.Euler(currentRot);
        }
    }
}
