using UnityEngine;
using System.Collections;

public class HideLocalPlayerTrigger : MonoBehaviour {

    bool hideOnUpdate = false;
	void OnTriggerEnter () {
        hideOnUpdate = !GameManager.Inst.LocalPlayer.Visible;
        GameManager.Inst.LocalPlayer.Visible = false;
	}

    void Update()
    {
        if (hideOnUpdate && GameManager.Inst.LocalPlayer.Visible)
        {
            GameManager.Inst.LocalPlayer.Visible = false;
            hideOnUpdate = false;
        }

    }
}
