using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Light))]
public class LightDimmer : MonoBehaviour {

    public static List<LightDimmer> allDimmers = new List<LightDimmer>();
    private Light light;

    public bool animIntensity = true;
    public float lowIntensity = 0.0f;
    public float highIntensity = 1.0f;
    public bool animRange = true;
    public float lowRange = 0.0f;
    public float highRange = 1.0f;
    private float intensityTarget = 0.0f;
    private float rangeTarget = 0.0f;
    private float animTimer = 0.0f;
    private float timeToAnim = -1.0f;

	void Awake () {
        allDimmers.Add(this);
        light = GetComponent<Light>();
	}

    void Update()
    {
        if (animTimer <= timeToAnim)
        {
            animTimer += Time.deltaTime;
            float t = animTimer / timeToAnim;
            if( animTimer > timeToAnim )
            {
                t = 1.0f;
                timeToAnim = -1.0f;
            }
            if( animIntensity )
                light.intensity = Mathf.Lerp(light.intensity, intensityTarget, t);
            if( animRange )
                light.range = Mathf.Lerp(light.range, rangeTarget, t);
        }
    }
	
	void OnDestroy () {
        allDimmers.Remove(this);
	}

    public void LightsUp(float secondsToAnimate = 1.0f)
    {
        timeToAnim = secondsToAnimate;
        animTimer = 0.0f;

        intensityTarget = highIntensity;
        rangeTarget = highRange;
    }

    public void LightsDown(float secondsToAnimate = 1.0f)
    {
        timeToAnim = secondsToAnimate;
        animTimer = 0.0f;

        intensityTarget = lowIntensity;
        rangeTarget = lowRange;
    }
}
