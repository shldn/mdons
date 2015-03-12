using UnityEngine;
using System.Collections;

public class StringConvert {

    public static string ToString(Vector3 v)
    {
        return "(" + v.x + "," + v.y + "," + v.z + ")";
    }

    public static Vector3 ToVector3(string s)
    {
        s = s.TrimStart(new char[] { '(', ' ' });
        s = s.TrimEnd(new char[] { ')', ' ', '\n' });
        string[] tok = s.Split(',');

        Vector3 v = new Vector3();
        v.x = float.Parse(tok[0]);
        v.y = float.Parse(tok[1]);
        v.z = float.Parse(tok[2]);
        return v;
    }

    public static Quaternion ToQuaternion(float eulerY)
    {
        Quaternion q = new Quaternion();
        q.eulerAngles = new Vector3(0,eulerY,0);
        return q;
    }
}
