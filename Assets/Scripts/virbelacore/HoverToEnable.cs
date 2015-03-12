using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class HoverToEnable : MonoBehaviour {

    public GameObject objectToEnable;
    public bool disableOnStart = true;

    void Start()
    {
        objectToEnable.SetActive(!disableOnStart);
    }

    void OnGUI()
    {
        if((objectToEnable != null) && ((MouseHelpers.GetCurrentGameObjectHit() == gameObject) != objectToEnable.activeSelf))
            objectToEnable.SetActive(!objectToEnable.activeSelf);
    }
}

