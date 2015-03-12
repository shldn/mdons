using UnityEngine;
using System.Collections.Generic;

public class GenericSimScreen : BizSimScreen {
    private static List<GenericSimScreen> genericScreens = new List<GenericSimScreen>();
    public static List<GenericSimScreen> GetAll() { return genericScreens; }

    protected override void Awake()
    {
        base.Awake();
        genericScreens.Add(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        genericScreens.Remove(this);
    }
}
