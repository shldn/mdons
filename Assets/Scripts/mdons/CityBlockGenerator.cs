using UnityEngine;
using System.Collections;

public class CityBlockGenerator : MonoBehaviour {

    // Portland has streets 60 ft in width and blocks 260 ft to street center lines (http://www.strongtowns.org/journal/2013/11/27/optimizing-the-street-grid.html) (http://en.wikipedia.org/wiki/City_block)
    public float streetWidth = 60.0f;
    public float blockWidth = 200.0f;
    public int rows = 3;
    public int cols = 3;
    public GameObject building = null;

	// Use this for initialization
	void Start () {
        if (building == null)
        {
            Debug.LogError("City Block Generator - Please Specify a building!");
            return;
        }

        float rowDist = blockWidth + streetWidth;
        float colDist = rowDist;
        Vector3 rowOffset = rowDist * Vector3.forward;
        Vector3 colOffset = colDist * Vector3.right;

        Vector3 startPos = transform.position - 0.5f * (float)(rows) * rowOffset - 0.5f * (float)(cols) * colOffset;
        Vector3 center = startPos + 0.5f * rowOffset + 0.5f * colOffset;
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                GameObject obj = GameObject.Instantiate(building) as GameObject;
                obj.transform.position = center;
                center += colOffset;
            }
            center -= cols * colOffset;
            center += rowOffset;
        }
	}

    // Update is called once per frame
    void Update()
    {
	
	}
}
