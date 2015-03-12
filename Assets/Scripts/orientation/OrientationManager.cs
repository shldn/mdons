using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;

public class OrientationManager : MonoBehaviour
{
    private SlidePresenter slidePresenter = null;
    public SlidePresenter Presenter
    {
        get
        {
            if (slidePresenter == null)
                slidePresenter = (SlidePresenter)FindObjectOfType(typeof(SlidePresenter));
            return slidePresenter;
        }
    }

   	private static OrientationManager mInstance;
    public static OrientationManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("OrientationManager")).AddComponent(typeof(OrientationManager)) as OrientationManager;
            return mInstance;
        }
    }

    public static OrientationManager Inst
    {
        get { return Instance; }
    }

    public void Touch() { }

    public void OnRoomJoin(BaseEvent evt)
    {
        Debug.Log("Successfully joined room: " + (evt != null ? ((Sfs2X.Entities.Room)evt.Params["room"]).Name : "Unknown"));

        if (Presenter != null)
            GameGUI.Inst.presenterToolCollabBrowser = Presenter.browserTexture;
    }
}
