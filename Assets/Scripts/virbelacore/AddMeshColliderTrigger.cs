using UnityEngine;
using System.Collections;

public class AddMeshColliderTrigger : MonoBehaviour {

    public Mesh meshToUse = null;
    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject && (gameObject.GetComponent<MeshCollider>() == null || gameObject.GetComponent<MeshCollider>().sharedMesh == null))
        {
            MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
            if( meshCollider == null )
                meshCollider = gameObject.AddComponent<MeshCollider>();
            if (meshToUse != null)
                meshCollider.sharedMesh = meshToUse;
        }
    }
}
