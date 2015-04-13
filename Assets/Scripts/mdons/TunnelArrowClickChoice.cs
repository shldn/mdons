using UnityEngine;
using System.Collections.Generic;

public class TunnelArrowClickChoice : MonoBehaviour {

    static HashSet<TunnelArrowClickChoice> all = new HashSet<TunnelArrowClickChoice>();
    bool selected = true;
    Color selectedColor = new Color(42f/255f, 154f/255f, 5f/255f, 255f/255f);
    Color unSelectedColor = new Color(70f/255f, 94f/255f, 62f/255f, 89f/255f);

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

    public static void ResetAll()
    {
        foreach (TunnelArrowClickChoice l in all)
        {
            l.ReCompute();
            l.Selected = true;
        }
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
        {
            foreach (TunnelArrowClickChoice arrow in all)
                arrow.Selected = (arrow == this);
        }
    }

    virtual public void ReCompute()
    {
    }

    bool Selected
    {
        set
        {
            Color c = value ? selectedColor : unSelectedColor;
            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
                renderers[i].material.color = c;
            selected = value;
        }
    }
}
