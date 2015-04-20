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

    virtual protected void ChooseThis()
    {
        foreach (TunnelArrowClickChoice arrow in all)
            arrow.Selected = (arrow == this);
    }

    virtual protected void OnGUI()
    {
        if (!TunnelGameManager.Inst.UseMouseButtonsToChoose && Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            ChooseThis();

        GUILayout.BeginArea(new Rect(0.25f * Screen.width, 0.25f * Screen.height, 0.5f * Screen.width, 0.5f * Screen.height));
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.clipping = TextClipping.Overflow;
            GUI.skin.label.fontSize = Mathf.CeilToInt(Screen.height * 0.05f);
            GUILayout.Space(Mathf.Max(Screen.height * 0.005f, 4));
            GUILayout.Label("Where did the tunnel start?");
            GUI.skin.label.fontSize = Mathf.CeilToInt(Screen.height * 0.02f);
            if( !TunnelGameManager.Inst.UseMouseButtonsToChoose)
                GUILayout.Label("(Click the arrow to choose)");
            else
            {
                GUILayout.Space(Mathf.Max(Screen.height * 0.005f, 4));
                GUILayout.Label("(Click left mouse button for left arrow | Click right mouse button for right arrow)");
            }
            GUI.skin.label.fontSize = 12;
        GUILayout.EndArea();
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
            if( selected )
                TunnelGameManager.Inst.RegisterEvent(TunnelEvent.DECISION);
        }
    }
}
