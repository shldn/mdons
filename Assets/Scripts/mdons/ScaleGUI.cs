using UnityEngine;
using System.Collections;

public class ScaleGUI : MonoBehaviour {

    public static ScaleGUI Inst = null;

    public Texture2D upArrow;
    public Texture2D downArrow;
    public bool flash = true;

    private Texture2D upImpl;
    private Texture2D downImpl;

    private float upDelay = 0.1f;
    private float downDelay = 0.3f;

    void Awake()
    {
        Inst = this;
        upImpl = upArrow;
        downImpl = downArrow;

        if(flash)
        {
            InvokeRepeating("SwapUpTexture", upDelay, upDelay);
            InvokeRepeating("SwapDownTexture", downDelay, downDelay);
        }
        enabled = false;
    }

	void OnGUI () {
        int gutter = 10;
        int buttonSize = Screen.width / 10;
        if (GUI.RepeatButton(new Rect(gutter, Screen.height - gutter - buttonSize, buttonSize, buttonSize), downImpl))
            ScaleGameManager.Inst.ScaleDown();
        if (GUI.RepeatButton(new Rect(Screen.width - gutter - buttonSize, Screen.height - gutter - buttonSize, buttonSize, buttonSize), upImpl))
            ScaleGameManager.Inst.ScaleUp();
    }

    void SwapUpTexture()
    {
        if (upImpl == upArrow)
            upImpl = null;
        else
            upImpl = upArrow;
    }

    void SwapDownTexture()
    {
        if (downImpl == downArrow)
            downImpl = null;
        else
            downImpl = downArrow;
    }
}
