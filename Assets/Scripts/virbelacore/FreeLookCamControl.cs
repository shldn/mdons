using UnityEngine;
using System.Collections;


public class FreeLookCamControl : MonoBehaviour {

    public static float sensitivityX = 0.5F;
    public static float sensitivityY = 0.2F;

    float mHdg = 0F;
    float mPitch = 0F;

    void Start()
    {
		// application controls for now, need to move to a more clear place. 
		Application.runInBackground = true;
    }

    void Update()
    {
		
    	Vector3 movement = Vector3.zero;
		float speed = 5.0f;
		bool hitShift = Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift );
		if( !hitShift )
		{
			movement.y = Input.GetAxis("Vertical");
    		movement.x = Input.GetAxis("Horizontal");
		}
		else
		{
			// holding shift causes vertical movement with up/down arrows & rotation with side arrows
			movement.z = Input.GetAxis("Vertical");
			
			float horizMovement = Input.GetAxis("Horizontal");
			if( horizMovement != 0 )
				transform.RotateAroundLocal(Vector3.up, horizMovement / 25.0F);
		}
		
		if( movement != Vector3.zero )
    		transform.Translate(movement * speed * Time.deltaTime, Space.Self);
		
		// hold alt for mouse button based movement
        if (!(Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) || !(Input.GetMouseButton(0) || Input.GetMouseButton(1)))
            return;

        float deltaX = Input.GetAxis("Mouse X") * sensitivityX;
        float deltaY = Input.GetAxis("Mouse Y") * sensitivityY;

        if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
        {
            Strafe(deltaX);
            ChangeHeight(deltaY);
        }
        else
        {
            if (Input.GetMouseButton(0))
            {
                MoveForwards(deltaY);
                ChangeHeading(deltaX);
            }
            else if (Input.GetMouseButton(1))
            {
                ChangeHeading(deltaX);
                ChangePitch(-deltaY);
            }
        }
    }

    void MoveForwards(float aVal)
    {
        Vector3 fwd = transform.forward;
        fwd.y = 0;
        fwd.Normalize();
        transform.position += aVal * fwd;
    }

    void Strafe(float aVal)
    {
        transform.position += aVal * transform.right;
    }

    void ChangeHeight(float aVal)
    {
        transform.position += aVal * Vector3.up;
    }

    void ChangeHeading(float aVal)
    {
        mHdg += aVal;
        WrapAngle(ref mHdg);
        transform.localEulerAngles = new Vector3(mPitch, mHdg, 0);
    }

    void ChangePitch(float aVal)
    {
        mPitch += aVal;
        WrapAngle(ref mPitch);
        transform.localEulerAngles = new Vector3(mPitch, mHdg, 0);
    }

    public static void WrapAngle(ref float angle)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
    }
}