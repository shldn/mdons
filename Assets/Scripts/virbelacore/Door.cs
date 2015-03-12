using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Door : MonoBehaviour {

    public enum TeleportType { TRIGGER = 1, CLICK = 2, BOTH = 3 }

    private static List<Door> doors = new List<Door>();
    public static List<Door> GetAll() { return doors; }
    public static bool GetDoorForReEntry(GameManager.Level destination, int roomInstanceID, out Door door)
    {
        for (int i = 0; i < doors.Count; ++i)
            if (doors[i].destination == destination && (doors[i].roomInstanceID == roomInstanceID || (doors[i].roomInstanceID == -1 && doors[i].useForGeneralReEntry)))
            {
                door = doors[i];
                return true;
            }
        door = null;
        return false;
    }

    public GameManager.Level destination = GameManager.Level.NONE;
    public int roomInstanceID = -1;
    public TeleportType teleportType = TeleportType.CLICK;
    public PlayerType minAccessRights = PlayerType.NORMAL;
    public float maxDistToClick = 20;
    public bool useForGeneralReEntry = false;
    public bool allowBackupTeleport = false;

    void Awake() {
        doors.Add(this);
    }

	void Start () {

        Teleporter teleporter = null;
        if (teleportType == TeleportType.CLICK || teleportType == TeleportType.BOTH)
        {
            ClickToTeleport ctTele = gameObject.AddComponent<ClickToTeleport>();
            ctTele.maxDistance = maxDistToClick;
            teleporter = ctTele;
            SetTeleporterProperties(teleporter);
        }

        if (teleportType == TeleportType.TRIGGER || teleportType == TeleportType.BOTH)
        {
            TeleportTrigger teleTrigger = gameObject.AddComponent<TeleportTrigger>();
            teleTrigger.allowBackupTeleport = allowBackupTeleport;
            teleporter = teleTrigger;
            SetTeleporterProperties(teleporter);
        }


        // add helper message
        MessageOnHover msgObj = gameObject.AddComponent<MessageOnHover>();
        string actionStr = (teleportType == TeleportType.CLICK) ? "Click to " : ((teleportType == TeleportType.BOTH) ? "Click or walk through to " : "Walk through to ");
        string enterExitStr = (GameManager.Inst.LevelLoaded == GameManager.Level.CAMPUS || GameManager.Inst.LevelLoaded == GameManager.Level.MINICAMPUS) ? "enter" : "exit";
        msgObj.message = actionStr + enterExitStr + " Room";
        msgObj.maxDistance = maxDistToClick;
	}

    void SetTeleporterProperties(Teleporter teleporter)
    {
        teleporter.levelDestination = destination;
        teleporter.teamIdOverride = roomInstanceID;
        teleporter.minAccessRights = minAccessRights;

        // Switch campus based on the last level they came from
        if (destination == GameManager.Level.CAMPUS && GameManager.Inst.lastCampus == GameManager.Level.MINICAMPUS)
            teleporter.levelDestination = GameManager.Level.MINICAMPUS;

    }

    void OnDestroy()
    {
        doors.Remove(this);
    }

}
