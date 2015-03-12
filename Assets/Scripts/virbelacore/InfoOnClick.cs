using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class InfoOnClick : WebToolBarItem {

    public string title = "Info";
    public string msg = "";

    void OnGUI()
    {
        if (msg != "" && Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            if (!AnnouncementManager.Inst.IsAnnouncementDisplayed)
            {
                AnnouncementManager.Inst.Announce(title, msg);
                SoundManager.Inst.PlayClick();
            }
        }
    }
}
