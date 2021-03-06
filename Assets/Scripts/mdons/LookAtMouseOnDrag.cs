﻿using UnityEngine;

// Will face the object's forward vector toward the point the mouse intersects with the ground once the object is clicked.
// Expects the ground to be on a layer called "ground"
// Will only rotate the object around the y-axis.

// For better results could use a virtual plane at 45 degree angle, centered at the object
public class LookAtMouseOnDrag : MonoBehaviour {

    bool dragging = false;
	void Update () {
        if (dragging)
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit, Mathf.Infinity, 1 << LayerMask.NameToLayer("ground")))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newForward = (hit.point - transform.position);

                // rotate only around the y-axis
                newForward.y = 0.0f;
                if (newForward != Vector3.zero)
                    transform.forward = newForward;
            }

        }
	}

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseDown && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            dragging = true;
        if (Event.current != null && Event.current.type == EventType.MouseUp)
            dragging = false;

    }
}
