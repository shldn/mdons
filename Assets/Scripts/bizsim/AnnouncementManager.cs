using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Announcement
{
    public Announcement(string title_, string msg_) { title = title_; msg = msg_; }
    public string title;
    public string msg;
}

public class AnnouncementManager {

    private Queue<Announcement> announcements = new Queue<Announcement>();

    private static AnnouncementManager mInstance;
    public static AnnouncementManager Instance
    {
        get {
            if (mInstance == null)
                mInstance = new AnnouncementManager();
            return mInstance;
        }
    }

    public static AnnouncementManager Inst { get { return Instance; } }

    private string GetTitleMsgJSON(string title, string msg)
    {
        return "{\"title\":\"" + title + "\",\"msg\":\"" + msg +"\"}";
    }

    public void Announce(string title, string announcementText)
    {
        if (title == "" && announcementText == "")
            return;
        announcements.Enqueue(new Announcement(title, announcementText));
        if( announcements.Count == 1 )
            DisplayAnnouncement(announcements.Peek());
    }

    private void DisplayAnnouncement(Announcement a)
    {
        string cmd = "showDialog('" + a.title + "','" + a.msg + "');";
        if (!GameGUI.Inst.ExecuteJavascriptOnGui(cmd))
        {
            Debug.LogError("Announcement failed, giving console announcement. " + a.msg);
            string msg = "<b>" + a.title + "</b>" + ((a.title != "") ? ": " : "") + a.msg;
            GameGUI.Inst.WriteToConsoleLog(msg);
        }
    }

    public void AnnouncementClosed()
    {
        if (announcements == null || announcements.Count == 0)
            return;
        announcements.Dequeue();
        if (announcements.Count > 0)
            DisplayAnnouncement(announcements.Peek());
    }

    public void Clear()
    {
        announcements.Clear();
    }

    public bool IsAnnouncementDisplayed { get { return announcements.Count > 0; } }
}
