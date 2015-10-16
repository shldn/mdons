using UnityEngine;
using System.Collections;

public class ScaleRelativeTo : MonoBehaviour {

    public GameObject relativeToObj = null;
    public float speed = 0.01f;
    public bool keepYFixed = false;
    public bool constantScaling = false;

	void Start () {
        if (relativeToObj == null)
            relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
	}
	

	void Update () {
        if (Input.GetKey(KeyCode.Period) || Input.GetKey(KeyCode.Comma) || constantScaling)
        {
            float dir = ( Input.GetKey(KeyCode.Comma) ) ? -1f : 1f;
            float scaleFactor = 1f + dir * Time.deltaTime * speed;
            transform.localScale *= scaleFactor;
            Vector3 offset = scaleFactor * (transform.position - relativeToObj.transform.position);

            Vector3 newPos = relativeToObj.transform.position + offset;
            if (keepYFixed)
                newPos.y = transform.position.y;
            transform.position = newPos;

            MainCameraController.Inst.updateMethod = UpdateMethod.UPDATE;
			if(ShepardEngine.Inst)
				ShepardEngine.Inst.SetVelocity(-dir);
        }
        if (Input.GetKeyUp(KeyCode.Period) || Input.GetKeyUp(KeyCode.Comma) || constantScaling)
        {
            ScaleGameManager.Inst.PhysicsAdjustment();
            MainCameraController.Inst.updateMethod = UpdateMethod.FIXED_UPDATE;
        }

        if (Input.GetKeyUp(KeyCode.Alpha0))
        {
            if(relativeToObj != gameObject)
                relativeToObj = gameObject;
            else
                relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
        }
	}
}
