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
}
