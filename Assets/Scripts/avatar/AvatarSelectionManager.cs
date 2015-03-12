using UnityEngine;

public class AvatarSelectionManager : MonoBehaviour
{
    private static AvatarSelectionManager mInstance;
    public static AvatarSelectionManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("AvatarSelectionManager")).AddComponent(typeof(AvatarSelectionManager)) as AvatarSelectionManager;
            }
            return mInstance;
        }
    }

    public static AvatarSelectionManager Inst
    {
        get { return Instance; }
    }

    public void Touch() { }

    void Awake()
    {
        InitPlayer();
        GameGUI.Inst.customizeAvatarGui.OnLevelWasLoaded_();
        GameGUI.Inst.fadeOut = false;
    }

    public void InitPlayer()
    {
        RestrictPlayerMovement();
        RemovePlayerName();
    }

    private void RestrictPlayerMovement()
    {
        GameManager.Inst.LocalPlayer.gameObject.GetComponent<PlayerController>().enabled = false;
    }

    private void RemovePlayerName()
    {
        GameManager.Inst.LocalPlayer.DisableNameTextMesh();
    }
}
