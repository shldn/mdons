using UnityEngine;

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
            GameManager.Inst.LocalPlayer.gameObject.transform.localScale *= (1.0f + scaleSpeed);
            UpdateGravity();            
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            GameManager.Inst.LocalPlayer.gameObject.transform.localScale *= (1.0f - scaleSpeed);
            UpdateGravity();
        }
    }

    void UpdateGravity()
    {
        Physics.gravity = GameManager.Inst.LocalPlayer.gameObject.transform.localScale.x * new Vector3(0, -19.62F, 0);
    }

    void OnDestroy()
    {
        Physics.gravity = new Vector3(0, -19.62F, 0);
        mInst = null;
    }
}
