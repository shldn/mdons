using UnityEngine;

public class EnableMeshTrigger : MonoBehaviour {

    public GameObject objectToEnable;
    public bool disableOnStart = true;

	void Start () {
        objectToEnable.SetActive(!disableOnStart);
	}

    void OnTriggerEnter()
    {
        objectToEnable.SetActive(true);
    }
}
