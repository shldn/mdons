using UnityEngine;
using System.Collections.Generic;

public class Factory : MonoBehaviour {

    private static List<Factory> factories = new List<Factory>();
    public static List<Factory> GetAll() { return factories; }
    private bool polluting = false;
    private List<GameObject> cleanParticleSystems = new List<GameObject>();
    private List<GameObject> pollutingParticleSystems = new List<GameObject>();
    private bool animate = false;
    private Vector3 targetPos = Vector3.zero;
    public float animTime = 0.5f;
    public float distDownToStart = 3.0f;
    private Vector3 curVel = Vector3.zero;

	void Awake () {
        factories.Add(this);
        
        int childCount = gameObject.transform.childCount; // might change mid-loop, don't want to create an infinite loop.
        for (int i = 0; i < childCount; ++i)
        {
            Transform particleSysTransform = gameObject.transform.GetChild(i);
            if (particleSysTransform.gameObject.name == "SmokeParticle")
            {
                if (QualitySettings.GetQualityLevel() == 0)
                    Destroy(particleSysTransform.gameObject);
                else
                {
                    cleanParticleSystems.Add(particleSysTransform.gameObject);
                    GameObject newParticleSystem = (GameObject)GameObject.Instantiate(Resources.Load("factory/DarkSmokeParticle"));
                    newParticleSystem.name = "zDarkSmoke"; // make sure the name is lexically after "SmokeParticle" -- hacky solution :(

                    newParticleSystem.transform.parent = this.gameObject.transform;
                    newParticleSystem.transform.position = particleSysTransform.position;
                    newParticleSystem.transform.rotation = particleSysTransform.rotation;
                    newParticleSystem.transform.localScale = particleSysTransform.localScale;
                    pollutingParticleSystems.Add(newParticleSystem);
                    newParticleSystem.SetActive(false);
                }
            }
        }
	}

    void Start()
    {
        if (animTime > 0.0f)
        {
            targetPos = transform.position;
            transform.position = transform.position - distDownToStart * Vector3.up;
            animate = true;
        }
    }

    void Update()
    {
        if (animate)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref curVel, animTime);
            animate = Vector3.SqrMagnitude(transform.position - targetPos) > 0.0001f;
        }
    }

    void OnDestroy() {
        factories.Remove(this);
    }

    public bool Polluting
    {
        get { return polluting; }
        set
        {
            if (polluting == value)
                return;
            polluting = value;
            for (int i = 0; i < cleanParticleSystems.Count; ++i)
                cleanParticleSystems[i].SetActive(!polluting);
            for (int i = 0; i < pollutingParticleSystems.Count; ++i)
                pollutingParticleSystems[i].SetActive(polluting);
        }
    }
}
