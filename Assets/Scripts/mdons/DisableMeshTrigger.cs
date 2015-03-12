using UnityEngine;
using System.Collections;

public class DisableMeshTrigger : MonoBehaviour {

    public GameObject objectToDisable;
    
    void OnTriggerEnter()
    {
        objectToDisable.SetActive(false);
    }
}

