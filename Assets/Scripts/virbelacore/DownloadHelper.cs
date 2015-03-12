using UnityEngine;
using System.Collections;

public class DownloadHelper : MonoBehaviour {
    
    public delegate void DownloadCallbackDelegate(WWW downloadObj);
    WWW www = null;
    bool sendGuilayerProgress = false;
    public void StartDownload(string url, DownloadCallbackDelegate callback, bool sendGuilayerProgress_ = false)
    {
        sendGuilayerProgress = sendGuilayerProgress_;
        StartCoroutine(DownloadProgress(url, callback));
    }

    IEnumerator DownloadProgress(string url, DownloadCallbackDelegate callback)
    {
        www = new WWW(url);
        if( !www.isDone )
            yield return www;

        callback(www);
        if( sendGuilayerProgress )
            GameGUI.Inst.guiLayer.SendGuiLayerProgress(name, www.progress);

        www = null;
        Destroy(this);
    }

    void Update()
    {
        if (www != null && sendGuilayerProgress && Time.frameCount % 2 == 0)
        {
            GameGUI.Inst.guiLayer.SendGuiLayerProgress(name, www.progress);
            VDebug.LogError("Download Progress: " + www.progress);
        }
    }
}
