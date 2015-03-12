using UnityEngine;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;

public class TeamRoomManager
{
    private static TeamRoomManager mInstance;
    public static TeamRoomManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = new TeamRoomManager();
            return mInstance;
        }
    }

    public static TeamRoomManager Inst
    {
        get { return Instance; }
    }

    public void Touch() { }

}
