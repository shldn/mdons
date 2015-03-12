using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Core;

//-----------------------------------------------------------------------------------
// RemoteEffectManager.cs
//   Wes Hawkins
//
// Handles incoming smartfox commands for various player-related effects, such as
//   everyone's favorite, the 'confetti' effect.
//-----------------------------------------------------------------------------------

public class RemoteEffectManager : MonoBehaviour {

    public void OnObjectMessage(ISFSObject msgObj)
    {
        // Confetti
        if(msgObj.ContainsKey("cnfon")){
            foreach (KeyValuePair<int, Player> playerPair in GameManager.Inst.playerManager)
                if (playerPair.Value.TeamID == msgObj.GetInt("cnftm"))
                    playerPair.Value.ConfettiActive(msgObj.GetBool("cnfon"));
        }

        // Typing indication
        if(msgObj.ContainsKey("typn")){
            foreach (KeyValuePair<int, Player> playerPair in GameManager.Inst.playerManager)
                if (playerPair.Value.Id == msgObj.GetInt("plyr"))
                    playerPair.Value.IsTyping = msgObj.GetBool("typn");
        }
    } // End of OnObjectMessage().
} // End of RemoteEffectManager class.
