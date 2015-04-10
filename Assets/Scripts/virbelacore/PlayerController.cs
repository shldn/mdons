using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------- //
// PlayerController.cs
//   -Wes Hawkins
//
// Written to replace the ThirdPersonController.js class.
// Attach to a Virbella character with a CharacterController component.
//
// Assumes the character has a 'forward' direction and will generally move along it, but
//   will allow for strafing.
//
// Depends on MathHelper class here and there.
// ---------------------------------------------------------------------------------- //

public enum CharacterState{
    Idle = 0,
    Walking = 1,
    Trotting = 2,
    Running = 3,
    Jumping = 4,
}; // End of CharacterState.


public class PlayerController : MonoBehaviour {

    public bool isLocalPlayer { get { return GameManager.Inst.LocalPlayer == playerScript; } }
    public static PlayerController Local { get { return GameManager.Inst.LocalPlayer.playerController; } }

    Animator animator = null;
    AnimatorHelper animatorHelper = null;

    public CharacterState playerState = CharacterState.Idle;
    public bool nonzeroThrottle { get { return (forwardThrottle != 0f) || (turnThrottle != 0f); } }

    public Player playerScript = null; // Set by the Player script automatically.

    // Movement characteristics ----------------------------------------------------- //
    float walkSpeed = 10f;
    float runSpeed = 20f;
    float sprintSpeed = 30f;
    public enum MovementSpeed{
        walk,
        run,
        sprint
    }
    public MovementSpeed speed = MovementSpeed.walk;
    public enum NavMode{
        locked,
        navmesh,
        physics,
        physicsSticky
    }
    NavMode _navMode = NavMode.navmesh;
    public NavMode navMode { get{ return _navMode; }
        set{
            switch(value){
                case NavMode.locked:
                    rigidbody.isKinematic = true;
                    collider.enabled = false;
                    if( navAgent != null )
                        navAgent.enabled = false;
                    break;
                case NavMode.navmesh:
                    rigidbody.isKinematic = false;
                    collider.enabled = true;
                    if (navAgent != null)
                        navAgent.enabled = true;
                    break;
                case NavMode.physics:
                case NavMode.physicsSticky:
                    rigidbody.isKinematic = false;
                    collider.enabled = true;
                    if (navAgent != null)
                        navAgent.enabled = false;
                    break;
            }
            _navMode = value;
        }
    }
    bool useNavMesh { get { return (navMode == NavMode.navmesh); } }
    bool usePhysics { get { return (navMode == NavMode.physics) || (navMode == NavMode.physicsSticky); } }

    public float turnSpeed = 90f;
    public float acceleration = 20f;

    // Control variables -- set these externally to drive the player around. -------- //
    public bool running = false;
    public float forwardThrottle = 0f;
    public float strafeThrottle = 0f;
    public float turnThrottle = 0f;

    // Functional movement variables. ----------------------------------------------- //
    public float forwardAngle = 0f;
    private float turnThrottleSmooth = 0f;
    public float moveSpeed = 0f;
    public float strafeSpeed = 0f;
    Vector3 moveVector = Vector3.zero;

    public bool isMoving = false;
    [HideInInspector] public bool positionDirty = false;
    [HideInInspector] public bool animationDirty = false;
    [HideInInspector] public bool scaleDirty = false;

    private Vector3 lastPos = Vector3.zero;
    private float lastAngle = 0f;

    public Vector3 groundNormal = Vector3.zero;

    public Vector3 holdNavDest = Vector3.zero;
    bool navDestHeld = false;
    float targetYOnArrival = 0f;
    bool targetArrivalY = false;

    public float targetDistFromDest = 0.1f;
    NavMeshAgent navAgent = null;
    public NavMeshAgent NavAgent { get { return navAgent; } }
    public GameObject goToIndicatorPrefab = null;
    GameObject goToIndicator = null;

    public bool pathfindingActive = false;
    public GameObject pathfindingDestPrefab = null;
    GameObject pathfindingDest = null;

    Player followedPlayer = null;

    NavMeshPath testNavPath = null;

    public bool targetRotActive = false;
    public float targetRot = 0f;

    public float shuffleTime = 0f;

    // Head Tilt helpers ------------------------------------------------------------ //
    Vector3 initHeadTiltMousePos = Vector3.zero;
    bool hasMovedMouseForHeadTilt = false;
    bool hasRecordedInitMousePosForHeadTilt = false;
    
    // Options ---------------------------------------------------------------------- //
    public bool clickToMove = true;
    public int ctmiFrames = 0;
    public bool lockMovement = false;
    bool loadDestinationIndicators = true;

    float maxGazeAnimPan = 80f;
    float maxGazeAnimTilt = 60f;

    public Vector2 gazePanTilt = new Vector2();
    Vector2 lastGazePanTilt = new Vector2();

    Vector3 targetLookAtPos = Vector3.zero;
    Vector3 smoothedLookAtPos = Vector3.zero;
    Vector3 lookAtSmoothVel = Vector3.zero;

    bool lookAtOverride = false;
    Vector3 lookAtOverridePos = Vector3.zero;
    Player lookAtOverridePlayer = null;
    Transform lookAtOverrideTransform = null;

    float lookAtWeight = 1f;
    public static float lookAtSpeed = 0.1f;
    public static bool focusOnSpeaker = false;

    public bool falling = false;
    public bool IsFalling{ get{ return falling; }}
    Vector3 fallVelocity = Vector3.zero;

    bool grounded = false;

    // Number of seconds user's mouse hasn't moved and user is moving.
    // Used for zeroing the camera while running.


	void Awake(){
        animator = GetComponent<Animator>();
        animatorHelper = GetComponent<AnimatorHelper>();
        testNavPath = new NavMeshPath();

        if (GameManager.Inst.LevelLoaded == GameManager.Level.MOTION_TEST)
            loadDestinationIndicators = false;

	} // End of Awake().


    void Start(){
        if(isLocalPlayer || (playerScript != null && playerScript.isBot)){
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.angularSpeed = 9999f;
            navAgent.height = 3.5f;

            navAgent.speed = 0f;
        }

        if (isLocalPlayer && loadDestinationIndicators) {
            if (goToIndicatorPrefab == null || pathfindingDest == null){
                VDebug.LogError("Warning: goto indicator and/or pathfinding destination prefab(s) have not been set");
                if (goToIndicatorPrefab == null)
                    goToIndicatorPrefab = (GameObject)Resources.Load("Avatars/Effects/PathfindingGoTo");
                if (pathfindingDest == null)
                    pathfindingDestPrefab = (GameObject)Resources.Load("Avatars/Effects/PathfindingDest");
            }

            goToIndicator = Instantiate(goToIndicatorPrefab) as GameObject;
            pathfindingDest = Instantiate(pathfindingDestPrefab) as GameObject;
        }
        else{
            goToIndicator = new GameObject();
            pathfindingDest = new GameObject();
        }

    } // End of Start().


    void Update(){

        if(useNavMesh && navAgent && navAgent.enabled){
            Debug.DrawLine(transform.position + (Vector3.up * 3f), pathfindingDest.transform.position, Color.green);

            if(followedPlayer){
                pathfindingDest.transform.position = followedPlayer.gameObject.transform.position;
                // would be better to only do this if the followedPlayer position has changed.
                navAgent.SetDestination(pathfindingDest.transform.position);
                pathfindingActive = true;
            }

            if (pathfindingActive){
                pathfindingDest.SetActive(true);
                //navAgent.SetDestination(pathfindingDest.transform.position);
            }
            else{
                pathfindingDest.SetActive(false);
                if(navAgent.pathStatus != NavMeshPathStatus.PathInvalid)
                    navAgent.SetDestination(transform.position);
            }

            if(navDestHeld){
                pathfindingDest.transform.position = holdNavDest;
                navAgent.SetDestination(pathfindingDest.transform.position);
                pathfindingActive = true;
                navDestHeld = false;
            }

            if (navAgent.hasPath && pathfindingActive){
                float desiredY = MathHelper.UnitVectorMoveAngle(navAgent.steeringTarget - transform.position);
                turnThrottle = Mathf.Clamp(Mathf.DeltaAngle(forwardAngle, desiredY) * 0.05f, -1f, 1f);

                if (Mathf.Abs(turnThrottle) <= 0.5f)
                    forwardThrottle = Mathf.Clamp01((Vector3.Distance(transform.position, pathfindingDest.transform.position) - targetDistFromDest) * 0.2f);
                else
                    forwardThrottle = 0f;
            }

            if (navAgent.hasPath && (Vector3.Distance(transform.position, pathfindingDest.transform.position) <= (0.3f + targetDistFromDest)) && pathfindingActive){
                pathfindingActive = false;
                forwardThrottle = 0f;
            }

            if(targetArrivalY && !pathfindingActive){
                turnThrottle = Mathf.Clamp(Mathf.DeltaAngle(forwardAngle, targetYOnArrival) * 0.05f, -1f, 1f);
                if(Mathf.Abs(Mathf.DeltaAngle(forwardAngle, targetYOnArrival)) <= 5f)
                    targetArrivalY = false;
            }

            // Indicator defaults to 'off', only shows if it has a valid position.
            if (isLocalPlayer) {
                goToIndicator.SetActive(false);
                if (clickToMove && (ctmiFrames == 0) && !GameGUI.Inst.IsMouseOverGuiLayerElement && !AnnouncementManager.Inst.IsAnnouncementDisplayed && GameManager.Inst.LevelLoaded != GameManager.Level.MOTION_TEST)
                {
                    RaycastHit mouseHit = new RaycastHit();
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f)), out mouseHit, 1 << LayerMask.NameToLayer("ground"))){
                        NavMeshHit navHit = new NavMeshHit();
                        if (NavMesh.SamplePosition(mouseHit.point, out navHit, 1f, 1)){
                            NavMesh.CalculatePath(transform.position, navHit.position, -1, testNavPath);

                            if(testNavPath.status == NavMeshPathStatus.PathComplete){
                                goToIndicator.transform.position = navHit.position;
                                goToIndicator.SetActive(true);
                            }
                        }
                    }
                }
            }

        } // Main navmesh/click-to-move control.

        if(falling){
            transform.position += fallVelocity * Time.deltaTime;
            fallVelocity += Physics.gravity * Time.deltaTime;
            fallVelocity.x = Mathf.Lerp(fallVelocity.x, 0f, 1f * Time.deltaTime);
            fallVelocity.z = Mathf.Lerp(fallVelocity.z, 0f, 1f * Time.deltaTime);
        }
        else
            fallVelocity = Vector3.zero;

        // Movement locks if manually set or character is doing a custom animation.
        if (lockMovement || (navMode == NavMode.locked) || GetComponent<AnimatorHelper>().CustomAnimPlaying()){
            forwardThrottle = 0f;
            strafeThrottle = 0f;
            turnThrottle = 0f;
        }

        // If character does anything at all, set state to dirty.
        if ((transform.position != lastPos) || (forwardAngle != lastAngle)){
            positionDirty = true;
            lastPos = transform.position;
            lastAngle = forwardAngle;
        }

        // Update angle and movement speed based on user input.
        turnThrottleSmooth = Mathf.MoveTowards(turnThrottleSmooth, turnThrottle, 10f * Time.deltaTime);
        forwardAngle += turnThrottleSmooth * turnSpeed * Time.deltaTime;

        forwardThrottle = Mathf.Clamp(forwardThrottle, -0.5f, 1f);
        float targetSpeed = 0f;
        switch(speed){
            case MovementSpeed.walk:
                targetSpeed = walkSpeed;
                break;
            case MovementSpeed.run:
                targetSpeed = runSpeed;
                break;
            case MovementSpeed.sprint:
                targetSpeed = sprintSpeed;
                break;
        }
        moveSpeed = Mathf.MoveTowards(moveSpeed, targetSpeed * forwardThrottle, acceleration * Time.deltaTime);


        // Propel the character.
        moveVector = gameObject.transform.localScale.x * (MathHelper.MoveAngleUnitVector(forwardAngle) * moveSpeed) + (MathHelper.MoveAngleUnitVector(forwardAngle + 90) * strafeSpeed);
        if(useNavMesh && navAgent && navAgent.enabled && (navAgent.pathStatus != NavMeshPathStatus.PathInvalid)){
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            navAgent.enabled = true;
            //Camera.main.transform.parent = null;

            navAgent.Move(moveVector * Time.deltaTime);
        }
        else if(usePhysics){
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            navAgent.enabled = false;

            //if(GameManager.Inst.LocalPlayer && GameManager.Inst.LocalPlayer.gameObject && GameManager.Inst.LocalPlayer.gameObject.activeSelf)
            //    Camera.main.transform.parent = GameManager.Inst.LocalPlayer.gameObject.transform;
        }

        RaycastHit normalHit = new RaycastHit();
        if (Physics.Raycast(new Ray(transform.position + (Vector3.up * 3f), Vector3.down * 5f), out normalHit))
            groundNormal = normalHit.normal;

        // Set transform/animations.
        Vector3 transformEulers = transform.eulerAngles;
        transformEulers.y = forwardAngle;
        transform.eulerAngles = transformEulers;

        float shuffleHelper = 0f;
        if(shuffleTime > 0f){
            shuffleTime -= Time.deltaTime;
            shuffleHelper = 1f;
        }

        // Animation speed should be relative to actual movement speed.
        animatorHelper.SetSpeed(moveSpeed + Mathf.Abs(turnThrottle) + shuffleHelper);

        // Update state based on movement speed.
        if (Mathf.Abs(moveSpeed) <= 0.1f)
            playerState = CharacterState.Idle;
        else if (Mathf.Abs(moveSpeed) <= walkSpeed)
            playerState = CharacterState.Walking;
        else if (Mathf.Abs(moveSpeed) <= runSpeed)
            playerState = CharacterState.Running;

        isMoving = moveSpeed > 0.1f;

        /*
        if(isLocalPlayer){
            // If player is running around but not using the mouse, their character will look straight ahead.
            if(isMoving && (Input.GetAxis("Mouse X") == 0f) && (Input.GetAxis("Mouse Y") == 0f))
                mouseRunTimeout += Time.deltaTime;
            // Next time the player moves the mouse, the character will turn his/her head again.
            else if((Input.GetAxis("Mouse X") != 0f) || (Input.GetAxis("Mouse Y") != 0f))
                mouseRunTimeout = 0f;
        }
        */

        // Turn head effect.
        if(PlayerManager.Inst.HeadTiltEnabled){

            Vector3 playerHead = transform.position + (Vector3.up * 3f);
            Vector3 localRelPos = transform.InverseTransformPoint(playerHead + transform.forward * 1000f);

            // User has a specific object/player/position to look at...
            if(lookAtOverride){
                // Look at targetted player's head position.
                if(lookAtOverridePlayer)
                    lookAtOverridePos = lookAtOverridePlayer.gameObject.transform.position + (Vector3.up * 3f);
            
                // Look at center of targetted object.
                if(lookAtOverrideTransform)
                    lookAtOverridePos = lookAtOverrideTransform.position;

                // If player has an override lookat target, look at that.
                localRelPos = transform.InverseTransformPoint(lookAtOverridePos);

                gazePanTilt.y = Mathf.Atan2(localRelPos.x, localRelPos.z) * Mathf.Rad2Deg;
                gazePanTilt.x = Mathf.Atan2(-localRelPos.y + 3f, MathHelper.Distance2D(Vector3.zero, localRelPos)) * Mathf.Rad2Deg;
            }
            // Local player is manually controlling their head tilt
            else if(isLocalPlayer && MainCameraController.Inst.cameraType == CameraType.FIRSTPERSON){
                gazePanTilt = new Vector2(-MainCameraController.Inst.gazePanTiltNormalized.x * MainCameraController.Inst.maxGazeTiltFirstPerson, MainCameraController.Inst.gazePanTiltNormalized.y * MainCameraController.Inst.maxGazePanFirstPerson);
            }
            else if(isLocalPlayer && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))){
                // make the player move their mouse before the head tilt kicks in.
                if (!hasRecordedInitMousePosForHeadTilt){
                    initHeadTiltMousePos = Input.mousePosition;
                    hasRecordedInitMousePosForHeadTilt = true;
                    hasMovedMouseForHeadTilt = false;
                }
                else
                    hasMovedMouseForHeadTilt = hasMovedMouseForHeadTilt || initHeadTiltMousePos != Input.mousePosition;

                if( hasMovedMouseForHeadTilt )
                    gazePanTilt = new Vector2(-MainCameraController.Inst.gazePanTiltNormalized.x, MainCameraController.Inst.gazePanTiltNormalized.y) * 90f;
            }
            // Default head to look straight ahead.
            else if(isLocalPlayer){
                gazePanTilt = Vector2.zero;
                hasRecordedInitMousePosForHeadTilt = false;
            }

            // Passively focus on speaker
            if(focusOnSpeaker && (isLocalPlayer || GameManager.buildType == GameManager.BuildType.REPLAY)){
                float talkLookRange = 10f;

                Player[] allPlayers = PlayerManager.Inst.allPlayersAndBots;
                for (int i = 0; i < allPlayers.Length; i++){
                    if((allPlayers[i].IsTyping || allPlayers[i].IsTalking) && (Vector3.Distance(transform.position, allPlayers[i].gameObject.transform.position) < talkLookRange)){

                        localRelPos = transform.InverseTransformPoint(allPlayers[i].gameObject.transform.position + (Vector3.up * 3f));

                        gazePanTilt.y = Mathf.Atan2(localRelPos.x, localRelPos.z) * Mathf.Rad2Deg;
                        gazePanTilt.x = Mathf.Atan2(-localRelPos.y + 3f, MathHelper.Distance2D(Vector3.zero, localRelPos)) * Mathf.Rad2Deg;
                        break;
                    }
                }
            }
            
            if(gazePanTilt != lastGazePanTilt){
                animationDirty = true;
                lastGazePanTilt = gazePanTilt;
            }

            Vector3 gazeAnimTargetEulers = new Vector3(gazePanTilt.x, gazePanTilt.y, 0f);
            Quaternion gazeLook = Quaternion.Euler(gazeAnimTargetEulers);

            // Smoothed lookat position...
            targetLookAtPos = playerHead + ((transform.rotation * gazeLook) * (Vector3.forward * 1000f));
            smoothedLookAtPos = Vector3.SmoothDamp(smoothedLookAtPos, targetLookAtPos, ref lookAtSmoothVel, lookAtSpeed);

            // Look at weight values...
            if( GameManager.Inst.LevelLoaded != GameManager.Level.SCALE_GAME )
            {
                GetComponent<Animator>().SetLookAtPosition(smoothedLookAtPos);
                GetComponent<Animator>().SetLookAtWeight(lookAtWeight, 0.1f, 1f, 0f, 0f);
            }
            // Lookat weight blends out if a custom animation is playing.
            lookAtWeight = Mathf.MoveTowards(lookAtWeight, (GetComponent<AnimatorHelper>().CustomAnimPlaying() && !playerScript.IsSitting )? 0f : 1f, Time.deltaTime * 2f);
        }


        // Closeouts ---------------------------------------------- ||
        // Movement defaults to unlocked.
        lockMovement = false;

        // Clicktomoveinterrupt defaults to false.
        if(ctmiFrames > 0)
            ctmiFrames--;

        turnThrottle = 0f;
    } // End of Update().


    void FixedUpdate(){
        if(usePhysics && grounded)
            rigidbody.AddForce(moveVector * 50f, ForceMode.Force);
    } // End of FixedUpdate().

    void OnCollisionEnter(Collision collision)
    {
        OnCollisionStay(collision);
    } // End of OnCollisionEnter().

    void OnCollisionStay(Collision collision){
        grounded = true;
        rigidbody.drag = 25f;
    } // End of OnCollisionStay().

    void OnCollisionExit(Collision collision){
        grounded = false;
        rigidbody.drag = 0f;
    } // End of OnCollisionExit().


    public void Jump(){
        if (grounded)
            rigidbody.AddForce(gameObject.transform.localScale.x * Vector3.up * 8f, ForceMode.Impulse);
    } // End of Jump().


    public static void ClickToMoveInterrupt(){
        if( GameManager.Inst.LocalPlayer )
            GameManager.Inst.LocalPlayer.playerController.ctmiFrames = 3;
    } // ClickToMoveInterrupt().


    public void LookAtPosition(Vector3 pos){
        lookAtOverride = true;
        lookAtOverridePos = pos;
    } // End of LookAtPosition().


    public void LookAtPlayer(Player player){
        lookAtOverride = true;
        lookAtOverridePlayer = player;
    } // End of LookAtPosition().


    public void LookAtTransform(Transform transform){
        lookAtOverride = true;
        lookAtOverrideTransform = transform;
    } // End of LookAtTransform().


    public void ClearLookAtPosition(){
        lookAtOverridePlayer = null;
        lookAtOverrideTransform = null;
        gazePanTilt = Vector2.zero;
        lookAtOverride = false;
    } // End of LookAtPosition().


    void OnApplicationFocus(){
        ctmiFrames = 3;
    } // End of Application Focus().


    void OnGUI(){
        // Allow user to click off of input fields without moving character.
        if(GUI.GetNameOfFocusedControl().Equals("consoleInput") || GUI.GetNameOfFocusedControl().Equals("presenterInput")){
            ctmiFrames = 3;
        }

        // User inputs "go to that position!"
        if(isLocalPlayer && clickToMove && (ctmiFrames == 0) && goToIndicator.activeSelf){
            if (Input.GetMouseButtonDown(0) && !GameGUI.Inst.IsMouseOverGuiLayerElement && (GameGUI.Inst.guiLayer == null || !GameGUI.Inst.guiLayer.ClickOffElementOpen) && !AnnouncementManager.Inst.IsAnnouncementDisplayed && MouseHelpers.GetCurrentGameObjectHit().GetComponent<CollabBrowserTexture>() == null) {
                followedPlayer = null;
                pathfindingDest.transform.position = goToIndicator.transform.position;
                navAgent.SetDestination(pathfindingDest.transform.position);
                pathfindingActive = true;
            }
        }
    } // End of OnGUI().


    public void SetNavDestination(Vector3 pos, Vector3 targetArrivalForward)
    {
        followedPlayer = null;
        SetNavDestination(pos);
        targetYOnArrival = Quaternion.LookRotation(targetArrivalForward).eulerAngles.y;
        targetArrivalY = true;
    }

    public void SetNavDestination(Vector3 pos, float _targetArrivalY){
        followedPlayer = null;
        SetNavDestination(pos);
        targetYOnArrival = _targetArrivalY;
        targetArrivalY = true;
    }
    public void SetNavDestination(Vector3 pos){
        followedPlayer = null;
        NavMeshHit navHit = new NavMeshHit();
        if (NavMesh.SamplePosition(pos, out navHit, 10f, 1)){
            holdNavDest = navHit.position;
            navDestHeld = true;
        }
    } // End of SetNavDestination().


    public void GoToPlayer(Player player){
        followedPlayer = null;
        NavMeshHit navHit = new NavMeshHit();
        if (NavMesh.SamplePosition(player.gameObject.transform.position, out navHit, 10f, 1)){
            holdNavDest = navHit.position;
            navDestHeld = true;
            targetDistFromDest = 2.5f;
        }
    } // End of GoToPlayer().

    public void FollowPlayer(Player playerToFollow){
        followedPlayer = playerToFollow;
        targetDistFromDest = 2.5f;
    } // End of FollowPlayer().


    public void StopFollowingPlayer(){
        followedPlayer = null;
        pathfindingActive = false;
        targetArrivalY = false;
    } // End of StopFollowingPlayer().


    public void StopFalling(){
        falling = false;
        fallVelocity = Vector3.zero;
        rigidbody.velocity = Vector3.zero;
        navAgent.enabled = true;
        moveVector = Vector3.zero;
        forwardThrottle = 0f;
        turnThrottle = 0f;
        moveSpeed = 0f;
    } // End of StopFalling().


    public void UpdateTransform(Vector3 pos, float newRot){
        navAgent.enabled = false;
        transform.position = pos;
        lastPos = pos;
        forwardAngle = newRot;
        navAgent.enabled = true;
    } // End of UpdateTransform().


    public void Fall(){
        navAgent.enabled = false;
        falling = true;
        fallVelocity = moveVector;
    } // End of Fall().

} // End of PlayerController().