using UnityEngine;
using System.Collections.Generic;

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
        END,
    }

    PlayerController playerController = null;
    private Vector3 destination = Vector3.zero;
    private List<Vector3> destinationSet = new List<Vector3>();
    private int destinationSetIdx = 0;
    public MoverStage stage = MoverStage.TURN;
    Plane destTestPlane = new Plane();

    public Vector3 Destination { set { destination = value; stage = MoverStage.TURN;  SetupEndTest(); } }

    // if a destination set is set, the bot will loop between the positions.
    public List<Vector3> DestinationSet{ set{destinationSet = value; destinationSetIdx = 0; Destination = destinationSet[destinationSetIdx];} }

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        SetupEndTest();
    }

    void SetupEndTest()
    {
        // Setup plane for testing if the bot has reached the destination
        Vector3 planeNormal = destination - transform.position;
        planeNormal.y = 0;
        if (planeNormal == Vector3.zero)
            Debug.LogError("SetupEndTest Warning: plane normal is zero!");
        destTestPlane = new Plane(planeNormal.normalized, destination);
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
                    stage = MoverStage.END;
                break;
            case MoverStage.END:
                playerController.forwardThrottle = 0;
                SetNextDestination();
                break;
            default:
                Debug.LogError("Unknown stage: " + stage.ToString());
                break;
        }
	}

    void SetNextDestination()
    {
        if(destinationSet.Count > 1)
        {
            destinationSetIdx = (destinationSetIdx + 1) % destinationSet.Count;
            Destination = destinationSet[destinationSetIdx];
        }
    }
}
