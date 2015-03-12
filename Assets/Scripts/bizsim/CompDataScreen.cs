using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CompDataScreen : BizSimScreen {
    private static List<CompDataScreen> compDataScreens = new List<CompDataScreen>();
    new public static List<CompDataScreen> GetAll() { return compDataScreens; }
    static bool showOneScreenAtATime = false;
    public int orderIndex;
    protected override void Awake()
    {
        base.Awake();
        compDataScreens.Add(this);
    }

    void OnEnable()
    {
        if( showOneScreenAtATime )
            SetAllOthersInvisible();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        compDataScreens.Remove(this);
    }

    void SetAllOthersInvisible()
    {
        foreach (CompDataScreen screen in compDataScreens)
            if (screen != this)
                screen.gameObject.SetActive(false);
    }
}
