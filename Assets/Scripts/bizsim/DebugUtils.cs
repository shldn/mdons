#define DEBUG

using System;
using System.Diagnostics;

public class DebugUtils
{
    [Conditional("UNITY_EDITOR")]
    static public void Assert(bool condition)
    {
        if (!condition) throw new Exception();
    }
}