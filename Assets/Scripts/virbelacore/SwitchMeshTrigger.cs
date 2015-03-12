using UnityEngine;
using System.Collections;

public class SwitchMeshTrigger : MonoBehaviour {

    public GameObject inTriggerMesh;    // mesh is visible when player is inside the trigger and invisible when player is outside the trigger
    public GameObject outOfTriggerMesh; // mesh is visible when player is outside the trigger and invisible when player is inside the trigger
    public bool setInMeshInvisibleOnStart = false;
    public bool setOutMeshInvisibleOnStart = false;
    void Start()
    {
        if (setInMeshInvisibleOnStart && inTriggerMesh != null)
            inTriggerMesh.SetActive(false);
        if (setOutMeshInvisibleOnStart && outOfTriggerMesh != null)
            outOfTriggerMesh.SetActive(false);
    }
    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
        {
            if( inTriggerMesh != null )
                inTriggerMesh.SetActive(true);
            if (outOfTriggerMesh != null)
                outOfTriggerMesh.SetActive(false);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
        {
            if (inTriggerMesh != null)
                inTriggerMesh.SetActive(false);
            if (outOfTriggerMesh != null)
                outOfTriggerMesh.SetActive(true);
        }
    }
}
