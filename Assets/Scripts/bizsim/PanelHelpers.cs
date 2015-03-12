using UnityEngine;
using System.Collections;

public class PanelHelpers {

    public static int GetPxHeightFromTransform(Transform t, int pixelWidth)
    {
        // width and height assumed to be = to the scale (y = height, x = width)
        if (pixelWidth <= 0)
        {
            Debug.LogError("Please specify a pixel width > 0");
            return -1;
        }
        float metersPerPx = t.localScale.x / (float)pixelWidth;
        return (int)(t.localScale.y / metersPerPx);
    }
}
