using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Awesomium.Mono;
using Awesomium.Unity;
using Boomlagoon.JSON;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Core;
using Sfs2X.Requests;
using System.IO;
using Sfs2X.Entities.Variables;

public enum ConsoleCommandEventType
{
	ONEVAL = 0,
	ONVALIDATION = 1
}

public class ConsoleInput
{
    public ConsoleInput(string str_, PlayerType permission_)
    {
        str = str_;
        permission = permission_;
    }
    public string str;
    public PlayerType permission;
}

public class ConsoleInterpreter : MonoBehaviour {
	public class ConsoleCommandEventArgs : EventArgs
	{
		ConsoleCommandEventType type;
		object data;
		public ConsoleCommandEventArgs(ConsoleCommandEventType t=ConsoleCommandEventType.ONEVAL, object d=null) {type = t; data=d;}
		public ConsoleCommandEventType eventType { get{ return type;} set{ type = value;} }
		public object eventData { get{ return data;} set{ data = value;} }
	}

    public class ConsoleCommand : IComparable<ConsoleCommand>
	{
	    public ConsoleCommand(string name_, int maxArgs_ = 1, PlayerType minRequiredPermissions_ = PlayerType.NORMAL, string[] alias_ = null, string helpText_ = "", int minArgs_ = 1, bool hidden_ = false)
	    {
	        name = name_;
			maxArgs = maxArgs_;
			minArgs = minArgs_;
            hidden = hidden_;
            minRequiredPermissions = minRequiredPermissions_;
	        alias = alias_;
			if(helpText_ == "")
		        helpText = "Usage: /" + name + "args...";
			else
				helpText = helpText_;
		}

		public string name;
		public string[] alias;
		public string helpText;
		public int maxArgs;
		public int minArgs;
        public bool hidden;
        public PlayerType minRequiredPermissions;
		public delegate void EvalDelegate(string[] args);
		public EvalDelegate Eval;
		public delegate void CommandEventHandler(object sender, ConsoleCommandEventArgs e, string error);
		public event CommandEventHandler Evaluated;

        public int CompareTo(ConsoleCommand rhs)
        {
            return String.Compare(name, rhs.name);
        }
		public virtual void OnEval(ConsoleCommandEventArgs e, string error)
		{
			if (Evaluated != null)
				Evaluated(this, e, error);
		}

        private bool HandleDelayOption(string input)
        {
            int delayIdx = input.LastIndexOf(" delay ", input.Length - 1, Math.Min(input.Length - 1, 20));
            if (delayIdx != -1)
            {
                float delaySeconds = 0;
                int delayTimeStartIdx = delayIdx + " delay ".Length;
                if (float.TryParse(input.Substring(delayTimeStartIdx, input.Length - delayTimeStartIdx), out delaySeconds))
                    ConsoleInterpreter.Inst.ExecuteDelayedCommand(input.Substring(0, delayIdx), delaySeconds);
                else
                    GameGUI.Inst.WriteToConsoleLog("Delay seconds parsing failed, please enter a valid number");
                return true;
            }
            return false;
        }
		
		public void ValidateCommandArgs(string input)
		{
			string error = null;
			string[] args = null;
			string[] ret = null;
            if (input != null && input != "")
            {
                // check if we should delay this command
                if (HandleDelayOption(input))
                    return;

                // split into arguments 
                args = input.Split(new char[] { ' ' }, maxArgs);

            }
            else
            {
                error = "Unknown error. Bad input: " + input;
                OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONVALIDATION, ret), error);
                return;
            }
			if (maxArgs > 0 )
			{
				if(args.Length == maxArgs || args.Length >= minArgs)
				{
					ret = args;
					foreach (string a in args)
					{
						if (a == null || a == "")
						{
							error = "Invalid argument.";
							ret = null;
							break;
						}
					}
				}
				else if (args.Length == 1)
					ret = new string[]{};
				else
					error = "Bad arguments\n" +helpText;
			}
			else
			{
				ret = args;
				VDebug.Log(input + " validated!\nExecuting!!");
			}
			OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONVALIDATION, ret), error);
		}
	}

	public Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();
    ConsoleCommand[] commandDefs = { 
		new ConsoleCommand("whisper", 3, PlayerType.NORMAL, new string[]{"w", "tell", "pm"}, ""),
        new ConsoleCommand("broadcast", 2, PlayerType.LEADER, new string[]{"messagetoall", "msgtoall", "bm", "broadcastmessage"}, ""),
        new ConsoleCommand("broadcasturl", 2, PlayerType.LEADER, new string[]{"urltoall", "websitetoall", "bu"}, ""),
        new ConsoleCommand("broadcastroom", 3, PlayerType.LEADER, new string[]{"broadcastrm", "messagetoallrooms", "msgtoallrms"}, "", 3, true),
        new ConsoleCommand("broadcastroomurl", 3, PlayerType.LEADER, new string[]{"urltoallrooms", "websitetoallrooms", "urltoallrms"}, "", 3, true),
		new ConsoleCommand("freelookcam", 2, PlayerType.LEADER, new string[]{"freelook"}, "args: on, off or snap. \"snap\" centers cam on the bizsim planes"),
		new ConsoleCommand("changecam", 3, PlayerType.LEADER, new string[]{"switchcam", "sc", "cam", "camera"}),
		new ConsoleCommand("camconfig", 3, PlayerType.LEADER, new string[]{"camsettings", "camcf", "cameraconfig", "camerasettings"}),
		new ConsoleCommand("help", 2, PlayerType.NORMAL, new string[]{"?"}),
		new ConsoleCommand("toggleconsole", 1, PlayerType.NORMAL, new string[]{"tc"}),
        new ConsoleCommand("consolesize", 3, PlayerType.NORMAL, new string[]{"cs"},"Changes the size of the console/console log.", 1, true),
        new ConsoleCommand("consoleindent", 3, PlayerType.NORMAL, new string[]{"ci"},"Changes the indent of the console/console log.", 1, true),
		new ConsoleCommand("setwallcolor", 2, PlayerType.ADMIN, new string[]{"setwallbg", "setwallbgc", "swc"}),
        new ConsoleCommand("floorvis", 2, PlayerType.ADMIN, new string[]{"floor"}),
        new ConsoleCommand("obstvis", 2, PlayerType.ADMIN, new string[]{"obst", "obstacles"}),
        new ConsoleCommand("guivis", 2, PlayerType.ADMIN, new string[]{"gui"}),
        new ConsoleCommand("toggleobjecttooltips", 1, PlayerType.ADMIN, new string[]{"objecttips"}),
		new ConsoleCommand("debugmsg", 2, PlayerType.ADMIN, new string[]{"debugmsgs", "dbg", "debug"}),
        new ConsoleCommand("confetti", 99, PlayerType.ADMIN, new string[]{"conf"}, "Activates or deactivates victory confetti effect for team(s).", 1, true),
        new ConsoleCommand("update", 99, PlayerType.LEADER, new string[]{"up"}, "Updates the information given as argument. ('schedule', 'team')", 2, true),
		new ConsoleCommand("restCommand", 4, PlayerType.ADMIN, new string[]{"rest", "db", "data"}, "Issue REST commands to Parse.com database.", 3),
		new ConsoleCommand("user", 4, PlayerType.ADMIN, new string[]{"profile"}, "Interface to interact with user profiles database.", 2),
		new ConsoleCommand("parse", 6, PlayerType.ADMIN, new string[]{"object, obj"}, "Interface to interact with object database.", 2),
        new ConsoleCommand("slidegen", 2, PlayerType.ADMIN, new string[]{"htmlgen"}, "Generate slideshow html from folder of images.", 2),
        new ConsoleCommand("doclistgen", 2, PlayerType.LEADER, new string[]{"docslistgen","doclist","docslist","drivelist"}, "Generate slideshow html from folder of images.", 2, true),
		new ConsoleCommand("loadlevel", 4, PlayerType.LEADER, new string[]{"load"},"args: bizsim, teamrm, campus, orient, avatar, nav, second arg to specify team instance(0 based)"),
		new ConsoleCommand("teleport",2, PlayerType.LEADER, new string[]{"tp", "tele"}),
        new ConsoleCommand("present",2, PlayerType.LEADER, new string[]{"presentation", "slides", "url"}, "arg: url you want to share"),
        new ConsoleCommand("loadassets",2, PlayerType.ADMIN, new string[]{"assets", "bundle", "decor"}, "arg: url of assetBundle you wish to load"),
        new ConsoleCommand("voice",5, PlayerType.NORMAL,new string[]{"voicechat"},"args: minfreq [\"ave\" or floatvalue] or feedbackdetect. (minfreq ave offset floatvalue - to set offset above average)"),
        new ConsoleCommand("guilayer", 99, PlayerType.LEADER, new string[]{"htmllayer","htmlui","ui"}, "reload: to reload the guilayer", 2),
        new ConsoleCommand("refresh", 1, PlayerType.NORMAL, new string[]{"reload"},"Refreshes all browsers - BizSim only"),
        new ConsoleCommand("stealth", 1, PlayerType.LEADER, new string[]{"hideuser", "showuser", "unhideuser", "uservis"},"Toggle your user visibility"),
        new ConsoleCommand("facilitator", 1, PlayerType.LEADER, new string[]{"fac"},"Enables or disables facilitator cameras - BizSim only"),
        new ConsoleCommand("replay", 99, PlayerType.LEADER, new string[]{"loadmessages"},"Replay game events from file, args: (filename startMsg numMsgsToLoad) or once file is loaded: \"jump\" msgIndex "),
        new ConsoleCommand("cache", 2, PlayerType.NORMAL,null,"/cache clear will clear the awesomium cache",1,true),
        new ConsoleCommand("resolution", 3, PlayerType.NORMAL, new string[]{"res"},"Set resolution while game is running",3, true),
        new ConsoleCommand("fullscreen", 2, PlayerType.NORMAL, null,"Set fullscreen on/off", 1, true),
        new ConsoleCommand("mic", 2, PlayerType.NORMAL, null, "Set the microphone device", 2, true),
        new ConsoleCommand("setmic", 1, PlayerType.NORMAL, null, "Select the microphone to use.", 1, true),
        new ConsoleCommand("sound", 2, PlayerType.NORMAL, null, "Open control panel sound options", 1, true),
        new ConsoleCommand("info", 2, PlayerType.NORMAL, null, "Display info message", 1, true),
        new ConsoleCommand("consolewrite", 2, PlayerType.NORMAL, null, "Write to the console", 1, true),
        new ConsoleCommand("wave", 1, PlayerType.NORMAL, new string[]{},"Wave animation"),
        new ConsoleCommand("dance", 1, PlayerType.NORMAL, new string[]{"gangnam"},"Dance animation"),
        new ConsoleCommand("confuse", 1, PlayerType.NORMAL, new string[]{"confused"},"Confused animation"),
        new ConsoleCommand("cry", 1, PlayerType.NORMAL, new string[]{"sad"},"Cry animation"),
        new ConsoleCommand("clap", 1, PlayerType.NORMAL, null,"Clap animation"),
        new ConsoleCommand("cheer", 1, PlayerType.NORMAL, new string[]{"happy"},"Cheer animation"),
        new ConsoleCommand("impatient", 1, PlayerType.NORMAL, new string[]{"toetap"},"Impatient animation"),
        new ConsoleCommand("think", 1, PlayerType.NORMAL, new string[]{"ponder"},"Think animation"),
        new ConsoleCommand("powerpose", 1, PlayerType.NORMAL, new string[]{"power"},"PowerPose animation"),
        new ConsoleCommand("laugh", 1, PlayerType.NORMAL, new string[]{"haha"},"Laugh animation"),
        new ConsoleCommand("samba", 1, PlayerType.NORMAL, null,"Samba animation"),
        new ConsoleCommand("comehere", 1, PlayerType.NORMAL, new string[]{"followme"},"Come here animation"),
        new ConsoleCommand("raisehand", 1, PlayerType.NORMAL, new string[]{"question", "handraise"},"Raise hand animation"),
        new ConsoleCommand("bow", 1, PlayerType.NORMAL, null,"Bow animation"),
        new ConsoleCommand("shakehand", 1, PlayerType.NORMAL, new string[]{"shake", "shakehands"},"Shake hands animation"),
        new ConsoleCommand("backflip", 1, PlayerType.NORMAL, new string[]{"flip"},"Back flip animation"),
        new ConsoleCommand("sit", 1, PlayerType.NORMAL, new string[]{"sitdown"},"Sit animation"),
        new ConsoleCommand("unsit", 1, PlayerType.NORMAL, new string[]{"stand", "stopsit", "stopsitting", "quitsit", "sitquit", "sitexit", "exitsit"},"Stops the sit animation"),
        new ConsoleCommand("ron", 99, PlayerType.LEADER, null,"Create a ron bot"),
        new ConsoleCommand("bot", 99, PlayerType.LEADER, null,"Create a bot"),
        new ConsoleCommand("botcmd", 99, PlayerType.LEADER, null,"Issue cmds to bots, specify name to apply to one bot, otherwise applied to all\noptions: name, talk, type, walk, remove\nExample: /botcmd name Erik anim wave", 1, true),
        new ConsoleCommand("goto", 2, PlayerType.NORMAL, new string[]{"mt", "moveto", "walkto"}, "Will move your avatar to someone else."),
        new ConsoleCommand("follow", 2, PlayerType.NORMAL, new string[]{"fp"}, "Will cause your avatar to follow someone else until you interrupt."),
        new ConsoleCommand("summonaudience", 2, PlayerType.NORMAL, new string[]{}, "", 1, true),
        new ConsoleCommand("clicktomove", 2, PlayerType.NORMAL, new string[]{"clickmove","moveclick"}, "", 1, true),
        new ConsoleCommand("headfollowmouse", 2, PlayerType.NORMAL, new string[]{"headtiltenable"}, "", 1, true),
        new ConsoleCommand("camfollowmouse", 2, PlayerType.NORMAL, new string[]{"camtiltenable"}, "", 1, true),
        new ConsoleCommand("lookatspeaker", 2, PlayerType.NORMAL, new string[]{"focusonspeaker"}, "", 1, true),
        new ConsoleCommand("lookatspeed", 2, PlayerType.NORMAL, new string[]{"lookspeed"}, "", 1, true),
        new ConsoleCommand("lights", 3, PlayerType.NORMAL, new string[]{"dimmer", "light"}, "", 1, true),
        new ConsoleCommand("minimap", 2, PlayerType.LEADER, new string[]{"minimapvis"}, "", 2, true),
        new ConsoleCommand("lookat", 99, PlayerType.NORMAL, new string[]{"look"}, "", 1, true),
        new ConsoleCommand("bizsim", 99, PlayerType.LEADER, null, "Control bizsim settings"),
        new ConsoleCommand("script", 2, PlayerType.LEADER, null, "Loads a script of commands"),
        new ConsoleCommand("record", 2, PlayerType.LEADER, null, "Start recording messages, save to disk"),
        new ConsoleCommand("webpanel", 4, PlayerType.LEADER, null, "Control actions for a webpanel, zoomin, change url, etc.",3,true),
        new ConsoleCommand("focusedwebpanel", 3, PlayerType.LEADER, null, "Control actions for a webpanel, zoomin, change url, etc.",2,true),
        new ConsoleCommand("privatebrowser", 20, PlayerType.NORMAL, new string[]{"privatebrowse", "pb"}, "Opens a private browser overlay ", 2, true),
        new ConsoleCommand("download", 2, PlayerType.LEADER, new string[]{"dl", "dnld"}, "Downloads a file from the web", 1, true),
        new ConsoleCommand("upload", 2, PlayerType.LEADER, new string[]{"upld"}, "Uploads a file to a server", 1, true),
        new ConsoleCommand("uploadvirbeo", 2, PlayerType.LEADER, new string[]{"upldv"}, "Uploads a file to a server", 1, true),
        new ConsoleCommand("silencecone", 2, PlayerType.LEADER, new string[]{"scone", "coneofsilence","grouppods","pods", "cos"}, "Enables all Cone of Silences in a Room", 1, true),
        new ConsoleCommand("winmove", 3, PlayerType.NORMAL, new string[]{"windowmove", "winmv", "movewin", "wmove"}, "", 3, true),
        new ConsoleCommand("quit", 1, PlayerType.NORMAL, new string[]{"exit"},"Quits the game")

	};
	public RESThelper restHelper = null;

    private Queue<ConsoleInput> consoleInputQueue = new Queue<ConsoleInput>();
	private static ConsoleInterpreter mInstance;
	public static ConsoleInterpreter Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("ConsoleInterpreter")).AddComponent(typeof(ConsoleInterpreter)) as ConsoleInterpreter;
            }
            return mInstance;
        }
    }   
	public static ConsoleInterpreter Inst
	{
		get{return Instance;}
	}	
    public void Touch() { }
	
	void Start()
	{
		restHelper = new RESThelper("https://api.parse.com/1/", new Dictionary<string, string>());
        UserProfile currentUserProfile = CommunicationManager.CurrentUserProfile;
		
		for(int i=0; i<commandDefs.Length; i++)
		{
			ConsoleCommand c = commandDefs[i];
			//add into our dictionary of commands
			commands.Add("/"+c.name, c);
			//map all the alias to ouur dictionary too
			if (c.alias != null)
				foreach(string a in c.alias)
					commands.Add("/"+a, c);
			//add a default OnEval handler
			c.Evaluated += delegate(object sender, ConsoleCommandEventArgs e, string error) {
				ConsoleCommand sentCommand = (ConsoleCommand)sender;
				if (error != null && error != "")
				{
					GameGUI.Inst.WriteToConsoleLog("Command " + sentCommand.name + " Error: " + error + "\nInput: "+ e.ToString());
				}
				else
				{
					if (e.eventType == ConsoleCommandEventType.ONEVAL)
					{
						Debug.Log("Command " + sentCommand.name + " success!");
					}
					else if (e.eventType == ConsoleCommandEventType.ONVALIDATION)
					{
						if( c != null && c.Eval != null)
						{
							c.Eval((string[])e.eventData);
							Debug.Log("Command " + sentCommand.name + " Validated!\nInput: "+ e.eventData.ToString());
						}
					}
				}
			};			
			if (c.name == "whisper")
			{
				//define the eval delegate
				c.Eval = delegate(string[] args) {
					string error = null;
					if(args.Length == 3)
					{
                        if (args[1].ToLower() == "help" || args[1].ToLower() == "techhelp")
                            GameGUI.Inst.guiLayer.HandleHelpMsg(args[1], args[2]);
						else if (!ChatManager.Inst.sendPrivateMsg(args[2], args[1]))
							error = "Failed to send message.";
					}
					else if (args.Length == 1)
						error = "Need to specify a user name." + c.helpText;
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if (c.name == "broadcast")
			{
				//define the eval delegate
				c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length == 2)
                    {
                        Debug.LogError("Sending adminMsg request");
                        ISFSObject msgObj = new SFSObject();
                        msgObj.PutUtfString("msg", args[1]);
                        ExtensionRequest request = new ExtensionRequest("admMsg", msgObj);
                        CommunicationManager.SendMsg(request);
                    }
                    else
                        error = "Need to specify a message to send." + c.helpText;
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "broadcasturl")
			{
				//define the eval delegate
				c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length == 2)
                    {
                        Debug.LogError("Sending adminMsg request with url");
                        ISFSObject msgObj = new SFSObject();
                        msgObj.PutUtfString("msg", WebStringHelpers.CreateValidURLOrSearch(args[1]));
                        ExtensionRequest request = new ExtensionRequest("admMsg", msgObj);
                        CommunicationManager.SendMsg(request);
                    }
                    else
                        error = "Need to specify a url to send." + c.helpText;
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if (c.name == "broadcastroom")
			{
				//define the eval delegate
				c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length == 3)
                    {
                        ISFSObject msgObj = new SFSObject();
                        msgObj.PutUtfString("room", args[1]);
                        msgObj.PutUtfString("msg", args[2]);
                        ExtensionRequest request = new ExtensionRequest("admRmMsg", msgObj);
                        CommunicationManager.SendMsg(request);
                    }
                    else
                        error = "Need to specify a room and message to send." + c.helpText;
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "broadcastroomurl")
			{
				//define the eval delegate
				c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length == 3)
                    {
                        ISFSObject msgObj = new SFSObject();
                        msgObj.PutUtfString("room", args[1]);
                        msgObj.PutUtfString("msg", WebStringHelpers.CreateValidURLOrSearch(args[2]));
                        ExtensionRequest request = new ExtensionRequest("admRmMsg", msgObj);
                        CommunicationManager.SendMsg(request);
                    }
                    else
                        error = "Need to specify a room and url to send." + c.helpText;
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if (c.name == "help")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					string helpText = "Press Enter to gain/release focus, up arrow shows the last command.\nSupported commands:\n";
                    bool showHidden = (args[args.Length - 1].Trim() == "all");
                    Array.Sort(commandDefs);
                    foreach (ConsoleCommand cmd in commandDefs)
                    {
                        if (!HasValidPermissions(cmd, GameManager.Inst.LocalPlayerType) || (cmd.hidden && !showHidden))
                            continue;
                        helpText += "/" + cmd.name;
                        if (cmd.alias != null && cmd.alias.Length > 0)
                        {
                            helpText += "        {";
                            foreach (string alt in cmd.alias)
                                helpText += "  /" + alt;
                            helpText += "  }";
                        }
                        helpText += "\n";
                    }
					GameGUI.Inst.WriteToConsoleLog(helpText);
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if(c.name == "freelookcam")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					if(args.Length == 2)
					{
						if (args[1] == "on" || args[1] == "snap")
						{
							MainCameraController.Inst.cameraType = CameraType.FREELOOK;
							GameGUI.Inst.WriteToConsoleLog("Camera Type Changed to: " + MainCameraController.Inst.cameraType);
							if( args[1] == "snap" )
								BizSimManager.Inst.SnapCameraToSeeAllBizSim();
						}
						else if (args[1] == "off")
						{
                            MainCameraController.Inst.cameraType = CameraType.FOLLOWPLAYER;
                            GameGUI.Inst.WriteToConsoleLog("Camera Type Changed to: " + MainCameraController.Inst.cameraType);
						}
						else
							error = "Arguments can only be on or off\n" + c.helpText;
					}
					else if(args.Length == 1)
                        MainCameraController.Inst.CycleCamera();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if(c.name == "changecam")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length == 1)
                    {
                        MainCameraController.Inst.CycleCamera();
                        GameGUI.Inst.WriteToConsoleLog("Camera Type Changed to: " + MainCameraController.Inst.cameraType);
                    }
                    else if( args.Length > 1 )
                    {
                        if (args[1] == "record" || args[1] == "rec")
                            CameraMoveManager.Enabled = true;
                        else if (args[1] == "exit" || args[1] == "quit" || args[1] == "back" || args[1] == "playercam")
                            CameraMoveManager.Enabled = false;
                        else
                        {
                            // look for player name
                            Player p = GameManager.Inst.playerManager.GetPlayerByName((args[1]));
                            GameObject targetGO = (p != null) ? p.gameObject : null;
                            if (targetGO == null)
                            {
                                if (ReplayManager.Initialized)
                                {
                                    p = PlayerManager.GetPlayerByName(ReplayManager.Inst.replayPlayers, args[1]);
                                    targetGO = (p != null) ? p.gameObject : null;
                                }
                            }
                            if( targetGO == null )
                                targetGO = GameObject.Find(args[1]);
                            if (targetGO != null)
                            {
                                bool follow = true;
                                if( args.Length > 2)
                                    follow = !(args[2] == "nofollow" || args[2] == "nf" || args[2] == "detach");
                                Debug.LogError("Focus camera on a gameobject " + targetGO.name);
                                CameraMoveManager.Inst.SetCameraTarget(targetGO, follow);
                            }
                            else
                                Debug.LogError("Couldn\'t find game object or player " + args[1]);

                        }
                    }
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "camconfig")
            {
                c.Eval = delegate(string[] args) {
					string error = null;
                    if (args.Length > 1)
                    {
                        float spd = 1.0f;
                        if (args[1] == "speed" && float.TryParse(args[2], out spd))
                        {
                            MainCameraController.CameraDriveSpeed = spd;
                            GameGUI.Inst.WriteToConsoleLog("Camera drive speed = " + MainCameraController.CameraDriveSpeed);
                        }
                        else if ((args[1] == "mousespeed" || args[1] == "mouselookspeed") && float.TryParse(args[2], out spd))
                        {
                            MainCameraController.MouseLookSpeed = spd;
                            GameGUI.Inst.WriteToConsoleLog("Mouse look speed = " + MainCameraController.MouseLookSpeed);
                        }
                    }
                    else
                        error = "specify speed or mousespeed";
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "toggleconsole")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 1)
                        GameGUI.Inst.ToggleDisplayConsole();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "consolesize")
			{
				c.Eval = delegate(string[] args)
                {
					string error = null;
                    int newWidth = 0;
                    int newHeight = 0;
                    if ((args.Length == 2) && int.TryParse(args[1].Trim(), out newWidth))
                    {
                        ConsoleGUI.consoleWidth = newWidth;
                    }
                    else if ((args.Length == 3) && int.TryParse(args[1].Trim(), out newWidth) && int.TryParse(args[2].Trim(), out newHeight))
                    {
                        ConsoleGUI.consoleWidth = newWidth;
                        ConsoleGUI.consoleHeightMax = newHeight;
                    }
                    else
                    {
                        error = "Please provide an integer width or width/height.";
                    }
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "consoleindent")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    int newLeftIndent = 0;
                    int newBottomIndent = 0;
                    if ((args.Length == 3) && int.TryParse(args[1].Trim(), out newLeftIndent) && int.TryParse(args[2].Trim(), out newBottomIndent))
                    {
                        ConsoleGUI.consoleLeftIndent = newLeftIndent;
                        ConsoleGUI.consoleBottomIndent = newBottomIndent;
                    }
                    else
                    {
                        error = "Please provide an integer left and right indent.";
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
			else if(c.name == "setwallcolor")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					string bgColor = "transparent";
					if(args == null || args.Length <= 1)
						error = "Background color not defined, using default: " + bgColor;
					if(args.Length == 2)
					{
						bgColor = args[1];
					}
                    foreach (BizSimScreen s in BizSimScreen.GetAll()) {
							s.bTex.SetBodyBGColor(bgColor);
					}
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            /*
            else if (c.name == "floorvis")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    bool enable = false;
                    if (args.Length == 1)
                        enable = !InvisiblePathManager.Inst.FloorVisible;
                    else if (args.Length > 1)
                    {
                        if (args[1] == "on")
                            enable = true;
                        else if (args[1] == "off")
                            enable = false;
                        else
                            error = "arguments: on or off " + c.helpText;
                    }
                    if (error == null)
                        InvisiblePathManager.Inst.ShowHideFloor(enable);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "obstvis")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    bool enable = false;
                    if (args.Length == 1)
                        enable = !InvisiblePathManager.Inst.ObstaclesVisible;
                    else if (args.Length > 1)
                    {
                        if (args[1] == "on")
                            enable = true;
                        else if (args[1] == "off")
                            enable = false;
                        else
                            error = "arguments: on or off " + c.helpText;
                    }
                    if (error == null)
                        InvisiblePathManager.Inst.ShowHideObstacles(enable);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            */
            else if (c.name == "guivis")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    int devIdx = Array.IndexOf(args, "dev");
                    if (devIdx == -1)
                    {
                        GameGUI.Inst.Visible = false;
                        GameGUI.Inst.guiLayer.Visible = false;
                    }
                    else
                        GameGUI.Inst.DevGUIEnabled = !GameGUI.Inst.DevGUIEnabled;

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "toggleobjecttooltips")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;

                    GameGUI.Inst.objectTooltips = !GameGUI.Inst.objectTooltips;
                    GameGUI.Inst.WriteToConsoleLog("Object tooltips " + (GameGUI.Inst.objectTooltips ? "on." : "off."));

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "guilayer")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    bool enable = !GameGUI.Inst.DevGUIEnabled;
                    if (args.Length >= 2)
                    {
                        if (args[1] == "reload" || args[1] == "refresh")
                            GameGUI.Inst.guiLayer.ReloadLayer();
                        else if (args[1] == "js")
                        {
                            string cmd = "";
                            for (int k = 2; k < args.Length; k++)
                                cmd += args[k] + " ";
                            JSValue ret = GameGUI.Inst.guiLayer.ExecuteJavascriptWithValue(cmd);
                            if( !string.IsNullOrEmpty(ret.ToString()) )
                                GameGUI.Inst.WriteToConsoleLog(ret.ToString());
                        }
                    }
                    if( error == null )
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
			else if (c.name == "debugmsg")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					bool toggle = (args.Length == 0); // if no args are given, toggle the value
					bool enable = false;
					if (args.Length > 1)
					{
						if (args[1] == "on")
							enable = true;
						else if (args[1] == "off")
							enable = false;
						else
							error = "arguments: on or off " + c.helpText;
					}
					if (toggle)
						enable = GameGUI.Inst.ToggleDebugConsoleMsg();	//reusing the enable var for getting current status
					else
						GameGUI.Inst.SetDebugConsoleMsg(enable);
					GameGUI.Inst.WriteToConsoleLog("Debug messages are now " + (enable ? "enabled." : "disabled."));
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "confetti")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    bool validCommand = true;

                    // Setting confetti on or off?
                    bool confettiOn = false;
                    if (args[1].Equals("on"))
                        confettiOn = true;
                    else if (args[1].Equals("off"))
                        confettiOn = false;
                    else
                    {
                        error = "Please designate whether confetti should be 'on' or 'off'.";
                        validCommand = false;
                    }

                    if (validCommand)
                    {
                        // Determine which teams we're affecting.
                        int[] teams = new int[args.Length - 2];
                        for (int j = 0; j < args.Length - 2; j++)
                        {
                            if (!(int.TryParse(args[j + 2], out teams[j])))
                            {
                                error = "Invalid team(s). Please provide integer team IDs separated by spaces.";
                                validCommand = false;
                                break;
                            }
                        }

                        if (validCommand && (teams.Length == 0))
                        {
                            error = "Please provide integer team IDs separated by spaces.";
                            validCommand = false;
                        }

                        if (validCommand)
                        {
                            foreach (KeyValuePair<int, Player> playerPair in GameManager.Inst.playerManager)
                            {
                                for (int k = 0; k < teams.Length; k++)
                                {
                                    if (playerPair.Value.TeamID == teams[k])
                                    {
                                        playerPair.Value.ConfettiActive(confettiOn);

                                        // Broadcast confetti change via ISFS.
                                        ISFSObject confettiObj = new SFSObject();
                                        confettiObj.PutBool("cnfon", confettiOn);
                                        confettiObj.PutInt("cnftm", teams[k]);
                                        CommunicationManager.SendObjectMsg(confettiObj);
                                    }
                                }
                            }

                            string teamsText = "";
                            if (teams.Length > 1)
                                teamsText = "teams";
                            else
                                teamsText = "team";

                            for (int j = 0; j < teams.Length; j++)
                                teamsText += " " + teams[j].ToString();

                            GameGUI.Inst.WriteToConsoleLog("Confetti is now " + (confettiOn ? "on" : "off") + " for " + teamsText);
                        }
                    }

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }

            else if (c.name == "update")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;

                    if (args.Length < 2)
                    {
                        error = "Please designate what to update: 'schedule', 'team', or 'room'.";
                    }
                    else if (args[1].Equals("schedule"))
                    {
                        BizSimManager.Inst.quarterScheduler.Reload();
                    }
                    else if (args[1].Equals("team"))
                    {
                        // Update team...
                        TeamInfo.Inst.Reload();
                        GameGUI.Inst.guiLayer.SendGuiLayerTeamInfo(null);
                        foreach(TeamNotesPanel tnp in TeamNotesPanel.GetAll())
                            tnp.Refresh();
                        foreach (TeamWebScreen tws in TeamWebScreen.GetAll())
                            tws.ReloadParseURL();
                    }
                    else if (args[1].Equals("room"))
                    {
                        // Update room
                        ParseRoomVarManager.Destroy();
                        bool refresh = args.Length <= 2 || args[2] != "norefresh";
                        if( refresh )
                            foreach (TeamWebScreen tws in TeamWebScreen.GetAll())
                                tws.ReloadParseURL();
                    }
                    else
                    {
                        error = "Please designate what to update.";
                    }

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }

			else if (c.name == "restCommand")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					var data = "";
					if(args.Length == 4 || args.Length == 3)
					{
						if (args.Length == 4)
							data = restHelper.sendRequest(args[2], args[1], args[3]);
						else
							data = restHelper.sendRequest(args[2], args[1]);
						GameGUI.Inst.WriteToConsoleLog(data);
					}
					else if (args.Length < 3)
					{
						error = "Need to specify a REST method and/or target." + c.helpText;
					}
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if (c.name == "user")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					var data = "";
					if(args.Length <= 4 || args.Length >= 2)
					{
						if (args[1] == "whoami" && args.Length == 2)
						{
							if (currentUserProfile.Initialized)
								data = currentUserProfile.RawJson;
							else
								data = "No user is currently logged in!";
						}
						else if (args[1] == "create" && args.Length == 4)
							data = currentUserProfile.CreateProfile(args[2], args[3]);
						else if (args[1] == "login" && args.Length == 4)
							data = currentUserProfile.Login(args[2], args[3]);
						else if (args[1] == "passreset" && args.Length == 3)
							data = currentUserProfile.PasswordReset(args[2]);
						else if (args[1] == "update" && args.Length == 4)
							data = currentUserProfile.UpdateProfile(args[2], args[3]);
                        else if (args[1] == "updatejson" && args.Length == 3)
                            data = currentUserProfile.UpdateProfile(args[2]);
						else if (args[1] == "delete" && args.Length == 2)
							data = currentUserProfile.DeleteProfile();
                        else if (args[1] == "randomavatar" && args.Length >= 2)
                        {
                            int characterIdx = 0;
                            if( args.Length >= 3 )
                                int.TryParse(args[2], out characterIdx);
                            data = currentUserProfile.UpdateProfile(AvatarOptionManager.Inst.CreateRandomAvatarJSON(characterIdx));

                        }
//						else if (args[1] == "show" && args.Length == 2)
//							data = GameGUI.Inst.userprofileGui.ShowProfile(currentUserProfile);
//						
//						else if (args[1] == "show" && args.Length == 3)
//						{
//							UserProfile other = new UserProfile();
//							data = GameGUI.Inst.userprofileGui.ShowProfile(other);
//						}
//						else if (args[1] == "hide" && args.Length == 2)
//							data = GameGUI.Inst.userprofileGui.HideProfile();
//						else if (args[1] == "refresh" && args.Length == 2)
//							GameGUI.Inst.userprofileGui.htmlPanel.browserTexture.RefreshWebView();
						else if (args[1] == "logout" && args.Length == 2)
						{
							currentUserProfile.Logout();
							data = "Logged out!";
						}
                        else if (args[1] == "backup" || args[1] == "csv")
                        {
                            UsersDAO userDao = new UsersDAO();
                            if (args[1] == "backup")
                                data = "All Users:\n" + userDao.GetAllUsers();
                            else
                            {
                                // Can specify 3rd argument to be the registration code.
                                string resultStr = "";
                                string delim = "\t";

                                List<string> includedCols = new List<string>() { "email", "updatedAt", "logins", "teamid", "registrationCode", "firstname", "lastname", "hwi_os" };
                                foreach (string col in includedCols)
                                    resultStr += col + delim;
                                resultStr += "\n";

                                string colRequirement = (args.Length >= 3 ? ("{\"registrationCode\":\"" + args[2] + "\"}") : "{}");
                                JSONArray rowArray = ParseDAOFactory.GetAllRowsFromJson(userDao.GetUserByColumnValue(colRequirement));
                                foreach (JSONValue value in rowArray)
                                {
                                    foreach( string col in includedCols )
                                    {
                                        if (value.Obj.ContainsKey(col))
                                            resultStr += value.Obj[col].ToString();
                                        resultStr += delim;
                                    }
                                    resultStr += "\n";
                                }
                                Debug.LogError(resultStr);
                            }
                        }
                        else
                            data = "Error: Command not recognized.";
						GameGUI.Inst.WriteToConsoleLog(data);
					}
					else if (args.Length < 2)
					{
						error = "Need to specify a REST method and/or target." + c.helpText;
					}
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
			else if (c.name == "parse")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					var data = "";
					if(args.Length <= 6 && args.Length >= 3)
					{
                        if (args[1] == "create" && args.Length == 4)
                        {
                            ParseObject obj = ParseObjectFactory.CreateParseObject(args[2], args[3]);
                            data = obj.RawJson;
                        }
                        else if (args[1] == "remove" && args.Length == 4)
                        {
                            ParseObject obj = ParseObjectFactory.FindParseObjectById(args[2], args[3]);
                            data = obj.Remove();
                        }
                        else if (args[1] == "update" && (args.Length == 5 || args.Length == 6))
                        {
                            ParseObject obj = ParseObjectFactory.FindParseObjectById(args[2], args[3]);
                            if (args.Length == 5)
                                data = obj.Update(args[4], "");
                            else
                                data = obj.Update(args[4], args[5]);
                        }
                        else if (args[1] == "findid" && args.Length == 4)
                        {
                            ParseObject obj = ParseObjectFactory.FindParseObjectById(args[2], args[3]);
                            if (obj != null)
                                data = obj.RawJson;
                            else
                                data = "Object not found!";
                        }
                        else if (args[1] == "findcv" && args.Length == 5)
                        {
                            ParseObject obj = ParseObjectFactory.FindByParseObjectByColumnValue(args[2], args[3], args[4]);
                            if (obj != null)
                                data = obj.RawJson;
                            else
                                data = "Object not found!";
                        }
                        else if (args[1] == "class")
                        {
                            if (args.Length == 3)
                                data = args[2] + ((ParseObjectFactory.DoesClassExist(args[2])) ? " exists" : " does not exist");
                            if (args.Length == 4 && (args[3] == "backup" || args[3] == "print"))
                            {
                                if (!ParseObjectFactory.DoesClassExist(args[2]))
                                    data = "Class " + args[2] + " does not exist";
                                else
                                {
                                    data = "Writing class: " + args[2] + " to output log";
                                    string resultStr = args[2] + ":\n";
                                    resultStr += ParseDAOFactory.GetAllObject(args[2]);
                                    Debug.LogError(resultStr);
                                }
                            }
                        }
                        else
                            data = "Error: Command not recognized.";
						GameGUI.Inst.WriteToConsoleLog(data);
					}
                    else if(args.Length == 2 && args[1] == "backup")
                    {
                        string resultStr = "";
                        UsersDAO userDao = new UsersDAO();
                        resultStr = "Users:\n" + userDao.GetAllUsers();
                        string[] classNames = { "NewApplications", "Profile", "Schedule", "ServerConfig", "Team" };
                        data = "Writing classes: User";
                        foreach (string cl in classNames)
                        {
                            data += ", " + cl;
                            resultStr += "\n" + cl + ":\n";
                            resultStr += ParseDAOFactory.GetAllObject(cl);
                        }
                        data += " to output log";
                        Debug.LogError(resultStr);
                        GameGUI.Inst.WriteToConsoleLog(data);
                    }
					else if (args.Length < 3)
					{
						error = "Need to specify a Parse class name and command and/or target." + c.helpText;
					}
					c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
				};
			}
            else if (c.name == "slidegen")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length > 1)
                    {
                        if( !SlideHTMLGenerator.Generate(args[1]) )
                            error = SlideHTMLGenerator.errorStr;
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };

            }
            else if (c.name == "doclistgen")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length > 1)
                    {
                        if( !GoogleDriveListGen.Generate(args[1]) )
                            error = GoogleDriveListGen.errorStr;
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
			else if (c.name == "loadlevel")
			{
				c.Eval = delegate(string[] args) {
					string error = null;
					if (args.Length > 1)
					{
                        bool privateRm = false;
                        GameManager.Level levelToLoad = GetLevel(args[1]);
                        if (args.Length > 2)
                        {
                            int teamID = -1;
                            if (int.TryParse(args[2], out teamID))
                                CommunicationManager.Inst.roomNumToLoad = teamID;
                            else if (levelToLoad == GameManager.Level.BIZSIM && args[2] == "new")
                                BizSimManager.forceNewGame = true;
                            else if (args[2] == "private")
                                privateRm = true;
                            else
                                error = "Team instance " + args[2] + " not an int. " + c.helpText; 
                        }
                        if (args.Length > 3 && levelToLoad == GameManager.Level.BIZSIM && args[3] == "new")
                            BizSimManager.forceNewGame = true;
                        if (levelToLoad == GameManager.Level.NONE)
                            error = "Improper level name." + c.helpText;
                        if( error == null )
                            GameManager.Inst.LoadLevel(levelToLoad, privateRm);
					}
					else
						error = "Argument required." + c.helpText;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "teleport")
            {
                c.Eval = delegate(String[] args)
                {
                    string error = null;
                    if (args.Length > 0)
                    {
                        int teleNum = 0;
                        if (int.TryParse(args[1], out teleNum))
                        {
                            if (teleNum < TeleportLocation.GetAll().Count)
                                GameManager.Inst.playerManager.SetLocalPlayerTransform(TeleportLocation.GetAll()[teleNum].transform);
                            else
                                GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
                        }
                        else
                        {
                            bool foundOne = false;
                            for (int j = 0; j < TeleportLocation.GetAll().Count; ++j)
                                if (TeleportLocation.GetAll()[j].spawnPtName.ToLower().IndexOf(args[1].ToLower()) != -1)
                                {
                                    GameManager.Inst.playerManager.SetLocalPlayerTransform(TeleportLocation.GetAll()[j].transform);
                                    foundOne = true;
                                }
                            if (!foundOne)
                                GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
                        }
                    }
                    else
                        GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "present")
            {
                c.Eval = delegate(String[] args)
                {
                    string error = null;
                    string url = WebStringHelpers.CreateValidURLOrSearch(args[1]);

                    SlidePresenter[] slidePresenter = (SlidePresenter[])FindObjectsOfType(typeof(SlidePresenter));
                    if (slidePresenter.Length == 0)
                        error = "No slide presenters in the scene";

                    if (error == null)
                    {
                        for (int idx = 0; idx < slidePresenter.Length; ++idx)
                        {
                            if (!slidePresenter[idx].browserTexture.WriteAccessAllowed)
                                error = "You don't have permission to change the presentation url";
                            else
                                slidePresenter[idx].SetNewURL(url, true);
                        }
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "loadassets")
            {
                c.Eval = delegate(String[] args) {
                    string error = null;
                    string url = WebStringHelpers.AppendHTTP(args[1]);

                    if (error == null)
                    {
                        // Load bundle!
                        AssetBundleManager.Inst.LoadAssetsCoroutine(url);
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "voice")
            {
                c.Eval = delegate(string[] args) {
                    string error = null;

                    if (args.Length > 1 && args[1].ToLower() == "minfreq")
                    {
                        float newMinFreq = 0.0f;
                        if (args.Length > 2)
                        {
                            if ((args[2] == "auto" || args[2] == "ave" || args[2] == "average"))
                            {
                                if (args.Length > 3 && (args[3] == "offset"))
                                {
                                    float newOffset = 0.0f;
                                    if (args.Length <= 4)
                                        error = "offset float value not specified. " + c.helpText;
                                    else if (float.TryParse(args[4], out newOffset))
                                    {
                                        VoiceChatRecorder.Instance.AutoDetectSpeechAveFFTOffset = newOffset;
                                        GameGUI.Inst.WriteToConsoleLog("Voice Chat now using average FFT offset of " + VoiceChatRecorder.Instance.AutoDetectSpeechAveFFTOffset);
                                        if (VoiceChatSettings.Instance.UseAverageFFTForVoiceDetect == false)
                                            GameGUI.Inst.WriteToConsoleLog("\tWarning: Average FFT not currently being used, last command has no effect with that off");
                                    }
                                    else
                                        error = "Not able to convert \"" + args[3] + "\" into a float. " + c.helpText;
                                }
                                else
                                {
                                    VoiceChatRecorder.Instance.AutoDetectSpeech = true;
                                    VoiceChatSettings.Instance.UseAverageFFTForVoiceDetect = true;
                                    GameGUI.Inst.WriteToConsoleLog("Voice Chat now using average FFT for voice detection threshold");
                                }
                            }
                            else if (float.TryParse(args[2], out newMinFreq))
                            {
                                VoiceChatRecorder.Instance.AutoDetectSpeech = true;
                                VoiceChatRecorder.Instance.AutoDetectMinFreq = newMinFreq;
                                VoiceChatSettings.Instance.UseAverageFFTForVoiceDetect = false;
                                GameGUI.Inst.WriteToConsoleLog("Voice Chat now using a min frequency of " + VoiceChatRecorder.Instance.AutoDetectMinFreq + " for voice detection threshold");
                            }
                            else
                                error = "argument \'" + args[1] + "\' couldn\'t be converted to a float.\n\t" + c.helpText;
                        }
                        else
                            error = "Must specify \"auto\" or give a float value for the min freq. " + c.helpText;
                    }
                    else if (args.Length > 1 && args[1].ToLower() == "feedbackdetect")
                    {
                        VoiceManager.Inst.HandleFeedbackDetection = !VoiceManager.Inst.HandleFeedbackDetection;
                        GameGUI.Inst.WriteToConsoleLog("Feedback detection: " + (VoiceManager.Inst.HandleFeedbackDetection ? "on" : "off"));
                    }
                    else
                        error = "Unsupported arguments. " + c.helpText;

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "stealth")
            {
                c.Eval = delegate(string[] args)
                {
                    if (GameManager.Inst.LocalPlayer.Type == PlayerType.STEALTH)
                    {
                        GameManager.Inst.LocalPlayer.Type = Player.prevType;
                        GameManager.Inst.LocalPlayer.Visible = true;
                    }
                    else
                    {
                        Player.prevType = GameManager.Inst.LocalPlayer.Type;
                        GameManager.Inst.LocalPlayer.Type = PlayerType.STEALTH;
                    }
                    GameManager.Inst.playerManager.playerTypeIsDirty = true;
                    CommunicationManager.CurrentUserProfile.UpdateProfile("permissionType", GameManager.Inst.LocalPlayer.GetPlayerTypeStr());
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "facilitator")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 1)
                        GameGUI.Inst.ToggleFacilitatorCameras();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "refresh")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 1)
                    {
                        if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
                            BizSimManager.Inst.ReloadAll();
                        else
                            error = "this command is only valid in the biz sim room";
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "replay")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    string filename = (args.Length > 1) ? args[1] : "";
                    int endCmdIdx = Array.IndexOf(args, "endcmd");
                    int playersIdx = Array.IndexOf(args, "players");
                    int startIdx = Array.IndexOf(args, "start");
                    int endIdx = Array.IndexOf(args, "end");
                    int clicksIdx = Array.IndexOf(args, "clicks");

                    int startMsg = 0;
                    if (startIdx != -1)
                        int.TryParse(args[startIdx + 1], out startMsg);
                    if (args.Length > 1 && args[1] == "jump") // jump expects an offset in number of messages from the original starting point.
                    {
                        int.TryParse(args[2], out startMsg);
                        if (!ReplayManager.Inst.SetCurrentMessage(startMsg - ReplayManager.Inst.MessageOffset))
                            error = "Failed to set message to " + startMsg + " it is out of range";
                    }
                    else if (args.Length > 1 && (args[1] == "percent" || args[1] == "pjump"))
                    {
                        float percent = 0.0f;
                        float.TryParse(args[2], out percent);
                        ReplayManager.Inst.SetPlaybackPercent(percent);
                    }
                    else if (args.Length > 1 && args[1] == "pause")
                        ReplayManager.Inst.Paused = true;
                    else if (args.Length > 1 && args[1] == "play")
                        ReplayManager.Inst.Paused = false;
                    else if (args.Length > 1 && args[1] == "clear")
                        ReplayManager.Inst.Destroy();
                    else if (args.Length > 1 && args[1].ToLower() == "messagenum")
                        ReplayGUI.showMessageNum = !ReplayGUI.showMessageNum;
                    else if (args.Length > 1 && args[1] == "nogui")
                        ReplayManager.Inst.hideNativeGui = !ReplayManager.Inst.hideNativeGui;
                    else if (args.Length > 1 && args[1] == "speed")
                    {
                        float speed = 1.0f;
                        if (args.Length > 2 && float.TryParse(args[2], out speed))
                        {
                            ReplayManager.Inst.Speed = speed;
                            GameGUI.Inst.WriteToConsoleLog("Setting replay speed to " + speed);
                        }
                        else
                            error = "Failed to set speed, could not parse " + args[2] + " into a valid speed";
                    }
                    else
                    {
                        bool saveCSV = (args[args.Length - 1].Trim() == "save");
                        bool clicks = true;
                        string endCmd = "";
                        if (endCmdIdx != -1)
                            endCmd = args[endCmdIdx + 1];
                        if (playersIdx != -1)
                        {
                            // restrict playback only to players in the comma separated list
                            string playersStr = args[playersIdx + 1];
                            string[] playerNames = playersStr.Split(',');
                            ReplayManager.Inst.allowedPlayerNames.AddRange(playerNames);
                        }
                        int numMsgsToLoad = -1; // -1 ==> all of them
                        int endMsg = startMsg;
                        if (endIdx != -1)
                        {
                            //int endMsg = startMsg;
                            if (int.TryParse(args[endIdx + 1], out endMsg))
                                numMsgsToLoad = endMsg - startMsg + 1;
                        }
                        if (clicksIdx != -1 && args.Length > clicksIdx)
                            clicks = !(args[clicksIdx + 1] == "off" || args[clicksIdx + 1] == "0" || args[clicksIdx + 1] == "false");
                        if (filename == "")
                        {
#if UNITY_STANDALONE
                            filename = NativePanels.OpenFileDialog(new string[]{"virbeo"});
#else
                            Debug.LogError("File dialogs are not supported in the editor, build a standalone for this functionality");
                            return;
#endif
                        }
                        if (filename != "")
                        {
                            if (filename.StartsWith("http") || filename.StartsWith("www."))
                                ReplayManager.Inst.ReadUrl(filename, startMsg, numMsgsToLoad, saveCSV, endCmd, clicks);
                            else
                                ReplayManager.Inst.ReadFile(filename, startMsg, numMsgsToLoad, saveCSV, endCmd, clicks);
                        }
                        else
                            error = "No valid replay file supplied";
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "cache")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    GameGUI.Inst.WriteToConsoleLog("Clearing web cache");
                    WebCore.ClearCache();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "resolution")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    int width = Screen.width;
                    int height = Screen.height;
                    if (!int.TryParse(args[1], out width))
                        error = "width: " + args[1] + " is not a valid int";
                    if (!int.TryParse(args[2], out height))
                        error = "height: " + args[2] + " is not a valid int";
                    if (error == null)
                        Screen.SetResolution(width, height, Screen.fullScreen);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "fullscreen")
            {
                c.Eval = delegate(string[] args)
                {
                    bool fullscreen = (args.Length < 2) ? !Screen.fullScreen : (args[1] == "true" || args[1] == "on" || args[1] == "1");
                    Screen.SetResolution(Screen.width, Screen.height, fullscreen);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "mic")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length > 1)
                        VoiceManager.Inst.DeviceName = args[1];
                    else
                        error = "mic: please specify the name of the device you want to set";
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "setmic")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;

                    VoiceManager.Inst.micSelectMenuOpen = !VoiceManager.Inst.micSelectMenuOpen;

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "sound")
            {
                c.Eval = delegate(string[] args)
                {
                    if( args.Length > 1 && args[1] == "mic")
                        Native.OpenMicSettings();
                    else
                        Native.OpenSpeakerSettings();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "info")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = (args.Length <= 1) ? "No message provided" : null;
                    if (error == null)
                        InfoMessageManager.Display(args[1]);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "consolewrite")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = (args.Length <= 1) ? "No message provided" : null;
                    if (error == null)
                        GameGUI.Inst.WriteToConsoleLog(args[1]);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if ((new List<string> { "wave", "dance", "confuse", "cry", "clap", "cheer", "impatient", "think", "powerpose", "laugh", "samba", "comehere", "raisehand", "bow", "shakehand", "backflip", "sit" }).Contains(c.name))
            {
                c.Eval = delegate(string[] args)
                {
                    string animName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(c.name);
                    GameManager.Inst.LocalPlayer.gameObject.GetComponent<AnimatorHelper>().StartAnim(animName, true);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "unsit")
            {
                c.Eval = delegate(string[] args)
                {
                    GameManager.Inst.LocalPlayer.Stand();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }                
            else if (c.name == "ron")
            {
                c.Eval = delegate(string[] args)
                {
                    bool male = true;
                    string name = "Ron";
                    int posIdx = Array.IndexOf(args, "pos");
                    int rotIdx = Array.IndexOf(args, "rot");


                    // default is at local player's current position.
                    Transform t = GameManager.Inst.LocalPlayer.gameObject.transform;
                    Vector3 pos = (posIdx != -1) ? StringConvert.ToVector3(args[posIdx + 1]) : t.position;
                    Quaternion rot = (rotIdx != -1) ? StringConvert.ToQuaternion(float.Parse(args[rotIdx + 1])) : t.rotation;

                    Player p = LocalBotManager.Inst.CreateRon(pos, rot, male, name);

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                    string botCmdStr = "/ron " + (male ? "m" : "f") + " " + p.Name + " pos " + StringConvert.ToString(pos) + " rot " + rot.eulerAngles.y;
                    Debug.Log(botCmdStr);
                    BotScript.AddCmd(botCmdStr);
                };
            }
            else if (c.name == "bot")
            {
                c.Eval = delegate(string[] args)
                {
                    bool male = true;
                    string name = "";
                    int posIdx = Array.IndexOf(args, "pos");
                    int rotIdx = Array.IndexOf(args, "rot");
                    int listIdx = Array.IndexOf(args, "list");

                    if (args.Length > 1)
                        male = !(args[1].ToLower().StartsWith("f") || args[1].ToLower().StartsWith("g") || args[1] == "1"); // female, girl

                    if (args.Length > 2 && args[2].ToLower() != "random")
                        name = args[2];

                    bool addToUserList = listIdx != -1 || GameManager.buildType == GameManager.BuildType.REPLAY;

                    // default is at local player's current position.
                    Transform t = GameManager.Inst.LocalPlayer.gameObject.transform;
                    Vector3 pos = (posIdx != -1) ? StringConvert.ToVector3(args[posIdx + 1]) : t.position;
                    Quaternion rot = (rotIdx != -1) ? StringConvert.ToQuaternion(float.Parse(args[rotIdx + 1])) : t.rotation;

                    Player p = LocalBotManager.Inst.Create(pos, rot, male, addToUserList, name);

                    string botCmdStr = "/bot " + (male ? "m" : "f") + " " + p.Name + " pos " + StringConvert.ToString(pos) + " rot " + rot.eulerAngles.y;

                    // Allow customization controls
                    for (int j = 0; j < args.Length; ++j)
                    {
                        if (AvatarOptionManager.Inst.Options[p.ModelIdx].ContainsKey(args[j]))
                        {
                            int index = 0;
                            if ((j + 1 < args.Length) && int.TryParse(args[j + 1], out index))
                            {
                                AvatarOptionManager.Inst.UpdateElement(p.gameObject, p.ModelIdx, args[j], index);
                                botCmdStr += " " + args[j] + " " + args[j + 1];
                            }
                        }
                    }

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                    
                    Debug.Log(botCmdStr);
                    BotScript.AddCmd(botCmdStr);
                };
            }
            else if (c.name == "botcmd")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;

                    int animIdx = Array.IndexOf(args, "anim");
                    int nameIdx = Array.IndexOf(args, "name");
                    int talkIdx = Array.IndexOf(args, "talk");
                    int typeIdx = Array.IndexOf(args, "type");
                    int walkIdx = Array.IndexOf(args, "walk");
                    int lookIdx = Array.IndexOf(args, "lookat");
                    int loopIdx = Array.IndexOf(args, "loop");
                    int posIdx = Array.IndexOf(args, "pos");
                    int rotIdx = Array.IndexOf(args, "rot");
                    int nameCIdx = Array.IndexOf(args, "namec");
                    int randomIdx = Array.IndexOf(args, "random");
                    int followIdx = Array.IndexOf(args, "follow");
                    int removeIdx = Array.IndexOf(args, "remove");
                    int scriptIdx = Array.IndexOf(args, "script");


                    string name = (nameIdx != -1 && (nameIdx + 1) < args.Length) ? args[nameIdx + 1] : "";
                    string anim = (animIdx != -1 && (animIdx + 1) < args.Length) ? args[animIdx + 1] : "";
                    string talk = (talkIdx != -1 && (talkIdx + 1) < args.Length) ? args[talkIdx + 1] : "";
                    string type = (typeIdx != -1 && (typeIdx + 1) < args.Length) ? args[typeIdx + 1] : "";
                    string loop = (loopIdx != -1 && (loopIdx + 1) < args.Length) ? args[loopIdx + 1] : "";
                    string look = (lookIdx != -1 && (lookIdx + 1) < args.Length) ? args[lookIdx + 1] : "";
                    string nameC = (nameCIdx != -1 && (nameCIdx + 1) < args.Length) ? args[nameCIdx + 1] : "";
                    string follow = (followIdx != -1 && (followIdx + 1) < args.Length) ? args[followIdx + 1] : "";
                    string posStr = (posIdx != -1 && (posIdx + 1) < args.Length) ? args[posIdx + 1] : "";
                    string rotStr = (rotIdx != -1 && (rotIdx + 1) < args.Length) ? args[rotIdx + 1] : "";

                    Vector3 pos = (posIdx != -1) ? StringConvert.ToVector3(posStr) : GameManager.Inst.LocalPlayer.gameObject.transform.position;
                    float rot = (rotStr != "") ? float.Parse(rotStr) : GameManager.Inst.LocalPlayer.gameObject.transform.rotation.eulerAngles.y;

                    string cmdStr = "";
                    for (int j = 0; j < args.Length; ++j)
                        cmdStr += args[j] + " ";

                    if (name == "")
                    {
                        // apply to all bots

                        if (removeIdx != -1)
                            LocalBotManager.Inst.DestroyAll();
                        if( anim != "" && anim != "unsit" )
                            LocalBotManager.Inst.AnimateAll(anim);
                        if (scriptIdx != -1)
                        {
                            GameGUI.Inst.WriteToConsoleLog(BotScript.ToString());
                            return;
                        }

                        foreach (KeyValuePair<string,Player> bot in  LocalBotManager.Inst.GetBots())
                        {
                            if (talkIdx != -1)
                                bot.Value.IsTalking = !(talk == "off");
                            if (typeIdx != -1)
                                bot.Value.IsTyping = !(type == "off");
                            if (walkIdx != -1)
                            {
                                if (posIdx != -1 && rotStr == "")
                                    bot.Value.BotGoto(pos);
                                else
                                    bot.Value.BotGoto(pos, rot);
                                cmdStr = "/botcmd" + " walk pos " + StringConvert.ToString(pos) + ((posIdx != -1 && rotStr == "") ? "" : (" rot " + rot.ToString()));
                            }
                            if (followIdx != -1)
                            {
                                follow = follow == "me" ? GameManager.Inst.LocalPlayer.Name : follow;
                                if (follow != "stop")
                                    bot.Value.BotFollow(GameManager.Inst.playerManager.GetPlayerOrBotByName(follow));
                                else
                                    bot.Value.BotStopFollow();
                            }
                            if (lookIdx != -1)
                                HandleLookAtOptions(bot.Value, look);
                            if (randomIdx != -1)
                                AvatarOptionManager.Inst.CreateRandomAvatar(bot.Value, false);
                            if( anim == "unsit" )
                                LocalBotManager.Inst.StopAnimation(bot.Value.Name, "Sit");
                        }
                    }
                    else
                    {
                        if (removeIdx != -1)
                        {
                            LocalBotManager.Inst.DestroyBotByName(name);
                            BotScript.AddCmd(cmdStr);
                            return;
                        }

                        Player p = LocalBotManager.Inst.GetBotByName(name);
                        if (anim != "")
                        {
                            if (anim == "unsit")
                                LocalBotManager.Inst.StopAnimation(name, "Sit");
                            else
                                LocalBotManager.Inst.StartAnimation(name, anim);
                        }
                        if (talkIdx != -1)
                            p.IsTalking = !(talk == "off");
                        if (typeIdx != -1)
                            p.IsTyping = !(type == "off");
                        if (loopIdx != -1)
                            p.gameObject.GetComponent<AnimatorHelper>().EnableLooping(loop == "on");
                        if (walkIdx != -1)
                        {
                            if (posIdx != -1 && rotStr == "")
                                p.BotGoto(pos);
                            else
                                p.BotGoto(pos, rot);
                            cmdStr = "/botcmd" + " name " + name + " walk pos" + StringConvert.ToString(pos) + ((posIdx != -1 && rotStr == "") ? "" : (" rot " + rot.ToString()));
                        }
                        if (nameCIdx != -1)
                        {
                            int nameCID = -1;
                            if (int.TryParse(nameC, out nameCID))
                                p.SetNameTextColor(nameCID);
                        }
                        if (followIdx != -1)
                        {
                            follow = follow == "me" ? GameManager.Inst.LocalPlayer.Name : follow;
                            if (follow != "stop")
                                p.BotFollow(GameManager.Inst.playerManager.GetPlayerOrBotByName(follow));
                            else
                                p.BotStopFollow();
                        }
                        if (lookIdx != -1)
                            HandleLookAtOptions(p, look);
                        if (randomIdx != -1)
                            AvatarOptionManager.Inst.CreateRandomAvatar(p, false);

                        // change avatar appearance, expects option names from the parse database.
                        for (int j = 0; j < args.Length; ++j)
                        {
                            if (AvatarOptionManager.Inst.Options[p.ModelIdx].ContainsKey(args[j]))
                            {
                                int index = 0;
                                if ((j + 1 < args.Length) && int.TryParse(args[j + 1], out index))
                                    AvatarOptionManager.Inst.UpdateElement(p.gameObject, p.ModelIdx, args[j], index);
                            }
                        }
                    }

                    if (nameIdx == -1 && animIdx == -1 && talkIdx == -1 && typeIdx == -1 && walkIdx == -1 && removeIdx == -1 && followIdx == -1)
                        error = c.helpText;

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);

                    Debug.Log(cmdStr);
                    BotScript.AddCmd(cmdStr);
                };
            }         
            else if (c.name == "quit")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 1)
                        Application.Quit();
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "download")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    gameObject.AddComponent<DownloadHelper>().StartDownload(args[1], DownloadCallback, true);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "upload")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    string filename = (args.Length > 1) ? args[1] : "";

                    if (filename == "")
                    {
#if UNITY_STANDALONE
                        filename = NativePanels.OpenFileDialog(null);
#else
                        Debug.LogError("File dialogs are not supported in the editor, build a standalone for this functionality");
                        return;
#endif
                    }
                    gameObject.AddComponent<UploadHelper>().UploadFile(filename, CommunicationManager.uploadUrl);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "uploadvirbeo")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    string filename = (args.Length > 1) ? args[1] : "";

                    if (filename == "")
                    {
#if UNITY_STANDALONE
                        filename = NativePanels.OpenFileDialog(new string[]{"virbeo"});
#else
                        Debug.LogError("File dialogs are not supported in the editor, build a standalone for this functionality");
                        return;
#endif
                    }
                    gameObject.AddComponent<UploadHelper>().UploadFile(filename, CommunicationManager.uploadUrl);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "silencecone")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    string roomVariableName = "cos";
                    bool rmVarVal = true;
                    if (args.Length > 1)
                        rmVarVal = !(args[1] == "false" || args[1] == "0" || args[1] == "no" || args[1] == "off");
                    else
                    {
                        Room rm = CommunicationManager.LastValidRoom();
                        if( rm != null && rm.ContainsVariable(roomVariableName) )
                            rmVarVal = !rm.GetVariable(roomVariableName).GetBoolValue();
                    }
                    List<RoomVariable> roomVariables = new List<RoomVariable>();
                    roomVariables.Add(new SFSRoomVariable(roomVariableName, rmVarVal));
                    CommunicationManager.SendMsg(new SetRoomVariablesRequest(roomVariables, CommunicationManager.LastValidRoom()));

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "winmove")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length < 3)
                        error = "Incorrect args - must specify x and y";
                    else{
                        int x = 0, y = 0;
                        int.TryParse(args[1], out x);
                        int.TryParse(args[2], out y);
                        WindowMover.Move(x, y);
                    }

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "goto")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 2){
                        Player playerToGoto = GameManager.Inst.playerManager.GetPlayerOrBotByName(args[1]);

                        if(playerToGoto)
                            GameManager.Inst.LocalPlayer.playerController.GoToPlayer(playerToGoto);
                        else
                            error = "Player not found...";
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "follow")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 2){
                        Player playerToGoto = GameManager.Inst.playerManager.GetPlayerOrBotByName(args[1]);

                        if(playerToGoto)
                            GameManager.Inst.LocalPlayer.playerController.FollowPlayer(playerToGoto);
                        else
                            error = "Player not found...";
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "summonaudience")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    if (args.Length == 1){
                        OrientationBotAudience.Inst.SpawnAudience();
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "clicktomove")
            {
                c.Eval = delegate(string[] args)
                {
                    if (args.Length > 1)
                        GameManager.Inst.LocalPlayer.playerController.clickToMove = !(args[1] == "false" || args[1] == "0" || args[1] == "no");
                    else
                        GameManager.Inst.LocalPlayer.playerController.clickToMove = !GameManager.Inst.LocalPlayer.playerController.clickToMove;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "headfollowmouse")
            {
                c.Eval = delegate(string[] args)
                {
                    if (args.Length > 1)
                        PlayerManager.Inst.HeadTiltEnabled = !(args[1] == "false" || args[1] == "0" || args[1] == "no");
                    else
                        PlayerManager.Inst.HeadTiltEnabled = !PlayerManager.Inst.HeadTiltEnabled;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "camfollowmouse")
            {
                c.Eval = delegate(string[] args)
                {
                    if (args.Length > 1)
                        MainCameraController.Inst.gazeTiltEnabled = !(args[1] == "false" || args[1] == "0" || args[1] == "no");
                    else
                        MainCameraController.Inst.gazeTiltEnabled = !MainCameraController.Inst.gazeTiltEnabled;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "lookatspeaker")
            {
                c.Eval = delegate(string[] args)
                {
                    if (args.Length > 1)
                        PlayerController.focusOnSpeaker = !(args[1] == "false" || args[1] == "0" || args[1] == "no");
                    else
                        PlayerController.focusOnSpeaker = !PlayerController.focusOnSpeaker;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "lookatspeed")
            {
                c.Eval = delegate(string[] args)
                {
                    if (args.Length > 1)
                        PlayerController.lookAtSpeed = float.Parse(args[1]);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "minimap")
            {
                c.Eval = delegate(string[] args)
                {
                    GameObject go = GameObject.Find("minimapcam");
                    if (args.Length > 1)
                        go.GetComponent<Camera>().enabled = !(args[1] == "false" || args[1] == "0" || args[1] == "no");
                    else
                        go.GetComponent<Camera>().enabled = !go.GetComponent<Camera>().enabled;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "lights")
            {
                c.Eval = delegate(string[] args)
                {
                    bool up = (args.Length > 1) && (args[1] == "up");
                    float time = 1.0f;
                    if (args.Length > 2)
                        float.TryParse(args[2], out time);
                    for (int j = 0; j < LightDimmer.allDimmers.Count; ++j)
                    {
                        if (up)
                            LightDimmer.allDimmers[j].LightsUp(time);
                        else
                            LightDimmer.allDimmers[j].LightsDown(time);
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "lookat")
            {
                c.Eval = delegate(string[] args)
                {
                    HandleLookAtOptions(GameManager.Inst.LocalPlayer, args[1]);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if( c.name == "bizsim")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    int simIdIdx = Array.IndexOf(args, "simid");
                    string simId = (simIdIdx != -1 && (simIdIdx + 1) < args.Length) ? args[simIdIdx + 1] : "";
                    if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
                        BizSimManager.Inst.InitSim(simId);
                    else
                        error = "Bizsim cmd only valid in bizsim rooms";
                    error = (simIdIdx == -1) ? " simid not specified " : error;
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "script")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    string filename = args.Length > 1 ? args[1] : "";
                    string[] cmdLines = System.IO.File.ReadAllLines(filename);

                    foreach (string line in cmdLines)
                        ProcCommand(line);
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "record")
            {
                c.Eval = delegate(string[] args)
                {
                    string dirName = "replays";
                    string filename = dirName + "/" + GameManager.Inst.LevelLoadedInfo.GetShortName() + "_" + DateTime.Now.ToString("MM-dd-yy-h-mm-ss") + ".virbeo";
                    if (args.Length > 1 && (args[1] == "stop" || args[1] == "save" || args[1] == "delete"))
                    {
                        string currentFileName = RecordingPlayer.Inst.RecordingFilename;
                        RecordingPlayer.Destroy();

                        if (args[1] == "save")
                        {
#if UNITY_STANDALONE_WIN
                            string chosenFileName = NativePanels.SaveFileDialog(filename, "virbeo");
                            System.IO.File.Move(currentFileName, chosenFileName);
                            
#elif UNITY_STANDALONE_OSX
                            AnnouncementManager.Inst.Announce("Replay Saved", "Your replay file has been saved to:<br>" + currentFileName);
#else
                            Debug.LogError("Save dialog not supported in the editor.");
#endif
                        }
                        else if (args[1] == "delete")
                            System.IO.File.Delete(currentFileName);
                    }
                    else
                    {
                        System.IO.Directory.CreateDirectory(dirName);
                        RecordingPlayer.Inst.Init(filename, false);
                        VDebug.Log("recording to " + filename);
                    }
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), null);
                };
            }
            else if (c.name == "focusedwebpanel")
            {
                c.Eval = delegate(string[] args)
                {
                    // expected format: /webpanel id action input
                    string error = null;
                    int id = FocusManager.Inst.GetFocusedID();
                    if (id == -1)
                        error = "not focused on a panel";

                    string action = args[1];
                    string input = args.Length > 2 ? args[2] : "";
                    if( error == null )
                        error = HandleWebPanelAction(id, action, input);

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "webpanel")
            {
                c.Eval = delegate(string[] args)
                {
                    // expected format: /webpanel id action input
                    string error = null;
                    int id = -1;
                    if( !int.TryParse(args[1], out id ) )
                        error = "webpanel id " + id + " not an integer";

                    string action = args[2];
                    string input = args.Length > 3 ? args[3] : "";
                    if (error == null)
                        error = HandleWebPanelAction(id, action, input);

                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
            else if (c.name == "privatebrowser")
            {
                c.Eval = delegate(string[] args)
                {
                    string error = null;
                    float screenPercent = 0.75f;

                    // Find arguments
                    int xIdx = Array.IndexOf(args, "x");
                    int yIdx = Array.IndexOf(args, "y");
                    int wIdx = Math.Max(Array.IndexOf(args, "w"), Array.IndexOf(args, "width"));
                    int hIdx = Math.Max(Array.IndexOf(args, "h"), Array.IndexOf(args, "height"));
                    int pIdx = Math.Max(Array.IndexOf(args, "p"), Array.IndexOf(args, "percent"));

                    int moveXIdx = Array.IndexOf(args, "mx");
                    int moveYIdx = Array.IndexOf(args, "my");
                    int closeIdx = Array.IndexOf(args, "close");
                    int clickoffIdx = Array.IndexOf(args, "clickoffclose");

                   
 
                    // Modify Browser behavior
                    if (closeIdx != -1)
                    {
                        string closeStr = GetNextArg(args, closeIdx);
                        PrivateBrowser.Inst.Hide();
                        return;
                    }
                    if (clickoffIdx != -1)
                    {
                        string clickOffStr = GetNextArg(args, clickoffIdx);
                        bool clickOffToClose = false;
                        if( bool.TryParse(clickOffStr, out clickOffToClose ) )
                            PrivateBrowser.Inst.closeOnClickOff = clickOffToClose;
                        return;
                    }
                    if (moveXIdx != -1 || moveYIdx != -1)
                    {
                        int relativeIdx = Array.IndexOf(args, "relative");
                        string mxStr = GetNextArg(args, moveXIdx);
                        string myStr = GetNextArg(args, moveYIdx);
                        int mx = 0;
                        int my = 0;
                        if (int.TryParse(mxStr, out mx) && int.TryParse(myStr, out my))
                        {
                            if (relativeIdx == -1)
                                PrivateBrowser.Inst.SetPosition(mx, my);
                            else
                                PrivateBrowser.Inst.SetRelativePosition(mx, my);
                        }
                        return;
                    }


                    // Setup initial browser display
                    string xStr = GetNextArg(args, xIdx);
                    string yStr = GetNextArg(args, yIdx);
                    string wStr = GetNextArg(args, wIdx);
                    string hStr = GetNextArg(args, hIdx); 
                    string pStr = GetNextArg(args, pIdx);
                    if (pIdx != -1)
                        float.TryParse(pStr, out screenPercent);
                    int width = (int)(screenPercent * Screen.width);
                    int height =  (int)(screenPercent * Screen.height);
                    if (wIdx != -1)
                        int.TryParse(wStr, out width);
                    if (hIdx != -1)
                        int.TryParse(hStr, out height);

                    int x = (Screen.width - width) / 2;
                    int y = Screen.height - (Screen.height - height) / 2;
                    if (xIdx != -1)
                        int.TryParse(xStr, out x);
                    if (yIdx != -1)
                        int.TryParse(yStr, out y);

                    if (args.Length <= 1)
                        error = "must specify a url";
                    else
                        PrivateBrowser.Inst.SetURL(WebStringHelpers.CreateValidURLOrSearch(args[1]), width, height, x, y);
                    
                    c.OnEval(new ConsoleCommandEventArgs(ConsoleCommandEventType.ONEVAL), error);
                };
            }
		}
	}

    void DownloadCallback(WWW downloadObj)
    {
        VDebug.LogError("Writing download to file");
        string filename = downloadObj.url.Substring(downloadObj.url.TrimEnd('/').LastIndexOf('/'));
        try
        {
#if !UNITY_WEBPLAYER
            System.IO.Directory.CreateDirectory("downloads");
            File.WriteAllBytes("./downloads/" + filename, downloadObj.bytes);
#else
            Debug.LogError("Saving files to disk is not supported in webplayer");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError("Download write failed for " + downloadObj.url + " error: " +  e.ToString());
            AnnouncementManager.Inst.Announce("Warning", "Error downloading file " + WebStringHelpers.HtmlEncode(downloadObj.url));
        }
    }

    string GetNextArg(string[] args, int idx)
    {
        return (idx != -1 && (idx + 1) < args.Length) ? args[idx + 1] : "";
    }

    string HandleWebPanelAction(int id, string action, string actionInput = "")
    {
        string error = null;
        CollabBrowserTexture browserTexture = null;

        if (!CollabBrowserTexture.GetAll().TryGetValue(id, out browserTexture))
            return " could not find panel " + id;

        switch (action)
        {
            case "zoom":
                browserTexture.ToggleSnapCameraToObject();
                break;
            case "url":
                RoomVariableUrlController controller = browserTexture.gameObject.GetComponent<RoomVariableUrlController>();
                if (controller != null)
                    controller.SetNewURL(WebStringHelpers.CreateValidURLOrSearch(actionInput), true);
                else
                    browserTexture.GoToURL(WebStringHelpers.CreateValidURLOrSearch(actionInput));
                break;
            case "goback":
                browserTexture.GoBack();
                break;
            case "goforward":
                browserTexture.GoForward();
                break;
            case "refresh":
                browserTexture.RefreshWebView();
                break;
            case "external":
                Application.OpenURL(browserTexture.URL);
                break;
            case "focus":
                browserTexture.Focus();
                break;
            case "unfocus":
                browserTexture.Unfocus();
                break;
            case "js":
                if (browserTexture.isWebViewBusy())
                    browserTexture.jsOnLoadComplete += actionInput;
                else
                    browserTexture.ExecuteJavaScript(actionInput);
                break;
            case "jsOnComplete":
                browserTexture.jsOnLoadComplete += actionInput;
                break;
            case "normalPermission":
                browserTexture.minWriteAccessType = PlayerType.NORMAL; // this is just local to show white glow when guilayer allows permission.
                break;
            case "allowurlchange":
                if (actionInput == "")
                    browserTexture.AllowURLChanges = !browserTexture.AllowURLChanges;
                else
                    browserTexture.AllowURLChanges = (actionInput == "on" || actionInput == "true");
                break;
            case "allowinputchange":
                if (actionInput == "")
                    browserTexture.AllowInputChanges = !browserTexture.AllowInputChanges;
                else
                    browserTexture.AllowInputChanges = (actionInput == "on" || actionInput == "true");
                break;
            default:
                error = " unknown action: " + action;
                break;
        }
        return error;
    }

    GameObject GetPlayerOrGameObjectInScene(string name, ref bool isPlayer)
    {
        Player player = GameManager.Inst.playerManager.GetPlayerOrBotByName(name);
        isPlayer = (player != null);
        GameObject go =  isPlayer ? player.gameObject : null;
        if (go == null)
            go = GameObject.Find(name);
        return go;
    }

    /*
    void HandleLookAtOptions(Player p, string lookAtOption)
    {
        if (lookAtOption == "off")
        {
            CharacterLookAt lookAt = p.gameObject.GetComponent<CharacterLookAt>();
            lookAt.enabled = false;
        }
        else if (lookAtOption == "me" || lookAtOption == "on")
            p.gameObject.AddComponent<CharacterLookAtLocalPlayer>();
        else
        {
            GameObject lookAtObj = null;
            if (lookAtOption == "mouse")
            {
                PlayerMouseVisual pmv = RemoteMouseManager.Inst.GetVisual(p.Id);
                lookAtObj = (pmv != null) ? pmv.mouseVisual : null;
            }
            if (lookAtOption == "camera")                
                lookAtObj = (Camera.main != null) ? Camera.main.gameObject : null;

            bool isPlayer = false;
            if( lookAtObj == null )
                lookAtObj = GetPlayerOrGameObjectInScene(lookAtOption, ref isPlayer);
            if (lookAtObj != null)
            {
                CharacterLookAt lookAt = p.gameObject.GetComponent<CharacterLookAt>();
                if( lookAt == null )
                    lookAt = p.gameObject.AddComponent<CharacterLookAt>();
                lookAt.enabled = true;
                lookAt.lookAtGameObject = lookAtObj;
                if (isPlayer)
                    lookAt.UseCharacterOffset();
                else
                    lookAt.offset = Vector3.zero;
            }
        }
    }
     * */

    void HandleLookAtOptions(Player p, string lookAtOption)
    {
        if (lookAtOption == "off")
        {
            p.playerController.ClearLookAtPosition();
        }
        else if (lookAtOption == "me" || lookAtOption == "on"){
            p.playerController.LookAtPlayer(GameManager.Inst.LocalPlayer);
            //GameGUI.Inst.WriteToConsoleLog("Lookin'!");
        }
        else
        {
            GameObject lookAtObj = null;
            if (lookAtOption == "mouse")
            {
                int mouseID = p.Id;
                // if additional argument, look at first visible mouse you find
                if (!string.IsNullOrEmpty(lookAtOption))
                {
                    foreach(KeyValuePair<int, PlayerMouseVisual> kvp in RemoteMouseManager.playerMouseVisuals)
                    {
                        if( kvp.Value.Visible )
                        {
                            mouseID = kvp.Key;
                            break;
                        }
                    }
                }
                PlayerMouseVisual pmv = RemoteMouseManager.Inst.GetVisual(mouseID);
                lookAtObj = (pmv != null) ? pmv.mouseVisual : null;
            }
            if (lookAtOption == "camera")                
                lookAtObj = (Camera.main != null) ? Camera.main.gameObject : null;

            bool isPlayer = false;
            if( lookAtObj == null )
                lookAtObj = GetPlayerOrGameObjectInScene(lookAtOption, ref isPlayer);

            if(isPlayer)
                p.playerController.LookAtPlayer(lookAtObj.GetComponent<PlayerController>().playerScript);
            else
                p.playerController.LookAtTransform(lookAtObj.transform);
        }
    }


	void Update()
	{
		if (consoleInputQueue.Count > 0)
		{
			Preprocess(consoleInputQueue.Dequeue());
		}
	}

    private bool HasValidPermissions(ConsoleCommand cmd, PlayerType permission)
    {
        return ((int)permission >= (int)cmd.minRequiredPermissions);
    }

    public void Preprocess(ConsoleInput input)
	{
        if (input == null || input.str == null || input.str.Length == 0)
			return;
		if (input.str[0] != '/')
		{
			ChatManager.Inst.sendPublicMsg(input.str);
		}
		else 
		{
            // split lines for multiple commands
            string[] cmds = input.str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < cmds.Length; ++i)
            {
                string[] args = cmds[i].Split(new char[] { ' ' }, 2);
                if (args != null && args.Length > 0)
                {
                    string commandName = args[0];
                    if (!commands.ContainsKey(commandName))
                        commandName = "/?";
                    ConsoleCommand cmd = commands[commandName];
                    if (!HasValidPermissions(cmd, input.permission))
                    {
                        GameGUI.Inst.WriteToConsoleLog("You don\'t have the permission to use the " + cmd.name + " command.");
                        return;
                    }
                    if (cmd != null && cmd.Eval != null)
                        cmd.ValidateCommandArgs(cmds[i]);
                    else
                        Debug.LogError("Eval not defined for command: " + commandName);
                }
            }
		}
	}

    public void ExecuteDelayedCommand(string input, float delaySeconds)
    {
        StartCoroutine(ExecuteDelayedCommandImpl(input, delaySeconds));
    }

    IEnumerator ExecuteDelayedCommandImpl(string input, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        ProcCommand(input);
    }

	public void ProcCommand(string input)
	{
        consoleInputQueue.Enqueue( new ConsoleInput(input, GameManager.Inst.LocalPlayerType) );
	}

    // this should only be called from the administrator message, so local players cannot execute commands beyond their rights.
    public void ProcCommand(string input, PlayerType permission)
    {
        consoleInputQueue.Enqueue(new ConsoleInput(input, permission));
    }

    static public GameManager.Level GetLevel(string str)
    {
        switch (str.ToLower())
        {
            case "bizsim":
            case "bussim":
            case "bizsimdemo":
            case "2":
                return GameManager.Level.BIZSIM;
            case "campus":
            case "virbelacampus":
            case "1":
                return GameManager.Level.CAMPUS;
            case "minicampus":
            case "ucicampus":
            case "8":
                return GameManager.Level.MINICAMPUS;
            case "invpath":
            case "invisiblepath":
            case "3":
                return GameManager.Level.INVPATH;
            case "orient":
            case "orientation":
            case "lecture":
            case "lecturehall":
            case "presentation":
            case "present":
            case "4":
                return GameManager.Level.ORIENT;
            case "avatar":
            case "avatarselect":
            case "avatarcustom":
            case "avatarselection":
            case "avatarcustomization":
            case "5":
                return GameManager.Level.AVATARSELECT;
            case "nav":
            case "navtutorial":
            case "9":
                return GameManager.Level.NAVTUTORIAL;
            case "team":
            case "teamrm":
            case "teamroom":
            case "breakout":
            case "6":
                return GameManager.Level.TEAMROOM;
            case "cmdrm":
            case "commandrm":
            case "commandroom":
            case "cmdroom":
            case "7":
                return GameManager.Level.CMDROOM;
            case "court":
            case "courtroom":
            case "courtrm":
            case "10":
                return GameManager.Level.COURTROOM;
            case "hospital":
            case "hospitalroom":
            case "hospitalrm":
            case "hrm":
            case "hroom":
            case "11":
                return GameManager.Level.HOSPITALROOM;
            case "boardrm":
            case "boardroom":
            case "brm":
            case "broom":
            case "lgboardrm":
            case "largeboardrm":
            case "largeboardroom":
            case "12":
                return GameManager.Level.BOARDROOM;
            case "medboardrm":
            case "boardrmmed":
            case "mboardroom":
            case "mbrm":
            case "mbroom":
            case "medboardroom":
            case "mediumboardroom":
            case "13":
                return GameManager.Level.BOARDROOM_MED;
            case "smboardrm":
            case "boardrmsm":
            case "smboardroom":
            case "sbrm":
            case "sbroom":
            case "smallboardroom":
            case "14":
                return GameManager.Level.BOARDROOM_SM;
            case "ocampus":
            case "opencampus":
            case "dtcampus":
            case "teamcampus":
            case "distributedteamscampus":
            case "17":
                return GameManager.Level.OPENCAMPUS;
            case "mtest":
            case "mdonstest":
            case "motiontest":
            case "motion":
            case "tunnel":
            case "tunnelgame":
            case "18":
                return GameManager.Level.MOTION_TEST;
            case "scale":
            case "mdonsscale":
            case "scalegame":
            case "sgame":
            case "19":
                return GameManager.Level.SCALE_GAME;
            case "office":
            case "off":
            case "16":
                return GameManager.Level.OFFICE;
            case "connect":
			case "connection":
			case "login":
            case "title":
            case "0":
                return GameManager.Level.CONNECT;
            default:
                Debug.LogError("Level: " + str + " unknown");
                break;
        }
        return GameManager.Level.NONE;
    }
	
}
