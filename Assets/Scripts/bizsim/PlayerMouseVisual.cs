/********************************************************************************
 *    Project   : VirBELA
 *    File      : PlayerMouseVisual.cs
 *    Version   : 
 *    Date      : 11/13/2012
 *    Author    : Erik Hill
 *    Copyright : UCSD
 *-------------------------------------------------------------------------------
 *
 *    Notes     :
 *
 *    Class to handle the mouse visual of other players
 *    - Commonly used to display over web pages to visualize thier interactions in real-time
 *    
 ********************************************************************************/

using UnityEngine;

public class PlayerMouseVisual : MonoBehaviour
{
	public GameObject mouseVisual;
	public float scale = 0.326f;
    private float currentScale = 1f;
    public int browserId = -1;
    public bool mouseDown = false;
    private bool lastMouseDown = false;
	private bool initialized = false;
    public float textureScaleMult = 1f;
    public bool replayMode = false;
	
	private void Init()
	{
		if( initialized ) 
			return;
		initialized = true;
				
		mouseVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mouseVisual.transform.localScale = Vector3.one * scale * currentScale * textureScaleMult;
        mouseVisual.GetComponent<Renderer>().material.shader = Shader.Find("GUI/3D Text Shader");
        mouseVisual.GetComponent<Renderer>().material.renderQueue = 3500; // Transparent == 3000, Overlay == 4000
        mouseVisual.layer = LayerMask.NameToLayer("Ignore Raycast"); // allow users to click through the mouse visual (makes the representation disappear on rollover though...)		
	}
	
    public void Start()
    {
		Init();
    }
	
	public void SetColor(Color c)
	{
		Init();
		mouseVisual.GetComponent<Renderer>().material.color = c;
	}
	
	public void SetID(int id)
	{
		Init();
        Player player;
        if( GameManager.Inst.playerManager && GameManager.Inst.playerManager.TryGetPlayer(id, out player) )
    		this.SetColor( player.Color );
	}

    void Update()
    {
        if (mouseDown)
            currentScale = 0.5f;
        else
            currentScale = Mathf.Lerp(currentScale, 1f, 5f * Time.deltaTime);

        mouseVisual.transform.localScale = Vector3.one * scale * currentScale * textureScaleMult;

    }

    public bool Visible { get { Init(); return mouseVisual.GetComponent<Renderer>().enabled; } }

	public void SetVisibility(bool visible)
	{
		Init();
		mouseVisual.GetComponent<Renderer>().enabled = visible;
	}

    public Vector3 TranformBrowserTextureCoordinatesToWorldSpace(int textureX, int textureY)
    {
        CollabBrowserTexture browser = CollabBrowserTexture.GetAll()[browserId];
        if (browser != null)
            return browser.transform.TransformPoint(new Vector3(((float)textureX / 255f) - 0.5f, ((float)(textureY - 0.5f) / 255f) - 0.5f, 0f));
        return Vector3.zero;
    }

    public Vector3 TranformBrowserTextureCoordinatesToWorldSpace(float textureX, float textureY)
    {
        CollabBrowserTexture browser = CollabBrowserTexture.GetAll()[browserId];
        if (browser != null)
            return browser.transform.TransformPoint(new Vector3(textureX, textureY, 0f));
        return Vector3.zero;
    }

    public void SetPosition(int textureX, int textureY)
    {
        Init();
        SetVisibility(true);
        mouseVisual.transform.position = TranformBrowserTextureCoordinatesToWorldSpace(textureX, textureY);
        if (replayMode && browserId != -1 && (mouseDown || lastMouseDown))
        {
            Event dragEvent = new Event();
            if (mouseDown && lastMouseDown != mouseDown)
                dragEvent.type = EventType.MouseDown;
            else if (lastMouseDown != mouseDown)
                dragEvent.type = EventType.MouseUp;
            else
                dragEvent.type = EventType.MouseDrag;
            CollabBrowserTexture browser = CollabBrowserTexture.GetAll()[browserId];
            browser.HandleBrowserEvent(dragEvent, (int)((1.0f - (float)textureX / 255f) * browser.width), (int)((float)((textureY - 0.5f) / 255f) * browser.height));
        }
        lastMouseDown = mouseDown;
    }
	
	// sets the marker position, not the component transform
	public void SetPosition(float textureX, float textureY)
	{
		Init();
		SetVisibility(true);
        mouseVisual.transform.position = TranformBrowserTextureCoordinatesToWorldSpace(textureX, textureY);
        if (replayMode && browserId != -1 && (mouseDown || lastMouseDown))
        {
            Event dragEvent = new Event();
            if( mouseDown && lastMouseDown != mouseDown )
                dragEvent.type = EventType.MouseDown;
            else if( lastMouseDown != mouseDown )
                dragEvent.type = EventType.MouseUp;
            else
                dragEvent.type = EventType.MouseDrag;
            CollabBrowserTexture browser = CollabBrowserTexture.GetAll()[browserId];
            browser.HandleBrowserEvent(dragEvent, (int)((0.5f - textureX) * browser.width), (int)((textureY+0.5f) * browser.height));
        }
        lastMouseDown = mouseDown;
	}

	public void SetPosition(Vector3 pos)
	{
		Init();
		SetVisibility(true);
		mouseVisual.transform.position = pos;
	}
}
