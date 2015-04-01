using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TunnelLight : MonoBehaviour {

    private static HashSet<TunnelLight> all = new HashSet<TunnelLight>();
    public static HashSet<TunnelLight> GetAll() { return all; }
    public static void EnableAll()
    {
        foreach (TunnelLight l in all)
            l.gameObject.SetActive(true);
    }
    public static void DisableAll()
    {
        foreach (TunnelLight l in all)
            l.gameObject.SetActive(false);
    }

	void Awake () {
        if (GameManager.Inst.LevelLoaded != GameManager.Level.MOTION_TEST)
            gameObject.SetActive(false);
        all.Add(this);
	}
	
	void OnDestroy () {
        all.Remove(this);
	}
}
