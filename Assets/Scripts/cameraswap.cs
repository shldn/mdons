using UnityEngine;
using System.Collections;

public class cameraswap : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GameObject.Find("minimapcam1").GetComponent("Camera").active = false;
        GameObject.Find("minimapcam2").GetComponent("Camera").active = true;
	
	}
	
	
	// Update is called once per frame
	void Update () {
	
	}
}
