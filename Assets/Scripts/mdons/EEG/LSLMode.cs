using UnityEngine;
using System.Collections;

public class LSLMode{
    static bool sending = false;
    public static bool Sending { get { return sending; } }
    public static bool Receiving { get { return !sending; } }
}
