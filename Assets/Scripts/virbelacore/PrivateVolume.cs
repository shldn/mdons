using UnityEngine;
using Sfs2X.Entities;

/*-----------------------------------------
 * Note: The player must be in contact with the volume the whole time, can't be completely in it and not touching
 *       So, placing near the floor is a good approach, so they are always touching with their feet.
 * 
 */
public class PrivateVolume : MonoBehaviour {

    public int id = -1;

    [HideInInspector]
    public Room room;

    string RoomName { 
        get { 
            string sfsRm = GameManager.GetSmartFoxRoom(GameManager.Inst.LevelLoaded);
            sfsRm = string.IsNullOrEmpty(sfsRm) ? sfsRm : sfsRm.Substring(0,1);
            return "PR-" + id + "-" + CommunicationManager.Inst.roomNumToLoad + "-" + sfsRm;
        } 
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
        {
            CommunicationManager.SendJoinPrivateRoomRequest(RoomName);
            GameManager.Inst.LocalPlayer.InPrivateVolume = this;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
        {
            CommunicationManager.SendLeavePrivateRoomRequest(room);
            if (GameManager.Inst.LocalPlayer.InPrivateVolume == this)
                GameManager.Inst.LocalPlayer.InPrivateVolume = null;
        }
    }
}
