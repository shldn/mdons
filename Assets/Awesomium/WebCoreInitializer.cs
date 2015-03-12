using UnityEngine;
using Awesomium.Mono;
using System.Collections;

/// <summary>
/// Script that allows initialization of the WebCore.
/// Add this script to any object, but no more than once.
/// </summary>
public class WebCoreInitializer : MonoBehaviour {
	
	// Awake is called only once during the lifetime
	// of the script instance and always before any Start functions
	// are called. This is the best place to initialize our WebCore.
	void Awake()
	{
		if ( !WebCore.IsRunning )
			WebCore.Initialize( new WebCoreConfig() { SaveCacheAndCookies = true } );
	}

    void OnApplicationQuit()
    {
        WebCore.Shutdown();
    }
}
