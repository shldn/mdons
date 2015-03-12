using UnityEngine;
public class TeleportTrigger : Teleporter
{
    public bool allowBackupTeleport = false;
    public bool addTipOnHover = false;
    private bool checkAngleWithinTeleport = false;

    void Start()
    {
        if (addTipOnHover)
        {
            MessageOnHover moh = gameObject.AddComponent<MessageOnHover>();
            moh.message = "Walk through to go to the " + GameManager.LevelToString(levelDestination);
        }
    }
    protected override void OnTriggerEnter(Collider other)
	{
        if ((GameManager.Inst.LocalPlayer != null) && (other.gameObject == GameManager.Inst.LocalPlayer.gameObject))
        {
            if (AngleCheck(other))
                Teleport();
            else
            {
                checkAngleWithinTeleport = true;
                InfoMessageManager.Display("Turn around to teleport");
            }
        }
        else if (GameManager.buildType == GameManager.BuildType.REPLAY && other.GetComponent<PlayerController>() != null)
            other.gameObject.SetActive(false);
	}

    void OnTriggerExit(Collider other)
    {
        checkAngleWithinTeleport = false;
    }

    void OnTriggerStay(Collider other)
    {
        if (checkAngleWithinTeleport && (GameManager.Inst.LocalPlayer != null) && (other.gameObject == GameManager.Inst.LocalPlayer.gameObject) && AngleCheck(other))
        {
            checkAngleWithinTeleport = false;
            Teleport();
        }

    }

    private bool AngleCheck(Collider other)
    {
        return (allowBackupTeleport || Vector3.Angle(GameManager.Inst.LocalPlayer.gameObject.transform.forward, gameObject.transform.forward) >= 100);
    }
}