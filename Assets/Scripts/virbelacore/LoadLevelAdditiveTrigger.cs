using UnityEngine;
using System.Collections;

public class LoadLevelAdditiveTrigger : MonoBehaviour {

    public string levelName;
    bool loadStarted = false;

    void OnTriggerEnter(Collider collider)
    {
        if(collider.gameObject == GameManager.Inst.LocalPlayer.gameObject){
            if( !loadStarted )
                Application.LoadLevelAdditiveAsync(levelName);
            loadStarted = true;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
