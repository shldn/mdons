using UnityEngine;
using System.Collections;

public static class LightmapManager {

    private static void BuildLightMapArray(string name)
    {
        int numLightMaps = LightmapSettings.lightmaps.Length;
        string basePath = "Lightmaps/" + Application.loadedLevelName + name + "/";
        LightmapData[] lightmaparray = new LightmapData[numLightMaps];

        for (int i = 0; i < numLightMaps; i++)
        {
            LightmapData mapdata = new LightmapData();
            mapdata.lightmapFar = Resources.Load(basePath + "LightmapFar-" + i ) as Texture2D;
            mapdata.lightmapNear = Resources.Load(basePath + "LightmapNear-" + i ) as Texture2D;
            lightmaparray[i] = mapdata;
        }
        LightmapSettings.lightmaps = lightmaparray;
    }

    public static void ApplyLightMaps(string name)
    {
        BuildLightMapArray(name);
    }

    private static void ApplyLightMapToGameObject(string name, int lightmapIdx)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            Debug.LogError("Applying lightmap to " + name);
            if (go.GetComponent<Renderer>() != null)
                go.GetComponent<Renderer>().lightmapIndex = lightmapIdx;
            else
                Debug.LogError("Null renderer for " + name);
        }
    }
}