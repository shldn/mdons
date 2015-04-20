using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Core;
using Sfs2X.Requests;

public enum GUIVis
{
    NONE = 0,
    CONSOLE = 1,
    TIMER = 2,
    VOICE = 4,
    USERLIST = 8,
    AVATARBUTTON = 16,
    AVATARGUI = 32,
    PRESENTERINPUT = 64,
    REPLAYGUI = 128,
}

//----------------------------------------------------------
// GameGUI
//
// Sets up menu system for launching chat window, etc.
//----------------------------------------------------------
public class GameGUI : MonoBehaviour
{

    //----------------------------------------------------------
    // Setup variables
    //----------------------------------------------------------
    private TeleportGUI teleGui;
    public ConsoleGUI consoleGui;
    private ReplayGUI replayGUI;
    private DevGUI devGUI;
    public CustomizeAvatarGUI customizeAvatarGui;
    public HTMLGuiLayer guiLayer;
    public HTMLUserListManager userListMgr;

    //----------------------------------------------------------
    // Graphic Assets
    //----------------------------------------------------------
    private Texture2D consoleBGStretchable = null;
    private Texture2D consoleBGEndCap = null;

    public Texture2D consoleBackgroundTex = null;

    //----------------------------------------------------------
    // Presenter tool
    //----------------------------------------------------------
    public bool showPresentToolButtons = true;
    private VirCustomButton presentToolResizeButton = null;
    private VirCustomButton presentToolCloseButton = null;

    private bool allowPresenterT = false;
    public float presentToolScale = 0.5f;
    public bool mouseInPresenterTool = false;
    public bool sendGuiPresentToolUpdates = true;
    public bool forceSendNextGuiPresentToolRect = false;
    public CollabBrowserTexture presenterToolCollabBrowser = null;
    public string presenterURL = "";
    private bool presentVisibilityChanged = true;
    public bool allowPresenterTool {
        get { return allowPresenterT; }
        set {
            presentVisibilityChanged = allowPresenterT != value;
            allowPresenterT = value;
            if( presentVisibilityChanged )
                forceSendNextGuiPresentToolRect = true;
        }
    }
                                    

    //----------------------------------------------------------
    // ViewportCam controls
    //----------------------------------------------------------
    private Camera productViewportCam = null;
    private VirCustomButton productViewportResizeButton = null;
    private VirCustomButton productViewportCloseButton = null;

    public float productViewportScale = 0.4f;

    // Dev options --------------------------------------------- ||
    public bool objectTooltips = false;

    //----------------------------------------------------------
    // Chat variables
    //----------------------------------------------------------
    private bool isChatWindowVisible = false;
    private bool visible = true;
    private Vector2 chatScrollPosition;
    private Vector2 userScrollPosition;
    private Vector2 whoScrollPosition;
    private string chatHistory = "";
    private string newMessage = "";
    private int setFocusOnInput = 0; // represents number of consecutive cycles to force focus on the chat input, a single cycle wasn't working
    private bool pushToTalkKeyPressed = false;

    private bool facilitatorCamsOn = false;

    private float chatPanelWidth = Math.Min(375, Screen.width / 3 - 10);
    private float chatPanelHeight = Math.Min(325, Screen.height / 2);
    private float chatHistoryBuffer = 40;
    private float outerBuffer = 10;
    private float chatPanelPosX;
    private float chatPanelPosY;
    private Rect chatWindowRect;
    private Rect chatHistoryRect;
    private float userListPanelWidth;
    private Rect userListRect;
    private string chatTo = "Public";
    private Sfs2X.Entities.User lastSelectedUser = null;

    private string fixedTooltip = "";
    public Rect tooltipAvoidRect = new Rect(0, 0, 0, 0);

    private string lastConsoleText = "";
    private float typingIndicatorTimeout = 0f;

    // Omnibox
    private Texture2D obTLCorner = null;
    private Texture2D obTEdge = null;
    private Texture2D obLEdge = null;
    private Texture2D obFill = null;

    // Fadeout
    private Texture2D whiteTex = null;
    public bool fadeOut = false;
    public float fadeAlpha = 1.5f;

    private int menuWidth = Screen.width - 50;
    private int menuHeight = 22;
    public int gutter = 10;
    private int quitBtnWidth = 30; //50;
    public int prevVisibilityFlags = -1;
    private int awakeFrame = 0;
    public GUISkin customGUISkin;
    private static GameGUI mInstance;
    public static GameGUI Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = GameManager.Inst.gameObject.AddComponent<GameGUI>();
            return mInstance;
        }
    }

    public static GameGUI Inst
    {
        get { return Instance; }
    }

    public static void Destroy()
    {
        Destroy(mInstance);
        mInstance = null;
    }

    public bool DevGUIEnabled { get { return devGUI.enabled; } set { devGUI.enabled = value; teleGui.enabled = value; } }
    public bool AllowConsoleToTakeEnterFocus { set { if (consoleGui != null) { consoleGui.AllowConsoleToTakeEnterFocus = value; } } }
	public int VisibilityFlags{ get{ return CommunicationManager.nativeGUILevel; } set{ CommunicationManager.nativeGUILevel = value; } }
    public bool Visible
    {
        get { return visible; }
        set
        {
            visible = value;
            if (visible && prevVisibilityFlags != -1)
                VisibilityFlags = prevVisibilityFlags;
            else
            {
                prevVisibilityFlags = VisibilityFlags;
                VisibilityFlags = 0;
            }
            replayGUI.enabled = visible;
        }
    }
    //----------------------------------------------------------
    // Unity callbacks
    //----------------------------------------------------------
    void Awake()
    {
        mInstance = this;
        awakeFrame = Time.frameCount;

        chatHistoryBuffer = 40;
        outerBuffer = 50;
        chatPanelPosX = Screen.width - chatPanelWidth - outerBuffer;
        chatPanelPosY = Screen.height - chatPanelHeight - outerBuffer;
        userListPanelWidth = chatPanelWidth * 3 / 10;
        chatWindowRect = new Rect(chatPanelPosX, chatPanelPosY, chatPanelWidth, chatPanelHeight);

        teleGui = this.gameObject.AddComponent<TeleportGUI>();
        consoleGui = this.gameObject.AddComponent<ConsoleGUI>();
        customizeAvatarGui = this.gameObject.AddComponent<CustomizeAvatarGUI>();
        userListMgr = this.gameObject.AddComponent<HTMLUserListManager>();
        replayGUI = this.gameObject.AddComponent<ReplayGUI>();
        devGUI = this.gameObject.AddComponent<DevGUI>();
        DevGUIEnabled = false;

        // Load  dynamic console input TextField textures.
        consoleBGStretchable = Resources.Load("Textures/UnityNativeGUI/TextWindow_Middle") as Texture2D;
        consoleBGEndCap = Resources.Load("Textures/UnityNativeGUI/TextWindow_EndCap") as Texture2D;

        // Load presenter tool textures
        presentToolResizeButton = new VirCustomButton(Resources.Load("Textures/UnityNativeGUI/PresentTool_Resize") as Texture2D, 0, 0, 1.2f, "Resize");
        presentToolCloseButton = new VirCustomButton(Resources.Load("Textures/UnityNativeGUI/Button_ChatMin_Active") as Texture2D, 0, 0, 1.2f, "Minimize");

        consoleBackgroundTex = Resources.Load("Textures/UnityNativeGUI/ConsoleBackground") as Texture2D;

        
        // Set up viewport buttons
        productViewportResizeButton = new VirCustomButton(Resources.Load("Textures/UnityNativeGUI/PresentTool_Resize") as Texture2D, 0, 0, 1.2f, "Resize");
        productViewportCloseButton = new VirCustomButton(Resources.Load("Textures/UnityNativeGUI/Button_ChatMin_Active") as Texture2D, 0, 0, 1.2f, "Minimize");


        // Presenter tool defaults to off.
        allowPresenterTool = false;

        // Resize viewport if necessary
        if( !Screen.fullScreen && Screen.height >= (Screen.currentResolution.height - 40) )
            Screen.SetResolution((int)(0.85 * Screen.width), (int)(0.85 * Screen.height), Screen.fullScreen);

        // Omnibox
        obTLCorner = Resources.Load("Textures/UnityNativeGUI/omnibox/top_left_corner") as Texture2D;
        obTEdge = Resources.Load("Textures/UnityNativeGUI/omnibox/top_edge") as Texture2D;
        obLEdge = Resources.Load("Textures/UnityNativeGUI/omnibox/left_edge") as Texture2D;
        obFill = Resources.Load("Textures/UnityNativeGUI/omnibox/fill") as Texture2D;

        // Fadeout texture
        whiteTex = Resources.Load("Textures/white") as Texture2D;

        // Turn off all native gui for assembly
        if (GameManager.Inst.ServerConfig == "Assembly" || GameManager.Inst.ServerConfig == "MDONS")
            GameGUI.Inst.Visible = false;

    }

    void Update()
    {
        // Typing indication icon
        typingIndicatorTimeout -= Time.deltaTime;

        if (GameManager.Inst.LocalPlayer)
        {
            if ((consoleGui.consoleInput != "") && (consoleGui.consoleInput[0] != '/') && (consoleGui.consoleInput.Length > lastConsoleText.Length))
            {
                typingIndicatorTimeout = 3f;
                if (!GameManager.Inst.LocalPlayer.IsTyping)
                {
                    GameManager.Inst.LocalPlayer.IsTyping = true;
                    ISFSObject typingObj = new SFSObject();
                    typingObj.PutBool("typn", true);
                    typingObj.PutInt("plyr", GameManager.Inst.LocalPlayer.Id);
                    CommunicationManager.SendObjectMsg(typingObj);
                }
            }
            else if (GameManager.Inst.LocalPlayer.IsTyping && ((consoleGui.consoleInput == "") || (typingIndicatorTimeout <= 0f)))
            {
                GameManager.Inst.LocalPlayer.IsTyping = false;
                ISFSObject typingObj = new SFSObject();
                typingObj.PutBool("typn", false);
                typingObj.PutInt("plyr", GameManager.Inst.LocalPlayer.Id);
                CommunicationManager.SendObjectMsg(typingObj);
            }
            lastConsoleText = consoleGui.consoleInput;
        }

        fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeOut ? 1 : 0, Time.deltaTime);


    }

    void OnLevelWasLoaded(int level)
    {
        mouseInPresenterTool = false;
    }

    void OnGUI()
    {
        if (guiLayer == null && GameManager.Inst.ServerConfig != "Assembly" && GameManager.Inst.ServerConfig != "MDONS" && Time.frameCount > awakeFrame)
            guiLayer = this.gameObject.AddComponent<HTMLGuiLayer>();

        if (GameManager.Inst.Initialized && GameManager.Inst.playerManager && Application.loadedLevel > 0 && Application.loadedLevelName != "Connection")
        {
            if (GameManager.Inst.LevelLoaded == GameManager.Level.AVATARSELECT && IsVisible(GUIVis.AVATARGUI))
                customizeAvatarGui.DrawGUI(gutter, gutter);
            else
            {
                DrawMenu();
                // DrawChatGUI();
                DrawTimer();
                DrawUserList();
                DrawInvisiblePathTimer();
                teleGui.DrawGUI(Screen.width - teleGui.width - teleGui.height, Screen.height - teleGui.height);
                // Draw console backlog
                if (IsVisible(GUIVis.CONSOLE) && GameManager.Inst.LevelLoaded != GameManager.Level.AVATARSELECT)
                    consoleGui.DrawGUI();
                if (IsVisible(GUIVis.REPLAYGUI))
                    replayGUI.DrawGUI(0, 0);
                devGUI.DrawGUI(gutter, 100);

                // Draw presenter tool, if active.
                if (presenterToolCollabBrowser && allowPresenterTool && (GameManager.Inst.LevelLoaded == GameManager.Level.ORIENT || GameManager.Inst.LevelLoaded == GameManager.Level.OFFICE))
                    DrawPresenterTool();
                else
                    mouseInPresenterTool = false;

                // Find Product Viewport Cam and set up buttons for it.
                GameObject productViewportCamGO = GameObject.Find("ProductViewportCam");
                if (productViewportCamGO)
                    productViewportCam = productViewportCamGO.GetComponent<Camera>();

                if (productViewportCam && !facilitatorCamsOn)
                    DrawProductViewportControls();
            }
        }

        // Debugging tool; shows a tooltip for whatever the mouse is pointing at.
        if (objectTooltips)
        {
            RaycastHit devTipRay = new RaycastHit();
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out devTipRay))
            {
                GUI.tooltip = devTipRay.collider.gameObject.name;
            }
        }


        DrawTooltip();

        // Fade in/out effect.
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(-50f, -50f, Screen.width + 100f, Screen.height + 100f), whiteTex);
        }
    }
    void ChatWindowFunc(int windowID)
    {
        chatHistoryRect = new Rect(20, 25, chatWindowRect.width - chatHistoryBuffer - userListPanelWidth, chatWindowRect.height - 80);
        userListRect = new Rect(20 + chatHistoryRect.width, 25, userListPanelWidth, chatHistoryRect.height);

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.BeginArea(chatHistoryRect);
        chatScrollPosition = GUILayout.BeginScrollView(chatScrollPosition, false, false);
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.white;
        GUILayout.TextArea(chatHistory, textStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(false));
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUILayout.BeginArea(userListRect);
        userScrollPosition = GUILayout.BeginScrollView(userScrollPosition, false, false);
        foreach (Sfs2X.Entities.User u in CommunicationManager.LastJoinedRoom.UserList)
        {
            if (u == CommunicationManager.MySelf)
                continue;
            if (GUILayout.Button(new GUIContent(u.Name, "Click to chat privately with " + u.Name)))
            {
                chatTo = u.Name;
                lastSelectedUser = CommunicationManager.UserManager.GetUserByName(u.Name);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUILayout.EndHorizontal();

        // Send chat message text field and button
        GUILayout.BeginArea(new Rect(gutter, chatWindowRect.height - chatHistoryBuffer, chatWindowRect.width - 20, chatHistoryBuffer));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(chatTo))
        {
            if (lastSelectedUser == null || chatTo == lastSelectedUser.Name)
                chatTo = "Public";
            else
                chatTo = lastSelectedUser.Name;
        }
        GUI.SetNextControlName("ChatInput");
        newMessage = GUILayout.TextField(newMessage, 146, GUILayout.Width(chatWindowRect.width - 80));
        if (GUI.GetNameOfFocusedControl() == "ChatInput" && Event.current.type == EventType.keyUp && Event.current.keyCode == KeyCode.Return)
        {
            if (chatTo == "Public")
                ChatManager.Inst.sendPublicMsg(newMessage);
            else
                ChatManager.Inst.sendPrivateMsg(newMessage, lastSelectedUser);
            newMessage = "";
        }

        if (setFocusOnInput > 0)
        {
            GUI.FocusControl("ChatInput");
            setFocusOnInput--;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUILayout.EndVertical();

        chatWindowRect = ResizeGUIWindow.ResizeWindow(chatWindowRect);
        GUI.DragWindow();

    }

    string TimeSpanStr(TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:00}:", span.Days) : string.Empty,
            string.Format("{0:00}:", span.Hours),
            string.Format("{0:00}:", span.Minutes),
            string.Format("{0:00}", span.Seconds));

        return formatted;
    }

    void DrawTimer()
    {
        if (IsVisible(GUIVis.TIMER) && (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM || GameManager.Inst.LevelLoaded == GameManager.Level.CMDROOM))
        {
            TimeSpan timeLeft = BizSimManager.Inst.QuarterTimeLeft;
            int timerWidth = timeLeft.Days > 0 ? 90 : 70;
            if (TooltipButtonWithSound(new Rect(Screen.width - gutter - timerWidth - quitBtnWidth - gutter, gutter, timerWidth, 30), new GUIContent(((BizSimManager.Inst.QuarterTimeLeftValid) ? TimeSpanStr(timeLeft) : "Reload"), ((BizSimManager.Inst.QuarterTimeLeftValid) ? "Time remaining until quarter ends - click to refresh all panels" : "Click to refresh all panels"))))
                BizSimManager.Inst.ReloadAll();
        }
    }

    void DrawInvisiblePathTimer()
    {
        /*
        if (Application.loadedLevelName.ToLower() == "invisiblepath")
        {
            if (ButtonWithSound(new Rect(Screen.width - gutter - 70 - quitBtnWidth - gutter, gutter, 70, 30), InvisiblePathManager.Inst.invisPathTimer))
            {
                InvisiblePathManager.Inst.isFinishedInvisiblePath = false;
                InvisiblePathManager.Inst.fTimer = 0f;
                GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
                InvisiblePathManager.Inst.invisPathStart = DateTime.UtcNow;
            }
        }
        */
    }

    void DrawTooltip()
    {
        if (!String.IsNullOrEmpty(fixedTooltip))
        {
            if (String.IsNullOrEmpty(GUI.tooltip))
                GUI.tooltip = fixedTooltip;
            else
                fixedTooltip = "";
        }

        if (!String.IsNullOrEmpty(GUI.tooltip))
        {
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.box);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            centeredStyle.stretchWidth = true;
            GUIContent tooltipContent = new GUIContent(GUI.tooltip);
            Vector2 size = centeredStyle.CalcSize(tooltipContent);

            Rect tooltipRect = new Rect(new Rect(Input.mousePosition.x + 5, Screen.height - (Input.mousePosition.y + size.y + 5), size.x, size.y));

            if ((Screen.width - Input.mousePosition.x) < size.x)
                tooltipRect.x -= size.x;

            if ((Screen.height - Input.mousePosition.y) < size.y)
                tooltipRect.y += size.y;

            // If rect is to the right of avoidRect...
            if(tooltipRect.x + (tooltipRect.width * 0.5f) > (tooltipAvoidRect.x + (tooltipAvoidRect.width * 0.5f))){
                // if rect possibly overlaps vertically...
                if((tooltipRect.y < (tooltipAvoidRect.y + tooltipAvoidRect.height)) && ((tooltipRect.y + tooltipRect.height) > tooltipAvoidRect.y)){
                    // if rect overlaps...
                    if(tooltipRect.x < (tooltipAvoidRect.x + tooltipAvoidRect.width)){
                        // if there's enough space to move to the right...
                        if((Screen.width - (tooltipAvoidRect.x + tooltipAvoidRect.width)) >= tooltipRect.width){
                            // Move to the right.
                            tooltipRect.x = tooltipAvoidRect.x + tooltipAvoidRect.width;
                        }
                        // Otherwise we assume there's enough room to move to the left.
                        else
                            tooltipRect.x = tooltipAvoidRect.x - tooltipRect.width;
                    }
                }
            }
            // rect is to the left of avoidRect...
            else{
                // if rect possibly overlaps vertically...
                if((tooltipRect.y < (tooltipAvoidRect.y + tooltipAvoidRect.height)) && ((tooltipRect.y + tooltipRect.height) > tooltipAvoidRect.y)){
                    // if rect overlaps...
                    if(tooltipRect.x > (tooltipAvoidRect.x - tooltipRect.width)){
                        // if there's enough space to move to the left...
                        if(tooltipAvoidRect.x >= tooltipRect.width){
                            // Move to the right.
                            tooltipRect.x = tooltipAvoidRect.x - tooltipRect.width;
                        }
                        // Otherwise we assume there's enough room to move to the right.
                        else
                            tooltipRect.x = tooltipAvoidRect.x + tooltipAvoidRect.width;
                    }
                }
            }

            GUI.Box(tooltipRect, tooltipContent, centeredStyle);
        }
    }

    // Chat GUI consisting of a chat window, input area, toggle button
    void DrawChatGUI()
    {

        if (!isChatWindowVisible)
            return;
        // Chat history panel
        GUI.skin = customGUISkin;
        GUILayout.BeginArea(chatWindowRect);
        chatWindowRect = GUI.Window(0, chatWindowRect, ChatWindowFunc, new GUIContent("Chat", "Public/Private Chat window"));
        GUILayout.EndArea();

    }

    public void WriteToDebugLog(string msg)
    {
        consoleGui.WriteToConsoleLog(msg, true);
    }

    public void WriteToConsoleLog(string msg, bool chat = false)
    {
        consoleGui.WriteToConsoleLog(msg, false, chat);
    }

    public void UpdateChatGUI(string newChatLog)
    {
        chatHistory = newChatLog;
        chatScrollPosition.y = Mathf.Infinity;
        WriteToConsoleLog(ChatManager.Inst.lastmsg, true);
    }

    public void ToggleFacilitatorCameras()
    {
        facilitatorCamsOn = !facilitatorCamsOn;
        List<FacilitatorCameras> facCam = FacilitatorCameras.GetAll();

        foreach (FacilitatorCameras f in facCam)
        {
            f.gameObject.SetActive(facilitatorCamsOn);
        }
    }

    public void DrawConsoleGUIBar(Rect guiBarRect)
    {
        GUI.color = Color.white;
        // Left endcap
        Rect leftCapRect = new Rect(guiBarRect.x, guiBarRect.y, guiBarRect.height, guiBarRect.height);
        GUI.DrawTexture(leftCapRect, consoleBGEndCap, ScaleMode.StretchToFill);
        // Middle
        Rect middleRect = new Rect(guiBarRect.x + guiBarRect.height, guiBarRect.y, guiBarRect.width - (2 * guiBarRect.height), guiBarRect.height);
        GUI.DrawTexture(middleRect, consoleBGStretchable, ScaleMode.StretchToFill);
        // Right endcap
        Rect rightCapRect = new Rect(guiBarRect.x + guiBarRect.width, guiBarRect.y, -guiBarRect.height, guiBarRect.height);
        GUI.DrawTexture(rightCapRect, consoleBGEndCap, ScaleMode.StretchToFill);

        if(RectActuallyContainsMouse(guiBarRect))
            PlayerController.ClickToMoveInterrupt();
    }

    void DrawMenu()
    {
        //if (GUILayout.Button(new GUIContent("IM", (isChatWindowVisible ? "Close" : "Open") + " Private Chat Window"), GUILayout.Width(40)))
        //{
        //    SoundManager.Inst.PlayClick();
        //    isChatWindowVisible = !isChatWindowVisible;
        //    setFocusOnInput = 3; // force focus for 3 cycles, just one cycle was losing focus.
        //}

        if (consoleGui && IsVisible(GUIVis.CONSOLE) && GameManager.Inst.LevelLoaded != GameManager.Level.AVATARSELECT)
            consoleGui.DrawConsoleInput();

        // Base button for native gui button elements.
        Rect nativeConsoleButtonRect = new Rect(ConsoleGUI.consoleLeftIndent + ConsoleGUI.consoleWidth + gutter, Screen.height - (ConsoleGUI.consoleBottomIndent + ConsoleGUI.consoleBarHeight), 120, ConsoleGUI.consoleBarHeight);
        if (IsVisible(GUIVis.VOICE))
        {
            // disable voice options if no mic is available
            GUI.enabled = Microphone.devices.Length != 0;

            if (VoiceManager.Inst.AllowVoiceToggle)
            {
                if (GUI.Button(nativeConsoleButtonRect, new GUIContent("Toggle Voice " + ((VoiceManager.Inst.ToggleToTalk) ? "Off" : "On"), "Click to turn voice chat " + ((VoiceManager.Inst.ToggleToTalk) ? "off" : "on"))))
                    VoiceManager.Inst.ToggleToTalk = !VoiceManager.Inst.ToggleToTalk;
                // Shunt rect over for next button.
                nativeConsoleButtonRect.x += nativeConsoleButtonRect.width + 5;
            }
            if (!VoiceManager.Inst.ToggleToTalk)
            {
                nativeConsoleButtonRect.width = 80;
                VoiceManager.Inst.PushToTalkButtonDown = GUI.Button(nativeConsoleButtonRect, new GUIContent("VoicePush", "Push and hold to talk")) || Input.GetButton("PushToTalk");
            }

            GUI.enabled = true;
        }

        // Button overrides
        else if (!VoiceManager.Inst.ToggleToTalk && (Input.GetButton("PushToTalk") || Input.GetButton("PushToTalkAlt")))
        {
            pushToTalkKeyPressed = true;
            VoiceManager.Inst.PushToTalkButtonDown = true;
        }
        else if (pushToTalkKeyPressed) // need to allow for the gui layer to control PushToTalkButtonDown, so only change to false if unity input changed to true.
        {
            pushToTalkKeyPressed = false;
            VoiceManager.Inst.PushToTalkButtonDown = false;
        }

        // Show avatar modification button.
        if (IsVisible(GUIVis.AVATARBUTTON))
        {
            nativeConsoleButtonRect.x += nativeConsoleButtonRect.width + 5;
            nativeConsoleButtonRect.width = 130;
            //"Change the appearance of your avatar"
            if (GUI.Button(nativeConsoleButtonRect, "Change Avatar"))
                GameManager.Inst.LoadLevel(GameManager.Level.AVATARSELECT);
        }
    }

    Rect GetPresenterToolRect(Texture2D presenterToolTexture)
    {
        return new Rect(Screen.width - ((presenterToolTexture.width * presentToolScale) + gutter), Screen.height - ((presenterToolTexture.height * presentToolScale) + gutter), (presenterToolTexture.width * presentToolScale), (presenterToolTexture.height * presentToolScale));
    }

    // Shows the active collabBrowser in the corner of the screen.
    void DrawPresenterTool()
    {
        GUI.color = Color.white;

        if (presenterToolCollabBrowser == null)
            return;

        // Pull texture from collabBrowserTexture
        Texture2D presenterToolTexture = presenterToolCollabBrowser.GetTexture();

        Rect presenterToolRect = GetPresenterToolRect(presenterToolTexture);

        // Get position of mouse relative to presenter tool.
        Vector2 mouseLocalPos = Vector2.zero;
        mouseLocalPos.x = (Input.mousePosition.x - presenterToolRect.x) / presenterToolRect.width;
        mouseLocalPos.y = ((Screen.height - presenterToolRect.y) - Input.mousePosition.y) / presenterToolRect.height;

        // Detect if mouse is in presenter tool.
        mouseInPresenterTool = mouseLocalPos.x >= 0f && mouseLocalPos.x <= 1f && mouseLocalPos.y >= 0f && mouseLocalPos.y <= 1f;

        if (mouseInPresenterTool){
            presenterToolCollabBrowser.InjectMousePosition(new Vector2(-mouseLocalPos.x + 0.5f, -mouseLocalPos.y + 0.5f), true, Input.GetMouseButton(0));
            PlayerController.ClickToMoveInterrupt();
        }

        GUI.DrawTexture(presenterToolRect, presenterToolTexture);

        if (showPresentToolButtons)
        {
            presentToolResizeButton.SetPosition(VirCustomButton.Corner.bottomRight, (int)presenterToolRect.x, (int)presenterToolRect.y);
            presentToolResizeButton.Draw();
        }

        // Scale to 0 'minimizes' the presenter tool.
        if (presentToolScale > 0)
        {
            // Close button only shows if the presenter is open.
            if (showPresentToolButtons)
            {
                presentToolCloseButton.SetPosition(VirCustomButton.Corner.topRight, (int)presenterToolRect.x, (int)presenterToolRect.y);
                presentToolCloseButton.Draw();
            }

            // URL entry field
            if (GameGUI.Inst.IsVisible(GUIVis.PRESENTERINPUT) && GameManager.Inst.LocalPlayerType >= PlayerType.LEADER)
            {
                Rect presenterURLRect = new Rect(presenterToolRect.x + 5, presenterToolRect.y - 35, presenterToolRect.width - 5, 30);
                DrawConsoleGUIBar(presenterURLRect);
                Rect presenterURLTextRect = new Rect(presenterURLRect.x + 5, presenterURLRect.y + 3, presenterURLRect.width, presenterURLRect.height);

                // by default tab puts focus on unity text fields, disabling that here.
                if ( !((Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t') && FocusManager.Inst.AnyKeyInputBrowsersFocused()))
                {
                    GUI.SetNextControlName("presenterInput");
                    presenterURL = GUI.TextField(presenterURLTextRect, presenterURL);
                }

                Rect presenterURLTooltipRect = new Rect(presenterURLRect.x, presenterURLRect.y - 20, presenterURLRect.width, presenterURLRect.height);
                GUI.Label(presenterURLTooltipRect, "Enter URL:");

                Event e = Event.current;

                // Enter a URL
                if (GUI.GetNameOfFocusedControl().Equals("presenterInput") && (e.isKey) && (e.keyCode == KeyCode.Return))
                {
                    presenterURL = WebStringHelpers.AppendHTTP(presenterURL);
                    RoomVariableUrlController controller = presenterToolCollabBrowser.GetComponent<RoomVariableUrlController>();
                    if (controller != null)
                        controller.SetNewURL(presenterURL, true);
                    else
                    {
                        GameGUI.Inst.WriteToConsoleLog("Web panel does not have an associated room variable, url change is only local");
                        presenterToolCollabBrowser.GoToURL(presenterURL);
                    }
                }
            }
        }

        float previousScale = presentToolScale;

        // Resize based on mouse position.
        if (presentToolResizeButton.IsDragged())
            presentToolScale = Mathf.Max(Mathf.Clamp((((presenterToolRect.x + presenterToolRect.width) - (Input.mousePosition.x + (presentToolResizeButton.width * 0.5f))) / presenterToolTexture.width), 0.3f, 1f),
                                         Mathf.Clamp((((presenterToolRect.y + presenterToolRect.height) - ((Screen.height - Input.mousePosition.y) + (presentToolResizeButton.height * 0.5f))) / presenterToolTexture.height), 0.3f, 1f));

        // Tool is minimized.
        if (presentToolCloseButton.IsClicked())
        {
            presentToolScale = 0f;
            presentToolCloseButton.Close();
        }

        if (GameGUI.Inst.guiLayer != null && sendGuiPresentToolUpdates && (forceSendNextGuiPresentToolRect || (previousScale != presentToolScale)))
            GameGUI.Inst.guiLayer.SendGuiLayerPresenterToolInfo(GetPresenterToolRect(presenterToolTexture));
        forceSendNextGuiPresentToolRect = false;

        // Handle actions
        if (!presentToolResizeButton.IsDragged() && mouseInPresenterTool)
        {
            Event e = Event.current;
            if ((e.type != EventType.ScrollWheel) || (presenterToolCollabBrowser.enableScrollWheel))
                presenterToolCollabBrowser.HandleBrowserEvent(e, (int)(mouseLocalPos.x * presenterToolCollabBrowser.width), (int)((1 - mouseLocalPos.y) * presenterToolCollabBrowser.height));
        }
    }

    void DrawProductViewportControls()
    {
        float maxViewportSize = Screen.height * 0.75f;
        int productViewportSize = (int)(maxViewportSize * productViewportScale);
        Rect productViewportPixelRect = new Rect(Screen.width - (productViewportSize + gutter), Screen.height - (productViewportSize + gutter), productViewportSize, productViewportSize);

        // Draw viewport
        productViewportCam.rect = new Rect(productViewportPixelRect.x / Screen.width, (float)((float)gutter / (float)Screen.height), productViewportPixelRect.width / Screen.width, productViewportPixelRect.height / Screen.height);


        productViewportResizeButton.SetPosition(VirCustomButton.Corner.bottomRight, (int)productViewportPixelRect.x, (int)productViewportPixelRect.y);
        productViewportResizeButton.Draw();

        // Scale to 0 'minimizes' the presenter tool.
        // Close button only shows if the presenter is open.
        if (productViewportScale > 0)
        {
            productViewportCloseButton.SetPosition(VirCustomButton.Corner.topRight, (int)productViewportPixelRect.x, (int)productViewportPixelRect.y);
            productViewportCloseButton.Draw();
        }

        // Resize based on mouse position.
        if (productViewportResizeButton.IsDragged())
            productViewportScale = Mathf.Max(Mathf.Clamp((((productViewportPixelRect.x + productViewportPixelRect.width) - (Input.mousePosition.x + (productViewportResizeButton.width * 0.5f))) / maxViewportSize), 0.3f, 1f),
                                             Mathf.Clamp((((productViewportPixelRect.y + productViewportPixelRect.height) - ((Screen.height - Input.mousePosition.y) + (productViewportResizeButton.height * 0.5f))) / maxViewportSize), 0.3f, 1f));

        // Tool is minimized.
        if (productViewportCloseButton.IsClicked())
        {
            productViewportScale = 0f;
            productViewportCloseButton.Close();
        }
    }

    public void ToggleDisplayConsole()
    {
        consoleGui.ToggleDisplayConsole();
    }

    public bool ToggleDebugConsoleMsg()
    {
        consoleGui.DebugMsgEnable = !consoleGui.DebugMsgEnable;
        return consoleGui.DebugMsgEnable;
    }

    public bool ButtonWithSound(Rect position, string text, GUIStyle style)
    {
        if (GUI.Button(position, text, style))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public bool ButtonWithSound(Rect position, string text)
    {
        if (GUI.Button(position, text))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public bool ButtonWithSound(Rect position, GUIContent content)
    {
        if (GUI.Button(position, content))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public bool ButtonWithSound(GUIContent content, GUILayoutOption options)
    {
        if (GUILayout.Button(content, options))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public bool IsVisible(GUIVis element)
    {
        return (VisibilityFlags & (int)element) > 0;
    }

    public bool TooltipButtonWithSound(Rect position, GUIContent content)
    {
        if (GUI.Button(position, content))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public bool LayoutButtonWithSound(GUIContent content, GUILayoutOption options)
    {
        if (GUILayout.Button(content, options))
        {
            SoundManager.Inst.PlayClick();
            return true;
        }
        else
            return false;
    }

    public void SetDebugConsoleMsg(bool enable)
    {
        consoleGui.DebugMsgEnable = enable;
    }


    // --- old user list --- will use only if guilayer fails
    void DrawUserList()
    {
        if (!IsVisible(GUIVis.USERLIST))
            return;
        GUIStyle whiteBoxStyle = new GUIStyle(GUI.skin.box);
        GUIStyle redBoxStyle = new GUIStyle(GUI.skin.box);
        whiteBoxStyle.normal.textColor = Color.white;
        redBoxStyle.normal.textColor = Color.red;

        IEnumerator players = GameManager.Inst.playerManager.GetEnumerator();
        GUILayout.BeginArea(new Rect(10, 10, userListPanelWidth * 1.15f, 300));
        whoScrollPosition = GUILayout.BeginScrollView(whoScrollPosition, false, false);
        Player localPlayer = GameManager.Inst.playerManager.GetLocalPlayer();
        GUIStyle textStyle = (localPlayer != null && localPlayer.IsTalking) ? redBoxStyle : whiteBoxStyle;
        GUILayout.Box(CommunicationManager.MySelf.Name, textStyle);
        while (players.MoveNext())
        {
            KeyValuePair<int, Player> k = (KeyValuePair<int, Player>)players.Current;
            if (k.Value.SFSName != CommunicationManager.MySelf.Name && (!k.Value.IsStealth || (localPlayer != null && (int)localPlayer.Type > (int)PlayerType.NORMAL)))
            {
                textStyle = (k.Value.IsTalking) ? redBoxStyle : whiteBoxStyle;
                GUILayout.Box(k.Value.DisplayName, textStyle);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    public void SetFixedTooltip(string tooltip)
    {
        fixedTooltip = tooltip;
    }

    public void ClearFixedTooltip()
    {
        fixedTooltip = "";
    }

    public void NativeGUIBox(Rect rect)
    {
        /*
        private Texture2D obTRCorner = null;
        private Texture2D obTEdge = null;
        private Texture2D obLEdge = null;
        private Texture2D obFill = null;
        */

        float x = rect.x;
        float y = rect.y;
        float w = rect.width;
        float h = rect.height;

        float edgeWidth = 18f;

        // Corners
        GUI.DrawTexture(new Rect(x, y, edgeWidth, edgeWidth), obTLCorner);
        GUI.DrawTexture(new Rect(x + w, y, -edgeWidth, edgeWidth), obTLCorner);
        GUI.DrawTexture(new Rect(x, y + h, edgeWidth, -edgeWidth), obTLCorner);
        GUI.DrawTexture(new Rect(x + w, y + h, -edgeWidth, -edgeWidth), obTLCorner);

        // Edges
        GUI.DrawTexture(new Rect(x + edgeWidth, y, w - (edgeWidth * 2f), edgeWidth), obTEdge);
        GUI.DrawTexture(new Rect(x + edgeWidth, y + h, w - (edgeWidth * 2f), -edgeWidth), obTEdge);

        GUI.DrawTexture(new Rect(x, y + edgeWidth, edgeWidth, h - (edgeWidth * 2f)), obLEdge);
        GUI.DrawTexture(new Rect(x + w, y + edgeWidth, -edgeWidth, h - (edgeWidth * 2f)), obLEdge);

        // Fill
        GUI.DrawTexture(new Rect(x + edgeWidth, y + edgeWidth, w - (edgeWidth * 2f), h - (edgeWidth * 2f)), obFill);

        if(RectActuallyContainsMouse(rect))
            PlayerController.ClickToMoveInterrupt();


    } // End of NativeGUIBox().


    // Because Unity's mouse coordinates are stupid!
    public bool RectActuallyContainsMouse(Rect rect){
        return
        (Input.mousePosition.x > rect.x) &&
        (Input.mousePosition.x < (rect.x + rect.width)) &&
        ((Screen.height - Input.mousePosition.y) > rect.y) &&
        ((Screen.height - Input.mousePosition.y) < (rect.y + rect.height));
    } // End of RectActuallyContains().

    public void CutToBlack()
    {
        fadeAlpha = 1.0f;
    }


    // Helpers for Guilayer
    public bool ExecuteJavascriptOnGui(string cmd)
    {
        if (guiLayer != null)
            return guiLayer.ExecuteJavascript(cmd);
        return false;
    }

    public void HandleDownloadRequest(string url)
    {
        if (guiLayer != null)
            guiLayer.HandleDownloadRequest(url);
        else
            Application.OpenURL(url);
    }

    public bool GuiLayerHasInputFocus { get { return guiLayer != null && guiLayer.HasInputFocus; } }
    public bool IsMouseOverGuiLayerElement { get { return guiLayer != null && guiLayer.IsMouseOverGUIElement(); } }

}