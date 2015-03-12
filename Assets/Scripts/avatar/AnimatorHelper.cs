using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;

public class AnimatorHelper : MonoBehaviour {

    private int customAnimStartFrame = -1;
	public Animator anim;
    private Dictionary<int, string> availableAnims = new Dictionary<int, string>()
    {
        { Animator.StringToHash("Base Layer.Wave"), "Wave"  },
        { Animator.StringToHash("Base Layer.Dance1"), "Dance" },
        { Animator.StringToHash("Base Layer.Confused"), "Confuse" },
        { Animator.StringToHash("Base Layer.Think"), "Think" },
        { Animator.StringToHash("Base Layer.Impatient"), "Impatient" },
        { Animator.StringToHash("Base Layer.Cheer"), "Cheer" },
        { Animator.StringToHash("Base Layer.Clap"), "Clap" },
        { Animator.StringToHash("Base Layer.Cry"), "Cry" },
        { Animator.StringToHash("Base Layer.PowerPose"), "Powerpose" },
        { Animator.StringToHash("Base Layer.Laugh"), "Laugh" },
        { Animator.StringToHash("Base Layer.Samba"), "Samba" },
        { Animator.StringToHash("Base Layer.Comehere"), "Comehere" },
        { Animator.StringToHash("Base Layer.Raisehand"), "Raisehand" },
        { Animator.StringToHash("Base Layer.Bow"), "Bow" },
        { Animator.StringToHash("Base Layer.Shakehand"), "Shakehand" },
        { Animator.StringToHash("Base Layer.Backflip"), "Backflip" },
        { Animator.StringToHash("Base Layer.Walk"), "Walk" },
        { Animator.StringToHash("Base Layer.Char_sit_idle"), "Sit" }
    };

    // These need to be toggled off with a StopAnim call
    private List<string> toggleAnims = new List<string>() { "Sit" };

	void Awake () {
		anim = GetComponent<Animator>();	
	}
	
    public void SetSpeed(float speed) {
        anim.SetFloat("Speed", Mathf.Abs(speed));
        if (speed >= 0)
            anim.SetFloat("Direction", 1f);
        if (speed < 0)
            anim.SetFloat("Direction", -1f);
    }

    void Update() {
        int currentStateHash = anim.GetCurrentAnimatorStateInfo(0).nameHash;

        // If playing an anim that should only play once, turn off the flag that sends mecanim to its play state
        if (availableAnims.ContainsKey(currentStateHash))
            anim.SetBool(availableAnims[currentStateHash], false);

        bool customAnimationDone = IsIdle() && !HasNextAnimation() && Time.frameCount > customAnimStartFrame + 3;
        if (GameManager.Inst.ServerConfig == "Assembly" && MainCameraController.Inst.cameraType == CameraType.SNAPCAM && GameManager.Inst.LocalPlayer.gameObject == gameObject && customAnimationDone)
            MainCameraController.Inst.cameraType = CameraType.FOLLOWPLAYER;

        if (customAnimationDone)
            customAnimStartFrame = -1;
    }

    public bool IsValidAnim(string animName) {
        return availableAnims.ContainsValue(animName);
    }

    public void StartAnim(string animName, bool sendToServer) {
        customAnimStartFrame = Time.frameCount;
        if (toggleAnims.Contains(animName))
            anim.SetBool(animName + "Exit", false);
        anim.SetBool(animName, true);
        if (sendToServer) {
            ISFSObject animUpdateObj = new SFSObject();
            animUpdateObj.PutUtfString("type", "anim");
            animUpdateObj.PutUtfString("anim", animName);
            CommunicationManager.SendObjectMsg(animUpdateObj);
        }
    }

    // This is only applicable for anims that require this action to stop. Sitting for example.
    public void StopAnim(string animName, bool sendToServer)
    {
        anim.SetBool(animName + "Exit", true);
        if (sendToServer)
        {
            ISFSObject animUpdateObj = new SFSObject();
            animUpdateObj.PutUtfString("type", "panim");
            animUpdateObj.PutUtfString("anim", animName);
            CommunicationManager.SendObjectMsg(animUpdateObj);
        }
    }
    public void EnableLooping(bool enable)
    {
        anim.SetBool("loop", enable);
    }

    public bool IsIdle()
    {
        int currentNameHash = anim.GetCurrentAnimatorStateInfo(0).nameHash;
        return currentNameHash == Animator.StringToHash("Base Layer.IdleBlends") || currentNameHash == Animator.StringToHash("Base Layer.Idle");
    }

    public bool IsSitting()
    {
        return anim.GetCurrentAnimatorStateInfo(0).nameHash == Animator.StringToHash("Base Layer.Char_sit_idle");
    }

    public bool HasNextAnimation()
    {
        int nextNameHash = anim.GetNextAnimatorStateInfo(0).nameHash;
        return nextNameHash != 0;
    }

    public bool CustomAnimPlaying(){
        return customAnimStartFrame != -1;
    }
}
