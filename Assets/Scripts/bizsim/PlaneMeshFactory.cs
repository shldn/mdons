using UnityEngine;
using System.Collections;

public class PlaneMesh
{
    public PlaneMesh(GameObject go_, float worldWidth_, float worldHeight_)
    {
        go = go_;
        worldWidth = worldWidth_;
        worldHeight = worldHeight_;
    }

	public Vector3 GetCenter()
	{
		return go.transform.position + 0.5f * worldHeight * go.transform.up;
	}
    public CollabBrowserTexture GetBrowserTexture()
    {
        return go.GetComponent<CollabBrowserTexture>();
    }

    public void SaveToPNG(string path)
    {
        string filename = path + go.name + ".png";
        if( GetBrowserTexture() != null )
            GetBrowserTexture().SaveToPNG(filename);
    }

    public GameObject go; // game object is assumed to have a CollabBrowserTexture attached
    public float worldWidth;
    public float worldHeight;
}

public static class PlaneMeshFactory
{	
    static public float metersPerPixel = 0.009f;

	static GameObject CreatePlane(float width, float height, string name)
	{
	    float hw = 0.5f * width;

	    Mesh m = new Mesh();
        m.name = name;
	    m.vertices = new Vector3[4]{new Vector3(-hw, 0, 0.01f), new Vector3(hw, 0, 0.01f), new Vector3(hw, height, 0.01f), new Vector3(-hw, height, 0.01f)};
	    m.uv =  new Vector2[4]{new Vector2 (1, 0), new Vector2 (0, 0), new Vector2 (0, 1), new Vector2(1, 1)};
	    m.triangles = new int[6]{0, 1, 2, 0, 2, 3};
	    m.RecalculateNormals();
        GameObject obj = new GameObject(name + "GO", typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
	    obj.GetComponent<MeshCollider>().sharedMesh = m;
	    obj.GetComponent<MeshFilter>().mesh = m;
	    return obj;
	}

    public static PlaneMesh GetPlane(int pxWidth, int pxHeight, string planeGOName = "PlaneMesh", bool addBrowser = false, string initialURL = "")
	{
	        float worldWidth = metersPerPixel * pxWidth;
	        float worldHeight = metersPerPixel * pxHeight;
            GameObject planeMeshGO = CreatePlane(worldWidth, worldHeight, planeGOName);
            PlaneMesh newPlane = new PlaneMesh(planeMeshGO, worldWidth, worldHeight);
            if (planeMeshGO == null)
	            Debug.LogError("Public Browser failed to instantiate");
            if (addBrowser)
            {
                CollabBrowserTexture bTex = planeMeshGO.AddComponent<CollabBrowserTexture>();
                bTex.Width = pxWidth;
                bTex.Height = pxHeight;
                bTex.InitialURL = initialURL;
            }
	        return newPlane;
	}
}
