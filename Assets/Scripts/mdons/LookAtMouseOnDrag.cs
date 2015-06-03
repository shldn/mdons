using UnityEngine;

// Will face the object's forward vector toward the point the mouse intersects with the ground once the object is clicked.
// Expects the ground to be on a layer called "ground"
// Will only rotate the object around the y-axis.

// For better results could use a virtual plane at 45 degree angle, centered at the object
public class LookAtMouseOnDrag : MonoBehaviour {
    public float catchMousePlaneAngle = -20f;
    bool dragging = false;
	void Update () {
        if (dragging)
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 inNormal = (Quaternion.AngleAxis(catchMousePlaneAngle, Camera.main.transform.right) * Vector3.up).normalized;
            Plane mouseColliderPlane = new Plane(inNormal, transform.position);
            float enterDist = 0.0f;
            if (mouseColliderPlane.Raycast(mouseRay, out enterDist))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 hitPoint = mouseRay.GetPoint(enterDist);
                Vector3 newForward = (hitPoint - transform.position);

                // rotate only around the y-axis
                newForward.y = 0.0f;
                if (newForward != Vector3.zero)
                    transform.forward = newForward;
            }
        }
	}

    void OnGUI()
    {
        if (!TunnelGameManager.Inst.UseKeysToChoose)
        {
            if (Event.current != null && Event.current.type == EventType.MouseDown && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
                dragging = true;
            if (Event.current != null && Event.current.type == EventType.MouseUp)
                dragging = false;
        }
    }
}
