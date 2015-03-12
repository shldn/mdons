using UnityEngine;
using System.Collections;

public class HandleTunnelEndTrigger : MonoBehaviour
{
	void OnTriggerEnter () {
        TunnelLight.DisableAll();
	}
}
