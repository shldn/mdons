using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Awesomium.Mono;

public class SliderInfo
{
    public SliderInfo(string divName_, int id_, string val_)
    {
        divName = divName_;
        val = val_;
        id = id_;
    }

    public string divName;
    public string val;
    public int id;
}
public class StrategyScreen : BizSimScreen {

    private List<SliderInfo> sliderInfoList = new List<SliderInfo>(){   new SliderInfo("sl0base", 1, ""), 
                                                                        new SliderInfo("sl1base", 2, ""),
                                                                        new SliderInfo("sl2base", 4, ""),
                                                                        new SliderInfo("sl3base", 8, "") };
    int dirtyFlags = 0;
    private float[] waitTime = { 0.025f, 1.0f }; // check close to immediately and then after some time in case the javascript values didn't update immediately
    private bool checkNextMouseUp = false;
    public static StrategyScreen mInst = null;
    public static StrategyScreen Inst { get{ return mInst; } }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 10;
        bssId = CollabBrowserId.STRATEGY;
        url = BizSimScreen.GetStageItemURL(stageItem);
        mInst = this;
    }

    public override void Initialize()
    {
        base.Initialize();
        if (bTex.IsLoaded)
            InitializeSliderVals();
        else
            bTex.AddLoadCompleteEventListener(OnLoadComplete);
    }

    private void InitializeSliderVals()
    {
        UpdateSliderVals();
    }

    private void UpdateSliderVals()
    {
        for(int i=0; i < sliderInfoList.Count; ++i)
            sliderInfoList[i].val = UpdateSliderVal(sliderInfoList[i]);
    }

    private string UpdateSliderVal(SliderInfo sliderInfo)
    {
        string cmd = GetSliderPositionCmd(sliderInfo.divName);
        string newVal = GetCmdStrResult(cmd);
        if( sliderInfo.val != newVal )
            dirtyFlags |= sliderInfo.id;
        return newVal;
    }

    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        InitializeSliderVals();
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseDown && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
            checkNextMouseUp = true;

        if (Event.current != null && Event.current.type == EventType.MouseUp && checkNextMouseUp)
        {
            StartCoroutine(HandleMouseUpDelayed(waitTime[0], waitTime[1]));
            checkNextMouseUp = false;
        }
    }

    // if the webview is busy (loading a page), then both checks get canceled (industry masters server has latest info, so a refresh will give us that info)
    private IEnumerator HandleMouseUpDelayed(float waitTime1, float waitTime2)
    {
        yield return new WaitForSeconds(waitTime1);
        if (!bTex.isWebViewBusy())
        {
            dirtyFlags = 0;
            UpdateSliderVals();

            if (dirtyFlags != 0)
            {
                for(int i=0; i < sliderInfoList.Count; ++i)
                    if ((dirtyFlags & sliderInfoList[i].id) != 0)
                        UpdateServerWithNewSliderInfo(sliderInfoList[i]);
            }
            else
            {
                if (waitTime2 > 0)
                    StartCoroutine(HandleMouseUpDelayed(waitTime2, -1));
            }
        }
    }
    public void UpdateServerWithNewSliderInfo(SliderInfo sliderInfo)
    {
        // send message
        ISFSObject strategyUpdateObj = new SFSObject();
        strategyUpdateObj.PutUtfString("type", "ss"); // strategy screen
        strategyUpdateObj.PutUtfString("sv", sliderInfo.val); // slider value
        strategyUpdateObj.PutInt("id", sliderInfo.id); // type
        CommunicationManager.SendObjectMsg(strategyUpdateObj);
    }

    public void HandleMessage(ISFSObject msgObj)
    {
        string newSliderVal = msgObj.GetUtfString("sv");
        int id = msgObj.GetInt("id");

        for (int i = 0; i < sliderInfoList.Count; ++i)
            if (sliderInfoList[i].id == id)
                UpdateSlider(sliderInfoList[i].divName, newSliderVal);
    }

    private string GetSliderPositionCmd(string sliderElementName)
    {
        if (sliderElementName == null || sliderElementName == "")
            return "";
        return "var elem = document.getElementById(\"" + sliderElementName + "\"); if(elem != null && elem.firstChild != null && elem.firstChild.style != null){elem.firstChild.style.left;}";
    }

    private void UpdateSlider(string sliderElementName, string newValue)
    {
        string cmd = "var elem = document.getElementById(\"" + sliderElementName + "\"); if( elem != null && elem.firstChild != null && elem.firstChild.style != null){elem.firstChild.style.left=\"" + newValue + "\";elem.firstChild.getBoundingClientRect();}";
        if (bTex != null)
        {
            JSValue rect = bTex.ExecuteJavaScriptWithResult(cmd);
            if( rect != null )
                ClickOnCenterOfObject(rect.GetObject());
        }
    }

    private void ClickOnCenterOfObject(JSObject rectObj)
    {
        if (bTex == null || !bTex.IsLoaded || rectObj == null || !rectObj.HasProperty("left") || !rectObj.HasProperty("top") || !rectObj.HasProperty("width") || !rectObj.HasProperty("height"))
            return;

        int ltPos = rectObj["left"].ToInteger();
        int topPos = rectObj["top"].ToInteger();
        int width = rectObj["width"].ToInteger();
        int height = rectObj["height"].ToInteger();
        bTex.webView.InjectMouseMove(ltPos + width / 2, topPos + height / 2);
        bTex.webView.InjectMouseDown(MouseButton.Left);
        bTex.webView.InjectMouseUp(MouseButton.Left);
    }
}
