using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Core;
using Sfs2X.Requests;

public enum WallSide{
    LEFT,
    RIGHT
}

public struct BrowserParams
{
    public BrowserParams(int pxWidth_, int pxHeight_, string url_, WallSide side = WallSide.LEFT)
    {
        pxWidth = pxWidth_;
        pxHeight = pxHeight_;
        url = url_;
        wallSide = side;
    }
    public int pxWidth;
    public int pxHeight;
    public string url;
    public WallSide wallSide; // preference for side to add to display wall
}

public class BrowserDisplayWall : MonoBehaviour {

    private float wallAngle = 0.0f;
	private bool angleDirty = false; // controls when to send update messages to other clients
	private bool msgListenersSetup = false;
	private bool wallTrans = false;

    public LinkedList<PlaneMesh> browserPlanes = new LinkedList<PlaneMesh>();
    private LinkedListNode<PlaneMesh> focusedPlane = null;
    private LinkedListNode<PlaneMesh> middleBrowserPlane;

    public CollabBrowserTexture MiddleBrowser { get { return middleBrowserPlane.Value.go.GetComponent<CollabBrowserTexture>(); } }

	// Use this for initialization
	void Start () {
	}
	
	void Update()
	{
		// refactor -- shouldn't need this check every frame
		if ( !msgListenersSetup )
        {
            //CommunicationManager.Instance.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessage);
            msgListenersSetup = true;
        }
		
		if( angleDirty )
		{
			ISFSObject wallAngleObj = new SFSObject();
        	wallAngleObj.PutFloat("wa", wallAngle);
            CommunicationManager.SendObjectMsg(wallAngleObj); // Room Variable instead, so new users get it?
		}

#if UNITY_STANDALONE_OSX
        if( (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)) && (Input.GetKey(KeyCode.RightCommand) || Input.GetKey(KeyCode.LeftCommand)) && browserPlanes.First != null )
#else
        if( (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)) && (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && browserPlanes.First != null )
#endif
        {
            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                if (focusedPlane == null)
                    focusedPlane = browserPlanes.First;
                else if (focusedPlane.Previous != null)
                    focusedPlane = focusedPlane.Previous;
            }
            else // Input.GetKeyUp(KeyCode.RightArrow)
            {
                if (focusedPlane == null)
                    focusedPlane = browserPlanes.Last;
                else if (focusedPlane.Next != null)
                    focusedPlane = focusedPlane.Next;
            }
			
            float offsetBuffer = 4;
            float camPosOffset = CameraHelpers.GetCameraDistFromPlane(focusedPlane.Value, Camera.main) + offsetBuffer;
            Camera.main.transform.position = focusedPlane.Value.GetCenter() + camPosOffset * focusedPlane.Value.go.transform.forward;
            Camera.main.transform.forward = -focusedPlane.Value.go.transform.forward;
        }
	}
	
	public void ToggleTransparentBG()
	{
		wallTrans = !wallTrans;
		foreach (PlaneMesh p in browserPlanes)
		{
			p.GetBrowserTexture().SetBodyBGColor(wallTrans ? "transparent" : "");
		}
	}
	
    void OnGUI()
    {
        if (browserPlanes.Count > 1 && middleBrowserPlane != null)
        {
            float newAngle = GUI.HorizontalSlider(new Rect(Screen.width - 110, Screen.height - 20, 100, 30), wallAngle, 0.0f, 90);
            if (newAngle != wallAngle)
            {
                SetAngle(newAngle);
                angleDirty = true;
            }
        }
    }
	    
	public void OnObjectMessage(BaseEvent evt)
    {
        ISFSObject msgObj = (SFSObject)evt.Params["message"];

        if( msgObj.ContainsKey("wa") )
			SetAngle(msgObj.GetFloat("wa"));
    }

    // broadcastURL controls if the url change will be sent to all other clients.
    public void SetURL(int planeID, string newURL, bool broadcastURL)
    {
        if( planeID >= browserPlanes.Count )
            return;

        if( planeID > 2 )
        {
            // hack-a-roo implementation -- FIX ME
            Debug.Log("Logic only handles 3 planes, needs fixing");
        }
        switch( planeID )
        {
            case 0:
                GameObject fgo = (GameObject)browserPlanes.First.Value.go;
                fgo.GetComponent<CollabBrowserTexture>().GoToURL(newURL);
                break;
            case 1:
                MiddleBrowser.GoToURL(newURL);
                break;
            case 2:
                GameObject lgo = (GameObject)browserPlanes.Last.Value.go;
                lgo.GetComponent<CollabBrowserTexture>().GoToURL(newURL);
                break;
        }
    }

    public void AddBrowserPlane(BrowserParams bParams)
    {
        AddBrowserPlane(bParams.pxWidth, bParams.pxHeight, bParams.url, bParams.wallSide);
    }

    public void AddBrowserPlane(int pxWidth, int pxHeight, string defaultURL, WallSide sideToAdd)
    {

        if( pxWidth <= 0 || pxHeight <= 0 )
            return;

        PlaneMesh newPlane = PlaneMeshFactory.GetPlane(pxWidth, pxHeight, "PlaneMesh", true, defaultURL);

        if( browserPlanes.Count == 0 )
        {
            newPlane.go.transform.position = Vector3.zero;
            browserPlanes.AddFirst(newPlane);
            middleBrowserPlane = browserPlanes.First;
            return;
        }

        if (sideToAdd == WallSide.LEFT)
        {
            newPlane.go.transform.position = GetSidePos(WallSide.LEFT);
            browserPlanes.AddFirst(newPlane);
        }
        else // (sideToAdd == WallSide.RIGHT)
        {
            newPlane.go.transform.position = GetSidePos(WallSide.RIGHT);
            browserPlanes.AddLast(newPlane);
        }
    }

    Vector3 GetSidePos(WallSide side)
    {
        DebugUtils.Assert((side == WallSide.LEFT || side == WallSide.RIGHT));
        if (browserPlanes.Count == 0)
            return Vector3.zero;

        Transform neighborTransform;
        float rtDirection = 1.0f;
        if (side == WallSide.LEFT)
        {
            neighborTransform = browserPlanes.First.Value.go.transform;
        }
        else // side == WallSide.RIGHT
        {
            neighborTransform = browserPlanes.Last.Value.go.transform;
            rtDirection = -1.0f;
        }
        Vector3 rtAdjustment = browserPlanes.First.Value.worldWidth * neighborTransform.right;
        return neighborTransform.position + rtDirection * rtAdjustment;
    }
	
	public void SetAngle( float newDegreeAngle )
	{
		AdjustAngle(newDegreeAngle - wallAngle);
	}
	
    // Adjusts the angles of all wall joints by the specified delta angle
    public void AdjustAngle(float deltaDegreeAngle)
    {
        if( browserPlanes.Count <= 1 )
            return;
        Transform midTransform = middleBrowserPlane.Value.go.transform;
        Vector3 ltPt = 0.5f * middleBrowserPlane.Value.worldWidth * midTransform.localScale.x * midTransform.right;
        Vector3 rtPt = -0.5f * middleBrowserPlane.Value.worldWidth * midTransform.localScale.x * midTransform.right;
        LinkedListNode<PlaneMesh> it = browserPlanes.First;
        while (it != middleBrowserPlane && it != null)
        {
            it.Value.go.transform.RotateAround(ltPt, Vector3.up, -deltaDegreeAngle);
            it = it.Next;
        }
        it = browserPlanes.Last;
        while (it != middleBrowserPlane && it != null)
        {
            it.Value.go.transform.RotateAround(rtPt, Vector3.up, deltaDegreeAngle);
            it = it.Previous;
        }
        wallAngle += deltaDegreeAngle;
    }
}
