using UnityEngine;
using System.Collections;

public class CityBlockGenerator : MonoBehaviour {

    // Portland has streets 60 ft in width and blocks 260 ft to street center lines (http://www.strongtowns.org/journal/2013/11/27/optimizing-the-street-grid.html) (http://en.wikipedia.org/wiki/City_block)
    public float streetWidth = 60.0f;
    public float blockWidth = 200.0f;
    public int rows = 3;
    public int cols = 3;
    public GameObject building = null;

    Grid2D blockGrid = null;

	// Use this for initialization
	void Start () {
        if (building == null)
        {
            Debug.LogError("City Block Generator - Please Specify a building!");
            return;
        }

        blockGrid = new Grid2D(To2D(transform.position), rows, cols, new Vector2(blockWidth, blockWidth), streetWidth); 

        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                GameObject obj = GameObject.Instantiate(building) as GameObject;
                obj.transform.position = To3D(blockGrid.LeftEdgeLerp(r, c, 0.5f));
                obj.transform.forward = -Vector3.right;

                GameObject obj2 = GameObject.Instantiate(building) as GameObject;
                obj2.transform.position = To3D(blockGrid.RightEdgeLerp(r, c, 0.5f));
                obj2.transform.forward = Vector3.right;

                GameObject obj3 = GameObject.Instantiate(building) as GameObject;
                obj3.transform.position = To3D(blockGrid.TopEdgeLerp(r, c, 0.5f));
                obj3.transform.forward = Vector3.forward;

                GameObject obj4 = GameObject.Instantiate(building) as GameObject;
                obj4.transform.position = To3D(blockGrid.BottomEdgeLerp(r, c, 0.5f));
                obj4.transform.forward = -Vector3.forward;
            }
        }
	}

    Vector3 To3D(Vector2 v)
    {
        return new Vector3(v.x, transform.position.y, v.y);
    }

    Vector2 To2D(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

}
