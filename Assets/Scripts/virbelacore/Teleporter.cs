using UnityEngine;
using System.Collections;

public class Teleporter : MonoBehaviour {

    public GameManager.Level levelDestination = GameManager.Level.NONE;
    public int teamIdOverride = -1;
    public PlayerType minAccessRights = PlayerType.NORMAL;
    GameObject teleportDestination = null;
    public bool resetTutorialMovers = false;

    private float teleportTimer = 0f;
    private float teleportDelay = 2f;
    private bool makeActionRequest = false;
    protected bool teleporting = false;


    void Update()
    {
        if (teleporting)
        {
            teleportTimer += Time.deltaTime;
            GameGUI.Inst.fadeAlpha = 1f - Mathf.Clamp(teleportDelay - teleportTimer, 0f, 1f);
            GameManager.Inst.LocalPlayer.gameObject.GetComponent<PlayerController>().lockMovement = true;
        }
        else
            teleportTimer = 0f;



        // Aaaaand teleport!
        if (makeActionRequest && teleportTimer >= teleportDelay)
        {
            if (levelDestination != GameManager.Level.NONE)
            {
                if (!CommunicationManager.teamRmLockdown && teamIdOverride > -1)
                    CommunicationManager.Inst.roomNumToLoad = teamIdOverride;
                GameManager.Inst.LoadLevel(levelDestination);
            }
            else if (teleportDestination != null)
            {
                SoundManager.Inst.PlayTeleport();
                GameManager.Inst.playerManager.SetLocalPlayerTransform(teleportDestination.transform);
                GameManager.Inst.LocalPlayer.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                GameManager.Inst.LocalPlayer.gameObject.GetComponent<PlayerController>().forwardAngle = teleportDestination.transform.eulerAngles.y;
                //GameManager.Inst.LocalPlayer.EnableRigidBody = false;

                teleporting = false;
                GameManager.Inst.LocalPlayer.IsTeleporting = false;
                MainCameraController.Inst.initializedPosition = false;
            }
            else
                Debug.LogError("Teleport location not set in editor");

            makeActionRequest = false;
        }
    } // End of Update().

    protected virtual void OnTriggerEnter(Collider other)
    {
    }

    protected void Teleport()
    {
        if (GameManager.Inst.LocalPlayer.Type < minAccessRights)
        {
            InfoMessageManager.Display("Sorry, you do not have access to this room");
            return;
        }

        if (WebViewManager.Inst.AreAnyBusy())
            WebViewManager.Inst.StopAll();
        teleporting = true;
        GameManager.Inst.LocalPlayer.IsTeleporting = true;
        makeActionRequest = true;
    }
}
