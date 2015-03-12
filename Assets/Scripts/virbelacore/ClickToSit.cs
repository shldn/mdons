using UnityEngine;
using System.Collections;



public class ClickToSit : MonoBehaviour {
    public float maxDistance = 100;
    public bool flipDir = false;
    public ChairType chairType = ChairType.CUSTOM;
    private SitSettings sitSettings = null;


    void Awake()
    {
        sitSettings = GetComponent<SitSettings>();
        if (sitSettings == null)
        {
            sitSettings = gameObject.AddComponent<SitSettings>();
            sitSettings.ChairTypeAcc = chairType;
        }
    }

    void Start()
    {
        MessageOnHover moh = gameObject.AddComponent<MessageOnHover>();
        moh.message = "Click to Sit";
        moh.hideForCameraType = CameraType.FIRSTPERSON;
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            if (MaxDistCheck())
            {
                Transform playerTransform = GameManager.Inst.LocalPlayer.gameObject.transform;

                // put player in position
                float dirMultiplier = flipDir ? -1.0f : 1.0f;
                Vector3 forwardDir = dirMultiplier * gameObject.transform.forward;
                GameManager.Inst.playerManager.SetLocalPlayerTransform(gameObject.transform.position + (-sitSettings.DropDistance * Vector3.up) + (sitSettings.DistanceFromChair * forwardDir.normalized), Quaternion.LookRotation(forwardDir));
                GameManager.Inst.LocalPlayer.Sit();
            }
            else
                InfoMessageManager.Display("Please move closer to sit");
        }
    }

    bool MaxDistCheck()
    {
        return (maxDistance < 0 || MathHelper.Distance2D(gameObject.transform.position, GameManager.Inst.LocalPlayer.gameObject.transform.position) < maxDistance);
    }
}
