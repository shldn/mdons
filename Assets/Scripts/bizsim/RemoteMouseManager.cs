using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Core;

public class RemoteMouseManager : MonoBehaviour {

	static public Dictionary<int, PlayerMouseVisual> playerMouseVisuals;
    private static RemoteMouseManager mInstance;
    public static RemoteMouseManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("RemoteMouseMgr")).AddComponent(typeof(RemoteMouseManager)) as RemoteMouseManager;
            return mInstance;
        }
    }

    public static RemoteMouseManager Inst
    {
        get { return Instance; }
    }

	void Awake () {
        if (mInstance != null)
            Debug.LogError("RemoteMouseManager -- multiple instances");

        mInstance = this;
		playerMouseVisuals = new Dictionary<int, PlayerMouseVisual>();
	}

    public PlayerMouseVisual GetMyVisual()
    {
        return GetVisual(CommunicationManager.MySelf.Id);
    }

	public PlayerMouseVisual GetVisual(int id)
	{
		PlayerMouseVisual visual;
        if( !playerMouseVisuals.TryGetValue(id, out visual) )
        {
            try
            {
                GameObject emptyGO = new GameObject("MouseVisual" + id);
                visual = emptyGO.AddComponent<PlayerMouseVisual>();
                visual.SetID(id);
                playerMouseVisuals.Add(id,visual);
            }
            catch
            {
                Debug.Log("Exception caught adding new PlayerMouseVisual");
                return null;
            }
        }
        return visual;
	}

    public void OnObjectMessage(int userID, ISFSObject msgObj)
    {
        //if player is stealth, don't show a mouse sphere to others
        if (GameManager.Inst.playerManager != null && GameManager.Inst.playerManager.GetPlayer(userID) != null && GameManager.Inst.playerManager.GetPlayer(userID).Type == PlayerType.STEALTH)
            return;
        // Light-bandwidth mouse browers index, horizontal, vertical.
        else if (msgObj.ContainsKey("mpx"))
        {
            PlayerMouseVisual visual = GetVisual(userID);
            if (visual == null)
                return;

            Sfs2X.Util.ByteArray mouseBytes = msgObj.GetByteArray("mpx");

            int mouseBrowserID = mouseBytes.ReadByte();
            int mouseBrowserCoordX = mouseBytes.ReadByte();
            int mouseBrowserCoordY = mouseBytes.ReadByte();
            visual.mouseDown = mouseBytes.ReadBool();
            visual.browserId = mouseBrowserID;

            CollabBrowserTexture mouseBrowser = CollabBrowserTexture.GetAll()[mouseBrowserID];
            if (mouseBrowser)
            {
                visual.SetPosition(mouseBrowserCoordX, mouseBrowserCoordY);
                visual.textureScaleMult = Mathf.Min(mouseBrowser.transform.lossyScale.x, mouseBrowser.transform.lossyScale.y) * 0.1f;
            }
        }
        else if (msgObj.ContainsKey("mp"))
        {
            PlayerMouseVisual visual = GetVisual(userID);
            if (visual == null)
                return;

            Sfs2X.Util.ByteArray mouseBytes = msgObj.GetByteArray("mp");

            int mouseBrowserID = mouseBytes.ReadByte();
            float mouseBrowserCoordX = mouseBytes.ReadFloat();
            float mouseBrowserCoordY = mouseBytes.ReadFloat();
            visual.mouseDown = mouseBytes.ReadBool();
            visual.browserId = mouseBrowserID;

            CollabBrowserTexture mouseBrowser = CollabBrowserTexture.GetAll()[mouseBrowserID];
            if (mouseBrowser)
            {
                visual.SetPosition(mouseBrowserCoordX, mouseBrowserCoordY);
                visual.textureScaleMult = Mathf.Min(mouseBrowser.transform.lossyScale.x, mouseBrowser.transform.lossyScale.y) * 0.1f;
            }
        }
        else if( msgObj.ContainsKey("me") )
        {
            PlayerMouseVisual visual = GetVisual(userID);
            if( visual == null )
            	return;

            visual.SetVisibility(false);
        }
    }

    public void SetAllVisibility(bool visible)
    {
        foreach (KeyValuePair<int, PlayerMouseVisual> visualPair in playerMouseVisuals)
            visualPair.Value.SetVisibility(visible);
    }
}
