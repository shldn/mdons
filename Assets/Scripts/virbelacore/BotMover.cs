using UnityEngine;
using System.Collections.Generic;

//------------------------------------------------------------------------------
// BotMover
//
// Component to make bots move when there is no nav mesh in the scene
// Component expects no obsticles in the way.
//
// Keep internal positions in local space so containers can be scaled without affecting bot movers 
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
    private Vector3 localDestination = Vector3.zero;
    private List<Vector3> localDestinationSet = new List<Vector3>();
    private int destinationSetIdx = 0;
    public MoverStage stage = MoverStage.TURN;

    public Vector3 Destination { set { localDestination = transform.parent.InverseTransformPoint(value); stage = MoverStage.TURN; } get { return transform.parent.TransformPoint(localDestination); } }
    public Vector3 LocalDestination { set { localDestination = value; stage = MoverStage.TURN;  } }

    // if a destination set is set, the bot will loop between the positions.
    public List<Vector3> DestinationSet { set { SetLocalDestinationSet(value); destinationSetIdx = 0; LocalDestination = localDestinationSet[destinationSetIdx]; } }


    // Implementation Helpers
    float sqDistToTravel = 0f;
    float lastSqDist = 999999999f;

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

	void Update () {


        switch(stage)
        {
            case MoverStage.TURN:
                if (localDestination == transform.localPosition)
                    stage = MoverStage.END;
                else
                {
                    float deltaAngle = Mathf.DeltaAngle(playerController.forwardAngle, Quaternion.LookRotation(localDestination - transform.localPosition).eulerAngles.y);
                    playerController.turnThrottle = Mathf.Clamp(deltaAngle * 0.05f, -1f, 1f);
                    if (Mathf.Abs(deltaAngle) <= 0.2f)
                    {
                        sqDistToTravel = SqrMagnitude2D(transform.localPosition - localDestination);
                        lastSqDist = SqrMagnitude2D(transform.localPosition - localDestination) + transform.parent.lossyScale.x * 100f;
                        stage = MoverStage.WALK;
                    }
                }
                break;
            case MoverStage.WALK:
                float currSqDist = SqrMagnitude2D(transform.localPosition - localDestination);
                if(currSqDist > 0.001f)
                {
                    playerController.forwardThrottle = 1;
                    float deltaAngle2 = Mathf.DeltaAngle(playerController.forwardAngle, Quaternion.LookRotation(localDestination - transform.localPosition).eulerAngles.y);
                    playerController.turnThrottle = Mathf.Clamp(deltaAngle2 * 0.05f, -1f, 1f);
                }
                
                // Had trouble at start if just checking curr > last.
                if (currSqDist < 0.25f * sqDistToTravel && currSqDist > lastSqDist)
                    stage = MoverStage.END;
                lastSqDist = currSqDist;
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

    float SqrMagnitude2D(Vector3 diffVector)
    {
        diffVector.y = 0f;
        return diffVector.sqrMagnitude;
    }

    void SetNextDestination()
    {
        if(localDestinationSet.Count > 1)
        {
            destinationSetIdx = (destinationSetIdx + 1) % localDestinationSet.Count;
            LocalDestination = localDestinationSet[destinationSetIdx];
        }
    }

    void SetLocalDestinationSet(List<Vector3> worldDestinationSet)
    {
        for(int i=0; i < worldDestinationSet.Count; ++i)
            localDestinationSet.Add(transform.parent.InverseTransformPoint(worldDestinationSet[i]));
    }
}
