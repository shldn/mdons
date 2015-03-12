using UnityEngine;
using System.Collections;
using Sfs2X.Entities.Variables;
using System.Collections.Generic;
using Sfs2X.Requests;

public class SitController{

    Player player = null;
    bool isSitting = false;

    PlayerController.NavMode prevNavMode = PlayerController.NavMode.navmesh;

    public bool IsSitting { get { return isSitting; } }
    
    public SitController(Player p)
    {
        player = p;
    }

    public void Sit()
    {
        if (!isSitting)
        {
            prevNavMode = player.playerController.navMode;
            player.playerController.navMode = PlayerController.NavMode.locked;
            player.gameObject.GetComponent<AnimatorHelper>().StartAnim("Sit", true);
            if (player.IsLocal)
                UpdatePlayerServerVariable(true);
        }

#if UNITY_STANDALONE_OSX
        bool controlDown = (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple));
#else
        bool controlDown = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
#endif

        if (player.IsLocal && !controlDown)
            MainCameraController.Inst.cameraType = CameraType.FIRSTPERSON;

        if (!isSitting && GameGUI.Inst.guiLayer != null)
            GameGUI.Inst.guiLayer.SendGuiLayerSitStart();

        if (GameManager.Inst.LevelLoaded == GameManager.Level.OFFICE)
        {
            foreach (KeyValuePair<int, CollabBrowserTexture> cbt in CollabBrowserTexture.GetAll())
            {
                if (cbt.Key >= CollabBrowserId.TEAMWEB)
                    GameGUI.Inst.presenterToolCollabBrowser = cbt.Value;
            }
        }

        isSitting = true;
    }

    public void Stand()
    {
        if (!isSitting)
            return;

        player.playerController.navMode = prevNavMode;
        player.gameObject.GetComponent<AnimatorHelper>().StopAnim("Sit", true);
        if( player.IsLocal )
            MainCameraController.Inst.cameraType = CameraType.FOLLOWPLAYER;
        if (GameGUI.Inst.guiLayer != null)
            GameGUI.Inst.guiLayer.SendGuiLayerSitStop();
        isSitting = false;

        if (player.IsLocal)
            UpdatePlayerServerVariable(false);
    }

    private void UpdatePlayerServerVariable(bool sitting)
    {
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("sit", sitting));
        CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
    }

}
