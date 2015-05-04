using UnityEngine;
using System.Collections;

public class ScaleRelativeTo : MonoBehaviour {

    public GameObject relativeToObj = null;
    public float speed = 0.01f;
	// Use this for initialization
	void Start () {
        if (relativeToObj == null)
            relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
	}
	
	// Update is called once per frame
	void Update () {
	    if(Input.GetKey(KeyCode.Period) || Input.GetKey(KeyCode.Comma))
        {
            float dir = ( Input.GetKey(KeyCode.Comma) ) ? -1f : 1f;
            float scaleFactor = 1f + dir * speed;
            transform.localScale *= scaleFactor;
            Vector3 offset = scaleFactor * (transform.position - relativeToObj.transform.position);
            transform.position = relativeToObj.transform.position + offset;
        }
	}
}
