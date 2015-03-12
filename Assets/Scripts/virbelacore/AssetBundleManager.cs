using UnityEngine;
using System.Collections;

// For now, attach this to a 'hardpoints' gameObject. This script will load the asset bundle, then
//   match the position of the spawned-in gameobjects to the hardpoints with matching names in the
//   hierarchy of this object's transform. (And object named "Centerpiece" will be placed on the
//   'Centerpiece' child of this gameObject.

public class AssetBundleManager : MonoBehaviour {

    public static AssetBundleManager Inst = null;

    WWW request = null;
    public GameObject loadedObject = null;

    void Awake()
    {
        Inst = this;
    }

    public void LoadAssetsCoroutine(string s)
    {
        StartCoroutine(LoadAsset(s));
        Debug.LogError("Loading assets...");
    } // End of Start().

    private IEnumerator LoadAsset(string s)
    {
        Debug.LogError("Loading " + s + " ...");
        request = WWW.LoadFromCacheOrDownload(s, 0);
        while (!request.isDone)
            // avoid freezing the editor:
            yield return ""; // just wait.
        // I don't like the following line because it will freeze up 
        // your editor.  So I commented it out.  
        // yield return request;
        if (request.error != null)
            Debug.LogError(request.error);

        Debug.LogError("Object loaded!");

        loadedObject = (GameObject)request.assetBundle.mainAsset as GameObject;
        GameObject instantiatedObject = Instantiate(loadedObject) as GameObject;

        Transform[] hardpoints = gameObject.GetComponentsInChildren<Transform>();
        Transform[] loadedObjects = instantiatedObject.GetComponentsInChildren<Transform>();
        for (int i = 0; i < loadedObjects.Length; i++)
        {
            Transform currentLoadedObj = loadedObjects[i];
            for (int j = 0; j < hardpoints.Length; j++)
            {
                Transform currentHardpoint = hardpoints[j];
                if (currentHardpoint.gameObject.name.Equals(currentLoadedObj.gameObject.name))
                {
                    Instantiate(currentLoadedObj, currentHardpoint.position, currentHardpoint.rotation);
                }
            }
        }

        Destroy(instantiatedObject);


    } // End of LoadAsset().

} // End of AssetBundleManager.