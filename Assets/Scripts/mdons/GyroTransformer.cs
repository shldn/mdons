using UnityEngine;
using System.Collections;

public class GyroTransformer : MonoBehaviour {
    public enum GyroControl
    {
        ROTATION,
        MOVEMENT,
    }

    public GyroControl gyroControl = GyroControl.ROTATION;
    public int rotateSpeed = 1;
    public int movementSpeed = 100;
    int lastGyroFrameID = -1;
    string guiStr = "";


	// Use this for initialization
	void Start () {
        LSLManager.StartReceivingData();
	}
	
	// Update is called once per frame
	void Update () {
        float rotationX = 0f; 
        float rotationY = 0f;
        if (LSLManager.HasNewGyroData(lastGyroFrameID))
        {
            rotationX = LSLManager.GyroOffsetX;
            rotationY = LSLManager.GyroOffsetY;
        }
        lastGyroFrameID = LSLManager.GyroFrameID;
        guiStr = "rot: " + rotationX + ", " + rotationY;
        if (gyroControl == GyroControl.ROTATION)
        {
            rotationX = (rotationX > 0) ? Mathf.Clamp(rotationX, 0f, 300f) : Mathf.Clamp(rotationX, -300f, 0f);
            rotationY = (rotationY > 0) ? Mathf.Clamp(rotationY, 0f, 300f) : Mathf.Clamp(rotationY, -300f, 0f);
            transform.Rotate(new Vector3(rotationY, rotationX, 0) * rotateSpeed * Time.deltaTime, Space.World);
        }
        else if (gyroControl == GyroControl.MOVEMENT)
        {
            Transform playerTransform = GameManager.Inst.LocalPlayer.gameObject.transform;
            transform.position = transform.position + movementSpeed * Time.deltaTime * (rotationX * playerTransform.right - rotationY * playerTransform.up);
            GameManager.Inst.LocalPlayer.playerController.LookAtTransform(transform);
        }
	}

    void OnGUI()
    {
        GUILayout.Label(guiStr);
    }
}
