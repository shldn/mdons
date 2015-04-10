using UnityEngine;
using System.Collections;

public class TunnelEnvironmentManager : MonoBehaviour {

    public static TunnelEnvironmentManager Inst = null;
    
    GameObject tunnelSpline = null;
    GameObject tunnelDecorator = null;
    public GameObject rotatableArrow = null;

    void Awake()
    {
        Inst = this;

        tunnelSpline = GameObject.Find("TunnelSpline");
        tunnelDecorator = GameObject.Find("TunnelDecorator");
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
    }
}
