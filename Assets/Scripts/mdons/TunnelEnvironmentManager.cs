using UnityEngine;
using System.Collections;

public class TunnelEnvironmentManager : MonoBehaviour {

    public static TunnelEnvironmentManager Inst = null;
    
    GameObject tunnelSpline = null;
    GameObject tunnelDecorator = null;
    GameObject tunnelTriggerDecorator = null;
    public GameObject rotatableArrow = null;
    public GUISkin guiSkin = null;

    void Awake()
    {
        Inst = this;

        tunnelSpline = GameObject.Find("TunnelSpline");
        tunnelDecorator = GameObject.Find("TunnelDecorator");
        tunnelTriggerDecorator = GameObject.Find("TunnelTriggerDecorator");
        rotatableArrow = GameObject.Find("EndArrow");
    }

    void Start()
    {
        // Set initial state
        Reset();
    }

    public void Reset()
    {
        TunnelLight.EnableAll();
        TunnelArrowClickChoice.DisableAll();
        tunnelDecorator.SetActive(true);
        rotatableArrow.SetActive(false);
    }

    public void ReCompute()
    {
        tunnelSpline.GetComponent<SplineToTunnelTurn>().ReCompute();
        tunnelDecorator.GetComponent<SplineDecorator>().ReDecorate();
        tunnelTriggerDecorator.GetComponent<SplinePtDecorator>().ReDecorate();
        SetPositionRelativeToSplineEnd.ResetAll();
        TunnelArrowClickChoice.ResetAll(); 
    }

    public void SetTunnelAngle(float angle)
    {
        tunnelSpline.GetComponent<SplineToTunnelTurn>().angle = angle;
        ReCompute();
    }

    public float GetTunnelAngle()
    {
        return tunnelSpline.GetComponent<SplineToTunnelTurn>().angle;
    }

    public Vector3 EndTunnelDirection()
    {
        return tunnelSpline.GetComponent<BezierSpline>().GetDirection(1.0f).normalized;
    }

    public Vector3 StartTunnelDirection()
    {
        return tunnelSpline.GetComponent<BezierSpline>().GetDirection(0.0f).normalized;
    }

    public Vector3 StartTunnelPosition()
    {
        return tunnelSpline.GetComponent<BezierSpline>().GetPoint(0.0f);
    }

    public Vector3 EndTunnelPosition()
    {
        return tunnelSpline.GetComponent<BezierSpline>().GetPoint(1.0f);
    }
}
