using UnityEngine;

public class RemoveMeshColliderTrigger : MonoBehaviour
{
    public Mesh meshToUse = null;
    float delaySeconds = 1;

    void OnTriggerExit(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
            Invoke("RemoveMeshCollider", delaySeconds);
    }

    void OnTriggerEnter()
    {
        CancelInvoke();
    }

    void RemoveMeshCollider()
    {
        Destroy(gameObject.GetComponent<MeshCollider>());
    }
}
