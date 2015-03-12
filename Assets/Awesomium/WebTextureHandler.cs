using Awesomium.Mono;
using Awesomium.Unity;
using UnityEngine;
using System;

/// <summary>
/// Script that allows handling of event of a WebTexture component.
/// Add this script to the same game object you added a WebTexture.
/// </summary>
public class WebTextureHandler : MonoBehaviour {
	
	private WebTexture webTexture;
	
	// Use this for initialization
	void Start () 
	{
		// Get the WebTexture component of this game object.
		webTexture = this.GetComponent<WebTexture>();
		
		// Set a handler for the OpenExternalLink event.
		if ( webTexture != null )
			webTexture.OpenExternalLink += OnOpenExternalLink;
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	private void OnOpenExternalLink (object sender, OpenExternalLinkEventArgs e)
	{
		Debug.Log( String.Format( "Navigating to: {0}" , e.Url ) );
		// For this sample, we simply load the page to the same view.
		webTexture.LoadURL( e.Url );
	}
}
