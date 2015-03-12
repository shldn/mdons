using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class ClickToEnable : MonoBehaviour {

    public GameObject objectToEnable;
    public bool disableOnStart = true;

    void Start()
    {
        objectToEnable.SetActive(!disableOnStart);
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            objectToEnable.SetActive(!objectToEnable.activeSelf);
    }
}
