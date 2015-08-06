using UnityEngine;
using System.Collections;

public class CityBlockGenerator : MonoBehaviour {

    // Portland has streets 60 ft in width and blocks 260 ft to street center lines (http://www.strongtowns.org/journal/2013/11/27/optimizing-the-street-grid.html) (http://en.wikipedia.org/wiki/City_block)
    public float streetWidth = 60.0f;
    public float blockWidth = 200.0f;
    public float buildingOffsetFromStreet = 16f;
    public int rows = 3;
    public int cols = 3;
    public int innerRecursionLevel = 0;
    public int outerRecursionLevel = 0;
    public GameObject[] building;

    public Texture2D roadTexture;
    public Texture2D intersectionTexture;
    public Texture2D blockTexture;

    private float blockScale = 1f;
    public float BlockScale { 
        get { return blockScale; } 
        set { 
            blockScale = value;  
            transform.FindChild("city_meshes").localScale = blockScale * Vector3.one;
        }
    }

    Grid2D blockGrid = null;
    int buildingCounter = 0;
    Bounds ignoreBounds = new Bounds();

    public GameObject lowerLevel = null;
    public GameObject higherLevel = null;

    private bool initialized = false;
    public bool Initialized { get { return initialized; } }
    public Bounds IgnoreBounds { get { return ignoreBounds; } }
    bool IgnoreCenter { get { return innerRecursionLevel > 0; } }

    // Collider controller variables -- should be in controller class
    bool usingMeshCollider = false;
    bool autoAdjustColliderType = true;

	// Use this for initialization
	void Start () {
        if (building == null || building.Length == 0)
        {
            Debug.LogError("City Block Generator - Please Specify a building!");
            return;
        }

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
                    obj.transform.position = pos - buildingOffsetFromStreet * -Vector3.right;
                    obj.transform.forward = -Vector3.right;
                    obj.transform.parent = parent;
                    obj.transform.localScale = Vector3.one;
                }

                pos = To3D(blockGrid.RightEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj2 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj2.transform.position = pos - buildingOffsetFromStreet * Vector3.right;
                    obj2.transform.forward = Vector3.right;
                    obj2.transform.parent = parent;
                    obj2.transform.localScale = Vector3.one;
                }

                pos = To3D(blockGrid.TopEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj3 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj3.transform.position = pos - buildingOffsetFromStreet * Vector3.forward;
                    obj3.transform.forward = Vector3.forward;
                    obj3.transform.parent = parent;
                    obj3.transform.localScale = Vector3.one;
                }

                pos = To3D(blockGrid.BottomEdgeLerp(r, c, 0.5f));
                if (!ignoreBounds.Contains(pos))
                {
                    GameObject obj4 = GameObject.Instantiate(building[buildingCounter++ % building.Length]) as GameObject;
                    obj4.transform.position = pos - buildingOffsetFromStreet * -Vector3.forward;
                    obj4.transform.forward = -Vector3.forward;
                    obj4.transform.parent = parent;
                    obj4.transform.localScale = Vector3.one;
                }
            }
        }
        GridGutterMesher.CreateMeshes(blockGrid, parent, roadTexture, intersectionTexture, blockTexture, ignoreBounds);

        parent.localScale = blockScale * Vector3.one;

        // Map to origin for consistency of containment tests -- could be refactored since all ignoreBounds are the same dimensions
        ignoreBounds.center = Vector3.zero;

        initialized = true;

        if (innerRecursionLevel > 0 && lowerLevel == null)
            CreateNextLowerLevelImpl(GetTransformRoot());

        if (outerRecursionLevel > 0 && higherLevel == null)
            CreateNextHigherLevel(GetTransformRoot());

        gameObject.AddComponent<CityBlockBotController>();
	}

    void Update()
    {
        // This should be in a controller class
        if(autoAdjustColliderType)
        {
            float scaleDiffFactor = 3f;
            // both true or both false, will switch to mesh or to box
            if (usingMeshCollider == (Mathf.Log10(GameManager.Inst.LocalPlayer.Scale.x) > (Mathf.Log10(BlockScale * transform.lossyScale.x)) + scaleDiffFactor))
            {
                usingMeshCollider = !usingMeshCollider;
                ScaleSensitiveColliderController[] controllers = transform.FindChild("city_meshes").gameObject.GetComponentsInChildren<ScaleSensitiveColliderController>();
                for (int i = 0; i < controllers.Length; ++i)
                    controllers[i].AllowMeshCollisions = usingMeshCollider;
            }
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

    public void CreateNextLowerLevel()
    {
        RemoveCityCenter();
        CreateNextLowerLevelImpl(GetTransformRoot());
    }

    void CreateNextLowerLevelImpl(Transform parent)
    {
        GameObject nextLevel = new GameObject("Lower Level");
        nextLevel.transform.position = parent.position;
        nextLevel.transform.localScale = parent.transform.lossyScale;
        nextLevel.transform.parent = parent;

        CityBlockGenerator blockGen = AddCityBlockGenCopy(nextLevel);
        blockGen.blockScale = GetNextLowerLevelScale();
        blockGen.innerRecursionLevel = innerRecursionLevel - 1;
        blockGen.higherLevel = gameObject;
        lowerLevel = nextLevel;
    }

    public float GetNextHigherLevelScale()
    {
        return blockScale / ((blockGrid.TopLeft(rows - 2, 1).y - blockGrid.BottomLeft(1, 1).y) / blockGrid.GridSize.y);
    }

    public float GetNextLowerLevelScale()
    {
        return blockScale * (blockGrid.TopLeft(rows - 2, 1).y - blockGrid.BottomLeft(1, 1).y) / blockGrid.GridSize.y;
    }

    public void CreateNextHigherLevel(Transform parent)
    {
        GameObject nextLevel = new GameObject("Higher Level");

        CityBlockGenerator blockGen = AddCityBlockGenCopy(nextLevel);
        blockGen.blockScale = GetNextHigherLevelScale();
        blockGen.innerRecursionLevel = innerRecursionLevel + 1;
        blockGen.outerRecursionLevel = outerRecursionLevel - 1;
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
        blockGen.buildingOffsetFromStreet = buildingOffsetFromStreet;
        blockGen.rows = rows;
        blockGen.cols = cols;
        blockGen.building = building;
        blockGen.roadTexture = roadTexture;
        blockGen.intersectionTexture = intersectionTexture;
        blockGen.blockTexture = blockTexture;
        return blockGen;
    }

    public Vector3 GetRoadIntersectionPosition(int r, int c)
    {
        if( r >= rows || c >= cols )
        {
            Debug.LogError("GetRoadIntersectionPosition: query outsize row/col limits: " + r + ", " + c);
            return Vector3.zero;
        }

        Vector3 btmLt = To3D(blockGrid.TopRight(r, c));
        Vector3 btmRt = To3D(blockGrid.TopLeft(r, c + 1));
        Vector3 topLt = To3D(blockGrid.BottomRight(r + 1, c));
        Vector3 topRt = To3D(blockGrid.BottomLeft(r + 1, c + 1));
        return blockScale * 0.25f * (btmLt + btmRt + topLt + topRt);
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
