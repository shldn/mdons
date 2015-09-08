using UnityEngine;

// ---------------------------------------------------------------------------------- //
// PlayerController.cs
//
// Injects user input to the attached 'PlayerController' script for driving around
//   the local player.
//
// ---------------------------------------------------------------------------------- //

public class PlayerInputManager : MonoBehaviour {


    PlayerController playerController;
    int consecutiveInputCount = 0;
    public bool disableKeyPressMovement = false;

    CustomKey upKey = new CustomKey(new KeyCode[]{KeyCode.UpArrow, KeyCode.W});
    CustomKey downKey = new CustomKey(new KeyCode[]{KeyCode.DownArrow, KeyCode.S});
    CustomKey leftKey = new CustomKey(new KeyCode[]{KeyCode.LeftArrow, KeyCode.A});
    CustomKey rightKey = new CustomKey(new KeyCode[]{KeyCode.RightArrow, KeyCode.D});
    CustomKey jumpKey = new CustomKey(new KeyCode[]{KeyCode.Space});

    float v = 0f;
    float h = 0f;
    bool jump = false;


    // Use this for initialization
	void Start(){
        playerController = gameObject.GetComponent<PlayerController>();
	} // End of Start().


    void OnGUI(){
        upKey.Check();
        downKey.Check();
        leftKey.Check();
        rightKey.Check();
        jumpKey.Check();

        v = 0f;
        h = 0f;

        if (disableKeyPressMovement)
            return;

        if(upKey.down)
            v += 1f;
        if(downKey.down)
            v -= 1f;

        if(leftKey.down)
            h -= 1f;
        if(rightKey.down)
            h += 1f;

        if(jumpKey.down && GameManager.Inst.LevelLoaded != GameManager.Level.MOTION_TEST)
            jump = true;
    }


	void Update(){

        if (GameGUI.Inst.GuiLayerHasInputFocus)
            return;
        bool run = Input.GetKey (KeyCode.LeftShift) | Input.GetKey (KeyCode.RightShift);

        if(jump){
            playerController.Jump();
            jump = false;
        }

        // Overhaul
        if (MainCameraController.Inst.cameraType != CameraType.FIRSTPERSON){
            playerController.forwardThrottle = v;
            playerController.turnThrottle = h;
            playerController.speed = run ? PlayerController.MovementSpeed.run : PlayerController.MovementSpeed.walk;
        }

        if ((v != 0f) || (h != 0f)){
            playerController.pathfindingActive = false;
            playerController.StopFollowingPlayer();
        }

    } // End of Update().

}


public class CustomKey {

    KeyCode[] keyCodes;
    public bool down = false;

    public CustomKey(KeyCode _keyCode){
        keyCodes = new KeyCode[]{_keyCode};
    }

    public CustomKey(KeyCode[] _keyCodes){
        keyCodes = _keyCodes;
    }

    public void Check(){
        Event e = Event.current;
        for(int i = 0; i < keyCodes.Length; i++){
            KeyCode keyCode = keyCodes[i];
            if((e.type == EventType.keyDown) && (Event.current.keyCode == keyCode))
                down = true;
            else if((e.type == EventType.keyUp) && (Event.current.keyCode == keyCode))
                down = false;
        }
    } // End of Check().

} // End of CustomKey.