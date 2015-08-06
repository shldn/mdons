using UnityEngine;
using System.Collections;
using System;

// ---------------------------------------------------------------------------------- //
// MainCameraController.cs
//   -Wes Hawkins
//
// Written to replace the ThirdPersonCamera.js class.
// Attach this to the Main Camera in the scene if it is a scene in which players run
//   around and do stuff.
//
// This class controls the entirety of the main camera's motion. It assumes that there
//   will always be one and only one main camera, and its main functions are to follow
//   the player or observe entities near the player.
//
// Public booleans should be got/set through the static Inst.
//
// Setting cameraType should smoothly 'fix' the camera's activity, for instance if the
//   camera is in SNAPCAM mode and is set to FOLLOWPLAYER, it will smoothly reset back
//   to the position of the player.
//
// Setting followPlayerHeight/Distance etc. will affect how the camera behaves wehenver
//   it returns to FOLLOWPLAYER mode, etc.
// ---------------------------------------------------------------------------------- //

public enum CameraType{
    NONE = -1,
    FOLLOWPLAYER = 0,     // normal ThirdPerson controls
    FIRSTPERSON = 1,      // first-person view, mouse controls the look direction
    FREELOOK = 2,         // camera not tied to the player
    SNAPCAM = 3,	      // camera not tied to player and cannot be moved
}; // End of CameraType.

public enum UpdateMethod
{
    UPDATE = 0,
    FIXED_UPDATE = 1,
    LATE_UPDATE = 2,
}

public class CameraChangeEventArgs : EventArgs
{
    public CameraChangeEventArgs(CameraType old_, CameraType new_) { oldCameraType = old_; newCameraType = new_; }
    public CameraType oldCameraType;
    public CameraType newCameraType;
} // End of CameraChangeEventArgs.

public class MainCameraController : MonoBehaviour {

    public static MainCameraController Inst = null;

    private PlayerController playerController = null;
    private Camera mainCamera = null;
    private CameraType cameraTypeImpl = CameraType.FOLLOWPLAYER;
    private CameraType lastCameraType = CameraType.FOLLOWPLAYER;
    public CameraType cameraType { get { return cameraTypeImpl; } set { SetCameraType(value); } }
    public CameraType LastCameraType { get { return lastCameraType; } }

    private Vector3 cameraTargetPos = Vector3.zero;
    private Vector3 cameraPosVel = Vector3.zero;
    public Vector3 CameraTargetPos { get { return cameraTargetPos; } set { cameraTargetPos = value; } }

    private Vector3 cameraTargetEulers = Vector3.zero;
    private Vector3 cameraRotVel = Vector3.zero;
    public Vector3 CameraTargetEulers { get { return cameraTargetEulers; } set { cameraTargetEulers = value; } }

    public float cameraSmooth = 0.15f;
    public UpdateMethod updateMethod = UpdateMethod.UPDATE;

    // Player camera options
    public float followPlayerHeight = 4f;
    public float followPlayerDistance = 6f;
    public float rightOffset = 1f;
    public float tilt = 0f;
    public float orbitOffsetAngle = 0.0f; // 180 will cause the camera to look at the front of the player.

    // Snap camera options
    public bool snapCamMakesPlayersInvisible = true;
    public bool playerMovementExitsSnapCam = true;

    // Mouse-controlled camera options
    private static float mouseLookSpeed = 3f;
    private static float cameraDriveSpeed = 20f;
    public static float MouseLookSpeed { get { return mouseLookSpeed; } set { mouseLookSpeed = value; } }
    public static float CameraDriveSpeed { get { return cameraDriveSpeed; } set { cameraDriveSpeed = value; } }

    // Has the camera snapped to it's initial position?
    public bool initializedPosition = false;

    // Options
    public bool playerInvisibleIfSnapCam = true;

    // Player peruse tilt
    public bool gazeTiltEnabled = true; // If true, the camera will sway towards the direction of the cursor.
    public float maxGazePan = 10f;
    public float maxGazeTilt = 10f;
    public Vector2 gazePanTiltNormalized = Vector2.zero;

    public float maxGazePanFirstPerson = 180f;
    public float maxGazeTiltFirstPerson = 90f;

    // Game state
    private bool appFocused = true;

    // Events
    public delegate void ChangeCameraTypeEventHandler(object sender, CameraChangeEventArgs e);
    public event ChangeCameraTypeEventHandler ChangeCameraType;

    public bool gazePanLock = false;

    // tracking state helpers
    bool screenTaken = false;
    bool hidPlayer = false;

    void Awake(){
        Inst = this;
        playerMovementExitsSnapCam = true;

        GameGUI.Inst.fadeAlpha = 1.5f;
        GameGUI.Inst.fadeOut = false;
    } // End of Awake().

    void Start(){
        mainCamera = Camera.main;

        // Initialize camera settings for different levels.
        //   We do this here because it sucks having to push the entire .unity scene binary to
        //   git whenever we make an adjustment to a camera.
        switch(GameManager.Inst.LevelLoaded){
            case GameManager.Level.CAMPUS:
            case GameManager.Level.MINICAMPUS:
                //             fov, h,  d,  r, tlt, gpan, gtlt
                CameraSettings(40f, 5f, 8f, 0f, 5f, 10f, 10f);
                break;
            case GameManager.Level.ORIENT :
                CameraSettings(60f, 4f, 6f, 1f, 0f, 20f, 20f);
                break;
            case GameManager.Level.BIZSIM :
                CameraSettings(45f, 4.75f, 6f, 1f, 0f, 20f, 20f);
                break;
            case GameManager.Level.TEAMROOM :
                CameraSettings(60f, 4f, 6f, 1f, 0f, 20f, 20f);
                break;
        }
    } // End of Start().

    void Update()
    {
        if (updateMethod == UpdateMethod.UPDATE)
            UpdateImpl();
    }

    void LateUpdate()
    {
        if (updateMethod == UpdateMethod.LATE_UPDATE)
            UpdateImpl();
    }

    void FixedUpdate()
    {
        if(updateMethod == UpdateMethod.FIXED_UPDATE)
            UpdateImpl();
    }

    void UpdateImpl()
    {

        if(Input.GetKey(KeyCode.T) && Input.GetKey(KeyCode.P) && !screenTaken){
            Application.CaptureScreenshot("Screenshot_II" + Time.fixedTime + ".png");
            screenTaken = true;
        }
        if(!Input.GetKey(KeyCode.P))
            screenTaken = false;


        // Smooth damp position
        mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, cameraTargetPos, ref cameraPosVel, cameraSmooth);

        // Smooth damp rotation (euler angles)
        //cameraTargetEulers.x = Mathf.Clamp(cameraTargetEulers.x, -80f, 80f);

        Vector3 mainCameraEulers = mainCamera.transform.eulerAngles;
        mainCameraEulers.x = Mathf.SmoothDampAngle(mainCameraEulers.x, cameraTargetEulers.x, ref cameraRotVel.x, cameraSmooth);
        mainCameraEulers.y = Mathf.SmoothDampAngle(mainCameraEulers.y, cameraTargetEulers.y, ref cameraRotVel.y, cameraSmooth);
        mainCameraEulers.z = Mathf.SmoothDampAngle(mainCameraEulers.z, cameraTargetEulers.z, ref cameraRotVel.z, cameraSmooth);
        mainCamera.transform.eulerAngles = mainCameraEulers;


        // Find the local player's player controller.
        if (!playerController && GameManager.Inst.LocalPlayer != null)
            playerController = GameManager.Inst.LocalPlayer.gameObject.GetComponent<PlayerController>();


        Vector2 mouseScreenNormalized = new Vector2(MathHelper.Remap(Input.mousePosition.x, 0f, Screen.width, 0f, 1f), MathHelper.Remap(Input.mousePosition.y, 0f, Screen.height, 0f, 1f));
        if((mouseScreenNormalized.x >= 0f) && (mouseScreenNormalized.x < 1f) && (mouseScreenNormalized.y >= 0f) && (mouseScreenNormalized.y < 1f)){
            gazePanTiltNormalized.x = mouseScreenNormalized.y - 0.5f;
            gazePanTiltNormalized.y = mouseScreenNormalized.x - 0.5f;
        }

        // Camera following the player.
        if ((cameraType == CameraType.FOLLOWPLAYER) && (playerController != null) && (GameManager.Inst.LocalPlayer != null)){

            Transform playerTransform = GameManager.Inst.LocalPlayer.gameObject.transform;

            // Handy vectors for camera offsets.
            Vector3 playerForwardVector = MathHelper.MoveAngleUnitVector(playerController.forwardAngle);
            Vector3 playerRightVector = MathHelper.MoveAngleUnitVector(playerController.forwardAngle + 90f);


            cameraTargetPos = playerTransform.position + playerTransform.localScale.x * ((playerForwardVector * -followPlayerDistance) + (Vector3.up * followPlayerHeight) + (playerRightVector * rightOffset));

            // experimental ------------------------
            // Rotate camera target pos based on player's normal to ground.
            Vector3 cameraTargetVector = cameraTargetPos - playerTransform.position;
            Quaternion cameraTargetQuat = Quaternion.AngleAxis(orbitOffsetAngle, Vector3.up) * Quaternion.LookRotation(cameraTargetVector);

            float camAngleAdjust = Vector3.Angle(playerController.groundNormal, playerTransform.forward) - 90f;
            camAngleAdjust *= 0.5f;

            cameraTargetQuat *= Quaternion.AngleAxis(camAngleAdjust, Vector3.right);
            cameraTargetPos = playerTransform.position + (cameraTargetQuat * Vector3.forward * cameraTargetVector.magnitude);

            // experimental ------------------------
            Vector3 playerHead = GameManager.Inst.LocalPlayer.HeadPosition;
            Ray followPlayerRay = new Ray(playerHead, cameraTargetPos - playerHead);
            RaycastHit followPlayerHit = new RaycastHit();
            float castRadius = (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM) ? 0.3f : 0.7f;
            castRadius = (GameManager.Inst.LevelLoaded == GameManager.Level.MOTION_TEST && GameManager.Inst.LevelLoaded == GameManager.Level.SCALE_GAME) ? 0.075f : castRadius;
            if (GameManager.Inst.LocalPlayer.Scale.x > 0.8f && Physics.SphereCast(followPlayerRay, castRadius, out followPlayerHit, followPlayerDistance)){
                if(!followPlayerHit.collider.gameObject.GetComponent<PlayerController>())
                    cameraTargetPos = followPlayerRay.origin + followPlayerRay.direction.normalized * (followPlayerHit.distance - 1f);
            }

            
            
            if(gazeTiltEnabled && !gazePanLock && appFocused)
                cameraTargetEulers = new Vector3(tilt - camAngleAdjust - (gazePanTiltNormalized.x * maxGazeTilt), playerController.forwardAngle + (gazePanTiltNormalized.y * maxGazePan) + orbitOffsetAngle, 0f);
            else
                cameraTargetEulers = new Vector3(tilt - camAngleAdjust, playerController.forwardAngle + orbitOffsetAngle, 0f);
        }

        // First-person mouse-controlled camera.
        if ((cameraType == CameraType.FIRSTPERSON) && (playerController != null) && (GameManager.Inst.LocalPlayer != null)){
            cameraTargetPos = GameManager.Inst.LocalPlayer.HeadPosition;

            Quaternion targetRot = GameManager.Inst.LocalPlayer.playerController.transform.rotation;
            targetRot *= Quaternion.AngleAxis(maxGazePanFirstPerson * gazePanTiltNormalized.y, Vector3.up);
            targetRot *= Quaternion.AngleAxis(maxGazeTiltFirstPerson * -gazePanTiltNormalized.x, Vector3.right);

            cameraTargetEulers = targetRot.eulerAngles;
        }

        //if (Input.GetKeyDown(KeyCode.F))
        //    cameraType = CameraType.FREELOOK;

        // Free-flying camera
        if (cameraType == CameraType.FREELOOK && playerController != null){
            // Rotate camera with mouse
            cameraTargetEulers.x -= Input.GetAxis("Mouse Y") * mouseLookSpeed;
            cameraTargetEulers.y += Input.GetAxis("Mouse X") * mouseLookSpeed;

            // Move camera using QWEASD
            Vector3 camTempTargetPos = cameraTargetPos;
            camTempTargetPos += mainCamera.transform.forward * cameraDriveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
            camTempTargetPos += mainCamera.transform.right * cameraDriveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;

            if(Input.GetKey(KeyCode.E))
                camTempTargetPos += Vector3.up * cameraDriveSpeed * Time.deltaTime;
            if(Input.GetKey(KeyCode.Q))
                camTempTargetPos -= Vector3.up * cameraDriveSpeed * Time.deltaTime;

            cameraTargetPos = camTempTargetPos;

            playerController.lockMovement = true;
        }

        if (GameManager.Inst.LocalPlayer && GameManager.Inst.LocalPlayer.gameObject && !initializedPosition){
            CameraToInitialPos();
            initializedPosition = true;
        }

        // Hide player if camera is too close.
        if (GameManager.Inst.LocalPlayer != null)
        {
            float distToCam = Vector3.Distance(GameManager.Inst.LocalPlayer.HeadPosition, mainCamera.transform.position);
            float distHidePlayer = (GameManager.Inst.ServerConfig == "MDONS") ? 0.5f * GameManager.Inst.LocalPlayer.gameObject.transform.localScale.x : 1.5f;
            if (GameManager.Inst.LevelLoaded == GameManager.Level.MOTION_TEST)
                distHidePlayer = -1f;
            if ((distToCam < distHidePlayer) && GameManager.Inst.LocalPlayer.Visible)
            {
                GameManager.Inst.LocalPlayer.Visible = false;
                hidPlayer = true;
            }
            if ((distToCam > distHidePlayer) && !GameManager.Inst.LocalPlayer.Visible && hidPlayer)
            {
                GameManager.Inst.LocalPlayer.Visible = true;
                hidPlayer = false;
            }

            // No click to move while in Snap Cam
            if (cameraType == CameraType.SNAPCAM)
                PlayerController.ClickToMoveInterrupt();

            // Make player invisible if in SNAP mode.
            if ((cameraType == CameraType.SNAPCAM) && playerInvisibleIfSnapCam)
                GameManager.Inst.LocalPlayer.Visible = false;

            // Exit snap mode if movement (if applicable)
            if ((cameraType == CameraType.SNAPCAM) && playerMovementExitsSnapCam && PlayerController.Local.nonzeroThrottle)
                cameraType = CameraType.FOLLOWPLAYER;
        }

        gazePanLock = false;

        // Debug
        /*
        // Toggle first-person view with M.
        if (Input.GetKeyDown(KeyCode.M) && cameraType == CameraType.FOLLOWPLAYER)
            cameraType = CameraType.FIRSTPERSON;
        else if (Input.GetKeyDown(KeyCode.M) && cameraType == CameraType.FIRSTPERSON)
            cameraType = CameraType.FOLLOWPLAYER;
        */

    } // End of UpdateImpl().

    void CameraSettings(float _fieldOfView, float _followPlayerHeight, float _followPlayerDist, float _rightOffset, float _tilt, float _maxGazePan, float _maxGazeTilt){
        mainCamera.fieldOfView = _fieldOfView;
        followPlayerHeight = _followPlayerHeight;
        followPlayerDistance = _followPlayerDist;
        rightOffset = _rightOffset;
        tilt = _tilt;
        maxGazePan = _maxGazePan;
        maxGazeTilt = _maxGazeTilt;
    } // End of CameraSettings().

    public void CameraToInitialPos(){

        if (!GameManager.Inst.LocalPlayer || !GameManager.Inst.LocalPlayer.gameObject)
            return;

        Transform playerTransform = GameManager.Inst.LocalPlayer.gameObject.transform;

        Vector3 playerForwardVector = MathHelper.MoveAngleUnitVector(playerController.forwardAngle);
        Vector3 playerRightVector = MathHelper.MoveAngleUnitVector(playerController.forwardAngle + 90f);
        cameraTargetPos = playerTransform.position + playerTransform.localScale.x * ((playerForwardVector * -followPlayerDistance) + (Vector3.up * followPlayerHeight) + (playerRightVector * rightOffset));
        if (mainCamera == null)
            mainCamera = Camera.main;
        mainCamera.transform.position = cameraTargetPos;
        cameraPosVel = Vector3.zero;

        mainCamera.transform.rotation = playerTransform.rotation;
        cameraTargetEulers = playerTransform.eulerAngles;
        cameraRotVel = Vector3.zero;
    }

    public void SnapCamera(Vector3 snapTargetPos, Quaternion snapTargetRot, float smoothSpeed){
        cameraType = CameraType.SNAPCAM;
        cameraTargetPos = snapTargetPos;
        cameraTargetEulers = snapTargetRot.eulerAngles;
        cameraSmooth = smoothSpeed;
    } // End of SnapCamera().

    public void CycleCamera(){
        cameraType++;
        if ((int)cameraType > 3)
            cameraType = 0;
    } // End of CycleCamera().

    private void SetCameraType(CameraType cType)
    {
        if( cType != cameraTypeImpl )
        {
            lastCameraType = cameraTypeImpl;
            if (ChangeCameraType != null)
                ChangeCameraType(this, new CameraChangeEventArgs(cameraTypeImpl, cType));
        }
        cameraTypeImpl = cType;
        //if (cameraTypeImpl == CameraType.FIRSTPERSON)
        //    cameraTargetEulers = GameManager.Inst.LocalPlayer.gameObject.transform.eulerAngles;

    } // End SetCameraType().

    private void OnApplicationFocus(bool focusStatus)
    {
        appFocused = focusStatus;
    } // End OnApplicationFocus().

} // End of MainCameraController.