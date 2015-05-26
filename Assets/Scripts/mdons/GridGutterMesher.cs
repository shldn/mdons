using UnityEngine;
using System.Collections;

public class GridGutterMesher{

    public static void CreateMeshes(Grid2D grid, Transform parent, Texture2D mainTexture, Texture2D intersectionTexture, Texture2D holeTexture, Bounds? ignoreBounds = null)
    {
        int rows = grid.Rows;
        int cols = grid.Cols;

        // Create horizontal pieces
        for (int r = 0; r < rows-1; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                Vector3 btmLt = To3D(grid.BottomLeft(r + 1, c));
                Vector3 btmRt = To3D(grid.TopLeft(r, c));
                Vector3 topLt = To3D(grid.BottomRight(r + 1, c));
                Vector3 topRt = To3D(grid.TopRight(r, c));
                if (ignoreBounds != null && IsValid(ignoreBounds ?? new Bounds(), btmLt, btmRt, topLt, topRt))
                {
                    GameObject planeGO = CreatePlane(btmLt, btmRt, topLt, topRt, mainTexture, "RdH-" + r + "-" + c);
                    planeGO.transform.parent = parent;
                }
            }
        }

        // Create vertical pieces
        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols - 1; ++c)
            {
                Vector3 btmLt = To3D(grid.BottomRight(r, c));
                Vector3 btmRt = To3D(grid.BottomLeft(r, c + 1));
                Vector3 topLt = To3D(grid.TopRight(r, c));
                Vector3 topRt = To3D(grid.TopLeft(r, c + 1));
                if (ignoreBounds != null && IsValid(ignoreBounds ?? new Bounds(), btmLt, btmRt, topLt, topRt))
                {
                    GameObject planeGO = CreatePlane(btmLt, btmRt, topLt, topRt, mainTexture, "RdV-" + r + "-" + c);
                    planeGO.transform.parent = parent;
                }
            }
        }

        // Create intersections
        for (int r = 0; r < rows-1; ++r)
        {
            for (int c = 0; c < cols-1; ++c)
            {
                Vector3 btmLt = To3D(grid.TopRight(r, c));
                Vector3 btmRt = To3D(grid.TopLeft(r, c+1));
                Vector3 topLt = To3D(grid.BottomRight(r+1, c));
                Vector3 topRt = To3D(grid.BottomLeft(r+1, c+1));
                if (ignoreBounds != null && IsValid(ignoreBounds ?? new Bounds(), btmLt, btmRt, topLt, topRt))
                {
                    GameObject planeGO = CreatePlane(btmLt, btmRt, topLt, topRt, intersectionTexture, "RdIS-" + r + "-" + c);
                    planeGO.transform.parent = parent;
                }
            }
        }

        // Fill in the holes between the gutters
        if(holeTexture != null)
        {
            for (int r = 0; r < rows; ++r)
            {
                for (int c = 0; c < cols; ++c)
                {
                    Vector3 btmLt = To3D(grid.BottomLeft(r, c));
                    Vector3 btmRt = To3D(grid.BottomRight(r, c));
                    Vector3 topLt = To3D(grid.TopLeft(r, c));
                    Vector3 topRt = To3D(grid.TopRight(r, c));
                    if (ignoreBounds != null && IsValid(ignoreBounds ?? new Bounds(), btmLt, btmRt, topLt, topRt))
                    {
                        GameObject planeGO = CreatePlane(btmLt, btmRt, topLt, topRt, holeTexture, "Block-" + r + "-" + c);
                        planeGO.transform.parent = parent;
                    }
                }
            }
        }
    }

    // Valid if it doesn't contain all of the points.
    static bool IsValid(Bounds bounds, Vector3 pt1, Vector3 pt2, Vector3 pt3, Vector3 pt4)
    {
        return !(bounds.Contains(pt1) && bounds.Contains(pt2) && bounds.Contains(pt3) && bounds.Contains(pt4));
    }

    static GameObject CreatePlane(Vector3 btmLt, Vector3 btmRt, Vector3 topLt, Vector3 topRt, Texture2D texture, string name)
    {
        Mesh m = new Mesh();
        m.name = name;
        m.vertices = new Vector3[4] { topLt, topRt, btmRt, btmLt, };
        m.uv = new Vector2[4] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };
        m.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();
        GameObject obj = new GameObject(name + "GO", typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
        obj.GetComponent<MeshCollider>().sharedMesh = m;
        obj.GetComponent<MeshFilter>().mesh = m;
        obj.renderer.material = new Material(Shader.Find("Diffuse"));
        if( texture != null )
            obj.renderer.material.SetTexture("_MainTex", texture);
        return obj;
    }

    static Vector3 To3D(Vector2 v)
    {
        return new Vector3(v.x, 0f, v.y);
    }

}
