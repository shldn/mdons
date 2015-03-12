using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------- //
// MathHelper.cs
//   -Wes Hawkins
//
// Contains handy functions that aren't covered by C# or Unity's libraries.
// ---------------------------------------------------------------------------------- //

public class MathHelper {

    // Spits out a unit vector in the X/Z plane in the direction of the given y angle (in degrees.)
    public static Vector3 MoveAngleUnitVector(float yAngle)
    {
        return new Vector3(Mathf.Cos((yAngle - 90) * Mathf.Deg2Rad), 0f, Mathf.Sin(-(yAngle - 90) * Mathf.Deg2Rad));

    } // End of MoveAngleUnitVector().

    public static float UnitVectorMoveAngle(Vector3 vector)
    {
        Quaternion direction = Quaternion.LookRotation(vector);
        return direction.eulerAngles.y;
    } // End of UnitVectorMoveAngle().

    // Determines if two rectangles intersect.
    public static bool Intersect( Rect a, Rect b )
    {
        bool c1 = a.x < b.xMax;
        bool c2 = a.xMax > b.x;
        bool c3 = a.y < b.yMax;
        bool c4 = a.yMax > b.y;
        return c1 && c2 && c3 && c4;
    } // End of Intersect().

    // returns distance between the points projected on the xz plane
    public static float Distance2D(Vector3 ptA, Vector3 ptB)
    {
        float diffX = (ptA.x - ptB.x);
        float diffZ = (ptA.z - ptB.z);
        return Mathf.Sqrt(diffX * diffX + diffZ * diffZ);
    }

    // Remaps a value in a range to another range
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax){
        return toMin + (((value - fromMin) / (fromMax - fromMin)) * (toMax - toMin));
    }

    // expects a string without the leading #   example: "242424"
    public static Color HexToColor(string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte a = (hex.Length > 6) ? byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;
        return new Color32(r, g, b, a);
    }

} // End of MathHelper class.
