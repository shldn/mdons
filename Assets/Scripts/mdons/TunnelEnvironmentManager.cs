using UnityEngine;
using System.Collections;

public class TunnelEnvironmentManager : MonoBehaviour {

    public static TunnelEnvironmentManager Inst = null;
    
    GameObject tunnelSpline = null;
    GameObject tunnelDecorator = null;
    GameObject endArrow = null;

    void Awake()
    {
        Inst = this;

        tunnelSpline = GameObject.Find("TunnelSpline");
        tunnelDecorator = GameObject.Find("TunnelDecorator");
        endArrow = GameObject.Find("EndArrow");
    }

    public void Reset()
    {
        tunnelDecorator.SetActive(true);
        endArrow.SetActive(false);
    }
}
