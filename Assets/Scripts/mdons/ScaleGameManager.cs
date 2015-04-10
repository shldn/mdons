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
    public float startScale = 100.0f;

    // Helpers
    float origNearClip = 1.0f;
    float origFarClip = 15000.0f;

    public void Touch() { }

    void Awake()
    {
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;
        origNearClip = Camera.main.nearClipPlane;
        origFarClip = Camera.main.farClipPlane;

#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            gameObject.AddComponent<LSLSender>();
    }

    void Start()
    {
        SetToTargetScale(new Vector3(startScale,startScale,startScale));
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
            UpdateEnvironment();
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            GameManager.Inst.LocalPlayer.Scale *= (1.0f - scaleSpeed);
            UpdateEnvironment();
        }
        if (Input.GetKeyUp(KeyCode.T))
            Camera.main.gameObject.GetComponent<TiltShift>().enabled = !Camera.main.gameObject.GetComponent<TiltShift>().enabled;

        if (Input.GetKeyUp(KeyCode.P))
        {
            Vector3 targetScale = GameManager.Inst.LocalPlayer.Scale;
            GameGUI.Inst.customizeAvatarGui.ChangeCharacter((GameManager.Inst.LocalPlayer.ModelIdx + 1) % PlayerManager.PlayerModelNames.Length);
            GameManager.Inst.LocalPlayer.playerController.enabled = true;
            GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;
            SetToTargetScale(targetScale);
        }
    }

    void SetToTargetScale(Vector3 scale)
    {
        GameManager.Inst.LocalPlayer.Scale = scale;
        UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        UpdateGravity();
        UpdateFocalLength();
        UpdateClippingPlanes();
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

    void UpdateClippingPlanes()
    {
        Camera.main.farClipPlane = Mathf.Max(origFarClip * 0.1f, origFarClip * (GameManager.Inst.LocalPlayer.Scale.x));
        Camera.main.nearClipPlane = Mathf.Max(origNearClip * 0.1f, origNearClip * (GameManager.Inst.LocalPlayer.Scale.x));
    }

    void OnDestroy()
    {
        Physics.gravity = new Vector3(0, -19.62F, 0);
        mInst = null;
    }
}
