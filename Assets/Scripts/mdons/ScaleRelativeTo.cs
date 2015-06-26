using UnityEngine;
using System.Collections;

public class ScaleRelativeTo : MonoBehaviour {

    public GameObject relativeToObj = null;
    public float speed = 0.01f;
    public bool keepYFixed = false;

	void Start () {
        if (relativeToObj == null)
            relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
	}
	

	void Update () {
	    if(Input.GetKey(KeyCode.Period) || Input.GetKey(KeyCode.Comma))
        {
            float dir = ( Input.GetKey(KeyCode.Comma) ) ? -1f : 1f;
            float scaleFactor = 1f + dir * Time.deltaTime * speed;
            transform.localScale *= scaleFactor;
            Vector3 offset = scaleFactor * (transform.position - relativeToObj.transform.position);

            Vector3 newPos = relativeToObj.transform.position + offset;
            if (keepYFixed)
                newPos.y = transform.position.y;
            transform.position = newPos;

			if(ShepardEngine.Inst)
				ShepardEngine.Inst.velocity = -dir;
        }
        if(Input.GetKeyUp(KeyCode.Period) || Input.GetKeyUp(KeyCode.Comma))
            ScaleGameManager.Inst.PhysicsAdjustment();

        if (Input.GetKeyUp(KeyCode.Alpha0))
        {
            if(relativeToObj != gameObject)
                relativeToObj = gameObject;
            else
                relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
        }
	}
}
