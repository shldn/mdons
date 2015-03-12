using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Entities;

public class LevelInfo
{
    public LevelInfo(GameManager.Level level_, string sceneName_, string sfsRoom_, bool teamInstanceRoom_)
    {
        level = level_;
        sceneName = sceneName_;
        sfsRoom = sfsRoom_;
        teamInstanceRoom = teamInstanceRoom_;
    }

    public string GetShortName()
    {
        return GameManager.LevelToShortString(level);
    }

    public GameManager.Level level; // level -- cooresponds to the index in the build order
    public string sceneName;        // Unity scene file name
    public string sfsRoom;          // if "", no smartfox room will be used.
    public bool teamInstanceRoom;   // use the team id to create separate instances per team.


    private static Dictionary<string, LevelInfo> levelInfoFromSceneName = null;
    private static Dictionary<GameManager.Level, LevelInfo> levelInfoFromLevel = null;

    private static void BuildLevelInfo()
    {

        LevelInfo[] levelInfo = null;
        if (GameManager.buildType == GameManager.BuildType.DEMO)
        {
            levelInfo = new LevelInfo[]{
                new LevelInfo(GameManager.Level.CONNECT, "Connection", "", false),
                new LevelInfo(GameManager.Level.CAMPUS, "VirBELACampus", "Game Room", false),
                new LevelInfo(GameManager.Level.BIZSIM, "BizSimDemo", "", true),
                new LevelInfo(GameManager.Level.INVPATH, "InvisiblePath", "", true),
                new LevelInfo(GameManager.Level.ORIENT, "Orientation", "", false),
                new LevelInfo(GameManager.Level.AVATARSELECT, "AvatarSelection", "", false),
                new LevelInfo(GameManager.Level.TEAMROOM, "TeamRoom", "TeamRm", true),
                new LevelInfo(GameManager.Level.CMDROOM, "CommandRoom", "", true),
				new LevelInfo(GameManager.Level.MINICAMPUS, "ucicampus", "Mini Room", false),
                new LevelInfo(GameManager.Level.NAVTUTORIAL, "NavTutorial", "", false),
                new LevelInfo(GameManager.Level.COURTROOM, "courtroom", "CourtRm", true),
                new LevelInfo(GameManager.Level.HOSPITALROOM, "hospitalroom", "HospRm", true),
                new LevelInfo(GameManager.Level.BOARDROOM, "boardrmlg", "BoardRmL", true),
                new LevelInfo(GameManager.Level.BOARDROOM_MED, "boardrmmed", "BoardRmM", true),
                new LevelInfo(GameManager.Level.BOARDROOM_SM, "boardrmsm", "BoardRmS", true),
                new LevelInfo(GameManager.Level.OPENCAMPUS, "opencampus", "OCampus", false),
                new LevelInfo(GameManager.Level.MOTION_TEST, "mdons_motion", "", true),
                new LevelInfo(GameManager.Level.SCALE_GAME, "mdons_scale", "ScaleRm", true),
                new LevelInfo(GameManager.Level.OFFICE, "office", "Office", true)
            };
        }
        else
        {
            levelInfo = new LevelInfo[]{
                new LevelInfo(GameManager.Level.CONNECT, "Connection", "", false),
                new LevelInfo(GameManager.Level.CAMPUS, "VirBELACampus", "Game Room", false),
                new LevelInfo(GameManager.Level.BIZSIM, "BizSimDemo", "BizSimRm", true),
                new LevelInfo(GameManager.Level.INVPATH, "InvisiblePath", "InvPathRm", true),
                new LevelInfo(GameManager.Level.ORIENT, "Orientation", "OrientRm", false),
                new LevelInfo(GameManager.Level.AVATARSELECT, "AvatarSelection", "", false),
                new LevelInfo(GameManager.Level.TEAMROOM, "TeamRoom", "TeamRm", true),
                new LevelInfo(GameManager.Level.CMDROOM, "CommandRoom", "CmdRm", true),
				new LevelInfo(GameManager.Level.MINICAMPUS, "ucicampus", "Mini Room", false),
                new LevelInfo(GameManager.Level.NAVTUTORIAL, "NavTutorial", "", false),
                new LevelInfo(GameManager.Level.COURTROOM, "courtroom", "CourtRm", true),
                new LevelInfo(GameManager.Level.HOSPITALROOM, "hospitalroom", "HospRm", true),
                new LevelInfo(GameManager.Level.BOARDROOM, "boardrmlg", "BoardRmL", true),
                new LevelInfo(GameManager.Level.BOARDROOM_MED, "boardrmmed", "BoardRmM", true),
                new LevelInfo(GameManager.Level.BOARDROOM_SM, "boardrmsm", "BoardRmS", true),
                new LevelInfo(GameManager.Level.OPENCAMPUS, "opencampus", "OCampus", false),
                new LevelInfo(GameManager.Level.MOTION_TEST, "mdons_motion", "", true),
                new LevelInfo(GameManager.Level.SCALE_GAME, "mdons_scale", "ScaleRm", true),
                new LevelInfo(GameManager.Level.OFFICE, "office", "Office", true)
            };
        }

        levelInfoFromSceneName = new Dictionary<string, LevelInfo>();
        levelInfoFromLevel = new Dictionary<GameManager.Level, LevelInfo>();
        for (int i = 0; i < levelInfo.Length; ++i)
        {
            levelInfoFromSceneName.Add(levelInfo[i].sceneName, levelInfo[i]);
            levelInfoFromLevel.Add(levelInfo[i].level, levelInfo[i]);
        }

    }

    public static LevelInfo GetInfo(string sceneName)
    {
        if (levelInfoFromSceneName == null)
            BuildLevelInfo();
        return levelInfoFromSceneName[sceneName];
    }

    public static LevelInfo GetInfo(GameManager.Level level)
    {
        if (levelInfoFromLevel == null)
            BuildLevelInfo();
        return levelInfoFromLevel[level];
    }

    public static LevelInfo GetInfo(Room smartfoxRm)
    {
        if (levelInfoFromLevel == null)
            BuildLevelInfo();
        foreach (KeyValuePair<GameManager.Level, LevelInfo> levelPair in levelInfoFromLevel)
            if (smartfoxRm.Name.Contains(levelPair.Value.sfsRoom))
                return levelPair.Value;
        return null;
    }

    public static void Reset() {
        levelInfoFromSceneName = null;
        levelInfoFromLevel = null;
    }
}
