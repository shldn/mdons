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
        liblsl.StreamInfo info = new liblsl.StreamInfo("BioSemi", "EEG", 8, 100, liblsl.channel_format_t.cf_float32, "sddsfsdf");
        outlet = new liblsl.StreamOutlet(info);
	}
	
	// This is called at a fixed time step defined in project settings (Usually used for physics updates)
	void FixedUpdate () {

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

    public void SendCode(int code)
    {
        //outlet.push_sample(data);
    }

    public void SendChoice(int choice)
    {
        //outlet.push_sample(data);
    }
}
