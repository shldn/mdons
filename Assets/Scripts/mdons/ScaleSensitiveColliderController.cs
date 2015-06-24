using UnityEngine;
using System.Collections;

// Scale Sensitive Collider
//
// Changes the type of collider dynamically based on the local player's relative scale to this object
// When this object is relatively small, use a box collider
// When it gets larger, use a trigger to add a mesh collider.

public class ScaleSensitiveColliderController : MonoBehaviour {

    private bool allowMeshCollisions = false;

	void Start () {
        AllowMeshCollisions = allowMeshCollisions;
	}

	void Update () {
        if (Input.GetKeyUp(KeyCode.Alpha1))
            AllowMeshCollisions = !AllowMeshCollisions;
	}

    public bool AllowMeshCollisions
    {
        get{
            return allowMeshCollisions;
        }

        set
        {
            allowMeshCollisions = value;
            Transform container = transform.FindChild("Container");
            if (container != null )
                container.gameObject.SetActive(!allowMeshCollisions);
            GetComponent<BoxCollider>().enabled = allowMeshCollisions;

        }
    }
}
