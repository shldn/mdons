using UnityEngine;
using System.Collections;

public class CameraFollowOrbitController : MonoBehaviour {

    static CameraFollowOrbitController mInst = null;
    public static CameraFollowOrbitController Inst{
        get{
            if (mInst == null)
                mInst = MainCameraController.Inst.gameObject.AddComponent<CameraFollowOrbitController>();
            return mInst;
        }
    }

    // Lerp variables
    float duration = 0.0f;
    float startTime = 0.0f;
    float startAngle = 0.0f;
    float endAngle = 0.0f;
    bool doSnapStep = false;
    bool cancelling = false;
    bool cancelled = true;

    // Change Distance
    bool lerpDistance = false;
    float startDistance = 0.0f;
    float endDistance = 0.0f;


    public bool IsMoving { get { return duration >= Time.time - startTime || endAngle != MainCameraController.Inst.orbitOffsetAngle; } }
    public bool IsCancelling { get { return cancelling; } }
    public bool IsLerpingDistance { get { return lerpDistance; } }
    public bool InStartPosition { get { return cancelled; } }
    public float DestinationAngle { get { return endAngle; } }
    public float StartDistance { get { return startDistance; } }
    public float TimeSinceLastOrbit { get { return Time.time - startTime; } }
 
 
    void Update() {

        if (IsMoving)
        {
            float t = (Time.time - startTime) / duration;
            MainCameraController.Inst.orbitOffsetAngle = Mathf.Lerp(startAngle, endAngle, Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(t)));
            if( lerpDistance )
                MainCameraController.Inst.followPlayerDistance = Mathf.Lerp(startDistance, endDistance, Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(t)));
        }
        else if (doSnapStep)
        {
            MainCameraController.Inst.orbitOffsetAngle = endAngle = ((endAngle + 720) % 360);
            doSnapStep = false;
            if (cancelling)
            {
                cancelled = true;
                lerpDistance = false;
            }
            cancelling = false;
        }
    }

    public void Orbit(float destinationAngle, float secondsToGetThere, float newDistance)
    {
        Orbit(destinationAngle, secondsToGetThere);

        lerpDistance = true;
        startDistance = MainCameraController.Inst.followPlayerDistance;
        endDistance = newDistance;
    }

	public void Orbit(float destinationAngle, float secondsToGetThere)
    {
        startAngle = MainCameraController.Inst.orbitOffsetAngle;
        endAngle = destinationAngle;
        startTime = Time.time;
        duration = secondsToGetThere;
        doSnapStep = true;
        cancelled = false;
        cancelling = false;
    }

    public void CancelOrbit(float secondsToGetThere, bool backToZero = true)
    {
        float desiredDest = backToZero ? 0.0f : startAngle;
        if (lerpDistance)
            Orbit(ClosestDestination(MainCameraController.Inst.orbitOffsetAngle, desiredDest), secondsToGetThere, startDistance);
        else
            Orbit(ClosestDestination(MainCameraController.Inst.orbitOffsetAngle, desiredDest), secondsToGetThere);

        cancelling = true;
    }

    // find shortest path from current position to desired destination
    public float ClosestDestination(float current, float desiredDest)
    {
        if( Mathf.Abs(current - desiredDest ) <= 180 )
            return desiredDest;
        else
        {
            if (current >= desiredDest)
                return desiredDest + 360;
            return desiredDest - 360;
        }
    }

    void OnDestroy()
    {
        mInst = null;
    }

}

