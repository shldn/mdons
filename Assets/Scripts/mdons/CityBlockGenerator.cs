﻿using UnityEngine;
using System.Collections;

public class CityBlockGenerator : MonoBehaviour {

    // Portland has streets 60 ft in width and blocks 260 ft to street center lines (http://www.strongtowns.org/journal/2013/11/27/optimizing-the-street-grid.html) (http://en.wikipedia.org/wiki/City_block)
    public float streetWidth = 60.0f;
    public float blockWidth = 200.0f;
    public int rows = 3;
    public int cols = 3;
    public int innerRecursionLevel = 0;
    public GameObject[] building;

    public Texture2D roadTexture;
    public Texture2D intersectionTexture;
    public Texture2D blockTexture;

    public float roadScale = 1f; // helpful for recursion -- applied to all meshes added. -- IMPLEMENT ME!
    public float objScale = 1f;

    Grid2D blockGrid = null;
    int buildingCounter = 0;
    Bounds ignoreBounds = new Bounds();

    public GameObject lowerLevel = null;
    public GameObject higherLevel = null;

    public Bounds IgnoreBounds { get { return ignoreBounds; } }
    bool IgnoreCenter { get { return innerRecursionLevel > 0; } }

	// Use this for initialization
	void Start () {
        if (building == null || building.Length == 0)
        {
            Debug.LogError("City Block Generator - Please Specify a building!");
            return;
        }

        streetWidth *= roadScale;
        blockWidth *= roadScale;
        float xCenterOffset = 0.5f * ((cols - 1) * (blockWidth + streetWidth) + blockWidth);
        float zCenterOffset = 0.5f * ((rows - 1) * (blockWidth + streetWidth) + blockWidth);
        Vector2 btmLtPos = new Vector2(transform.position.x - xCenterOffset, transform.position.z - zCenterOffset);
        blockGrid = new Grid2D(btmLtPos, rows, cols, new Vector2(blockWidth, blockWidth), streetWidth);

        if (IgnoreCenter)
            ignoreBounds = GetIgnoreBounds();

        GameObject meshContainer = new GameObject("city_meshes");
        meshContainer.transform.position = transform.position;
        meshContainer.transform.parent = transform;
        Transform parent = meshContainer.transform;

        Vector3 pos = Vector3.zero;
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                pos = To3D(blockGrid.LeftEdgeLerp(r, c, 0.5f));
                if( !ignoreBounds.Contains(pos) )
                {
                    GameObject obj = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj.transform.position = pos;
                    obj.transform.forward = -Vector3.right;
                    obj.transform.localScale = objScale * parent.lossyScale;
                    obj.transform.parent = parent;
                }

                pos = To3D(blockGrid.RightEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj2 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj2.transform.position = pos;
                    obj2.transform.forward = Vector3.right;
                    obj2.transform.localScale = objScale * parent.lossyScale;
                    obj2.transform.parent = parent;
                }

                pos = To3D(blockGrid.TopEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj3 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj3.transform.position = pos;
                    obj3.transform.forward = Vector3.forward;
                    obj3.transform.localScale = objScale * parent.lossyScale;
                    obj3.transform.parent = parent;
                }

                pos = To3D(blockGrid.TopEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj4 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj4.transform.position = To3D(blockGrid.BottomEdgeLerp(r, c, 0.5f));
                    obj4.transform.forward = -Vector3.forward;
                    obj4.transform.localScale = objScale * parent.lossyScale;
                    obj4.transform.parent = parent;
                }
            }
        }
        GridGutterMesher.CreateMeshes(blockGrid, parent, roadTexture, intersectionTexture, blockTexture, ignoreBounds);

        // Help remap scale if the world scale has been adjusted.
        if (parent.localScale != Vector3.one)
            parent.localScale = Vector3.one;

        if (innerRecursionLevel > 0 && lowerLevel == null)
            CreateNextLowerLevel(parent);
	}

    void Update()
    {
        if (higherLevel == null && Input.GetKeyUp(KeyCode.R))
            CreateNextHigherLevel(GetTransformRoot());
        if (lowerLevel == null && Input.GetKeyUp(KeyCode.Y))
        {
            RemoveCityCenter();
            Transform parent = gameObject.transform.FindChild("city_meshes");
            CreateNextLowerLevel(parent);
        }
    }

    Transform GetTransformRoot()
    {
        Transform root = transform;
        while (root.parent != null)
            root = root.parent;
        return root;
    }

    Bounds GetIgnoreBounds()
    {
        return new Bounds(To3D(blockGrid.GridCenter), new Vector3(blockGrid.GridSize.x - 2f * (blockWidth + 0.75f * streetWidth), 10f, blockGrid.GridSize.y - 2f * (blockWidth + 0.75f * streetWidth)));
    }

    public void RemoveCityCenter()
    {
        ignoreBounds = GetIgnoreBounds();
        ignoreBounds.center = Vector3.zero;
        Transform city_meshes = transform.FindChild("city_meshes");
        for(int i=0; i < city_meshes.childCount; ++i)
        {
            Transform child = city_meshes.GetChild(i);
            if (ignoreBounds.Contains(child.transform.localPosition))// || (child.renderer != null && ignoreBounds.Intersects(child.renderer.bounds)))
                Destroy(child.gameObject);
        }
    }

    public void CreateNextLowerLevel(Transform parent)
    {
        GameObject nextLevel = new GameObject("Next Level");
        nextLevel.transform.position = parent.position;
        nextLevel.transform.localScale = parent.transform.lossyScale;
        nextLevel.transform.parent = parent;

        CityBlockGenerator blockGen = AddCityBlockGenCopy(nextLevel);
        blockGen.roadScale = (blockGrid.TopLeft(rows - 2, 1).y - blockGrid.BottomLeft(1, 1).y) / blockGrid.GridSize.y;
        blockGen.objScale = blockGen.roadScale * objScale;
        blockGen.innerRecursionLevel = innerRecursionLevel - 1;
        blockGen.higherLevel = gameObject;
        lowerLevel = nextLevel;
    }

    public void CreateNextHigherLevel(Transform parent)
    {
        GameObject nextLevel = new GameObject("Higher Level");

        CityBlockGenerator blockGen = AddCityBlockGenCopy(nextLevel);
        blockGen.roadScale = 1f / ((blockGrid.TopLeft(rows - 2, 1).y - blockGrid.BottomLeft(1, 1).y) / blockGrid.GridSize.y);
        blockGen.objScale = blockGen.roadScale * objScale;
        blockGen.innerRecursionLevel = innerRecursionLevel + 1;
        blockGen.lowerLevel = gameObject;
        higherLevel = nextLevel;

        nextLevel.transform.position = transform.position;
        nextLevel.transform.localScale = transform.lossyScale;
        nextLevel.transform.parent = parent;
        
    }

    CityBlockGenerator AddCityBlockGenCopy(GameObject go)
    {
        CityBlockGenerator blockGen = go.AddComponent<CityBlockGenerator>();
        blockGen.streetWidth = streetWidth;
        blockGen.blockWidth = blockWidth;
        blockGen.rows = rows;
        blockGen.cols = cols;
        blockGen.building = building;
        blockGen.roadTexture = roadTexture;
        blockGen.intersectionTexture = intersectionTexture;
        blockGen.blockTexture = blockTexture;
        return blockGen;
    }

    Vector3 To3D(Vector2 v)
    {
        return new Vector3(v.x, transform.position.y, v.y);
    }

    Vector2 To2D(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (blockGrid != null)
        {
            Gizmos.DrawWireCube(ignoreBounds.center, ignoreBounds.size);
        }
    }

}
