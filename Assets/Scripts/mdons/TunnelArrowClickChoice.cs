using UnityEngine;
using System.Collections.Generic;

public class TunnelArrowClickChoice : MonoBehaviour {

    static HashSet<TunnelArrowClickChoice> all = new HashSet<TunnelArrowClickChoice>();

    public static void EnableAll()
    {
        foreach (TunnelArrowClickChoice l in all)
            l.gameObject.SetActive(true);
    }

    public static void DisableAll()
    {
        foreach (TunnelArrowClickChoice l in all)
            l.gameObject.SetActive(false);
    }


	void Awake () {
        all.Add(this);
	}

    void OnDestroy()
    {
        all.Remove(this);
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            Debug.LogError("Good Choice");

    }
}
