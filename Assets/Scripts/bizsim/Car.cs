using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Car : MonoBehaviour {
    public ProductType type = ProductType.MINI;
    public float animTime = 0.5f;
    public float distDownToStart = 3.0f;
    public float delayBtwnCars = 0.05f; // seconds

    private Vector3 curVel = Vector3.zero;
    private bool animOnStart = false;
    private bool animate = false;
    private Vector3 targetPos = Vector3.zero;
    static Dictionary<ProductType, int> numCarsToAnim = new Dictionary<ProductType, int>();
    private float delay = 0.0f;

    void Awake()
    {
        if (!numCarsToAnim.ContainsKey(type))
            numCarsToAnim.Add(type, 0);
        delay = numCarsToAnim[type] * delayBtwnCars;
        numCarsToAnim[type]++;
    }

    void Start () {
        if (animTime > 0.0f)
        {
            targetPos = transform.position;
            transform.position = transform.position - distDownToStart * Vector3.up;
            animate = animOnStart;
            if (!animOnStart)
                StartCoroutine(StartAnimDelayed(delay));
        }
	}
	
	void Update () {
        if (animate)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref curVel, animTime);
            animate = Vector3.SqrMagnitude(transform.position - targetPos) > 0.0001f;
            if (!animate)
                numCarsToAnim[type]--;
        }
	}

    public IEnumerator StartAnimDelayed(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        animate = true;
    }
}
