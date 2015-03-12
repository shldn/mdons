using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class GUIPatcher : MonoBehaviour {

	public string path = "";
	// Use this for initialization
	void Start () {
		path = Directory.GetCurrentDirectory();
		Debug.LogError("GUIPatcher - current working dir:" + path);
#if UNITY_EDITOR
		Debug.Log("GUIPatcher - unity editor mode!");
#elif UNITY_STANDALONE_WIN && PATCHER_ENABLE
        if (!path.Contains("\\deploy"))
            path = path + "\\deploy";
		path = path + @"\virbela_Data\Plugins\";
		RESThelper.DownloadFile(CommunicationManager.patchFileUrl, path + "test.zip");
		ZipHelper.Unzip(path + "test.zip", path);
#elif UNITY_STANDALONE_OSX && UNITY_EDITOR && PATCHER_ENABLE
		path = "/Applications/Unity/Unity.app/Contents/Frameworks/Awesomium.framework/Versions/Current/";
		RESThelper.DownloadFile(CommunicationManager.patchFileUrl, path + "test.zip");
		ZipHelper.Unzip(path + "test.zip", path);
#elif UNITY_STANDALONE_OSX && PATCHER_ENABLE
		path = path + "/virbela.app/Contents/Frameworks/Awesomium.framework/Versions/Current/";
		RESThelper.DownloadFile( CommunicationManager.patchFileUrl, path + "test.zip");
		ZipHelper.Unzip(path + "test.zip", path);
#endif
        Debug.LogError("GUIPatcher - destination dir:" + path);
	}
	
	// Update is called once per frame
	void Update () {

	}
	
}
