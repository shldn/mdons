using UnityEngine;
using Sfs2X.Entities.Variables;
using System.Collections.Generic;

public enum PlayerType
{
    NORMAL = 0,   // participants with the least amount of rights
    LEADER = 1,   // leader of a group, presenter, facilitator, etc
    STEALTH = 2,  // invisible user
    MODERATOR = 4,// moderator, more rights than Leaders
    ADMIN = 8,    // most amount of rights to control sim.
}

public enum AvatarType
{
    NORMAL = 0,     // professional business clothing
    MEDICAL = 1,    // nurse, doctor clothing
    STUDENT = 2,    // more casual clothing
    PATIENT = 3,    // hospital patient clothing
}

public struct PlayerInit
{
    public int teamID;
    public int modelIndex;
    public float rotAngle;
    public float posx;
    public float posy;
    public float posz;
    public string optionsStr;
    public string parseID;
    public string displayName;
    public PlayerType ptype;
    public bool sit;

    public Vector3 Pos { get { return new Vector3(posx, posy, posz); } }
    public Quaternion Rot { get { return Quaternion.Euler(0, (float)rotAngle, 0); } }    
}

public class Player {

    // Boolean 'exists' comparison; replaces "!= null".
    public static implicit operator bool(Player exists){
        return exists != null;
    } // End of boolean operator.

    public Player(Sfs2X.Entities.User user_, GameObject go_, int modelIdx_, PlayerType pType_, string parseId_, string displayName_, int teamID_)
    {
        Init(user_, go_, modelIdx_, pType_, parseId_, displayName_, teamID_);
    }

    public Player(Sfs2X.Entities.User user_, GameObject go_, PlayerInit init)
    {
        Init(user_, go_, init.modelIndex, init.ptype, init.parseID, init.displayName, init.teamID);
    }

    public Player(Sfs2X.Entities.User user_, GameObject go_, UserProfile userProfile)
    {
        int modelIdx_ = PlayerManager.GetModelIdx(userProfile);
        PlayerType pType = GetPlayerType(userProfile.GetField("permissionType"));
        Init(user_, go_, modelIdx_, pType, userProfile.UserID, userProfile.DisplayName, int.Parse((userProfile.TeamID == "" ? "0" : userProfile.TeamID)));
        ApplyAvatarOptions(userProfile);
    }

    public Player(Sfs2X.Entities.User user_, GameObject go_, int modelIdx_, UserProfile userProfile)
    {
        PlayerType pType = GetPlayerType(userProfile.GetField("permissionType"));
        Init(user_, go_, modelIdx_, pType, userProfile.UserID, userProfile.DisplayName, int.Parse((userProfile.TeamID == "" ? "0" : userProfile.TeamID)));
        ApplyAvatarOptions(userProfile);
    }

    public static PlayerType prevType = PlayerType.LEADER; // hacky way to toggle between stealth player and remember previous mode

    private Sfs2X.Entities.User user;
    private PlayerType playerType;
    private AvatarType avatarType;
    private GameObject go;
    private bool isTalking = false;
    private bool isTyping = false;
    private bool isTeleporting = false;
    private bool visible = true;
    private bool visibilityHasBeenSet = false;
    private float maxSqDistToInterpolate = 144.0f;
    private string parseId = "-1";
    private string displayName;
    private int teamID = 0;
    private string mOptionStr = "";
    private TextMesh nameTextMesh = null;
    private GameObject isTalkingGO = null;
    private GameObject isTypingGO = null;
    private GameObject teleportGO = null;
    private GameObject minimapIconGO = null;
    private Stack<GameObject> disabledGOs = new Stack<GameObject>();
    private	Color[] colors = new Color[]{new Color(0.55f,0.21f,0.67f,0.57f),  // purple
        							     new Color(0.12f,0.40f,0.97f,0.57f),  // blue
							             new Color(0.93f,0.00f,0.26f,0.57f),  // red
							             new Color(0.00f,0.40f,0.22f,0.57f),  // green
    							         new Color(1.00f,0.95f,0.00f,0.57f),  // yellow
							             new Color(0.12f,0.97f,0.90f,0.57f),  // cyan
							             new Color(0.95f,0.40f,0.73f,0.57f),  // pink
    							         new Color(0.51f,0.84f,0.40f,0.57f),  // lime
							             new Color(0.96f,0.57f,0.12f,0.57f)   // orange
							            };

    private ConfettiController confettiController = null;
    public float speedStopTimer = 0f;
    public PlayerController playerController = null;
    private SitController sitController = null;

    private Vector3 lastUpdatePos = Vector3.zero;
    private float lastUpdateRot = 0f;
	
    // Accessors
    public GameObject gameObject { get { return go; } 
        set { 
            go = value; 
            nameTextMesh = go.GetComponentInChildren<TextMesh>();
            if (nameTextMesh != null)
            {
                nameTextMesh.color = OpaqueColor; // font color -- for show thru walls mode
                nameTextMesh.GetComponent<Renderer>().material.color = OpaqueColor; // material color -- for 3D text
            }
            Transform talkBubbleTrans = go.transform.Find("talk_bubble");
            if (talkBubbleTrans != null)
            {
                isTalkingGO = talkBubbleTrans.gameObject;
                isTalkingGO.SetActive(false);
            }
            Transform typeIconTrans = go.transform.Find("typing_icon");
            if (typeIconTrans != null)
            {
                isTypingGO = typeIconTrans.gameObject;
                isTypingGO.SetActive(false);
            }
            Transform teleportTrans = go.transform.Find("TeleportEffect");
            if (teleportTrans != null)
            {
                teleportGO = teleportTrans.gameObject;
            }
            Transform miniMapIcon = go.transform.Find("map_arrow");
            if (miniMapIcon != null)
            {
                minimapIconGO = miniMapIcon.gameObject;
                if (IsLocal)
                    minimapIconGO.transform.localScale *= 1.5f;
                minimapIconGO.GetComponent<Renderer>().material.color = OpaqueColor;
            }

            playerController = go.GetComponent<PlayerController>();
            playerController.playerScript = this;
        }
    }
    public Sfs2X.Entities.User User { get { return user; } }
    public PlayerType Type { get { return playerType; } set { playerType = value; Visible = (playerType != PlayerType.STEALTH); } }
    public AvatarType AvatarType { get { return avatarType; } set { avatarType = value; } } 
    public string SFSName { get { return user.Name; } }
    public string Name { get { return DisplayName; } }
    public int Id { get { return user.Id; } }
    public int PlayerId { get { return (CommunicationManager.InASmartFoxRoom ? user.PlayerId : 0); } }
    public int ModelIdx { get; set; } // this needs to be refactored, should set this and spawn the new avatar gameObject based on it.
    public string ParseId { get { return parseId; } set { parseId = value; } }
    public string DisplayName { get { return (displayName == "") ? user.Name : displayName; } set { displayName = (value.Length >= 5 && value.Substring(0, 5) == "guest") ? SFSName : value; nameTextMesh.text = displayName; } }
    public int TeamID { get { return teamID; } set { teamID = value; } }
    public bool IsLocal { get { return CommunicationManager.MySelf == user; } } // is this the player I'm controlling
    public bool IsRemote { get { return !IsLocal; } }
    public bool IsStealth { get { return Type == PlayerType.STEALTH; } }
    public bool IsSitting { get { return IsLocal ? sitController.IsSitting : gameObject.GetComponent<AnimatorHelper>().IsSitting(); } }
    private int ColorIdx { get { return (PlayerId != 0 ? PlayerId : Id) % colors.Length; } }
    public Color Color { get { return colors[ColorIdx]; } }
    public Color OpaqueColor { get { Color c = colors[ColorIdx]; c.a = 1.0f; return c; } }
    public PrivateVolume InPrivateVolume { get; set; }
    public Vector3 HeadPosition { get { return gameObject.transform.position + (Vector3.up * 3f * Scale.x); } }
    public Vector3 HeadTopPosition { get { return gameObject.transform.position + (Vector3.up * 3.44f * Scale.x); } }
    public Vector3 Scale { get { return gameObject.transform.localScale; } set { gameObject.transform.localScale = value; playerController.scaleDirty = true; } }
    public bool isBot = false;
    public bool Visible
    {
        get { return visible; }

        set
        {
            if (gameObject == null || (IsStealth && value == true) || (visible == value && visibilityHasBeenSet) )
                return;
            visible = value;
            visibilityHasBeenSet = true;

            if (IsRemote)
                gameObject.SetActive(visible);
            if (IsLocal)
            {                
                // Turning off children still allows input to effect player
                if (!visible)
                {
                    Transform[] ts = gameObject.GetComponentsInChildren<Transform>(); // can't use this approach when they are disabled, GetComponents function will return nothing.
                    for (int i = 0; i < ts.Length; ++i)
                        if (ts[i].gameObject != gameObject)
                        {
                            ts[i].gameObject.SetActive(visible);
                            disabledGOs.Push(ts[i].gameObject);
                        }
                }
                else
                {
                    if (disabledGOs.Count == 0)
                        ApplyAvatarOptions(GetAvatarOptionStr());
                    else
                        while (disabledGOs.Count > 0)
                            disabledGOs.Pop().SetActive(visible);

                    if (!IsTalking)
                        isTalkingGO.SetActive(false);
                    if (!IsTyping)
                        isTypingGO.SetActive(false);
                }

            }
            gameObject.layer = LayerMask.NameToLayer(visible ? "Default" : "Ignore Raycast");
        }
    }

    public bool IsTalking { 
        get { return isTalking; }
        set {
            if (isTalking != value)
            {
                isTalking = value;
                if (isTalkingGO != null)
                    isTalkingGO.SetActive(isTalking);
                string cmd = "updateUserTalk({userid:\"" + parseId + "\", name : \"" + Name + "\"}, " + isTalking.ToString().ToLower() + ");";
                GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
            }
        }
    }

    public bool IsTyping
    {
        get { return isTyping; }
        set
        {
            if (isTyping != value)
            {
                isTyping = value;
                if (isTypingGO != null)
                    isTypingGO.SetActive(isTyping);
                string cmd = "updateUserTyping({userid:\"" + parseId + "\", name : \"" + Name + "\"}, " + isTyping.ToString().ToLower() + ");";
                GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
            }
        }
    }

    public bool IsTeleporting
    {
        get { return isTeleporting; }
        set
        {
            if (isTeleporting != value)
            {
                isTeleporting = value;
                if (isTeleporting != null)
                {
                    foreach (ParticleEmitter emitter in teleportGO.GetComponentsInChildren<ParticleEmitter>())
                    {
                        emitter.emit = value;
                    }
                }
                //string cmd = "updateUserTalk({userid:\"" + parseId + "\", name : \"" + Name + "\"}, " + isTalking.ToString().ToLower() + ");";
                //GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
            }
        }
    }


    public void BotGoto(Vector3 pos, float arrivalRot){
        if(isBot){
            playerController.SetNavDestination(pos, arrivalRot);
        }
    }
    public void BotGoto(Vector3 pos){
        if(isBot){
            playerController.SetNavDestination(pos);
        }
    }
    public void BotFollow(Player p)
    {
        if (isBot)
            playerController.FollowPlayer(p);
    }
    public void BotStopFollow()
    {
        if (isBot)
            playerController.StopFollowingPlayer();
    }


    public bool EnableRigidBody {
        set {
        /*
            if (value == true) {
                if (gameObject.rigidbody == null)
                    gameObject.AddComponent<Rigidbody>();
                if (gameObject.GetComponent<CapsuleCollider>() == null) {
                    CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
                    CharacterController controller = gameObject.GetComponent<CharacterController>();
                    collider.radius = controller.radius;
                    collider.height = controller.height;
                    collider.center = controller.center;
                }
            }
            else {
                if (gameObject.rigidbody != null)
                    GameObject.Destroy(gameObject.rigidbody);
                if (gameObject.GetComponent<CapsuleCollider>() != null) {
                    gameObject.GetComponent<CapsuleCollider>().enabled = false;
                    GameObject.Destroy(gameObject.GetComponent<CapsuleCollider>());
                }
            }
            gameObject.GetComponent<CharacterController>().enabled = !value;
            gameObject.GetComponent<PlayerController>().enabled = !value;
        */
        }
    }

    private void Init(Sfs2X.Entities.User user_, GameObject go_, int modelIdx_, PlayerType pType_, string parseId_, string displayName_, int teamID_)
    {
        user = user_;
        gameObject = go_;
        playerType = pType_;
        ModelIdx = modelIdx_;
        ParseId = (parseId_ == null) ? "" : parseId_;
        DisplayName = (displayName_ == null) ? "" : displayName_;
        TeamID = teamID_;
        InPrivateVolume = null;
        if (IsStealth)
        {
            ApplyAvatarOptions(GetAvatarOptionStr());
            Visible = false;
        }
        if (CameraMoveManager.Enabled)
            SetNameBillboardCam(CameraMoveManager.Inst.Cam);

        sitController = new SitController(this);

        // Check hardware info for changes.
        if (IsLocal)
        {
            string oldHardwareInfo = CommunicationManager.CurrentUserProfile.GetLocalHardwareInfo();
            CommunicationManager.CurrentUserProfile.UpdateLocalHardwareInfo();

            // If user's hardware has changed, send the new info to Parse.
            if (oldHardwareInfo != CommunicationManager.CurrentUserProfile.GetLocalHardwareInfo())
                CommunicationManager.CurrentUserProfile.UpdateParseHardwareInfo();
        }
    }

    public void DisableNameTextMesh()
    {
        if( nameTextMesh != null )
            nameTextMesh.gameObject.SetActive(false);
    }

    public void SetNameTextColor(int id)
    {
        if (nameTextMesh != null)
        {
            Color c = colors[id % colors.Length];
            c.a = 1.0f;
            nameTextMesh.color = c;
            nameTextMesh.GetComponent<Renderer>().material.color = c;
        }
    }

    public void SetNameBillboardCam(Camera cam)
    {
        if (nameTextMesh != null)
            nameTextMesh.GetComponent<Billboard>().cam = cam;
        if (isTalkingGO != null)
            isTalkingGO.GetComponent<Billboard>().cam = cam;
    }

    public void UpdateTransform(Vector3 newPos, float rotAngle)
    {
        //GameGUI.Inst.WriteToConsoleLog("Updating transform!");

        bool interpolate = (newPos - gameObject.transform.position).sqrMagnitude < maxSqDistToInterpolate;
        interpolate = true;
        gameObject.GetComponent<SimpleRemoteInterpolation>().SetTransform(newPos, Quaternion.Euler(0, rotAngle, 0), interpolate);

        playerController.forwardAngle = rotAngle;

        speedStopTimer = 2f;
    }

    public void UpdateScale(UserVariable userVar)
    {
        UpdateScale((float)userVar.GetDoubleValue());
    }

    public void UpdateScale(float scale)
    {
        gameObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void UpdateSpeed(UserVariable userVar) {
        UpdateSpeed((float)userVar.GetDoubleValue());
    }

    public void UpdateSpeed(float spd)
    {
        PlayerController playerController = gameObject.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.moveSpeed = spd;
    }

    public void UpdateAnimState(UserVariable userVar){
        UpdateAnimState(userVar.GetIntValue());
    }

    public void UpdateAnimState(int animState)
    {
        CharacterAnimator animator = gameObject.GetComponent<CharacterAnimator>();
        if (animator != null)
            animator.SetCurrentAnimation(animState);
    }

    public void UpdateType(UserVariable userVar){
        UpdateType(userVar.GetIntValue());
    }

    public void UpdateType(int newType) {
        Type = (PlayerType)newType;
    }

    public void UpdateGaze(string userVarName, float newVal)
    {
        if (userVarName == "gt")
            playerController.gazePanTilt.x = newVal;
        if (userVarName == "gp")
            playerController.gazePanTilt.y = newVal;
    }

    public void UpdateUserVar(UserVariable userVar, bool fromReplay = false)
    {
        if (userVar.Name == "spd")
            UpdateSpeed(userVar);
        else if (userVar.Name == "ptype")
            UpdateType(userVar);
        else if (userVar.Name == "modelAnimation")
            UpdateAnimState(userVar);
        else if (userVar.Name == "op")
            ApplyAvatarOptions(userVar.GetStringValue());
        else if (userVar.Name == "parseId")
        {
            string postfix = fromReplay ? ">" : "";
            ParseId = userVar.GetStringValue() + postfix;
        }
        else if (userVar.Name == "displayName")
            DisplayName = userVar.GetStringValue();
        else if (userVar.Name == "team")
            teamID = userVar.GetIntValue();
        else if (userVar.Name == "gt" || userVar.Name == "gp")
            UpdateGaze(userVar.Name, (float)userVar.GetDoubleValue());
        else if (userVar.Name == "sit")
            UpdateSit(userVar.GetBoolValue());
        else if (userVar.Name == "scl")
            UpdateScale((float)userVar.GetDoubleValue());
        else
            Debug.LogError("Unsupported userVar: " + userVar.Name);
    }

    public void ApplyAvatarOptions(string optionStr)
    {
        mOptionStr = optionStr;
        if (optionStr == null)
        {
            Debug.LogError("ApplyAvatarOptions: option str is null");
            return; 
        }
        string[] options = optionStr.Split(',');
        for (int i = 0; options != null && i < options.Length; ++i)
        {
            if (i >= AvatarOptionManager.Inst.OptionTypes[ModelIdx].Count)
            {
                Debug.LogError("Encountered more avatar options than this client has, must be an out of date client");
                return;
            }
            int newIdx = 0;
            if (options != null && options.Length > 0 && int.TryParse(options[i], out newIdx))
                AvatarOptionManager.Inst.UpdateElement(gameObject, ModelIdx, AvatarOptionManager.Inst.OptionTypes[ModelIdx][i], newIdx);
        }

        // fix for bug in unity > 4.2.2
#if !UNITY_4_2 || UNITY_WEBPLAYER
        gameObject.SetActive(true);
#endif
    }

    public void ApplyAvatarOptions(UserProfile userProfile)
    {
        if (Type == PlayerType.STEALTH)
            return;
        int numOptionsDefined = 0;
        foreach (string option in AvatarOptionManager.Inst.OptionTypes[ModelIdx])
        {
            int fieldIdx = 0;
            if (int.TryParse(userProfile.GetField(option), out fieldIdx))
                ++numOptionsDefined;
            AvatarOptionManager.Inst.UpdateElement(gameObject, ModelIdx, option, fieldIdx);
        }
        if (numOptionsDefined == 0)
        {
            Debug.LogError("Character has " + numOptionsDefined + " options set");

            Dictionary<string, int> avatarOptions = new Dictionary<string, int>();
            AvatarOptionManager.Inst.CreateRandomAvatar(this, avatarOptions, false);
            
            if (GameManager.Inst.ServerConfig == "Medical")
            {
                // force scrubs by default
                AvatarOptionManager.Inst.UpdateElement(gameObject, ModelIdx, "Shirt", 0);
                AvatarOptionManager.Inst.UpdateElement(gameObject, ModelIdx, "Jacket", 0);
            }

            AvatarOptionManager.Inst.SaveStateToServer(this, avatarOptions);
        }

        // invalidate cached option string if it is queried.
        mOptionStr = "";

        // fix for bug in unity > 4.2.2
#if !UNITY_4_2 || UNITY_WEBPLAYER
        gameObject.SetActive(true);
#endif
    }

    public string GetAvatarOptionStr()
    {
        if (mOptionStr != "")
            return mOptionStr;

        if (!IsLocal)
        {
            Debug.LogError("GetAvatarOptionStr is only valid for the local player at the moment");
            return "";
        }

        string customizationStr = "";
        foreach (string option in AvatarOptionManager.Inst.OptionTypes[ModelIdx])
        {
            if (customizationStr != "")
                customizationStr += ",";
            customizationStr += CommunicationManager.CurrentUserProfile.GetField(option);
        }
        return customizationStr;
    }

    private string AddModelTypeOptionJSON(ref string jsonStr, int modelIdx)
    {
        if (!IsLocal)
            Debug.LogError("AddModelTypeOptionJSON is only valid for the local player at the moment");

        // add each avatar customization option
        foreach (string option in AvatarOptionManager.Inst.OptionTypes[modelIdx])
            jsonStr += "\"" + option + "\":\"" + CommunicationManager.CurrentUserProfile.GetField(option) + "\",";
        if (jsonStr != "")
            jsonStr = "{" + jsonStr.Remove(jsonStr.Length - 1) + "}"; // remove the last comma, put in brackets
        return jsonStr;
    }

    public string GetAvatarOptionJSON()
    {
        string jsonStr = "\"Model\":\"" + ModelIdx.ToString() + "\",";
        return AddModelTypeOptionJSON(ref jsonStr, ModelIdx);
    }

    // Includes male options for the female, to make switching between genders easier in the ui.
    public string GetUnisexAvatarOptionJSON()
    {
        string jsonStr = "\"Model\":\"" + ModelIdx.ToString() + "\",";
        return AddModelTypeOptionJSON(ref jsonStr, AvatarOptionManager.MALE);
    }

    // helpers
    public static PlayerType GetPlayerType(string playerTypeStr)
    {
        string playerTypeStrLC = playerTypeStr.ToLower();
        if (playerTypeStrLC == "admin" || playerTypeStrLC == "a")
            return PlayerType.ADMIN;
        if (playerTypeStrLC == "moderator" || playerTypeStrLC == "m")
            return PlayerType.ADMIN;
        if (playerTypeStrLC == "facilitator" || playerTypeStrLC == "f" || playerTypeStrLC == "leader" || playerTypeStrLC == "l")
            return PlayerType.LEADER;
        if (playerTypeStrLC == "stealth" || playerTypeStrLC == "s" || playerTypeStrLC == "hidden" || playerTypeStrLC == "h" || playerTypeStrLC == "invisible" || playerTypeStrLC == "i")
            return PlayerType.STEALTH;
        return PlayerType.NORMAL;
    }

    public string GetPlayerTypeStr()
    {
        switch (Type)
        {
            case PlayerType.ADMIN:
                return "a";
            case PlayerType.LEADER:
                return "f";
            case PlayerType.STEALTH:
                return "s";
            case PlayerType.NORMAL:
                return "n";
        }
        return "";
    }

    private bool IsGuest()
    {
        return (SFSName.Length >= 5 && SFSName.Substring(0, 5) == "guest");
    }

    private string GetUserListJson(string room = "")
    {
        string jsonStr = "{userid:\"" + ParseId + "\", permissionType : \"" + GetPlayerTypeStr() + "\", isCurrent : " + (GameManager.Inst.LocalPlayer == this ? "true" : "false");
        if( IsGuest() )
            jsonStr += ", name : \"" + SFSName + "\"";
        else
            jsonStr += ", name : \"" + DisplayName + "\"";
        if( room != "" )
            jsonStr += ", room : \"" + room + "\"";
        jsonStr += "}";
        return jsonStr;
    }

    public string GetAddToGUIUserListJSCmd() {
        return "addToUserList(" + GetUserListJson() + ");";
    }

    public string GetUserEnterPrivateRoomJSCmd(string room)
    {
        return "handleUserEntersPrivateRoom(" + GetUserListJson(room) + ");";
    }
    public string GetUserExitPrivateRoomJSCmd(string room)
    {
        return "handleUserExitsPrivateRoom(" + GetUserListJson(room) + ");";
    }

    public void ConfettiActive(bool showConfetti)
    {
        if(confettiController == null){
            GameObject confettiGO = MonoBehaviour.Instantiate(Resources.Load("Avatars/Effects/confetti", typeof(GameObject))) as GameObject;
            confettiGO.transform.parent = go.transform;
            confettiGO.transform.localPosition = nameTextMesh.transform.localPosition;
            confettiController = confettiGO.GetComponent<ConfettiController>();
        }

        confettiController.Emit = showConfetti;
    }

    public void Sit()
    {
        sitController.Sit();
    }

    public void Stand()
    {
        sitController.Stand();
    }

    public void UpdateSit(bool sit)
    {
        if (sit)
            Sit();
        else
            Stand();
    }

}
