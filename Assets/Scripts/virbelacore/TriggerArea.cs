using UnityEngine;
using System;
using System.Collections;

public class TriggerArea : MonoBehaviour
{

    public enum AreaShape { sphere, cube }
    public AreaShape areaShape = AreaShape.sphere;
    public float areaSize = 10f;

    public bool presenterToolWithin = false;

    // Prevents users from interacting with the browser window if they are outside this trigger.
    public bool lockBrowserOutside = false;
    // If set, this browser is locked. If null, locks all browsers.
    public CollabBrowserTexture[] browsersToLock = new CollabBrowserTexture[0];

    public string levelToLoadAsync = "";
    AsyncOperation async = null;

    // events
    public delegate void EnterHandler(object sender, EventArgs e);
    public event EnterHandler TriggerEnter;

    public delegate void ExitHandler(object sender, EventArgs e);
    public event ExitHandler TriggerExit;

    public bool stageTrigger = false;
    private bool playerWithin = false;
    private bool playerWithinCheck = false;


    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (areaShape == AreaShape.sphere)
            Gizmos.DrawWireSphere(transform.position, areaSize);
        else if (areaShape == AreaShape.cube)
            Gizmos.DrawWireCube(transform.position, Vector3.one * areaSize);

    }

    public bool ContainsPoint(Vector3 point)
    {
        if (areaShape == AreaShape.sphere)
            return Vector3.Distance(point, transform.position) <= areaSize;
        else
            return false;
    }

    void Update()
    {
        playerWithin = GameManager.Inst.LocalPlayer != null && ContainsPoint(GameManager.Inst.LocalPlayer.gameObject.transform.position);

        if (stageTrigger)
        {
            if (playerWithin && !playerWithinCheck)
            {
                playerWithinCheck = true;
                if (TriggerEnter != null)
                    TriggerEnter(this, new System.EventArgs());
            }
            else if (!playerWithin && playerWithinCheck)
            {
                playerWithinCheck = false;
                if (TriggerExit != null)
                    TriggerExit(this, new System.EventArgs());
            }
        }

        if (presenterToolWithin)
            GameGUI.Inst.allowPresenterTool = playerWithin;


        // Restrict use of (all) billboard interaction if you are not within the trigger.
        // This is more or less unique to the orientation hall.
        if (lockBrowserOutside)
        {
            // Lock all browsers...
            if (browsersToLock.Length == 0)
            {
                CollabBrowserTexture[] allBrowsers = FindObjectsOfType(typeof(CollabBrowserTexture)) as CollabBrowserTexture[];
                for (int i = 0; i < allBrowsers.Length; i++)
                {
                    if (allBrowsers[i] != GameGUI.Inst.guiLayer.htmlPanel.browserTexture)
                        allBrowsers[i].restrictedByTrigger = !playerWithin;
                }
            }
            // Lock specific browser(s)...
            else
            {
                for (int i = 0; i < browsersToLock.Length; i++)
                {
                    browsersToLock[i].restrictedByTrigger = !playerWithin;
                }
            }
        }
    }
}