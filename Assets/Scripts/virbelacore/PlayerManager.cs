
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Awesomium.Mono;

public class PlayerManager : MonoBehaviour {

    public static PlayerManager Inst = null;

    public static PlayerController playerController = null;
    public List<GameObject> playerModels = new List<GameObject>();
    private static string[] playerModelNames = { "Avatars/male", "Avatars/sketchy_guy", "Avatars/billboard_guy"};
    private static string[] playerMedModelNames = { "Avatars/male_med", "Avatars/female_med" };
    public static string[] playerModelDisplayNames = { "Male", "Female" };
    public static string[] PlayerModelNames { get { return (GameManager.Inst.ServerConfig == "Medical" ? playerMedModelNames : playerModelNames); } }
    private GameObject localPlayerGO;
    private Player localPlayer;
    public PlayerInputManager playerInputMgr;
    private Dictionary<SFSUser, GameObject> remotePlayers = new Dictionary<SFSUser, GameObject>();
    private Dictionary<int, Player> players = new Dictionary<int, Player>(); // key == id
    private bool playerModelIsDirty = false;
    public bool playerTypeIsDirty = false;
	private float respawnHeight = -75f;
    private float maxPlayerSqDistToInterpolate = 100.0f;
    private bool avoidOtherUsersWhenSpawning = true;
    private static GameObject teleportLocationHelperGO = null;
    private static GameObject teleportDoorLocationHelperGO = null;
    private bool roomEntrySoundFlag = false;

    private float playerLastSentSpd = 0f;
    private float playerCurrentSpd = 0f;

    public static string[] maleNames = null;
    public static string[] femaleNames = null;

    public static RemoteEffectManager remoteEffectManager = null;

    private bool headTiltGlobal = true; // If true, the player's head will tilt in the direction of the cursor.
    public bool HeadTiltEnabled{
        get{ return headTiltGlobal; }
        set{ headTiltGlobal = value; }}

    public Player[] allPlayersAndBots{
        get{
            Player[] allPlayersNBots = new Player[players.Count + LocalBotManager.Inst.GetBots().Count];
            players.Values.CopyTo(allPlayersNBots, 0);
            LocalBotManager.Inst.GetBots().Values.CopyTo(allPlayersNBots, players.Count);

            return allPlayersNBots;
        }
    }

    void Awake() {
		DontDestroyOnLoad(this);
        Inst = this;

        remoteEffectManager = gameObject.AddComponent<RemoteEffectManager>();

        bool isWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        string lineEnding = isWindows ? "\r\n" : "\n";
        TextAsset maleNamesText = Resources.Load("Avatars/names_male") as TextAsset;
        maleNames = maleNamesText.text.Split(new string[] { lineEnding }, System.StringSplitOptions.RemoveEmptyEntries);

        TextAsset femaleNamesText = Resources.Load("Avatars/names_female") as TextAsset;
        femaleNames = femaleNamesText.text.Split(new string[] { lineEnding }, System.StringSplitOptions.RemoveEmptyEntries);

    }

    public void OnLevelWasLoaded_(int level) // not an override, called from GameManager so we can control ordering
    {
        if (level != 0) // connection level doesn't need an avatar
		{
            try {
                roomEntrySoundFlag = false;
                LocalBotManager.Inst.Clear();
                ClearPlayerLists();
                BuildPlayerModelList();
                if( GameGUI.Inst.guiLayer != null )
                    GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue("hideUserList(true);");
                GetRemoteUsers();
            }
            catch (System.Exception e) {
                Debug.LogError("Exception caught PlayerManager::OnLevelWasLoaded: " + e.ToString());
            }
            SpawnLocalPlayer(GetModelIdx(CommunicationManager.CurrentUserProfile.Model));
            roomEntrySoundFlag = true;
		}
		else
		{
			//may need to disconnect from smartfox here...
            if (GameGUI.Inst.guiLayer != null)
            {
                JSValue res = GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue("hideUserList(true);");
                Debug.LogError("Attempting to clear user list in Login: " + res.ToString());
            }
		}
    }

    public static int GetModelIdx(UserProfile userProfile)
    {
        return GetModelIdx(userProfile.GetField("model"));
    }

    public static int GetModelIdx(string modelIdxStr)
    {
        int modelIdx = 0;
        if (!int.TryParse(modelIdxStr, out modelIdx))
            Debug.LogError("Error converting local model index " + modelIdxStr + " to an int");
        if (modelIdx >= PlayerManager.PlayerModelNames.Length)
            Debug.LogError("Player model: " + modelIdx + " index out of range, out of date client?");
        return modelIdx % PlayerManager.PlayerModelNames.Length;
    }
	
	void ClearPlayerLists()
	{
		foreach( KeyValuePair<SFSUser, GameObject> remotePlayer in remotePlayers )
		{
			Destroy(remotePlayer.Value);
            players.Remove(remotePlayer.Key.Id);			
		}
		remotePlayers.Clear();            
	}

    void BuildPlayerModelList()
    {
        if (playerModels.Count != 0)
            return;
        foreach (string modelName in PlayerModelNames)
            playerModels.Add(Resources.Load(modelName) as GameObject);
    }

    void Update()
    {
        // Loop through all players and update their speedStopTimer.
        foreach (KeyValuePair<int, Player> player in players)
        {
            if (player.Value.IsLocal)
                continue;

            player.Value.speedStopTimer -= Time.deltaTime;

            // If speedStopTimer reaches 0, stop the animation speed.
            if (player.Value.speedStopTimer <= 0)
                player.Value.UpdateSpeed(0f);
        }

    }

    void FixedUpdate()
    {
        if( localPlayerGO != null)
            playerController = localPlayerGO.GetComponent<PlayerController>();

        if (localPlayerGO != null && BotManager.botControlled)
            BotManager.Inst.UpdateBot(playerInputMgr);

        // Check animation speed
        Animator localAnimator = null;
        if(localPlayerGO)
            localAnimator = localPlayerGO.GetComponent<Animator>();

        if( localAnimator != null )
		{
            float speed = playerController.moveSpeed;
            playerCurrentSpd = speed;
		}


        if (((localPlayerGO != null) && playerController && (playerController.positionDirty || playerController.animationDirty)))
        {
            //GameGUI.Inst.WriteToConsoleLog("Sending update from FixedUpdate(). " + Time.time);

			List<UserVariable> userVariables = new List<UserVariable>();
            if(playerController.positionDirty){
                userVariables.Add(new SFSUserVariable("x", (double)localPlayerGO.transform.position.x));
                userVariables.Add(new SFSUserVariable("y", (double)localPlayerGO.transform.position.y));
                userVariables.Add(new SFSUserVariable("z", (double)localPlayerGO.transform.position.z));
                userVariables.Add(new SFSUserVariable("rot", (double)playerController.forwardAngle));
            }
            if(playerController.animationDirty){
                userVariables.Add(new SFSUserVariable("modelAnimation", (int)playerController.playerState));
                userVariables.Add(new SFSUserVariable("gt", (double)playerController.gazePanTilt.x));
                userVariables.Add(new SFSUserVariable("gp", (double)playerController.gazePanTilt.y));
            }
            if (playerController.scaleDirty)
                userVariables.Add(new SFSUserVariable("scl", (double)localPlayerGO.transform.localScale.x));

            if( localAnimator != null )
			{
                float speed = playerController.moveSpeed;
                playerLastSentSpd = speed;
                userVariables.Add(new SFSUserVariable("spd", (double)speed));
			}

            CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
            playerController.positionDirty = false;
            playerController.animationDirty = false;
            playerController.scaleDirty = false;
        }

		if (playerModelIsDirty)
		{
			List<UserVariable> userVariables = new List<UserVariable>();
            userVariables.Add(new SFSUserVariable("playerModel", localPlayer.ModelIdx));
            CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
            playerModelIsDirty = false;
		}

        if (playerTypeIsDirty)
        {
            List<UserVariable> userVariables = new List<UserVariable>();
            userVariables.Add(new SFSUserVariable("ptype", (int)localPlayer.Type));
            CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
            playerTypeIsDirty = false;
        }

		if (localPlayerGO != null && localPlayerGO.transform.position.y <= respawnHeight)
		{
			//Moves player to first teleport location
            SoundManager.Inst.PlayTeleport();
			SetLocalPlayerTransform(GetLocalSpawnTransform());
            //localPlayer.EnableRigidBody = false;
		}
	}

    public void Shutdown()
    {
        RemoveLocalPlayer();
    }

    //----------------------------------------------------------
    // SmartFox callbacks
    //----------------------------------------------------------

    public void OnUserExitRoom(BaseEvent evt)
    {
        // Someone left - lets make certain they are removed if they didnt nicely send a remove command
        SFSUser user = (SFSUser)evt.Params["user"];
        Room room = (Room)evt.Params["room"];
        Debug.LogError(user.Name + " left the room " + (( room != null ) ? room.Name : ""));
        
        Player p = null;
        if (TryGetPlayer(user.Id, out p) && (GameManager.Inst.LocalPlayer == null || user.Name != GameManager.Inst.LocalPlayer.SFSName) && p.Type != PlayerType.STEALTH)
            SoundManager.Inst.PlayExit();
        
        if( room != null && p != null && CommunicationManager.IsPrivateRoom(room.Name) )
            GameGUI.Inst.ExecuteJavascriptOnGui(p.GetUserExitPrivateRoomJSCmd(room.Name));

        if( !CommunicationManager.IsPrivateRoom(room.Name) )
            RemoveRemotePlayer(user);
    }


    public void OnUserEnterRoom(BaseEvent evt)
    {
		if (evt == null)
		{
			Debug.Log("self localplayer has just joined default Room");
		}
		else
		{
		    SFSRoom room = (SFSRoom)evt.Params["room"];
		    SFSUser user = (SFSUser)evt.Params["user"];
            Debug.Log("User: " + user.Name + " has just joined Room: " + room.Name);

            //lets spawn the remote user after some delay so server can receive its position
            delayedSpawnRemotePlayer(user, room.Name, 1);

            Player player = null;
            if (TryGetPlayer(user.Id, out player) && CommunicationManager.IsPrivateRoom(room.Name))
            {
                GameGUI.Inst.ExecuteJavascriptOnGui(player.GetUserEnterPrivateRoomJSCmd(room.Name));
                Debug.LogError(user.Name + " entered private room");
            }
		}
    }

    public void OnUserVariableUpdate(BaseEvent evt)
    {


        // When user variable is updated on any client, then this callback is being received

        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        SFSUser user = (SFSUser)evt.Params["user"];
		
        if (user == CommunicationManager.MySelf || user == null)
            return;

        //GameGUI.Inst.WriteToConsoleLog("OnUserVariableUpdate() " + Time.time);


        if (!remotePlayers.ContainsKey(user))
            SpawnRemotePlayer(user);

        Player remotePlayer = players[user.Id];

        try {
            // Check if the remote user changed his position or rotation
            if (changedVars.Contains("x") || changedVars.Contains("y") || changedVars.Contains("z") || changedVars.Contains("rot"))
            {
                float rotAngle = (float)user.GetVariable("rot").GetDoubleValue();
                bool rotChanged = Mathf.Abs(rotAngle - remotePlayer.playerController.forwardAngle) > 0.01f;
                Vector3 newPos = new Vector3((float)user.GetVariable("x").GetDoubleValue(), (float)user.GetVariable("y").GetDoubleValue(), (float)user.GetVariable("z").GetDoubleValue());
                remotePlayer.UpdateTransform(newPos, rotAngle);
                if (rotChanged)
                    remotePlayer.playerController.shuffleTime = 0.25f;
            }

            if (changedVars.Contains("scl"))
                remotePlayer.UpdateScale(user.GetVariable("scl"));

            if (changedVars.Contains("modelAnimation"))
                remotePlayer.UpdateAnimState(user.GetVariable("modelAnimation"));

            if (changedVars.Contains("spd"))
                remotePlayer.UpdateSpeed(user.GetVariable("spd"));

            if (changedVars.Contains("gt")){
                remotePlayer.playerController.gazePanTilt.x = (float)user.GetVariable("gt").GetDoubleValue();
                remotePlayer.playerController.gazePanTilt.y = (float)user.GetVariable("gp").GetDoubleValue();
            }

            if (changedVars.Contains("ptype"))
                remotePlayer.UpdateType(user.GetVariable("ptype"));

            if (changedVars.Contains("playerModel"))
            {
                SetRemotePlayerModel(user, user.GetVariable("playerModel").GetIntValue());
                Debug.Log("remote changed model!:" + user.GetVariable("playerModel").GetIntValue());
            }
        }
        catch (KeyNotFoundException e) {
            Debug.LogError("KeyNotFoundException, changed vars dont exist for user, msg from different level? " + e.StackTrace);
        }
    }

    public Transform GetClosestTransformAvoidingPlayers(Transform desiredTransform)
    {
        if (desiredTransform == null)
            return null; // if you get here from GetLocalSpawnTransform, add a TeleportLocation to your scene!

        float minDist = 1.3f; // minimum distance you are allowed to spawn from another player.
        float dir = (players.Count % 2 == 0) ? 1.0f : -1.0f; // switch spawning to the right or left of the transform.

        // don't want to modify the Transform, because it will move the original, making a copy to move.
        if (teleportLocationHelperGO == null)
        {
            teleportLocationHelperGO = new GameObject("TeleportLocationHelper");
            DontDestroyOnLoad(teleportLocationHelperGO);
        }

        teleportLocationHelperGO.transform.position = desiredTransform.position;
        teleportLocationHelperGO.transform.rotation = desiredTransform.rotation;
        int tryCount = 0;
        int bailCount = 0; // peace of mind to avoid infinite loop
        while (tryCount < players.Count && bailCount < 200)
        {
            ++bailCount;
            foreach (KeyValuePair<int, Player> player in players)
            {
                if (player.Value.IsLocal)
                    continue;
                Vector3 diff = teleportLocationHelperGO.transform.position - player.Value.gameObject.transform.position;
                diff.y = 0;
                if (diff.sqrMagnitude < minDist)
                {
                    teleportLocationHelperGO.transform.position += dir * minDist * teleportLocationHelperGO.transform.right;
                    tryCount = 0;
                    break;
                }
                else
                    ++tryCount;
            }
        }
        return teleportLocationHelperGO.transform;
    }

    public Transform GetLocalSpawnTransform()
    {
        Transform spawnTransform = null;
        Transform defaultTransform = null;
        foreach (TeleportLocation tele in TeleportLocation.GetAll())
        {
            if ( tele != null && tele.spawnPtName.LastIndexOf(GameManager.Inst.LastLevel.ToString()) != -1 )
                spawnTransform = tele.gameObject.transform;
            Door lastRoomDoor;
            if (spawnTransform == null && Door.GetDoorForReEntry(GameManager.Inst.LastLevel, GameManager.Inst.LastRoomID, out lastRoomDoor))
            {
                if (teleportDoorLocationHelperGO == null)
                {
                    teleportDoorLocationHelperGO = new GameObject("TeleportDoorLocationHelper");
                    DontDestroyOnLoad(teleportDoorLocationHelperGO);
                }
                teleportDoorLocationHelperGO.transform.rotation = lastRoomDoor.transform.rotation;
                teleportDoorLocationHelperGO.transform.position = lastRoomDoor.transform.position + 4f * lastRoomDoor.transform.forward;
                spawnTransform = teleportDoorLocationHelperGO.transform;
            }
            if (tele != null && defaultTransform == null && tele.spawnPtName.LastIndexOf('*') == tele.spawnPtName.Length - 1)
                defaultTransform = tele.gameObject.transform;
        }
        if (spawnTransform == null)
            spawnTransform = defaultTransform != null ? defaultTransform : TeleportLocation.GetAll()[0].gameObject.transform;
        return (avoidOtherUsersWhenSpawning) ? GetClosestTransformAvoidingPlayers(spawnTransform) : spawnTransform;
    }

    private void CreateLocalPlayerGO(int modelIdx)
    {
        GameObject avatarGO = playerModels[modelIdx];
        Transform initTransform = GetLocalSpawnTransform();

        // See if there already exists a model - if so, take its pos+rot before destroying it
        if (localPlayerGO != null)
        {
            initTransform = localPlayerGO.transform;
            playerInputMgr = null;
            Destroy(localPlayerGO);
        }

        localPlayerGO = GameObject.Instantiate(avatarGO) as GameObject;
        SetLocalPlayerTransform(initTransform);
        playerInputMgr = localPlayerGO.AddComponent<PlayerInputManager>();
    }

    private void SpawnLocalPlayer(int modelIdx)
    {
        CreateLocalPlayerGO(modelIdx);
        localPlayer = new Player(CommunicationManager.MySelf, localPlayerGO, modelIdx, CommunicationManager.CurrentUserProfile);

        UpdateLocalSFSUser();
        SetDefaultCamSettings();

        players[CommunicationManager.MySelf.Id] = localPlayer;
        if (GameManager.Inst.LevelLoaded != GameManager.Level.AVATARSELECT && GameGUI.Inst.guiLayer != null)
        {
            string cmd = localPlayer.GetAddToGUIUserListJSCmd() + "showUserList();";
            GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue(cmd);
        }
    }

    private void SetDefaultCamSettings()
    {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
        {
            // set default camera settings.
            MainCameraController.Inst.followPlayerHeight = 4;
            MainCameraController.Inst.followPlayerDistance = 9;
            MainCameraController.Inst.rightOffset = 1;
        }
        else if (GameManager.Inst.LevelLoaded == GameManager.Level.TEAMROOM)
        {
            MainCameraController.Inst.followPlayerHeight = 4;
            MainCameraController.Inst.followPlayerDistance = 5;
            MainCameraController.Inst.rightOffset = 0;
        }
        else if (GameManager.Inst.LevelLoaded == GameManager.Level.CMDROOM)
        {
            MainCameraController.Inst.followPlayerHeight = 4;
            MainCameraController.Inst.followPlayerDistance = 6;
            MainCameraController.Inst.rightOffset = 0;
        }
    }

    public static PlayerInit InitPlayerVariablesFromUserVariables(List<UserVariable> vars)
    {
        PlayerInit pInit = new PlayerInit();
        pInit.scale = 1f;

        for (int i = 0; i < vars.Count; ++i)
        {
            switch (vars[i].Name)
            {
                case "rot":
                    pInit.rotAngle = (float)vars[i].GetDoubleValue();
                    break;
                case "x":
                    pInit.posx = (float)vars[i].GetDoubleValue();
                    break;
                case "y":
                    pInit.posy = (float)vars[i].GetDoubleValue();
                    break;
                case "z":
                    pInit.posz = (float)vars[i].GetDoubleValue();
                    break;
                case "playerModel":
                    pInit.modelIndex = vars[i].GetIntValue();
                    break;
                case "team":
                    pInit.teamID = vars[i].GetIntValue();
                    break;
                case "ptype":
                    pInit.ptype = (PlayerType)vars[i].GetIntValue();
                    break;
                case "op":
                    pInit.optionsStr = vars[i].GetStringValue();
                    break;
                case "parseId":
                    pInit.parseID = vars[i].GetStringValue();
                    break;
                case "displayName":
                    pInit.displayName = vars[i].GetStringValue();
                    break;
                case "sit":
                    pInit.sit = vars[i].GetBoolValue();
                    break;
                case "scl":
                    pInit.scale = (float)vars[i].GetDoubleValue();
                    break;
            }
        }

        return pInit;
        

    }

    private void SpawnRemotePlayer(SFSUser user, string room = "")
    {
        VDebug.Log ("Spawning user: " + user.Name + " room: " + room);

        // New client just started transmitting - lets create remote player
        PlayerInit pInit = InitPlayerVariablesFromUserVariables(user.GetVariables());
        pInit.displayName = (string.IsNullOrEmpty(pInit.displayName)) ? user.Name : pInit.displayName;


        // See if there already exists a model so we can destroy it first
        if (remotePlayers.ContainsKey(user) && remotePlayers[user] != null)
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
            players.Remove(user.Id);
            if (user.Name == GetLocalPlayer().SFSName)
            {
                Debug.LogError("Caught someone logging in with my name, logging out");
                CommunicationManager.Inst.LogOut();
                return;
            }
        }

        if (pInit.ptype != PlayerType.STEALTH && roomEntrySoundFlag)
            SoundManager.Inst.PlayEnter();

        Player remotePlayer = CreateRemotePlayer(user, pInit.modelIndex, pInit.Pos, pInit.Rot, pInit.optionsStr, pInit.parseID, pInit.displayName, pInit.teamID, pInit.ptype, pInit.sit);
        remotePlayer.UpdateScale(pInit.scale);

        if (CommunicationManager.IsPrivateRoom(room))
            GameGUI.Inst.ExecuteJavascriptOnGui(remotePlayer.GetUserEnterPrivateRoomJSCmd(room));
    }

    public void SetRemotePlayerModel(SFSUser user, int modelIndex)
    {
        if (user == CommunicationManager.MySelf) return;
        if (remotePlayers.ContainsKey(user))
        {
            PlayerType pType = PlayerType.NORMAL;
            Player p;
            if(players.TryGetValue(user.Id, out p))
                pType = p.Type;
            Vector3 pos = remotePlayers[user].transform.position;
            Quaternion rot = remotePlayers[user].transform.rotation;
            string displayName = (user.ContainsVariable("displayName")) ? user.GetVariable("displayName").GetStringValue() : user.Name;
            CreateRemotePlayer(user, modelIndex, pos, rot, user.GetVariable("op").GetStringValue(), user.GetVariable("parseId").GetStringValue(), displayName, user.GetVariable("team").GetIntValue(), pType, user.GetVariable("sit").GetBoolValue());
        }
    }

    public GameObject CreateRemotePlayerGO(int modelIdx)
    {
        GameObject remotePlayerGO = new GameObject();
        if (modelIdx >= playerModels.Count)
        {
            Debug.LogError("modelIndex out of range, modelIndex: " + modelIdx + " num player models: " + playerModels.Count);
            if (playerModels.Count == 0)
                BuildPlayerModelList();
        }

        GameObject avatarGO = playerModels[modelIdx];
        remotePlayerGO = GameObject.Instantiate(avatarGO) as GameObject;

        remotePlayerGO.AddComponent<SimpleRemoteInterpolation>();
        return remotePlayerGO;
    }

    private Player CreateRemotePlayer(SFSUser user, int modelIndex, Vector3 pos, Quaternion rot, string optionsStr, string parseId, string displayName, int teamID, PlayerType ptype, bool isSitting)
    {
        GameObject remotePlayerGO = CreateRemotePlayerGO(modelIndex);
        remotePlayerGO.GetComponent<SimpleRemoteInterpolation>().SetTransform(pos, rot, false);
        GameObject oldGO = null;
        if (remotePlayers.TryGetValue(user, out oldGO) && oldGO != null)
            Destroy(oldGO);
        remotePlayers[user] = remotePlayerGO;
        Player remotePlayer;
        if (players.TryGetValue(user.Id, out remotePlayer))
        {
            remotePlayer.ModelIdx = modelIndex;
            remotePlayer.gameObject = remotePlayerGO;
            remotePlayer.ParseId = parseId;
            remotePlayer.DisplayName = displayName;
            remotePlayer.Type = ptype;
        }
        else
        {
            remotePlayer = new Player(user, remotePlayerGO, modelIndex, ptype, parseId, displayName, teamID);
            players.Add(user.Id, remotePlayer);
            if (parseId != "" && GameGUI.Inst.guiLayer != null)
                GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue(remotePlayer.GetAddToGUIUserListJSCmd());
        }
        remotePlayer.UpdateTransform(pos, rot.eulerAngles.y);
        remotePlayer.ApplyAvatarOptions(optionsStr);
        if (isSitting)
            remotePlayer.Sit();
        return remotePlayer;
    }

    IEnumerator delayedSpawnRemotePlayer(SFSUser user, string room, int delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnRemotePlayer(user, room);
    }

    void GetRemoteUsers()
    {
        if (CommunicationManager.LastJoinedRoom == null)
            return;
        foreach (SFSUser u in CommunicationManager.LastJoinedRoom.UserList)
        {
             if (u == CommunicationManager.MySelf)
                continue;
             try {
                 SpawnRemotePlayer(u);
             }
             catch (System.Exception e) {
                 Debug.LogError("Caught exception spawning remote player: " + e.ToString());
             }
        }
    }

    void UpdateLocalSFSUser()
    {
        //GameGUI.Inst.WriteToConsoleLog("UpdateLocalSFSUser() " + Time.time);

        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("x", (double)localPlayer.gameObject.transform.position.x));
        userVariables.Add(new SFSUserVariable("y", (double)localPlayer.gameObject.transform.position.y));
        userVariables.Add(new SFSUserVariable("z", (double)localPlayer.gameObject.transform.position.z));
        userVariables.Add(new SFSUserVariable("rot", (double)localPlayer.gameObject.transform.rotation.eulerAngles.y));
        userVariables.Add(new SFSUserVariable("modelAnimation", (playerController != null) ? (int)playerController.playerState : 0));
        userVariables.Add(new SFSUserVariable("playerModel", (int)localPlayer.ModelIdx));
        userVariables.Add(new SFSUserVariable("op", localPlayer.GetAvatarOptionStr()));
        userVariables.Add(new SFSUserVariable("parseId", CommunicationManager.CurrentUserProfile.UserID));
        userVariables.Add(new SFSUserVariable("displayName", CommunicationManager.CurrentUserProfile.DisplayName));
        userVariables.Add(new SFSUserVariable("ptype", (int)localPlayer.Type));
        userVariables.Add(new SFSUserVariable("team", (int)localPlayer.TeamID));
        userVariables.Add(new SFSUserVariable("sit", localPlayer.IsSitting));

        Animator localAnimator = localPlayerGO.GetComponent<Animator>();

        if ((localAnimator != null) && (playerController != null))
		{
            float speed = playerController.moveSpeed;
            userVariables.Add(new SFSUserVariable("spd", (double)speed));
		}
        
        CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
    }

    private void RemoveLocalPlayer()
    {
        // Someone dropped off the grid. Lets remove him
        SFSObject obj = new SFSObject();
        obj.PutUtfString("type", "cmd");
        obj.PutUtfString("cmd", "rm");
        CommunicationManager.SendObjectMsg(obj);
    }

    public void RemoveRemotePlayer(SFSUser user)
    {
        if (user == CommunicationManager.MySelf) return;

        if (remotePlayers.ContainsKey(user))
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
            string cmd = "removeFromUserList({name:\"" + players[user.Id].Name + "\",userid:\"" + players[user.Id].ParseId + "\"});";
            players.Remove(user.Id);
            if( GameGUI.Inst.guiLayer != null )
                GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue(cmd);
        }
    }

    public void SetLocalPlayerTransform(Transform transform, bool snapCamera = true)
    {
        if (localPlayerGO != null && transform != null)
        {
            localPlayerGO.transform.position = transform.position;
            localPlayerGO.transform.rotation = transform.rotation;
            PlayerController playerController = localPlayerGO.GetComponent<PlayerController>();
            playerController.forwardAngle = transform.eulerAngles.y;
            playerController.StopMomentum();
            playerController.SetNavDestination(transform.position, transform.eulerAngles.y);
            if (snapCamera)
                MainCameraController.Inst.CameraToInitialPos();              
        }
    }

    public void SetLocalPlayerTransform(Vector3 newPosition, Quaternion newRotation)
    {
        if (localPlayerGO != null)
        {
            localPlayerGO.transform.position = newPosition;
            localPlayerGO.transform.rotation = newRotation;
            localPlayerGO.GetComponent<PlayerController>().forwardAngle = newRotation.eulerAngles.y;
        }
    }

    public void SetLocalPlayerModel(int modelIndex, bool saveToDatabase = true)
    {
        CreateLocalPlayerGO(modelIndex);
        localPlayer.ModelIdx = modelIndex;
        localPlayer.gameObject = localPlayerGO;
        localPlayer.ApplyAvatarOptions(CommunicationManager.CurrentUserProfile);

        playerModelIsDirty = true; // update smartfox user
        if (saveToDatabase)
            CommunicationManager.CurrentUserProfile.UpdateProfile("model", modelIndex.ToString());
    }

    public void SetAllPlayerVisibility(bool visible)
    {
        foreach (KeyValuePair<int, Player> player in players)
            player.Value.Visible = visible;
    }

    public Player GetLocalPlayer()
    {
        return localPlayer;
    }

    public Player GetPlayer(int id)
    {
        Player p = null;
        if (!players.TryGetValue(id, out p) || ReplayManager.Initialized && p == GameManager.Inst.LocalPlayer)
        {
            if (ReplayManager.Initialized && !ReplayManager.Inst.replayPlayers.TryGetValue(id, out p))
                 Debug.LogError("Could not find player " + id + " in player dictionary or replay players");
            if( p == null )
                Debug.LogError("Could not find player " + id);
        }
        return p;
    }

    public bool TryGetPlayer(int id, out Player player)
    {
        if (!players.TryGetValue(id, out player) || ReplayManager.Initialized && player == GameManager.Inst.LocalPlayer)
            return ReplayManager.Initialized && ReplayManager.Inst.replayPlayers.TryGetValue(id, out player);
        return true;
    }
	
	public IEnumerator GetEnumerator()
	{
		return players.GetEnumerator();
	}

    public Player GetPlayerByName(string name)
    {
        return GetPlayerByName(players, name);
    }

    public Player GetPlayerOrBotByName(string name){
        Player gotPlayer = null;
        gotPlayer = GetPlayerByName(name);

        if(gotPlayer == null)
            gotPlayer = LocalBotManager.Inst.GetBotByName(name);

        return gotPlayer;
    }

    public static Player GetPlayerByName(Dictionary<int, Player> players, string name)
    {
        string lowercaseName = name.ToLower();
        foreach (KeyValuePair<int, Player> playerPair in players)
        {
            if (playerPair.Value.DisplayName.ToLower() == lowercaseName)
                return playerPair.Value;
        }
        return null;
    }

    //----------------------------------------------------------
    // Accessors
    //----------------------------------------------------------

    public int NumPlayers { get { return players.Count; } }
}
