using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FocusManager {
    private static FocusManager mInstance;
    public static FocusManager Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = new FocusManager();
            return mInstance;
        }
    }

    // keep track of focused panel id
    private int focusedPanelID = -1;
    public int GetFocusedID() { return focusedPanelID; }
    public bool IsFocusedOnPanel() { return GetFocusedID() != -1; }
    public void UpdateFocusID(int id, bool focused, string url = "", bool showControlPanel = true, PlayerType minAccessRights = PlayerType.NORMAL)
    {
        if (focused)
            focusedPanelID = id;
        else if (focusedPanelID == id)
            focusedPanelID = -1;
        GameGUI.Inst.ExecuteJavascriptOnGui("setFocusedId(" + focusedPanelID + ", \"" + (focused ? url : "") + "\", \"" + showControlPanel + "\"," + (int)minAccessRights + ");");
    }

    public bool AnyKeyInputBrowsersFocused()
    {
        bool browserFocus = false;
        foreach (KeyValuePair<int, CollabBrowserTexture> cbt in CollabBrowserTexture.GetAll())
            browserFocus |= (cbt.Value.KeyInputEnabled && cbt.Value.Focused);
        return browserFocus;
    }

    public bool RestrictKeyPressMovement()
    {
        return false;
    }

    public void UnfocusKeyInputBrowserPanels()
    {
        foreach (KeyValuePair<int, CollabBrowserTexture> cbt in CollabBrowserTexture.GetAll())
            if (cbt.Value.KeyInputEnabled && cbt.Value.Focused)
                cbt.Value.Unfocus();
    }
}
