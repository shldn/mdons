using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RemoteLogo : MonoBehaviour {
    public enum LogoType
    {
        FLAG = 0,
        BANNER = 1,
    }

    public LogoType logoType = LogoType.FLAG;

    private static Dictionary<string, Texture2D> texturesDownloaded = new Dictionary<string, Texture2D>();
    private static Dictionary<string, List<Material>> texturesToApplyOnDownloadComplete = new Dictionary<string, List<Material>>();

	void Start () {
        Texture2D texture = null;
        if (texturesDownloaded.TryGetValue(GetLogoURL(), out texture))
            renderer.material.mainTexture = texture;
        else if (texturesToApplyOnDownloadComplete.ContainsKey(GetLogoURL()))
            texturesToApplyOnDownloadComplete[GetLogoURL()].Add(renderer.material);
        else
        {
            List<Material> matList = new List<Material>() { renderer.material };
            texturesToApplyOnDownloadComplete.Add(GetLogoURL(), matList);

            gameObject.AddComponent<DownloadHelper>().StartDownload(GetLogoURL(), HandleDownloadComplete);
        }
	}

    void HandleDownloadComplete(WWW wwwObj)
    {
        if (string.IsNullOrEmpty(wwwObj.error) && wwwObj.isDone && wwwObj.texture != null)
        {
            for (int i = 0; i < texturesToApplyOnDownloadComplete[GetLogoURL()].Count; ++i)
                texturesToApplyOnDownloadComplete[GetLogoURL()][i].mainTexture = wwwObj.texture;
            texturesDownloaded.Add(GetLogoURL(), wwwObj.texture);
        }
    }

    string GetLogoURL()
    {
        return "http://game.virbela.com/logos/" + GameManager.Inst.ServerConfig.ToLower() + "_" + logoType.ToString().ToLower() + ".jpg";
    }
}
