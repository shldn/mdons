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
    public float startScale = 100.0f;

    // Helpers
    float origNearClip = 1.0f;
    float origFarClip = 15000.0f;
    bool adjustmentToggle = false;

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
        Screen.showCursor = true;
        SetToTargetScale(new Vector3(startScale,startScale,startScale));
        PhysicsAdjustment();
    }

    void Update()
    {
        if (MainCameraController.Inst.updateMethod == UpdateMethod.UPDATE)
            UpdateImpl();
    }

    void FixedUpdate()
    {
        if (MainCameraController.Inst.updateMethod == UpdateMethod.FIXED_UPDATE)
            UpdateImpl();
    }

    void UpdateImpl()
    {
        if (Input.GetKey(KeyCode.Equals))
            ScaleUp();
        if (Input.GetKey(KeyCode.Minus))
            ScaleDown();

        if (Input.GetKeyUp(KeyCode.T))
            Camera.main.gameObject.GetComponent<TiltShift>().enabled = !Camera.main.gameObject.GetComponent<TiltShift>().enabled;

        if (Input.GetKeyUp(KeyCode.P))
        {
            GameManager.Inst.LoadLevel(GameManager.Level.AVATARSELECT);
            //Vector3 targetScale = GameManager.Inst.LocalPlayer.Scale;
            //GameGUI.Inst.customizeAvatarGui.ChangeCharacter((GameManager.Inst.LocalPlayer.ModelIdx + 1) % PlayerManager.PlayerModelNames.Length);
            //GameManager.Inst.LocalPlayer.playerController.enabled = true;
            //GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;
            //SetToTargetScale(targetScale);
        }

        if( Input.GetKeyUp(KeyCode.O))
            ScaleAvatarOnCollision.used.Clear();

        if (Input.GetKeyUp(KeyCode.RightBracket))
        {
            MainCameraController.Inst.tilt = -0.5f * Camera.main.fieldOfView;
            MainCameraController.Inst.followPlayerDistance = 1.69f;
            MainCameraController.Inst.followPlayerHeight = 0.0f;
        }

        if (Input.GetKeyUp(KeyCode.LeftBracket))
        {
            MainCameraController.Inst.tilt = 11f;
            MainCameraController.Inst.followPlayerDistance = 10f;
            MainCameraController.Inst.followPlayerHeight = 10f;
        }

        if (Input.GetKeyUp(KeyCode.Alpha3))
            GameManager.Inst.LoadLevel(GameManager.Level.MDONS_TEST);

        // Initialize focal length
        if (Time.timeSinceLevelLoad < 1f)
            UpdateFocalLength();

    }

    public void ScaleUp()
    {
        float scaleFactor = 1.0f + scaleSpeed;

        // Check if there is something above the avatar
        RaycastHit hit = new RaycastHit();
        Ray ray = new Ray(GameManager.Inst.LocalPlayer.HeadTopPosition, Vector3.up);
        if (Physics.Raycast(ray, out hit) && hit.distance < 1.0f)
            scaleFactor = 1.0f;

        GameManager.Inst.LocalPlayer.Scale *= scaleFactor;

        if (ShepardEngine.Inst)
            ShepardEngine.Inst.SetVelocity(1f);
    }

    public void ScaleDown()
    {
        GameManager.Inst.LocalPlayer.Scale *= (1.0f - scaleSpeed);

        if (ShepardEngine.Inst)
            ShepardEngine.Inst.SetVelocity(-1f);
    }

    void SetToTargetScale(Vector3 scale)
    {
        GameManager.Inst.LocalPlayer.Scale = scale;
    }

    // After scaling the world, this seems to be necessary for the avatar to properly collide with things.
    public void PhysicsAdjustment()
    {
        float dir = adjustmentToggle ? 1f : -1f;
        GameManager.Inst.LocalPlayer.Scale *= (1.0f + dir * 0.001f);
        adjustmentToggle = !adjustmentToggle;
    }

    public void UpdateEnvironment()
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
