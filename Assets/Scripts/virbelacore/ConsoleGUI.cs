using UnityEngine;
using System;
using System.Collections.Generic;

public struct ConsoleLog
{
    public ConsoleLog(string msg, bool dbg = false)
    {
        isDebug = dbg;
        message = msg;
    }

    public bool isDebug;
    public string message;
}

public class ConsoleGUI : MonoBehaviour
{
    public string consoleInput = "";
    private List<string> consoleInputs = new List<string>();
    private int inputIndex;
    private Vector2 logScrollPosition;
    private bool drawConsole = false;
    private bool keepConsoleUpWhileInputHasFocus = true;
    private float consoleLogLastDrawn;
    private List<ConsoleLog> consoleLogs = new List<ConsoleLog>();
    private float consoleHideTime = 5.0f;
    private bool debugMsgEnable = false;
    private bool showConsoleOnUpdate = false;
    private bool allowToTakeEnterFocus = true;
    private bool hadFocusLastFrame = false;
    private bool makeUrlsClickable = true;

    public static int consoleLeftIndent = 10;
    public static int consoleBottomIndent = 10;
    public static int consoleHeightMax = (int)(Screen.height * 0.40f);
    public static int consoleWidth = (int)(Screen.width * 0.40f);
    private int consoleHeight = 0;

    public static int consoleBarHeight = 30;

    private int focusConsoleTicks = 0;

    public bool AllowConsoleToTakeEnterFocus { get { return allowToTakeEnterFocus && !GameGUI.Inst.GuiLayerHasInputFocus; } set { allowToTakeEnterFocus = value; } }
    public bool DebugMsgEnable
    {
        get { return debugMsgEnable; }
        set { debugMsgEnable = value; }
    }


    void Update()
    {
        if (showConsoleOnUpdate)
        {
            drawConsole = true;
            consoleLogLastDrawn = Time.realtimeSinceStartup;
            showConsoleOnUpdate = false;
        }

        if (consoleHideTime == 0)
            drawConsole = true;
        else
        {
            if (drawConsole && Time.realtimeSinceStartup - consoleLogLastDrawn > consoleHideTime)
                drawConsole = false;
        }
    }

    public void DrawConsoleLog()
    {
        // by default tab puts focus on unity text fields, disabling that here.
        if ((Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t') && FocusManager.Inst.AnyKeyInputBrowsersFocused())
            return;

        int firstLineHeight = (consoleLogs.Count >= 1) ? 25 : 0;
        int lineHeight = 15;
        int calculatedHeight = Math.Min(firstLineHeight + (consoleLogs.Count - 1) * lineHeight, consoleHeightMax); 
        consoleHeight = (calculatedHeight < consoleHeight) ? consoleHeight : calculatedHeight;
        Rect consoleLogRect = new Rect(consoleLeftIndent, Screen.height - (consoleBottomIndent + consoleHeight + consoleBarHeight + GameGUI.Inst.gutter), consoleWidth, consoleHeight);
        GUILayout.BeginArea(consoleLogRect);
        logScrollPosition = GUILayout.BeginScrollView(logScrollPosition, GUI.skin.box);

        // if scollbar appears, make console bigger for next render (within max height)
        if (consoleHeight < consoleHeightMax && logScrollPosition.y > 0 && logScrollPosition.y < 99999999) // ScrollView returns Infinity when no scrollbar
            consoleHeight += 15;

        GUI.contentColor = Color.white;
        foreach (ConsoleLog log in consoleLogs)
        {
            GUI.skin.textArea.normal.background = null;
            GUI.skin.textArea.active.background = null;
            GUI.skin.textArea.onHover.background = null;
            GUI.skin.textArea.hover.background = null;
            GUI.skin.textArea.onFocused.background = null;
            GUI.skin.textArea.focused.background = null;
            GUI.skin.textArea.padding.top = 0;
            GUI.skin.textArea.padding.bottom = 0;
            GUI.skin.textArea.margin.top = 0;
            GUI.skin.textArea.margin.bottom = 0;

            GUI.SetNextControlName("Console Log Entry");
            if (log.isDebug && debugMsgEnable)
            {
                GUI.contentColor = Color.red;
                GUILayout.TextArea(log.message);
                GUI.contentColor = Color.white;
            }
            else if (!log.isDebug)
            {
                int urlIndex = makeUrlsClickable ? log.message.IndexOf("http") : -1;
                if (urlIndex != -1)
                {
                    string firstToken = log.message.Substring(0, urlIndex);
                    int endUrlIndex = log.message.IndexOf(' ', urlIndex);
                    string url = (endUrlIndex == -1) ? log.message.Substring(urlIndex) : log.message.Substring(urlIndex, endUrlIndex-urlIndex);
                    string endToken = (endUrlIndex == -1) ? "" : log.message.Substring(endUrlIndex);
                    int charWidth = 8;

                    GUILayout.BeginHorizontal();
                    GUILayout.TextArea(firstToken, GUILayout.Width(firstToken.Length * charWidth));
                    GUI.contentColor = MathHelper.HexToColor("3b7ec4");
                    bool buttonClicked = (endToken != "") ? GUILayout.Button(url, "TextArea", GUILayout.Width(url.Length * charWidth)) : GUILayout.Button(url, "TextArea");
                    if (buttonClicked)
                        GameGUI.Inst.guiLayer.HandleClickOnUrlText(url);
                    GUI.contentColor = Color.white;
                    GUILayout.TextArea(endToken);
                    GUILayout.EndHorizontal();

                }
                else
                    GUILayout.TextArea(log.message);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (GameGUI.Inst.RectActuallyContainsMouse(consoleLogRect))
            PlayerController.ClickToMoveInterrupt();

        if (consoleLogRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
        {
            consoleLogLastDrawn = Time.realtimeSinceStartup;
        }
    }

    public void DrawConsoleInput()
    {
        // by default tab puts focus on unity text fields, disabling that here.
        if ((Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t') && FocusManager.Inst.AnyKeyInputBrowsersFocused())
            return;

        // Render the console input bar.
        // Console Input Rect
        Rect consoleRect = new Rect(consoleLeftIndent, Screen.height - (consoleBottomIndent + consoleBarHeight), consoleWidth, consoleBarHeight);
        GameGUI.Inst.DrawConsoleGUIBar(consoleRect);

        GUI.skin.textField.normal.background = null;
        GUI.skin.textField.active.background = null;
        GUI.skin.textField.focused.background = null;
        GUI.skin.textField.hover.background = null;

        int consoleTextMarginY = 5;
        int consoleTextMarginX = 8;

        GUI.SetNextControlName("consoleInput");
        consoleInput = GUI.TextField(new Rect(consoleRect.x + consoleTextMarginX, consoleRect.y + consoleTextMarginY, consoleRect.width - (2 * consoleTextMarginX), consoleRect.height - (2 * consoleTextMarginY)), consoleInput);

        // FocusConsole() runs this stuff until the console is focused.
        // This takes two ticks to work (not sure why)... focusConsoleTicks keeps track.
        if (focusConsoleTicks > 0)
        {
            GUI.FocusControl("consoleInput");

            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (editor != null)
            {
                editor.selectPos = consoleInput.Length;
                editor.pos = consoleInput.Length;
            }

            focusConsoleTicks--;
        }

        bool consoleInputHasFocus = GUI.GetNameOfFocusedControl() == "consoleInput";
        // Enter command with Enter.
        if (consoleInputHasFocus && Event.current.type == EventType.keyUp && Event.current.keyCode == KeyCode.Return)
        {
            ConsoleInterpreter.Inst.ProcCommand(consoleInput);
            ClearConsoleInput();
            if (GameGUI.Inst.guiLayer == null)
                GameGUI.Inst.Visible = false;
        }
        // Scroll up through inputs.
        else if (consoleInputHasFocus && Event.current.type == EventType.keyUp && Event.current.keyCode == KeyCode.UpArrow)
        {
            if (inputIndex < consoleInputs.Count)
            {
                SeedConsole(consoleInputs[inputIndex]);
                inputIndex++;
            }
        }
        // Scroll down through inputs.
        else if (consoleInputHasFocus && Event.current.type == EventType.keyUp && Event.current.keyCode == KeyCode.DownArrow)
        {
            if (inputIndex > 0)
            {
                inputIndex--;
                SeedConsole(consoleInputs[inputIndex]);
            }
        }
        // Exit console input.
        else if (consoleInputHasFocus && Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            ClearConsoleInput();
        // Console takes focus on 'Enter', if applicable.
        if (!consoleInputHasFocus && AllowConsoleToTakeEnterFocus && Input.GetKeyUp(KeyCode.Return))
        {
            if( FocusManager.Inst.IsFocusedOnPanel() && MainCameraController.Inst.cameraType == CameraType.SNAPCAM )
                InfoMessageManager.Display("Web Panel has focus, to chat, click in the console input area");
            else
                FocusConsole();
        }
        if (!hadFocusLastFrame && consoleInputHasFocus)
            FocusManager.Inst.UnfocusKeyInputBrowserPanels();

        showConsoleOnUpdate = keepConsoleUpWhileInputHasFocus && consoleInputHasFocus;
        hadFocusLastFrame = consoleInputHasFocus;
    }

    public void WriteToConsoleLog(string msg, bool debug, bool chat = false)
    {
        consoleLogs.Add(new ConsoleLog(msg, debug));
        if( GameGUI.Inst.guiLayer != null && !GameGUI.Inst.IsVisible(GUIVis.CONSOLE) )
            GameGUI.Inst.guiLayer.SendGuiLayerConsoleMsg(msg, debug, chat);
        Debug.Log("Console : " + msg);
        if (!debug || debugMsgEnable)
        {
            logScrollPosition.y = Mathf.Infinity;
            drawConsole = true;
            consoleLogLastDrawn = Time.realtimeSinceStartup;
        }
    }

    void ClearConsoleInput()
    {
        inputIndex = 0;
        if (consoleInput != "")
        {
            consoleInputs.Insert(0, consoleInput);
            consoleInput = "";
        }
        GUI.FocusControl("");
    }

    public void ToggleDisplayConsole()
    {
        if (consoleHideTime == 0)
        {
            consoleHideTime = 5.0f;
            drawConsole = false;
        }
        else
        {
            consoleHideTime = 0;
            drawConsole = true;
        }
    }

    public void DrawGUI()
    {
        if (drawConsole)
            DrawConsoleLog();
    }

    // Focuses to the console and moves the cursor to the end of current consoleInput string.
    public void FocusConsole()
    {
        focusConsoleTicks = 2;
    }

    public void SeedConsole(string inputSeed)
    {
        consoleInput = inputSeed;
        FocusConsole();
    }

}
