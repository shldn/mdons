using UnityEngine;
using LSL;

// LSLSender
// Class to send data on the LSL layer
public class LSLSender : MonoBehaviour {
    
    public bool sendLSLData = true;
    liblsl.StreamOutlet outlet;
    string logFileName = "";

    public bool HasConsumers { get { return outlet.have_consumers(); } }

	// Unity calls this when the object is constructed
	void Awake () {
        // create stream info and outlet
        liblsl.StreamInfo info = new liblsl.StreamInfo("TunnelEvents", "Markers", 1, liblsl.IRREGULAR_RATE, liblsl.channel_format_t.cf_float32, "unique_tunnel");
        outlet = new liblsl.StreamOutlet(info);
        System.IO.Directory.CreateDirectory("logs");
        logFileName = "logs/tunneldata-" + System.DateTime.Now.ToString("MM-dd-yy-h-mm-ss") + ".txt";
	}

    public void SendCode(int code, float metaData=0f)
    {
        float[] data = new float[1];
        data[0] = (float)code;
        outlet.push_sample(data);

        if (metaData != 0f)
            WriteLineToTextFile(code.ToString() + "\t" + metaData.ToString());
    }

    public void SendChoice(int choice)
    {
        float[] data = new float[1];
        data[0] = choice;
        outlet.push_sample(data);
    }

    public void SendAngleOffsets(float alloAngleOffset, float egoAngleOffset, float absoluteAngle, int code = 0)
    {
        float[] data = new float[1];
        data[0] = (float)code;
        outlet.push_sample(data);

        string str = "";
        str += code;
        str += "\t";
        str += (alloAngleOffset < egoAngleOffset ? 500f : 600f);
        str += "\t";
        str += alloAngleOffset;
        str += "\t";
        str += egoAngleOffset;
        str += "\t";
        str += absoluteAngle;

        WriteLineToTextFile(str); 
    }

    public void WriteLineToTextFile(string str)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
        {
            file.WriteLine(str);
        }
    }
}
