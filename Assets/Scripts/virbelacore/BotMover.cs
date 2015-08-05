using UnityEngine;

//------------------------------------------------------------------------------
// BotMover
//
// Component to make bots move when there is no nav mesh in the scene
// Component expects no obsticles in the way.
// 
//------------------------------------------------------------------------------
public class BotMover : MonoBehaviour {
    public enum MoverStage
    {
        TURN,
        WALK,
        STOP,
    }

    PlayerController playerController = null;
    public Vector3 destination = Vector3.zero;
    public MoverStage stage = MoverStage.TURN;
    Plane destTestPlane = new Plane();

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // Setup plane for testing if the bot has reached the destination
        Vector3 planeNormal = destination - transform.position;
        planeNormal.y = 0;
        destTestPlane = new Plane(planeNormal, destination);

    }
	void Update () {

        switch(stage)
        {
            case MoverStage.TURN:
                float deltaAngle = Mathf.DeltaAngle(playerController.forwardAngle, Quaternion.LookRotation(destination - transform.position).eulerAngles.y);
                playerController.turnThrottle = Mathf.Clamp(deltaAngle * 0.05f, -1f, 1f);
                if (Mathf.Abs(deltaAngle) <= 0.2f)
                    stage = MoverStage.WALK;
                break;
            case MoverStage.WALK:
                playerController.forwardThrottle = 1;
                if (destTestPlane.GetSide(transform.position))
                    stage = MoverStage.STOP;
                break;
            case MoverStage.STOP:
                playerController.forwardThrottle = 0;
                break;
            default:
                Debug.LogError("Unknown stage: " + stage.ToString());
                break;
        }
	}
}
