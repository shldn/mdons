using UnityEngine;
using System.Collections;

// Currently this expects a box collider to already be added to the game object.
// It then puts a roof on top of the existing box collider
public class BoxColliderRoof : MonoBehaviour {

    public float percentOverhang = 0.25f;

	void Start () {
        BoxCollider currentCollider = gameObject.GetComponent<BoxCollider>();
        GameObject roof = new GameObject("Roof");

        BoxCollider collider = roof.AddComponent<BoxCollider>();
        float widthScale = 1f + percentOverhang;
        collider.size = new Vector3(widthScale * currentCollider.size.x, percentOverhang * currentCollider.size.y, widthScale * currentCollider.size.z);
        collider.center = currentCollider.center + 0.5f * (currentCollider.size.y + collider.size.y) * Vector3.up;

        roof.transform.parent = transform;
        roof.transform.localPosition = Vector3.zero;
        roof.transform.localRotation = Quaternion.identity;
        roof.transform.localScale = Vector3.one;
        
	}
	
}
