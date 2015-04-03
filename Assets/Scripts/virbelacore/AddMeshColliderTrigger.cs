using UnityEngine;
using System.Collections;

public class AddMeshColliderTrigger : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject && gameObject.GetComponent<MeshCollider>() == null)
            gameObject.AddComponent<MeshCollider>();
    }
}
