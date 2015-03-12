using UnityEngine;
using System.Collections;

public class CameraHelpers {

    // returns the distance that will fill the screen with the plane widthwise
    static public float GetCameraDistFromPlane(PlaneMesh plane, Camera cam)
    {
        return GetCameraDistFromPlaneHeight(plane.worldHeight, cam.fieldOfView);
    }

    // returns distance the camera needs to be from the plane to fit it's height perfectly in the window
    // note: fieldOfView is vertical
    static public float GetCameraDistFromPlaneHeight(float planeHeight, float fieldOfView)
    {
        return (0.5f * planeHeight) / Mathf.Tan(Mathf.Deg2Rad * 0.5f * fieldOfView);
    }

    // returns distance the camera needs to be from the plane to fit it's width perfectly in the window
    // note: fieldOfView is vertical
    static public float GetCameraDistFromPlaneWidth(float planeWidth, float fieldOfView)
    {
        float heightForPlaneWidth = planeWidth * Screen.height / Screen.width;
        return GetCameraDistFromPlaneHeight(heightForPlaneWidth, fieldOfView);
    }

    // returns minimum distance the camera needs to be from the plane to fit the entire plane in the window
    static public float GetCamDistToFitPlane(float planeWidth, float planeHeight, float fieldOfView)
    {
        float distForWidth = GetCameraDistFromPlaneWidth(planeWidth, fieldOfView);
        float distForHeight = GetCameraDistFromPlaneHeight(planeHeight, fieldOfView);
        return Mathf.Max(distForWidth, distForHeight);
    }
}
