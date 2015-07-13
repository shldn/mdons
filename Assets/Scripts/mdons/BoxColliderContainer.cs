using UnityEngine;
using System.Collections;

// Currently this expects a box collider to already be added to the game object.
// It then puts a collider around the existing box collider
public class BoxColliderContainer : MonoBehaviour {

    public float percentOverhang = 0.25f;

	void Awake () {
        BoxCollider currentCollider = gameObject.GetComponent<BoxCollider>();
        GameObject container = new GameObject("Container");

        BoxCollider collider = container.AddComponent<BoxCollider>();
        float widthScale = 1f + percentOverhang;
        collider.size = new Vector3(widthScale * currentCollider.size.x, widthScale * currentCollider.size.y, widthScale * currentCollider.size.z);
        collider.center = currentCollider.center;

        container.transform.parent = transform;
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;
        
	}
	
}
