using UnityEngine;
using System.Collections;

public class DragToRotate : MonoBehaviour {

    public Vector3 rotateAxis = Vector3.forward;
    public float speed = 30.0f;
    private bool dragging = false;

    void Update()
    {
        if( dragging )
            transform.Rotate(rotateAxis, speed * Time.deltaTime);
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseDown && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            dragging = true;
        if (Event.current != null && Event.current.type == EventType.MouseUp)
            dragging = false;

    }
}
