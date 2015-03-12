using UnityEngine;
using System.Collections;

public class InfoTrigger : MonoBehaviour {

    public string title = "Info";
    public string msg = "";
    public bool showOnce = true;
    private bool hasShown = false;
    private bool disable = false;

    void OnTriggerEnter(Collider other)
    {
        if (!disable && (!showOnce || !hasShown) && GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
        {
            AnnouncementManager.Inst.Announce(title, msg);
            hasShown = true;
        }
    }
}
