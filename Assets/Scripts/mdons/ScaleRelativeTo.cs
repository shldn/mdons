using UnityEngine;
using System.Collections;

public class ScaleRelativeTo : MonoBehaviour {

    public static ScaleRelativeTo Inst = null;

    public GameObject relativeToObj = null;
    public float speed = 0.01f;
    public bool keepYFixed = false;
    public bool constantScaling = false;

    void Awake(){
        Inst = this; 
    }

	void Start () {
        if (relativeToObj == null)
            relativeToObj = GameManager.Inst.LocalPlayer.gameObject;
	}
	

	void Update () {
        if (Input.GetKey(KeyCode.Period) || Input.GetKey(KeyCode.Comma) || constantScaling)
        {
            if (relativeToObj == null)
                relativeToObj = GameManager.Inst.LocalPlayer.gameObject;

            float dir = ( Input.GetKey(KeyCode.Comma) ) ? -1f : 1f;
            float scaleFactor = 1f + dir * Time.deltaTime * speed;
            SetRelativeScale(scaleFactor, relativeToObj.transform.position);

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

    void OnDestroy()
    {
        Inst = null;
    }

    void SetRelativeScale(float scaleFactor, Vector3 relativePos)
    {

        transform.localScale *= scaleFactor;
        Vector3 offset = scaleFactor * (transform.position - relativePos);

        Vector3 newPos = relativePos + offset;
        if (keepYFixed)
            newPos.y = transform.position.y;
        transform.position = newPos;
    }

    public void NormalizeLocalPlayerToWorld()
    {
        float scaleFactor = PlayerController.Local.playerScript.Scale.y / 100f;
        PlayerController.Local.playerScript.Scale = 100f * Vector3.one;
        SetRelativeScale(1f / scaleFactor, GameManager.Inst.LocalPlayer.gameObject.transform.position);
        MainCameraController.Inst.CameraToInitialPos();
    }
}
