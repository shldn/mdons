using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TunnelLight : MonoBehaviour {

    private static HashSet<TunnelLight> all = new HashSet<TunnelLight>();
    public static HashSet<TunnelLight> GetAll() { return all; }
    public static void DisableAll()
    {
        foreach (TunnelLight l in all)
            l.gameObject.SetActive(false);
    }

	void Awake () {
        all.Add(this);
	}
	
	void OnDestroy () {
        all.Remove(this);
	}
}
