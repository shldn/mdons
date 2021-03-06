﻿using UnityEngine;

public class ScaleGameManager : MonoBehaviour
{

    private static ScaleGameManager mInst = null;

    public static ScaleGameManager Inst
    {
        get
        {
            if (mInst == null)
                mInst = (new GameObject("ScaleGameManager")).AddComponent(typeof(ScaleGameManager)) as ScaleGameManager;
            return mInst;
        }
    }

    // LSL variables
    public bool sendLSLData = true;

    // Options
    public float scaleSpeed = 0.01f;

    public void Touch() { }

    void Awake()
    {
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;

#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            gameObject.AddComponent<LSLSender>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Equals))
        {
            float scaleFactor = 1.0f + scaleSpeed;

            // Check if there is something above the avatar
            RaycastHit hit = new RaycastHit();
            Ray ray = new Ray(GameManager.Inst.LocalPlayer.HeadTopPosition, Vector3.up);
            if (Physics.Raycast(ray, out hit) && hit.distance < 1.0f)
                scaleFactor = 1.0f;

            GameManager.Inst.LocalPlayer.Scale *= scaleFactor;
            UpdateGravity();
            UpdateFocalLength();
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            GameManager.Inst.LocalPlayer.Scale *= (1.0f - scaleSpeed);
            UpdateGravity();
            UpdateFocalLength();
        }
        if (Input.GetKeyUp(KeyCode.T))
            Camera.main.gameObject.GetComponent<TiltShift>().enabled = !Camera.main.gameObject.GetComponent<TiltShift>().enabled;
    }

    void UpdateGravity()
    {
        Physics.gravity = GameManager.Inst.LocalPlayer.Scale.x * new Vector3(0, -19.62F, 0);
    }

    void UpdateFocalLength()
    {
        float distToPlayer = (GameManager.Inst.LocalPlayer.HeadPosition - Camera.main.transform.position).magnitude;
        Camera.main.GetComponent<TiltShift>().focalPoint = distToPlayer;
    }

    void OnDestroy()
    {
        Physics.gravity = new Vector3(0, -19.62F, 0);
        mInst = null;
    }
}
