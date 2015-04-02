using UnityEngine;
using System.Collections;

// Grid2D
// Grows in the positive x and y direction from the startPos
public class Grid2D {

    int mRows;
    int mCols;

    Vector2 mStartPos;
    Vector2 mScale;
    float mGutter;

    public Grid2D(Vector2 start, int rows, int cols, Vector2 scale, float gutter = 0.0f)
    {
        mStartPos = start;
        mRows = rows;
        mCols = cols;
        mScale = scale;
        mGutter = gutter;
    }

    public int Rows { get { return mRows; } }
    public int Cols { get { return mCols; } }
    public Vector2 GridCenter { get { return mStartPos + 0.5f * (float)(mRows) * mScale.y * mGutter * Vector2.up + 0.5f * (float)(mCols) * mScale.x * mGutter * Vector2.right; } }


    public Vector2 Center(int row, int col)
    {
        return BottomLeft(row, col) + 0.5f * mScale;
    }


    // Corners
    //---------------------------------------------------

    public Vector2 BottomLeft(int row, int col)
    {
        return mStartPos + row * (mScale.y + mGutter) * Vector2.up + col * (mScale.x + mGutter) * Vector2.right;
    }

    public Vector2 TopRight(int row, int col)
    {
        return BottomLeft(row, col) + mScale;
    }

    public Vector2 BottomRight(int row, int col)
    {
        return BottomLeft(row, col) + mScale.x * Vector2.right;
    }

    public Vector2 TopLeft(int row, int col)
    {
        return BottomLeft(row, col) + mScale.y * Vector2.up;
    }


    // Edges
    //---------------------------------------------------
    public Tuple<Vector2, Vector2> LeftEdge(int row, int col)
    {
        return new Tuple<Vector2, Vector2>(BottomLeft(row, col), TopLeft(row, col));
    }

    public Tuple<Vector2, Vector2> RightEdge(int row, int col)
    {
        return new Tuple<Vector2, Vector2>(BottomRight(row, col), TopRight(row, col));
    }

    public Tuple<Vector2, Vector2> BottomEdge(int row, int col)
    {
        return new Tuple<Vector2, Vector2>(BottomLeft(row, col), BottomRight(row, col));
    }

    public Tuple<Vector2, Vector2> TopEdge(int row, int col)
    {
        return new Tuple<Vector2, Vector2>(TopLeft(row, col), TopRight(row, col));
    }


    // Edge Lerps
    //---------------------------------------------------
    public Vector2 LeftEdgeLerp(int row, int col, float t)
    {
        Tuple<Vector2, Vector2> ltEdge = LeftEdge(row, col);
        return new Vector2(Mathf.Lerp(ltEdge.First.x, ltEdge.Second.x, t), Mathf.Lerp(ltEdge.First.y, ltEdge.Second.y, t));
    }

    public Vector2 RightEdgeLerp(int row, int col, float t)
    {
        Tuple<Vector2, Vector2> rtEdge = RightEdge(row, col);
        return new Vector2(Mathf.Lerp(rtEdge.First.x, rtEdge.Second.x, t), Mathf.Lerp(rtEdge.First.y, rtEdge.Second.y, t));
    }

    public Vector2 BottomEdgeLerp(int row, int col, float t)
    {
        Tuple<Vector2, Vector2> btmEdge = BottomEdge(row, col);
        return new Vector2(Mathf.Lerp(btmEdge.First.x, btmEdge.Second.x, t), Mathf.Lerp(btmEdge.First.y, btmEdge.Second.y, t));
    }

    public Vector2 TopEdgeLerp(int row, int col, float t)
    {
        Tuple<Vector2, Vector2> topEdge = TopEdge(row, col);
        return new Vector2(Mathf.Lerp(topEdge.First.x, topEdge.Second.x, t), Mathf.Lerp(topEdge.First.y, topEdge.Second.y, t));
    }

}
