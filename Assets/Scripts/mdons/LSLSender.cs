using UnityEngine;
using LSL;

// LSLSender
// Class to send data on the LSL layer
// Initial implementation sends player position and rotation data and rotation data for the arrow
public class LSLSender : MonoBehaviour {
    
    public bool sendLSLData = true;
    liblsl.StreamOutlet outlet;

	// Unity calls this when the object is constructed
	void Awake () {
        // create stream info and outlet
        liblsl.StreamInfo info = new liblsl.StreamInfo("TunnelEvents", "Markers", 8, liblsl.IRREGULAR_RATE, liblsl.channel_format_t.cf_float32, "unique_tunnel");
        outlet = new liblsl.StreamOutlet(info);
	}
	
	// This is called at a fixed time step defined in project settings (Usually used for physics updates)
	void FixedUpdate () {

        if( false )
        {
            // initial data to send
            Vector3 playerPos = GameManager.Inst.LocalPlayer.gameObject.transform.position;
            Quaternion playerRot = GameManager.Inst.LocalPlayer.gameObject.transform.rotation;
            float[] data = new float[8];
            data[0] = playerPos.x;
            data[1] = playerPos.z;
            data[2] = playerRot.eulerAngles.x;
            data[3] = playerRot.eulerAngles.y;
            data[4] = playerRot.eulerAngles.z;
            data[5] = 0f;
            data[6] = (float)TunnelGameManager.Inst.lastCode;
            data[7] = (float)TunnelGameManager.Inst.lastChoice;

            outlet.push_sample(data);
        }

	}

    public void SendCode(int code, float metaData=0f)
    {
        float[] data = new float[8];
        data[0] = (float)code;
        data[1] = 0f;
        data[2] = 0f;
        data[3] = 0f;
        data[4] = 0f;
        data[5] = metaData;
        data[6] = 0f;
        data[7] = 0f;
        outlet.push_sample(data);
    }

    public void SendChoice(int choice)
    {
        float[] data = new float[8];
        data[0] = 0f;
        data[1] = (float)choice;
        data[2] = 0f;
        data[3] = 0f;
        data[4] = 0f;
        data[5] = 0f;
        data[6] = 0f;
        data[7] = 0f;
        outlet.push_sample(data);
    }

    public void SendAngleOffsets(float alloAngleOffset, float egoAngleOffset, float absoluteAngle, int code = 0)
    {
        float[] data = new float[8];
        data[0] = (float)code;
        data[1] = alloAngleOffset < egoAngleOffset ? 1f : 2f;
        data[2] = alloAngleOffset;
        data[3] = egoAngleOffset;
        data[4] = absoluteAngle;
        data[5] = 0f;
        data[6] = 0f;
        data[7] = 0f;
        outlet.push_sample(data);
    }
}
