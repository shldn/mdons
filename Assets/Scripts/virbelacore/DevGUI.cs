using UnityEngine;
using System.Collections;

public class DevGUI : MonoBehaviour {

    public static DevGUI Inst = null;

    // FPS variables ------------------------------------------- ||
    public float updateInterval = 0.5F;
    private float accum = 0; // FPS accumulated over the interval
    private int frames = 0; // Frames drawn over the interval
    private float timeleft; // Left time for current interval
    private string formatedFPS = "";

    

	public void DrawGUI (int x, int y) {
        DrawFPSCounter();
	}

    void DrawFPSCounter()
    {
        if (this.enabled)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 170 - 20 - 80, Screen.height - 20, 70, 20));
            GUILayout.Box(formatedFPS);
            GUILayout.EndArea();
        }
    }

    void Start()
    {
        Inst = this;
        DontDestroyOnLoad(this);

        timeleft = updateInterval;
    }

    void Update()
    {
        //FPS code
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Interval ended - update start new interval
        if (timeleft <= 0.0)
        {
            // display two fractional digits (f2 format)
            float fps = accum / frames;
            formatedFPS = System.String.Format("{0:F2} FPS", fps);
            timeleft = updateInterval;
            accum = 0.0F;
            frames = 0;
        }
    }
}
