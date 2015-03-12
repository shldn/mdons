using UnityEngine;
using System.Collections;

public class TestScreen : BizSimScreen {

    protected override void Awake()
    {
        base.Awake();
        bssId = CollabBrowserId.TEST;
        disableScrolling = false;
        url = "http://fiddle.jshell.net/9UdnD/3/show/";
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AllowURLChanges = true;
        GameGUI.Inst.presenterToolCollabBrowser = bTex;
    }
}