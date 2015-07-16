using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CustomizeAvatarGUI : MonoBehaviour {
    const int typeWidth = 100;
    const int buttonWidth = 20;
    GameObject avatarGO;
    int currentModelIdx = 0;
    const float turnPlayerAngleDelta = 0.05f;
    int originalModelIdx = -1;
    bool rotateBtnDown = false;
    int turn = 0; // < 0 == left, > 0 == right, 0 == no turn

    // keep set of changes here, push when we are done.
    Dictionary<string, int> avatarOptions = new Dictionary<string, int>();

    private int GetResourceIdx(string name, bool next)
    {
        int optionIdx = 0;
        if (!avatarOptions.TryGetValue(name, out optionIdx))
            int.TryParse(CommunicationManager.CurrentUserProfile.GetField(name), out optionIdx);
        int dir = next ? 1 : -1;
        return (optionIdx + AvatarOptionManager.Inst.NumOptions(currentModelIdx, name) + dir) % AvatarOptionManager.Inst.NumOptions(currentModelIdx, name);
    }

    private void SaveState()
    {
        // Guests can save a local copy to the registry
        if (CommunicationManager.CurrentUserProfile.IsGuest())
        {
            PlayerPrefs.SetString("VirbelaAvatarOpt", AvatarOptionManager.Inst.AvatarOptionsToString(avatarOptions));

            // clean slate next time
            avatarOptions = new Dictionary<string, int>();
            return;
        }
        if( !CommunicationManager.CurrentUserProfile.CheckLogin() )
        {
            Debug.LogError("Current profile not logged in?");
            return;
        }
        string jsonStr = AvatarOptionManager.Inst.AvatarOptionsToJson(avatarOptions);
        if (jsonStr != "")
            CommunicationManager.CurrentUserProfile.UpdateProfile(jsonStr);
        if( originalModelIdx != currentModelIdx )
            CommunicationManager.CurrentUserProfile.UpdateProfile("model", currentModelIdx.ToString());
        // clear avatar options, clean slate next time if they don't save
        avatarOptions = new Dictionary<string, int>();
    }

    public void DrawGUI (int x, int y)
    {
        if (GameManager.Inst.playerManager != null)
            currentModelIdx = GameManager.Inst.playerManager.GetLocalPlayer().ModelIdx;

        GUI.enabled = true;
        GUILayout.BeginArea(new Rect(x, y, typeWidth + 2 * buttonWidth + 8, Screen.height));

        // Buttons for changing the active character.
        AddCharacterBtn();

        // Buttons for changing character elements.
        foreach (KeyValuePair<string, ResourceOptionList> option in AvatarOptionManager.Inst.Options[currentModelIdx])
            AddCategoryBtn(option.Value.uniqueElementName, option.Value.displayName);
        AddRotateBtn();
        AddRandomizeBtn();
        AddDoneBtn();

        GUI.enabled = true;

        GUILayout.EndArea();
    }

    void AddCharacterBtn()
    {
        GUILayout.BeginHorizontal();

        string nextDisplayName = PlayerManager.playerModelDisplayNames[(currentModelIdx + 1) % PlayerManager.playerModelDisplayNames.Length];

        if (GameGUI.Inst.ButtonWithSound(new GUIContent("<", nextDisplayName + " Character"), GUILayout.Width(buttonWidth)))
            ChangeCharacter(false);
        
        GUILayout.Box(new GUIContent("Character", "Change Base Character"), GUILayout.Width(typeWidth));

        if (GameGUI.Inst.ButtonWithSound(new GUIContent(">", nextDisplayName + " Character"), GUILayout.Width(buttonWidth)))
            ChangeCharacter(true);

        GUILayout.EndHorizontal();
    }

    // Draws buttons for configuring a specific category of items, like pants or shoes.
    void AddCategoryBtn(string category, string displayName)
    {
        GUILayout.BeginHorizontal();

        if (GameGUI.Inst.ButtonWithSound(new GUIContent("<"),GUILayout.Width(buttonWidth)))
            ChangeElement(category, false);

        GUILayout.Box(displayName, GUILayout.Width(typeWidth));

        if (GameGUI.Inst.ButtonWithSound(new GUIContent(">"), GUILayout.Width(buttonWidth)))
            ChangeElement(category, true);

        GUILayout.EndHorizontal();
    }

    void AddRotateBtn()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.RepeatButton(new GUIContent("<", "Rotate the player left"), GUILayout.Width(buttonWidth)))
            HandleRotateBtn(true);
        GUILayout.Box(new GUIContent("Rotate", "Rotate Player"), GUILayout.Width(typeWidth));
        if (GUILayout.RepeatButton(new GUIContent(">", "Rotate the player right"), GUILayout.Width(buttonWidth)))
            HandleRotateBtn(false);
        GUILayout.EndHorizontal();
    }

    public void HandleRotateBtn(bool left)
    {
        turn += (left ? -1 : 1);
    }

    public void HandleRotateDown(bool down)
    {
        rotateBtnDown = down;
    }

    void AddRandomizeBtn()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Button(" ", GUILayout.Width(buttonWidth));
        if (GameGUI.Inst.ButtonWithSound(new GUIContent("Randomize", "Generate a Random Character"), GUILayout.Width(typeWidth)))
            HandleRandomizeBtn();
        GUILayout.Button(" ", GUILayout.Width(buttonWidth));
        GUILayout.EndHorizontal();
    }

    public void HandleRandomizeBtn()
    {
        AvatarOptionManager.Inst.CreateRandomAvatar(GameManager.Inst.LocalPlayer, avatarOptions, false);
    }

    void AddDoneBtn()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Button(" ", GUILayout.Width(buttonWidth));
        if (GameGUI.Inst.ButtonWithSound(new GUIContent("Done", "Save changes & exit"), GUILayout.Width(typeWidth)))
            HandleDoneBtn(true);
        GUILayout.Button(" ", GUILayout.Width(buttonWidth));
        GUILayout.EndHorizontal();
    }

    public void HandleDoneBtn(bool save)
    {
        if (save)
            SaveState();
        if( !GameManager.Inst.IsLoadingLevel )
            GameManager.Inst.LoadLastLevel();
    }

    public void ChangeCharacter(int modelIdx)
    {
        currentModelIdx = modelIdx;
        GameManager.Inst.playerManager.SetLocalPlayerModel(modelIdx,false);
        avatarGO = GameManager.Inst.LocalPlayer.gameObject;
        AvatarSelectionManager.Inst.InitPlayer();
        avatarOptions = new Dictionary<string, int>();
    }

    void ChangeCharacter(bool next)
    {
        int dir = next ? 1 : -1;
        int newModelIdx = (GameManager.Inst.playerManager.GetLocalPlayer().ModelIdx + dir + PlayerManager.PlayerModelNames.Length) % PlayerManager.PlayerModelNames.Length;
        ChangeCharacter(newModelIdx);
    }

    void ChangeElement(string uniqueElementName, bool next)
    {
        ChangeElement(uniqueElementName, GetResourceIdx(uniqueElementName, next));
    }

    public void ChangeElement(string uniqueElementName, int optionIdx)
    {
        if (avatarGO == null)
            avatarGO = GameManager.Inst.LocalPlayer.gameObject;
        optionIdx = optionIdx % AvatarOptionManager.Inst.NumOptions(currentModelIdx, uniqueElementName);
        avatarOptions[uniqueElementName] = optionIdx;
        AvatarOptionManager.Inst.UpdateElement(avatarGO, currentModelIdx, uniqueElementName, optionIdx);
    }

    void Update()
    {
        if (GameManager.Inst.LevelLoaded != GameManager.Level.AVATARSELECT)
            return;

        // update turning
        if (Input.GetKey(KeyCode.LeftArrow) || turn < 0)
            GameManager.Inst.LocalPlayer.gameObject.transform.RotateAround(Vector3.up, turnPlayerAngleDelta);
        if (Input.GetKey(KeyCode.RightArrow) || turn > 0)
            GameManager.Inst.LocalPlayer.gameObject.transform.RotateAround(Vector3.up, -turnPlayerAngleDelta);
        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
            rotateBtnDown = false;
        if(!rotateBtnDown)
            turn = 0;
    }

    public void OnLevelWasLoaded_()
    {
        originalModelIdx = GameManager.Inst.playerManager.GetLocalPlayer().ModelIdx;
        currentModelIdx = originalModelIdx;
        if (!GameGUI.Inst.IsVisible(GUIVis.AVATARGUI) && GameGUI.Inst.guiLayer != null)
            GameGUI.Inst.guiLayer.InitAvatarLevel(GameManager.Inst.playerManager.GetLocalPlayer().GetUnisexAvatarOptionJSON());
    }
}
